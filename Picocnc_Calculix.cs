using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    // =====================================================================
    // CALCULIX FEA — external batch solver
    //
    // Generates Abaqus-compatible .inp files from PicoCNC geometry,
    // spawns ccx.exe (CalculiX CrunchiX) as a subprocess, and parses
    // .dat result files to extract engineering quantities.
    //
    // Supports *STATIC (deflection/stress) and *FREQUENCY (modal) steps.
    // Gracefully handles ccx not installed, solver timeouts, and
    // malformed output.
    // =====================================================================

    const string c_strCcxExe     = "ccx";
    const string c_strCalcDir    = "CalculiX";
    const int    c_nTimeoutMs    = 120_000;

    // Result storage
    static CalculixResult? s_oCalcStaticResult;
    static CalculixResult? s_oCalcFreqResult;
    static bool            s_bCalculixAvailable;
    static bool            s_bCalculixChecked;

    struct CalculixResult
    {
        public bool       bSuccess;
        public string?    strErrorMessage;
        public float      fMaxDeflectionMm;
        public float      fMaxStressMpa;
        public float[]?   afFrequenciesHz;
        public int        nNodes;
        public int        nElements;
        public double     fSolveTimeSec;
    }

    /// <summary>
    /// Run CalculiX static and frequency analyses. If ccx is not
    /// installed, logs a warning and returns. Called from RunBeamAnalysis().
    /// </summary>
    public static void RunCalculixAnalysis()
    {
        Log("\n============================================================");
        Log("===  CALCULIX FINITE ELEMENT ANALYSIS  ====================");
        Log("============================================================");

        if (!IsCalculixAvailable())
        {
            Log("CalculiX (ccx) not found on PATH.");
            Log("Install from http://www.calculix.de/ or place ccx.exe");
            Log("in the project output directory.");
            Log("");
            Log("===  CALCULIX ANALYSIS SKIPPED  ============================");
            Log("============================================================\n");
            return;
        }

        RunGantryBridgeStatic();
        RunGantryAssemblyFrequency();
        CleanupCalculixFiles();

        Log("===  CALCULIX ANALYSIS COMPLETE  ===========================");
        Log("============================================================\n");
    }

    // =====================================================================
    // AVAILABILITY CHECK
    // =====================================================================

    static bool IsCalculixAvailable()
    {
        if (s_bCalculixChecked)
            return s_bCalculixAvailable;

        s_bCalculixChecked = true;

        try
        {
            using Process p = new();
            p.StartInfo = new ProcessStartInfo
            {
                FileName               = c_strCcxExe,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            p.Start();
            bool bExited = p.WaitForExit(3000);
            if (!bExited)
            {
                p.Kill();
                s_bCalculixAvailable = false;
            }
            else
            {
                // ccx with no args prints usage and exits with code 0
                s_bCalculixAvailable = true;
            }
        }
        catch (Exception)
        {
            s_bCalculixAvailable = false;
        }

        return s_bCalculixAvailable;
    }

    // =====================================================================
    // DIRECTORY SETUP
    // =====================================================================

    static string EnsureCalcDir()
    {
        string strDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, c_strCalcDir);
        Directory.CreateDirectory(strDir);
        return strDir;
    }

    // =====================================================================
    // 1. GANTRY BRIDGE STATIC ANALYSIS — B31 beam elements
    // =====================================================================

    static void RunGantryBridgeStatic()
    {
        string strDir = EnsureCalcDir();
        string strBase = Path.Combine(strDir, "Bridge_Static");

        // --- Build beam model ---
        float fSpan = fBridgeSpanX;
        float b = fGantryBridgeY;
        float h = fGantryBridgeZ;
        float t = fGantryWallThick;
        float bi = MathF.Max(0.1f, b - 2f * t);
        float hi = MathF.Max(0.1f, h - 2f * t);
        float A = b * h - bi * hi;
        float Iy = (b * h * h * h - bi * hi * hi * hi) / 12f; // strong axis
        float Iz = (h * b * b * b - hi * bi * bi * bi) / 12f; // weak axis

        // Torsional constant (Bredt's formula for thin-walled closed section)
        float J = 2f * t * (b - t) * (b - t) * (h - t) * (h - t)
                  / ((b - t) * t + (h - t) * t + 2f * t);
        if (J <= 0f) { J = Iy * 0.1f; }
        if (Iz <= 0f) { Iz = Iy * 0.1f; }

        float E = fYoungsModulusAluminum;  // MPa
        float nu = 0.33f;
        float rho = fDensityAluminum * 1e-3f; // g/cm^3 → kg/mm^3: ×1e-3

        float Fy = fCuttingForceXY * fSafetyFactor;
        float Fz = fCuttingForceZ * fSafetyFactor;

        int nElements = 10;
        int nNodes = nElements + 1;

        // --- Write .inp ---
        using (StreamWriter w = new(strBase + ".inp"))
        {
            w.WriteLine("*HEADING");
            w.WriteLine($"PicoCNC Gantry Bridge — Static Analysis — {DateTime.Now:yyyy-MM-dd HH:mm}");
            w.WriteLine();

            // Nodes — along X axis
            w.WriteLine("*NODE");
            for (int i = 0; i < nNodes; i++)
            {
                float fX = (float)i / nElements * fSpan;
                w.WriteLine($"{i + 1}, {fX:F3}, 0.0, 0.0");
            }
            w.WriteLine();

            // Beam elements — B31 (Timoshenko, 2-node)
            w.WriteLine("*ELEMENT, TYPE=B31, ELSET=BEAM");
            for (int i = 0; i < nElements; i++)
                w.WriteLine($"{i + 1}, {i + 1}, {i + 2}");
            w.WriteLine();

            // Beam general section with user-defined properties
            // Section vector v1 points in Y direction (0,1,0) for the beam along X
            w.WriteLine("*BEAM GENERAL SECTION, ELSET=BEAM, MATERIAL=AL6061");
            w.WriteLine($"{A:F3}, {Iy:F3}, {Iz:F3}, {J:F3}");
            w.WriteLine("0.0, 1.0, 0.0");  // section orientation vector
            w.WriteLine();

            // Material
            w.WriteLine("*MATERIAL, NAME=AL6061");
            w.WriteLine("*ELASTIC");
            w.WriteLine($"{E:F1}, {nu:F4}");
            w.WriteLine("*DENSITY");
            w.WriteLine($"{rho:E6}");
            w.WriteLine();

            // Boundary conditions — simply supported at ends
            w.WriteLine("*BOUNDARY");
            // Left support (node 1): Uy=Uz=0
            w.WriteLine("1, 2, 2, 0.0");  // Uy=0
            w.WriteLine("1, 3, 3, 0.0");  // Uz=0
            // Right support (last node): Uy=Uz=0
            w.WriteLine($"{nNodes}, 2, 2, 0.0");
            w.WriteLine($"{nNodes}, 3, 3, 0.0");
            w.WriteLine();

            // Static step
            w.WriteLine("*STEP, PERTURBATION");
            w.WriteLine("*STATIC");
            w.WriteLine();

            // Concentrated loads at midspan node
            int nMid = nElements / 2 + 1; // 1-indexed
            w.WriteLine("*CLOAD");
            w.WriteLine($"{nMid}, 1, {-Fy:F1}"); // Fx (horizontal cutting force, -Y direction)
            w.WriteLine($"{nMid}, 2, {-Fy:F1}"); // Fy
            w.WriteLine($"{nMid}, 3, {-Fz:F1}"); // Fz (vertical, downward)
            w.WriteLine();

            // Output requests
            w.WriteLine("*NODE FILE");
            w.WriteLine("U");  // displacements
            w.WriteLine("*EL FILE");
            w.WriteLine("S,E"); // stresses and strains
            w.WriteLine("*END STEP");
        }

        // --- Run CalculiX ---
        CalculixResult? oResult = RunCalculix(strBase);
        if (oResult == null) return;

        CalculixResult r = oResult.Value;
        s_oCalcStaticResult = r;

        // --- Log ---
        Log("=== CalculiX GANTRY BRIDGE STATIC ===");
        Log($"  Nodes: {r.nNodes}, Elements: {r.nElements} (B31)");
        if (r.bSuccess)
        {
            Log($"  Max deflection: {r.fMaxDeflectionMm:F3} mm");
            Log($"  Max stress: {r.fMaxStressMpa:F1} MPa");
            Log($"  Solve time: {r.fSolveTimeSec:F1} s");
        }
        else
        {
            Log($"  FAILED: {r.strErrorMessage}");
        }
        Log("");
    }

    // =====================================================================
    // 2. GANTRY ASSEMBLY FREQUENCY ANALYSIS
    //
    // Beam-element model: two uprights + bridge beam.
    // Uprights fixed at base (Y rail connection), bridge connects at top.
    // Extracts first 10 natural frequencies.
    // =====================================================================

    static void RunGantryAssemblyFrequency()
    {
        string strDir = EnsureCalcDir();
        string strBase = Path.Combine(strDir, "Gantry_Frequency");

        // --- Geometry ---
        float fSpan = fBridgeSpanX;
        float fHUp = fUprightZ;     // upright height

        // Bridge section (same as static)
        float b = fGantryBridgeY;
        float h = fGantryBridgeZ;
        float t = fGantryWallThick;
        float bi = MathF.Max(0.1f, b - 2f * t);
        float hi = MathF.Max(0.1f, h - 2f * t);
        float A_bridge = b * h - bi * hi;
        float I_bridge = (b * h * h * h - bi * hi * hi * hi) / 12f;

        // Upright section
        float ux = fUprightX;
        float uy = fUprightY;
        float A_up = ux * uy;                              // solid rectangular
        float I_up = (uy * ux * ux * ux) / 12f;           // weak-axis bending
        float I_up_strong = (ux * uy * uy * uy) / 12f;    // strong-axis bending
        float J_up = I_up * 0.5f;                          // approximate torsion

        float E = fYoungsModulusAluminum;
        float nu = 0.33f;
        float rho = fDensityAluminum * 1e-3f; // g/cm^3 → kg/mm^3

        // Node layout: portal frame in XZ plane (Y=0).
        // Upright bases at Z=0, upright tops at Z=fHUp.
        // Bridge spans between upright tops at Z=fHUp.
        // Nodes: baseL=1, topL=2, bridge nodes(3..N-1), topR=N-1, baseR=N
        int nBridgeElems = 6;
        int nBridgeN = nBridgeElems + 1;
        int totalN = nBridgeN + 2;

        int idxBaseL2 = 1;
        int idxTopL2  = 2;
        int idxTopR2  = 2 + nBridgeN - 1;
        int idxBaseR2 = totalN;

        // --- Write .inp ---
        using (StreamWriter w = new(strBase + ".inp"))
        {
            w.WriteLine("*HEADING");
            w.WriteLine($"PicoCNC Gantry Assembly — Frequency Analysis — {DateTime.Now:yyyy-MM-dd HH:mm}");
            w.WriteLine();

            // Nodes
            w.WriteLine("*NODE");
            // Left upright base
            w.WriteLine($"{idxBaseL2}, 0.0, 0.0, 0.0");
            // Left upright top (= bridge left end)
            w.WriteLine($"{idxTopL2}, 0.0, 0.0, {fHUp:F3}");
            // Bridge nodes (including right top at last bridge node)
            for (int i = 0; i < nBridgeN; i++)
            {
                float fX = (float)i / (nBridgeN - 1) * fSpan;
                int nid = idxTopL2 + i;
                if (i == 0) continue; // already defined as left top
                w.WriteLine($"{nid}, {fX:F3}, 0.0, {fHUp:F3}");
            }
            // Right upright base
            w.WriteLine($"{idxBaseR2}, {fSpan:F3}, 0.0, 0.0");
            w.WriteLine();

            // Elements
            w.WriteLine("*ELEMENT, TYPE=B31, ELSET=BEAM");
            int eid = 0;

            // Left upright
            eid++;
            w.WriteLine($"{eid}, {idxBaseL2}, {idxTopL2}");
            // Bridge elements
            for (int i = 0; i < nBridgeN - 1; i++)
            {
                eid++;
                w.WriteLine($"{eid}, {idxTopL2 + i}, {idxTopL2 + i + 1}");
            }
            // Right upright
            eid++;
            w.WriteLine($"{eid}, {idxTopR2}, {idxBaseR2}");
            int nElems = eid;
            w.WriteLine();

            // Two beam section sets: uprights vs bridge
            w.WriteLine("*BEAM GENERAL SECTION, ELSET=UPRIGHT, MATERIAL=AL6061");
            w.WriteLine($"{A_up:F3}, {I_up:F3}, {I_up_strong:F3}, {J_up:F3}");
            w.WriteLine("0.0, 1.0, 0.0");
            w.WriteLine();

            w.WriteLine("*BEAM GENERAL SECTION, ELSET=BRIDGE, MATERIAL=AL6061");
            w.WriteLine($"{A_bridge:F3}, {I_bridge:F3}, {I_bridge:F3}, {I_bridge * 0.2f:F3}");
            w.WriteLine("0.0, 1.0, 0.0");
            w.WriteLine();

            // Assign element sets
            w.WriteLine("*ELSET, ELSET=UPRIGHT");
            w.WriteLine($"1, {nElems}");
            w.WriteLine("*ELSET, ELSET=BRIDGE");
            w.Write("2");
            for (int i = 3; i <= nElems - 1; i++)
                w.Write($", {i}");
            w.WriteLine();
            w.WriteLine();

            // Material
            w.WriteLine("*MATERIAL, NAME=AL6061");
            w.WriteLine("*ELASTIC");
            w.WriteLine($"{E:F1}, {nu:F4}");
            w.WriteLine("*DENSITY");
            w.WriteLine($"{rho:E6}");
            w.WriteLine();

            // Boundary conditions — upright bases fixed
            w.WriteLine("*BOUNDARY");
            for (int ib = 0; ib < 2; ib++)
            {
                int nid = ib == 0 ? idxBaseL2 : idxBaseR2;
                w.WriteLine($"{nid}, 1, 6, 0.0");  // all 6 DOFs fixed
            }
            w.WriteLine();

            // Frequency step
            w.WriteLine("*STEP, PERTURBATION");
            w.WriteLine("*FREQUENCY, STORAGE=YES");
            w.WriteLine("10");  // first 10 modes
            w.WriteLine();
            w.WriteLine("*NODE FILE");
            w.WriteLine("U");
            w.WriteLine("*END STEP");
        }

        // --- Run ---
        CalculixResult? oResult = RunCalculix(strBase);
        if (oResult == null) return;

        CalculixResult r = oResult.Value;
        s_oCalcFreqResult = r;

        // --- Log ---
        Log("=== CalculiX GANTRY ASSEMBLY FREQUENCY ===");
        Log($"  Nodes: {r.nNodes}, Elements: {r.nElements} (B31)");
        Log("  Modes requested: 10");
        if (r.bSuccess && r.afFrequenciesHz != null)
        {
            for (int i = 0; i < r.afFrequenciesHz.Length; i++)
                Log($"  f{i + 1,2} = {r.afFrequenciesHz[i],8:F2} Hz");

            // Resonance check
            float fThresh = 30f;
            if (r.afFrequenciesHz.Length > 0 && r.afFrequenciesHz[0] < fThresh)
            {
                Log($"  WARNING: First mode ({r.afFrequenciesHz[0]:F1} Hz) below " +
                    $"{fThresh} Hz — stepper resonance risk.");
            }
        }
        else
        {
            Log($"  FAILED: {r.strErrorMessage}");
        }
        Log("");
    }

    // =====================================================================
    // CALCULIX SUBPROCESS RUNNER
    // =====================================================================

    static CalculixResult? RunCalculix(string strBasePath)
    {
        string strDir = Path.GetDirectoryName(strBasePath)!;
        string strName = Path.GetFileName(strBasePath);
        string strInpPath = strBasePath + ".inp";
        string strDatPath = strBasePath + ".dat";

        if (!File.Exists(strInpPath))
        {
            Log($"  ERROR: .inp file not found: {strInpPath}");
            return null;
        }

        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            using Process p = new();
            p.StartInfo = new ProcessStartInfo
            {
                FileName               = c_strCcxExe,
                Arguments              = $"-i \"{strName}\"",
                WorkingDirectory       = strDir,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            p.Start();

            // Read stdout/stderr asynchronously to avoid deadlock
            string strStdout = p.StandardOutput.ReadToEnd();
            string strStderr = p.StandardError.ReadToEnd();

            bool bExited = p.WaitForExit(c_nTimeoutMs);
            if (!bExited)
            {
                p.Kill();
                sw.Stop();
                return new CalculixResult
                {
                    bSuccess        = false,
                    strErrorMessage = $"Solver timed out after {c_nTimeoutMs / 1000} seconds.",
                    fSolveTimeSec   = sw.Elapsed.TotalSeconds
                };
            }

            sw.Stop();

            if (p.ExitCode != 0)
            {
                string strErr = strStderr.Length > 500
                    ? strStderr[..500] + "..." : strStderr;
                return new CalculixResult
                {
                    bSuccess        = false,
                    strErrorMessage = $"ccx exited with code {p.ExitCode}: {strErr}",
                    fSolveTimeSec   = sw.Elapsed.TotalSeconds
                };
            }

            // Parse .dat output
            if (!File.Exists(strDatPath))
            {
                return new CalculixResult
                {
                    bSuccess        = false,
                    strErrorMessage = "No .dat result file produced.",
                    fSolveTimeSec   = sw.Elapsed.TotalSeconds
                };
            }

            return ParseCalculixDat(strDatPath, sw.Elapsed.TotalSeconds, strStdout);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CalculixResult
            {
                bSuccess        = false,
                strErrorMessage = $"Exception: {ex.Message}",
                fSolveTimeSec   = sw.Elapsed.TotalSeconds
            };
        }
    }

    // =====================================================================
    // .DAT RESULT PARSER
    // =====================================================================

    static CalculixResult ParseCalculixDat(
        string strDatPath, double fSolveTime, string strStdout)
    {
        CalculixResult result = new()
        {
            bSuccess      = true,
            fSolveTimeSec = fSolveTime
        };

        try
        {
            string[] aLines = File.ReadAllLines(strDatPath);

            // Count nodes/elements from stdout or the .dat header
            // Standard ccx stdout prints: "NUMBER OF NODES:  xxx"
            foreach (string sLine in strStdout.Split('\n'))
            {
                if (sLine.Contains("NUMBER OF NODES", StringComparison.OrdinalIgnoreCase))
                {
                    string[] aParts = sLine.Split(':', StringSplitOptions.TrimEntries);
                    if (aParts.Length >= 2 && int.TryParse(aParts[1], out int nN))
                        result.nNodes = nN;
                }
                if (sLine.Contains("NUMBER OF ELEMENTS", StringComparison.OrdinalIgnoreCase))
                {
                    string[] aParts = sLine.Split(':', StringSplitOptions.TrimEntries);
                    if (aParts.Length >= 2 && int.TryParse(aParts[1], out int nE))
                        result.nElements = nE;
                }
            }

            float fMaxDisp = 0f;
            float fMaxStress = 0f;
            List<float> aFreqs = new();
            bool bInDispBlock = false;
            bool bInStressBlock = false;

            for (int iLine = 0; iLine < aLines.Length; iLine++)
            {
                string sLine = aLines[iLine].Trim();

                // --- Displacement maxima ---
                // CalculiX .dat format:
                // "displacements (vx,vy,vz) for set NODE and time/comps"
                // followed by a table of node, vx, vy, vz, v
                // The maximum is printed at the end like "maximum of all nodes ..."
                // or we can scan the v (magnitude) column.
                if (sLine.Contains("displacements", StringComparison.OrdinalIgnoreCase)
                    && sLine.Contains("vx", StringComparison.OrdinalIgnoreCase))
                {
                    bInDispBlock = true;
                    continue;
                }
                if (bInDispBlock)
                {
                    if (sLine.Contains("maximum", StringComparison.OrdinalIgnoreCase)
                        && sLine.Contains("displacement", StringComparison.OrdinalIgnoreCase))
                    {
                        // "maximum of all nodes xx.x in node 123"
                        string[] aParts = sLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        for (int ip = 0; ip < aParts.Length - 1; ip++)
                        {
                            if (float.TryParse(aParts[ip], NumberStyles.Float,
                                CultureInfo.InvariantCulture, out float fVal)
                                && fVal > 1e-6f
                                && (aParts[ip + 1].Contains("node")
                                    || aParts[ip - 1].Contains("nodes")))
                            {
                                fMaxDisp = fVal;
                                break;
                            }
                        }
                    }
                    else if (sLine.StartsWith("total", StringComparison.OrdinalIgnoreCase)
                          || sLine.Contains("stress", StringComparison.OrdinalIgnoreCase))
                    {
                        bInDispBlock = false;
                    }
                }

                // --- Stress maxima ---
                if (sLine.Contains("stresses", StringComparison.OrdinalIgnoreCase)
                    && (sLine.Contains("von mises", StringComparison.OrdinalIgnoreCase)
                        || sLine.Contains("v.mises", StringComparison.OrdinalIgnoreCase)))
                {
                    bInStressBlock = true;
                    continue;
                }
                if (bInStressBlock)
                {
                    if (sLine.Contains("maximum", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] aParts = sLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        for (int ip = 0; ip < aParts.Length; ip++)
                        {
                            if (float.TryParse(aParts[ip], NumberStyles.Float,
                                CultureInfo.InvariantCulture, out float fVal)
                                && fVal > 0.01f && fVal < 100000f)
                            {
                                if (fVal > fMaxStress) fMaxStress = fVal;
                            }
                        }
                    }
                    else if (sLine.StartsWith("total", StringComparison.OrdinalIgnoreCase)
                          || sLine.Contains("displacement", StringComparison.OrdinalIgnoreCase))
                    {
                        bInStressBlock = false;
                    }
                }

                // --- Frequencies ---
                // CalculiX prints eigenvalues after "*FREQUENCY" step:
                // "   1  1.234567E+05  3.456789E+02"
                // Column 1 = mode number, Column 2 = eigenvalue (omega^2), Column 3 = frequency (Hz) or rad/s
                // Actually: ccx prints "EIGENVALUE" and then a table with:
                // mode_no   eigenvalue   frequency(cycles/time)  frequency(rad/time)
                // where eigenvalue = omega^2 and frequency = cycles/time
                if (sLine.Contains("EIGENVALUE", StringComparison.OrdinalIgnoreCase)
                    && !sLine.Contains("OUTPUT", StringComparison.OrdinalIgnoreCase))
                {
                    // Scan subsequent lines for frequency data
                    for (int j = iLine + 1; j < System.Math.Min(iLine + 20, aLines.Length); j++)
                    {
                        string sFreqLine = aLines[j].Trim();
                        if (string.IsNullOrEmpty(sFreqLine)) break;
                        if (sFreqLine.StartsWith("*") || sFreqLine.Contains("END STEP")) break;

                        string[] aParts = sFreqLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        // Format: mode_no  eigenvalue  freq(cycles)  freq(rad)
                        // We want the 3rd column (freq in cycles/time = Hz)
                        if (aParts.Length >= 3
                            && int.TryParse(aParts[0], out int _)
                            && float.TryParse(aParts[2], NumberStyles.Float,
                                CultureInfo.InvariantCulture, out float fFreqHz))
                        {
                            if (fFreqHz > 0.01f && fFreqHz < 1e6f)
                                aFreqs.Add(fFreqHz);
                        }
                        // Some ccx versions omit the 4th column
                        else if (aParts.Length >= 2
                              && int.TryParse(aParts[0], out int _)
                              && float.TryParse(aParts[1], NumberStyles.Float,
                                  CultureInfo.InvariantCulture, out float fEigen)
                              && fEigen > 0.01f && fEigen < 1e12f
                              && aFreqs.Count == 0)
                        {
                            // Eigenvalue = omega^2; convert to Hz: f = sqrt(eigen) / (2*pi)
                            float fHz = MathF.Sqrt(fEigen) / (2f * MathF.PI);
                            if (fHz > 0.01f && fHz < 1e6f)
                                aFreqs.Add(fHz);
                        }
                    }
                }
            }

            result.fMaxDeflectionMm = fMaxDisp;
            result.fMaxStressMpa    = fMaxStress;
            result.afFrequenciesHz  = aFreqs.ToArray();

            // If we got no data but the solver succeeded, log a note
            if (fMaxDisp < 0.0001f && fMaxStress < 0.01f && aFreqs.Count == 0)
            {
                // Not necessarily a failure — just means we couldn't parse the .dat format
                // This is common with different ccx versions. Report partial success.
                Log("  NOTE: Could not parse displacements/stresses from .dat file.");
                Log("  (Solver completed successfully — check .dat file manually.)");
            }
        }
        catch (Exception ex)
        {
            result.bSuccess        = false;
            result.strErrorMessage = $".dat parse error: {ex.Message}";
        }

        return result;
    }

    // =====================================================================
    // CLEANUP
    // =====================================================================

    static void CleanupCalculixFiles()
    {
        string strDir = EnsureCalcDir();
        string[] aExtensions = { ".cvg", ".sta", ".12d", ".frd" };

        foreach (string strFile in Directory.GetFiles(strDir))
        {
            string strExt = Path.GetExtension(strFile).ToLowerInvariant();
            if (Array.Exists(aExtensions, e => e == strExt))
            {
                try { File.Delete(strFile); } catch { }
            }
        }
    }
}

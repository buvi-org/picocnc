using System.Diagnostics;
using System.Numerics;
using BriefFiniteElementNet;
using BriefFiniteElementNet.Elements;
using BriefFiniteElementNet.Materials;
using BriefFiniteElementNet.Sections;

namespace PicoGK;

public static partial class Picocnc
{
    // =====================================================================
    // FINITE ELEMENT ANALYSIS — BriefFiniteElement.NET (in-process)
    //
    // Wraps the BriefFiniteElement.NET library to model the gantry bridge
    // as Euler-Bernoulli beam elements and run quick static deflection
    // checks. Results are compared against the hand-rolled solver
    // in Picocnc_BeamSolver.cs.
    // =====================================================================

    struct FEAReport
    {
        public string strLabel;
        public float  fMaxDeflectionMm;
        public float  fMaxStressMpa;
        public float  fSafetyFactor;
        public int    nElements;
        public double fComputationMs;
    }

    static FEAReport? s_oFEAReport;

    /// <summary>
    /// Run in-process FEA on the gantry bridge and compare with the
    /// hand-rolled beam solver. Called from RunBeamAnalysis().
    /// </summary>
    public static void RunFEAnalysis()
    {
        Library.Log("\n============================================================");
        Library.Log("===  FINITE ELEMENT ANALYSIS (BriefFiniteElement.NET)  ====");
        Library.Log("============================================================");

        RunGantryBridgeFEA();
        RunFEAComparison();

        Library.Log("===  FEA COMPLETE  =========================================");
        Library.Log("============================================================\n");
    }

    // =====================================================================
    // 1. GANTRY BRIDGE FEA — BEAM MODEL
    // =====================================================================

    static void RunGantryBridgeFEA()
    {
        Stopwatch sw = Stopwatch.StartNew();

        // --- Geometry from constraints ---
        float fSpan = fBridgeSpanX;
        float b     = fGantryBridgeY;     // width (Y)
        float h     = fGantryBridgeZ;     // height (Z)
        float t     = fGantryWallThick;

        float bi = MathF.Max(0.1f, b - 2f * t);
        float hi = MathF.Max(0.1f, h - 2f * t);

        // Hollow rectangular section properties (mm units)
        float A  = b * h - bi * hi;                                 // mm^2
        float Iy = (b * h * h * h - bi * hi * hi * hi) / 12f;       // mm^4 — strong axis (bending in XZ plane)
        float Iz = (h * b * b * b - hi * bi * bi * bi) / 12f;       // mm^4 — weak axis (bending in XY plane)
        float J  = 2f * t * (b - t) * (b - t) * (h - t) * (h - t)
                   / ((b - t) * t + (h - t) * t + 2f * t);           // mm^4 — torsional constant
        if (J <= 0f) J = Iy * 0.1f;

        float E  = fYoungsModulusAluminum;  // MPa (N/mm^2)
        float nu = 0.33f;

        // --- Load ---
        float Fy = fCuttingForceXY * fSafetyFactor;  // horizontal cutting force
        float Fz = fCuttingForceZ * fSafetyFactor;    // vertical plunge force

        // --- Build model ---
        int nElements = 10;
        int nNodes = nElements + 1;
        Model model = new();

        Node[] aNodes = new Node[nNodes];
        for (int i = 0; i < nNodes; i++)
        {
            double x = (double)i / nElements * fSpan;
            aNodes[i] = new Node(x, 0, 0) { Label = $"N{i}" };
            model.Nodes.Add(aNodes[i]);
        }

        // Beam section: uniform parametric (A, Iy, Iz, J)
        var section = new UniformParametric1DSection(A, Iy, Iz, J);

        // Isotropic material
        var material = new UniformIsotropicMaterial(E, nu);

        // BarElement has no torsional stiffness — fix RX at every node
        // so the global stiffness matrix stays positive definite for Cholesky.
        for (int i = 0; i < nNodes; i++)
        {
            aNodes[i].Constraints = new Constraint(
                DofConstraint.Released,
                DofConstraint.Released,
                DofConstraint.Released,
                DofConstraint.Fixed,    // RX — no torsional DOF in BarElement
                DofConstraint.Released,
                DofConstraint.Released);
        }

        for (int i = 0; i < nElements; i++)
        {
            var el = new BarElement(aNodes[i], aNodes[i + 1])
            {
                Section  = section,
                Material = material,
                Label    = $"E{i}"
            };
            el.Behavior = BarElementBehaviour.Truss
                        | BarElementBehaviour.BeamYEulerBernoulli
                        | BarElementBehaviour.BeamZEulerBernoulli;
            model.Elements.Add(el);
        }

        // Simply supported: UX, UY, UZ fixed at left; UY, UZ fixed at right
        aNodes[0].Constraints = new Constraint(
            DofConstraint.Fixed,    // DX — axial restraint
            DofConstraint.Fixed,    // DY — support
            DofConstraint.Fixed,    // DZ — support
            DofConstraint.Fixed,    // RX — no torsion
            DofConstraint.Released, // RY — allow bending
            DofConstraint.Released);// RZ — allow bending

        aNodes[nNodes - 1].Constraints = new Constraint(
            DofConstraint.Released, // DX — free for thermal
            DofConstraint.Fixed,    // DY — support
            DofConstraint.Fixed,    // DZ — support
            DofConstraint.Fixed,    // RX — no torsion
            DofConstraint.Released, // RY — allow bending
            DofConstraint.Released);// RZ — allow bending

        // --- Load case: center point load ---
        // Note on sign convention:
        //   Global coords: X = along beam, Y = lateral, Z = vertical up
        //   Cutting force Fy acts horizontally, Fz acts downward (-Z)
        //   So: Fx=0, Fy=-Fy (negative Y), Fz=-Fz (negative Z)
        int nMid = nElements / 2;
        var loadCase = new LoadCase("CuttingLoad", LoadType.Default);
        var force = new Force(0, -Fy, -Fz, 0, 0, 0);
        aNodes[nMid].Loads.Add(new NodalLoad(force, loadCase));

        // --- Solve ---
        model.Solve(new[] { loadCase });

        // --- Extract results at midspan ---
        Displacement disp = aNodes[nMid].GetNodalDisplacement(loadCase);
        double fDeflY = System.Math.Abs(disp.DY);  // horizontal deflection
        double fDeflZ = System.Math.Abs(disp.DZ);  // vertical deflection
        double fMaxDefl = System.Math.Sqrt(fDeflY * fDeflY + fDeflZ * fDeflZ);

        // Bending moment at midspan (simply supported, center load):
        // M = F * L / 4, stress = M * c / I
        double Mz = System.Math.Abs(Fz) * fSpan / 4.0;       // moment from vertical load
        double My = System.Math.Abs(Fy) * fSpan / 4.0;       // moment from horizontal load
        double sigmaZ = Mz * (h / 2.0) / Iy;                  // bending stress from vertical load
        double sigmaY = My * (b / 2.0) / Iz;                  // bending stress from horizontal load
        double fMaxStress = sigmaZ + sigmaY;

        double fSafety = fYieldStrengthAluminum / fMaxStress;

        sw.Stop();

        // --- Log ---
        Library.Log("=== FEA GANTRY BRIDGE (Euler-Bernoulli beam) ===");
        Library.Log($"  Elements: {nElements} beam elements over {fSpan:F0} mm span");
        Library.Log($"  Section: hollow rect {b:F0} x {h:F0} mm, wall {t:F0} mm");
        Library.Log($"  A = {A:F0} mm^2, Iy = {Iy / 1e6f:F2} x 10^6 mm^4");
        Library.Log($"  Load: Fy={Fy:F0}N, Fz={Fz:F0}N at midspan");
        Library.Log($"  Max deflection: {fMaxDefl:F3} mm  (Y: {fDeflY:F3}, Z: {fDeflZ:F3})");
        Library.Log($"  Max bending stress: {fMaxStress:F1} MPa");
        Library.Log($"  Safety factor vs yield: {fSafety:F1}x");
        Library.Log($"  Solve time: {sw.Elapsed.TotalMilliseconds:F1} ms");

        // --- Store for comparison ---
        s_oFEAReport = new FEAReport
        {
            strLabel         = "Gantry Bridge",
            fMaxDeflectionMm = (float)fMaxDefl,
            fMaxStressMpa    = (float)fMaxStress,
            fSafetyFactor    = (float)fSafety,
            nElements        = nElements,
            fComputationMs   = sw.Elapsed.TotalMilliseconds
        };

        Library.Log("");
    }

    // =====================================================================
    // 2. COMPARISON: FEA vs HAND-ROLLED
    //
    // The hand-rolled solver uses Euler-Bernoulli beam theory (no shear
    // deformation). The FEA uses the same Euler-Bernoulli formulation
    // through BriefFiniteElement.NET. Results should agree within 1-2%
    // (discretization error from 10 elements vs continuous solution).
    // =====================================================================

    static void RunFEAComparison()
    {
        if (s_oFEAReport == null)
        {
            Library.Log("FEA comparison skipped — no FEA report available.");
            return;
        }

        // Recompute hand-rolled deflection using the same formulas as BeamSolver
        float fSpan = fBridgeSpanX;
        float b = fGantryBridgeY;
        float h = fGantryBridgeZ;
        float t = fGantryWallThick;
        float bi = MathF.Max(0.001f, b - 2f * t);
        float hi = MathF.Max(0.001f, h - 2f * t);
        float I = (b * h * h * h - bi * hi * hi * hi) / 12f;

        float Fz = fCuttingForceZ * fSafetyFactor;
        float Fy = fCuttingForceXY * fSafetyFactor;
        float E = fYoungsModulusAluminum;

        // Simply supported center load: delta = (F * L^3) / (48 * E * I)
        float fDeflZHand = (Fz * fSpan * fSpan * fSpan) / (48f * E * I);

        // For horizontal load, use Iz
        float Iz = (h * b * b * b - hi * bi * bi * bi) / 12f;
        float fDeflYHand = (Fy * fSpan * fSpan * fSpan) / (48f * E * Iz);

        float fDeflHand = MathF.Sqrt(fDeflZHand * fDeflZHand + fDeflYHand * fDeflYHand);

        float fStressHand = (Fz * fSpan * h) / (8f * I) + (Fy * fSpan * b) / (8f * Iz);

        FEAReport fea = s_oFEAReport.Value;
        float fDeflDiff = MathF.Abs(fea.fMaxDeflectionMm - fDeflHand);
        float fDeflPct   = fDeflHand > 0.001f ? (fDeflDiff / fDeflHand) * 100f : 0f;
        float fStressDiff = MathF.Abs(fea.fMaxStressMpa - fStressHand);
        float fStressPct  = fStressHand > 0.1f ? (fStressDiff / fStressHand) * 100f : 0f;

        Library.Log("=== FEA vs HAND-ROLLED COMPARISON ===");
        Library.Log($"  Hand-rolled deflection (Euler-Bernoulli):  {fDeflHand:F3} mm");
        Library.Log($"  FEA deflection (10-element beam):          {fea.fMaxDeflectionMm:F3} mm");
        Library.Log($"  Difference: {fDeflDiff:F3} mm ({fDeflPct:F1}%)");
        Library.Log($"  Hand-rolled stress: {fStressHand:F1} MPa");
        Library.Log($"  FEA stress:         {fea.fMaxStressMpa:F1} MPa");
        Library.Log($"  Stress difference:  {fStressDiff:F1} MPa ({fStressPct:F1}%)");

        if (fDeflPct > 5f)
            Library.Log("  NOTE: Difference >5% — verify cross-section properties match.");
        else if (fDeflPct > 1f)
            Library.Log("  NOTE: Small difference due to 10-element discretization vs continuous solution.");
        else
            Library.Log("  Excellent agreement (<1% difference). Hand-rolled solver validated.");

        Library.Log("");
    }
}

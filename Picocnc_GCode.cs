using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Internal representation of a parsed G-code motion command.
    /// All values are absolute and in millimeters.
    /// </summary>
    public struct GCodeCommand
    {
        public float X, Y, Z;   // target position (mm, absolute)
        public float F;         // feed rate (mm/min)
        public float S;         // spindle speed (RPM)
        public bool  bHasX, bHasY, bHasZ, bHasF, bHasS;
        public bool  bIsRapid;  // true = G0 (traverse), false = G1 (feed)
    }

    /// <summary>
    /// Parse a G-code file and return a list of absolute tool positions
    /// in millimeters. Handles G0/G1 motion, G90/G91 absolute/incremental,
    /// G20/G21 inch/mm, comments, line numbers, and modal state.
    /// G2/G3 arcs are approximated as linear moves.
    /// </summary>
    public static List<Vector3> ParseGCode(string strFilePath)
    {
        return ParseGCodeFile(strFilePath);
    }

    /// <summary>
    /// Parse a G-code file into GCodeCommand list, then convert to
    /// absolute Vector3 tool positions in mm.
    /// </summary>
    public static List<Vector3> ParseGCodeFile(string strFilePath)
    {
        List<GCodeCommand> aCommands = ParseCommands(strFilePath);

        List<Vector3> aPositions = new();
        Vector3 vecCurrent = Vector3.Zero;

        foreach (GCodeCommand cmd in aCommands)
        {
            if (cmd.bHasX) vecCurrent.X = cmd.X;
            if (cmd.bHasY) vecCurrent.Y = cmd.Y;
            if (cmd.bHasZ) vecCurrent.Z = cmd.Z;
            aPositions.Add(new Vector3(vecCurrent.X, vecCurrent.Y, vecCurrent.Z));
        }

        return aPositions;
    }

    /// <summary>
    /// Core G-code line parser with full modal state tracking.
    /// </summary>
    static List<GCodeCommand> ParseCommands(string strFilePath)
    {
        List<GCodeCommand> aCommands = new();
        GCodeCommand curModal = new();   // persistent modal state

        bool  bAbsolute    = true;       // G90 (default)
        float fUnitScale   = 1.0f;       // 1.0 = mm, 25.4 = inches
        int   nCurrentG    = 0;          // modal motion mode (0 = rapid, 1 = feed)
        Vector3 vecPos     = Vector3.Zero; // tracked absolute position

        foreach (string strLine in File.ReadLines(strFilePath))
        {
            string line = strLine.Trim();

            // Skip empty lines
            if (string.IsNullOrEmpty(line))
                continue;

            // Skip block-delete lines
            if (line.StartsWith("/"))
                continue;

            // Strip comments: semicolon to EOL, and ( ... ) blocks
            line = StripComments(line);
            if (string.IsNullOrEmpty(line))
                continue;

            // Strip leading Nxxx line number
            line = StripLineNumber(line);
            if (string.IsNullOrEmpty(line))
                continue;

            // Parse all G-code words on this line
            List<(char letter, float value)> aWords = ParseWords(line);
            if (aWords.Count == 0)
                continue;

            // Build per-line command, seeded from modal state
            GCodeCommand cmdLine = curModal;
            bool bEmitCommand = false;

            foreach ((char ch, float fVal) in aWords)
            {
                switch (ch)
                {
                    case 'G':
                    {
                        int nG = (int)fVal;
                        switch (nG)
                        {
                            case 0:
                            case 1:
                                nCurrentG = nG;
                                bEmitCommand = true;
                                break;
                            case 90: bAbsolute  = true;  break;
                            case 91: bAbsolute  = false; break;
                            case 20: fUnitScale = 25.4f; break;
                            case 21: fUnitScale = 1.0f;  break;
                            // G2/G3 arcs, G17/G18/G19 planes, etc. — ignored
                        }
                        break;
                    }

                    case 'X':
                        cmdLine.X = fVal * fUnitScale;
                        cmdLine.bHasX = true;
                        bEmitCommand = true;
                        break;

                    case 'Y':
                        cmdLine.Y = fVal * fUnitScale;
                        cmdLine.bHasY = true;
                        bEmitCommand = true;
                        break;

                    case 'Z':
                        cmdLine.Z = fVal * fUnitScale;
                        cmdLine.bHasZ = true;
                        bEmitCommand = true;
                        break;

                    case 'F':
                        cmdLine.F = fVal * fUnitScale;
                        cmdLine.bHasF = true;
                        break;

                    case 'S':
                        cmdLine.S = fVal;
                        cmdLine.bHasS = true;
                        break;

                    // M-codes (M3, M5, M2, M30), I/J/K for arcs,
                    // T, P, Q, R, D, H, L — ignored
                }
            }

            if (bEmitCommand)
            {
                cmdLine.bIsRapid = (nCurrentG == 0);

                // Convert incremental coordinates to absolute
                if (!bAbsolute)
                {
                    if (cmdLine.bHasX) cmdLine.X += vecPos.X;
                    if (cmdLine.bHasY) cmdLine.Y += vecPos.Y;
                    if (cmdLine.bHasZ) cmdLine.Z += vecPos.Z;
                }

                // Fill in unspecified axes from modal position
                if (!cmdLine.bHasX) { cmdLine.X = vecPos.X; cmdLine.bHasX = true; }
                if (!cmdLine.bHasY) { cmdLine.Y = vecPos.Y; cmdLine.bHasY = true; }
                if (!cmdLine.bHasZ) { cmdLine.Z = vecPos.Z; cmdLine.bHasZ = true; }

                // Update tracked absolute position
                vecPos = new Vector3(cmdLine.X, cmdLine.Y, cmdLine.Z);

                // Persist modal state for subsequent lines
                curModal = cmdLine;

                aCommands.Add(cmdLine);
            }
        }

        return aCommands;
    }

    /// <summary>
    /// Remove semicolon comments (; ...) and parenthesised comments
    /// (( ... )) from a G-code line.
    /// </summary>
    static string StripComments(string line)
    {
        // Semicolon to end of line
        int iSemi = line.IndexOf(';');
        if (iSemi >= 0)
            line = line[..iSemi];

        // Parenthesis-block comments: ( ... )
        while (true)
        {
            int iOpen = line.IndexOf('(');
            if (iOpen < 0) break;

            int iClose = line.IndexOf(')', iOpen);
            if (iClose < 0)
                // Unterminated comment — truncate to open paren
                iClose = line.Length - 1;

            line = line[..iOpen] + line[(iClose + 1)..];
        }

        return line.Trim();
    }

    /// <summary>
    /// Strip a leading N-prefixed line number (e.g. "N100").
    /// Returns the remainder of the line.
    /// </summary>
    static string StripLineNumber(string line)
    {
        line = line.TrimStart();
        if (line.Length > 1 && char.ToUpperInvariant(line[0]) == 'N')
        {
            int i = 1;
            while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.'))
                i++;
            line = line[i..].TrimStart();
        }
        return line;
    }

    /// <summary>
    /// Parse G-code word tokens from a line.
    /// Each word consists of a letter (G, M, X, Y, Z, F, S, I, J, K, T,
    /// P, Q, R, D, H, L) optionally followed by whitespace, followed by a
    /// numeric value (integer or decimal, optionally signed).
    /// Spaces between words are optional.
    /// </summary>
    static List<(char letter, float value)> ParseWords(string line)
    {
        List<(char, float)> aWords = new();
        int i = 0;

        while (i < line.Length)
        {
            // Skip whitespace
            while (i < line.Length && char.IsWhiteSpace(line[i]))
                i++;
            if (i >= line.Length) break;

            char c = line[i];
            char upper = char.ToUpperInvariant(c);

            // Known G-code address letters
            if (upper is 'G' or 'M' or 'X' or 'Y' or 'Z'
                      or 'F' or 'S' or 'I' or 'J' or 'K'
                      or 'T' or 'P' or 'Q' or 'R' or 'D'
                      or 'H' or 'L' or 'N')
            {
                i++; // consume letter

                // Optional whitespace between letter and number
                while (i < line.Length && char.IsWhiteSpace(line[i]))
                    i++;

                // Parse numeric value (signed, with optional decimal point)
                int nStart = i;
                if (i < line.Length && (line[i] == '-' || line[i] == '+'))
                    i++;
                while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.'))
                    i++;

                if (i > nStart)
                {
                    string strNum = line[nStart..i];
                    if (float.TryParse(strNum,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out float fVal))
                    {
                        aWords.Add((upper, fVal));
                    }
                }
            }
            else
            {
                // Unrecognised character — skip
                i++;
            }
        }

        return aWords;
    }

    /// <summary>
    /// Visualize a list of tool positions as thin line segments in the
    /// PicoGK viewer. Each consecutive pair of positions becomes a lattice
    /// beam with 1 mm radius. Short collocated segments (under 0.5 mm)
    /// are skipped.
    /// </summary>
    public static void VisualizeToolpath(List<Vector3> aPositions)
    {
        Library.Log($"Visualizing toolpath: {aPositions.Count} points");

        Voxels voxToolpath = new();
        float fRadius = 1.0f; // thin visible line at 2 mm voxel resolution

        for (int iPos = 0; iPos < aPositions.Count - 1; iPos++)
        {
            Vector3 a = aPositions[iPos];
            Vector3 b = aPositions[iPos + 1];

            // Skip collocated points (no visible segment)
            if (Vector3.Distance(a, b) < 0.5f)
                continue;

            Voxels voxSegment = Voxels.voxLatticeBeam(a, fRadius, b, fRadius);
            voxToolpath += voxSegment;
        }

        Library.oViewer().Add(voxToolpath, 13);
        Library.Log("Toolpath added to viewer (group 13).");
    }

    /// <summary>
    /// Convenience method: parse a G-code file and immediately visualize
    /// the toolpath. Reports bounding-box bounds and warns if the toolpath
    /// exceeds the machine work area.
    /// </summary>
    public static void LoadAndVisualizeGCode(string strFilePath)
    {
        if (!File.Exists(strFilePath))
        {
            Library.Log($"G-code file not found: {strFilePath}");
            return;
        }

        Library.Log($"Loading G-code: {strFilePath}");

        List<Vector3> aPositions = ParseGCodeFile(strFilePath);
        Library.Log($"Parsed {aPositions.Count} tool positions.");

        if (aPositions.Count > 0)
        {
            VisualizeToolpath(aPositions);

            // Report bounding box
            float fMinX = aPositions.Min(p => p.X);
            float fMaxX = aPositions.Max(p => p.X);
            float fMinY = aPositions.Min(p => p.Y);
            float fMaxY = aPositions.Max(p => p.Y);
            float fMinZ = aPositions.Min(p => p.Z);
            float fMaxZ = aPositions.Max(p => p.Z);

            Library.Log($"Toolpath bounds X: [{fMinX:F1}, {fMaxX:F1}]");
            Library.Log($"Toolpath bounds Y: [{fMinY:F1}, {fMaxY:F1}]");
            Library.Log($"Toolpath bounds Z: [{fMinZ:F1}, {fMaxZ:F1}]");

            // Warn if toolpath exceeds work area
            if (fMaxX > fWorkAreaX || fMaxY > fWorkAreaY)
            {
                Library.Log($"WARNING: Toolpath exceeds work area ({fWorkAreaX}x{fWorkAreaY})!");
            }
        }
    }
}

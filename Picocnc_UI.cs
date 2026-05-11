namespace PicoGK;

using System.Diagnostics;

public static partial class Picocnc
{
    // ========================================================================
    // KEY INPUT TYPES (defined here because they don't exist in PicoGK v2.0-beta11)
    // ========================================================================

    /// <summary>
    /// Keyboard key identifiers for the interactive handler.
    /// Mirrors what the future PicoGK EKeys enum would provide.
    /// </summary>
    public enum EKeys
    {
        None = 0,
        A, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        Up, Down, Left, Right,
        Tab, Space, Enter, Backspace, Escape
    }

    /// <summary>
    /// Interface for keyboard event handlers. Expected to be part of
    /// a future PicoGK release; defined locally for now.
    /// </summary>
    public interface IKeyHandler
    {
        bool bHandleEvent(Viewer oViewer, EKeys eKey,
                          bool bPressed, bool bShift, bool bCtrl,
                          bool bAlt, bool bCmd);
    }

    // ========================================================================
    // INTERACTIVE STATE
    // ========================================================================

    static bool   s_bCollisionCheck    = false;
    static bool   s_bBeamAnalysis      = false;
    static int    s_nSelectedParamIdx  = 0;
    static string[] s_aParamKeys       = null!;
    static readonly bool[] s_aGroupVisible = new bool[14]; // index 0 unused, 1-13
    static readonly HashSet<EKeys> s_heldKeys = new();

    /// <summary>
    /// Maps ConsoleKey values to our EKeys enum for the console-based
    /// keyboard input fallback (used until PicoGK adds native IKeyHandler).
    /// </summary>
    static EKeys MapConsoleKey(ConsoleKey key)
    {
        return key switch
        {
            ConsoleKey.A => EKeys.A, ConsoleKey.B => EKeys.B, ConsoleKey.C => EKeys.C,
            ConsoleKey.D => EKeys.D, ConsoleKey.E => EKeys.E, ConsoleKey.F => EKeys.F,
            ConsoleKey.G => EKeys.G, ConsoleKey.H => EKeys.H, ConsoleKey.I => EKeys.I,
            ConsoleKey.J => EKeys.J, ConsoleKey.K => EKeys.K, ConsoleKey.L => EKeys.L,
            ConsoleKey.M => EKeys.M, ConsoleKey.N => EKeys.N, ConsoleKey.O => EKeys.O,
            ConsoleKey.P => EKeys.P, ConsoleKey.Q => EKeys.Q, ConsoleKey.R => EKeys.R,
            ConsoleKey.S => EKeys.S, ConsoleKey.T => EKeys.T, ConsoleKey.U => EKeys.U,
            ConsoleKey.V => EKeys.V, ConsoleKey.W => EKeys.W, ConsoleKey.X => EKeys.X,
            ConsoleKey.Y => EKeys.Y, ConsoleKey.Z => EKeys.Z,
            ConsoleKey.D0 => EKeys.D0, ConsoleKey.D1 => EKeys.D1,
            ConsoleKey.D2 => EKeys.D2, ConsoleKey.D3 => EKeys.D3,
            ConsoleKey.D4 => EKeys.D4, ConsoleKey.D5 => EKeys.D5,
            ConsoleKey.D6 => EKeys.D6, ConsoleKey.D7 => EKeys.D7,
            ConsoleKey.D8 => EKeys.D8, ConsoleKey.D9 => EKeys.D9,
            ConsoleKey.F1 => EKeys.F1, ConsoleKey.F2 => EKeys.F2,
            ConsoleKey.F3 => EKeys.F3, ConsoleKey.F4 => EKeys.F4,
            ConsoleKey.F5 => EKeys.F5, ConsoleKey.F6 => EKeys.F6,
            ConsoleKey.F7 => EKeys.F7, ConsoleKey.F8 => EKeys.F8,
            ConsoleKey.F9 => EKeys.F9, ConsoleKey.F10 => EKeys.F10,
            ConsoleKey.F11 => EKeys.F11, ConsoleKey.F12 => EKeys.F12,
            ConsoleKey.UpArrow => EKeys.Up,
            ConsoleKey.DownArrow => EKeys.Down,
            ConsoleKey.LeftArrow => EKeys.Left,
            ConsoleKey.RightArrow => EKeys.Right,
            ConsoleKey.Tab => EKeys.Tab,
            ConsoleKey.Spacebar => EKeys.Space,
            ConsoleKey.Enter => EKeys.Enter,
            ConsoleKey.Backspace => EKeys.Backspace,
            ConsoleKey.Escape => EKeys.Escape,
            _ => EKeys.None
        };
    }

    // ========================================================================
    // VIEWER SETUP
    // ========================================================================

    /// <summary>
    /// Configures viewer group materials with distinct colors per component type.
    /// </summary>
    public static void SetupViewer()
    {
        // Group 1: structural (steel gray)
        Library.oViewer().SetGroupMaterial(1, "8899AA", 0.3f, 0.2f);
        // Group 2: work bed (wood brown)
        Library.oViewer().SetGroupMaterial(2, "AA8844", 0.5f, 0.1f);
        // Group 3: rails (dark metal)
        Library.oViewer().SetGroupMaterial(3, "667788", 0.2f, 0.6f);
        // Group 4: uprights (blue-gray)
        Library.oViewer().SetGroupMaterial(4, "556688", 0.3f, 0.3f);
        // Group 5: gantry bridge (red-orange accent)
        Library.oViewer().SetGroupMaterial(5, "CC6633", 0.3f, 0.2f);
        // Group 6: X rails (dark metal)
        Library.oViewer().SetGroupMaterial(6, "667788", 0.2f, 0.6f);
        // Group 7: Z assembly (aluminum)
        Library.oViewer().SetGroupMaterial(7, "99AABB", 0.3f, 0.4f);
        // Group 8: spindle mount (dark gray)
        Library.oViewer().SetGroupMaterial(8, "444444", 0.3f, 0.3f);
        // Group 9: motor mounts (black)
        Library.oViewer().SetGroupMaterial(9, "222222", 0.4f, 0.2f);
        // Group 10: lead screws (shiny steel)
        Library.oViewer().SetGroupMaterial(10, "CCCCCC", 0.1f, 0.8f);
        // Group 11: drag chains (dark plastic)
        Library.oViewer().SetGroupMaterial(11, "333322", 0.5f, 0.1f);
        // Group 12: safety (bright yellow/red)
        Library.oViewer().SetGroupMaterial(12, "FF4444", 0.2f, 0.1f);
        // Group 13: toolpath (bright green)
        Library.oViewer().SetGroupMaterial(13, "00FF44", 0.2f, 0.5f);
    }

    // ========================================================================
    // HELP DISPLAY
    // ========================================================================

    /// <summary>
    /// Logs a formatted keybinding reference for interactive mode.
    /// </summary>
    public static void ShowHelp()
    {
        Log(@"
============================================================
  PicoCNC — INTERACTIVE MODE KEYBINDINGS
============================================================
--- Parameter Navigation ---
  Left/Right Arrow    Cycle through adjustable parameters
  Up/Down Arrow       Increase/decrease selected parameter
  U / J               Previous / Next parameter
  I / K               Decrease / Increase selected param by step
  Tab / Shift+Tab     Cycle through parameter categories

--- Material & Budget ---
  1 / 2 / 3 / 4       Material: Wood / Plastic / Aluminum / Steel
  5 / 6 / 7           Budget: Budget / Standard / Premium

--- Design ---
  V                   Cycle voxel size: 2.0 -> 1.0 -> 0.5 mm
  F1 / F2 / F3        Presets: Small (300x200x100) /
                       Medium (500x400x120) / Large (1000x750x150)

--- Actions ---
  R                   Force rebuild machine
  C                   Toggle collision verification on rebuild
  B                   Toggle beam analysis on rebuild
  E                   Export STLs (all components)
  S                   Save current config to JSON
  L                   Load config from JSON
  H                   Show this help

--- Visibility Toggle ---
  1-9                 Toggle component visibility
  0                   Toggle ALL components
  1=BaseFrame   2=WorkBed      3=YRails      4=GantryUprights
  5=GantryBridge 6=XRails      7=ZAssembly   8=SpindleMount
  9=MotorMounts

NOTE: For material/budget, press the number key WITHOUT Shift/Ctrl.
      For visibility toggles, visibility keys (D0-D9) toggle on/off.
      Material keys (D1-D4) take priority over visibility D1-D4;
      groups 1-4 visibility is toggled via viewer menu or arrow-key nav.

============================================================
");
    }

    // ========================================================================
    // PARAMETER GET / SET
    // ========================================================================

    /// <summary>
    /// Reads the current value of a mutable parameter by its mpParams key.
    /// </summary>
    static float GetParamValue(string key)
    {
        return key switch
        {
            "fWorkAreaX"        => fWorkAreaX,
            "fWorkAreaY"        => fWorkAreaY,
            "fWorkAreaZ"        => fWorkAreaZ,
            "fBaseOuterX"       => fBaseOuterX,
            "fBaseOuterY"       => fBaseOuterY,
            "fBaseOuterZ"       => fBaseOuterZ,
            "fBaseWallThick"    => fBaseWallThick,
            "fRibThick"         => fRibThick,
            "fGantryWallThick"  => fGantryWallThick,
            "fRibSpacing"       => fRibSpacing,
            "fRailWidth"        => fRailWidth,
            "fRailHeight"       => fRailHeight,
            "fRailInsetX"       => fRailInsetX,
            "fBoltHoleDia"      => fBoltHoleDia,
            "fBoltSpacingY"     => fBoltSpacingY,
            "fUprightX"         => fUprightX,
            "fUprightY"         => fUprightY,
            "fUprightZ"         => fUprightZ,
            "fGantryBridgeY"    => fGantryBridgeY,
            "fGantryBridgeZ"    => fGantryBridgeZ,
            "fZPlateX"          => fZPlateX,
            "fZPlateY"          => fZPlateY,
            "fZPlateZ"          => fZPlateZ,
            "fZRailSpace"       => fZRailSpace,
            "fZRailSize"        => fZRailSize,
            "fSpindleOD"        => fSpindleOD,
            "fClampOD"          => fClampOD,
            "fClampHeight"      => fClampHeight,
            "fClampSlit"        => fClampSlit,
            "fNema23Width"      => fNema23Width,
            "fNema23BoltCircle" => fNema23BoltCircle,
            "fNema23ShaftBore"  => fNema23ShaftBore,
            "fMountPlateThick"  => fMountPlateThick,
            "fLeadScrewDia"     => fLeadScrewDia,
            "fNutBlockSize"     => fNutBlockSize,
            "fTSlotUpperW"      => fTSlotUpperW,
            "fTSlotLowerW"      => fTSlotLowerW,
            "fTSlotDepth"       => fTSlotDepth,
            "fTSlotSpacing"     => fTSlotSpacing,
            "fTableThick"       => fTableThick,
            "fChainWidth"       => fChainWidth,
            "fChainHeight"      => fChainHeight,
            "fVoxelSizeMM"      => fVoxelSizeMM,
            "eCutMaterial"      => (float)eCutMaterial,
            "eBudgetTier"       => (float)eBudgetTier,
            _                   => 0f
        };
    }

    /// <summary>
    /// Writes a value to a mutable parameter by its mpParams key.
    /// The property setter automatically calls MarkDirty().
    /// </summary>
    static void SetParamValue(string key, float value)
    {
        switch (key)
        {
            case "fWorkAreaX":        fWorkAreaX        = value; break;
            case "fWorkAreaY":        fWorkAreaY        = value; break;
            case "fWorkAreaZ":        fWorkAreaZ        = value; break;
            case "fBaseOuterZ":       fBaseOuterZ       = value; break;
            case "fBaseWallThick":    fBaseWallThick    = value; break;
            case "fRibThick":         fRibThick         = value; break;
            case "fGantryWallThick":  fGantryWallThick  = value; break;
            case "fRibSpacing":       fRibSpacing       = value; break;
            case "fRailWidth":        fRailWidth        = value; break;
            case "fRailHeight":       fRailHeight       = value; break;
            case "fRailInsetX":       fRailInsetX       = value; break;
            case "fBoltHoleDia":      fBoltHoleDia      = value; break;
            case "fBoltSpacingY":     fBoltSpacingY     = value; break;
            case "fUprightX":         fUprightX         = value; break;
            case "fUprightY":         fUprightY         = value; break;
            case "fUprightZ":         fUprightZ         = value; break;
            case "fGantryBridgeY":    fGantryBridgeY    = value; break;
            case "fGantryBridgeZ":    fGantryBridgeZ    = value; break;
            case "fZPlateX":          fZPlateX          = value; break;
            case "fZPlateY":          fZPlateY          = value; break;
            case "fZPlateZ":          fZPlateZ          = value; break;
            case "fZRailSpace":       fZRailSpace       = value; break;
            case "fZRailSize":        fZRailSize        = value; break;
            case "fSpindleOD":        fSpindleOD        = value; break;
            case "fClampOD":          fClampOD          = value; break;
            case "fClampHeight":      fClampHeight      = value; break;
            case "fClampSlit":        fClampSlit        = value; break;
            case "fNema23Width":      fNema23Width      = value; break;
            case "fNema23BoltCircle": fNema23BoltCircle = value; break;
            case "fNema23ShaftBore":  fNema23ShaftBore  = value; break;
            case "fMountPlateThick":  fMountPlateThick  = value; break;
            case "fLeadScrewDia":     fLeadScrewDia     = value; break;
            case "fNutBlockSize":     fNutBlockSize     = value; break;
            case "fTSlotUpperW":      fTSlotUpperW      = value; break;
            case "fTSlotLowerW":      fTSlotLowerW      = value; break;
            case "fTSlotDepth":       fTSlotDepth       = value; break;
            case "fTSlotSpacing":     fTSlotSpacing     = value; break;
            case "fTableThick":       fTableThick       = value; break;
            case "fChainWidth":       fChainWidth       = value; break;
            case "fChainHeight":      fChainHeight      = value; break;
            case "fVoxelSizeMM":      fVoxelSizeMM      = value; break;
            case "eCutMaterial":
                eCutMaterial = (MaterialToCut)(int)MathF.Round(value);
                break;
            case "eBudgetTier":
                eBudgetTier = (BudgetTier)(int)MathF.Round(value);
                break;
        }
    }

    /// <summary>
    /// Returns the step size for a parameter, or 0 for non-editable.
    /// </summary>
    static float GetParamStep(string key)
    {
        if (mpParams.TryGetValue(key, out var meta))
            return meta.fStep;
        return 0f;
    }

    /// <summary>
    /// Clamps a parameter value to its defined min/max range.
    /// </summary>
    static float ClampParam(string key, float value)
    {
        if (mpParams.TryGetValue(key, out var meta) && meta.fMax > meta.fMin)
            return Math.Clamp(value, meta.fMin, meta.fMax);
        return value;
    }

    // ========================================================================
    // BUILD MACHINE
    // ========================================================================

    /// <summary>
    /// Rebuilds the full CNC machine, applying current parameter values,
    /// group visibility settings, and optional collision/beam analysis.
    /// </summary>
    public static void BuildMachine()
    {
        Stopwatch sw = Stopwatch.StartNew();

        // Clear previous geometry from the viewer
        Library.oViewer().RemoveAllObjects();

        // Build all components (adds to viewer as each is done)
        Voxels voxMachine = voxConstruct();

        // Apply group visibility -- hide groups the user toggled off
        for (int i = 1; i <= 13; i++)
            Library.oViewer().SetGroupVisible(i, s_aGroupVisible[i]);

        // Optional: collision verification
        if (s_bCollisionCheck)
        {
            Log("Running collision verification...");
            VerifyCollisions();
        }

        // Optional: beam structural analysis
        if (s_bBeamAnalysis)
        {
            Log("Running beam analysis...");
            RunBeamAnalysis();
        }

        sw.Stop();
        Log($"Build completed in {sw.Elapsed.TotalSeconds:F1}s");
    }

    // ========================================================================
    // MAIN INTERACTIVE LOOP
    // ========================================================================

    /// <summary>
    /// Runs the interactive CNC design loop. Sets up the viewer, registers
    /// the key handler via internal polling, does initial COTS selection
    /// and build, then polls for parameter changes and rebuilds live.
    ///
    /// Keyboard input uses Console.ReadKey as a fallback since the current
    /// PicoGK release (v2.0.0-beta11) does not yet expose IKeyHandler.
    /// When the viewer window is focused, key events reach the console
    /// on the task thread via Console.KeyAvailable.
    /// </summary>
    public static void RunInteractive()
    {
        // Initialize group visibility -- all visible by default
        for (int i = 0; i < s_aGroupVisible.Length; i++)
            s_aGroupVisible[i] = true;

        // Build the editable parameter key list from mpParams.
        // Only include entries with fStep > 0 (exclude derived read-only).
        var editableKeys = new System.Collections.Generic.List<string>();
        foreach (var kvp in mpParams)
        {
            if (kvp.Value.fStep > 0f)
                editableKeys.Add(kvp.Key);
        }
        s_aParamKeys = editableKeys.ToArray();

        // Create the key handler instance
        var keyHandler = new CNCKeyHandler();

        // Configure viewer groups
        SetupViewer();
        ShowHelp();

        // Log current configuration
        Log("PicoCNC -- CNC Machine Generator (Interactive Mode)");
        Log($"Voxel size: {fVoxelSizeMM} mm");
        Log($"Work area: {fWorkAreaX} x {fWorkAreaY} x {fWorkAreaZ} mm");
        Log($"Material: {eCutMaterial}, Budget: {eBudgetTier}");

        // COTS parts selection based on current parameters
        var req = new CNCRequirements
        {
            fWorkAreaX = fWorkAreaX,
            fWorkAreaY = fWorkAreaY,
            fWorkAreaZ = fWorkAreaZ,
            eMaterial  = eCutMaterial,
            eBudget    = eBudgetTier
        };
        CNCSelectedParts parts = SelectParts(req);
        PrintPartsList(parts);

        // Mark dirty so the first iteration of the loop triggers a build.
        MarkDirty();

        // ---- Interactive poll loop ----
        while (Library.bContinueTask())
        {
            // Process console keyboard input
            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo ki = Console.ReadKey(intercept: true);
                EKeys eKey = MapConsoleKey(ki.Key);

                if (eKey == EKeys.None)
                    continue;

                bool bShift = (ki.Modifiers & ConsoleModifiers.Shift) != 0;
                bool bCtrl  = (ki.Modifiers & ConsoleModifiers.Control) != 0;
                bool bAlt   = (ki.Modifiers & ConsoleModifiers.Alt) != 0;

                try
                {
                    keyHandler.bHandleEvent(
                        Library.oViewer(), eKey,
                        bPressed: true, bShift, bCtrl, bAlt, bCmd: false);
                }
                catch (Exception ex)
                {
                    Log($"Key error ({eKey}): {ex.Message}");
                }
            }

            // Rebuild if parameters changed
            if (bDirty)
            {
                Log("Rebuilding machine...");
                BuildMachine();
                ClearDirty();
                Log("Ready. Press H for help.");
            }

            Thread.Sleep(150);
        }
    }

    // ========================================================================
    // PRESET HELPERS
    // ========================================================================

    static void LoadSmallPreset()
    {
        fWorkAreaX = 300f;
        fWorkAreaY = 200f;
        fWorkAreaZ = 100f;
        Log("Preset: Small (300x200x100 mm)");
    }

    static void LoadMediumPreset()
    {
        fWorkAreaX = 500f;
        fWorkAreaY = 400f;
        fWorkAreaZ = 120f;
        Log("Preset: Medium (500x400x120 mm)");
    }

    static void LoadLargePreset()
    {
        fWorkAreaX = 1000f;
        fWorkAreaY = 750f;
        fWorkAreaZ = 150f;
        Log("Preset: Large (1000x750x150 mm)");
    }

    // ========================================================================
    // VOXEL SIZE CYCLING
    // ========================================================================

    static void CycleVoxelSize()
    {
        // 2.0 -> 1.0 -> 0.5 -> 2.0 mm cycle
        if (MathF.Abs(fVoxelSizeMM - 2.0f) < 0.01f)
            fVoxelSizeMM = 1.0f;
        else if (MathF.Abs(fVoxelSizeMM - 1.0f) < 0.01f)
            fVoxelSizeMM = 0.5f;
        else
            fVoxelSizeMM = 2.0f;
        Log($"Voxel size: {fVoxelSizeMM} mm");
    }

    // ========================================================================
    // PARAMETER CATEGORY NAVIGATION
    // ========================================================================

    /// <summary>
    /// Jump to the first parameter in the next/previous parameter category.
    /// </summary>
    static void CycleCategory(bool bForward)
    {
        if (s_aParamKeys.Length == 0) return;

        string strCurKey = s_aParamKeys[s_nSelectedParamIdx];
        string strCurCat = mpParams.TryGetValue(strCurKey, out var curMeta)
                           ? curMeta.strCategory : "";

        // Collect unique sorted categories from editable params
        var aCats = new System.Collections.Generic.List<string>();
        foreach (string key in s_aParamKeys)
        {
            if (mpParams.TryGetValue(key, out var m))
            {
                if (!aCats.Contains(m.strCategory))
                    aCats.Add(m.strCategory);
            }
        }
        if (aCats.Count == 0) return;

        int nCurCatIdx = aCats.IndexOf(strCurCat);
        if (nCurCatIdx < 0) nCurCatIdx = 0;

        int nTargetIdx = bForward
            ? (nCurCatIdx + 1) % aCats.Count
            : (nCurCatIdx - 1 + aCats.Count) % aCats.Count;

        string strTargetCat = aCats[nTargetIdx];

        // Find first editable param in target category
        for (int i = 0; i < s_aParamKeys.Length; i++)
        {
            if (mpParams.TryGetValue(s_aParamKeys[i], out var m)
                && m.strCategory == strTargetCat)
            {
                s_nSelectedParamIdx = i;
                LogCurrentParam();
                return;
            }
        }
    }

    // ========================================================================
    // PARAMETER LOG HELPER
    // ========================================================================

    static void LogCurrentParam()
    {
        if (s_aParamKeys.Length == 0) return;

        string strKey = s_aParamKeys[s_nSelectedParamIdx];
        float fVal  = GetParamValue(strKey);
        float fStep = GetParamStep(strKey);

        if (mpParams.TryGetValue(strKey, out var meta))
        {
            Log($"  [{s_nSelectedParamIdx + 1}/{s_aParamKeys.Length}] " +
                $"{meta.strLabel} = {fVal:F2} {meta.strUnit}  (step: {fStep:F2}, " +
                $"range: {meta.fMin:F1}-{meta.fMax:F1}, cat: {meta.strCategory})");
        }
        else
        {
            Log($"  [{s_nSelectedParamIdx + 1}/{s_aParamKeys.Length}] " +
                $"{strKey} = {fVal:F2}");
        }
    }

    // ========================================================================
    // KEY HANDLER (NESTED CLASS)
    // ========================================================================

    /// <summary>
    /// Implements IKeyHandler to provide keyboard-driven interactive control
    /// over CNC machine parameters, visibility, and actions.
    ///
    /// All handlers only fire on key DOWN (bPressed=true), not on release or
    /// repeat (tracked via s_heldKeys).
    ///
    /// NOTE: Keys D1-D4 set material; D5-D7 set budget.  These take priority
    /// over the visibility-toggle D0-D9 binding.  Groups 1-7 can still be
    /// toggled via the viewer menu or by remapping in a future version.
    /// </summary>
    public class CNCKeyHandler : IKeyHandler
    {
        public bool bHandleEvent(Viewer oViewer, EKeys eKey,
                                 bool bPressed, bool bShift, bool bCtrl,
                                 bool bAlt, bool bCmd)
        {
            // --- Key up: remove from held set, allow next press ---
            if (!bPressed)
            {
                s_heldKeys.Remove(eKey);
                return false;
            }

            // --- Key repeat filter ---
            if (s_heldKeys.Contains(eKey))
                return false;
            s_heldKeys.Add(eKey);

            bool bHandled = false;

            try
            {
                // ============================================================
                // PARAMETER NAVIGATION
                // ============================================================

                // --- Arrow Right: next parameter ---
                if (eKey == EKeys.Right && !bShift && !bCtrl && !bAlt)
                {
                    if (s_aParamKeys.Length > 0)
                    {
                        s_nSelectedParamIdx =
                            (s_nSelectedParamIdx + 1) % s_aParamKeys.Length;
                        LogCurrentParam();
                    }
                    bHandled = true;
                }
                // --- Arrow Left: previous parameter ---
                else if (eKey == EKeys.Left && !bShift && !bCtrl && !bAlt)
                {
                    if (s_aParamKeys.Length > 0)
                    {
                        s_nSelectedParamIdx =
                            (s_nSelectedParamIdx - 1 + s_aParamKeys.Length)
                            % s_aParamKeys.Length;
                        LogCurrentParam();
                    }
                    bHandled = true;
                }
                // --- Arrow Up: increase parameter ---
                else if (eKey == EKeys.Up && !bShift && !bCtrl && !bAlt)
                {
                    AdjustParam(+1);
                    bHandled = true;
                }
                // --- Arrow Down: decrease parameter ---
                else if (eKey == EKeys.Down && !bShift && !bCtrl && !bAlt)
                {
                    AdjustParam(-1);
                    bHandled = true;
                }

                // --- Tab: next category / Shift+Tab: previous category ---
                else if (eKey == EKeys.Tab && !bCtrl && !bAlt)
                {
                    CycleCategory(!bShift);
                    bHandled = true;
                }

                // --- U: previous parameter ---
                else if (eKey == EKeys.U && !bShift && !bCtrl && !bAlt)
                {
                    if (s_aParamKeys.Length > 0)
                    {
                        s_nSelectedParamIdx =
                            (s_nSelectedParamIdx - 1 + s_aParamKeys.Length)
                            % s_aParamKeys.Length;
                        LogCurrentParam();
                    }
                    bHandled = true;
                }
                // --- J: next parameter ---
                else if (eKey == EKeys.J && !bShift && !bCtrl && !bAlt)
                {
                    if (s_aParamKeys.Length > 0)
                    {
                        s_nSelectedParamIdx =
                            (s_nSelectedParamIdx + 1) % s_aParamKeys.Length;
                        LogCurrentParam();
                    }
                    bHandled = true;
                }
                // --- I: decrease parameter ---
                else if (eKey == EKeys.I && !bShift && !bCtrl && !bAlt)
                {
                    AdjustParam(-1);
                    bHandled = true;
                }
                // --- K: increase parameter ---
                else if (eKey == EKeys.K && !bShift && !bCtrl && !bAlt)
                {
                    AdjustParam(+1);
                    bHandled = true;
                }

                // ============================================================
                // MATERIAL (1-4: Wood / Plastic / Aluminum / Steel)
                // ============================================================
                else if (eKey == EKeys.D1 && !bShift && !bCtrl && !bAlt)
                {
                    eCutMaterial = MaterialToCut.Wood;
                    Log("Material: Wood");
                    bHandled = true;
                }
                else if (eKey == EKeys.D2 && !bShift && !bCtrl && !bAlt)
                {
                    eCutMaterial = MaterialToCut.Plastic;
                    Log("Material: Plastic (Composites)");
                    bHandled = true;
                }
                else if (eKey == EKeys.D3 && !bShift && !bCtrl && !bAlt)
                {
                    eCutMaterial = MaterialToCut.Aluminum;
                    Log("Material: Aluminum");
                    bHandled = true;
                }
                else if (eKey == EKeys.D4 && !bShift && !bCtrl && !bAlt)
                {
                    eCutMaterial = MaterialToCut.Steel;
                    Log("Material: Steel");
                    bHandled = true;
                }

                // ============================================================
                // BUDGET (5-7: Budget / Standard / Premium)
                // ============================================================
                else if (eKey == EKeys.D5 && !bShift && !bCtrl && !bAlt)
                {
                    eBudgetTier = BudgetTier.Budget;
                    Log("Budget: Budget");
                    bHandled = true;
                }
                else if (eKey == EKeys.D6 && !bShift && !bCtrl && !bAlt)
                {
                    eBudgetTier = BudgetTier.Standard;
                    Log("Budget: Standard");
                    bHandled = true;
                }
                else if (eKey == EKeys.D7 && !bShift && !bCtrl && !bAlt)
                {
                    eBudgetTier = BudgetTier.Premium;
                    Log("Budget: Premium");
                    bHandled = true;
                }

                // ============================================================
                // VOXEL SIZE (V)
                // ============================================================
                else if (eKey == EKeys.V && !bShift && !bCtrl && !bAlt)
                {
                    CycleVoxelSize();
                    bHandled = true;
                }

                // ============================================================
                // PRESETS (F1 / F2 / F3)
                // ============================================================
                else if (eKey == EKeys.F1 && !bShift && !bCtrl && !bAlt)
                {
                    LoadSmallPreset();
                    bHandled = true;
                }
                else if (eKey == EKeys.F2 && !bShift && !bCtrl && !bAlt)
                {
                    LoadMediumPreset();
                    bHandled = true;
                }
                else if (eKey == EKeys.F3 && !bShift && !bCtrl && !bAlt)
                {
                    LoadLargePreset();
                    bHandled = true;
                }

                // ============================================================
                // ACTIONS
                // ============================================================
                // --- R: force rebuild ---
                else if (eKey == EKeys.R && !bShift && !bCtrl && !bAlt)
                {
                    MarkDirty();
                    Log("Forcing rebuild...");
                    bHandled = true;
                }
                // --- C: toggle collision verification ---
                else if (eKey == EKeys.C && !bShift && !bCtrl && !bAlt)
                {
                    s_bCollisionCheck = !s_bCollisionCheck;
                    Log($"Collision verification: " +
                        $"{(s_bCollisionCheck ? "ON" : "OFF")}");
                    bHandled = true;
                }
                // --- B: toggle beam analysis ---
                else if (eKey == EKeys.B && !bShift && !bCtrl && !bAlt)
                {
                    s_bBeamAnalysis = !s_bBeamAnalysis;
                    Log($"Beam analysis: " +
                        $"{(s_bBeamAnalysis ? "ON" : "OFF")}");
                    bHandled = true;
                }
                // --- E: export STLs ---
                else if (eKey == EKeys.E && !bShift && !bCtrl && !bAlt)
                {
                    Log("Exporting STLs...");
                    ExportStl(voxConstruct(), "Assembly");
                    ExportAllComponents();
                    Log("STL export complete.");
                    bHandled = true;
                }
                // --- S: save config ---
                else if (eKey == EKeys.S && !bShift && !bCtrl && !bAlt)
                {
                    SaveConfig();
                    Log("Config saved.");
                    bHandled = true;
                }
                // --- L: load config ---
                else if (eKey == EKeys.L && !bShift && !bCtrl && !bAlt)
                {
                    LoadConfig();
                    Log("Config loaded.");
                    MarkDirty();
                    bHandled = true;
                }
                // --- H: show help ---
                else if (eKey == EKeys.H && !bShift && !bCtrl && !bAlt)
                {
                    ShowHelp();
                    bHandled = true;
                }

                // ============================================================
                // VISIBILITY TOGGLES
                // 0 = toggle ALL, 8/9 = SpindleMount/MotorMounts
                // (D1-D7 already consumed by material/budget above)
                // ============================================================
                else if (eKey == EKeys.D0 && !bShift && !bCtrl && !bAlt)
                {
                    // Toggle ALL groups
                    bool bAnyVisible = false;
                    for (int i = 1; i <= 13; i++)
                    {
                        if (s_aGroupVisible[i]) { bAnyVisible = true; break; }
                    }
                    bool bNewState = !bAnyVisible;
                    for (int i = 1; i <= 13; i++)
                        s_aGroupVisible[i] = bNewState;
                    Log($"All groups: {(bNewState ? "VISIBLE" : "HIDDEN")}");
                    MarkDirty();
                    bHandled = true;
                }
                else if (eKey == EKeys.D8 && !bShift && !bCtrl && !bAlt)
                {
                    s_aGroupVisible[8] = !s_aGroupVisible[8];
                    Log($"SpindleMount: " +
                        $"{(s_aGroupVisible[8] ? "VISIBLE" : "HIDDEN")}");
                    Library.oViewer().SetGroupVisible(8, s_aGroupVisible[8]);
                    bHandled = true;
                }
                else if (eKey == EKeys.D9 && !bShift && !bCtrl && !bAlt)
                {
                    s_aGroupVisible[9] = !s_aGroupVisible[9];
                    Log($"MotorMounts: " +
                        $"{(s_aGroupVisible[9] ? "VISIBLE" : "HIDDEN")}");
                    Library.oViewer().SetGroupVisible(9, s_aGroupVisible[9]);
                    bHandled = true;
                }
            }
            catch (Exception ex)
            {
                Log($"Key handler error ({eKey}): {ex.Message}");
            }

            return bHandled;
        }

        // --- Parameter adjustment ---
        static void AdjustParam(int nDirection)
        {
            if (s_aParamKeys.Length == 0) return;

            string strKey = s_aParamKeys[s_nSelectedParamIdx];
            float fCur  = GetParamValue(strKey);
            float fStep = GetParamStep(strKey);
            float fNew  = ClampParam(strKey, fCur + nDirection * fStep);
            SetParamValue(strKey, fNew);

            string strLabel = mpParams.TryGetValue(strKey, out var m)
                              ? m.strLabel : strKey;
            Log($"  {strLabel}: {fCur:F2} -> {fNew:F2}");
        }
    }
}

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PicoGK;

public static partial class Picocnc
{
    // ====================================================================
    // MACHINE PRESET DEFINITIONS
    // ====================================================================

    /// <summary>
    /// A named preset configuration that bundles commonly used CNC sizes
    /// with recommended material and budget tiers.
    /// </summary>
    public struct MachinePreset
    {
        public string strName        { get; init; }
        public string strDescription { get; init; }
        public float  fWorkAreaX     { get; init; }
        public float  fWorkAreaY     { get; init; }
        public float  fWorkAreaZ     { get; init; }
        public float  fVoxelSizeMM   { get; init; }
        public MaterialToCut eMaterial { get; init; }
        public BudgetTier    eBudget   { get; init; }

        public MachinePreset(string name, string desc, float x, float y, float z,
                             float voxel, MaterialToCut mat, BudgetTier budget)
        {
            strName = name; strDescription = desc;
            fWorkAreaX = x; fWorkAreaY = y; fWorkAreaZ = z;
            fVoxelSizeMM = voxel; eMaterial = mat; eBudget = budget;
        }
    }

    /// <summary>
    /// Built-in machine presets covering the most common DIY CNC sizes,
    /// from small desktop routers to full-sheet production machines.
    /// </summary>
    public static readonly MachinePreset[] aPresets =
    {
        new("Mini",        "~A4 desktop engraver/router — lowest cost entry point",
            300f,  200f,   80f, 2.0f, MaterialToCut.Wood,     BudgetTier.Budget),
        new("Desktop",     "Typical 500x400 hobby CNC router — most popular DIY size",
            500f,  400f,  120f, 2.0f, MaterialToCut.Aluminum, BudgetTier.Standard),
        new("Workbench",   "Mid-size machine for larger projects on a dedicated bench",
            750f,  600f,  150f, 2.0f, MaterialToCut.Aluminum, BudgetTier.Standard),
        new("Full Sheet",  "Full 4x4 ft sheet capability — semi-production machine",
            1250f, 1250f, 200f, 2.0f, MaterialToCut.Aluminum, BudgetTier.Premium),
        new("Steel Mill",  "Rigid machine for mild steel milling — low-speed high-torque",
            600f,  400f,  150f, 1.0f, MaterialToCut.Steel,    BudgetTier.Premium),
    };

    // ====================================================================
    // APPLY PRESET
    // ====================================================================

    /// <summary>
    /// Applies a MachinePreset by copying all its values into the mutable
    /// static parameters. Each setter calls MarkDirty() automatically.
    /// </summary>
    public static void ApplyPreset(MachinePreset preset)
    {
        fWorkAreaX   = preset.fWorkAreaX;
        fWorkAreaY   = preset.fWorkAreaY;
        fWorkAreaZ   = preset.fWorkAreaZ;
        fVoxelSizeMM = preset.fVoxelSizeMM;
        eCutMaterial = preset.eMaterial;
        eBudgetTier  = preset.eBudget;

        Log($"Preset applied: {preset.strName} — {preset.strDescription}");
        Log($"  Envelope: {preset.fWorkAreaX}x{preset.fWorkAreaY}x{preset.fWorkAreaZ} mm");
        Log($"  Material: {preset.eMaterial}, Budget: {preset.eBudget}");
    }

    // ====================================================================
    // PRINT PRESETS
    // ====================================================================

    /// <summary>
    /// Logs a formatted table of all available machine presets.
    /// </summary>
    public static void PrintPresets()
    {
        Log("\n=== AVAILABLE PRESETS ===");
        for (int i = 0; i < aPresets.Length; i++)
        {
            var p = aPresets[i];
            Log($"  F{i+1}: {p.strName,-15} {p.fWorkAreaX}x{p.fWorkAreaY}x{p.fWorkAreaZ} mm  " +
                        $"{p.eMaterial,-10} {p.eBudget}");
        }
        Log("");
    }

    // ====================================================================
    // CNC CONFIG — JSON-serializable snapshot of all mutable parameters
    // ====================================================================

    /// <summary>
    /// Lightweight DTO that captures the complete mutable parameter state
    /// so a user can save and reload their custom machine configuration.
    /// Enums are stored as strings for human-readable JSON.
    /// </summary>
    public sealed class CNCConfig
    {
        // --- Envelope ---
        public float fWorkAreaX { get; set; }
        public float fWorkAreaY { get; set; }
        public float fWorkAreaZ { get; set; }
        public float fBaseOuterZ { get; set; }

        // --- Wall thicknesses ---
        public float fBaseWallThick   { get; set; }
        public float fRibThick        { get; set; }
        public float fGantryWallThick { get; set; }

        // --- Rib spacing ---
        public float fRibSpacing { get; set; }

        // --- Rail dimensions ---
        public float fRailWidth    { get; set; }
        public float fRailHeight   { get; set; }
        public float fRailInsetX   { get; set; }
        public float fBoltHoleDia  { get; set; }
        public float fBoltSpacingY { get; set; }

        // --- Upright dimensions ---
        public float fUprightX { get; set; }
        public float fUprightY { get; set; }
        public float fUprightZ { get; set; }

        // --- Gantry bridge ---
        public float fGantryBridgeY { get; set; }
        public float fGantryBridgeZ { get; set; }

        // --- Z-axis ---
        public float fZPlateX    { get; set; }
        public float fZPlateY    { get; set; }
        public float fZPlateZ    { get; set; }
        public float fZRailSpace { get; set; }
        public float fZRailSize  { get; set; }

        // --- Spindle ---
        public float fSpindleOD   { get; set; }
        public float fClampOD     { get; set; }
        public float fClampHeight { get; set; }
        public float fClampSlit   { get; set; }

        // --- Motor mounts ---
        public float fNema23Width      { get; set; }
        public float fNema23BoltCircle { get; set; }
        public float fNema23ShaftBore  { get; set; }
        public float fMountPlateThick  { get; set; }

        // --- Lead screws ---
        public float fLeadScrewDia { get; set; }
        public float fNutBlockSize { get; set; }

        // --- T-slot ---
        public float fTSlotUpperW  { get; set; }
        public float fTSlotLowerW  { get; set; }
        public float fTSlotDepth   { get; set; }
        public float fTSlotSpacing { get; set; }

        // --- Work bed ---
        public float fTableThick { get; set; }

        // --- Drag chain ---
        public float fChainWidth  { get; set; }
        public float fChainHeight { get; set; }

        // --- Material & Budget (stored as strings for readability) ---
        public string strMaterial { get; set; }   // "Wood", "Plastic", "Aluminum", "Steel"
        public string strBudget   { get; set; }   // "Budget", "Standard", "Premium"

        // --- Voxel ---
        public float fVoxelSizeMM { get; set; }

        // --- Metadata ---
        public string strConfigName { get; set; }
        public string strSavedAt    { get; set; }   // ISO 8601 timestamp
    }

    // ====================================================================
    // SAVE / LOAD
    // ====================================================================

    /// <summary>
    /// Saves all current mutable parameters to a JSON file.
    /// Creates the target directory if it does not exist.
    /// </summary>
    public static void SaveConfig(string strFilePath)
    {
        var cfg = new CNCConfig
        {
            // Envelope
            fWorkAreaX  = fWorkAreaX,
            fWorkAreaY  = fWorkAreaY,
            fWorkAreaZ  = fWorkAreaZ,
            fBaseOuterZ = fBaseOuterZ,

            // Wall thicknesses
            fBaseWallThick   = fBaseWallThick,
            fRibThick        = fRibThick,
            fGantryWallThick = fGantryWallThick,

            // Rib spacing
            fRibSpacing = fRibSpacing,

            // Rail dimensions
            fRailWidth    = fRailWidth,
            fRailHeight   = fRailHeight,
            fRailInsetX   = fRailInsetX,
            fBoltHoleDia  = fBoltHoleDia,
            fBoltSpacingY = fBoltSpacingY,

            // Upright dimensions
            fUprightX = fUprightX,
            fUprightY = fUprightY,
            fUprightZ = fUprightZ,

            // Gantry bridge
            fGantryBridgeY = fGantryBridgeY,
            fGantryBridgeZ = fGantryBridgeZ,

            // Z-axis
            fZPlateX    = fZPlateX,
            fZPlateY    = fZPlateY,
            fZPlateZ    = fZPlateZ,
            fZRailSpace = fZRailSpace,
            fZRailSize  = fZRailSize,

            // Spindle
            fSpindleOD   = fSpindleOD,
            fClampOD     = fClampOD,
            fClampHeight = fClampHeight,
            fClampSlit   = fClampSlit,

            // Motor mounts
            fNema23Width      = fNema23Width,
            fNema23BoltCircle = fNema23BoltCircle,
            fNema23ShaftBore  = fNema23ShaftBore,
            fMountPlateThick  = fMountPlateThick,

            // Lead screws
            fLeadScrewDia = fLeadScrewDia,
            fNutBlockSize = fNutBlockSize,

            // T-slot
            fTSlotUpperW  = fTSlotUpperW,
            fTSlotLowerW  = fTSlotLowerW,
            fTSlotDepth   = fTSlotDepth,
            fTSlotSpacing = fTSlotSpacing,

            // Work bed
            fTableThick = fTableThick,

            // Drag chain
            fChainWidth  = fChainWidth,
            fChainHeight = fChainHeight,

            // Material & Budget
            strMaterial = eCutMaterial.ToString(),
            strBudget   = eBudgetTier.ToString(),

            // Voxel
            fVoxelSizeMM = fVoxelSizeMM,

            // Metadata
            strConfigName = "PicoCNC Config",
            strSavedAt    = DateTime.UtcNow.ToString("o")
        };

        string strDir = Path.GetDirectoryName(strFilePath);
        if (!string.IsNullOrEmpty(strDir) && !Directory.Exists(strDir))
            Directory.CreateDirectory(strDir);

        var opts = new JsonSerializerOptions { WriteIndented = true };
        string strJson = JsonSerializer.Serialize(cfg, opts);
        File.WriteAllText(strFilePath, strJson);
        Log($"Config saved to: {strFilePath}");
    }

    /// <summary>
    /// Loads a JSON config file and applies all parameters to the
    /// mutable static properties. Logs a message if the file is missing.
    /// </summary>
    public static void LoadConfig(string strFilePath)
    {
        if (!File.Exists(strFilePath))
        {
            Log($"Config file not found: {strFilePath}");
            return;
        }

        string strJson = File.ReadAllText(strFilePath);
        var cfg = JsonSerializer.Deserialize<CNCConfig>(strJson);

        if (cfg is null)
        {
            Log($"Failed to deserialize config from: {strFilePath}");
            return;
        }

        // Envelope
        fWorkAreaX  = cfg.fWorkAreaX;
        fWorkAreaY  = cfg.fWorkAreaY;
        fWorkAreaZ  = cfg.fWorkAreaZ;
        fBaseOuterZ = cfg.fBaseOuterZ;

        // Wall thicknesses
        fBaseWallThick   = cfg.fBaseWallThick;
        fRibThick        = cfg.fRibThick;
        fGantryWallThick = cfg.fGantryWallThick;

        // Rib spacing
        fRibSpacing = cfg.fRibSpacing;

        // Rail dimensions
        fRailWidth    = cfg.fRailWidth;
        fRailHeight   = cfg.fRailHeight;
        fRailInsetX   = cfg.fRailInsetX;
        fBoltHoleDia  = cfg.fBoltHoleDia;
        fBoltSpacingY = cfg.fBoltSpacingY;

        // Upright dimensions
        fUprightX = cfg.fUprightX;
        fUprightY = cfg.fUprightY;
        fUprightZ = cfg.fUprightZ;

        // Gantry bridge
        fGantryBridgeY = cfg.fGantryBridgeY;
        fGantryBridgeZ = cfg.fGantryBridgeZ;

        // Z-axis
        fZPlateX    = cfg.fZPlateX;
        fZPlateY    = cfg.fZPlateY;
        fZPlateZ    = cfg.fZPlateZ;
        fZRailSpace = cfg.fZRailSpace;
        fZRailSize  = cfg.fZRailSize;

        // Spindle
        fSpindleOD   = cfg.fSpindleOD;
        fClampOD     = cfg.fClampOD;
        fClampHeight = cfg.fClampHeight;
        fClampSlit   = cfg.fClampSlit;

        // Motor mounts
        fNema23Width      = cfg.fNema23Width;
        fNema23BoltCircle = cfg.fNema23BoltCircle;
        fNema23ShaftBore  = cfg.fNema23ShaftBore;
        fMountPlateThick  = cfg.fMountPlateThick;

        // Lead screws
        fLeadScrewDia = cfg.fLeadScrewDia;
        fNutBlockSize = cfg.fNutBlockSize;

        // T-slot
        fTSlotUpperW  = cfg.fTSlotUpperW;
        fTSlotLowerW  = cfg.fTSlotLowerW;
        fTSlotDepth   = cfg.fTSlotDepth;
        fTSlotSpacing = cfg.fTSlotSpacing;

        // Work bed
        fTableThick = cfg.fTableThick;

        // Drag chain
        fChainWidth  = cfg.fChainWidth;
        fChainHeight = cfg.fChainHeight;

        // Material & Budget
        eCutMaterial = Enum.Parse<MaterialToCut>(cfg.strMaterial);
        eBudgetTier  = Enum.Parse<BudgetTier>(cfg.strBudget);

        // Voxel
        fVoxelSizeMM = cfg.fVoxelSizeMM;

        Log($"Config loaded from: {strFilePath}");
    }

    /// <summary>
    /// Saves config to the default location next to the executable.
    /// </summary>
    public static void SaveConfig()
    {
        string strPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "picocnc_config.json");
        SaveConfig(strPath);
    }

    /// <summary>
    /// Loads config from the default location next to the executable.
    /// </summary>
    public static void LoadConfig()
    {
        string strPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "picocnc_config.json");
        LoadConfig(strPath);
    }
}

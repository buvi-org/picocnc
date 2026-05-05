namespace PicoGK;

public static partial class Picocnc
{
    // --- Machine envelope ---
    public const float fWorkAreaX = 500f;   // table width (mm)
    public const float fWorkAreaY = 400f;   // table depth (mm)
    public const float fWorkAreaZ = 120f;   // max Z clearance (mm)

    // --- Derived envelope dimensions ---
    public const float fBaseOuterX = fWorkAreaX + 100f;
    public const float fBaseOuterY = fWorkAreaY + 100f;
    public const float fBaseOuterZ = 150f;

    // --- Wall thicknesses ---
    public const float fBaseWallThick   = 15f;
    public const float fRibThick        = 10f;
    public const float fGantryWallThick = 8f;

    // --- Rib spacing ---
    public const float fRibSpacing = 120f;

    // --- Rail dimensions ---
    public const float fRailWidth      = 20f;
    public const float fRailHeight     = 25f;
    public const float fRailInsetX     = 30f;  // rail distance from base outer edge
    public const float fBoltHoleDia    = 5.2f; // M5 clearance
    public const float fBoltSpacingY   = 80f;

    // --- Upright dimensions ---
    public const float fUprightX = 40f;
    public const float fUprightY = 60f;
    public const float fUprightZ = 200f;

    // --- Gantry bridge ---
    public const float fGantryBridgeY = 60f;
    public const float fGantryBridgeZ = 80f;

    // --- Z-axis ---
    public const float fZPlateX     = 80f;
    public const float fZPlateY     = 15f;
    public const float fZPlateZ     = 250f;
    public const float fZRailSpace  = 50f;
    public const float fZRailSize   = 15f;

    // --- Spindle ---
    public const float fSpindleOD    = 65f;
    public const float fClampOD      = 80f;
    public const float fClampHeight  = 60f;
    public const float fClampSlit    = 3f;

    // --- Motor mounts ---
    public const float fNema23Width      = 57f;
    public const float fNema23BoltCircle = 47.14f;
    public const float fNema23ShaftBore  = 12f;
    public const float fMountPlateThick  = 8f;

    // --- Lead screws ---
    public const float fLeadScrewDia = 12f;
    public const float fNutBlockSize = 25f;

    // --- T-slot ---
    public const float fTSlotUpperW = 20f;
    public const float fTSlotLowerW = 10f;
    public const float fTSlotDepth  = 10f;
    public const float fTSlotSpacing = 100f;

    // --- Work bed ---
    public const float fTableThick = 20f;

    // --- Drag chain ---
    public const float fChainWidth  = 30f;
    public const float fChainHeight = 20f;

    // --- Voxel resolution ---
    // 2.0mm = fast preview (~30s); 0.5mm = production quality (~10-30min)
    public const float fVoxelSizeMM = 2.0f;
}

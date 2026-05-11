namespace PicoGK;

public static partial class Picocnc
{
    // ========================================================================
    // DIRTY TRACKING — lets the interactive task loop detect changes
    // and rebuild the machine on the fly without recompilation.
    // ========================================================================
    static bool s_bDirty = true;  // start dirty so first build happens
    public static bool bDirty => s_bDirty;

    static void MarkDirty() { s_bDirty = true; }
    public static void ClearDirty() { s_bDirty = false; }

    // ========================================================================
    // MACHINE ENVELOPE
    // ========================================================================

    // --- Work area ---
    static float s_fWorkAreaX = 500f;
    public static float fWorkAreaX
    {
        get => s_fWorkAreaX;
        set { if (MathF.Abs(s_fWorkAreaX - value) > 0.01f) { s_fWorkAreaX = value; MarkDirty(); } }
    }

    static float s_fWorkAreaY = 400f;
    public static float fWorkAreaY
    {
        get => s_fWorkAreaY;
        set { if (MathF.Abs(s_fWorkAreaY - value) > 0.01f) { s_fWorkAreaY = value; MarkDirty(); } }
    }

    static float s_fWorkAreaZ = 120f;
    public static float fWorkAreaZ
    {
        get => s_fWorkAreaZ;
        set { if (MathF.Abs(s_fWorkAreaZ - value) > 0.01f) { s_fWorkAreaZ = value; MarkDirty(); } }
    }

    // --- Derived envelope dimensions ---
    public static float fBaseOuterX => fWorkAreaX + 100f;
    public static float fBaseOuterY => fWorkAreaY + 100f;

    static float s_fBaseOuterZ = 150f;
    public static float fBaseOuterZ
    {
        get => s_fBaseOuterZ;
        set { if (MathF.Abs(s_fBaseOuterZ - value) > 0.01f) { s_fBaseOuterZ = value; MarkDirty(); } }
    }

    // ========================================================================
    // WALL THICKNESSES
    // ========================================================================

    static float s_fBaseWallThick = 15f;
    public static float fBaseWallThick
    {
        get => s_fBaseWallThick;
        set { if (MathF.Abs(s_fBaseWallThick - value) > 0.01f) { s_fBaseWallThick = value; MarkDirty(); } }
    }

    static float s_fRibThick = 10f;
    public static float fRibThick
    {
        get => s_fRibThick;
        set { if (MathF.Abs(s_fRibThick - value) > 0.01f) { s_fRibThick = value; MarkDirty(); } }
    }

    static float s_fGantryWallThick = 8f;
    public static float fGantryWallThick
    {
        get => s_fGantryWallThick;
        set { if (MathF.Abs(s_fGantryWallThick - value) > 0.01f) { s_fGantryWallThick = value; MarkDirty(); } }
    }

    // --- Rib spacing ---
    static float s_fRibSpacing = 120f;
    public static float fRibSpacing
    {
        get => s_fRibSpacing;
        set { if (MathF.Abs(s_fRibSpacing - value) > 0.01f) { s_fRibSpacing = value; MarkDirty(); } }
    }

    // ========================================================================
    // RAIL DIMENSIONS
    // ========================================================================

    static float s_fRailWidth = 20f;
    public static float fRailWidth
    {
        get => s_fRailWidth;
        set { if (MathF.Abs(s_fRailWidth - value) > 0.01f) { s_fRailWidth = value; MarkDirty(); } }
    }

    static float s_fRailHeight = 25f;
    public static float fRailHeight
    {
        get => s_fRailHeight;
        set { if (MathF.Abs(s_fRailHeight - value) > 0.01f) { s_fRailHeight = value; MarkDirty(); } }
    }

    static float s_fRailInsetX = 30f;  // rail distance from base outer edge
    public static float fRailInsetX
    {
        get => s_fRailInsetX;
        set { if (MathF.Abs(s_fRailInsetX - value) > 0.01f) { s_fRailInsetX = value; MarkDirty(); } }
    }

    static float s_fBoltHoleDia = 5.2f; // M5 clearance
    public static float fBoltHoleDia
    {
        get => s_fBoltHoleDia;
        set { if (MathF.Abs(s_fBoltHoleDia - value) > 0.01f) { s_fBoltHoleDia = value; MarkDirty(); } }
    }

    static float s_fBoltSpacingY = 80f;
    public static float fBoltSpacingY
    {
        get => s_fBoltSpacingY;
        set { if (MathF.Abs(s_fBoltSpacingY - value) > 0.01f) { s_fBoltSpacingY = value; MarkDirty(); } }
    }

    // ========================================================================
    // UPRIGHT DIMENSIONS
    // ========================================================================

    static float s_fUprightX = 40f;
    public static float fUprightX
    {
        get => s_fUprightX;
        set { if (MathF.Abs(s_fUprightX - value) > 0.01f) { s_fUprightX = value; MarkDirty(); } }
    }

    static float s_fUprightY = 60f;
    public static float fUprightY
    {
        get => s_fUprightY;
        set { if (MathF.Abs(s_fUprightY - value) > 0.01f) { s_fUprightY = value; MarkDirty(); } }
    }

    static float s_fUprightZ = 200f;
    public static float fUprightZ
    {
        get => s_fUprightZ;
        set { if (MathF.Abs(s_fUprightZ - value) > 0.01f) { s_fUprightZ = value; MarkDirty(); } }
    }

    // ========================================================================
    // GANTRY BRIDGE
    // ========================================================================

    static float s_fGantryBridgeY = 60f;
    public static float fGantryBridgeY
    {
        get => s_fGantryBridgeY;
        set { if (MathF.Abs(s_fGantryBridgeY - value) > 0.01f) { s_fGantryBridgeY = value; MarkDirty(); } }
    }

    static float s_fGantryBridgeZ = 80f;
    public static float fGantryBridgeZ
    {
        get => s_fGantryBridgeZ;
        set { if (MathF.Abs(s_fGantryBridgeZ - value) > 0.01f) { s_fGantryBridgeZ = value; MarkDirty(); } }
    }

    // ========================================================================
    // Z-AXIS
    // ========================================================================

    static float s_fZPlateX = 80f;
    public static float fZPlateX
    {
        get => s_fZPlateX;
        set { if (MathF.Abs(s_fZPlateX - value) > 0.01f) { s_fZPlateX = value; MarkDirty(); } }
    }

    static float s_fZPlateY = 15f;
    public static float fZPlateY
    {
        get => s_fZPlateY;
        set { if (MathF.Abs(s_fZPlateY - value) > 0.01f) { s_fZPlateY = value; MarkDirty(); } }
    }

    static float s_fZPlateZ = 250f;
    public static float fZPlateZ
    {
        get => s_fZPlateZ;
        set { if (MathF.Abs(s_fZPlateZ - value) > 0.01f) { s_fZPlateZ = value; MarkDirty(); } }
    }

    static float s_fZRailSpace = 50f;
    public static float fZRailSpace
    {
        get => s_fZRailSpace;
        set { if (MathF.Abs(s_fZRailSpace - value) > 0.01f) { s_fZRailSpace = value; MarkDirty(); } }
    }

    static float s_fZRailSize = 15f;
    public static float fZRailSize
    {
        get => s_fZRailSize;
        set { if (MathF.Abs(s_fZRailSize - value) > 0.01f) { s_fZRailSize = value; MarkDirty(); } }
    }

    // ========================================================================
    // SPINDLE
    // ========================================================================

    static float s_fSpindleOD = 65f;
    public static float fSpindleOD
    {
        get => s_fSpindleOD;
        set { if (MathF.Abs(s_fSpindleOD - value) > 0.01f) { s_fSpindleOD = value; MarkDirty(); } }
    }

    static float s_fClampOD = 80f;
    public static float fClampOD
    {
        get => s_fClampOD;
        set { if (MathF.Abs(s_fClampOD - value) > 0.01f) { s_fClampOD = value; MarkDirty(); } }
    }

    static float s_fClampHeight = 60f;
    public static float fClampHeight
    {
        get => s_fClampHeight;
        set { if (MathF.Abs(s_fClampHeight - value) > 0.01f) { s_fClampHeight = value; MarkDirty(); } }
    }

    static float s_fClampSlit = 3f;
    public static float fClampSlit
    {
        get => s_fClampSlit;
        set { if (MathF.Abs(s_fClampSlit - value) > 0.01f) { s_fClampSlit = value; MarkDirty(); } }
    }

    // ========================================================================
    // MOTOR MOUNTS
    // ========================================================================

    static float s_fNema23Width = 57f;
    public static float fNema23Width
    {
        get => s_fNema23Width;
        set { if (MathF.Abs(s_fNema23Width - value) > 0.01f) { s_fNema23Width = value; MarkDirty(); } }
    }

    static float s_fNema23BoltCircle = 47.14f;
    public static float fNema23BoltCircle
    {
        get => s_fNema23BoltCircle;
        set { if (MathF.Abs(s_fNema23BoltCircle - value) > 0.01f) { s_fNema23BoltCircle = value; MarkDirty(); } }
    }

    static float s_fNema23ShaftBore = 12f;
    public static float fNema23ShaftBore
    {
        get => s_fNema23ShaftBore;
        set { if (MathF.Abs(s_fNema23ShaftBore - value) > 0.01f) { s_fNema23ShaftBore = value; MarkDirty(); } }
    }

    static float s_fMountPlateThick = 8f;
    public static float fMountPlateThick
    {
        get => s_fMountPlateThick;
        set { if (MathF.Abs(s_fMountPlateThick - value) > 0.01f) { s_fMountPlateThick = value; MarkDirty(); } }
    }

    // ========================================================================
    // LEAD SCREWS
    // ========================================================================

    static float s_fLeadScrewDia = 12f;
    public static float fLeadScrewDia
    {
        get => s_fLeadScrewDia;
        set { if (MathF.Abs(s_fLeadScrewDia - value) > 0.01f) { s_fLeadScrewDia = value; MarkDirty(); } }
    }

    static float s_fNutBlockSize = 25f;
    public static float fNutBlockSize
    {
        get => s_fNutBlockSize;
        set { if (MathF.Abs(s_fNutBlockSize - value) > 0.01f) { s_fNutBlockSize = value; MarkDirty(); } }
    }

    // ========================================================================
    // T-SLOT
    // ========================================================================

    static float s_fTSlotUpperW = 20f;
    public static float fTSlotUpperW
    {
        get => s_fTSlotUpperW;
        set { if (MathF.Abs(s_fTSlotUpperW - value) > 0.01f) { s_fTSlotUpperW = value; MarkDirty(); } }
    }

    static float s_fTSlotLowerW = 10f;
    public static float fTSlotLowerW
    {
        get => s_fTSlotLowerW;
        set { if (MathF.Abs(s_fTSlotLowerW - value) > 0.01f) { s_fTSlotLowerW = value; MarkDirty(); } }
    }

    static float s_fTSlotDepth = 10f;
    public static float fTSlotDepth
    {
        get => s_fTSlotDepth;
        set { if (MathF.Abs(s_fTSlotDepth - value) > 0.01f) { s_fTSlotDepth = value; MarkDirty(); } }
    }

    static float s_fTSlotSpacing = 100f;
    public static float fTSlotSpacing
    {
        get => s_fTSlotSpacing;
        set { if (MathF.Abs(s_fTSlotSpacing - value) > 0.01f) { s_fTSlotSpacing = value; MarkDirty(); } }
    }

    // ========================================================================
    // WORK BED
    // ========================================================================

    static float s_fTableThick = 20f;
    public static float fTableThick
    {
        get => s_fTableThick;
        set { if (MathF.Abs(s_fTableThick - value) > 0.01f) { s_fTableThick = value; MarkDirty(); } }
    }

    // ========================================================================
    // DRAG CHAIN
    // ========================================================================

    static float s_fChainWidth = 30f;
    public static float fChainWidth
    {
        get => s_fChainWidth;
        set { if (MathF.Abs(s_fChainWidth - value) > 0.01f) { s_fChainWidth = value; MarkDirty(); } }
    }

    static float s_fChainHeight = 20f;
    public static float fChainHeight
    {
        get => s_fChainHeight;
        set { if (MathF.Abs(s_fChainHeight - value) > 0.01f) { s_fChainHeight = value; MarkDirty(); } }
    }

    // ========================================================================
    // VOXEL RESOLUTION
    // 2.0mm = fast preview (~30s); 0.5mm = production quality (~10-30min)
    // ========================================================================

    static float s_fVoxelSizeMM = 2.0f;
    public static float fVoxelSizeMM
    {
        get => s_fVoxelSizeMM;
        set { if (MathF.Abs(s_fVoxelSizeMM - value) > 0.001f) { s_fVoxelSizeMM = value; MarkDirty(); } }
    }

    // ========================================================================
    // MATERIAL & BUDGET
    // Determines spindle power, guideway rigidity, drive type, and
    // component grade selection.
    // ========================================================================

    // --- Material to cut ---
    // Determines spindle power and guideway rigidity selection
    static MaterialToCut s_eCutMaterial = MaterialToCut.Aluminum;
    public static MaterialToCut eCutMaterial
    {
        get => s_eCutMaterial;
        set { if (s_eCutMaterial != value) { s_eCutMaterial = value; MarkDirty(); } }
    }

    // --- Budget tier ---
    // Determines drive type (lead screw vs ball screw) and component grade
    static BudgetTier s_eBudgetTier = BudgetTier.Standard;
    public static BudgetTier eBudgetTier
    {
        get => s_eBudgetTier;
        set { if (s_eBudgetTier != value) { s_eBudgetTier = value; MarkDirty(); } }
    }

    // ========================================================================
    // PARAMETER METADATA — drives the interactive UI
    // ========================================================================

    public sealed class ParamMeta
    {
        public string strLabel;
        public string strUnit;
        public string strCategory;
        public float fMin, fMax, fStep, fDefault;
    }

    public static readonly Dictionary<string, ParamMeta> mpParams = new()
    {
        // --- Envelope ---
        ["fWorkAreaX"]   = new() { strLabel="Work Area X",       strUnit="mm", strCategory="Envelope",
                                   fMin=200, fMax=2000, fStep=50, fDefault=500 },
        ["fWorkAreaY"]   = new() { strLabel="Work Area Y",       strUnit="mm", strCategory="Envelope",
                                   fMin=200, fMax=2000, fStep=50, fDefault=400 },
        ["fWorkAreaZ"]   = new() { strLabel="Work Area Z",       strUnit="mm", strCategory="Envelope",
                                   fMin=50,  fMax=500,  fStep=10, fDefault=120 },
        ["fBaseOuterX"]  = new() { strLabel="Base Outer X",      strUnit="mm", strCategory="Envelope",
                                   fMin=0,   fMax=0,    fStep=0,  fDefault=600 },   // derived
        ["fBaseOuterY"]  = new() { strLabel="Base Outer Y",      strUnit="mm", strCategory="Envelope",
                                   fMin=0,   fMax=0,    fStep=0,  fDefault=500 },   // derived
        ["fBaseOuterZ"]  = new() { strLabel="Base Outer Z",      strUnit="mm", strCategory="Envelope",
                                   fMin=50,  fMax=300,  fStep=10, fDefault=150 },

        // --- Wall Thickness ---
        ["fBaseWallThick"]   = new() { strLabel="Base Wall Thickness",   strUnit="mm", strCategory="Wall Thickness",
                                       fMin=5,  fMax=40, fStep=1, fDefault=15 },
        ["fRibThick"]        = new() { strLabel="Rib Thickness",         strUnit="mm", strCategory="Wall Thickness",
                                       fMin=3,  fMax=30, fStep=1, fDefault=10 },
        ["fGantryWallThick"] = new() { strLabel="Gantry Wall Thickness", strUnit="mm", strCategory="Wall Thickness",
                                       fMin=3,  fMax=20, fStep=1, fDefault=8 },
        ["fRibSpacing"]      = new() { strLabel="Rib Spacing",           strUnit="mm", strCategory="Wall Thickness",
                                       fMin=40, fMax=300, fStep=10, fDefault=120 },

        // --- Rails ---
        ["fRailWidth"]    = new() { strLabel="Rail Width",      strUnit="mm", strCategory="Rails",
                                    fMin=10, fMax=45, fStep=1,  fDefault=20 },
        ["fRailHeight"]   = new() { strLabel="Rail Height",     strUnit="mm", strCategory="Rails",
                                    fMin=10, fMax=45, fStep=1,  fDefault=25 },
        ["fRailInsetX"]   = new() { strLabel="Rail Inset X",    strUnit="mm", strCategory="Rails",
                                    fMin=10, fMax=100, fStep=5, fDefault=30 },
        ["fBoltHoleDia"]  = new() { strLabel="Bolt Hole Dia",   strUnit="mm", strCategory="Rails",
                                    fMin=3,  fMax=12, fStep=0.1f, fDefault=5.2f },
        ["fBoltSpacingY"] = new() { strLabel="Bolt Spacing Y",  strUnit="mm", strCategory="Rails",
                                    fMin=40, fMax=200, fStep=10, fDefault=80 },

        // --- Uprights ---
        ["fUprightX"] = new() { strLabel="Upright X", strUnit="mm", strCategory="Uprights",
                                fMin=20, fMax=80,  fStep=5,  fDefault=40 },
        ["fUprightY"] = new() { strLabel="Upright Y", strUnit="mm", strCategory="Uprights",
                                fMin=30, fMax=120, fStep=5,  fDefault=60 },
        ["fUprightZ"] = new() { strLabel="Upright Z", strUnit="mm", strCategory="Uprights",
                                fMin=100, fMax=400, fStep=10, fDefault=200 },

        // --- Gantry Bridge ---
        ["fGantryBridgeY"] = new() { strLabel="Gantry Bridge Y", strUnit="mm", strCategory="Gantry Bridge",
                                     fMin=30, fMax=120, fStep=5, fDefault=60 },
        ["fGantryBridgeZ"] = new() { strLabel="Gantry Bridge Z", strUnit="mm", strCategory="Gantry Bridge",
                                     fMin=40, fMax=160, fStep=5, fDefault=80 },

        // --- Z-Axis ---
        ["fZPlateX"]    = new() { strLabel="Z Plate X",    strUnit="mm", strCategory="Z-Axis",
                                  fMin=40, fMax=160, fStep=5,  fDefault=80 },
        ["fZPlateY"]    = new() { strLabel="Z Plate Y",    strUnit="mm", strCategory="Z-Axis",
                                  fMin=5,  fMax=30,  fStep=1,  fDefault=15 },
        ["fZPlateZ"]    = new() { strLabel="Z Plate Z",    strUnit="mm", strCategory="Z-Axis",
                                  fMin=100, fMax=400, fStep=10, fDefault=250 },
        ["fZRailSpace"] = new() { strLabel="Z Rail Space", strUnit="mm", strCategory="Z-Axis",
                                  fMin=20, fMax=100, fStep=5, fDefault=50 },
        ["fZRailSize"]  = new() { strLabel="Z Rail Size",  strUnit="mm", strCategory="Z-Axis",
                                  fMin=8,  fMax=30,  fStep=1, fDefault=15 },

        // --- Spindle ---
        ["fSpindleOD"]   = new() { strLabel="Spindle OD",   strUnit="mm", strCategory="Spindle",
                                   fMin=40, fMax=120, fStep=1,   fDefault=65 },
        ["fClampOD"]     = new() { strLabel="Clamp OD",     strUnit="mm", strCategory="Spindle",
                                   fMin=50, fMax=140, fStep=1,   fDefault=80 },
        ["fClampHeight"] = new() { strLabel="Clamp Height", strUnit="mm", strCategory="Spindle",
                                   fMin=30, fMax=120, fStep=5,   fDefault=60 },
        ["fClampSlit"]   = new() { strLabel="Clamp Slit",   strUnit="mm", strCategory="Spindle",
                                   fMin=1,  fMax=8,   fStep=0.5f, fDefault=3 },

        // --- Motor Mounts ---
        ["fNema23Width"]      = new() { strLabel="NEMA23 Width",      strUnit="mm", strCategory="Motor Mounts",
                                        fMin=40, fMax=86,  fStep=1,    fDefault=57 },
        ["fNema23BoltCircle"] = new() { strLabel="NEMA23 Bolt Circle", strUnit="mm", strCategory="Motor Mounts",
                                        fMin=30, fMax=70,  fStep=0.1f, fDefault=47.14f },
        ["fNema23ShaftBore"]  = new() { strLabel="NEMA23 Shaft Bore",  strUnit="mm", strCategory="Motor Mounts",
                                        fMin=5,  fMax=20,  fStep=1,    fDefault=12 },
        ["fMountPlateThick"]  = new() { strLabel="Mount Plate Thick",  strUnit="mm", strCategory="Motor Mounts",
                                        fMin=3,  fMax=20,  fStep=1,    fDefault=8 },

        // --- Lead Screws ---
        ["fLeadScrewDia"] = new() { strLabel="Lead Screw Dia", strUnit="mm", strCategory="Lead Screws",
                                    fMin=8,  fMax=25, fStep=1, fDefault=12 },
        ["fNutBlockSize"] = new() { strLabel="Nut Block Size", strUnit="mm", strCategory="Lead Screws",
                                    fMin=10, fMax=50, fStep=1, fDefault=25 },

        // --- T-Slots ---
        ["fTSlotUpperW"]  = new() { strLabel="T-Slot Upper W",  strUnit="mm", strCategory="T-Slots",
                                    fMin=8,  fMax=40, fStep=1,  fDefault=20 },
        ["fTSlotLowerW"]  = new() { strLabel="T-Slot Lower W",  strUnit="mm", strCategory="T-Slots",
                                    fMin=4,  fMax=20, fStep=1,  fDefault=10 },
        ["fTSlotDepth"]   = new() { strLabel="T-Slot Depth",    strUnit="mm", strCategory="T-Slots",
                                    fMin=4,  fMax=20, fStep=1,  fDefault=10 },
        ["fTSlotSpacing"] = new() { strLabel="T-Slot Spacing",  strUnit="mm", strCategory="T-Slots",
                                    fMin=40, fMax=200, fStep=10, fDefault=100 },

        // --- Work Bed ---
        ["fTableThick"] = new() { strLabel="Table Thickness", strUnit="mm", strCategory="Work Bed",
                                  fMin=5, fMax=50, fStep=1, fDefault=20 },

        // --- Drag Chains ---
        ["fChainWidth"]  = new() { strLabel="Chain Width",  strUnit="mm", strCategory="Drag Chains",
                                   fMin=10, fMax=60, fStep=5, fDefault=30 },
        ["fChainHeight"] = new() { strLabel="Chain Height", strUnit="mm", strCategory="Drag Chains",
                                   fMin=10, fMax=40, fStep=5, fDefault=20 },

        // --- Resolution ---
        ["fVoxelSizeMM"] = new() { strLabel="Voxel Size",     strUnit="mm", strCategory="Resolution",
                                   fMin=0.2f, fMax=5.0f, fStep=0.1f, fDefault=2.0f },

        // --- Material & Budget ---
        ["eCutMaterial"] = new() { strLabel="Cut Material", strUnit="", strCategory="Material & Budget",
                                   fMin=0, fMax=3, fStep=1, fDefault=(float)MaterialToCut.Aluminum },
        ["eBudgetTier"]  = new() { strLabel="Budget Tier",  strUnit="", strCategory="Material & Budget",
                                   fMin=0, fMax=2, fStep=1, fDefault=(float)BudgetTier.Standard },
    };

    // ========================================================================
    // RESET TO DEFAULTS
    // ========================================================================

    public static void ResetToDefaults()
    {
        s_fWorkAreaX        = 500f;
        s_fWorkAreaY        = 400f;
        s_fWorkAreaZ        = 120f;
        s_fBaseOuterZ       = 150f;
        s_fBaseWallThick    = 15f;
        s_fRibThick         = 10f;
        s_fGantryWallThick  = 8f;
        s_fRibSpacing       = 120f;
        s_fRailWidth        = 20f;
        s_fRailHeight       = 25f;
        s_fRailInsetX       = 30f;
        s_fBoltHoleDia      = 5.2f;
        s_fBoltSpacingY     = 80f;
        s_fUprightX         = 40f;
        s_fUprightY         = 60f;
        s_fUprightZ         = 200f;
        s_fGantryBridgeY    = 60f;
        s_fGantryBridgeZ    = 80f;
        s_fZPlateX          = 80f;
        s_fZPlateY          = 15f;
        s_fZPlateZ          = 250f;
        s_fZRailSpace       = 50f;
        s_fZRailSize        = 15f;
        s_fSpindleOD        = 65f;
        s_fClampOD          = 80f;
        s_fClampHeight      = 60f;
        s_fClampSlit        = 3f;
        s_fNema23Width      = 57f;
        s_fNema23BoltCircle = 47.14f;
        s_fNema23ShaftBore  = 12f;
        s_fMountPlateThick  = 8f;
        s_fLeadScrewDia     = 12f;
        s_fNutBlockSize     = 25f;
        s_fTSlotUpperW      = 20f;
        s_fTSlotLowerW      = 10f;
        s_fTSlotDepth       = 10f;
        s_fTSlotSpacing     = 100f;
        s_fTableThick       = 20f;
        s_fChainWidth       = 30f;
        s_fChainHeight      = 20f;
        s_fVoxelSizeMM      = 2.0f;
        s_eCutMaterial      = MaterialToCut.Aluminum;
        s_eBudgetTier       = BudgetTier.Standard;

        MarkDirty();
    }
}

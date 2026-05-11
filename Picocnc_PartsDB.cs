using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    // ========================================================================
    // ENUMS
    // ========================================================================

    public enum PartCategory
    {
        LinearGuide,
        BallScrew,
        LeadScrew,
        StepperMotor,
        SpindleMotor,
        Coupling,
        DragChain,
        LimitSwitch,
        Fastener,
        AluminumExtrusion,
        TNut,
        BearingBlock
    }

    public enum BudgetTier { Budget, Standard, Premium }

    public enum MaterialToCut { Wood, Plastic, Aluminum, Steel }

    // ========================================================================
    // STRUCTURES
    // ========================================================================

    /// <summary>
    /// Represents a single Commercial Off-The-Shelf (COTS) part with
    /// geometry data, performance specs, and sourcing information.
    /// </summary>
    public struct COTSPart
    {
        public string   strManufacturer;    // Hiwin, TBI, NEMA, etc.
        public string   strPartNumber;      // HGR20, SFU1605, etc.
        public string   strDescription;     // "20mm linear guideway, 1500mm"
        public PartCategory eCategory;
        public BudgetTier    eBudget;

        // Critical interface dimensions (mm)
        public float    fLength;            // overall length of rail/screw/extrusion
        public float    fWidth;             // body width
        public float    fHeight;            // body height / rail height
        public float    fRailWidth;         // for linear guides: rail width
        public float    fBlockLength;       // for linear guides: bearing block length
        public float    fBlockWidth;        // bearing block width
        public float    fBlockHeight;       // bearing block height
        public float    fBoltDiameter;      // mounting bolt size (mm)
        public float    fBoltSpacing;       // spacing between bolts along rail (mm)
        public float[]  afBlockBoltX;       // bolt pattern on bearing block (X offsets)
        public float[]  afBlockBoltY;       // bolt pattern on bearing block (Y offsets)

        // Performance specs
        public float    fDynamicLoad;       // N — dynamic load rating
        public float    fStaticLoad;        // N — static load rating
        public float    fMaxSpeed;          // m/s (linear) or RPM (rotary)
        public float    fAccuracy;          // mm/300mm (linear) or arc-min (rotary)
        public float    fWeight;            // kg/m (linear parts) or kg (motors)

        // Motor-specific
        public float    fHoldingTorque;     // Nm — stepper holding torque
        public float    fRatedPower;        // kW — spindle/motor power
        public float    fRatedSpeed;        // RPM — rated speed
        public float    fShaftDiameter;     // mm — output shaft diameter
        public float    fMotorBodyLength;   // mm — motor body length
        public float    fNemaSize;          // 17, 23, 24, 34

        // Spindle-specific
        public float    fSpindleOD;         // mm — spindle body diameter
        public float    fColletType;        // 11=ER11, 16=ER16, 20=ER20, 25=ER25, 32=ER32
        public float    fMaxToolDiameter;   // mm — max tool shank diameter

        // Availability
        public bool     bCommonlyAvailable;  // easily sourced worldwide
        public string   strDatasheetURL;     // link to datasheet
    }

    /// <summary>
    /// User-specified requirements for the CNC machine.
    /// </summary>
    public struct CNCRequirements
    {
        public float         fWorkAreaX;     // mm
        public float         fWorkAreaY;
        public float         fWorkAreaZ;
        public MaterialToCut eMaterial;
        public BudgetTier    eBudget;
    }

    /// <summary>
    /// Complete set of COTS parts selected for a CNC machine build.
    /// </summary>
    public struct CNCSelectedParts
    {
        public COTSPart oYGuideway;      // Y-axis linear guideways (pair)
        public COTSPart oXGuideway;      // X-axis linear guideways (pair)
        public COTSPart oZGuideway;      // Z-axis linear guideways
        public COTSPart oYDrive;         // Y-axis drive (ball screw or lead screw)
        public COTSPart oXDrive;         // X-axis drive
        public COTSPart oZDrive;         // Z-axis drive
        public COTSPart oYStepper;       // Y-axis stepper motor
        public COTSPart oXStepper;       // X-axis stepper (smaller — moves with gantry)
        public COTSPart oZStepper;       // Z-axis stepper
        public COTSPart oSpindle;        // Spindle motor
        public COTSPart oYCoupling;      // Y motor coupling
        public COTSPart oXCoupling;      // X motor coupling
        public COTSPart oZCoupling;      // Z motor coupling
        public COTSPart oYDragChain;     // Y cable carrier
        public COTSPart oXDragChain;     // X cable carrier
        public COTSPart oLimitSwitch;    // Limit switch type
        public COTSPart oFastenerMain;   // Main structural fasteners (M5/M6/M8)
    }

    // ========================================================================
    // PART DATABASES — static readonly arrays of real-world COTS parts
    // ========================================================================

    // ------------------------------------------------------------------------
    // LINEAR GUIDEWAYS — HGR profile rails + HGH bearing blocks
    //
    // These are the standard square-profile linear guideways used in virtually
    // every DIY and professional CNC router. Hiwin HGR is the industry
    // reference; THK, IKO, and NSK are premium equivalents. Cheap clones
    // (VXB, no-name) exist at the budget tier.
    //
    // Bolt spacing: M5 at 60mm (HGR15/20), M6 at 60mm (HGR25)
    // Rails are countersunk for SHCS from below.
    // ------------------------------------------------------------------------

    static readonly COTSPart oGuideway_HGR15 = new()
    {
        strManufacturer     = "Hiwin",
        strPartNumber       = "HGR15",
        strDescription      = "15mm square-profile linear guideway, HGH15CA bearing block",
        eCategory           = PartCategory.LinearGuide,
        eBudget             = BudgetTier.Budget,

        fLength             = 1500f,        // max common stock length
        fWidth              = 15f,          // rail width
        fHeight             = 15f,          // rail height
        fRailWidth          = 15f,
        fBlockLength        = 59f,          // HGH15CA block length (along rail)
        fBlockWidth         = 34f,          // HGH15CA block width (across rail)
        fBlockHeight        = 24f,          // HGH15CA block height (Z)
        fBoltDiameter       = 5f,           // M5 rail mounting bolts
        fBoltSpacing        = 60f,          // bolt hole spacing along rail
        afBlockBoltX        = new[] { -13f, -13f,  13f,  13f },  // 26×26mm M4 block bolt pattern
        afBlockBoltY        = new[] { -13f,  13f, -13f,  13f },

        fDynamicLoad        = 11400f,       // 11.4 kN
        fStaticLoad         = 17000f,       // ~17 kN
        fMaxSpeed           = 5f,           // 5 m/s
        fAccuracy           = 0.02f,        // 0.02 mm/300mm (H class)
        fWeight             = 1.43f,        // kg/m for rail (block adds ~0.3 kg)

        bCommonlyAvailable  = true,
        strDatasheetURL     = "https://www.hiwin.com/linear-guideways"
    };

    static readonly COTSPart oGuideway_HGR20 = new()
    {
        strManufacturer     = "Hiwin",
        strPartNumber       = "HGR20",
        strDescription      = "20mm square-profile linear guideway, HGH20CA bearing block",
        eCategory           = PartCategory.LinearGuide,
        eBudget             = BudgetTier.Standard,

        fLength             = 2000f,
        fWidth              = 20f,          // rail width
        fHeight             = 17.5f,        // rail height
        fRailWidth          = 20f,
        fBlockLength        = 73f,          // HGH20CA block length (along rail)
        fBlockWidth         = 44f,          // HGH20CA block width (across rail)
        fBlockHeight        = 30f,          // HGH20CA block height (Z)
        fBoltDiameter       = 5f,           // M5 rail mounting bolts
        fBoltSpacing        = 60f,
        afBlockBoltX        = new[] { -16f, -16f,  16f,  16f },  // 32×36mm M5 block bolt pattern
        afBlockBoltY        = new[] { -18f,  18f, -18f,  18f },

        fDynamicLoad        = 17800f,       // 17.8 kN
        fStaticLoad         = 27000f,       // ~27 kN
        fMaxSpeed           = 5f,
        fAccuracy           = 0.02f,
        fWeight             = 2.30f,        // kg/m for rail (block adds ~0.5 kg)

        bCommonlyAvailable  = true,
        strDatasheetURL     = "https://www.hiwin.com/linear-guideways"
    };

    static readonly COTSPart oGuideway_HGR25 = new()
    {
        strManufacturer     = "Hiwin",
        strPartNumber       = "HGR25",
        strDescription      = "25mm square-profile linear guideway, HGH25CA bearing block",
        eCategory           = PartCategory.LinearGuide,
        eBudget             = BudgetTier.Premium,

        fLength             = 2000f,
        fWidth              = 23f,          // rail width
        fHeight             = 22f,          // rail height
        fRailWidth          = 23f,
        fBlockLength        = 82f,          // HGH25CA block length (along rail)
        fBlockWidth         = 48f,          // HGH25CA block width (across rail)
        fBlockHeight        = 36f,          // HGH25CA block height (Z)
        fBoltDiameter       = 6f,           // M6 rail mounting bolts
        fBoltSpacing        = 60f,
        afBlockBoltX        = new[] { -17.5f, -17.5f,  17.5f,  17.5f },  // 35×40mm M6 block bolt pattern
        afBlockBoltY        = new[] { -20f,   20f,   -20f,   20f },

        fDynamicLoad        = 27300f,       // 27.3 kN
        fStaticLoad         = 42000f,       // ~42 kN
        fMaxSpeed           = 5f,
        fAccuracy           = 0.015f,       // 0.015 mm/300mm (precision ground)
        fWeight             = 3.40f,        // kg/m for rail (block adds ~0.8 kg)

        bCommonlyAvailable  = true,
        strDatasheetURL     = "https://www.hiwin.com/linear-guideways"
    };

    static readonly COTSPart[] aLinearGuideways = {
        oGuideway_HGR15, oGuideway_HGR20, oGuideway_HGR25
    };

    // ------------------------------------------------------------------------
    // BALL SCREWS — SFU series, C7 rolled accuracy
    //
    // TBI Motion SFU series is the de facto standard for DIY CNC. C7 grade
    // provides ±0.05mm/300mm accuracy. End machining: fixed end ~10mm shaft
    // for SFU12, ~12mm for SFU16/20/25. BK/BF bearing blocks are standard.
    // ------------------------------------------------------------------------

    static readonly COTSPart oBallScrew_SFU1204 = new()
    {
        strManufacturer     = "TBI Motion",
        strPartNumber       = "SFU1204",
        strDescription      = "12mm OD ball screw, 4mm lead, C7 rolled, with nut",
        eCategory           = PartCategory.BallScrew,
        eBudget             = BudgetTier.Budget,

        fLength             = 1000f,        // representative length
        fWidth              = 12f,          // screw OD
        fHeight             = 4f,           // lead
        fRailWidth          = 0f,
        fBlockLength        = 40f,          // nut length
        fBlockWidth         = 34f,          // nut width
        fBlockHeight        = 34f,          // nut height
        fBoltDiameter       = 4f,           // M4 nut mounting bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -13f, -13f, 13f, 13f },   // nut mounting bolt pattern
        afBlockBoltY        = new[] { -13f,  13f, -13f, 13f },

        fDynamicLoad        = 5700f,        // 5.7 kN
        fStaticLoad         = 8100f,        // 8.1 kN
        fMaxSpeed           = 1000f,        // ~1000 RPM usable
        fAccuracy           = 0.05f,        // C7 grade: ±0.05 mm/300mm
        fWeight             = 0.80f,        // ~0.8 kg/m

        fShaftDiameter      = 10f,          // end-machined shaft diameter
        bCommonlyAvailable  = true,
        strDatasheetURL     = "https://www.tbimotion.com"
    };

    static readonly COTSPart oBallScrew_SFU1605 = new()
    {
        strManufacturer     = "TBI Motion",
        strPartNumber       = "SFU1605",
        strDescription      = "16mm OD ball screw, 5mm lead, C7 rolled, with nut",
        eCategory           = PartCategory.BallScrew,
        eBudget             = BudgetTier.Standard,

        fLength             = 1200f,
        fWidth              = 16f,          // screw OD
        fHeight             = 5f,           // lead
        fRailWidth          = 0f,
        fBlockLength        = 46f,          // nut length
        fBlockWidth         = 40f,          // nut width
        fBlockHeight        = 40f,          // nut height
        fBoltDiameter       = 5f,           // M5 nut mounting bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -15f, -15f, 15f, 15f },   // nut mounting bolt pattern
        afBlockBoltY        = new[] { -15f,  15f, -15f, 15f },

        fDynamicLoad        = 9700f,        // 9.7 kN
        fStaticLoad         = 14300f,       // 14.3 kN
        fMaxSpeed           = 1000f,
        fAccuracy           = 0.05f,
        fWeight             = 1.50f,        // ~1.5 kg/m

        fShaftDiameter      = 12f,          // end-machined shaft diameter
        bCommonlyAvailable  = true,
        strDatasheetURL     = "https://www.tbimotion.com"
    };

    static readonly COTSPart oBallScrew_SFU2005 = new()
    {
        strManufacturer     = "TBI Motion",
        strPartNumber       = "SFU2005",
        strDescription      = "20mm OD ball screw, 5mm lead, C7 rolled, with nut",
        eCategory           = PartCategory.BallScrew,
        eBudget             = BudgetTier.Premium,

        fLength             = 1500f,
        fWidth              = 20f,          // screw OD
        fHeight             = 5f,           // lead
        fRailWidth          = 0f,
        fBlockLength        = 54f,          // nut length
        fBlockWidth         = 48f,          // nut width
        fBlockHeight        = 48f,          // nut height
        fBoltDiameter       = 6f,           // M6 nut mounting bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -18f, -18f, 18f, 18f },   // nut mounting bolt pattern
        afBlockBoltY        = new[] { -18f,  18f, -18f, 18f },

        fDynamicLoad        = 14500f,       // 14.5 kN
        fStaticLoad         = 22600f,       // 22.6 kN
        fMaxSpeed           = 1000f,
        fAccuracy           = 0.05f,
        fWeight             = 2.40f,        // ~2.4 kg/m

        fShaftDiameter      = 12f,          // end-machined shaft diameter
        bCommonlyAvailable  = true,
        strDatasheetURL     = "https://www.tbimotion.com"
    };

    static readonly COTSPart oBallScrew_SFU2505 = new()
    {
        strManufacturer     = "TBI Motion",
        strPartNumber       = "SFU2505",
        strDescription      = "25mm OD ball screw, 5mm lead, C7 rolled, with nut",
        eCategory           = PartCategory.BallScrew,
        eBudget             = BudgetTier.Premium,

        fLength             = 2000f,
        fWidth              = 25f,          // screw OD
        fHeight             = 5f,           // lead
        fRailWidth          = 0f,
        fBlockLength        = 62f,          // nut length
        fBlockWidth         = 53f,          // nut width
        fBlockHeight        = 53f,          // nut height
        fBoltDiameter       = 6f,           // M6 nut mounting bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -20f, -20f, 20f, 20f },   // nut mounting bolt pattern
        afBlockBoltY        = new[] { -20f,  20f, -20f, 20f },

        fDynamicLoad        = 18300f,       // 18.3 kN
        fStaticLoad         = 30800f,       // 30.8 kN
        fMaxSpeed           = 1000f,
        fAccuracy           = 0.05f,
        fWeight             = 3.80f,        // ~3.8 kg/m

        fShaftDiameter      = 12f,          // end-machined shaft diameter
        bCommonlyAvailable  = true,
        strDatasheetURL     = "https://www.tbimotion.com"
    };

    static readonly COTSPart[] aBallScrews = {
        oBallScrew_SFU1204, oBallScrew_SFU1605, oBallScrew_SFU2005, oBallScrew_SFU2505
    };

    // ------------------------------------------------------------------------
    // LEAD SCREWS — ACME/trapezoidal thread, budget alternative to ball screws
    //
    // T8 uses 8mm OD Delrin anti-backlash nuts. T12 uses 12mm OD brass nuts.
    // Lead per revolution: T8x2 = 2mm, T8x4 = 4mm, T12x3 = 3mm, T12x6 = 6mm.
    // These are far less efficient (~40%) vs ball screws (~90%) but cost ~10x less.
    // ------------------------------------------------------------------------

    static readonly COTSPart oLeadScrew_T8x2 = new()
    {
        strManufacturer     = "Generic ACME",
        strPartNumber       = "T8x2",
        strDescription      = "8mm OD trapezoidal lead screw, 2mm lead, Delrin nut",
        eCategory           = PartCategory.LeadScrew,
        eBudget             = BudgetTier.Budget,

        fLength             = 1000f,
        fWidth              = 8f,           // screw OD
        fHeight             = 2f,           // lead per revolution
        fRailWidth          = 0f,
        fBlockLength        = 15f,          // Delrin nut length
        fBlockWidth         = 20f,          // Delrin nut OD/flange width
        fBlockHeight        = 20f,          // Delrin nut height
        fBoltDiameter       = 3f,           // M3 nut mounting bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -8f,  8f },           // 2-bolt flange
        afBlockBoltY        = new[] {  0f,  0f },

        fDynamicLoad        = 1500f,        // ~1.5 kN (Delrin nut)
        fStaticLoad         = 2500f,
        fMaxSpeed           = 600f,         // ~600 RPM before whip
        fAccuracy           = 0.15f,        // ±0.15 mm/300mm
        fWeight             = 0.39f,        // ~0.39 kg/m

        fShaftDiameter      = 8f,           // same as OD (no turned-down ends)
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oLeadScrew_T8x4 = new()
    {
        strManufacturer     = "Generic ACME",
        strPartNumber       = "T8x4",
        strDescription      = "8mm OD trapezoidal lead screw, 4mm lead, Delrin nut",
        eCategory           = PartCategory.LeadScrew,
        eBudget             = BudgetTier.Budget,

        fLength             = 1000f,
        fWidth              = 8f,           // screw OD
        fHeight             = 4f,           // lead
        fRailWidth          = 0f,
        fBlockLength        = 15f,
        fBlockWidth         = 20f,
        fBlockHeight        = 20f,
        fBoltDiameter       = 3f,
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -8f,  8f },
        afBlockBoltY        = new[] {  0f,  0f },

        fDynamicLoad        = 1500f,
        fStaticLoad         = 2500f,
        fMaxSpeed           = 800f,         // faster lead = same RPM = more speed
        fAccuracy           = 0.15f,
        fWeight             = 0.39f,

        fShaftDiameter      = 8f,
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oLeadScrew_T12x3 = new()
    {
        strManufacturer     = "Generic ACME",
        strPartNumber       = "T12x3",
        strDescription      = "12mm OD trapezoidal lead screw, 3mm lead, brass nut",
        eCategory           = PartCategory.LeadScrew,
        eBudget             = BudgetTier.Budget,

        fLength             = 1200f,
        fWidth              = 12f,          // screw OD
        fHeight             = 3f,           // lead
        fRailWidth          = 0f,
        fBlockLength        = 20f,          // brass nut length
        fBlockWidth         = 25f,          // brass nut OD/flange
        fBlockHeight        = 25f,          // brass nut height
        fBoltDiameter       = 4f,           // M4 nut mounting
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -10f, 10f },
        afBlockBoltY        = new[] {   0f,  0f },

        fDynamicLoad        = 3000f,        // ~3 kN (brass nut)
        fStaticLoad         = 5000f,
        fMaxSpeed           = 600f,
        fAccuracy           = 0.10f,        // ±0.10 mm/300mm
        fWeight             = 0.89f,        // ~0.89 kg/m

        fShaftDiameter      = 12f,
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oLeadScrew_T12x6 = new()
    {
        strManufacturer     = "Generic ACME",
        strPartNumber       = "T12x6",
        strDescription      = "12mm OD trapezoidal lead screw, 6mm lead, brass nut",
        eCategory           = PartCategory.LeadScrew,
        eBudget             = BudgetTier.Budget,

        fLength             = 1200f,
        fWidth              = 12f,          // screw OD
        fHeight             = 6f,           // lead
        fRailWidth          = 0f,
        fBlockLength        = 20f,
        fBlockWidth         = 25f,
        fBlockHeight        = 25f,
        fBoltDiameter       = 4f,
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -10f, 10f },
        afBlockBoltY        = new[] {   0f,  0f },

        fDynamicLoad        = 3000f,
        fStaticLoad         = 5000f,
        fMaxSpeed           = 800f,
        fAccuracy           = 0.10f,
        fWeight             = 0.89f,

        fShaftDiameter      = 12f,
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart[] aLeadScrews = {
        oLeadScrew_T8x2, oLeadScrew_T8x4, oLeadScrew_T12x3, oLeadScrew_T12x6
    };

    // ------------------------------------------------------------------------
    // STEPPER MOTORS — NEMA standard frame sizes
    //
    // NEMA 17 = 42.3mm square face, NEMA 23 = 56.4mm, NEMA 24 = 60mm,
    // NEMA 34 = 85.6mm. Bipolar hybrid steppers, 1.8° step angle (200 steps/rev).
    // Holding torque drops at speed; usable to ~800-1000 RPM.
    // ------------------------------------------------------------------------

    static readonly COTSPart oStepper_17HS4401 = new()
    {
        strManufacturer     = "NEMA Standard",
        strPartNumber       = "17HS4401",
        strDescription      = "NEMA 17 stepper motor, 0.42 Nm, 40mm body, 5mm shaft",
        eCategory           = PartCategory.StepperMotor,
        eBudget             = BudgetTier.Budget,

        fLength             = 0f,           // N/A for motors
        fWidth              = 42.3f,        // NEMA 17 face width
        fHeight             = 40f,          // body length as "height"
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 3f,           // M3 mounting bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -15.5f, -15.5f,  15.5f,  15.5f },  // 31mm square pattern
        afBlockBoltY        = new[] { -15.5f,  15.5f, -15.5f,  15.5f },

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 800f,         // ~800 RPM usable
        fAccuracy           = 0f,
        fWeight             = 0.28f,        // 0.28 kg

        fHoldingTorque      = 0.42f,        // Nm
        fRatedPower         = 0f,
        fRatedSpeed         = 0f,
        fShaftDiameter      = 5f,           // 5mm shaft
        fMotorBodyLength    = 40f,
        fNemaSize           = 17f,

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oStepper_23HS5628 = new()
    {
        strManufacturer     = "NEMA Standard",
        strPartNumber       = "23HS5628",
        strDescription      = "NEMA 23 stepper motor, 1.26 Nm, 56mm body, 6.35mm shaft",
        eCategory           = PartCategory.StepperMotor,
        eBudget             = BudgetTier.Standard,

        fLength             = 0f,
        fWidth              = 56.4f,        // NEMA 23 face width
        fHeight             = 56f,          // body length
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 5f,           // M5 mounting bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -23.57f, -23.57f,  23.57f,  23.57f },  // 47.14mm square
        afBlockBoltY        = new[] { -23.57f,  23.57f, -23.57f,  23.57f },

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 800f,
        fAccuracy           = 0f,
        fWeight             = 0.70f,        // 0.70 kg

        fHoldingTorque      = 1.26f,
        fRatedPower         = 0f,
        fRatedSpeed         = 0f,
        fShaftDiameter      = 6.35f,        // 1/4" shaft
        fMotorBodyLength    = 56f,
        fNemaSize           = 23f,

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oStepper_23HS8430 = new()
    {
        strManufacturer     = "NEMA Standard",
        strPartNumber       = "23HS8430",
        strDescription      = "NEMA 23 stepper motor, 1.89 Nm, 76mm body, 6.35mm shaft",
        eCategory           = PartCategory.StepperMotor,
        eBudget             = BudgetTier.Standard,

        fLength             = 0f,
        fWidth              = 56.4f,
        fHeight             = 76f,          // body length
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 5f,           // M5 mounting bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -23.57f, -23.57f,  23.57f,  23.57f },
        afBlockBoltY        = new[] { -23.57f,  23.57f, -23.57f,  23.57f },

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 800f,
        fAccuracy           = 0f,
        fWeight             = 1.00f,        // 1.0 kg

        fHoldingTorque      = 1.89f,
        fRatedPower         = 0f,
        fRatedSpeed         = 0f,
        fShaftDiameter      = 6.35f,
        fMotorBodyLength    = 76f,
        fNemaSize           = 23f,

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oStepper_24HS9630 = new()
    {
        strManufacturer     = "NEMA Standard",
        strPartNumber       = "24HS9630",
        strDescription      = "NEMA 24 stepper motor, 2.83 Nm, 96mm body, 8mm shaft",
        eCategory           = PartCategory.StepperMotor,
        eBudget             = BudgetTier.Premium,

        fLength             = 0f,
        fWidth              = 60f,          // NEMA 24 face width
        fHeight             = 96f,          // body length
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 5f,           // M5 mounting bolts (same bolt circle as NEMA 23)
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -23.57f, -23.57f,  23.57f,  23.57f },
        afBlockBoltY        = new[] { -23.57f,  23.57f, -23.57f,  23.57f },

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 800f,
        fAccuracy           = 0f,
        fWeight             = 1.60f,        // 1.6 kg

        fHoldingTorque      = 2.83f,
        fRatedPower         = 0f,
        fRatedSpeed         = 0f,
        fShaftDiameter      = 8f,           // 8mm shaft
        fMotorBodyLength    = 96f,
        fNemaSize           = 24f,

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oStepper_34HS5435 = new()
    {
        strManufacturer     = "NEMA Standard",
        strPartNumber       = "34HS5435",
        strDescription      = "NEMA 34 stepper motor, 4.90 Nm, 94mm body, 14mm shaft",
        eCategory           = PartCategory.StepperMotor,
        eBudget             = BudgetTier.Premium,

        fLength             = 0f,
        fWidth              = 85.6f,        // NEMA 34 face width
        fHeight             = 94f,          // body length
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 6f,           // M6 mounting bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = new[] { -34.8f, -34.8f,  34.8f,  34.8f },  // 69.6mm square
        afBlockBoltY        = new[] { -34.8f,  34.8f, -34.8f,  34.8f },

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 800f,
        fAccuracy           = 0f,
        fWeight             = 2.80f,        // 2.8 kg

        fHoldingTorque      = 4.90f,
        fRatedPower         = 0f,
        fRatedSpeed         = 0f,
        fShaftDiameter      = 14f,          // 14mm shaft
        fMotorBodyLength    = 94f,
        fNemaSize           = 34f,

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart[] aStepperMotors = {
        oStepper_17HS4401, oStepper_23HS5628, oStepper_23HS8430,
        oStepper_24HS9630, oStepper_34HS5435
    };

    // ------------------------------------------------------------------------
    // SPINDLE MOTORS — GDZ series, air-cooled and water-cooled
    //
    // Chinese GDZ spindles are the standard for DIY CNC routers. Air-cooled
    // (0.8kW) is simpler, water-cooled (1.5kW+) is quieter and handles
    // longer runs. ER collet chucks are integral. VFD-driven, 220V.
    // ------------------------------------------------------------------------

    static readonly COTSPart oSpindle_GDZ65_800A = new()
    {
        strManufacturer     = "GDZ",
        strPartNumber       = "GDZ-65-800A",
        strDescription      = "0.8kW air-cooled spindle, 65mm OD, ER11, 24000 RPM",
        eCategory           = PartCategory.SpindleMotor,
        eBudget             = BudgetTier.Budget,

        fLength             = 180f,         // body length
        fWidth              = 65f,          // body OD
        fHeight             = 65f,          // body OD (cylindrical)
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 6f,           // M6 clamp bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 24000f,       // 24000 RPM
        fAccuracy           = 0.005f,       // 0.005 mm runout
        fWeight             = 3.0f,         // ~3 kg

        fRatedPower         = 0.8f,         // 0.8 kW
        fRatedSpeed         = 24000f,
        fSpindleOD          = 65f,
        fColletType         = 11f,          // ER11
        fMaxToolDiameter    = 7f,           // ER11 max: 7mm

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oSpindle_GDZ65_1500W = new()
    {
        strManufacturer     = "GDZ",
        strPartNumber       = "GDZ-65-1500W",
        strDescription      = "1.5kW water-cooled spindle, 65mm OD, ER11, 24000 RPM",
        eCategory           = PartCategory.SpindleMotor,
        eBudget             = BudgetTier.Standard,

        fLength             = 210f,         // body length
        fWidth              = 65f,
        fHeight             = 65f,
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 6f,
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 24000f,
        fAccuracy           = 0.005f,
        fWeight             = 5.0f,         // ~5 kg

        fRatedPower         = 1.5f,         // 1.5 kW
        fRatedSpeed         = 24000f,
        fSpindleOD          = 65f,
        fColletType         = 11f,          // ER11
        fMaxToolDiameter    = 7f,

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oSpindle_GDZ80_2200W = new()
    {
        strManufacturer     = "GDZ",
        strPartNumber       = "GDZ-80-2200W",
        strDescription      = "2.2kW water-cooled spindle, 80mm OD, ER20, 24000 RPM",
        eCategory           = PartCategory.SpindleMotor,
        eBudget             = BudgetTier.Standard,

        fLength             = 230f,         // body length
        fWidth              = 80f,          // body OD
        fHeight             = 80f,
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 8f,           // M8 clamp bolts
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 24000f,
        fAccuracy           = 0.005f,
        fWeight             = 6.5f,         // ~6.5 kg

        fRatedPower         = 2.2f,         // 2.2 kW
        fRatedSpeed         = 24000f,
        fSpindleOD          = 80f,
        fColletType         = 20f,          // ER20
        fMaxToolDiameter    = 13f,          // ER20 max: 13mm

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oSpindle_GDZ100_3000W = new()
    {
        strManufacturer     = "GDZ",
        strPartNumber       = "GDZ-100-3000W",
        strDescription      = "3.0kW water-cooled spindle, 100mm OD, ER25, 18000 RPM",
        eCategory           = PartCategory.SpindleMotor,
        eBudget             = BudgetTier.Premium,

        fLength             = 260f,         // body length
        fWidth              = 100f,         // body OD
        fHeight             = 100f,
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 8f,
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 18000f,       // 18000 RPM (larger = slower)
        fAccuracy           = 0.005f,
        fWeight             = 9.0f,         // ~9 kg

        fRatedPower         = 3.0f,         // 3.0 kW
        fRatedSpeed         = 18000f,
        fSpindleOD          = 100f,
        fColletType         = 25f,          // ER25
        fMaxToolDiameter    = 16f,          // ER25 max: 16mm

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart[] aSpindleMotors = {
        oSpindle_GDZ65_800A, oSpindle_GDZ65_1500W,
        oSpindle_GDZ80_2200W, oSpindle_GDZ100_3000W
    };

    // ------------------------------------------------------------------------
    // FLEXIBLE COUPLINGS — aluminum helical beam, stepper-to-screw
    //
    // Bore1 is the motor shaft end (smaller), Bore2 is the screw end (larger).
    // We store Bore1 in fShaftDiameter and encode Bore2 as a convention:
    // fHeight is used to store the second bore diameter (non-standard but
    // practical for this database — couplings have no meaningful "height").
    // ------------------------------------------------------------------------

    static readonly COTSPart oCoupling_D19L25 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "D19L25",
        strDescription      = "Flexible coupling, 19mm OD x 25mm length, bore 6.35mm / 8mm",
        eCategory           = PartCategory.Coupling,
        eBudget             = BudgetTier.Budget,

        fLength             = 25f,          // coupling overall length
        fWidth              = 19f,          // OD
        fHeight             = 8f,           // Bore2 (screw side, 8mm)
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 3f,           // M3 clamp screws
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 6000f,        // 6000 RPM max
        fAccuracy           = 0f,
        fWeight             = 0.02f,        // ~20g

        fShaftDiameter      = 6.35f,        // Bore1 (motor side)
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oCoupling_D25L30 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "D25L30",
        strDescription      = "Flexible coupling, 25mm OD x 30mm length, bore 6.35mm / 10mm",
        eCategory           = PartCategory.Coupling,
        eBudget             = BudgetTier.Budget,

        fLength             = 30f,
        fWidth              = 25f,          // OD
        fHeight             = 10f,          // Bore2 (screw side, 10mm)
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 4f,           // M4 clamp screws
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 6000f,
        fAccuracy           = 0f,
        fWeight             = 0.04f,        // ~40g

        fShaftDiameter      = 6.35f,        // Bore1 (motor side)
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oCoupling_D25L30_8_10 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "D25L30-8-10",
        strDescription      = "Flexible coupling, 25mm OD x 30mm length, bore 8mm / 10mm",
        eCategory           = PartCategory.Coupling,
        eBudget             = BudgetTier.Budget,

        fLength             = 30f,
        fWidth              = 25f,          // OD
        fHeight             = 10f,          // Bore2 (screw side, 10mm)
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 4f,
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 6000f,
        fAccuracy           = 0f,
        fWeight             = 0.04f,

        fShaftDiameter      = 8f,           // Bore1 (motor side, 8mm)
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oCoupling_D32L40 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "D32L40",
        strDescription      = "Flexible coupling, 32mm OD x 40mm length, bore 10mm / 12mm",
        eCategory           = PartCategory.Coupling,
        eBudget             = BudgetTier.Budget,

        fLength             = 40f,
        fWidth              = 32f,          // OD
        fHeight             = 12f,          // Bore2 (screw side, 12mm)
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 5f,           // M5 clamp screws
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 6000f,
        fAccuracy           = 0f,
        fWeight             = 0.08f,        // ~80g

        fShaftDiameter      = 10f,          // Bore1 (motor side, 10mm)
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart[] aCouplings = {
        oCoupling_D19L25, oCoupling_D25L30, oCoupling_D25L30_8_10, oCoupling_D32L40
    };

    // ------------------------------------------------------------------------
    // DRAG CHAINS — nylon cable carriers
    //
    // Inner dimensions determine what cables/hoses fit. Outer dimensions
    // determine mounting clearance. Bend radius is the minimum curve.
    // fLength = bend radius, fWidth/fHeight = outer dimensions.
    // ------------------------------------------------------------------------

    static readonly COTSPart oDragChain_15x20 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "15x20",
        strDescription      = "Cable drag chain, inner 15x20mm, outer 23x28mm, R38 bend",
        eCategory           = PartCategory.DragChain,
        eBudget             = BudgetTier.Budget,

        fLength             = 38f,          // bend radius
        fWidth              = 23f,          // outer width
        fHeight             = 28f,          // outer height
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 15f,          // inner width
        fBlockHeight        = 20f,          // inner height
        fBoltDiameter       = 4f,           // M4 mounting bolts
        fBoltSpacing        = 20f,          // chain pitch
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 10f,          // 10 m/s travel
        fAccuracy           = 0f,
        fWeight             = 0.3f,         // ~0.3 kg/m

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oDragChain_15x30 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "15x30",
        strDescription      = "Cable drag chain, inner 15x30mm, outer 23x38mm, R48 bend",
        eCategory           = PartCategory.DragChain,
        eBudget             = BudgetTier.Budget,

        fLength             = 48f,          // bend radius
        fWidth              = 23f,          // outer width
        fHeight             = 38f,          // outer height
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 15f,          // inner width
        fBlockHeight        = 30f,          // inner height
        fBoltDiameter       = 4f,
        fBoltSpacing        = 20f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 10f,
        fAccuracy           = 0f,
        fWeight             = 0.4f,

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oDragChain_25x30 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "25x30",
        strDescription      = "Cable drag chain, inner 25x30mm, outer 35x41mm, R55 bend",
        eCategory           = PartCategory.DragChain,
        eBudget             = BudgetTier.Standard,

        fLength             = 55f,          // bend radius
        fWidth              = 35f,          // outer width
        fHeight             = 41f,          // outer height
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 25f,          // inner width
        fBlockHeight        = 30f,          // inner height
        fBoltDiameter       = 5f,           // M5 mounting
        fBoltSpacing        = 30f,          // chain pitch
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 10f,
        fAccuracy           = 0f,
        fWeight             = 0.6f,

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oDragChain_25x40 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "25x40",
        strDescription      = "Cable drag chain, inner 25x40mm, outer 35x51mm, R75 bend",
        eCategory           = PartCategory.DragChain,
        eBudget             = BudgetTier.Standard,

        fLength             = 75f,          // bend radius
        fWidth              = 35f,          // outer width
        fHeight             = 51f,          // outer height
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 25f,          // inner width
        fBlockHeight        = 40f,          // inner height
        fBoltDiameter       = 5f,
        fBoltSpacing        = 30f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 10f,
        fAccuracy           = 0f,
        fWeight             = 0.7f,

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart[] aDragChains = {
        oDragChain_15x20, oDragChain_15x30, oDragChain_25x30, oDragChain_25x40
    };

    // ------------------------------------------------------------------------
    // LIMIT SWITCHES — microswitches and inductive proximity sensors
    // ------------------------------------------------------------------------

    static readonly COTSPart oLimitSwitch_KW12 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "KW12-3",
        strDescription      = "Microswitch limit switch, 20x10x6mm, 125VAC",
        eCategory           = PartCategory.LimitSwitch,
        eBudget             = BudgetTier.Budget,

        fLength             = 0f,
        fWidth              = 20f,          // body length
        fHeight             = 10f,          // body width
        fRailWidth          = 0f,
        fBlockLength        = 6f,           // body thickness
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 2.5f,         // M2.5 mounting
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 0f,
        fAccuracy           = 0.1f,         // ~0.1mm repeatability
        fWeight             = 0.002f,       // ~2g

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oLimitSwitch_LJ12A3 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "LJ12A3-4-Z/BX",
        strDescription      = "Inductive proximity sensor, M12 x 55mm, 6-36VDC, NPN NO",
        eCategory           = PartCategory.LimitSwitch,
        eBudget             = BudgetTier.Standard,

        fLength             = 55f,          // body length
        fWidth              = 12f,          // M12 thread OD
        fHeight             = 12f,
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 12f,          // M12 mounting thread
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 0f,
        fAccuracy           = 0.01f,        // ~0.01mm repeatability
        fWeight             = 0.03f,        // ~30g

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oLimitSwitch_SN04N = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "SN04-N",
        strDescription      = "Inductive proximity sensor, M18 x 55mm, 5-30VDC, NPN NO",
        eCategory           = PartCategory.LimitSwitch,
        eBudget             = BudgetTier.Standard,

        fLength             = 55f,          // body length
        fWidth              = 18f,          // M18 thread OD
        fHeight             = 18f,
        fRailWidth          = 0f,
        fBlockLength        = 0f,
        fBlockWidth         = 0f,
        fBlockHeight        = 0f,
        fBoltDiameter       = 18f,          // M18 mounting thread
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fDynamicLoad        = 0f,
        fStaticLoad         = 0f,
        fMaxSpeed           = 0f,
        fAccuracy           = 0.01f,
        fWeight             = 0.05f,        // ~50g

        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart[] aLimitSwitches = {
        oLimitSwitch_KW12, oLimitSwitch_LJ12A3, oLimitSwitch_SN04N
    };

    // ------------------------------------------------------------------------
    // FASTENERS — structural bolts
    //
    // Fasteners are sized based on the guideway bolt pattern. M5 for HGR15
    // machines, M6 for HGR20, M8 for HGR25. We store representative
    // socket-head cap screws (SHCS) in common lengths.
    // ------------------------------------------------------------------------

    static readonly COTSPart oFastener_M5 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "M5-SHCS",
        strDescription      = "M5 socket-head cap screw, 12.9 grade, 20mm length",
        eCategory           = PartCategory.Fastener,
        eBudget             = BudgetTier.Budget,

        fLength             = 20f,          // bolt length
        fWidth              = 5f,           // M5 thread
        fHeight             = 5f,           // head height
        fBoltDiameter       = 5f,
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fWeight             = 0.005f,       // ~5g per bolt
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oFastener_M6 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "M6-SHCS",
        strDescription      = "M6 socket-head cap screw, 12.9 grade, 25mm length",
        eCategory           = PartCategory.Fastener,
        eBudget             = BudgetTier.Budget,

        fLength             = 25f,
        fWidth              = 6f,           // M6 thread
        fHeight             = 6f,           // head height
        fBoltDiameter       = 6f,
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fWeight             = 0.010f,       // ~10g per bolt
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart oFastener_M8 = new()
    {
        strManufacturer     = "Generic",
        strPartNumber       = "M8-SHCS",
        strDescription      = "M8 socket-head cap screw, 12.9 grade, 30mm length",
        eCategory           = PartCategory.Fastener,
        eBudget             = BudgetTier.Budget,

        fLength             = 30f,
        fWidth              = 8f,           // M8 thread
        fHeight             = 8f,           // head height
        fBoltDiameter       = 8f,
        fBoltSpacing        = 0f,
        afBlockBoltX        = Array.Empty<float>(),
        afBlockBoltY        = Array.Empty<float>(),

        fWeight             = 0.020f,       // ~20g per bolt
        bCommonlyAvailable  = true,
        strDatasheetURL     = ""
    };

    static readonly COTSPart[] aFasteners = { oFastener_M5, oFastener_M6, oFastener_M8 };

    // ========================================================================
    // SELECTION ALGORITHM
    // ========================================================================

    /// <summary>
    /// Main entry point: takes user requirements and returns the optimal set
    /// of COTS parts for a DIY CNC router build.
    ///
    /// Selection priorities (in order):
    ///   1. Performance — parts must handle the required loads and spans
    ///   2. Budget tier — prefer parts matching the user's budget level
    ///   3. Compatibility — all interfaces (shafts, bolt patterns) must match
    ///   4. Availability — prefer commonly available parts
    /// </summary>
    public static CNCSelectedParts SelectParts(CNCRequirements req)
    {
        CNCSelectedParts sel = new();

        // Determine guideway size based on longest axis span and material
        int nGuideSize = GuidewaySizeFromSpan(req.fWorkAreaX);
        nGuideSize = UpgradeForMaterial(nGuideSize, req.eMaterial);

        sel.oYGuideway = SelectGuidewayBest(nGuideSize, req.eBudget);
        sel.oXGuideway = SelectGuidewayBest(nGuideSize, req.eBudget);
        sel.oZGuideway = SelectGuidewayBest(Math.Max(1, nGuideSize - 1), req.eBudget);

        // Drives: ball screw vs lead screw based on budget
        bool bUseBallScrew = req.eBudget >= BudgetTier.Standard;

        sel.oYDrive = SelectDrive(req.fWorkAreaY, bUseBallScrew, req.eBudget);
        sel.oXDrive = SelectDrive(req.fWorkAreaX, bUseBallScrew, req.eBudget);
        sel.oZDrive = SelectDrive(req.fWorkAreaZ, bUseBallScrew, req.eBudget);

        // Stepper motors: torque based on axis role
        sel.oYStepper = SelectStepperForAxis("Y", req.fWorkAreaX, req.eBudget);
        sel.oXStepper = SelectStepperForAxis("X", req.fWorkAreaX, req.eBudget);
        sel.oZStepper = SelectStepperForAxis("Z", req.fWorkAreaX, req.eBudget);

        // Spindle: power based on material to cut
        sel.oSpindle = SelectSpindle(req.eMaterial, req.eBudget);

        // Couplings: match motor shaft to drive screw shaft
        sel.oYCoupling = SelectCoupling(sel.oYStepper.fShaftDiameter,
                                         sel.oYDrive.fShaftDiameter);
        sel.oXCoupling = SelectCoupling(sel.oXStepper.fShaftDiameter,
                                         sel.oXDrive.fShaftDiameter);
        sel.oZCoupling = SelectCoupling(sel.oZStepper.fShaftDiameter,
                                         sel.oZDrive.fShaftDiameter);

        // Drag chains: size based on budget
        sel.oYDragChain = SelectDragChain(req.eBudget);
        sel.oXDragChain = sel.oYDragChain;  // same size for both axes

        // Limit switches: inductive prox for standard/premium, microswitch for budget
        sel.oLimitSwitch = SelectLimitSwitch(req.eBudget);

        // Fasteners: size based on guideway bolt diameter
        sel.oFastenerMain = SelectFastener(sel.oYGuideway.fBoltDiameter);

        return sel;
    }

    /// <summary>
    /// Returns a guideway size index: 0=HGR15, 1=HGR20, 2=HGR25.
    /// </summary>
    static int GuidewaySizeFromSpan(float fSpan)
    {
        if (fSpan < 500f)       return 0;   // HGR15
        if (fSpan <= 750f)      return 1;   // HGR20
        return 2;                            // HGR25
    }

    /// <summary>
    /// Upgrades guideway size for harder materials.
    /// Aluminum = +1, Steel = +2 (rigidity demands).
    /// </summary>
    static int UpgradeForMaterial(int nSize, MaterialToCut eMat)
    {
        return eMat switch
        {
            MaterialToCut.Aluminum => Math.Min(nSize + 1, 2),
            MaterialToCut.Steel    => Math.Min(nSize + 2, 2),
            _                      => nSize   // Wood, Plastic
        };
    }

    /// <summary>
    /// Selects the best guideway matching the requested size index and budget.
    /// Falls back to closest available size if the exact budget tier doesn't
    /// have a part at that size.
    /// </summary>
    static COTSPart SelectGuidewayBest(int nSizeIndex, BudgetTier eBudget)
    {
        // Find all guideways of the requested size index
        COTSPart? oExactBudget = null;
        COTSPart? oFallback    = null;

        foreach (COTSPart o in aLinearGuideways)
        {
            int nThisSize = o.fWidth switch
            {
                15f => 0,
                20f => 1,
                _   => 2   // 23mm = HGR25
            };

            if (nThisSize == nSizeIndex)
            {
                if (o.eBudget == eBudget)
                    oExactBudget = o;
                else if (oFallback == null)
                    oFallback = o;
                else if (Math.Abs((int)o.eBudget - (int)eBudget) <
                         Math.Abs((int)oFallback.Value.eBudget - (int)eBudget))
                    oFallback = o;
            }
        }

        if (oExactBudget.HasValue)
            return oExactBudget.Value;

        if (oFallback.HasValue)
            return oFallback.Value;

        // Ultimate fallback: return closest size
        return aLinearGuideways[Math.Min(nSizeIndex, aLinearGuideways.Length - 1)];
    }

    /// <summary>
    /// Selects a drive screw (ball screw or lead screw) for the given axis span.
    /// </summary>
    static COTSPart SelectDrive(float fSpan, bool bBallScrew, BudgetTier eBudget)
    {
        if (bBallScrew)
        {
            // Ball screw selection by span
            if (fSpan < 500f)
                return FindBestMatch(aBallScrews, 12f, eBudget);   // SFU1204
            if (fSpan <= 750f)
                return FindBestMatch(aBallScrews, 16f, eBudget);   // SFU1605
            if (fSpan <= 1200f)
                return FindBestMatch(aBallScrews, 20f, eBudget);   // SFU2005
            return FindBestMatch(aBallScrews, 25f, eBudget);       // SFU2505
        }
        else
        {
            // Lead screw selection by span (budget)
            if (fSpan < 500f)
                return oLeadScrew_T8x4;     // small span, fast lead OK
            if (fSpan <= 750f)
                return oLeadScrew_T12x3;    // medium span
            return oLeadScrew_T12x6;        // long span, bigger is stiffer
        }
    }

    /// <summary>
    /// Finds the best match from an array where fWidth equals the search OD.
    /// </summary>
    static COTSPart FindBestMatch(COTSPart[] aParts, float fSearchOD, BudgetTier eBudget)
    {
        COTSPart oBest = aParts[0];
        int       nBestDist = int.MaxValue;

        foreach (COTSPart o in aParts)
        {
            if (Math.Abs(o.fWidth - fSearchOD) < 0.5f)
            {
                int nDist = Math.Abs((int)o.eBudget - (int)eBudget);
                if (nDist < nBestDist)
                {
                    nBestDist = nDist;
                    oBest = o;
                }
            }
        }
        return oBest;
    }

    /// <summary>
    /// Selects a stepper motor based on axis role and machine size.
    ///
    /// Y-axis: heaviest (moves entire gantry + Z assembly)
    /// X-axis: medium (moves Z assembly only)
    /// Z-axis: lightest (moves spindle + Z carriage only)
    ///
    /// Budget: downsizes motors to save cost.
    /// </summary>
    static COTSPart SelectStepperForAxis(string strAxis, float fSpan, BudgetTier eBudget)
    {
        bool bLarge = fSpan > 750f;

        return strAxis switch
        {
            "Y" => eBudget switch
            {
                BudgetTier.Budget  => bLarge ? oStepper_23HS5628 : oStepper_17HS4401,
                BudgetTier.Standard => bLarge ? oStepper_23HS8430 : oStepper_23HS5628,
                BudgetTier.Premium => bLarge ? oStepper_24HS9630 : oStepper_23HS8430,
                _                  => oStepper_23HS5628
            },

            "X" => eBudget switch
            {
                BudgetTier.Budget  => bLarge ? oStepper_23HS5628 : oStepper_17HS4401,
                BudgetTier.Standard => oStepper_23HS5628,
                BudgetTier.Premium => bLarge ? oStepper_23HS8430 : oStepper_23HS5628,
                _                  => oStepper_23HS5628
            },

            _ => eBudget switch     // Z-axis
            {
                BudgetTier.Budget  => oStepper_17HS4401,
                BudgetTier.Standard => oStepper_23HS5628,
                BudgetTier.Premium => oStepper_23HS5628,
                _                  => oStepper_17HS4401
            }
        };
    }

    /// <summary>
    /// Selects a spindle based on material and budget.
    ///
    /// Wood/plastic: 0.8kW sufficient, 1.5kW generous
    /// Aluminum:      1.5kW minimum, 2.2kW recommended
    /// Steel:         2.2kW minimum, 3.0kW for productivity
    /// Budget:        step down one power level
    /// </summary>
    static COTSPart SelectSpindle(MaterialToCut eMat, BudgetTier eBudget)
    {
        int nPowerIndex = eMat switch
        {
            MaterialToCut.Wood    => 0,     // 0.8kW
            MaterialToCut.Plastic => 0,     // 0.8kW
            MaterialToCut.Aluminum => 1,    // 1.5kW
            MaterialToCut.Steel   => 2,     // 2.2kW
            _                     => 0
        };

        // Budget: step down one tier
        if (eBudget == BudgetTier.Budget)
            nPowerIndex = Math.Max(0, nPowerIndex - 1);

        // Premium: step up if beneficial
        if (eBudget == BudgetTier.Premium && eMat >= MaterialToCut.Aluminum)
            nPowerIndex = Math.Min(aSpindleMotors.Length - 1, nPowerIndex + 1);

        return aSpindleMotors[Math.Clamp(nPowerIndex, 0, aSpindleMotors.Length - 1)];
    }

    /// <summary>
    /// Selects a flexible coupling that matches the motor shaft (Bore1)
    /// and screw end shaft (Bore2).
    ///
    /// Coupling convention: fShaftDiameter = Bore1 (motor side),
    /// fHeight = Bore2 (screw side). This is a pragmatic reuse of fields
    /// for parts that have no natural "height" dimension.
    /// </summary>
    static COTSPart SelectCoupling(float fMotorShaft, float fScrewShaft)
    {
        COTSPart oBest = aCouplings[0];
        float    fBestScore = float.MaxValue;

        foreach (COTSPart o in aCouplings)
        {
            float fBore1 = o.fShaftDiameter;
            float fBore2 = o.fHeight;       // Bore2 stored in fHeight per coupling convention

            // Score: how closely do the bores match? (lower is better)
            float fScore = Math.Abs(fBore1 - fMotorShaft) + Math.Abs(fBore2 - fScrewShaft);

            if (fScore < fBestScore)
            {
                fBestScore = fScore;
                oBest = o;
            }
        }
        return oBest;
    }

    /// <summary>
    /// Selects a drag chain: 15x30 for budget/standard, 25x30 for premium/large.
    /// </summary>
    static COTSPart SelectDragChain(BudgetTier eBudget)
    {
        return eBudget >= BudgetTier.Premium ? oDragChain_25x30 : oDragChain_15x30;
    }

    /// <summary>
    /// Selects limit switches: inductive proximity for standard/premium,
    /// microswitch for budget.
    /// </summary>
    static COTSPart SelectLimitSwitch(BudgetTier eBudget)
    {
        return eBudget >= BudgetTier.Standard ? oLimitSwitch_LJ12A3 : oLimitSwitch_KW12;
    }

    /// <summary>
    /// Selects structural fasteners based on guideway bolt diameter:
    /// M4 rail bolts → M5 structure, M5 → M6, M6 → M8.
    /// </summary>
    static COTSPart SelectFastener(float fRailBoltDia)
    {
        if (fRailBoltDia <= 4f)   return oFastener_M5;
        if (fRailBoltDia <= 5f)   return oFastener_M6;
        return oFastener_M8;
    }

    // ========================================================================
    // BILL OF MATERIALS PRINTER
    // ========================================================================

    /// <summary>
    /// Prints a formatted Bill of Materials (BOM) with part numbers and key specs
    /// for the selected COTS parts.
    /// </summary>
    public static void PrintPartsList(CNCSelectedParts parts)
    {
        Log("================================================================");
        Log("  PicoCNC — BILL OF MATERIALS (COTS Parts)");
        Log("================================================================");

        PrintPart("Y-AXIS GUIDEWAY (×2)", parts.oYGuideway);
        PrintPart("X-AXIS GUIDEWAY (×2)", parts.oXGuideway);
        PrintPart("Z-AXIS GUIDEWAY",      parts.oZGuideway);
        PrintPart("Y-AXIS DRIVE",         parts.oYDrive);
        PrintPart("X-AXIS DRIVE",         parts.oXDrive);
        PrintPart("Z-AXIS DRIVE",         parts.oZDrive);
        PrintPart("Y-AXIS STEPPER",       parts.oYStepper);
        PrintPart("X-AXIS STEPPER",       parts.oXStepper);
        PrintPart("Z-AXIS STEPPER",       parts.oZStepper);
        PrintPart("SPINDLE MOTOR",        parts.oSpindle);
        PrintPart("Y-AXIS COUPLING",      parts.oYCoupling);
        PrintPart("X-AXIS COUPLING",      parts.oXCoupling);
        PrintPart("Z-AXIS COUPLING",      parts.oZCoupling);
        PrintPart("Y-AXIS DRAG CHAIN",    parts.oYDragChain);
        PrintPart("X-AXIS DRAG CHAIN",    parts.oXDragChain);
        PrintPart("LIMIT SWITCHES",       parts.oLimitSwitch);
        PrintPart("STRUCTURAL FASTENERS", parts.oFastenerMain);

        Log("================================================================");

        // Summary of key specs
        Log($"Guideway load rating: {parts.oYGuideway.fDynamicLoad / 1000f:F1} kN dynamic");
        Log($"Y stepper torque:     {parts.oYStepper.fHoldingTorque:F2} Nm");
        Log($"Spindle power:        {parts.oSpindle.fRatedPower:F1} kW, " +
                     $"{parts.oSpindle.fRatedSpeed:F0} RPM, " +
                     $"ER{parts.oSpindle.fColletType:F0} collet");
        Log($"Budget tier:          {parts.oYGuideway.eBudget}");
        Log("================================================================");
    }

    /// <summary>
    /// Logs a single part entry with key specs.
    /// </summary>
    static void PrintPart(string strLabel, COTSPart o)
    {
        if (string.IsNullOrEmpty(o.strPartNumber))
        {
            Log($"  {strLabel}: (not selected)");
            return;
        }

        Log($"  {strLabel}:");
        Log($"    {o.strManufacturer} {o.strPartNumber} — {o.strDescription}");

        // Print relevant specs based on category
        switch (o.eCategory)
        {
            case PartCategory.LinearGuide:
                Log($"    Rail: {o.fWidth:F0}x{o.fHeight:F0}mm, " +
                             $"Block: {o.fBlockWidth:F0}x{o.fBlockLength:F0}x{o.fBlockHeight:F0}mm");
                Log($"    Bolt: M{o.fBoltDiameter:F0} @ {o.fBoltSpacing:F0}mm spacing, " +
                             $"Dyn Load: {o.fDynamicLoad / 1000f:F1} kN");
                break;

            case PartCategory.BallScrew:
            case PartCategory.LeadScrew:
                Log($"    OD: {o.fWidth:F0}mm, Lead: {o.fHeight:F0}mm, " +
                             $"Nut: {o.fBlockWidth:F0}x{o.fBlockHeight:F0}mm");
                Log($"    Accuracy: {o.fAccuracy:F3} mm/300mm, " +
                             $"Shaft end: {o.fShaftDiameter:F0}mm");
                break;

            case PartCategory.StepperMotor:
                Log($"    NEMA {o.fNemaSize:F0}, {o.fHoldingTorque:F2} Nm, " +
                             $"Body: {o.fMotorBodyLength:F0}mm, " +
                             $"Shaft: {o.fShaftDiameter:F2}mm");
                Log($"    Mounting: M{o.fBoltDiameter:F0} bolts, " +
                             $"Face: {o.fWidth:F1}mm");
                break;

            case PartCategory.SpindleMotor:
                Log($"    {o.fRatedPower:F1} kW, {o.fRatedSpeed:F0} RPM, " +
                             $"OD: {o.fSpindleOD:F0}mm, " +
                             $"ER{o.fColletType:F0} collet");
                Log($"    Body: {o.fLength:F0}mm long, " +
                             $"Max tool: {o.fMaxToolDiameter:F0}mm, " +
                             $"Weight: {o.fWeight:F1} kg");
                break;

            case PartCategory.Coupling:
                Log($"    OD: {o.fWidth:F0}mm, Length: {o.fLength:F0}mm, " +
                             $"Bores: {o.fShaftDiameter:F2}mm / {o.fHeight:F0}mm");
                break;

            case PartCategory.DragChain:
                Log($"    Inner: {o.fBlockWidth:F0}x{o.fBlockHeight:F0}mm, " +
                             $"Outer: {o.fWidth:F0}x{o.fHeight:F0}mm, " +
                             $"Bend R: {o.fLength:F0}mm");
                break;

            case PartCategory.LimitSwitch:
                Log($"    {o.strDescription}");
                break;

            case PartCategory.Fastener:
                Log($"    {o.strPartNumber} — {o.strDescription}");
                break;
        }
    }
}

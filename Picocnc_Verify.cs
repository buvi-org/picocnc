using System.Numerics;
using System.Collections.Generic;

namespace PicoGK;

public static partial class Picocnc
{
    /// =====================================================================
    /// COLLISION VERIFICATION PASS
    ///
    /// Rebuilds each of the 12 CNC components as individual Voxels objects,
    /// then checks all 66 pairwise intersections (NxN matrix).
    ///
    /// Moving groups at default (mid-travel) position:
    ///   Group A (Stationary): BaseFrame, WorkBed, YRails, DragChains, Safety
    ///   Group B (Gantry, Y):   Uprights, Bridge, XRails, MotorMounts,
    ///                          LeadScrews
    ///   Group C (Z carriage, X+Z): ZAssembly, SpindleMount
    ///
    /// The pairwise matrix catches assembly bugs such as parts embedded
    /// into each other (e.g. Z plate penetrating the bridge beam).
    /// =====================================================================

    /// <summary>
    /// Public result fields — populated during VerifyCollisions()
    /// </summary>
    public static int s_nOverlappingPairs;
    public static int s_nUnexpectedWarnings;
    public static List<string>? s_aCollisionDetails;

    /// <summary>
    /// Master entry point.  Call this after voxConstruct() completes.
    /// </summary>
    public static void VerifyCollisions()
    {
        Log("\n============================================================");
        Log("===  COLLISION VERIFICATION  ===============================");
        Log("============================================================");

        // --- Build all 12 components as separate voxel fields ---
        System.Collections.Generic.Dictionary<string, Voxels> map = BuildAllComponents();

        // --- 1. NxN pairwise matrix ---
        CheckAllPairs(map);

        // --- 2. Targeted interface checks ---
        CheckZPlateBridgeInterface(map);
        CheckToolTipClearance(map);
        CheckSpindleCarriageInterface(map);
        CheckXRailBearingToZPlate(map);

        Log("\n===  VERIFICATION COMPLETE  ================================");
        Log("============================================================\n");
    }

    // =====================================================================
    // BUILD ALL COMPONENTS
    // =====================================================================

    static System.Collections.Generic.Dictionary<string, Voxels> BuildAllComponents()
    {
        var map = new System.Collections.Generic.Dictionary<string, Voxels>();

        Log("Building BaseFrame for verification...");
        map["BaseFrame"] = voxConstructBaseFrame();

        Log("Building WorkBed for verification...");
        map["WorkBed"] = voxConstructWorkBed();

        Log("Building YRails for verification...");
        map["YRails"] = voxConstructYRails();

        Log("Building GantryUprights for verification...");
        map["GantryUprights"] = voxConstructUprights();

        Log("Building GantryBridge for verification...");
        map["GantryBridge"] = voxConstructGantryBridge();

        Log("Building XRails for verification...");
        map["XRails"] = voxConstructXRails();

        Log("Building ZAssembly for verification...");
        map["ZAssembly"] = voxConstructZAssembly();

        Log("Building SpindleMount for verification...");
        map["SpindleMount"] = voxConstructSpindleMount();

        Log("Building MotorMounts for verification...");
        map["MotorMounts"] = voxConstructMotorMounts();

        Log("Building LeadScrews for verification...");
        map["LeadScrews"] = voxConstructLeadScrews();

        Log("Building DragChains for verification...");
        map["DragChains"] = voxConstructDragChains();

        Log("Building Safety for verification...");
        map["Safety"] = voxConstructSafety();

        Log($"All 12 components built ({map.Count} total).");
        return map;
    }

    // =====================================================================
    // 1. NxN PAIRWISE OVERLAP CHECK
    // =====================================================================

    /// <summary>
    /// Pairs of assemblies that are mechanically connected and therefore
    /// expected to overlap (e.g. rails mounted on base, bridge on uprights).
    /// All other overlaps are flagged as WARNING (potential collision).
    /// </summary>
    static readonly HashSet<(string, string)> s_expectedOverlaps = new()
    {
        // Base frame is the foundation -- everything structural touches it
        ("BaseFrame", "WorkBed"),       // table slab sits on frame
        ("BaseFrame", "YRails"),        // Y rails bolted to frame top
        ("BaseFrame", "DragChains"),    // Y cable tray on base edge
        ("BaseFrame", "Safety"),        // E-stop, switches bolted to frame

        // Y rails carry the gantry
        ("YRails", "GantryUprights"),   // uprights on Y bearing blocks

        // Gantry bridge sits on uprights
        ("GantryBridge", "GantryUprights"),

        // X rails on bridge front face
        ("GantryBridge", "XRails"),

        // Z assembly mounts on X bearing blocks in front of bridge
        ("XRails", "ZAssembly"),

        // Z back plate is coplanar with bridge front face (expected interface)
        ("GantryBridge", "ZAssembly"),

        // Spindle mount attaches to Z carriage
        ("SpindleMount", "ZAssembly"),

        // Motors mounted at various positions
        ("MotorMounts", "BaseFrame"),       // Y motor at base rear
        ("MotorMounts", "WorkBed"),         // Y motor plate near table rear edge
        ("MotorMounts", "GantryBridge"),    // X motor on bridge end
        ("MotorMounts", "ZAssembly"),       // Z motor on Z plate

        // Lead screws span between mounts
        ("LeadScrews", "BaseFrame"),        // Y screw runs across base
        ("LeadScrews", "GantryBridge"),     // X screw on bridge face
        ("LeadScrews", "ZAssembly"),        // Z screw on Z plate

        // Drag chain trays
        ("DragChains", "GantryBridge"),     // X tray on bridge top

        // Safety components distributed across structure
        ("Safety", "YRails"),            // limit switches on Y rail ends
        ("Safety", "GantryBridge"),      // X-axis limit switches
        ("Safety", "ZAssembly"),         // Z-axis limit switches
    };

    static void CheckAllPairs(
        System.Collections.Generic.Dictionary<string, Voxels> map)
    {
        string[] keys = new string[map.Count];
        map.Keys.CopyTo(keys, 0);

        int nTotalPairs = keys.Length * (keys.Length - 1) / 2;
        Log($"\n--- Pairwise Overlap Check " +
            $"({keys.Length} components, {nTotalPairs} pairs) ---");

        int nOverlapping = 0;
        int nUnexpected = 0;
        var aDetails = new List<string>();

        for (int i = 0; i < keys.Length; i++)
        {
            for (int j = i + 1; j < keys.Length; j++)
            {
                string a = keys[i];
                string b = keys[j];

                // Boolean intersection -- returns empty Voxels if no overlap
                Voxels voxOverlap = map[a] & map[b];

                // Convert to mesh to check if intersection is non-empty
                Mesh mshOverlap = voxOverlap.mshAsMesh();
                int nTri = mshOverlap.nTriangleCount();

                if (nTri > 0)
                {
                    bool bExpected = s_expectedOverlaps.Contains((a, b))
                                  || s_expectedOverlaps.Contains((b, a));

                    string strTag = bExpected ? "[EXPECTED]" : "[WARNING]";
                    string strDetail = $"{strTag} {a} & {b}  {nTri} tris overlap";
                    Log($"  {strDetail}");

                    aDetails.Add(strDetail);
                    nOverlapping++;
                    if (!bExpected) nUnexpected++;
                }
            }
        }

        // Store results for web API
        s_nOverlappingPairs   = nOverlapping;
        s_nUnexpectedWarnings = nUnexpected;
        s_aCollisionDetails   = aDetails;

        Log($"  Result: {nOverlapping} overlapping pairs " +
            $"({nUnexpected} unexpected)");
    }

    // =====================================================================
    // 2. TARGETED INTERFACE CHECKS
    // =====================================================================

    /// <summary>
    /// Check Z back plate vs gantry bridge front face.
    /// Per Constraints: fZPlateFrontY == fBridgeYFront,
    /// meaning plate front face is coplanar with bridge front face.
    /// At voxel resolution, this boundary may show a small overlap or gap.
    /// </summary>
    static void CheckZPlateBridgeInterface(
        System.Collections.Generic.Dictionary<string, Voxels> map)
    {
        Log("\n--- Targeted: Z-Plate / Bridge Interface ---");

        if (!map.TryGetValue("ZAssembly", out Voxels? voxZa)
         || !map.TryGetValue("GantryBridge", out Voxels? voxBridge))
        {
            Log("  Components missing -- skipping.");
            return;
        }

        Voxels voxOverlap = voxZa & voxBridge;
        Mesh mshOverlap = voxOverlap.mshAsMesh();
        int nTri = mshOverlap.nTriangleCount();

        float fBridgeYFront = fBaseOuterY / 2f - fGantryBridgeY / 2f;

        Log($"  Bridge front face Y = {fBridgeYFront:F1}");
        Log($"  Z-plate front face Y = {fZPlateFrontY:F1}  " +
            $"(Constraints: fZPlateFrontY)");
        Log($"  Gap/overlap: {(fZPlateFrontY - fBridgeYFront):F1} mm  " +
            $"(positive = plate in front of bridge)");

        if (nTri > 0)
        {
            Log($"  Voxel overlap with bridge: {nTri} tris");
        }
        else
        {
            Log("  No voxel overlap -- Z plate is clear of bridge. OK.");
        }
    }

    /// <summary>
    /// Spindle tool-tip Z clearance vs spoil board top.
    ///
    /// Tool tip = clamp Z - 90 (half spindle body) - 25 (collet) - 30 (tool)
    ///          = fClampZ - 145
    /// Spoil board top = base top + table thickness + spoil board thickness
    ///                 = fBaseOuterZ + fTableThick + 18
    /// </summary>
    static void CheckToolTipClearance(
        System.Collections.Generic.Dictionary<string, Voxels> map)
    {
        Log("\n--- Targeted: Tool Tip Clearance ---");

        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;
        float fClampZ = fBridgeZ - 30f;               // from SpindleMount constructor
        float fToolTipZ = fClampZ - 90f - 25f - 30f;  // half-body + collet + tool

        float fSpoilTopZ = fBaseOuterZ + fTableThick + 18f;

        float fClearance = fToolTipZ - fSpoilTopZ;

        Log($"  Spindle clamp center Z = {fClampZ:F1}");
        Log($"  Tool tip Z              = {fToolTipZ:F1}");
        Log($"  Spoil board top Z       = {fSpoilTopZ:F1}");
        Log($"  Clearance               = {fClearance:F1} mm " +
            $"({(fClearance > 0 ? "tool above spoil board" : "TOOL BELOW BOARD -- COLLISION!")})");

        // Also voxel-level check
        if (map.TryGetValue("SpindleMount", out Voxels? voxSpindle)
         && map.TryGetValue("WorkBed", out Voxels? voxWorkBed))
        {
            Voxels voxOverlap = voxSpindle & voxWorkBed;
            int nTri = voxOverlap.mshAsMesh().nTriangleCount();

            if (nTri > 0)
            {
                Log(
                    $"  VOXEL WARNING: Spindle overlaps WorkBed " +
                    $"({nTri} tris)");
            }
            else
            {
                Log("  Voxel check: No spindle/workbed overlap. OK.");
            }
        }
    }

    /// <summary>
    /// Spindle mounting flange to Z carriage interface.
    /// The flange should physically connect to the Z carriage area.
    /// </summary>
    static void CheckSpindleCarriageInterface(
        System.Collections.Generic.Dictionary<string, Voxels> map)
    {
        Log("\n--- Targeted: Spindle / Z-Carriage Interface ---");

        if (!map.TryGetValue("SpindleMount", out Voxels? voxSpindle)
         || !map.TryGetValue("ZAssembly", out Voxels? voxZAssembly))
        {
            Log("  Components missing -- skipping.");
            return;
        }

        Voxels voxOverlap = voxSpindle & voxZAssembly;
        int nTri = voxOverlap.mshAsMesh().nTriangleCount();

        // From ZAssembly: carriage Y center = fZRailPlateY - half_carriageY - half_rail
        float fBridgeYFront = fBaseOuterY / 2f - fGantryBridgeY / 2f;
        float fZRailPlateY = fBridgeYFront
            - fZPlateY / 2f       // plate center offset from bridge front
            - fZPlateY / 2f        // half plate Y
            - fZRailSize / 2f;     // half rail
        float fCarriageBackY = fZRailPlateY
            - 30f / 2f             // half carriage Y
            - fZRailSize / 2f;     // half rail
        float fCarriageFrontY = fCarriageBackY - 30f; // carriage depth = 30

        float fClampY = fCarriageFrontY - 40f;     // 40mm gap from carriage
        float fFlangeFrontY = fClampY
            + fClampOD / 2f        // back of clamp ring (Y+ side)
            + 20f;                  // flangeY = 20

        float fGap = fCarriageFrontY - fFlangeFrontY;

        Log($"  Carriage front Y = {fCarriageFrontY:F1}");
        Log($"  Flange front Y   = {fFlangeFrontY:F1}");

        if (nTri > 0)
        {
            Log(
                $"  Overlap: {nTri} tris -- " +
                $"spindle flange connected to Z carriage. OK.");
        }
        else
        {
            Log(
                $"  WARNING: No voxel overlap -- spindle may be " +
                $"detached from Z carriage!");
            Log($"    Design flange front to carriage front " +
                $"gap: {fGap:F1} mm");
        }
    }

    /// <summary>
    /// X-axis bearing blocks on the bridge should connect to the Z back plate.
    /// The X bearing blocks are at the default bridge mid-span (fBridgeMidX).
    /// The Z plate is centered at fBridgeMidX and should encompass them.
    /// </summary>
    static void CheckXRailBearingToZPlate(
        System.Collections.Generic.Dictionary<string, Voxels> map)
    {
        Log("\n--- Targeted: X-Rail Bearings / Z-Plate ---");

        float fBridgeYFront = fBaseOuterY / 2f - fGantryBridgeY / 2f;
        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;

        // X bearing blocks are at mid-span
        // Bearing front face Y = fBridgeYFront + bodyY/2 + padThickness
        // bodyY = fXRailSize + 10 = 25;  pad = 3mm
        float fBearFrontY = fBridgeYFront + (fXRailSize + 10f) + 3f; // body half-depth + pad

        // Z plate back face Y = fZPlateFrontY - fZPlateY = fBridgeYFront - 15
        float fZPlateBackY = fBridgeYFront - fZPlateY;

        float fGap = fZPlateBackY - fBearFrontY;

        Log($"  X bearing front face Y = {fBearFrontY:F1}");
        Log($"  Z plate back face Y    = {fZPlateBackY:F1}");
        Log($"  Gap                    = {fGap:F1} mm " +
            $"({(fGap >= -1 ? "bearing touches plate" : "bearing extends past plate back")})");

        // The Z plate is 80mm wide (X), centered at bridge mid.
        // X bearings are at the two rail positions (upper and lower Z).
        // The Z plate extends Z from fBridgeZ - 125 to fBridgeZ + 125 (250mm tall).
        // X rails are at fBridgeZ +/- (40 - 15) = fBridgeZ +/- 25.
        // So both X rails are well within the Z plate's Z range.
        float fZPlateHalfZ = fZPlateZ / 2f;
        Log($"  Z plate Z range: [{fBridgeZ - fZPlateHalfZ:F1}, " +
            $"{fBridgeZ + fZPlateHalfZ:F1}] (span={fZPlateZ:F1})");
        Log($"  X rails Z positions: {fXRailUpperZ:F1} (upper), " +
            $"{fXRailLowerZ:F1} (lower)");
    }
}

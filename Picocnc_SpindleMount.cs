using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Spindle mount: cylindrical clamp ring + mounting flange + slit + bolt bosses,
    /// plus spindle motor body, ER collet, tool placeholder, cooling jacket, and top cap.
    /// Attaches to the Z-axis carriage.
    /// </summary>
    public static Voxels voxConstructSpindleMount()
    {
        Log("Building spindle mount...");

        // --- Spindle body dimensions (local constants) ---
        float fSpindleBodyHeight    = 180f;  // typical 1.5-2.2kW spindle motor length
        float fColletOD             = 40f;   // ER20 collet nut outer diameter
        float fColletHeight         = 25f;   // ER20 collet nut height
        float fToolDia              = 6f;    // 1/4" end mill shank diameter
        float fToolLength           = 30f;   // exposed tool length below collet
        float fCoolingJacketOD      = fSpindleOD + 10f;  // 75mm — water jacket OD
        float fCoolingJacketHeight  = 120f;  // water jacket height
        float fBarbOD               = 8f;    // inlet/outlet barb diameter
        float fBarbLength           = 20f;   // inlet/outlet barb length
        float fTopCapThickness      = 10f;   // spindle top cap thickness
        float fCableOD              = 12f;   // power cable exit diameter
        float fCableHeight          = 15f;   // power cable exit height

        // --- Shared origin: same X,Y as the clamp ring ---
        float fMidY = fBaseOuterY / 2f;
        float fBridgeMidX = fBaseOuterX / 2f;
        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;

        // Z carriage front face position
        float fCarriageFront = fMidY
            - fGantryBridgeY / 2f
            - fZPlateY
            - fZRailSize
            - 30f / 2f
            - fZRailSize / 2f;

        // Spindle clamp position: below Z carriage, in front of it
        float fClampY = fCarriageFront - 40f;
        float fClampZ = fBridgeZ - 30f; // offset downward from bridge center

        Vector3 vecClampCenter = new(fBridgeMidX, fClampY, fClampZ);

        // ==================================================================
        // 1. CLAMP RING (existing code, preserved)
        // ==================================================================
        Voxels voxOuterClamp = voxCylinderZ(fClampOD, fClampHeight, vecClampCenter);
        Voxels voxInnerBore  = voxCylinderZ(fSpindleOD, fClampHeight + 20f, vecClampCenter);

        Voxels voxClampRing = voxOuterClamp - voxInnerBore;

        // --- Clamp slit: thin vertical cut ---
        Voxels voxSlit = voxBox(
            new Vector3(fClampSlit, fClampOD + 10f, fClampHeight + 10f),
            vecClampCenter);
        voxClampRing.BoolSubtract(voxSlit);

        // --- Mounting flange: plate connecting clamp to Z carriage ---
        // Reduced Y from 20mm to 12mm to clear Z lead screw
        float fFlangeX = fZPlateX;
        float fFlangeY = 12f;
        float fFlangeZ = 80f;

        Vector3 vecFlangeCenter = new(
            fBridgeMidX,
            fClampY + fClampOD / 2f + fFlangeY / 2f,
            fClampZ);

        Voxels voxFlange = voxBox(
            new Vector3(fFlangeX, fFlangeY, fFlangeZ),
            vecFlangeCenter);

        // --- Bolt bosses on clamp slit ---
        float fBossDia = 14f;
        float fBossDepth = 20f;

        for (int side = -1; side <= 1; side += 2)
        {
            for (float zOff = -fClampHeight / 3f; zOff <= fClampHeight / 3f; zOff += fClampHeight * 2f / 3f)
            {
                Vector3 vecBoss = new(
                    fBridgeMidX + side * (fClampOD / 2f - 3f),
                    fClampY,
                    fClampZ + zOff);

                voxClampRing += voxCylinderY(fBossDia, fBossDepth, vecBoss);
            }
        }

        // ==================================================================
        // 2. SPINDLE MOTOR BODY (cylinder centered inside the clamp ring)
        // ==================================================================
        // Center is coaxial with the clamp: same X,Y, same Z center
        Vector3 vecSpindleCenter = new(fBridgeMidX, fClampY, fClampZ);
        Voxels voxSpindleBody = voxCylinderZ(fSpindleOD, fSpindleBodyHeight, vecSpindleCenter);

        // ==================================================================
        // 3. ER COLLET NUT (below the spindle body)
        // ==================================================================
        // Spindle body bottom is at fClampZ - fSpindleBodyHeight/2 = fClampZ - 90
        // Collet nut sits directly below: its top is at the spindle bottom
        float fColletCenterZ = fClampZ - fSpindleBodyHeight / 2f - fColletHeight / 2f;
        Vector3 vecColletCenter = new(fBridgeMidX, fClampY, fColletCenterZ);
        Voxels voxCollet = voxCylinderZ(fColletOD, fColletHeight, vecColletCenter);

        // ==================================================================
        // 4. TOOL PLACEHOLDER (end mill below collet)
        // ==================================================================
        float fToolCenterZ = fColletCenterZ - fColletHeight / 2f - fToolLength / 2f;
        Vector3 vecToolCenter = new(fBridgeMidX, fClampY, fToolCenterZ);
        Voxels voxTool = voxCylinderZ(fToolDia, fToolLength, vecToolCenter);

        // ==================================================================
        // 5. COOLING JACKET (water-cooled spindle shell)
        // ==================================================================
        // Jacket covers the upper portion of the spindle body (120mm tall)
        // OD = 75mm (fSpindleOD + 10), leaving a 5mm wall around the 65mm motor
        float fJacketCenterZ = fClampZ + fSpindleBodyHeight / 2f - fCoolingJacketHeight / 2f;
        Vector3 vecJacketCenter = new(fBridgeMidX, fClampY, fJacketCenterZ);

        Voxels voxJacketOuter = voxCylinderZ(fCoolingJacketOD, fCoolingJacketHeight, vecJacketCenter);
        Voxels voxJacketInner = voxCylinderZ(fSpindleOD, fCoolingJacketHeight + 10f, vecJacketCenter);
        Voxels voxCoolingJacket = voxJacketOuter - voxJacketInner;

        // Inlet/outlet barbs on the sides of the jacket
        float fJacketRadius = fCoolingJacketOD / 2f;
        float fBarbZLow  = fJacketCenterZ - fCoolingJacketHeight / 4f;
        float fBarbZHigh = fJacketCenterZ + fCoolingJacketHeight / 4f;

        // Barb on +X side (inlet), positioned radially outward
        Vector3 vecBarbInlet = new(
            fBridgeMidX + fJacketRadius,
            fClampY,
            fBarbZLow);
        Voxels voxBarbInlet = voxCylinderX(fBarbOD, fBarbLength, vecBarbInlet);

        // Barb on -X side (outlet), positioned radially outward
        Vector3 vecBarbOutlet = new(
            fBridgeMidX - fJacketRadius,
            fClampY,
            fBarbZHigh);
        Voxels voxBarbOutlet = voxCylinderX(fBarbOD, fBarbLength, vecBarbOutlet);

        // ==================================================================
        // 6. SPINDLE TOP CAP (disc + cable exit)
        // ==================================================================
        float fTopCapCenterZ = fClampZ + fSpindleBodyHeight / 2f + fTopCapThickness / 2f;
        Vector3 vecTopCapCenter = new(fBridgeMidX, fClampY, fTopCapCenterZ);
        Voxels voxTopCap = voxCylinderZ(fSpindleOD, fTopCapThickness, vecTopCapCenter);

        // Cable exit cylinder on top of the cap
        float fCableCenterZ = fTopCapCenterZ + fTopCapThickness / 2f + fCableHeight / 2f;
        Vector3 vecCableCenter = new(fBridgeMidX, fClampY, fCableCenterZ);
        Voxels voxCable = voxCylinderZ(fCableOD, fCableHeight, vecCableCenter);

        // ==================================================================
        // COMPOSE: union all components
        // ==================================================================
        Voxels voxResult = new();
        voxResult += voxClampRing;
        voxResult += voxFlange;
        voxResult += voxSpindleBody;
        voxResult += voxCollet;
        voxResult += voxTool;
        voxResult += voxCoolingJacket;
        voxResult += voxBarbInlet;
        voxResult += voxBarbOutlet;
        voxResult += voxTopCap;
        voxResult += voxCable;

        Log("Spindle mount done: clamp + body + collet + tool + cooling jacket + top cap.");
        return voxResult;
    }
}

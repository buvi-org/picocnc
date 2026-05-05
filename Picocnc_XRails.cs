using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// X-axis rails: two parallel rails mounted on the front face of the gantry bridge,
    /// running in X direction with bolt holes, realistic bearing blocks, rail end
    /// supports, and grease ports.
    /// </summary>
    public static Voxels voxConstructXRails()
    {
        Library.Log("Building X-axis rails...");

        float fMidY = fBaseOuterY / 2f;
        float fBridgeMidX = fBaseOuterX / 2f;
        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;

        float fBridgeYFront = fMidY - fGantryBridgeY / 2f;
        float fRailSize = 15f;

        float fRailSpanX = (fBaseOuterX - fRailInsetX) - fRailInsetX - fUprightX;
        float fRailMidX = fBridgeMidX;

        float fRailHalfSpan = fRailSpanX / 2f;
        float fRailStartX = fRailMidX - fRailHalfSpan;
        float fRailEndX   = fRailMidX + fRailHalfSpan;

        // Two rails: upper and lower on the bridge front face
        float fRailZ_Upper = fBridgeZ + fGantryBridgeZ / 2f - fRailSize;
        float fRailZ_Lower = fBridgeZ - fGantryBridgeZ / 2f + fRailSize;

        // Bearing block dimensions
        float fBearBodyX = 30f;              // along rail (X)
        float fBearBodyY = fRailSize + 10f;  // 25mm — forward from gantry
        float fBearBodyZ = fRailSize + 10f;  // 25mm — vertical

        float fCapSizeX = 5f;                // along rail
        float fCapSizeY = fBearBodyY - 4f;   // 21mm
        float fCapSizeZ = fBearBodyZ - 4f;   // 21mm

        float fPadThick = 3f;

        // Support block dimensions
        float fSupportSizeX = 15f;              // along rail
        float fSupportSizeY = fRailSize + 10f;  // 25mm — into gantry
        float fSupportSizeZ = fRailSize + 10f;  // 25mm — vertical

        Voxels voxXRails = new();

        foreach (float fRailZ in new[] { fRailZ_Upper, fRailZ_Lower })
        {
            Vector3 vecRailCenter = new(fRailMidX, fBridgeYFront, fRailZ);

            // =================================================================
            // Main rail bar
            // =================================================================
            Voxels voxRail = voxCylinderX(fRailSize, fRailSpanX, vecRailCenter);

            // --- Bolt holes through the rail into the bridge (Y-axis, 80mm spacing) ---
            Vector3 vecHoleAxis  = new(0, 1, 0); // drill into front face
            Vector3 vecHoleStart = new(fRailStartX + fBoltSpacingY / 2f, fBridgeYFront, fRailZ);
            Vector3 vecHoleEnd   = new(fRailEndX   - fBoltSpacingY / 2f, fBridgeYFront, fRailZ);

            List<Voxels> aRailHoles = aBoltHolesAlongLine(
                vecHoleStart, vecHoleEnd,
                fBoltSpacingY, fBoltHoleDia, fGantryBridgeY + 10f,
                vecHoleAxis);
            SubtractHoles(ref voxRail, aRailHoles);

            // =================================================================
            // Rail end supports (capping blocks at each end)
            // =================================================================
            Vector3 vecSuppStart = new(fRailStartX + fSupportSizeX / 2f, fBridgeYFront, fRailZ);
            Vector3 vecSuppEnd   = new(fRailEndX   - fSupportSizeX / 2f, fBridgeYFront, fRailZ);

            // Bolt holes through supports into gantry (Y-axis holes)
            float fSuppBoltDepth = fSupportSizeY + fGantryBridgeY / 2f + 10f;
            // Two holes per support, spaced vertically (Z) by 12mm
            Vector3 vecHoleTop = vecSuppStart + new Vector3(0, 0, 6);
            Vector3 vecHoleBot = vecSuppStart - new Vector3(0, 0, 6);

            Voxels voxSuppStart = voxBox(
                new Vector3(fSupportSizeX, fSupportSizeY, fSupportSizeZ), vecSuppStart);
            Voxels voxSuppEnd = voxBox(
                new Vector3(fSupportSizeX, fSupportSizeY, fSupportSizeZ), vecSuppEnd);

            // Subtract bolt holes from start support
            foreach (Vector3 vecH in new[] { vecSuppStart + new Vector3(0, 0, 6),
                                              vecSuppStart - new Vector3(0, 0, 6) })
            {
                voxSuppStart.BoolSubtract(voxCylinder(
                    vecH - vecHoleAxis * (fSuppBoltDepth / 2f),
                    vecH + vecHoleAxis * (fSuppBoltDepth / 2f),
                    fBoltHoleDia));
            }

            // Subtract bolt holes from end support
            foreach (Vector3 vecH in new[] { vecSuppEnd + new Vector3(0, 0, 6),
                                              vecSuppEnd - new Vector3(0, 0, 6) })
            {
                voxSuppEnd.BoolSubtract(voxCylinder(
                    vecH - vecHoleAxis * (fSuppBoltDepth / 2f),
                    vecH + vecHoleAxis * (fSuppBoltDepth / 2f),
                    fBoltHoleDia));
            }

            voxRail += voxSuppStart;
            voxRail += voxSuppEnd;

            // =================================================================
            // Grease ports at rail ends (tiny cylinders extending outward along X)
            // =================================================================
            float fGreaseOD = 4f;
            float fGreaseL  = 6f;
            Voxels voxGreaseStart = voxCylinder(
                new Vector3(fRailStartX,          fBridgeYFront, fRailZ),
                new Vector3(fRailStartX - fGreaseL, fBridgeYFront, fRailZ),
                fGreaseOD);
            Voxels voxGreaseEnd = voxCylinder(
                new Vector3(fRailEndX,          fBridgeYFront, fRailZ),
                new Vector3(fRailEndX + fGreaseL, fBridgeYFront, fRailZ),
                fGreaseOD);
            voxRail += voxGreaseStart;
            voxRail += voxGreaseEnd;

            voxXRails += voxRail;

            // =================================================================
            // Realistic linear bearing block (one per rail at mid-span)
            // =================================================================
            float fBearingX = fRailMidX;
            float fBearingY = fBridgeYFront + fBearBodyY / 2f;
            float fBearingZ = fRailZ;

            Vector3 vecBearCenter = new(fBearingX, fBearingY, fBearingZ);

            Voxels voxBearing = new();

            // Central body
            voxBearing += voxBox(
                new Vector3(fBearBodyX, fBearBodyY, fBearBodyZ),
                vecBearCenter);

            // Two end caps (stepped appearance at wiper-seal ends)
            float fCapHalfX = fBearBodyX / 2f + fCapSizeX / 2f;
            Vector3 vecCapRight = new(fBearingX + fCapHalfX, fBearingY, fBearingZ);
            Vector3 vecCapLeft  = new(fBearingX - fCapHalfX, fBearingY, fBearingZ);
            voxBearing += voxBox(new Vector3(fCapSizeX, fCapSizeY, fCapSizeZ), vecCapRight);
            voxBearing += voxBox(new Vector3(fCapSizeX, fCapSizeY, fCapSizeZ), vecCapLeft);

            // Side wipers on end faces (1mm thick, 4mm wide, full block height)
            float fWiperX = 1f;
            float fWiperY = 4f;
            float fWiperZ = fBearBodyZ;
            float fWiperXOff = fBearBodyX / 2f + fCapSizeX + fWiperX / 2f;
            voxBearing += voxBox(
                new Vector3(fWiperX, fWiperY, fWiperZ),
                new Vector3(fBearingX + fWiperXOff, fBearingY, fBearingZ));
            voxBearing += voxBox(
                new Vector3(fWiperX, fWiperY, fWiperZ),
                new Vector3(fBearingX - fWiperXOff, fBearingY, fBearingZ));

            // Front mounting pad (on the Y face — away from gantry)
            float fPadY = fBearingY + fBearBodyY / 2f + fPadThick / 2f;
            Voxels voxPad = voxBox(
                new Vector3(fBearBodyX, fPadThick, fBearBodyZ),
                new Vector3(fBearingX, fPadY, fBearingZ));
            voxBearing += voxPad;

            // 4 bolt holes on front mounting surface (M5 clearance, 15mm deep, into body along -Y)
            float fBoltXOff = 10f;   // 20mm apart along rail
            float fBoltZOff = 7.5f;  // 15mm apart vertically
            float fBoltDepth = 15f;
            float fFrontY = fPadY + fPadThick / 2f;
            Vector3 vecBoltAxis = new(0, -1, 0); // into body

            Vector3[] aFrontBolts = new[] {
                new Vector3(fBearingX - fBoltXOff, fFrontY, fBearingZ - fBoltZOff),
                new Vector3(fBearingX - fBoltXOff, fFrontY, fBearingZ + fBoltZOff),
                new Vector3(fBearingX + fBoltXOff, fFrontY, fBearingZ - fBoltZOff),
                new Vector3(fBearingX + fBoltXOff, fFrontY, fBearingZ + fBoltZOff),
            };
            foreach (Vector3 vecBoltFront in aFrontBolts)
            {
                Vector3 vecBoltBack = vecBoltFront + vecBoltAxis * fBoltDepth;
                voxBearing.BoolSubtract(voxCylinder(vecBoltBack, vecBoltFront, fBoltHoleDia));
            }

            // Zerk grease fitting on front pad (6mm OD, 8mm tall, between left-side bolt holes)
            float fZerkOD = 6f;
            float fZerkH  = 8f;
            float fZerkY  = fFrontY + fZerkH / 2f;
            Vector3 vecZerk = new(fBearingX - fBoltXOff, fZerkY, fBearingZ);
            // Cylinder along Y, extending outward from pad
            voxBearing += voxCylinder(
                new Vector3(fBearingX - fBoltXOff, fFrontY, fBearingZ),
                new Vector3(fBearingX - fBoltXOff, fFrontY + fZerkH, fBearingZ),
                fZerkOD);

            voxXRails += voxBearing;
        }

        Library.Log("X-axis rails done.");
        return voxXRails;
    }
}

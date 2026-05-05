using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Y-axis linear rails: two parallel rails along the Y edges of the base frame,
    /// with bolt holes, realistic bearing blocks, rail end supports, and grease ports.
    /// </summary>
    public static Voxels voxConstructYRails()
    {
        Library.Log("Building Y-axis rails...");

        Voxels voxYRails = new();

        // Rail positions (left and right edges of base)
        float fRailX_Left  = fRailInsetX;
        float fRailX_Right = fBaseOuterX - fRailInsetX;

        float fRailStartY = 0f;
        float fRailEndY   = fBaseOuterY;
        float fRailMidY   = fBaseOuterY / 2f;

        // Rail center sits on top of base frame
        float fRailZ = fBaseOuterZ + fRailHeight / 2f;

        // Bearing block dimensions
        float fBearBodyX = fRailWidth + 10f;    // 30mm — cross-rail width
        float fBearBodyY = 30f;                  // along rail
        float fBearBodyZ = fRailHeight + 15f;    // 40mm tall

        // End cap dimensions (smaller cross-section, 5mm long each)
        float fCapSizeX = fBearBodyX - 4f;       // 26mm
        float fCapSizeY = 5f;
        float fCapSizeZ = fBearBodyZ - 4f;       // 36mm

        // Top mounting pad
        float fPadThick = 3f;

        // Support block dimensions
        float fSupportSizeX = fRailWidth + 10f;  // 30mm
        float fSupportSizeY = 15f;                // along rail
        float fSupportSizeZ = fRailHeight + 5f;   // 30mm tall

        foreach (float fRailX in new[] { fRailX_Left, fRailX_Right })
        {
            // =================================================================
            // Main rail bar
            // =================================================================
            Voxels voxRail = voxCylinderY(fRailWidth, fBaseOuterY,
                new Vector3(fRailX, fRailMidY, fRailZ));

            // --- Bolt holes along the rail (vertical, Z-axis, at 80mm spacing) ---
            Vector3 vecHoleAxis = new(0, 0, 1);
            Vector3 vecHoleStart = new(fRailX, fRailStartY + fBoltSpacingY / 2f, fRailZ);
            Vector3 vecHoleEnd   = new(fRailX, fRailEndY   - fBoltSpacingY / 2f, fRailZ);

            List<Voxels> aRailHoles = aBoltHolesAlongLine(
                vecHoleStart, vecHoleEnd,
                fBoltSpacingY, fBoltHoleDia, fRailHeight + 10f,
                vecHoleAxis);
            SubtractHoles(ref voxRail, aRailHoles);

            // =================================================================
            // Rail end supports (capping blocks at each end)
            // =================================================================
            Vector3 vecSuppStart = new(fRailX, fRailStartY + fSupportSizeY / 2f, fRailZ);
            Vector3 vecSuppEnd   = new(fRailX, fRailEndY   - fSupportSizeY / 2f, fRailZ);

            Voxels voxSupportStart = voxBox(
                new Vector3(fSupportSizeX, fSupportSizeY, fSupportSizeZ), vecSuppStart);
            Voxels voxSupportEnd = voxBox(
                new Vector3(fSupportSizeX, fSupportSizeY, fSupportSizeZ), vecSuppEnd);

            // Bolt holes through the supports (into base frame, Z-axis downward)
            float fSuppBoltDepth = fSupportSizeZ + 10f;
            // Start support bolt holes
            foreach (float fOffX in new[] { -6f, 6f })
            {
                Vector3 vecH = vecSuppStart + new Vector3(fOffX, 0, 0);
                voxSupportStart.BoolSubtract(voxCylinder(
                    vecH - new Vector3(0, 0, fSuppBoltDepth / 2f),
                    vecH + new Vector3(0, 0, fSuppBoltDepth / 2f),
                    fBoltHoleDia));
            }
            // End support bolt holes
            foreach (float fOffX in new[] { -6f, 6f })
            {
                Vector3 vecH = vecSuppEnd + new Vector3(fOffX, 0, 0);
                voxSupportEnd.BoolSubtract(voxCylinder(
                    vecH - new Vector3(0, 0, fSuppBoltDepth / 2f),
                    vecH + new Vector3(0, 0, fSuppBoltDepth / 2f),
                    fBoltHoleDia));
            }

            voxRail += voxSupportStart;
            voxRail += voxSupportEnd;

            // =================================================================
            // Grease ports at rail ends (tiny cylinders extending outward)
            // =================================================================
            float fGreaseOD = 4f;
            float fGreaseL  = 6f;
            Voxels voxGreaseStart = voxCylinder(
                new Vector3(fRailX, fRailStartY,          fRailZ),
                new Vector3(fRailX, fRailStartY - fGreaseL, fRailZ),
                fGreaseOD);
            Voxels voxGreaseEnd = voxCylinder(
                new Vector3(fRailX, fRailEndY,          fRailZ),
                new Vector3(fRailX, fRailEndY + fGreaseL, fRailZ),
                fGreaseOD);
            voxRail += voxGreaseStart;
            voxRail += voxGreaseEnd;

            voxYRails += voxRail;

            // =================================================================
            // Realistic linear bearing block (one per rail at mid-span)
            // =================================================================
            float fBearingY = fRailMidY;
            float fBearingZ = fRailZ + fBearBodyZ / 2f - fRailHeight / 2f;

            Vector3 vecBearCenter = new(fRailX, fBearingY, fBearingZ);

            Voxels voxBearing = new();

            // Central body
            voxBearing += voxBox(
                new Vector3(fBearBodyX, fBearBodyY, fBearBodyZ),
                vecBearCenter);

            // Two end caps (stepped appearance at wiper-seal ends)
            float fCapHalfY = fBearBodyY / 2f + fCapSizeY / 2f;
            Vector3 vecCapNorth = new(fRailX, fBearingY + fCapHalfY, fBearingZ);
            Vector3 vecCapSouth = new(fRailX, fBearingY - fCapHalfY, fBearingZ);
            voxBearing += voxBox(new Vector3(fCapSizeX, fCapSizeY, fCapSizeZ), vecCapNorth);
            voxBearing += voxBox(new Vector3(fCapSizeX, fCapSizeY, fCapSizeZ), vecCapSouth);

            // Side wipers on end faces (1mm thick, full block height, 4mm wide)
            float fWiperX = 4f;
            float fWiperY = 1f;
            float fWiperZ = fBearBodyZ;
            float fWiperYOff = fBearBodyY / 2f + fCapSizeY + fWiperY / 2f;
            voxBearing += voxBox(
                new Vector3(fWiperX, fWiperY, fWiperZ),
                new Vector3(fRailX, fBearingY + fWiperYOff, fBearingZ));
            voxBearing += voxBox(
                new Vector3(fWiperX, fWiperY, fWiperZ),
                new Vector3(fRailX, fBearingY - fWiperYOff, fBearingZ));

            // Top mounting pad (flat plate on top of the bearing)
            float fPadZ = fBearingZ + fBearBodyZ / 2f + fPadThick / 2f;
            Voxels voxPad = voxBox(
                new Vector3(fBearBodyX, fBearBodyY, fPadThick),
                new Vector3(fRailX, fBearingY, fPadZ));
            voxBearing += voxPad;

            // 4 bolt holes on top mounting surface (M5 clearance, 15mm deep)
            float fBoltYOff = 10f;   // 20mm apart along rail
            float fBoltXOff = 7.5f;  // 15mm apart across rail
            float fBoltDepth = 15f;
            float fTopZ = fPadZ + fPadThick / 2f;

            Vector3[] aTopBolts = new[] {
                new Vector3(fRailX - fBoltXOff, fBearingY - fBoltYOff, fTopZ),
                new Vector3(fRailX - fBoltXOff, fBearingY + fBoltYOff, fTopZ),
                new Vector3(fRailX + fBoltXOff, fBearingY - fBoltYOff, fTopZ),
                new Vector3(fRailX + fBoltXOff, fBearingY + fBoltYOff, fTopZ),
            };
            foreach (Vector3 vecBoltTop in aTopBolts)
            {
                Vector3 vecBoltBot = vecBoltTop - new Vector3(0, 0, fBoltDepth);
                voxBearing.BoolSubtract(voxCylinder(vecBoltBot, vecBoltTop, fBoltHoleDia));
            }

            // Zerk grease fitting on top pad (6mm OD, 8mm tall)
            float fZerkOD = 6f;
            float fZerkH  = 8f;
            float fZerkZ  = fTopZ + fZerkH / 2f;
            Vector3 vecZerk = new(fRailX - fBoltXOff, fBearingY, fZerkZ);
            voxBearing += voxCylinderZ(fZerkOD, fZerkH, vecZerk);

            voxYRails += voxBearing;
        }

        Library.Log("Y-axis rails done.");
        return voxYRails;
    }
}

using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Y-axis linear rails: two parallel rails along the Y edges of the base frame,
    /// with bolt holes and bearing blocks.
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

        // Rail sits on top of base frame
        float fRailZ = fBaseOuterZ + fRailHeight / 2f;

        foreach (float fRailX in new[] { fRailX_Left, fRailX_Right })
        {
            Vector3 vecRailCenter = new(fRailX, fRailMidY, fRailZ);

            // Main rail bar
            Voxels voxRail = voxCylinderY(fRailWidth, fBaseOuterY, vecRailCenter);

            // Bolt holes along the rail (vertical, Z-axis)
            Vector3 vecHoleAxis = new(0, 0, 1);
            Vector3 vecHoleStart = new(fRailX, fRailStartY + fBoltSpacingY / 2f, fRailZ);
            Vector3 vecHoleEnd   = new(fRailX, fRailEndY - fBoltSpacingY / 2f, fRailZ);

            List<Voxels> aHoles = aBoltHolesAlongLine(
                vecHoleStart, vecHoleEnd,
                fBoltSpacingY, fBoltHoleDia, fRailHeight + 10f,
                vecHoleAxis);

            SubtractHoles(ref voxRail, aHoles);
            voxYRails += voxRail;

            // --- Bearing blocks (sliding carriages) ---
            // Two bearing blocks per rail, positioned along Y
            float fBearingY = fRailMidY;
            float fBearingSize = 40f;
            float fBearingH = fRailHeight + 15f;

            Voxels voxBearing = voxBox(
                new Vector3(fRailWidth + 10f, fBearingSize, fBearingH),
                new Vector3(fRailX, fBearingY, fRailZ + fBearingH / 2f - fRailHeight / 2f));

            voxYRails += voxBearing;
        }

        Library.Log("Y-axis rails done.");
        return voxYRails;
    }
}

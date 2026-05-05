using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// X-axis rails: two parallel rails mounted on the front face of the gantry bridge,
    /// running in X direction with bolt holes.
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

        // Two rails: upper and lower on the bridge front face
        float fRailZ_Upper = fBridgeZ + fGantryBridgeZ / 2f - fRailSize;
        float fRailZ_Lower = fBridgeZ - fGantryBridgeZ / 2f + fRailSize;

        Voxels voxXRails = new();

        foreach (float fRailZ in new[] { fRailZ_Upper, fRailZ_Lower })
        {
            Vector3 vecRailCenter = new(fRailMidX, fBridgeYFront, fRailZ);

            Voxels voxRail = voxCylinderX(fRailSize, fRailSpanX, vecRailCenter);

            // Bolt holes through the rail into the bridge (Y-axis holes)
            Vector3 vecHoleStart = new(fRailMidX - fRailSpanX / 2f + 30f, fBridgeYFront, fRailZ);
            Vector3 vecHoleEnd   = new(fRailMidX + fRailSpanX / 2f - 30f, fBridgeYFront, fRailZ);
            Vector3 vecHoleAxis  = new(0, 1, 0); // drill into front face

            List<Voxels> aHoles = aBoltHolesAlongLine(
                vecHoleStart, vecHoleEnd,
                fBoltSpacingY, fBoltHoleDia, fGantryBridgeY + 10f,
                vecHoleAxis);

            SubtractHoles(ref voxRail, aHoles);
            voxXRails += voxRail;

            // --- Bearing blocks (X-axis carriages) ---
            float fBearingX = fBridgeMidX;
            float fBearingSize = 40f;
            Voxels voxBearing = voxBox(
                new Vector3(fBearingSize, fRailSize + 10f, fRailSize + 10f),
                new Vector3(fBearingX, fBridgeYFront + (fRailSize + 10f) / 2f, fRailZ));

            voxXRails += voxBearing;
        }

        Library.Log("X-axis rails done.");
        return voxXRails;
    }
}

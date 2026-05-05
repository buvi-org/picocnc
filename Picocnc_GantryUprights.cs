using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Gantry uprights: two vertical columns at the Y-rail positions,
    /// with gussets and top mounting plates for the bridge.
    /// </summary>
    public static Voxels voxConstructUprights()
    {
        Library.Log("Building gantry uprights...");

        Voxels voxUprights = new();

        float fRailX_Left  = fRailInsetX;
        float fRailX_Right = fBaseOuterX - fRailInsetX;

        // Upright base Z: top of Y rails
        float fBaseZ = fBaseOuterZ + fRailHeight;
        float fMidZ = fBaseZ + fUprightZ / 2f;

        // Y position: mid-travel
        float fMidY = fBaseOuterY / 2f;

        foreach (float fX in new[] { fRailX_Left, fRailX_Right })
        {
            Vector3 vecColCenter = new(fX, fMidY, fMidZ);

            // Main column
            Voxels voxColumn = voxBox(
                new Vector3(fUprightX, fUprightY, fUprightZ),
                vecColCenter);

            // --- Base gussets (triangular reinforcements) ---
            Voxels voxGussets = new();
            float fGussetSize = 40f;
            float fGussetThick = fRibThick;

            // Front and back gussets (Y direction)
            foreach (float fYSign in new[] { -1f, 1f })
            {
                float fGY = fMidY + fYSign * (fUprightY / 2f + fGussetSize / 4f);
                Voxels voxGussetY = voxBox(
                    new Vector3(fUprightX + fGussetSize, fGussetThick, fGussetSize),
                    new Vector3(fX, fGY, fBaseZ + fGussetSize / 2f));
                voxGussets += voxGussetY;
            }

            // Side gussets (X direction)
            foreach (float fXSign in new[] { -1f, 1f })
            {
                float fGX = fX + fXSign * (fUprightX / 2f + fGussetSize / 4f);
                Voxels voxGussetX = voxBox(
                    new Vector3(fGussetThick, fUprightY + fGussetSize, fGussetSize),
                    new Vector3(fGX, fMidY, fBaseZ + fGussetSize / 2f));
                voxGussets += voxGussetX;
            }

            // --- Top mounting plate ---
            float fPlateThick = 12f;
            Voxels voxTopPlate = voxBox(
                new Vector3(fUprightX + 20f, fUprightY + 20f, fPlateThick),
                new Vector3(fX, fMidY, fBaseZ + fUprightZ + fPlateThick / 2f));

            // Bolt holes in top plate (vertical)
            List<Vector3> aBoltPos = aBoltCircle(
                new Vector3(fX, fMidY, fBaseZ + fUprightZ + fPlateThick / 2f),
                30f, 4);
            foreach (Vector3 vecBolt in aBoltPos)
            {
                Voxels voxHole = voxCylinderZ(
                    fBoltHoleDia, fPlateThick + 5f, vecBolt);
                voxTopPlate.BoolSubtract(voxHole);
            }

            voxUprights += voxColumn;
            voxUprights += voxGussets;
            voxUprights += voxTopPlate;
        }

        Library.Log("Gantry uprights done.");
        return voxUprights;
    }
}

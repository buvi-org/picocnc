using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Spindle mount: cylindrical clamp ring + mounting flange + slit.
    /// Attaches to the Z-axis carriage.
    /// </summary>
    public static Voxels voxConstructSpindleMount()
    {
        Library.Log("Building spindle mount...");

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

        // --- Clamp ring ---
        Voxels voxOuterClamp = voxCylinderZ(fClampOD, fClampHeight, vecClampCenter);
        Voxels voxInnerBore  = voxCylinderZ(fSpindleOD, fClampHeight + 20f, vecClampCenter);

        Voxels voxClampRing = voxOuterClamp - voxInnerBore;

        // --- Clamp slit: thin vertical cut ---
        Voxels voxSlit = voxBox(
            new Vector3(fClampSlit, fClampOD + 10f, fClampHeight + 10f),
            vecClampCenter);
        voxClampRing.BoolSubtract(voxSlit);

        // --- Mounting flange: plate connecting clamp to Z carriage ---
        float fFlangeX = fZPlateX;
        float fFlangeY = 20f;
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

        // Bosses on each side of the slit
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

        // --- Compose ---
        Voxels voxResult = new();
        voxResult += voxClampRing;
        voxResult += voxFlange;

        Library.Log("Spindle mount done.");
        return voxResult;
    }
}

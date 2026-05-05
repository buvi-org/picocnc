using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Z-axis assembly: back plate + Z rails + Z carriage.
    /// Mounted on the X-axis bearing blocks at center of the gantry bridge.
    /// </summary>
    public static Voxels voxConstructZAssembly()
    {
        Library.Log("Building Z-axis assembly...");

        float fMidY = fBaseOuterY / 2f;
        float fBridgeMidX = fBaseOuterX / 2f;
        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;

        // Z-axis back plate: sits on front face of gantry bridge
        float fPlateX = fZPlateX;
        float fPlateY = fZPlateY;
        float fPlateZ = fZPlateZ;

        float fBridgeYFront = fMidY - fGantryBridgeY / 2f;
        float fPlateYCenter = fBridgeYFront - fPlateY / 2f;

        Vector3 vecPlateCenter = new(fBridgeMidX, fPlateYCenter, fBridgeZ);

        Voxels voxBackPlate = voxBox(
            new Vector3(fPlateX, fPlateY, fPlateZ),
            vecPlateCenter);

        // --- Z rails: two vertical rails on the back plate front face ---
        float fZRailPlateY = fPlateYCenter - fPlateY / 2f - fZRailSize / 2f;
        float fZRailSpacingX = fZRailSpace;
        float fZRailZOffset = 0f;
        float fZRailLength = fPlateZ - 20f;

        Voxels voxZRails = new();

        for (int side = -1; side <= 1; side += 2)
        {
            float fRailX = fBridgeMidX + side * fZRailSpacingX / 2f;
            Vector3 vecRailCenter = new(fRailX, fZRailPlateY, fBridgeZ + fZRailZOffset);

            voxZRails += voxCylinderX(fZRailSize, fZRailLength, vecRailCenter);
        }

        // --- Z carriage: sliding block on Z rails, carrying the spindle mount ---
        float fCarriageX = fZPlateX - 10f;
        float fCarriageY = 30f;
        float fCarriageZ = 60f;

        Vector3 vecCarriageCenter = new(
            fBridgeMidX,
            fZRailPlateY - fCarriageY / 2f - fZRailSize / 2f,
            fBridgeZ);

        Voxels voxCarriage = voxBox(
            new Vector3(fCarriageX, fCarriageY, fCarriageZ),
            vecCarriageCenter);

        // Bolt holes through carriage into Z rails
        List<Vector3> aCarriageBolts = new();
        aCarriageBolts.Add(new Vector3(vecCarriageCenter.X - 20f, vecCarriageCenter.Y, vecCarriageCenter.Z + 15f));
        aCarriageBolts.Add(new Vector3(vecCarriageCenter.X + 20f, vecCarriageCenter.Y, vecCarriageCenter.Z + 15f));
        aCarriageBolts.Add(new Vector3(vecCarriageCenter.X - 20f, vecCarriageCenter.Y, vecCarriageCenter.Z - 15f));
        aCarriageBolts.Add(new Vector3(vecCarriageCenter.X + 20f, vecCarriageCenter.Y, vecCarriageCenter.Z - 15f));

        foreach (Vector3 vecBolt in aCarriageBolts)
        {
            voxCarriage.BoolSubtract(
                voxCylinderY(fBoltHoleDia, fCarriageY + fZRailSize + fPlateY, vecBolt));
        }

        // --- Compose ---
        Voxels voxResult = new();
        voxResult += voxBackPlate;
        voxResult += voxZRails;
        voxResult += voxCarriage;

        Library.Log("Z-axis assembly done.");
        return voxResult;
    }
}

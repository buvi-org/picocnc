using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Gantry bridge: hollow box-section beam spanning X between uprights,
    /// with internal diagonal lattice ribs for stiffness.
    /// </summary>
    public static Voxels voxConstructGantryBridge()
    {
        Library.Log("Building gantry bridge...");

        float fLeftX  = fRailInsetX;
        float fRightX = fBaseOuterX - fRailInsetX;
        float fMidY   = fBaseOuterY / 2f;

        // Bridge spans between uprights
        float fBridgeSpanX = fRightX - fLeftX - fUprightX;

        // Bridge Z center: top of uprights + bridge half-height
        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;
        float fBridgeMidX = fBaseOuterX / 2f;

        Vector3 vecBridgeCenter = new(fBridgeMidX, fMidY, fBridgeZ);

        // --- Solid beam ---
        Voxels voxSolid = voxBox(
            new Vector3(fBridgeSpanX, fGantryBridgeY, fGantryBridgeZ),
            vecBridgeCenter);

        // --- Hollow shell ---
        Voxels voxBeam = voxSolid.voxShell(-fGantryWallThick);

        // --- Internal diagonal lattice ribs ---
        Lattice latRibs = new();
        float fRibR = fRibThick / 2f;

        float fXMin = fBridgeMidX - fBridgeSpanX / 2f + fGantryWallThick;
        float fXMax = fBridgeMidX + fBridgeSpanX / 2f - fGantryWallThick;
        float fYMin = fMidY - fGantryBridgeY / 2f + fGantryWallThick;
        float fYMax = fMidY + fGantryBridgeY / 2f - fGantryWallThick;
        float fZMin = fBridgeZ - fGantryBridgeZ / 2f + fGantryWallThick;
        float fZMax = fBridgeZ + fGantryBridgeZ / 2f - fGantryWallThick;

        float fRibSpacingX = 80f;

        for (float x = fXMin; x < fXMax - fRibSpacingX / 2f; x += fRibSpacingX)
        {
            float xNext = MathF.Min(x + fRibSpacingX, fXMax);

            // Diagonal cross: front face (Y-min side)
            latRibs.AddBeam(
                new Vector3(x,     fYMin, fZMin), fRibR,
                new Vector3(xNext, fYMin, fZMax), fRibR);
            latRibs.AddBeam(
                new Vector3(x,     fYMin, fZMax), fRibR,
                new Vector3(xNext, fYMin, fZMin), fRibR);

            // Diagonal cross: back face (Y-max side)
            latRibs.AddBeam(
                new Vector3(x,     fYMax, fZMin), fRibR,
                new Vector3(xNext, fYMax, fZMax), fRibR);
            latRibs.AddBeam(
                new Vector3(x,     fYMax, fZMax), fRibR,
                new Vector3(xNext, fYMax, fZMin), fRibR);

            // Vertical struts at each rib position
            latRibs.AddBeam(
                new Vector3(x, fYMin, fZMin), fRibR,
                new Vector3(x, fYMin, fZMax), fRibR);
            latRibs.AddBeam(
                new Vector3(x, fYMax, fZMin), fRibR,
                new Vector3(x, fYMax, fZMax), fRibR);
        }

        Voxels voxRibs = new(latRibs);

        // --- End mounting bosses ---
        Voxels voxBossLeft = voxBox(
            new Vector3(fUprightX + 10f, fGantryBridgeY + 10f, fGantryBridgeZ + 10f),
            new Vector3(fLeftX, fMidY, fBridgeZ));
        Voxels voxBossRight = voxBox(
            new Vector3(fUprightX + 10f, fGantryBridgeY + 10f, fGantryBridgeZ + 10f),
            new Vector3(fRightX, fMidY, fBridgeZ));

        // --- Compose ---
        Voxels voxResult = new();
        voxResult += voxBeam;
        voxResult += voxRibs;
        voxResult += voxBossLeft;
        voxResult += voxBossRight;

        Library.Log("Gantry bridge done.");
        return voxResult;
    }
}

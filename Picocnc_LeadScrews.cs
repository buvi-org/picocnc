using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Lead screws: T12 threaded drive rods for X, Y, and Z axes.
    /// Built using Lattice beams for smooth cylindrical representation.
    /// Includes nut blocks and end bearing blocks.
    /// </summary>
    public static Voxels voxConstructLeadScrews()
    {
        Library.Log("Building lead screws...");

        float fMidY = fBaseOuterY / 2f;
        float fBridgeMidX = fBaseOuterX / 2f;
        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;

        Voxels voxAllScrews = new();

        // --- Y-axis lead screw ---
        // Runs along Y, centered between the two Y rails
        float fYScrewX = fBaseOuterX / 2f;
        float fYScrewZ = fBaseOuterZ + fRailHeight + fLeadScrewDia / 2f;

        voxAllScrews += voxConstructScrew(
            new Vector3(fYScrewX, 50f, fYScrewZ),
            new Vector3(fYScrewX, fBaseOuterY - 50f, fYScrewZ));

        // Y nut: at the gantry upright position (moving element)
        voxAllScrews += voxConstructNutBlock(
            new Vector3(fYScrewX, fMidY, fYScrewZ),
            new Vector3(0, 1, 0));

        // Y end bearings
        voxAllScrews += voxConstructEndBearing(
            new Vector3(fYScrewX, 50f, fYScrewZ), new Vector3(0, 1, 0));
        voxAllScrews += voxConstructEndBearing(
            new Vector3(fYScrewX, fBaseOuterY - 50f, fYScrewZ), new Vector3(0, 1, 0));

        // --- X-axis lead screw ---
        // Runs along X, on the gantry bridge front face
        float fXScrewY = fMidY - fGantryBridgeY / 2f - fLeadScrewDia;
        float fXScrewZ = fBridgeZ;

        voxAllScrews += voxConstructScrew(
            new Vector3(fRailInsetX + fUprightX / 2f + 20f, fXScrewY, fXScrewZ),
            new Vector3(fBaseOuterX - fRailInsetX - fUprightX / 2f - 20f, fXScrewY, fXScrewZ));

        // X nut: at center of bridge
        voxAllScrews += voxConstructNutBlock(
            new Vector3(fBridgeMidX, fXScrewY, fXScrewZ),
            new Vector3(1, 0, 0));

        // --- Z-axis lead screw ---
        // Runs along Z, on the Z-axis back plate
        float fPlateYFront = fMidY - fGantryBridgeY / 2f - fZPlateY;
        float fZScrewBotZ = fBridgeZ - fZPlateZ / 2f + 20f;
        float fZScrewTopZ = fBridgeZ + fZPlateZ / 2f - 20f;

        voxAllScrews += voxConstructScrew(
            new Vector3(fBridgeMidX, fPlateYFront - 35f, fZScrewBotZ),
            new Vector3(fBridgeMidX, fPlateYFront - 35f, fZScrewTopZ));

        // Z nut: at Z carriage height
        voxAllScrews += voxConstructNutBlock(
            new Vector3(fBridgeMidX, fPlateYFront - 35f, fBridgeZ),
            new Vector3(0, 0, 1));

        Library.Log("Lead screws done.");
        return voxAllScrews;
    }

    /// <summary>
    /// Creates a lead screw as a smooth cylindrical rod using a lattice beam.
    /// </summary>
    private static Voxels voxConstructScrew(Vector3 vecStart, Vector3 vecEnd)
    {
        float fRadius = fLeadScrewDia / 2f;
        Voxels vox = Voxels.voxLatticeBeam(vecStart, fRadius, vecEnd, fRadius);
        return vox;
    }

    /// <summary>
    /// Creates a lead nut block (anti-backlash nut).
    /// </summary>
    private static Voxels voxConstructNutBlock(Vector3 vecCenter, Vector3 vecAxis)
    {
        float fNutSize = fNutBlockSize;

        if (MathF.Abs(vecAxis.Y) > 0.9f)
            return voxBox(new Vector3(fNutSize, fNutSize * 0.6f, fNutSize), vecCenter);
        else if (MathF.Abs(vecAxis.X) > 0.9f)
            return voxBox(new Vector3(fNutSize * 0.6f, fNutSize, fNutSize), vecCenter);
        else
            return voxBox(new Vector3(fNutSize, fNutSize, fNutSize * 0.6f), vecCenter);
    }

    /// <summary>
    /// Creates a pillow block end bearing.
    /// </summary>
    private static Voxels voxConstructEndBearing(Vector3 vecCenter, Vector3 vecAxis)
    {
        float fSize = 25f;
        if (MathF.Abs(vecAxis.Y) > 0.9f)
            return voxBox(new Vector3(fSize * 0.8f, fSize, fSize), vecCenter);
        else if (MathF.Abs(vecAxis.X) > 0.9f)
            return voxBox(new Vector3(fSize, fSize * 0.8f, fSize), vecCenter);
        else
            return voxBox(new Vector3(fSize, fSize, fSize * 0.8f), vecCenter);
    }
}

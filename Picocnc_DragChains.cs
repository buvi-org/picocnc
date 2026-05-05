using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Drag chain mounts: cable management brackets and trays for
    /// routing spindle power/signal cables along Y and X axes.
    /// </summary>
    public static Voxels voxConstructDragChains()
    {
        Library.Log("Building drag chain mounts...");

        float fMidY = fBaseOuterY / 2f;
        float fBridgeMidX = fBaseOuterX / 2f;
        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;

        Voxels voxAllChains = new();

        // --- Y-axis cable tray: U-channel along the base right edge ---
        float fTrayX = fBaseOuterX - fRailInsetX + fRailWidth + 20f;
        float fTrayZ = fBaseOuterZ + 10f;
        float fTrayY = fBaseOuterY - 80f;
        float fTrayMidY = fBaseOuterY / 2f;

        // Tray bottom plate
        float fTrayFloorThick = 3f;
        Voxels voxTrayFloor = voxBox(
            new Vector3(fChainWidth + 6f, fTrayY, fTrayFloorThick),
            new Vector3(fTrayX, fTrayMidY, fTrayZ));

        // Tray side walls
        float fWallH = fChainHeight + fTrayFloorThick;
        float fWallThick = 3f;

        Voxels voxWallInner = voxBox(
            new Vector3(fWallThick, fTrayY, fWallH),
            new Vector3(fTrayX - fChainWidth / 2f, fTrayMidY, fTrayZ + fWallH / 2f - fTrayFloorThick / 2f));

        Voxels voxWallOuter = voxBox(
            new Vector3(fWallThick, fTrayY, fWallH),
            new Vector3(fTrayX + fChainWidth / 2f, fTrayMidY, fTrayZ + fWallH / 2f - fTrayFloorThick / 2f));

        voxAllChains += voxTrayFloor + voxWallInner + voxWallOuter;

        // --- X-axis cable tray: smaller U-channel on the gantry bridge top ---
        float fXTrayX = (fBaseOuterX - fRailInsetX) - fRailInsetX - fUprightX;
        float fXTrayMidX = fBridgeMidX;
        float fXTrayY = fMidY - fGantryBridgeY / 2f - fChainWidth;
        float fXTrayZ = fBridgeZ + fGantryBridgeZ / 2f + 5f;

        Voxels voxXTrayFloor = voxBox(
            new Vector3(fXTrayX, fTrayFloorThick, fChainWidth + 6f),
            new Vector3(fXTrayMidX, fXTrayY, fXTrayZ));

        Voxels voxXWallTop = voxBox(
            new Vector3(fXTrayX, fWallThick, fWallH),
            new Vector3(fXTrayMidX, fXTrayY, fXTrayZ + fChainWidth / 2f));

        Voxels voxXWallBot = voxBox(
            new Vector3(fXTrayX, fWallThick, fWallH),
            new Vector3(fXTrayMidX, fXTrayY, fXTrayZ - fChainWidth / 2f));

        voxAllChains += voxXTrayFloor + voxXWallTop + voxXWallBot;

        // --- Mounting brackets: L-shaped brackets at tray ends ---
        float fBracketThick = 5f;
        float fBracketSize = 25f;

        // Y-tray brackets (two ends)
        foreach (float fBY in new[] { 60f, fBaseOuterY - 60f })
        {
            Voxels voxBracket = voxBox(
                new Vector3(fChainWidth + 10f, fBracketThick, fBracketSize),
                new Vector3(fTrayX, fBY, fTrayZ - fBracketSize / 2f + fTrayFloorThick));
            voxAllChains += voxBracket;
        }

        Library.Log("Drag chain mounts done.");
        return voxAllChains;
    }
}

using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Drag chain mounts: cable management brackets and trays for
    /// routing spindle power/signal cables along Y and X axes.
    ///
    /// Features:
    ///   - U-channel trays (Y and X) — original
    ///   - Articulated drag chain link segments inside each tray
    ///   - Top cover plates with attachment tabs
    ///   - PG9 cable glands at tray ends
    ///   - Cable bundle placeholders
    /// </summary>
    public static Voxels voxConstructDragChains()
    {
        Log("Building drag chain mounts...");

        float fMidY         = fBaseOuterY / 2f;
        float fBridgeMidX   = fBaseOuterX / 2f;
        float fBridgeZ      = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;

        Voxels voxAllChains = new();

        // =====================================================================
        // ORIGINAL: Y-axis cable tray — U-channel along the base right edge
        // =====================================================================
        float fTrayX       = fYTrayX; // uses constraint: base edge + rail + offset
        float fTrayZ       = fBaseOuterZ + 10f;
        float fTrayY       = fBaseOuterY - 80f;
        float fTrayMidY    = fBaseOuterY / 2f;
        float fTrayFloorThick = 3f;

        Voxels voxTrayFloor = voxBox(
            new Vector3(fChainWidth + 6f, fTrayY, fTrayFloorThick),
            new Vector3(fTrayX, fTrayMidY, fTrayZ));

        float fWallH     = fChainHeight + fTrayFloorThick;
        float fWallThick = 3f;

        Voxels voxWallInner = voxBox(
            new Vector3(fWallThick, fTrayY, fWallH),
            new Vector3(fTrayX - fChainWidth / 2f, fTrayMidY,
                fTrayZ + fWallH / 2f - fTrayFloorThick / 2f));

        Voxels voxWallOuter = voxBox(
            new Vector3(fWallThick, fTrayY, fWallH),
            new Vector3(fTrayX + fChainWidth / 2f, fTrayMidY,
                fTrayZ + fWallH / 2f - fTrayFloorThick / 2f));

        voxAllChains += voxTrayFloor + voxWallInner + voxWallOuter;

        // =====================================================================
        // ORIGINAL: X-axis cable tray — U-channel on the gantry bridge top
        // =====================================================================
        float fXTrayX     = (fBaseOuterX - fRailInsetX) - fRailInsetX - fUprightX;
        float fXTrayMidX  = fBridgeMidX;
        float fXTrayZ     = fBridgeZ + fGantryBridgeZ / 2f + 5f;

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

        // =====================================================================
        // ORIGINAL: Mounting brackets — L-shaped brackets at tray ends
        // =====================================================================
        float fBracketThick = 5f;
        float fBracketSize  = 25f;

        foreach (float fBY in new[] { 60f, fBaseOuterY - 60f })
        {
            Voxels voxBracket = voxBox(
                new Vector3(fChainWidth + 10f, fBracketThick, fBracketSize),
                new Vector3(fTrayX, fBY,
                    fTrayZ - fBracketSize / 2f + fTrayFloorThick));
            voxAllChains += voxBracket;
        }

        // =====================================================================
        // PART A: Articulated chain link segments — Y-axis tray
        // =====================================================================
        {
            float fPlateThick   = 4f;
            float fLinkLength   = 15f;
            float fLinkPitch    = 17f;          // 15mm link + 2mm gap
            float fChainZ       = fTrayZ + fTrayFloorThick / 2f + fChainHeight / 2f;
            float fInnerSpace   = fChainWidth - 8f;
            float fPlateHalfSpan = fInnerSpace / 2f + fPlateThick / 2f;
            float fHalfLink     = fLinkLength / 2f;
            float fHalfHeight   = fChainHeight / 2f;
            float fPinDia       = 3f;
            float fPinLen       = 4f;

            int    nLinks   = (int)MathF.Floor(fTrayY / fLinkPitch);
            float  fYStart  = fTrayMidY - fTrayY / 2f + fHalfLink;

            Log($"  Y-axis chain: {nLinks} links");

            for (int i = 0; i < nLinks; i++)
            {
                float fLinkY = fYStart + i * fLinkPitch;
                float fLeftX = fTrayX - fPlateHalfSpan;
                float fRightX = fTrayX + fPlateHalfSpan;

                // Left side plate (YZ plane, 4mm thick in X, 15mm long in Y, 20mm tall in Z)
                voxAllChains += voxBox(
                    new Vector3(fPlateThick, fLinkLength, fChainHeight),
                    new Vector3(fLeftX, fLinkY, fChainZ));

                // Right side plate
                voxAllChains += voxBox(
                    new Vector3(fPlateThick, fLinkLength, fChainHeight),
                    new Vector3(fRightX, fLinkY, fChainZ));

                // Top crossbar (spans X between plates, 4mm thick in Z)
                voxAllChains += voxBox(
                    new Vector3(fChainWidth, fLinkLength, fPlateThick),
                    new Vector3(fTrayX, fLinkY, fChainZ + fHalfHeight - fPlateThick / 2f));

                // Bottom crossbar
                voxAllChains += voxBox(
                    new Vector3(fChainWidth, fLinkLength, fPlateThick),
                    new Vector3(fTrayX, fLinkY, fChainZ - fHalfHeight + fPlateThick / 2f));

                // Pivot pins (3mm OD, 4mm long, along Y) at each plate end
                // Left plate — front pin
                voxAllChains += voxCylinderY(fPinDia, fPinLen,
                    new Vector3(fLeftX, fLinkY - fHalfLink, fChainZ));
                // Left plate — back pin
                voxAllChains += voxCylinderY(fPinDia, fPinLen,
                    new Vector3(fLeftX, fLinkY + fHalfLink, fChainZ));
                // Right plate — front pin
                voxAllChains += voxCylinderY(fPinDia, fPinLen,
                    new Vector3(fRightX, fLinkY - fHalfLink, fChainZ));
                // Right plate — back pin
                voxAllChains += voxCylinderY(fPinDia, fPinLen,
                    new Vector3(fRightX, fLinkY + fHalfLink, fChainZ));
            }
        }

        // =====================================================================
        // PART B: Articulated chain link segments — X-axis tray
        //
        // The X tray opens toward +Y, so plates are horizontal (XY-plane)
        // with 4mm thickness in Z and fChainHeight (20mm) in Y.
        // Plates are separated in Z, matching the tray's channel width.
        // =====================================================================
        {
            float fPlateThick   = 4f;
            float fLinkLength   = 15f;
            float fLinkPitch    = 17f;
            float fInnerSpace   = fChainWidth - 8f;
            float fPlateHalfSpan = fInnerSpace / 2f + fPlateThick / 2f;
            float fChainY       = fXTrayY;
            float fChainZ       = fXTrayZ;
            float fHalfLink     = fLinkLength / 2f;
            float fHalfHeight   = fChainHeight / 2f;
            float fPinDia       = 3f;
            float fPinLen       = 4f;

            int   nLinks   = (int)MathF.Floor(fXTrayX / fLinkPitch);
            float fXStart  = fXTrayMidX - fXTrayX / 2f + fHalfLink;

            Log($"  X-axis chain: {nLinks} links");

            for (int i = 0; i < nLinks; i++)
            {
                float fLinkX  = fXStart + i * fLinkPitch;
                float fLeftZ  = fXTrayZ - fPlateHalfSpan;
                float fRightZ = fXTrayZ + fPlateHalfSpan;

                // Lower-Z side plate (XY plane, 4mm thick in Z, 15mm long in X, 20mm tall in Y)
                voxAllChains += voxBox(
                    new Vector3(fLinkLength, fChainHeight, fPlateThick),
                    new Vector3(fLinkX, fChainY, fLeftZ));

                // Upper-Z side plate
                voxAllChains += voxBox(
                    new Vector3(fLinkLength, fChainHeight, fPlateThick),
                    new Vector3(fLinkX, fChainY, fRightZ));

                // Top crossbar (spans Z between plates, 4mm thick in Y)
                voxAllChains += voxBox(
                    new Vector3(fLinkLength, fPlateThick, fChainWidth),
                    new Vector3(fLinkX, fChainY + fHalfHeight - fPlateThick / 2f, fChainZ));

                // Bottom crossbar
                voxAllChains += voxBox(
                    new Vector3(fLinkLength, fPlateThick, fChainWidth),
                    new Vector3(fLinkX, fChainY - fHalfHeight + fPlateThick / 2f, fChainZ));

                // Pivot pins (3mm OD, 4mm long, along X) at each plate end
                // Lower-Z plate — front/back pins
                voxAllChains += voxCylinderX(fPinDia, fPinLen,
                    new Vector3(fLinkX - fHalfLink, fChainY, fLeftZ));
                voxAllChains += voxCylinderX(fPinDia, fPinLen,
                    new Vector3(fLinkX + fHalfLink, fChainY, fLeftZ));
                // Upper-Z plate — front/back pins
                voxAllChains += voxCylinderX(fPinDia, fPinLen,
                    new Vector3(fLinkX - fHalfLink, fChainY, fRightZ));
                voxAllChains += voxCylinderX(fPinDia, fPinLen,
                    new Vector3(fLinkX + fHalfLink, fChainY, fRightZ));
            }
        }

        // =====================================================================
        // PART C: Top cover plates for the trays
        // =====================================================================
        {
            float fCovThick = 2f;
            float fCovHalf  = fCovThick / 2f;

            // --- Y-tray cover ---
            float fYCovZ = fTrayZ + fWallH - fTrayFloorThick / 2f + fCovHalf;
            voxAllChains += voxBox(
                new Vector3(fChainWidth + 6f, fTrayY, fCovThick),
                new Vector3(fTrayX, fTrayMidY, fYCovZ));

            // --- Y-tray cover attachment tabs (5x5x2mm, every 100mm) ---
            {
                float fTabW   = 5f;
                float fCovW   = fChainWidth + 6f;
                int   nYTabs  = (int)MathF.Floor((fTrayY - 50f) / 100f) + 1;
                float fTabY0  = fTrayMidY - fTrayY / 2f + 50f;

                for (int i = 0; i < nYTabs; i++)
                {
                    float fTabY = fTabY0 + i * 100f;
                    // Left-edge tab
                    voxAllChains += voxBox(
                        new Vector3(fTabW, fTabW, fCovThick),
                        new Vector3(fTrayX - fCovW / 2f, fTabY, fYCovZ));
                    // Right-edge tab
                    voxAllChains += voxBox(
                        new Vector3(fTabW, fTabW, fCovThick),
                        new Vector3(fTrayX + fCovW / 2f, fTabY, fYCovZ));
                }
            }

            // --- X-tray cover ---
            // X tray opens toward +Y; cover is a thin plate on the front face
            float fXCovY = fXTrayY + fWallThick / 2f + fCovHalf;
            float fXCovZ = fXTrayZ;
            voxAllChains += voxBox(
                new Vector3(fXTrayX, fCovThick, fChainWidth + 6f),
                new Vector3(fXTrayMidX, fXCovY, fXCovZ));

            // --- X-tray cover attachment tabs (5x5x2mm, every 100mm) ---
            {
                float fTabW   = 5f;
                float fCovW   = fChainWidth + 6f;
                int   nXTabs  = (int)MathF.Floor((fXTrayX - 50f) / 100f) + 1;
                float fTabX0  = fXTrayMidX - fXTrayX / 2f + 50f;

                for (int i = 0; i < nXTabs; i++)
                {
                    float fTabX = fTabX0 + i * 100f;
                    // Lower-Z edge tab
                    voxAllChains += voxBox(
                        new Vector3(fTabW, fCovThick, fTabW),
                        new Vector3(fTabX, fXCovY, fXCovZ - fCovW / 2f));
                    // Upper-Z edge tab
                    voxAllChains += voxBox(
                        new Vector3(fTabW, fCovThick, fTabW),
                        new Vector3(fTabX, fXCovY, fXCovZ + fCovW / 2f));
                }
            }
        }

        // =====================================================================
        // PART D: Cable glands (PG9 style) at drag chain ends
        // =====================================================================
        {
            // Gland dimensions
            float fBodyDia    = 15f;   // threaded body OD
            float fBodyLen    = 20f;   // threaded body length
            float fFlangeDia  = 22f;   // hex flange OD
            float fFlangeLen  = 5f;    // flange thickness
            float fNutDia     = 18f;   // compression nut OD
            float fNutLen     = 8f;    // compression nut length
            float fGlandZ     = fTrayZ + fTrayFloorThick / 2f + fChainHeight / 2f;

            // --- Y-axis tray glands ---
            float fYGlandFrontY = fTrayMidY - fTrayY / 2f;  // Y = 40
            float fYGlandBackY  = fTrayMidY + fTrayY / 2f;  // Y = 460

            foreach (float fGY in new[] { fYGlandFrontY, fYGlandBackY })
            {
                Vector3 vPos = new(fTrayX, fGY, fGlandZ);

                // Threaded body: 15mm OD, 20mm long along Y
                voxAllChains += voxCylinderY(fBodyDia, fBodyLen, vPos);

                // Hex flange: 22mm OD, 5mm thick along Y
                voxAllChains += voxCylinderY(fFlangeDia, fFlangeLen, vPos);

                // Compression nut: 18mm OD, 8mm long, on the outside
                float fNutSign = (fGY < fTrayMidY) ? -1f : 1f;
                float fNutY    = fGY + fNutSign * (fBodyLen / 2f - fNutLen / 2f);
                voxAllChains += voxCylinderY(fNutDia, fNutLen,
                    new Vector3(fTrayX, fNutY, fGlandZ));
            }

            // --- X-axis tray glands ---
            float fXGlandFrontX = fXTrayMidX - fXTrayX / 2f;  // X = 50
            float fXGlandBackX  = fXTrayMidX + fXTrayX / 2f;  // X = 550
            float fXGlandY      = fXTrayY;
            float fXGlandZ      = fXTrayZ;

            foreach (float fGX in new[] { fXGlandFrontX, fXGlandBackX })
            {
                Vector3 vPos = new(fGX, fXGlandY, fXGlandZ);

                // Threaded body: 15mm OD, 20mm long along X
                voxAllChains += voxCylinderX(fBodyDia, fBodyLen, vPos);

                // Hex flange: 22mm OD, 5mm thick along X
                voxAllChains += voxCylinderX(fFlangeDia, fFlangeLen, vPos);

                // Compression nut: 18mm OD, 8mm long, on the outside
                float fNutSign = (fGX < fXTrayMidX) ? -1f : 1f;
                float fNutX    = fGX + fNutSign * (fBodyLen / 2f - fNutLen / 2f);
                voxAllChains += voxCylinderX(fNutDia, fNutLen,
                    new Vector3(fNutX, fXGlandY, fXGlandZ));
            }
        }

        // =====================================================================
        // PART E: Cable bundle placeholder — 3 parallel 4mm-OD cylinders
        // =====================================================================
        {
            float fCableDia   = 4f;
            float fCableSpace = 4.5f;  // center-to-center spacing

            // --- Y-axis cable bundle ---
            {
                float fCYStart = fTrayMidY - fTrayY / 2f;
                float fCYEnd   = fTrayMidY + fTrayY / 2f;
                float fCYZ     = fTrayZ + fTrayFloorThick / 2f + fChainHeight / 2f;

                voxAllChains += voxCylinder(
                    new Vector3(fTrayX - fCableSpace, fCYStart, fCYZ),
                    new Vector3(fTrayX - fCableSpace, fCYEnd, fCYZ),
                    fCableDia);

                voxAllChains += voxCylinder(
                    new Vector3(fTrayX, fCYStart, fCYZ),
                    new Vector3(fTrayX, fCYEnd, fCYZ),
                    fCableDia);

                voxAllChains += voxCylinder(
                    new Vector3(fTrayX + fCableSpace, fCYStart, fCYZ),
                    new Vector3(fTrayX + fCableSpace, fCYEnd, fCYZ),
                    fCableDia);
            }

            // --- X-axis cable bundle ---
            {
                float fCXStart = fXTrayMidX - fXTrayX / 2f;
                float fCXEnd   = fXTrayMidX + fXTrayX / 2f;
                float fCXY     = fXTrayY;
                float fCXZ     = fXTrayZ;

                voxAllChains += voxCylinder(
                    new Vector3(fCXStart, fCXY, fCXZ - fCableSpace),
                    new Vector3(fCXEnd,   fCXY, fCXZ - fCableSpace),
                    fCableDia);

                voxAllChains += voxCylinder(
                    new Vector3(fCXStart, fCXY, fCXZ),
                    new Vector3(fCXEnd,   fCXY, fCXZ),
                    fCableDia);

                voxAllChains += voxCylinder(
                    new Vector3(fCXStart, fCXY, fCXZ + fCableSpace),
                    new Vector3(fCXEnd,   fCXY, fCXZ + fCableSpace),
                    fCableDia);
            }
        }

        Log("Drag chain mounts done.");
        return voxAllChains;
    }
}

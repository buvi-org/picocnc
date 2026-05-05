using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Lead screws: T12 threaded drive rods for X, Y, and Z axes.
    /// Includes thread approximation rings, anti-backlash nut blocks,
    /// end bearing blocks, and shaft collars.
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

        Vector3 vecYScrewStart = new Vector3(fYScrewX, 50f, fYScrewZ);
        Vector3 vecYScrewEnd   = new Vector3(fYScrewX, fBaseOuterY - 50f, fYScrewZ);

        voxAllScrews += voxConstructScrew(vecYScrewStart, vecYScrewEnd);

        // Y nut: at the gantry upright position (moving element)
        voxAllScrews += voxConstructNutBlock(
            new Vector3(fYScrewX, fMidY, fYScrewZ),
            new Vector3(0, 1, 0));

        // Y shaft collars at each end bearing
        voxAllScrews += voxShaftCollar(vecYScrewStart, new Vector3(0, 1, 0));
        voxAllScrews += voxShaftCollar(vecYScrewEnd,   new Vector3(0, 1, 0));

        // Y end bearings
        voxAllScrews += voxConstructEndBearing(vecYScrewStart, new Vector3(0, 1, 0));
        voxAllScrews += voxConstructEndBearing(vecYScrewEnd,   new Vector3(0, 1, 0));

        // --- X-axis lead screw ---
        // Runs along X, on the gantry bridge front face
        float fXScrewY = fMidY - fGantryBridgeY / 2f - fLeadScrewDia;
        float fXScrewZ = fBridgeZ;

        Vector3 vecXScrewStart = new Vector3(fRailInsetX + fUprightX / 2f + 20f, fXScrewY, fXScrewZ);
        Vector3 vecXScrewEnd   = new Vector3(fBaseOuterX - fRailInsetX - fUprightX / 2f - 20f, fXScrewY, fXScrewZ);

        voxAllScrews += voxConstructScrew(vecXScrewStart, vecXScrewEnd);

        // X nut: at center of bridge
        voxAllScrews += voxConstructNutBlock(
            new Vector3(fBridgeMidX, fXScrewY, fXScrewZ),
            new Vector3(1, 0, 0));

        // X shaft collars at each end
        voxAllScrews += voxShaftCollar(vecXScrewStart, new Vector3(1, 0, 0));
        voxAllScrews += voxShaftCollar(vecXScrewEnd,   new Vector3(1, 0, 0));

        // --- Z-axis lead screw ---
        // Runs along Z, on the Z-axis back plate
        float fPlateYFront = fMidY - fGantryBridgeY / 2f - fZPlateY;
        float fZScrewBotZ = fBridgeZ - fZPlateZ / 2f + 20f;
        float fZScrewTopZ = fBridgeZ + fZPlateZ / 2f - 20f;

        Vector3 vecZScrewStart = new Vector3(fBridgeMidX, fPlateYFront - 35f, fZScrewBotZ);
        Vector3 vecZScrewEnd   = new Vector3(fBridgeMidX, fPlateYFront - 35f, fZScrewTopZ);

        voxAllScrews += voxConstructScrew(vecZScrewStart, vecZScrewEnd);

        // Z nut: at Z carriage height
        voxAllScrews += voxConstructNutBlock(
            new Vector3(fBridgeMidX, fPlateYFront - 35f, fBridgeZ),
            new Vector3(0, 0, 1));

        // Z shaft collars at each end
        voxAllScrews += voxShaftCollar(vecZScrewStart, new Vector3(0, 0, 1));
        voxAllScrews += voxShaftCollar(vecZScrewEnd,   new Vector3(0, 0, 1));

        Library.Log("Lead screws done.");
        return voxAllScrews;
    }

    /// <summary>
    /// Creates a lead screw as a cylindrical rod with thread-approximation
    /// annular rings spaced every 4 mm, offset alternatingly to suggest
    /// a spiral profile.
    /// </summary>
    private static Voxels voxConstructScrew(Vector3 vecStart, Vector3 vecEnd)
    {
        float fRadius   = fLeadScrewDia / 2f;          // 6 mm
        float fRingOD   = 14f;                          // ring outer diameter
        float fRingHalf = 1f;                           // ring half-thickness (2 mm / 2)

        // Smooth core
        Voxels vox = Voxels.voxLatticeBeam(vecStart, fRadius, vecEnd, fRadius);

        // Screw axis info
        Vector3 vecAxis = Vector3.Normalize(vecEnd - vecStart);
        float   fTotalLen = Vector3.Distance(vecStart, vecEnd);

        // Two perpendicular axes for ring offset pattern
        Vector3 vecPerp1, vecPerp2;
        if (MathF.Abs(vecAxis.Y) > 0.9f)
        {
            vecPerp1 = new Vector3(1, 0, 0);   // X
            vecPerp2 = new Vector3(0, 0, 1);   // Z
        }
        else if (MathF.Abs(vecAxis.X) > 0.9f)
        {
            vecPerp1 = new Vector3(0, 1, 0);   // Y
            vecPerp2 = new Vector3(0, 0, 1);   // Z
        }
        else
        {
            vecPerp1 = new Vector3(1, 0, 0);   // X
            vecPerp2 = new Vector3(0, 1, 0);   // Y
        }

        // Place annular rings every 4 mm along the screw
        float fRingSpacing = 4f;
        int   nRingIndex   = 0;

        for (float d = fRingSpacing / 2f; d < fTotalLen - fRingSpacing / 4f; d += fRingSpacing)
        {
            // Alternating offset pattern (repeat every 4 rings)
            float fOffset = 0.5f;
            Vector3 vecOffset = (nRingIndex % 4) switch
            {
                0 =>  vecPerp1 * fOffset,
                1 =>  vecPerp2 * fOffset,
                2 => -vecPerp1 * fOffset,
                3 => -vecPerp2 * fOffset,
                _ => Vector3.Zero
            };

            Vector3 vecRingCenter = vecStart + vecAxis * d + vecOffset;
            Vector3 vecRingStart  = vecRingCenter - vecAxis * fRingHalf;
            Vector3 vecRingEnd    = vecRingCenter + vecAxis * fRingHalf;

            vox += voxCylinder(vecRingStart, vecRingEnd, fRingOD);
            nRingIndex++;
        }

        return vox;
    }

    /// <summary>
    /// Creates an anti-backlash split lead nut block.
    /// The nut is split in half perpendicular to the screw axis,
    /// with a spring between the halves to eliminate backlash.
    /// </summary>
    private static Voxels voxConstructNutBlock(Vector3 vecCenter, Vector3 vecAxis)
    {
        float fNutSize  = fNutBlockSize;   // 25 mm
        float fHalfNut  = fNutSize / 2f;   // 12.5 mm
        float fMargin   = 5f;              // oversize to ensure clean boolean cuts
        float fOverSize = fNutSize + fMargin * 2f;

        // 1. Main cube
        Voxels vox;
        if (MathF.Abs(vecAxis.Y) > 0.9f)
            vox = voxBox(new Vector3(fNutSize, fNutSize, fNutSize), vecCenter);
        else if (MathF.Abs(vecAxis.X) > 0.9f)
            vox = voxBox(new Vector3(fNutSize, fNutSize, fNutSize), vecCenter);
        else
            vox = voxBox(new Vector3(fNutSize, fNutSize, fNutSize), vecCenter);

        // 2. Central screw bore (through-hole along vecAxis)
        Vector3 vecBoreFwd = vecCenter + vecAxis * (fHalfNut + fMargin);
        Vector3 vecBoreBwd = vecCenter - vecAxis * (fHalfNut + fMargin);
        vox.BoolSubtract(voxCylinder(vecBoreBwd, vecBoreFwd, fLeadScrewDia));

        // 3. Split gap — thin box (1 mm wide) perpendicular to vecAxis
        Voxels voxSplit;
        if (MathF.Abs(vecAxis.Y) > 0.9f)
            voxSplit = voxBox(new Vector3(fOverSize, 1f, fOverSize), vecCenter);
        else if (MathF.Abs(vecAxis.X) > 0.9f)
            voxSplit = voxBox(new Vector3(1f, fOverSize, fOverSize), vecCenter);
        else
            voxSplit = voxBox(new Vector3(fOverSize, fOverSize, 1f), vecCenter);
        vox.BoolSubtract(voxSplit);

        // 4. Perpendicular offset direction (for spring pocket + bolt holes)
        Vector3 vecPerp1 = MathF.Abs(vecAxis.Y) > 0.9f ? new Vector3(1, 0, 0)
                         : MathF.Abs(vecAxis.X) > 0.9f ? new Vector3(0, 1, 0)
                         :                               new Vector3(1, 0, 0);

        // 5. Spring pocket: cylinder 10 mm OD  x  15 mm deep on one side of the split
        float fPocketDepth = 15f;
        Vector3 vecPocketPos   = vecCenter + vecPerp1 * 8f;      // 8 mm radial offset
        Vector3 vecPocketStart = vecPocketPos + vecAxis * 1f;    // just past the split gap
        Vector3 vecPocketEnd   = vecPocketPos + vecAxis * (1f + fPocketDepth);
        vox.BoolSubtract(voxCylinder(vecPocketStart, vecPocketEnd, 10f));

        // 6. Spring inside the pocket: cylinder 8 mm OD  x  12 mm long
        float fSpringLen = 12f;
        Vector3 vecSpringStart = vecPocketPos + vecAxis * 2f;
        Vector3 vecSpringEnd   = vecPocketPos + vecAxis * (2f + fSpringLen);
        vox += voxCylinder(vecSpringStart, vecSpringEnd, 8f);

        // 7. Two M4 bolt holes (4.2 mm dia) perpendicular to the split
        //    (i.e. parallel to vecAxis), passing through both nut halves
        Vector3 vecBoltPos1 = vecCenter + vecPerp1 * 10f;
        Vector3 vecBoltPos2 = vecCenter - vecPerp1 * 10f;

        Vector3 vecBolt1Fwd = vecBoltPos1 + vecAxis * (fHalfNut + fMargin);
        Vector3 vecBolt1Bwd = vecBoltPos1 - vecAxis * (fHalfNut + fMargin);
        vox.BoolSubtract(voxCylinder(vecBolt1Bwd, vecBolt1Fwd, 4.2f));

        Vector3 vecBolt2Fwd = vecBoltPos2 + vecAxis * (fHalfNut + fMargin);
        Vector3 vecBolt2Bwd = vecBoltPos2 - vecAxis * (fHalfNut + fMargin);
        vox.BoolSubtract(voxCylinder(vecBolt2Bwd, vecBolt2Fwd, 4.2f));

        return vox;
    }

    /// <summary>
    /// Creates a shaft collar ring (18 mm OD, 8 mm wide) around a screw
    /// at the given center position.  Used at end bearing locations.
    /// </summary>
    private static Voxels voxShaftCollar(Vector3 vecCenter, Vector3 vecAxis)
    {
        float fWidth = 8f;
        float fHalf  = fWidth / 2f;

        Vector3 vecCollarStart = vecCenter - vecAxis * fHalf;
        Vector3 vecCollarEnd   = vecCenter + vecAxis * fHalf;

        return voxCylinder(vecCollarStart, vecCollarEnd, 18f);
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

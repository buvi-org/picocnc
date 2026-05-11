namespace PicoGK;

public static partial class Picocnc
{
    // === Machine centerlines ===
    public static float fMidX => fBaseOuterX / 2f;
    public static float fMidY => fBaseOuterY / 2f;

    // === Y rail positions ===
    public static float fRailXLeft  => fRailInsetX;
    public static float fRailXRight => fBaseOuterX - fRailInsetX;

    // === Upright positions ===
    public static float fUprightBaseZ => fBaseOuterZ + fRailHeight;
    public static float fUprightTopZ   => fBaseOuterZ + fRailHeight + fUprightZ;
    public static float fUprightMidZ   => fBaseOuterZ + fRailHeight + fUprightZ / 2f;

    // === Gantry bridge positions ===
    public static float fBridgeZ       => fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;
    public static float fBridgeTopZ    => fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ;
    public static float fBridgeBottomZ => fBaseOuterZ + fRailHeight + fUprightZ;
    public static float fBridgeYFront  => fBaseOuterY / 2f - fGantryBridgeY / 2f;
    public static float fBridgeYBack   => fBaseOuterY / 2f + fGantryBridgeY / 2f;
    public static float fBridgeSpanX   => (fBaseOuterX - fRailInsetX) - fRailInsetX - fUprightX;

    // === Work bed positions ===
    public static float fTableZCenter => fBaseOuterZ + fTableThick / 2f;
    public static float fTableZTop    => fBaseOuterZ + fTableThick;

    // === Z plate positions ===
    public static float fZPlateBackY   => fBridgeYFront - fZPlateY;   // back face of Z plate
    public static float fZPlateCenterY => fBridgeYFront - fZPlateY / 2f;
    public static float fZPlateFrontY  => fBridgeYFront;               // front face = bridge face

    // === Z rail positions ===
    public static float fZRailCenterY => fZPlateCenterY - fZPlateY / 2f - fZRailSize / 2f;

    // === Z carriage positions ===
    public static float fCarriageX       => fZPlateX - 10f;   // 10mm narrower than plate
    public static float fCarriageY       => 30f;               // carriage depth
    public static float fCarriageZ       => 60f;               // carriage height
    public static float fCarriageCenterY => fZRailCenterY - fCarriageY / 2f - fZRailSize / 2f;
    public static float fCarriageFrontY  => fCarriageCenterY - fCarriageY / 2f;

    // === Spindle positions (relative to carriage) ===
    public static float fSpindleClampY => fCarriageFrontY - 40f;   // 40mm gap from carriage
    public static float fSpindleClampZ => fBridgeZ - fCarriageZ / 2f;

    // === Motor positions ===
    public static float fYMotorY => fBaseOuterY - 20f;  // back from table edge (Y max 470)
    public static float fXMotorX => fRailInsetX + fUprightX + 30f;
    public static float fZMotorZ => fBridgeZ + fZPlateZ / 2f - 20f;
    public static float fZMotorY => fBridgeYFront - fZPlateY + fNema23Width / 2f;

    // === Lead screw positions ===
    public static float fYScrewStartY => 65f;
    public static float fYScrewEndY   => fBaseOuterY - 65f;
    // Y screw Z: positioned above workbed + spoil board.
    // Thread rings are 14mm OD (7mm radius), larger than the 6mm nominal screw radius.
    // Spoil board adds 18mm on top of table. 15mm clearance from ring bottom to spoil board top.
    public static float fYScrewZ      => fBaseOuterZ + fTableThick + 18f + 15f + 7f;
    public static float fXScrewStartX => fRailInsetX + fUprightX / 2f + 30f;
    public static float fXScrewEndX   => fBaseOuterX - fRailInsetX - fUprightX / 2f - 30f;
    public static float fXScrewZ      => fBridgeZ;
    public static float fZScrewY      => fZPlateFrontY - 28f;       // Y=192: behind flange (Ymax~174), ahead of Z plate (Ymin=205)
    public static float fZScrewBotZ   => fBridgeZ - fZPlateZ / 2f + 20f;
    public static float fZScrewTopZ   => fBridgeZ + fZPlateZ / 2f - 20f;

    // === Drag chain positions ===
    public static float fYTrayZ         => fBaseOuterZ + 10f;
    public static float fYTrayX         => fBaseOuterX - fRailInsetX + fRailWidth + 25f;
    public static float fXTrayZ         => fBridgeTopZ + 5f;
    public static float fXTrayY         => fMidY - fGantryBridgeY / 2f - fChainWidth - 50f; // Y=140: clears Z carriage (Ymin=160) + spindle jacket (Ymax=165)
    public static float fChainFloorThick => 3f;
    public static float fChainWallThick  => 3f;
    public static float fChainTrayWidth  => fChainWidth + 6f;
    public static float fChainWallH      => fChainHeight + 3f;

    // === X rail positions ===
    public static float fXRailSize      => 15f;
    public static float fXRailUpperZ    => fBridgeZ + fGantryBridgeZ / 2f - fXRailSize;
    public static float fXRailLowerZ    => fBridgeZ - fGantryBridgeZ / 2f + fXRailSize;
    public static float fXBearingSize   => 40f;
    public static float fXBearingY      => fXRailSize + 10f;
    public static float fXBearingZ      => fXRailSize + 10f;

    // === Y rail positions ===
    public static float fYRailZ       => fBaseOuterZ + fRailHeight / 2f;
    public static float fYBearingSize => 40f;
    public static float fYBearingH    => fRailHeight + 15f;
    public static float fYBearingX    => fRailWidth + 10f;

    // === Spindle mount details ===
    public static float fFlangeY        => 20f;
    public static float fFlangeZ        => 80f;
    public static float fClampBossDia   => 14f;
    public static float fClampBossDepth => 20f;

    // === T-slot details ===
    public static float fSlotInset   => 40f;
    public static float fSlotUpperH  => fTSlotDepth * 0.4f;
    public static float fSlotLowerH  => fTSlotDepth * 0.6f;
    public static float fTableOverhang => 40f;

    // === Motor mount details ===
    public static float fStandoffH => 15f;
    public static float fStandoffR => 4f;

    // === Gantry details ===
    public static float fGussetSize          => 40f;
    public static float fUprightPlateThick    => 12f;
    public static float fUprightPlateOverhang => 20f;
    public static float fUprightBoltCircle    => 30f;
    public static float fBridgeEndBossOverhang => 10f;
    public static float fBridgeRibSpacingX    => 80f;

    // === Drag chain bracket details ===
    public static float fBracketThick => 5f;
    public static float fBracketSize  => 25f;
}

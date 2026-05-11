using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    // =========================================================================
    // Component dimensions
    // =========================================================================
    // Microswitch body: 20mm along rail x 10mm deep (normal) x 6mm tall (up)
    const float fSwLong  = 20f;   // along rail/travel direction
    const float fSwDeep  = 10f;   // along mount-surface normal
    const float fSwTall  = 6f;    // along bracket-up

    // Roller lever: extends from body along the rail/travel direction
    const float fLvLong  = 15f;
    const float fLvWide  = 3f;
    const float fLvThick = 1.5f;

    // L-bracket vertical plate (against mount surface):
    //   thin (2mm) along normal, 8mm along up, 20mm along rail
    const float fBraVertThick  = 2f;   // along normal
    const float fBraVertUp     = 8f;   // along up
    const float fBraVertAlong  = 20f;  // along rail

    // L-bracket shelf (extends out from surface):
    //   8mm along normal, 2mm along up, 20mm along rail
    const float fBraShelfDeep  = 8f;   // along normal
    const float fBraShelfThick = 2f;   // along up
    const float fBraShelfAlong = 20f;  // along rail

    // M3 bolt clearance
    const float fM3Clear = 3.2f;

    // Hard-stop bumper: 15x15x10mm block (10mm extends from surface)
    const float fBumpFace  = 15f;
    const float fBumpDeep  = 10f;  // along normal

    // E-stop dimensions
    const float fEstopPlateW   = 40f;
    const float fEstopPlateThk = 3f;
    const float fEstopPlateH   = 40f;
    const float fEstopBtnOD    = 40f;
    const float fEstopBtnLen   = 20f;
    const float fEstopColOD    = 44f;
    const float fEstopColLen   = 5f;

    // =========================================================================
    // Helper: oriented box via manual mesh construction.
    //
    // (fN, fU, fA) = extents along (vecNormal, vecUp, vecAlong).
    // vecAlong is derived as Cross(vecNormal, vecUp).
    // =========================================================================
    static Voxels voxOrientedBox(
        float fN, float fU, float fA,
        Vector3 vecCenter,
        Vector3 vecNormal, Vector3 vecUp)
    {
        Vector3 vecAlong = Vector3.Cross(vecNormal, vecUp);

        float hn = fN / 2f;
        float hu = fU / 2f;
        float ha = fA / 2f;

        // 8 corners indexed as: N,U,A = (+/-hn, +/-hu, +/-ha)
        Vector3[] c = new Vector3[8];
        int i = 0;
        for (int sn = -1; sn <= 1; sn += 2)
            for (int su = -1; su <= 1; su += 2)
                for (int sa = -1; sa <= 1; sa += 2)
                    c[i++] = vecCenter
                        + vecNormal * (sn * hn)
                        + vecUp     * (su * hu)
                        + vecAlong  * (sa * ha);

        Mesh msh = new();

        // 6 faces, 2 triangles each.
        // Corner layout (sn,su,sa):
        //   0:(-,-,-) 1:(-,-,+) 2:(-,+,-) 3:(-,+,+)
        //   4:(+,-,-) 5:(+,-,+) 6:(+,+,-) 7:(+,+,+)
        //
        // Each quad listed in CCW order when viewed from outside (outward normal).

        // -N face:  (0,1,3,2)
        msh.nAddTriangle(c[0], c[1], c[3]);
        msh.nAddTriangle(c[0], c[3], c[2]);

        // +N face:  (4,6,7,5)
        msh.nAddTriangle(c[4], c[6], c[7]);
        msh.nAddTriangle(c[4], c[7], c[5]);

        // -U face:  (0,4,5,1)
        msh.nAddTriangle(c[0], c[4], c[5]);
        msh.nAddTriangle(c[0], c[5], c[1]);

        // +U face:  (2,3,7,6)
        msh.nAddTriangle(c[2], c[3], c[7]);
        msh.nAddTriangle(c[2], c[7], c[6]);

        // -A face:  (0,2,6,4)
        msh.nAddTriangle(c[0], c[2], c[6]);
        msh.nAddTriangle(c[0], c[6], c[4]);

        // +A face:  (1,5,7,3)
        msh.nAddTriangle(c[1], c[5], c[7]);
        msh.nAddTriangle(c[1], c[7], c[3]);

        return new Voxels(msh);
    }

    // =========================================================================
    // Helper: single limit-switch assembly.
    //
    // vecMountPos  — point on the machine surface where the bracket bolts on
    // vecNormal    — unit normal pointing outward from the mount surface
    // vecAlong     — unit vector along the rail / axis of travel
    //                (switch body's long dimension; lever extends this way)
    // vecUp        — unit vector defining "up" for the bracket
    // fLeverSign   — +1 or -1 : which direction along vecAlong the lever extends
    // =========================================================================
    static Voxels voxLimitSwitch(
        Vector3 vecMountPos,
        Vector3 vecNormal,
        Vector3 vecAlong,
        Vector3 vecUp,
        float fLeverSign)
    {
        Voxels vox = new();

        float fVertHalfN = fBraVertThick / 2f;  // 1.0
        float fVertHalfU = fBraVertUp    / 2f;  // 4.0
        float fVertHalfA = fBraVertAlong / 2f;  // 10.0

        // --- Vertical bracket plate (flush against mount surface) ---
        // Offset outward by half its thickness along normal
        Vector3 vecVertCenter = vecMountPos + vecNormal * fVertHalfN;

        Voxels voxVert = voxOrientedBox(
            fBraVertThick, fBraVertUp, fBraVertAlong,
            vecVertCenter, vecNormal, vecUp);

        // Two M3 bolt holes through the vertical plate, spaced along vecAlong
        float fBoltHoleDepth = fBraVertThick + 10f;
        float fBoltSpacing   = fBraVertAlong * 0.55f;   // ~11 mm apart
        for (float d = -fBoltSpacing / 2f; d <= fBoltSpacing / 2f; d += fBoltSpacing)
        {
            Vector3 vecHoleCenter = vecVertCenter + vecAlong * d;
            Vector3 vecHoleStart  = vecHoleCenter - vecNormal * (fBoltHoleDepth / 2f);
            Vector3 vecHoleEnd    = vecHoleCenter + vecNormal * (fBoltHoleDepth / 2f);
            voxVert.BoolSubtract(voxCylinder(vecHoleStart, vecHoleEnd, fM3Clear));
        }

        vox += voxVert;

        // --- Shelf (extends outward along vecNormal from the bottom of vertical plate) ---
        float fShfHalfN = fBraShelfDeep  / 2f;  // 4.0
        float fShfHalfU = fBraShelfThick / 2f;  // 1.0
        float fShfHalfA = fBraShelfAlong / 2f;  // 10.0

        Vector3 vecShelfCenter = vecMountPos
            + vecNormal * (fBraVertThick + fShfHalfN)
            + vecUp     * (-fVertHalfU + fShfHalfU);

        vox += voxOrientedBox(
            fBraShelfDeep, fBraShelfThick, fBraShelfAlong,
            vecShelfCenter, vecNormal, vecUp);

        // --- Switch body (sits on shelf) ---
        float fBodyHalfN = fSwDeep / 2f;  // 5.0
        float fBodyHalfU = fSwTall / 2f;  // 3.0
        float fBodyHalfA = fSwLong / 2f;  // 10.0

        Vector3 vecBodyCenter = vecShelfCenter
            + vecUp     * (fShfHalfU + fBodyHalfU)
            + vecNormal * (fBodyHalfN - fShfHalfN);

        vox += voxOrientedBox(
            fSwDeep, fSwTall, fSwLong,
            vecBodyCenter, vecNormal, vecUp);

        // --- Roller lever (extends from one end of the body along vecAlong) ---
        float fLvHalfN = fLvWide  / 2f;  // 1.5
        float fLvHalfU = fLvThick / 2f;  // 0.75
        float fLvHalfA = fLvLong  / 2f;  // 7.5

        Vector3 vecLeverCenter = vecBodyCenter
            + vecAlong * (fLeverSign * (fBodyHalfA + fLvHalfA));

        vox += voxOrientedBox(
            fLvWide, fLvThick, fLvLong,
            vecLeverCenter, vecNormal, vecUp);

        return vox;
    }

    // =========================================================================
    // Helper: hard-stop bumper (simple block extending from surface)
    // =========================================================================
    static Voxels voxBumper(Vector3 vecMountPos, Vector3 vecNormal, Vector3 vecUp)
    {
        Vector3 vecCenter = vecMountPos + vecNormal * (fBumpDeep / 2f);
        return voxOrientedBox(
            fBumpDeep, fBumpFace, fBumpFace,
            vecCenter, vecNormal, vecUp);
    }

    // =========================================================================
    // Helper: E-stop button (bracket plate + yellow collar + red mushroom)
    // =========================================================================
    static Voxels voxEStop(Vector3 vecBracketCenter, Vector3 vecForward)
    {
        Voxels vox = new();

        // vecForward points from bracket into the button (away from machine)
        Vector3 vecUp    = new(0, 0, 1);
        Vector3 vecRight = Vector3.Cross(vecForward, vecUp);

        // Mounting plate
        Voxels voxPlate = voxOrientedBox(
            fEstopPlateThk, fEstopPlateH, fEstopPlateW,
            vecBracketCenter, vecForward, vecUp);

        // 4 bolt holes in plate corners
        float fCornerOff  = fEstopPlateW * 0.35f;
        float fBoltDepth  = fEstopPlateThk + 10f;
        for (float sx = -1f; sx <= 1f; sx += 2f)
            for (float sz = -1f; sz <= 1f; sz += 2f)
            {
                Vector3 vecBolt = vecBracketCenter
                    + vecRight * (sx * fCornerOff)
                    + vecUp    * (sz * fCornerOff);
                voxPlate.BoolSubtract(voxCylinder(
                    vecBolt - vecForward * (fBoltDepth / 2f),
                    vecBolt + vecForward * (fBoltDepth / 2f),
                    fM3Clear));
            }

        vox += voxPlate;

        // Yellow collar
        float fCollarStart = fEstopPlateThk / 2f;
        Voxels voxCollar = voxCylinder(
            vecBracketCenter + vecForward * fCollarStart,
            vecBracketCenter + vecForward * (fCollarStart + fEstopColLen),
            fEstopColOD);
        vox += voxCollar;

        // Red mushroom head button
        float fBtnStart = fEstopPlateThk / 2f + fEstopColLen;
        Voxels voxButton = voxCylinder(
            vecBracketCenter + vecForward * fBtnStart,
            vecBracketCenter + vecForward * (fBtnStart + fEstopBtnLen),
            fEstopBtnOD);
        vox += voxButton;

        return vox;
    }

    // =========================================================================
    // Public: voxConstructSafety()
    //   Builds all limit switches, hard end stops, and E-stop.
    // =========================================================================
    public static Voxels voxConstructSafety()
    {
        Log("Building safety components...");

        Voxels voxSafety = new();

        // ---- Reusable direction vectors ----
        Vector3 vX = new(1, 0, 0);
        Vector3 vY = new(0, 1, 0);
        Vector3 vZ = new(0, 0, 1);

        // ---- Derived reference positions ----
        float fRailXLeft   = fRailInsetX;                           // left  rail center X
        float fRailXRight  = fBaseOuterX - fRailInsetX;             // right rail center X
        float fRailOutLeft = fRailXLeft  - fRailWidth / 2f;         // left  rail outer face X
        float fRailOutRight= fRailXRight + fRailWidth / 2f;         // right rail outer face X
        float fRailTopZ    = fBaseOuterZ + fRailHeight;             // Y-rail top Z
        float fMidY        = fBaseOuterY / 2f;

        // Bridge (from XRails / GantryBridge code)
        float fBridgeYFront = fMidY - fGantryBridgeY / 2f;
        float fBridgeZ      = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;
        float fRailSpanX    = (fBaseOuterX - fRailInsetX) - fRailInsetX - fUprightX;
        float fBridgeMidX   = fBaseOuterX / 2f;
        float fRailStartX   = fBridgeMidX - fRailSpanX / 2f;
        float fRailEndX     = fBridgeMidX + fRailSpanX / 2f;

        // Z plate (from ZAssembly)
        float fPlateRightX  = fBridgeMidX + fZPlateX / 2f;
        float fPlateFrontY  = fBridgeYFront - fZPlateY / 2f;        // plate front face Y
        float fPlateTopZ    = fBridgeZ + fZPlateZ / 2f;
        float fPlateBotZ    = fBridgeZ - fZPlateZ / 2f;

        // =================================================================
        // 1. Y-AXIS LIMIT SWITCHES  (2 total — one per end)
        //    Left-rail outer side @ front,  right-rail outer side @ back
        // =================================================================

        // Front switch (left rail, outer face)
        {
            float fYsw = 5f;   // slightly inside the extreme front
            Vector3 vecMount = new(fRailOutLeft, fYsw, fRailTopZ);
            // normal=-X (outward from left rail face), along=+Y (forward), up=+Z
            // Lever extends in +Y (into path of bearing block coming from front)
            voxSafety += voxLimitSwitch(vecMount, -vX, vY, vZ, +1f);
        }

        // Back switch (right rail, outer face)
        {
            float fYsw = fBaseOuterY - 5f;
            Vector3 vecMount = new(fRailOutRight, fYsw, fRailTopZ);
            // normal=+X (outward from right rail face), along=-Y (backward), up=+Z
            // Lever extends in -Y
            voxSafety += voxLimitSwitch(vecMount, vX, -vY, vZ, +1f);
        }

        // =================================================================
        // 2. X-AXIS LIMIT SWITCHES  (2 total — left and right)
        //    Mounted on gantry bridge front face
        // =================================================================

        // Left switch
        {
            float fXsw = fRailStartX + 5f;
            Vector3 vecMount = new(fXsw, fBridgeYFront, fBridgeZ);
            // normal=-Y (forward from bridge face), along=+X (left→right), up=+Z
            // Lever extends in +X
            voxSafety += voxLimitSwitch(vecMount, -vY, vX, vZ, +1f);
        }

        // Right switch
        {
            float fXsw = fRailEndX - 5f;
            Vector3 vecMount = new(fXsw, fBridgeYFront, fBridgeZ);
            // normal=-Y, along=-X, up=+Z  (lever extends in -X)
            voxSafety += voxLimitSwitch(vecMount, -vY, -vX, vZ, +1f);
        }

        // =================================================================
        // 3. Z-AXIS LIMIT SWITCHES  (2 total — top and bottom)
        //    Mounted on right edge of Z back plate
        // =================================================================

        // Top switch
        {
            float fZsw = fPlateTopZ - 10f;
            Vector3 vecMount = new(fPlateRightX, fPlateFrontY, fZsw);
            // normal=+X (outward from plate right edge), along=+Z (up), up=+Y
            // Lever extends in +Z
            voxSafety += voxLimitSwitch(vecMount, vX, vZ, vY, +1f);
        }

        // Bottom switch
        {
            float fZsw = fPlateBotZ + 10f;
            Vector3 vecMount = new(fPlateRightX, fPlateFrontY, fZsw);
            // normal=+X, along=-Z (down), up=+Y; lever extends in -Z
            voxSafety += voxLimitSwitch(vecMount, vX, -vZ, vY, +1f);
        }

        // =================================================================
        // 4. HARD END STOPS  (6 total — one per limit switch, 5mm further)
        // =================================================================

        // --- Y-axis hard stops ---
        // Front (extreme, left rail outer face)
        voxSafety += voxBumper(
            new(fRailOutLeft, 0f, fRailTopZ), -vX, vZ);

        // Back (extreme, right rail outer face)
        voxSafety += voxBumper(
            new(fRailOutRight, fBaseOuterY, fRailTopZ), vX, vZ);

        // --- X-axis hard stops ---
        // Left extreme
        voxSafety += voxBumper(
            new(fRailStartX, fBridgeYFront, fBridgeZ), -vY, vZ);

        // Right extreme
        voxSafety += voxBumper(
            new(fRailEndX, fBridgeYFront, fBridgeZ), -vY, vZ);

        // --- Z-axis hard stops ---
        // Top extreme
        voxSafety += voxBumper(
            new(fPlateRightX, fPlateFrontY, fPlateTopZ), vX, vY);

        // Bottom extreme
        voxSafety += voxBumper(
            new(fPlateRightX, fPlateFrontY, fPlateBotZ), vX, vY);

        // =================================================================
        // 5. E-STOP BUTTON
        //    Front center of base frame, at 70% height
        // =================================================================
        voxSafety += voxEStop(
            new Vector3(fBaseOuterX / 2f, 0f, fBaseOuterZ * 0.7f),
            -vY);   // button extends forward (-Y) from front face

        Log("Safety components done — 6 limit switches, 6 end stops, 1 E-stop.");
        return voxSafety;
    }
}

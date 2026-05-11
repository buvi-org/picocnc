using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// NEMA 23 stepper motor mounting plates for X, Y, and Z axes.
    /// Each plate has 4 bolt holes in a NEMA 23 bolt circle and a center shaft bore.
    /// Motor bodies project behind each plate.
    /// </summary>
    public static Voxels voxConstructMotorMounts()
    {
        Log("Building motor mounts...");

        float fMidY = fBaseOuterY / 2f;
        float fBridgeMidX = fBaseOuterX / 2f;
        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;

        Voxels voxAllMotors = new();

        // --- Y-axis motor: at the back end of the base, centered ---
        Vector3 vecYMotor = new(fBridgeMidX, fYMotorY, fBaseOuterZ + fRailHeight + fNema23Width / 2f);
        voxAllMotors += voxConstructNema23Plate(vecYMotor, new Vector3(0, 1, 0));
        voxAllMotors += voxBuildMotorBody(vecYMotor, new Vector3(0, 1, 0), fMountPlateThick);

        // --- X-axis motor: at one end of the gantry bridge ---
        // Positioned behind the bridge face to clear X rails (Y=[212.5,248])
        // and X-axis limit switches (Y=[200,220]).
        float fBridgeYFront = fMidY - fGantryBridgeY / 2f;
        Vector3 vecXMotoR = new(
            fXMotorX,
            fBridgeYFront + fNema23Width / 2f + 30f,
            fBridgeZ);
        voxAllMotors += voxConstructNema23Plate(vecXMotoR, new Vector3(0, -1, 0));
        voxAllMotors += voxBuildMotorBody(vecXMotoR, new Vector3(-1, 0, 0), fMountPlateThick);

        // --- Z-axis motor: at the top of Z plate ---
        float fPlateYFront = fBridgeYFront - fZPlateY;
        Vector3 vecZMotoR = new(
            fBridgeMidX,
            fPlateYFront + fNema23Width / 2f,
            fBridgeZ + fZPlateZ / 2f - 20f);
        voxAllMotors += voxConstructNema23Plate(vecZMotoR, new Vector3(0, -1, 0));
        voxAllMotors += voxBuildMotorBody(vecZMotoR, new Vector3(0, 1, 0), fMountPlateThick);

        Log("Motor mounts done.");
        return voxAllMotors;
    }

    /// <summary>
    /// Creates a single NEMA 23 motor mount plate with bolt pattern.
    /// vecNormal is the direction the motor shaft points (plate normal).
    /// </summary>
    private static Voxels voxConstructNema23Plate(Vector3 vecCenter, Vector3 vecNormal)
    {
        // Plate: orient so thickness is along vecNormal
        float fPlateSize = fNema23Width;
        float fPlateThick = fMountPlateThick;

        // Build plate as a flat box, then we'll position bolt holes in its plane
        Voxels voxPlate;

        if (MathF.Abs(vecNormal.Y) > 0.9f)
        {
            // Plate is in XZ plane (motor faces Y)
            voxPlate = voxBox(
                new Vector3(fPlateSize, fPlateThick, fPlateSize),
                vecCenter);
        }
        else if (MathF.Abs(vecNormal.X) > 0.9f)
        {
            // Plate is in YZ plane (motor faces X)
            voxPlate = voxBox(
                new Vector3(fPlateThick, fPlateSize, fPlateSize),
                vecCenter);
        }
        else
        {
            // Plate faces Z
            voxPlate = voxBox(
                new Vector3(fPlateSize, fPlateSize, fPlateThick),
                vecCenter);
        }

        // Bolt holes in the plate plane
        List<Vector3> aBolts = aBoltCircle(vecCenter, fNema23BoltCircle, 4);
        foreach (Vector3 vecBolt in aBolts)
        {
            // Hole goes through the plate thickness (along vecNormal)
            Vector3 vecBoltTop = vecBolt + vecNormal * (fPlateThick + 5f);
            Vector3 vecBoltBot = vecBolt - vecNormal * (fPlateThick + 5f);
            voxPlate.BoolSubtract(voxCylinder(vecBoltBot, vecBoltTop, fBoltHoleDia));
        }

        // Center shaft bore
        Vector3 vecShaftTop = vecCenter + vecNormal * (fPlateThick + 5f);
        Vector3 vecShaftBot = vecCenter - vecNormal * (fPlateThick + 5f);
        voxPlate.BoolSubtract(voxCylinder(vecShaftBot, vecShaftTop, fNema23ShaftBore));

        // Standoffs (small cylinders behind the plate)
        Vector3 vecStandoffDir = -vecNormal;
        float fStandoffH = 15f;
        float fStandoffR = 8f / 2f;

        foreach (Vector3 vecBolt in aBolts)
        {
            Vector3 vecStandoffCenter = vecBolt + vecStandoffDir * (fPlateThick / 2f + fStandoffH / 2f);
            Voxels voxStandoff = voxCylinder(
                vecBolt,
                vecBolt + vecStandoffDir * (fPlateThick + fStandoffH),
                fStandoffR * 2f);
            voxPlate += voxStandoff;
        }

        return voxPlate;
    }

    /// <summary>
    /// Builds a NEMA 23 stepper motor body behind a mounting plate.
    /// vecPlateCenter = center of the mounting plate
    /// vecForward = unit vector pointing from plate toward where motor extends
    /// fPlateThick = plate thickness (fMountPlateThick = 8f)
    /// </summary>
    private static Voxels voxBuildMotorBody(Vector3 vecPlateCenter, Vector3 vecForward, float fPlateThick)
    {
        Voxels voxBody = new();

        float fHalfPlate = fPlateThick / 2f;      // 4f
        float fBodyLen   = 76f;                   // motor body length along vecForward
        float fBodyHalf  = fBodyLen / 2f;         // 38f
        float fBodyW     = 57f;                   // NEMA 23 width / height

        // --- Box size oriented so the 76mm dimension is along vecForward ---
        Vector3 vecBoxSize;
        if (MathF.Abs(vecForward.Y) > 0.9f)
            vecBoxSize = new Vector3(fBodyW, fBodyLen, fBodyW);  // (57, 76, 57)
        else if (MathF.Abs(vecForward.X) > 0.9f)
            vecBoxSize = new Vector3(fBodyLen, fBodyW, fBodyW);  // (76, 57, 57)
        else
            vecBoxSize = new Vector3(fBodyW, fBodyW, fBodyLen);  // (57, 57, 76)

        // 1. Main body: 57 mm square  x  76 mm long, centred behind the plate
        Vector3 vecBodyCenter = vecPlateCenter + vecForward * (fHalfPlate + fBodyHalf);
        voxBody += voxBox(vecBoxSize, vecBodyCenter);

        // 2. Rear cover: cylinder disc 57 mm OD  x  5 mm thick at the back face
        float fCoverThick  = 5f;
        float fCoverHalf   = fCoverThick / 2f;    // 2.5f
        Vector3 vecCoverCenter = vecPlateCenter + vecForward * (fHalfPlate + fBodyLen + fCoverHalf);
        Vector3 vecCoverStart  = vecCoverCenter - vecForward * fCoverHalf;
        Vector3 vecCoverEnd    = vecCoverCenter + vecForward * fCoverHalf;
        voxBody += voxCylinder(vecCoverStart, vecCoverEnd, fBodyW);

        // 3. Cable exit: 10 mm OD  x  15 mm long, extending further from the rear cover
        float fCableLen  = 15f;
        float fCableHalf = fCableLen / 2f;         // 7.5f
        Vector3 vecCableCenter = vecPlateCenter + vecForward * (fHalfPlate + fBodyLen + fCoverThick + fCableHalf);
        Vector3 vecCableStart  = vecCableCenter - vecForward * fCableHalf;
        Vector3 vecCableEnd    = vecCableCenter + vecForward * fCableHalf;
        voxBody += voxCylinder(vecCableStart, vecCableEnd, 10f);

        // 4. Motor shaft: 6.35 mm OD  x  25 mm long, extending forward through the bore
        float fShaftLen  = 25f;
        float fShaftDia  = 6.35f;
        Vector3 vecShaftStart = vecPlateCenter;
        Vector3 vecShaftEnd   = vecPlateCenter + vecForward * fShaftLen;
        voxBody += voxCylinder(vecShaftStart, vecShaftEnd, fShaftDia);

        return voxBody;
    }
}

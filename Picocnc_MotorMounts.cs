using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// NEMA 23 stepper motor mounting plates for X, Y, and Z axes.
    /// Each plate has 4 bolt holes in a NEMA 23 bolt circle and a center shaft bore.
    /// </summary>
    public static Voxels voxConstructMotorMounts()
    {
        Library.Log("Building motor mounts...");

        float fMidY = fBaseOuterY / 2f;
        float fBridgeMidX = fBaseOuterX / 2f;
        float fBridgeZ = fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;

        Voxels voxAllMotors = new();

        // --- Y-axis motor: at the back end of the base, centered ---
        Vector3 vecYMotor = new(fBridgeMidX, fBaseOuterY - 30f, fBaseOuterZ + fRailHeight + fNema23Width / 2f);
        voxAllMotors += voxConstructNema23Plate(vecYMotor, new Vector3(0, 1, 0));

        // --- X-axis motor: at one end of the gantry bridge ---
        float fBridgeYFront = fMidY - fGantryBridgeY / 2f;
        Vector3 vecXMotoR = new(
            fRailInsetX + fUprightX + 30f,
            fBridgeYFront + fNema23Width / 2f,
            fBridgeZ);
        voxAllMotors += voxConstructNema23Plate(vecXMotoR, new Vector3(0, -1, 0));

        // --- Z-axis motor: at the top of Z plate ---
        float fPlateYFront = fBridgeYFront - fZPlateY;
        Vector3 vecZMotoR = new(
            fBridgeMidX,
            fPlateYFront + fNema23Width / 2f,
            fBridgeZ + fZPlateZ / 2f - 20f);
        voxAllMotors += voxConstructNema23Plate(vecZMotoR, new Vector3(0, -1, 0));

        Library.Log("Motor mounts done.");
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
}

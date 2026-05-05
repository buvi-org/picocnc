using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Work bed: flat table slab on top of the base frame with T-slot grooves,
    /// spoil board, T-nuts, reference fence, and hold-down clamps.
    /// </summary>
    public static Voxels voxConstructWorkBed()
    {
        Library.Log("Building work bed...");

        float fTableX = fWorkAreaX + 40f;    // slight overhang beyond work area
        float fTableY = fWorkAreaY + 40f;
        float fTableZ = fTableThick;

        Vector3 vecTableCenter = new(
            fBaseOuterX / 2f,
            fBaseOuterY / 2f,
            fBaseOuterZ + fTableZ / 2f);

        Voxels voxTable = voxBox(
            new Vector3(fTableX, fTableY, fTableZ),
            vecTableCenter);

        // --- T-slot grooves ---
        // Each T-slot is a T-shaped subtraction volume running full Y depth
        float fSlotY = fTableY + 20f; // extend past table edges
        float fSlotUpperH = fTSlotDepth * 0.4f;
        float fSlotLowerH = fTSlotDepth * 0.6f;

        float fStartX = vecTableCenter.X - fTableX / 2f + 40f; // inset from edge
        float fEndX   = vecTableCenter.X + fTableX / 2f - 40f;

        List<float> aSlotXPositions = new();
        List<Voxels> aSlots = new();

        for (float x = fStartX; x <= fEndX + 1f; x += fTSlotSpacing)
        {
            aSlotXPositions.Add(x);

            // Upper (wider) slot
            Voxels voxUpper = voxBox(
                new Vector3(fTSlotUpperW, fSlotY, fSlotUpperH),
                new Vector3(x, vecTableCenter.Y, vecTableCenter.Z + fTableZ / 2f - fSlotUpperH / 2f));

            // Lower (narrower) slot
            Voxels voxLower = voxBox(
                new Vector3(fTSlotLowerW, fSlotY, fSlotLowerH),
                new Vector3(x, vecTableCenter.Y, vecTableCenter.Z + fTableZ / 2f - fSlotUpperH - fSlotLowerH / 2f));

            aSlots.Add(voxUpper + voxLower);
        }

        foreach (Voxels voxSlot in aSlots)
            voxTable.BoolSubtract(voxSlot);

        float fTableTopZ = fBaseOuterZ + fTableZ;

        // ============================================================
        // 1. Spoil board (sacrificial MDF layer on top of the table)
        // ============================================================
        const float fSpoilBoardThick = 18f;

        Vector3 vecSpoilCenter = new(
            fBaseOuterX / 2f,
            fBaseOuterY / 2f,
            fTableTopZ + fSpoilBoardThick / 2f);

        Voxels voxSpoil = voxBox(
            new Vector3(fTableX, fTableY, fSpoilBoardThick),
            vecSpoilCenter);

        // Counterbored mounting holes near the four corners (M8 clearance)
        {
            float fSBInset = 50f;
            float fSBHalfX = fTableX / 2f;
            float fSBHalfY = fTableY / 2f;

            Vector3[] aSBHoleCenters = new Vector3[]
            {
                new(vecSpoilCenter.X - fSBHalfX + fSBInset, vecSpoilCenter.Y - fSBHalfY + fSBInset, vecSpoilCenter.Z),
                new(vecSpoilCenter.X + fSBHalfX - fSBInset, vecSpoilCenter.Y - fSBHalfY + fSBInset, vecSpoilCenter.Z),
                new(vecSpoilCenter.X - fSBHalfX + fSBInset, vecSpoilCenter.Y + fSBHalfY - fSBInset, vecSpoilCenter.Z),
                new(vecSpoilCenter.X + fSBHalfX - fSBInset, vecSpoilCenter.Y + fSBHalfY - fSBInset, vecSpoilCenter.Z),
            };

            foreach (Vector3 vecHC in aSBHoleCenters)
            {
                // Through hole: 8.5mm diameter through entire spoil board
                Voxels voxThrough = voxCylinderZ(8.5f, fSpoilBoardThick + 4f, vecHC);
                voxSpoil.BoolSubtract(voxThrough);

                // Counterbore: 16mm diameter, 5mm deep from top face
                float fSBTopZ = vecSpoilCenter.Z + fSpoilBoardThick / 2f;
                float fCBBotZ = fSBTopZ - 5f + 1f;  // 5mm deep, +1 margin
                float fCBTopZ = fSBTopZ + 1f;
                Voxels voxCB = voxCylinder(
                    new Vector3(vecHC.X, vecHC.Y, fCBBotZ),
                    new Vector3(vecHC.X, vecHC.Y, fCBTopZ),
                    16f);
                voxSpoil.BoolSubtract(voxCB);
            }
        }

        // ============================================================
        // 2. T-slot nuts (in the existing T-slot grooves)
        // ============================================================
        float fNutLen = fTSlotSpacing * 0.3f; // 30mm
        const float fNutUpperH = 6f;
        const float fNutLowerH = 4f;

        float fSlotYStart = vecTableCenter.Y - fSlotY / 2f;
        float fSlotYEnd   = vecTableCenter.Y + fSlotY / 2f;
        float fSlotYMid   = vecTableCenter.Y;

        List<Voxels> aTNuts = new();

        foreach (float xSlot in aSlotXPositions)
        {
            float[] aNutYPos = { fSlotYStart + 30f, fSlotYMid, fSlotYEnd - 30f };

            foreach (float yNut in aNutYPos)
            {
                // T-nut upper part (wider): 20mm(X) x 30mm(Y) x 6mm(Z)
                Vector3 vecUpperCenter = new(xSlot, yNut, fTableTopZ - fNutUpperH / 2f);
                Voxels voxNutUp = voxBox(
                    new Vector3(fTSlotUpperW, fNutLen, fNutUpperH),
                    vecUpperCenter);

                // T-nut lower part (narrower): 10mm(X) x 30mm(Y) x 4mm(Z)
                Vector3 vecLowerCenter = new(xSlot, yNut,
                    fTableTopZ - fNutUpperH - fNutLowerH / 2f);
                Voxels voxNutLo = voxBox(
                    new Vector3(fTSlotLowerW, fNutLen, fNutLowerH),
                    vecLowerCenter);

                Voxels voxNut = voxNutUp + voxNutLo;

                // M8 threaded hole: 6.8mm tap drill, 10mm deep from top face
                Voxels voxHole = voxCylinderZ(6.8f, 12f,
                    new Vector3(xSlot, yNut, fTableTopZ - 5f));
                voxNut.BoolSubtract(voxHole);

                aTNuts.Add(voxNut);
            }
        }

        // Combine all T-nuts into one Voxels
        Voxels voxTNuts = aTNuts[0];
        for (int i = 1; i < aTNuts.Count; i++)
            voxTNuts = voxTNuts + aTNuts[i];

        // ============================================================
        // 3. Reference edge / alignment fence
        // ============================================================
        float fSpoilTopZ = fTableTopZ + fSpoilBoardThick;
        const float fFenceW = 15f;
        const float fFenceH = 25f;

        // Spoil board back edge (positive Y side)
        float fSpoilBackY = vecSpoilCenter.Y + fTableY / 2f;

        // Fence sits on top of spoil board, back edge flush with spoil board back
        Vector3 vecFenceCenter = new(
            fBaseOuterX / 2f,
            fSpoilBackY - fFenceW / 2f,
            fSpoilTopZ + fFenceH / 2f);

        Voxels voxFence = voxBox(
            new Vector3(fTableX, fFenceW, fFenceH),
            vecFenceCenter);

        // ============================================================
        // 4. Hold-down clamps (step clamps)
        // ============================================================
        Voxels voxClamp1 = BuildStepClamp(
            fBaseOuterX / 4f,
            fBaseOuterY / 2f + 50f,
            fTableTopZ, fSpoilTopZ);

        Voxels voxClamp2 = BuildStepClamp(
            fBaseOuterX * 3f / 4f,
            fBaseOuterY / 2f + 50f,
            fTableTopZ, fSpoilTopZ);

        // ============================================================
        // Combine everything
        // ============================================================
        Voxels voxWorkBed = voxTable + voxSpoil + voxTNuts + voxFence + voxClamp1 + voxClamp2;

        Library.Log("Work bed done.");
        return voxWorkBed;
    }

    /// <summary>
    /// Builds a single step-clamp assembly (body, stud, T-nut, hex nut)
    /// at the given XY position on the spoil board.
    /// </summary>
    private static Voxels BuildStepClamp(
        float fClampX, float fClampY,
        float fTableTopZ, float fSpoilTopZ)
    {
        // --- Clamp body: 80mm(X) x 20mm(Y), stepped in Z ---
        const float fClampLenX = 80f;
        const float fClampWidY = 20f;
        const float fHeelH = 12f;
        const float fToeH  = 6f;

        float fClampBottomZ = fSpoilTopZ;  // clamp sits on spoil board

        // Heel (rear, toward +Y fence): full 12mm tall
        float fHeelCenterY = fClampY + fClampWidY / 4f;
        Vector3 vecHeelCenter = new(fClampX, fHeelCenterY, fClampBottomZ + fHeelH / 2f);
        Voxels voxHeel = voxBox(
            new Vector3(fClampLenX, fClampWidY / 2f, fHeelH),
            vecHeelCenter);

        // Toe (front, toward center): 6mm tall
        float fToeCenterY = fClampY - fClampWidY / 4f;
        Vector3 vecToeCenter = new(fClampX, fToeCenterY, fClampBottomZ + fToeH / 2f);
        Voxels voxToe = voxBox(
            new Vector3(fClampLenX, fClampWidY / 2f, fToeH),
            vecToeCenter);

        Voxels voxClamp = voxHeel + voxToe;

        // Through-slot for hold-down stud: 30mm(X) x 10mm(Y) through full thickness
        Vector3 vecSlotCenter = new(fClampX, fClampY, fClampBottomZ + fHeelH / 2f);
        Voxels voxSlot = voxBox(
            new Vector3(30f, 10f, fHeelH + 4f),
            vecSlotCenter);
        voxClamp.BoolSubtract(voxSlot);

        // --- Stud: M8 threaded rod, 8mm OD, 60mm tall ---
        float fStudBotZ = fTableTopZ - 10f;  // extend into T-nut zone
        float fStudTopZ = fStudBotZ + 60f;
        Voxels voxStud = voxCylinderZ(8f, 60f,
            new Vector3(fClampX, fClampY, (fStudBotZ + fStudTopZ) / 2f));

        // --- T-slot nut below stud (same geometry as T-slot nuts above) ---
        float fNutLen = fTSlotSpacing * 0.3f; // 30mm
        const float fNutUpH = 6f;
        const float fNutLoH = 4f;

        Vector3 vecCNutUpCenter = new(fClampX, fClampY, fTableTopZ - fNutUpH / 2f);
        Voxels voxCNutUp = voxBox(
            new Vector3(fTSlotUpperW, fNutLen, fNutUpH), vecCNutUpCenter);

        Vector3 vecCNutLoCenter = new(fClampX, fClampY,
            fTableTopZ - fNutUpH - fNutLoH / 2f);
        Voxels voxCNutLo = voxBox(
            new Vector3(fTSlotLowerW, fNutLen, fNutLoH), vecCNutLoCenter);

        Voxels voxCNut = voxCNutUp + voxCNutLo;
        voxCNut.BoolSubtract(
            voxCylinderZ(6.8f, 12f,
                new Vector3(fClampX, fClampY, fTableTopZ - 5f)));

        // --- Hex nut on top: 12mm OD cylinder, 8mm tall ---
        float fHexNutBotZ = fClampBottomZ + fHeelH + 2f;  // slight gap above heel
        Voxels voxHexNut = voxCylinderZ(12f, 8f,
            new Vector3(fClampX, fClampY, fHexNutBotZ + 4f));

        return voxClamp + voxStud + voxCNut + voxHexNut;
    }
}

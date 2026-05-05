using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Work bed: flat table slab on top of the base frame with T-slot grooves.
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

        List<Voxels> aSlots = new();

        for (float x = fStartX; x <= fEndX + 1f; x += fTSlotSpacing)
        {
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

        Library.Log("Work bed done.");
        return voxTable;
    }
}

using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    public static Voxels voxConstructBaseFrame()
    {
        Log("Building base frame...");

        // --- Outer shell ---
        Voxels voxOuter = voxBox(
            new Vector3(fBaseOuterX, fBaseOuterY, fBaseOuterZ),
            new Vector3(fBaseOuterX / 2f, fBaseOuterY / 2f, fBaseOuterZ / 2f));

        // Hollow out the interior
        Voxels voxHollow = voxOuter.voxShell(-fBaseWallThick);

        // --- Internal ribs (Y-direction) ---
        Voxels voxRibsY = new();
        float fInnerXMin = fBaseWallThick;
        float fInnerXMax = fBaseOuterX - fBaseWallThick;
        float fMidX = fBaseOuterX / 2f;

        for (float y = fRibSpacing; y < fBaseOuterY - fRibSpacing / 2f; y += fRibSpacing)
        {
            // Each rib is a rectangular frame built from beams
            Lattice lat = new();
            float fRibR = fRibThick / 2f;

            // Bottom beam (along X, at Z = wall thickness)
            lat.AddBeam(
                new Vector3(fInnerXMin, y, fBaseWallThick), fRibR,
                new Vector3(fInnerXMax, y, fBaseWallThick), fRibR);

            // Top beam (along X, at Z = base height - wall thickness)
            lat.AddBeam(
                new Vector3(fInnerXMin, y, fBaseOuterZ - fBaseWallThick), fRibR,
                new Vector3(fInnerXMax, y, fBaseOuterZ - fBaseWallThick), fRibR);

            // Left vertical beam
            lat.AddBeam(
                new Vector3(fInnerXMin, y, fBaseWallThick), fRibR,
                new Vector3(fInnerXMin, y, fBaseOuterZ - fBaseWallThick), fRibR);

            // Right vertical beam
            lat.AddBeam(
                new Vector3(fInnerXMax, y, fBaseWallThick), fRibR,
                new Vector3(fInnerXMax, y, fBaseOuterZ - fBaseWallThick), fRibR);

            // Center vertical beam
            lat.AddBeam(
                new Vector3(fMidX, y, fBaseWallThick), fRibR,
                new Vector3(fMidX, y, fBaseOuterZ - fBaseWallThick), fRibR);

            voxRibsY.BoolAdd(new Voxels(lat));
        }

        // --- Internal ribs (X-direction) ---
        Voxels voxRibsX = new();
        float fInnerYMin = fBaseWallThick;
        float fInnerYMax = fBaseOuterY - fBaseWallThick;
        float fMidY = fBaseOuterY / 2f;

        for (float x = fRibSpacing; x < fBaseOuterX - fRibSpacing / 2f; x += fRibSpacing)
        {
            Lattice lat = new();
            float fRibR = fRibThick / 2f;

            lat.AddBeam(
                new Vector3(x, fInnerYMin, fBaseWallThick), fRibR,
                new Vector3(x, fInnerYMax, fBaseWallThick), fRibR);

            lat.AddBeam(
                new Vector3(x, fInnerYMin, fBaseOuterZ - fBaseWallThick), fRibR,
                new Vector3(x, fInnerYMax, fBaseOuterZ - fBaseWallThick), fRibR);

            lat.AddBeam(
                new Vector3(x, fInnerYMin, fBaseWallThick), fRibR,
                new Vector3(x, fInnerYMin, fBaseOuterZ - fBaseWallThick), fRibR);

            lat.AddBeam(
                new Vector3(x, fInnerYMax, fBaseWallThick), fRibR,
                new Vector3(x, fInnerYMax, fBaseOuterZ - fBaseWallThick), fRibR);

            lat.AddBeam(
                new Vector3(x, fMidY, fBaseWallThick), fRibR,
                new Vector3(x, fMidY, fBaseOuterZ - fBaseWallThick), fRibR);

            voxRibsX.BoolAdd(new Voxels(lat));
        }

        // --- Corner feet ---
        float fFootSize = 40f;
        float fFootHeight = 20f;
        Voxels voxFeet = new();
        Vector3[] aFootPositions = new[]
        {
            new Vector3(fFootSize / 2f, fFootSize / 2f, fFootHeight / 2f),
            new Vector3(fBaseOuterX - fFootSize / 2f, fFootSize / 2f, fFootHeight / 2f),
            new Vector3(fFootSize / 2f, fBaseOuterY - fFootSize / 2f, fFootHeight / 2f),
            new Vector3(fBaseOuterX - fFootSize / 2f, fBaseOuterY - fFootSize / 2f, fFootHeight / 2f),
        };

        foreach (Vector3 vecFoot in aFootPositions)
        {
            voxFeet.BoolAdd(voxBox(
                new Vector3(fFootSize, fFootSize, fFootHeight),
                vecFoot));
        }

        // --- Compose ---
        Voxels voxResult = new();
        voxResult.BoolAdd(voxHollow);
        voxResult.BoolAdd(voxRibsY);
        voxResult.BoolAdd(voxRibsX);
        voxResult.BoolAdd(voxFeet);

        Log("Base frame done.");
        return voxResult;
    }
}

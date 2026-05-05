namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Master assembly: builds all 11 components, adds each to the viewer
    /// as it's built so the user sees progress live.
    /// </summary>
    public static Voxels voxConstruct()
    {
        Voxels voxMachine = new();

        // 1. Base Frame — foundation
        Voxels vox = voxConstructBaseFrame();
        voxMachine += vox;
        Library.oViewer().Add(vox, 1);

        // 2. Work Bed — table slab with T-slots
        vox = voxConstructWorkBed();
        voxMachine += vox;
        Library.oViewer().Add(vox, 2);

        // 3. Y-Axis Rails — linear rails on base sides
        vox = voxConstructYRails();
        voxMachine += vox;
        Library.oViewer().Add(vox, 3);

        // 4. Gantry Uprights — vertical columns
        vox = voxConstructUprights();
        voxMachine += vox;
        Library.oViewer().Add(vox, 4);

        // 5. Gantry Bridge — hollow box beam with internal ribs
        vox = voxConstructGantryBridge();
        voxMachine += vox;
        Library.oViewer().Add(vox, 5);

        // 6. X-Axis Rails — linear rails on gantry face
        vox = voxConstructXRails();
        voxMachine += vox;
        Library.oViewer().Add(vox, 6);

        // 7. Z-Axis Assembly — back plate + Z rails + carriage
        vox = voxConstructZAssembly();
        voxMachine += vox;
        Library.oViewer().Add(vox, 7);

        // 8. Spindle Mount — clamp ring + flange
        vox = voxConstructSpindleMount();
        voxMachine += vox;
        Library.oViewer().Add(vox, 8);

        // 9. Motor Mounts — NEMA 23 plates (X, Y, Z)
        vox = voxConstructMotorMounts();
        voxMachine += vox;
        Library.oViewer().Add(vox, 9);

        // 10. Lead Screws — T12 drive rods (X, Y, Z)
        vox = voxConstructLeadScrews();
        voxMachine += vox;
        Library.oViewer().Add(vox, 10);

        // 11. Drag Chain Mounts — cable management
        vox = voxConstructDragChains();
        voxMachine += vox;
        Library.oViewer().Add(vox, 11);

        return voxMachine;
    }

    /// <summary>
    /// Exports each component as a separate STL file for manufacturing.
    /// Only called after the viewer is closed (or skipped during fast runs).
    /// </summary>
    public static void ExportAllComponents()
    {
        Library.Log("Exporting individual components...");

        ExportStl(voxConstructBaseFrame(), "BaseFrame");
        ExportStl(voxConstructWorkBed(), "WorkBed");
        ExportStl(voxConstructYRails(), "YRails");
        ExportStl(voxConstructUprights(), "GantryUprights");
        ExportStl(voxConstructGantryBridge(), "GantryBridge");
        ExportStl(voxConstructXRails(), "XRails");
        ExportStl(voxConstructZAssembly(), "ZAssembly");
        ExportStl(voxConstructSpindleMount(), "SpindleMount");
        ExportStl(voxConstructMotorMounts(), "MotorMounts");
        ExportStl(voxConstructLeadScrews(), "LeadScrews");
        ExportStl(voxConstructDragChains(), "DragChains");

        Library.Log("Component export complete.");
    }
}

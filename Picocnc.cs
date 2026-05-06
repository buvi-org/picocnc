namespace PicoGK;

public static partial class Picocnc
{
    public static void Task()
    {
        Library.Log("PicoCNC — CNC Machine Generator");
        Library.Log($"Voxel size: {fVoxelSizeMM} mm");
        Library.Log($"Work area: {fWorkAreaX} x {fWorkAreaY} x {fWorkAreaZ} mm");

        // Set up viewer groups with distinct colors per component type
        // Group 1: structural (steel gray)
        Library.oViewer().SetGroupMaterial(1, "8899AA", 0.3f, 0.2f);
        // Group 2: work bed (wood brown)
        Library.oViewer().SetGroupMaterial(2, "AA8844", 0.5f, 0.1f);
        // Group 3: rails (dark metal)
        Library.oViewer().SetGroupMaterial(3, "667788", 0.2f, 0.6f);
        // Group 4: uprights (blue-gray)
        Library.oViewer().SetGroupMaterial(4, "556688", 0.3f, 0.3f);
        // Group 5: gantry bridge (red-orange accent)
        Library.oViewer().SetGroupMaterial(5, "CC6633", 0.3f, 0.2f);
        // Group 6: X rails (dark metal)
        Library.oViewer().SetGroupMaterial(6, "667788", 0.2f, 0.6f);
        // Group 7: Z assembly (aluminum)
        Library.oViewer().SetGroupMaterial(7, "99AABB", 0.3f, 0.4f);
        // Group 8: spindle mount (dark gray)
        Library.oViewer().SetGroupMaterial(8, "444444", 0.3f, 0.3f);
        // Group 9: motor mounts (black)
        Library.oViewer().SetGroupMaterial(9, "222222", 0.4f, 0.2f);
        // Group 10: lead screws (shiny steel)
        Library.oViewer().SetGroupMaterial(10, "CCCCCC", 0.1f, 0.8f);
        // Group 11: drag chains (dark plastic)
        Library.oViewer().SetGroupMaterial(11, "333322", 0.5f, 0.1f);
        // Group 12: safety (bright yellow/red)
        Library.oViewer().SetGroupMaterial(12, "FF4444", 0.2f, 0.1f);
        // Group 13: toolpath (bright green)
        Library.oViewer().SetGroupMaterial(13, "00FF44", 0.2f, 0.5f);

        // Build the full machine (components appear live in viewer)
        Voxels voxMachine = voxConstruct();

        Library.Log("PicoCNC construction complete.");

        // --- Collision verification pass ---
        // Rebuilds each component individually and checks all pairwise
        // overlaps. Reports unexpected collisions (parts embedded into
        // each other at default mid-travel positions).
        VerifyCollisions();

        // --- G-code toolpath visualization ---
        // Load and visualize a sample G-code file if one exists in the
        // project output directory. To use, place a .nc file named
        // "sample.nc" alongside Picocnc.exe (or uncomment and provide
        // an explicit path below).
        string strGCode = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample.nc");
        if (File.Exists(strGCode))
            LoadAndVisualizeGCode(strGCode);

        // --- Beam structural analysis ---
        RunBeamAnalysis();

        // Export STLs (this is slow — do it after preview)
        ExportStl(voxMachine, "Assembly");
        ExportAllComponents();

        Library.Log("All exports complete. PicoCNC finished.");
    }
}

# PicoCNC — CNC Machine Generator

A **gantry-style CNC router** designed entirely through code using [PicoGK](https://picogk.org), LEAP 71's open-source computational geometry kernel.

The entire machine — from base frame to spindle mount — is generated algorithmically via voxel-based signed distance fields, boolean CSG operations, and lattice structures. The result is STL files ready for 3D printing or CNC manufacturing.

![PicoCNC](https://raw.githubusercontent.com/leap71/PicoGK/refs/heads/main/PicoGK_Animation.cs)

## How It Works

**Computational Engineering** replaces traditional CAD with code. Instead of manually clicking through a GUI to model each part, we write algorithms that describe the machine's geometry. PicoGK builds it as a volumetric voxel field, applies boolean operations to combine/subtract components, and surfaces the final mesh as an STL.

### Architecture

```
Program.cs                     Entry point → Library.Go()
  └── Picocnc.Task()            Master orchestrator
        └── voxConstruct()      Boolean composition tree
              ├── voxConstructBaseFrame()      Hollow box + lattice ribs
              ├── voxConstructWorkBed()        Slab − T-slot grooves
              ├── voxConstructYRails()         Rails + bolt holes
              ├── voxConstructUprights()       Columns + gussets
              ├── voxConstructGantryBridge()   Hollow beam + diagonal ribs
              ├── voxConstructXRails()         Rails on bridge
              ├── voxConstructZAssembly()      Z plate + rails + carriage
              ├── voxConstructSpindleMount()   Clamp ring + flange
              ├── voxConstructMotorMounts()    NEMA 23 plates
              ├── voxConstructLeadScrews()     T12 rods + nuts
              └── voxConstructDragChains()     Cable trays + brackets
```

### How We Use PicoGK

| PicoGK Feature | How We Use It |
|----------------|---------------|
| **Voxels** | Every component is a `Voxels` object — a volumetric signed distance field. Components are combined via `+` (union) and `−` (subtract) operators for full CSG. |
| **Lattice** | Internal ribs, lead screws, and cylindrical features are built as collections of beams (`AddBeam`) with specified radii, then voxelized. |
| **Mesh** | Box primitives via `Utils.mshCreateCube()` are voxelized into Voxels. Final geometry is surfaced back to Mesh for STL export via `mshAsMesh().SaveToStlFile()`. |
| **voxShell** | Creates thin-walled hollow structures (base frame, gantry bridge) by offsetting a solid volume inward. |
| **voxLatticeBeam** | Creates cylindrical volumes (bolts, shafts, rails) as single-beam voxel fields — used instead of traditional cylinder primitives. |
| **Viewer groups** | Each of the 11 components gets its own viewer group with distinct material colors, so the machine assembles visually piece by piece. |

### What's New Here

Traditional Computational Engineering models in PicoGK have focused on single-function objects (heat exchangers, rover wheels, rocket engines). PicoCNC is different:

1. **Multi-component machine assembly** — 11 discrete components designed to fit together, each independently exportable for manufacturing
2. **Real-world hardware integration** — NEMA motor bolt patterns, T-slot profiles, spindle diameters — standard off-the-shelf parts inform the geometry
3. **Parametric envelope** — change `fWorkAreaX` from 500 to 1000mm and the entire machine rescales: base frame, rails, gantry bridge, and all components adapt automatically
4. **Live progressive preview** — each component renders as it's built, color-coded by subsystem (structural, motion, drive)
5. **PicoGK v2 API** — built on the latest `PicoGK 2.0.0-beta11` with `.NET 10.0`, using the new `voxLatticeBeam`, `SetGroupMaterial(fRoughness, fMetallic)`, and singleton-less voxel constructors

## Quick Start

### Prerequisites
- Windows x64 or macOS Apple Silicon
- .NET 10.0 SDK
- PicoGK native runtime ([installer](https://github.com/leap71/PicoGKInstaller))

### Run

```bash
dotnet run
```

The PicoGK viewer opens. Each component builds and appears in the viewer in sequence. Close the viewer window to trigger STL export.

### Tune the Machine

Edit `Picocnc_Parameters.cs`:

```csharp
// Machine envelope
public const float fWorkAreaX = 500f;   // change to 750, 1000, etc.
public const float fWorkAreaY = 400f;
public const float fWorkAreaZ = 120f;

// Voxel resolution
public const float fVoxelSizeMM = 2.0f; // 2mm = fast preview, 0.5mm = production

// Wall thicknesses
public const float fBaseWallThick = 15f;
public const float fGantryWallThick = 8f;

// Spindle
public const float fSpindleOD = 65f;    // match your spindle
```

Then `dotnet build && dotnet run` to regenerate.

### Output

12 STL files are exported to your home directory:

| File | Description | Size (2mm voxels) |
|------|-------------|-------------------|
| `PicoCNC_Assembly.stl` | Full machine | ~67 MB |
| `PicoCNC_BaseFrame.stl` | Hollow frame + ribs | ~44 MB |
| `PicoCNC_WorkBed.stl` | Table with T-slots | ~14 MB |
| `PicoCNC_YRails.stl` | Y-axis rails + bearings | ~2 MB |
| `PicoCNC_GantryUprights.stl` | Vertical columns | ~4 MB |
| `PicoCNC_GantryBridge.stl` | Hollow beam + lattice ribs | ~8 MB |
| `PicoCNC_XRails.stl` | X-axis rails | ~2 MB |
| `PicoCNC_ZAssembly.stl` | Z plate + rails + carriage | ~2 MB |
| `PicoCNC_SpindleMount.stl` | 65mm clamp ring | ~2 MB |
| `PicoCNC_MotorMounts.stl` | 3× NEMA 23 plates | ~1 MB |
| `PicoCNC_LeadScrews.stl` | T12 rods + nuts + bearings | ~2 MB |
| `PicoCNC_DragChains.stl` | Cable trays + brackets | ~3 MB |

## Project Structure

```
picocnc/
├── Program.cs                  Entry point
├── Picocnc.cs                  Task() + viewer setup
├── Picocnc_Parameters.cs       All dimensions (single source of truth)
├── Picocnc_Helpers.cs          voxBox, voxCylinder, bolt patterns, export
├── Picocnc_Assembly.cs         Boolean composition tree + per-component export
├── Picocnc_BaseFrame.cs        Component 1: Base frame
├── Picocnc_WorkBed.cs          Component 2: Work bed + T-slots
├── Picocnc_YRails.cs           Component 3: Y-axis rails
├── Picocnc_GantryUprights.cs   Component 4: Gantry uprights
├── Picocnc_GantryBridge.cs     Component 5: Gantry bridge
├── Picocnc_XRails.cs           Component 6: X-axis rails
├── Picocnc_ZAssembly.cs        Component 7: Z-axis assembly
├── Picocnc_SpindleMount.cs     Component 8: Spindle mount
├── Picocnc_MotorMounts.cs      Component 9: Motor mounts
├── Picocnc_LeadScrews.cs       Component 10: Lead screws
├── Picocnc_DragChains.cs       Component 11: Drag chain mounts
└── Picocnc.csproj              .NET 10.0 project file
```

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Language | C# 13, .NET 10.0 |
| Geometry Kernel | PicoGK 2.0.0-beta11 (LEAP 71) |
| Voxel Engine | OpenVDB (via PicoGK native runtime) |
| Mesh Output | Binary STL |

## License

Apache 2.0 — same as PicoGK.

Built with [PicoGK](https://picogk.org) by [LEAP 71](https://leap71.com).

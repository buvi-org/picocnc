# PicoCNC Simulation Stack — Digital Twin Architecture

## Purpose

This document defines the simulation, analysis, and verification toolchain for the PicoCNC digital twin. It covers collision detection, rigid body dynamics, structural FEA, kinematic simulation, and modal analysis — all integrated with PicoGK's voxel geometry kernel.

---

## Architecture

```
                    PicoCNC Parameters (Picocnc_Parameters.cs)
                                   |
                    +--------------+--------------+
                    |              |              |
            PicoGK Voxels    CalculiX .inp    BepuPhysics
            (geometry)       (structural      (dynamics
                    |         FEA, modal)      simulation)
                    |              |              |
            +-------+-------+     ccx.exe      PrismaticServo
            |       |       |     (subprocess)   constraints
        Collision  STL   Mesh       |              |
        detection  export  export   .dat parser    Motion data
        (&, vol=0)    |       |                    |
                      v       v                    v
                  CAMotics  3D printing        G-code
                  (external)  slicers           visualization
```

---

## 1. Collision Detection — PicoGK Native

**Status: IMPLEMENTED** (`Picocnc_Verify.cs`)

| Attribute | Detail |
|-----------|--------|
| Library | PicoGK 2.0.0-beta11 |
| License | Apache 2.0 |
| Integration | Already in-process |
| API used | `Voxels & Voxels` (BoolIntersect), `mshAsMesh().nTriangleCount()` |

**How it works:**
- All 12 components rebuilt as separate Voxels objects
- 66 pairwise boolean intersections checked
- Expected overlaps (mechanically connected parts) tagged separately from unexpected collisions
- 4 targeted interface checks: Z-plate/bridge, tool tip clearance, spindle/carriage, X-bearing/Z-plate

**Limitations:**
- Static check only (default mid-travel position)
- Does not simulate motion through travel range
- Voxel boolean is accurate but not real-time (ms per check, not μs)

---

## 2. Rigid Body Dynamics — BepuPhysics v2

**Status: PLANNED (Phase D)**

| Attribute | Detail |
|-----------|--------|
| Library | BepuPhysics v2.5.0-beta.28 |
| License | Apache 2.0 |
| NuGet | `BepuPhysics` |
| Language | 100% C# |
| Target | .NET 8+, compatible with .NET 10 |
| Platforms | Windows, Linux, macOS |

**Why BepuPhysics:**
- Same language, same runtime, same process — zero interop overhead
- Apache 2.0 license matches PicoGK
- Prismatic constraints map directly to CNC linear axes
- Mesh collidables consume PicoGK `mshAsMesh()` output directly
- Actively maintained (latest release March 2026)

**Integration path:**
```csharp
// CNC axes → BepuPhysics constraints
// Y-axis: PrismaticServo along Y (gantry motion)
// X-axis: PrismaticServo along X (carriage on bridge)
// Z-axis: PrismaticServo along Z (spindle plunge)

var simulation = Simulation.Create(...);
// Static: base frame, work bed as Mesh collidables from PicoGK
// Dynamic: gantry assembly, Z carriage with prismatic constraints
// Cutting force: ApplyImpulse() at tool tip contact point
```

**Effort:** Medium. Requires PicoGK→BepuPhysics mesh converter and constraint setup (~500 lines infrastructure).

**Alternatives evaluated and rejected:**
- MagicPhysX (PhysX 5): C# bindings but `unsafe` code, overkill for CNC scale
- BulletSharp: Wraps Bullet 2.x (older), P/Invoke complexity
- MuJoCo: No maintained .NET bindings

---

## 3. FEA / Structural Analysis — CalculiX + BriefFiniteElement.NET

**Status: PLANNED (Phase C)**

### Primary: CalculiX (batch FEA)

| Attribute | Detail |
|-----------|--------|
| Library | CalculiX CrunchiX (ccx) |
| License | GPL v2 |
| Integration | CLI subprocess, Abaqus-compatible `.inp` files |
| Capabilities | Linear/nonlinear static, dynamic, frequency/modal, buckling, thermal, contact |
| Platforms | Windows, Linux, macOS |

**Integration pattern:**
```csharp
// PicoGK geometry → CalculiX .inp → solve → parse .dat results
void RunGantryBridgeFEA()
{
    WriteCalculixInput("bridge.inp", span, sectionH, sectionW, material, load);
    Process.Start("ccx", "-i bridge.inp").WaitForExit();
    float deflection = ParseCalculixDat("bridge.dat");
    Library.Log($"Bridge deflection under {load}N: {deflection:F3} mm");
}
```

**Analysis types for PicoCNC:**
- Static: Bridge beam deflection under 100N cutting load
- Frequency: First 10 natural frequencies of gantry assembly
- Buckling: Upright column stability
- Contact: Bolt preload in critical joints (optional)

**Effort:** Medium-High. Requires `.inp` file generation code, `.dat` result parsing, and bundling `ccx` executable.

### Secondary: BriefFiniteElement.NET (in-process quick checks)

| Attribute | Detail |
|-----------|--------|
| Library | BriefFiniteElement.Net v2.0.5 |
| License | MIT |
| NuGet | `BriefFiniteElement.Net` |
| Elements | Beam, truss, column, shaft, plate, tetra |
| Limitations | Linear static only, no modal, no contact |

**Use case:** Quick beam deflection checks during geometry generation. "Is this wall thickness sufficient?" — answered in-process without spawning CalculiX.

**Effort:** Low. Pure NuGet, same process.

### Beam deflection (no external dependency)

For immediate use, basic beam deflection equations work without any library:

```csharp
// Simply supported beam, center load
// δ_max = (F * L³) / (48 * E * I)
// I = (b * h³) / 12  (rectangular section)
float fDeflection = (fForce * MathF.Pow(fSpan, 3)) / (48f * fYoungsModulus * fMomentOfInertia);
```

This is sufficient for initial wall thickness and rib spacing calculations.

---

## 4. Kinematic Simulation / G-code Execution

**Status: PLANNED (Phase B)**

| Attribute | Detail |
|-----------|--------|
| Approach | Custom C# G-code parser |
| Language | C#, same project |
| Scope | 3-axis router subset: G0, G1, G2, G3, G17-19, G20-21, G90-91, M-codes |
| Effort | Low-Medium (~300-500 lines) |

**Why custom:**
- Keeps everything code-driven, in-process
- Integrates directly with PicoGK viewer for toolpath visualization
- Can feed axis positions to collision checker and BepuPhysics
- No external dependency for a well-defined text format

**Parser design:**
```csharp
class GCodeParser {
    List<GCodeCommand> Parse(string filepath);
    // Each command → target position, feed rate, spindle speed
    // Interpolation between positions (linear G1, rapid G0)
    // Output: sequence of Vector3 tool positions over time
}
```

**Toolpath visualization in PicoGK viewer:**
- Parse G-code → list of tool positions
- Build thin cylinder (tool) at each position
- Add to viewer as a separate group (toolpath trace)
- Color-code by feed rate or operation type

---

## 5. Modal / Vibration Analysis

**Status: PLANNED (Phase D)**

### Primary: CalculiX `*FREQUENCY` step

Same `.inp` infrastructure as structural FEA. Add `*FREQUENCY` step to extract natural frequencies and mode shapes. Output in `.dat` file.

### Secondary: Math.NET Numerics (parametric sweeps)

| Attribute | Detail |
|-----------|--------|
| Library | Math.NET Numerics |
| License | MIT |
| NuGet | `MathNet.Numerics` |

For quick parametric studies ("what happens to the first natural frequency if bridge wall goes from 8mm to 12mm?"), assemble reduced-order beam/spring-mass models and solve the eigenproblem in-process:

```csharp
using MathNet.Numerics.LinearAlgebra;
// K·φ = ω²·M·φ  →  generalized eigenvalue problem
// Output: natural frequencies in Hz
```

**Effort:** Low for beam models, Medium for CalculiX modal.

---

## 6. Recommended Implementation Order

### Phase B — Kinematics & Verification (NOW)

| Step | What | Dependencies | Effort |
|------|------|-------------|--------|
| B1 | G-code parser (`Picocnc_GCode.cs`) | None | Low |
| B2 | Toolpath visualization in viewer | B1 | Low |
| B3 | Travel-range collision sweep | B1 + Verify.cs | Medium |
| B4 | Refactor to Constraints.cs | Constraints.cs | Medium |

### Phase C — Load-Driven Dimensioning

| Step | What | Dependencies | Effort |
|------|------|-------------|--------|
| C1 | Beam deflection solver (`Picocnc_BeamSolver.cs`) | None | Low |
| C2 | Compute min wall thickness from cutting forces | C1 | Low |
| C3 | BriefFiniteElement.NET integration | NuGet, csproj | Low |
| C4 | CalculiX `.inp` generator | C3 | Medium-High |

### Phase D — Full Digital Twin

| Step | What | Dependencies | Effort |
|------|------|-------------|--------|
| D1 | BepuPhysics integration | NuGet, csproj | Medium |
| D2 | CNC axis constraint setup | D1 | Medium |
| D3 | Cutting force simulation | D1, D2 | Medium |
| D4 | Modal analysis (CalculiX) | C4 | Medium |
| D5 | Closed-loop optimization | All | High |

---

## 7. Design Principles

1. **PicoGK voxels are the single source of truth.** All simulation geometry derives from `Voxels` or `Mesh` objects. No secondary CAD model.

2. **In-process where possible.** BepuPhysics, BriefFiniteElement.NET, Math.NET, and the custom G-code parser all run as in-process C#. Only CalculiX runs out-of-process (batch FEA, not real-time).

3. **Code-driven, not GUI-driven.** Every tool accepts programmatic input (C# API, `.inp` text files, NuGet) and produces machine-readable output. No interactive GUI required.

4. **Parametric dimensions close the loop.** The constraint graph feeds dimensions to simulation. Simulation results (deflection, frequency, collision status) feed back to dimension selection. This is computational engineering: the parameters are outputs, not inputs.

5. **Windows + macOS compatible.** All recommended tools run on both platforms via .NET 10 and native binaries.

---

## 8. Comparison Matrix

| Tool | License | Integration | Real-time | PicoGK Bridge | Effort |
|------|---------|-------------|-----------|---------------|--------|
| **PicoGK (&)** | Apache 2.0 | In-process | No (ms) | Native | Done |
| **BepuPhysics v2** | Apache 2.0 | NuGet, in-process | Yes (μs) | Mesh→Collidable | Medium |
| **CalculiX** | GPL v2 | CLI subprocess | No (batch) | .inp generator | Med-High |
| **BriefFiniteElement** | MIT | NuGet, in-process | N/A | Direct API | Low |
| **Math.NET** | MIT | NuGet, in-process | N/A | Direct API | Low |
| **Custom G-code** | N/A | In-process | N/A | Direct | Low-Med |
| **CAMotics** | GPL v2 | External process | No | File export | Low |
| **MagicPhysX** | MIT | NuGet, unsafe | Yes | Mesh→Convex | High |
| **BulletSharp** | zlib | P/Invoke | Yes | Mesh→Convex | Med-High |

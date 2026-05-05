# PicoCNC Computational Model — Audit & Gap Analysis

## Purpose

This document catalogs every inter-component dependency that should be a solved constraint (not hardcoded arithmetic) and every missing component/feature compared to a real-world gantry CNC router. It is the specification for upgrading PicoCNC from parametric CAD to computational engineering.

---

## Part 1: Dependency & Constraint Audit

### Critical: Duplicated Position Computations

The same physical quantity is recomputed independently in multiple files. Change one and miss another = silent misalignment.

#### `fBridgeZ` — Gantry bridge Z-center (7 files recompute identically)

| File | Formula |
|------|---------|
| GantryBridge.cs | `fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f` |
| XRails.cs | same |
| ZAssembly.cs | same |
| SpindleMount.cs | same |
| MotorMounts.cs | same |
| LeadScrews.cs | same |
| DragChains.cs | same |

**Fix**: `public static float fBridgeZ => fBaseOuterZ + fRailHeight + fUprightZ + fGantryBridgeZ / 2f;` — one computed property.

#### `fMidY` — Machine Y centerline (9 files recompute)

| File | Formula |
|------|---------|
| BaseFrame.cs, GantryBridge.cs, GantryUprights.cs, XRails.cs, ZAssembly.cs, SpindleMount.cs, MotorMounts.cs, LeadScrews.cs, DragChains.cs | `fBaseOuterY / 2f` |

**Fix**: `public static float fMidY => fBaseOuterY / 2f;`

#### `fBridgeMidX` — Machine X centerline (7 files recompute)

Same pattern. **Fix**: `public static float fMidX => fBaseOuterX / 2f;`

#### `fBridgeYFront` — Bridge front face Y position (6 files)

| File | Formula |
|------|---------|
| XRails.cs | `fMidY - fGantryBridgeY / 2f` |
| ZAssembly.cs | same |
| MotorMounts.cs | same |
| SpindleMount.cs | same (embedded in multi-line expression) |
| LeadScrews.cs | same |
| DragChains.cs | (uses indirectly) |

**Fix**: `public static float fBridgeYFront => fBaseOuterY / 2f - fGantryBridgeY / 2f;`

### Critical: Silent Breakage Chains

#### 1. Spindle mount position is decoupled from Z carriage position

`SpindleMount.cs` computes "carriage front face" from scratch with a **6-term formula** that never references `fCarriageY` (the carriage depth declared in `ZAssembly.cs`):

```csharp
// SpindleMount.cs:20-25 — independent 6-term chain
fCarriageFront = fMidY
    - fGantryBridgeY / 2f    // bridge front face
    - fZPlateY                // through Z plate
    - fZRailSize              // through Z rail
    - 30f / 2f                // hardcoded half-carriage-depth
    - fZRailSize / 2f;        // (duplicate subtraction — bug)
```

If `fCarriageY` changes in ZAssembly from 30f to 40f, the spindle mount stays put — it has its own hardcoded `30f`.

**Fix**: Carriage front face Y must be a shared computed property:
```csharp
public static float fCarriageFrontY => fBridgeYFront - fZPlateY - fZRailSize - fCarriageY / 2f - fZRailSize / 2f;
```
Both ZAssembly and SpindleMount reference this single source of truth.

#### 2. Spindle Z position is hardcoded, not relative to carriage

```csharp
// SpindleMount.cs:29
fClampZ = fBridgeZ - 30f;  // hardcoded — no reference to fCarriageZ (60f)
```

**Fix**: `public static float fSpindleClampZ => fBridgeZ - fCarriageZ / 2f;`

#### 3. Spindle clamp Y gap is hardcoded

```csharp
// SpindleMount.cs:28
fClampY = fCarriageFront - 40f;  // 40mm gap — what is this?
```

**Fix**: Add parameter `fSpindleOffsetY = 40f` and document what it represents.

#### 4. Z lead screw Y position uses hardcoded 35mm offset

```csharp
// LeadScrews.cs:63
fPlateYFront - 35f;  // 35mm offset from plate front — no parameter
```

**Fix**: Add parameter `fZScrewOffsetY = 35f` or derive from plate/rail geometry.

#### 5. Gantry bridge ignores upright top plate thickness

Upright top plate is 12mm thick. Bridge bottom Z = `fBaseOuterZ + fRailHeight + fUprightZ`. Bridge top plate height = `fBaseOuterZ + fRailHeight + fUprightZ + fPlateThick(12f)`.

The bridge bottom sits at the upright column top, not the plate top — creating a 12mm gap/overlap depending on intent.

**Fix**: `public static float fUprightTopZ => fBaseOuterZ + fRailHeight + fUprightZ + fPlateThick;` — bridge references this.

#### 6. Z plate is embedded into the gantry bridge by 15mm

Z plate back face Y = `fBridgeYFront - fZPlateY(15f)` — the plate sits behind the bridge front face, embedded into the bridge volume. X bearing blocks project forward from `fBridgeYFront` and don't contact the Z plate.

**Fix**: This is a design issue — either the Z plate should mount to bearing blocks (and bearing block position should define plate position), or the plate mounts directly to the bridge face (which would then be Y=`fBridgeYFront`, not `fBridgeYFront - fPlateY`).

### Every Hardcoded Numeric Literal Used for Positioning

| File | Line | Literal | Represents |
|------|------|---------|------------|
| BaseFrame.cs | 95-96 | `40f, 20f` | Foot size/height |
| WorkBed.cs | 14-15 | `40f` | Table overhang beyond work area |
| WorkBed.cs | 33-34 | `40f` | T-slot inset from table edge |
| YRails.cs | 42 | `10f` | Bolt hole extra depth |
| YRails.cs | 50-51 | `40f, 15f` | Bearing block size/height |
| GantryUprights.cs | 38 | `40f` | Gusset size |
| GantryUprights.cs | 62-63 | `12f, 20f` | Top plate thickness, overhang |
| GantryUprights.cs | 69 | `30f` | Bolt circle diameter |
| GantryBridge.cs | 47 | `80f` | Rib spacing (not fRibSpacing) |
| GantryBridge.cs | 82 | `10f` | End boss overhang |
| XRails.cs | 20 | `15f` | Rail diameter (not fZRailSize) |
| XRails.cs | 38-39 | `30f` | Bolt inset from ends |
| ZAssembly.cs | 37 | `20f` | Z rail length shorter than plate |
| ZAssembly.cs | 51-52 | `30f, 60f` | Carriage Y depth, Z height |
| ZAssembly.cs | 65-68 | `20f, 15f` | Carriage bolt offsets |
| SpindleMount.cs | 28 | `40f` | Clamp Y gap from carriage |
| SpindleMount.cs | 29 | `30f` | Clamp Z offset from bridge |
| SpindleMount.cs | 47-48 | `20f, 80f` | Flange thickness, height |
| SpindleMount.cs | 68-69 | `14f, 20f` | Boss diameter, depth |
| MotorMounts.cs | 22 | `30f` | Y motor offset from back |
| MotorMounts.cs | 28 | `30f` | X motor offset from upright |
| MotorMounts.cs | 38 | `20f` | Z motor offset from plate top |
| LeadScrews.cs | 28-29 | `50f` | Y screw inset from base edges |
| LeadScrews.cs | 48-49 | `20f` | X screw inset from uprights |
| LeadScrews.cs | 59-60 | `20f` | Z screw inset from plate ends |
| LeadScrews.cs | 63 | `35f` | Z screw Y offset from plate front |
| DragChains.cs | 23-24 | `10f, 80f` | Tray Z, tray length reduction |
| DragChains.cs | 28-36 | `3f, 6f, 3f` | Floor thickness, width clearance, wall thickness |
| DragChains.cs | 51 | `5f` | X tray Z above bridge |
| DragChains.cs | 69-72 | `5f, 25f, 60f` | Bracket thickness, size, Y positions |

### Dependency Chains That Should Be Solved, Not Repeated

```
fWorkAreaX → fBaseOuterX → fMidX ─┬→ GantryBridge span
                                   ├→ XRails span
                                   ├→ Upright X positions
                                   ├→ YRails X positions
                                   ├→ LeadScrew X positions
                                   ├→ MotorMount X position
                                   └→ DragChain X positions

fBaseOuterZ → +fRailHeight → +fUprightZ → +fGantryBridgeZ/2 = fBridgeZ ─┬→ XRails Z
                                                                          ├→ ZAssembly Z
                                                                          ├→ SpindleMount Z
                                                                          ├→ MotorMounts Z
                                                                          ├→ LeadScrews Z
                                                                          └→ DragChains Z

fBaseOuterY → fMidY → -fGantryBridgeY/2 = fBridgeYFront ─┬→ ZAssembly Y
                                                          ├→ SpindleMount Y
                                                          ├→ XRails Y
                                                          ├→ MotorMounts Y
                                                          └→ LeadScrews Y (Z screw)
```

---

## Part 2: Missing Components vs Real-World CNC

### Structural (30% complete)

| # | Missing Item | Attaches To | Complexity |
|---|-------------|-------------|------------|
| 1 | Machine stand / legs | BaseFrame bottom | Hard |
| 2 | Leveling feet (adjustable) | Corner feet | Medium |
| 3 | Articulated drag chain links (not just U-channel) | DragChains.cs | Hard |
| 4 | Limit switch mounts (6+) | Rail ends (Y, X, Z) | Trivial |
| 5 | Hard end stops at travel limits | Rail ends | Trivial |
| 6 | Cable entry/exit + strain relief | DragChain ends, spindle | Trivial |
| 7 | Dust shoe / dust collection mount | Spindle mount, Z carriage | Medium |
| 8 | Coolant tray / chip collection pan | Below work bed | Medium |
| 9 | Enclosure mounting flanges | Base frame perimeter | Medium |
| 10 | Spoil board (sacrificial layer) | Top of work bed | Trivial |
| 11 | T-slot nuts (in grooves) | Work bed T-slots | Trivial each |

### Motion System (25% complete)

| # | Missing Item | Attaches To | Complexity |
|---|-------------|-------------|------------|
| 12 | Thread geometry on lead screws (currently smooth cylinders) | LeadScrews.cs | Hard |
| 13 | Linear bearing blocks (real profile, not plain boxes) | YRails.cs, XRails.cs | Medium |
| 14 | Rail end supports / rail mounting blocks | YRails.cs, XRails.cs | Trivial |
| 15 | Motor couplers (flexible coupling between motor and screw) | MotorMounts ↔ LeadScrews | Medium |
| 16 | Anti-backlash nut mechanism (split nut + spring) | LeadScrews.cs | Medium |
| 17 | Belt drive option (pulleys, belt, tensioner) | MotorMounts, LeadScrews | Hard |
| 18 | Shaft collars + thrust bearings at screw ends | LeadScrews.cs | Trivial |
| 19 | Real pillow block geometry (not plain boxes) | LeadScrews.cs | Medium |
| 20 | Grease fittings (Zerk nipples) | Bearings, nut blocks | Trivial |

### Spindle & Tooling (10% complete)

| # | Missing Item | Attaches To | Complexity |
|---|-------------|-------------|------------|
| 21 | Spindle motor body (the actual motor, not just the clamp) | Inside clamp ring | Medium |
| 22 | ER collet nut below spindle | Spindle bottom | Medium |
| 23 | Tool / end mill placeholder | Collet nut | Trivial |
| 24 | Spindle cooling (water jacket or fan fins) | Spindle body | Medium |
| 25 | Spindle cable routing clips | Spindle → Z carriage | Trivial |
| 26 | RPM sensor mount | Spindle top | Trivial |

### Electronics & Wiring (0% complete)

| # | Missing Item | Attaches To | Complexity |
|---|-------------|-------------|------------|
| 27 | Stepper motor bodies (NEMA 23, behind mounting plates) | MotorMounts.cs | Medium |
| 28 | Motor wiring clips / cable management | MotorMounts → DragChains | Trivial |
| 29 | Control box / electronics enclosure | Base frame or stand | Hard |
| 30 | Power supply mounting bracket | Control box | Medium |
| 31 | Cable glands (PG7/PG9 fittings) | Drag chain ends | Trivial |
| 32 | E-stop button (mushroom head) | Base frame front or upright | Trivial |

### Hardware Detail (5% complete)

| # | Missing Item | Attaches To | Complexity |
|---|-------------|-------------|------------|
| 33 | Bolt heads / nut geometry at every bolted joint | All components | Trivial each (50+ locations) |
| 34 | Threaded vs clearance hole distinction | All bolt holes | Medium |
| 35 | Counterbores / spotfaces | All bolted joints | Trivial each |
| 36 | Dowel pins / alignment features | Mating part interfaces | Trivial |
| 37 | Set screws in shaft collars | LeadScrews.cs | Trivial |
| 38 | Snap ring grooves at bearing positions | Lead screw ends | Trivial |

### Workholding (10% complete)

| # | Missing Item | Attaches To | Complexity |
|---|-------------|-------------|------------|
| 39 | T-slot nuts (array in slots) | WorkBed.cs | Trivial each |
| 40 | Step clamps / hold-downs | WorkBed T-slots | Medium |
| 41 | Machinist vise placeholder | WorkBed T-slots | Medium |
| 42 | Reference edge / alignment fence | WorkBed edge | Trivial |

### Safety (0% complete)

| # | Missing Item | Attaches To | Complexity |
|---|-------------|-------------|------------|
| 43 | Limit switch bodies (6 total) | Rail ends (Y, X, Z) | Trivial |
| 44 | E-stop button body | Frame front | Trivial |
| 45 | Drag chain top covers (enclosed cables) | DragChains.cs | Medium |
| 46 | Lead screw bellows / way covers | LeadScrews.cs, rails | Medium |
| 47 | Guard panels / enclosure | Base frame or stand | Hard |

### Manufacturing & Verification (0% complete)

| # | Missing Item | Affects | Complexity |
|---|-------------|---------|------------|
| 48 | 3D printing clearances between mating parts (0.2-0.4mm) | All interfaces | Medium |
| 49 | Assembly hardware modeled (bolts, nuts, washers) | All bolted joints | Medium |
| 50 | Print orientation / support considerations | Overhanging features | Hard |
| 51 | Interference checking between moving parts at travel extremes | Entire assembly | Hard |
| 52 | Travel limit verification (does Z actually reach fWorkAreaZ?) | All axes | Medium |
| 53 | Bolt hole alignment verification between mating parts | All interfaces | Medium |
| 54 | Fastener length / depth verification | All bolted joints | Medium |

---

## Part 3: What "Real Computational Engineering" Means Here

### Current state
Parametric CAD in voxels: dimensions are constants, positions are recomputed arithmetic, components fit because of shared naming conventions.

### Target state
Constraint-based computational model where:

1. **Every physical interface is a declared constraint, not recomputed arithmetic.**
   - "Gantry bridge rests on upright top surface" → solver computes bridge Z
   - "Spindle clamp is centered on Z carriage front face" → solver computes clamp Y, Z
   - Change one parameter, everything that depends on it re-solves correctly.

2. **Dimensions are the output of engineering calculations, not hand-picked constants.**
   - Wall thickness = f(span, load, material, max_deflection)
   - Rib spacing = f(box_dimensions, buckling_load)
   - Bolt size/count = f(shear_force, bolt_material)

3. **The system verifies correctness, not just renders shapes.**
   - Travel range actually achieved vs claimed
   - Bolt patterns actually align between mating parts
   - Moving parts don't collide at travel extremes
   - Clearances are appropriate for 3D printing tolerances

### Implementation priority

**Phase A — Constraint Graph (foundation)**
Convert all duplicated position computations to shared computed properties. Replace hardcoded literals with named parameters. Every component reads from the constraint graph, not local recomputation.

**Phase B — Interface Verification**
Add checks that mating surfaces align, bolt patterns match, and travel ranges are achievable. Flag violations at build time.

**Phase C — Load-Driven Dimensioning**
Add beam deflection equations for gantry bridge and uprights. Wall thickness and rib spacing become computed outputs, not inputs.

**Phase D — Missing Components**
Add items from Part 2 in priority order: safety features first (limit switches, E-stop), then motion system details (couplers, real bearings, thread forms), then electronics, then workholding.

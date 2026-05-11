namespace PicoGK;

public static partial class Picocnc
{
    // =====================================================================
    // STRUCTURAL BEAM SOLVER
    //
    // Computes minimum dimensions from engineering first principles using
    // beam theory. Replaces hand-picked constants with calculated dimensions
    // based on material properties, loads, and deflection limits.
    //
    // All math is self-contained — no external dependencies.
    // =====================================================================

    // === Material Properties ===
    // Aluminum 6061-T6 (typical for CNC frames)
    public const float fYoungsModulusAluminum = 69_000f;    // MPa (N/mm²)
    public const float fYieldStrengthAluminum  = 276f;       // MPa
    public const float fDensityAluminum        = 2.70f;      // g/cm³

    // Steel (for rails, lead screws)
    public const float fYoungsModulusSteel     = 200_000f;   // MPa
    public const float fYieldStrengthSteel      = 350f;       // MPa (mild steel)

    // MDF (spoil board)
    public const float fYoungsModulusMDF       = 3_000f;     // MPa

    // === Load Assumptions ===
    // Cutting forces — typical for a hobby/desktop CNC router
    // Cutting aluminum with 6mm end mill ≈ 50-100N
    // Cutting wood with 6mm end mill ≈ 20-50N
    public const float fCuttingForceXY    = 100f;   // N — horizontal cutting force at tool tip
    public const float fCuttingForceZ     = 50f;    // N — vertical plunge force
    public const float fGantryWeight      = 0f;     // self-weight (add later)
    public const float fSafetyFactor      = 3.0f;   // engineering safety factor

    // =====================================================================
    // PUBLIC RESULT FIELDS — populated during RunBeamAnalysis()
    // =====================================================================

    public static float s_fBridgeDeflectionMm;
    public static float s_fBridgeSafetyFactor;
    public static float s_fLeadScrewBucklingSafety;
    public static float s_fUprightBucklingSafety;
    public static float s_fUprightSlenderness;
    public static float s_fBaseRibBucklingSafety;

    // =====================================================================
    // MASTER ENTRY POINT
    // =====================================================================

    /// <summary>
    /// Run all structural checks and report results via Library.Log.
    /// Call after VerifyCollisions() and before STL export.
    /// </summary>
    public static void RunBeamAnalysis()
    {
        Log("\n============================================================");
        Log("===  BEAM STRUCTURAL ANALYSIS  =============================");
        Log("============================================================");

        Log($"  Material: Aluminum 6061-T6  (E={fYoungsModulusAluminum} MPa, " +
            $"yield={fYieldStrengthAluminum} MPa)");
        Log($"  Loads: XY force={fCuttingForceXY}N, Z force={fCuttingForceZ}N, " +
            $"safety factor={fSafetyFactor:F1}x");
        Log("");

        AnalyzeGantryBridge();
        AnalyzeBaseFrame();
        AnalyzeUprights();
        AnalyzeLeadScrews();

        // --- Finite Element Analysis (in-process, BriefFiniteElement.NET) ---
        RunFEAnalysis();

        // --- CalculiX batch FEA (external subprocess) ---
        RunCalculixAnalysis();

        Log("\n===  BEAM ANALYSIS COMPLETE  ===============================");
        Log("============================================================\n");
    }

    // =====================================================================
    // 1. GANTRY BRIDGE ANALYSIS
    //
    // Hollow rectangular box beam spanning between the two uprights.
    // Model: simply supported beam with center point load.
    // =====================================================================

    static void AnalyzeGantryBridge()
    {
        float fSpan = fBridgeSpanX;  // mm — from Constraints (between upright centers)

        // Outer dimensions
        float b = fGantryBridgeY;    // width (Y)
        float h = fGantryBridgeZ;    // height (Z)
        float t = fGantryWallThick;  // wall thickness

        // Inner dimensions (hollow section)
        float bi = MathF.Max(0.1f, b - 2f * t);
        float hi = MathF.Max(0.1f, h - 2f * t);

        // Moment of inertia for hollow rectangular section (mm⁴)
        // I = (b_outer * h_outer³ - b_inner * h_inner³) / 12
        float I = (b * h * h * h - bi * hi * hi * hi) / 12f;

        // Deflection under cutting force at center
        float F = fCuttingForceXY * fSafetyFactor;
        float E = fYoungsModulusAluminum;
        float deflection = (F * fSpan * fSpan * fSpan) / (48f * E * I);

        Log("=== GANTRY BRIDGE ANALYSIS ===");
        Log($"  Span: {fSpan:F0} mm, Section: {b:F0}x{h:F0}mm, Wall: {t:F0}mm");
        Log($"  I = {I / 1e6f:F2} × 10⁶ mm⁴");
        Log($"  Deflection at {F:F0}N center load: {deflection:F3} mm");
        Log($"  L/δ = {fSpan / deflection:F0} (target > 500 for CNC rigidity)");

        // Stress check
        float stress = (F * fSpan * h) / (8f * I);
        float stressSafety = fYieldStrengthAluminum / stress;
        Log($"  Max bending stress: {stress:F1} MPa (yield: {fYieldStrengthAluminum} MPa)");
        Log($"  Safety factor vs yield: {stressSafety:F1}x (target > {fSafetyFactor:F1})");

        // What wall thickness would give L/1000 stiffness?
        float targetDefl = fSpan / 1000f;
        float tRequired = FindWallThicknessForDeflection(b, h, fSpan, F, E, targetDefl);
        Log($"  Wall thickness for L/1000 stiffness: {tRequired:F1} mm");

        // What wall thickness to stay below yield/safetyFactor?
        float tYield = FindWallThicknessForStress(b, h, fSpan, F, E,
            fYieldStrengthAluminum / fSafetyFactor);
        Log($"  Wall thickness for yield/{fSafetyFactor:F0}: {tYield:F1} mm");

        float tRec = MathF.Max(tRequired, tYield);
        string verdict = tRec <= t ? "PASS" : "UNDERSIZED";
        Log($"  RECOMMENDATION: min wall = {tRec:F1} mm (current: {t:F0} mm) [{verdict}]");

        // Store results for web API
        s_fBridgeDeflectionMm = deflection;
        s_fBridgeSafetyFactor = stressSafety;

        Log("");
    }

    // =====================================================================
    // BINARY SEARCH HELPERS
    //
    // The moment of inertia equation can't be inverted analytically for
    // wall thickness, so we use iterative binary search.
    // =====================================================================

    /// <summary>
    /// Find the wall thickness needed to achieve a target midspan deflection.
    /// </summary>
    static float FindWallThicknessForDeflection(
        float b, float h, float L, float F, float E, float targetDefl)
    {
        float tMin = 0.5f;
        float tMax = h / 2f - 1f; // can't be thicker than half the height
        if (tMax <= tMin) return tMin;

        for (int iter = 0; iter < 40; iter++)
        {
            float t = (tMin + tMax) / 2f;
            float bi = MathF.Max(0.001f, b - 2f * t);
            float hi = MathF.Max(0.001f, h - 2f * t);
            float I = (b * h * h * h - bi * hi * hi * hi) / 12f;
            if (I <= 0f) I = 1f; // degenerate guard

            float defl = (F * L * L * L) / (48f * E * I);
            if (defl > targetDefl)
                tMin = t; // need thicker
            else
                tMax = t; // can be thinner
        }
        return (tMin + tMax) / 2f;
    }

    /// <summary>
    /// Find the wall thickness needed to stay below a target bending stress.
    /// </summary>
    static float FindWallThicknessForStress(
        float b, float h, float L, float F, float E, float targetStress)
    {
        float tMin = 0.5f;
        float tMax = h / 2f - 1f;
        if (tMax <= tMin) return tMin;

        for (int iter = 0; iter < 40; iter++)
        {
            float t = (tMin + tMax) / 2f;
            float bi = MathF.Max(0.001f, b - 2f * t);
            float hi = MathF.Max(0.001f, h - 2f * t);
            float I = (b * h * h * h - bi * hi * hi * hi) / 12f;
            if (I <= 0f) I = 1f;

            float stress = (F * L * h) / (8f * I);
            if (stress > targetStress)
                tMin = t; // need thicker
            else
                tMax = t; // can be thinner
        }
        return (tMin + tMax) / 2f;
    }

    // =====================================================================
    // 2. BASE FRAME RIB ANALYSIS
    //
    // The base frame uses internal vertical ribs for stiffness.
    // Each rib is modeled as a vertical rectangular plate/column.
    // Check critical buckling load.
    // =====================================================================

    static void AnalyzeBaseFrame()
    {
        // Rib buckling check
        // Each rib is a vertical rectangular plate: thickness = fRibThick,
        // height = fBaseOuterZ, effective width = fRibSpacing

        float t = fRibThick;         // rib plate thickness
        float h = fBaseOuterZ;       // rib height = base height
        float w = fRibSpacing;       // effective width of plate segment per rib

        // Moment of inertia about the weak (vertical) axis of the rib
        // Cross-section viewed from above: width=w, depth=t
        // I = (w * t³) / 12  — bending about axis parallel to the wall
        float I = (w * t * t * t) / 12f;

        float K = 0.5f; // fixed-fixed end condition (rib constrained top and bottom)
        float L = h;
        float E = fYoungsModulusAluminum;
        float Pcr = MathF.PI * MathF.PI * E * I / ((K * L) * (K * L));

        int nRibsPerSide = (int)(fBaseOuterY / fRibSpacing);

        Log("=== BASE FRAME RIB ANALYSIS ===");
        Log($"  Rib: {t:F0}mm thick, {h:F0}mm tall, {w:F0}mm spacing");
        Log($"  Buckling load: {Pcr:F0} N ({Pcr / 9.81f:F1} kg)");
        Log($"  Rib spacing at {fRibSpacing:F0}mm — ~{nRibsPerSide} ribs per frame side");

        // Compare to estimated machine weight on base
        float fEstimatedWeight = fGantryBridgeY * fGantryBridgeZ * fBridgeSpanX * fDensityAluminum / 1000f;
        float fRibBucklingSafety = Pcr / (fEstimatedWeight * 9.81f * fSafetyFactor);
        Log($"  Estimated gantry weight: ~{fEstimatedWeight:F1} kg");
        Log($"  Per-rib buckling safety: {fRibBucklingSafety:F1}x");

        // Store results for web API
        s_fBaseRibBucklingSafety = fRibBucklingSafety;

        Log("");
    }

    // =====================================================================
    // 3. UPRIGHT COLUMN ANALYSIS
    //
    // Uprights are vertical rectangular columns carrying the gantry weight
    // plus cutting forces. Check Euler buckling under compressive load.
    // =====================================================================

    static void AnalyzeUprights()
    {
        // Upright cross-section: fUprightX (smaller) x fUprightY (larger)
        // Height: fUprightZ
        float a = fUprightX;  // smaller dimension (along bridge span X)
        float b = fUprightY;  // larger dimension (along bridge depth Y)
        float L = fUprightZ;  // column height

        float E = fYoungsModulusAluminum;

        // Euler buckling about the weak axis (smaller dimension a)
        // Imin = (b * a³) / 12
        float Imin = (b * a * a * a) / 12f;

        // Fixed at base (bolted to Y bearing), fixed at top (bridge constrains)
        float K = 0.5f;
        float Pcr = MathF.PI * MathF.PI * E * Imin / ((K * L) * (K * L));

        // Rough gantry bridge mass estimate (hollow section volume * density)
        float fSpan = fBridgeSpanX;
        float bo = fGantryBridgeY;
        float ho = fGantryBridgeZ;
        float wt = fGantryWallThick;
        float bi = MathF.Max(0f, bo - 2f * wt);
        float hi = MathF.Max(0f, ho - 2f * wt);
        float fBridgeVolume = (bo * ho - bi * hi) * fSpan; // mm³
        float fBridgeMass = fBridgeVolume * fDensityAluminum / 1000f; // grams -> kg
        float fZAssyMass = 5.0f; // rough estimate for Z plate + spindle + motor

        float fTotalPerUpright = (fBridgeMass + fZAssyMass) / 2f; // two uprights share load
        float fUprightLoad = fTotalPerUpright * 9.81f; // N

        // Slenderness ratio
        float area = a * b;
        float radiusGyration = MathF.Sqrt(Imin / area);
        float slenderness = (K * L) / radiusGyration;

        Log("=== UPRIGHT ANALYSIS ===");
        Log($"  Column: {a:F0}x{b:F0}mm, height: {L:F0}mm");
        Log($"  Critical buckling load: {Pcr:F0} N ({Pcr / 9.81f:F1} kg)");
        Log($"  Estimated gantry+bridge weight: ~{fBridgeMass:F1} kg");
        Log($"  Estimated load per upright: ~{fUprightLoad:F0} N ({fTotalPerUpright:F1} kg)");
        Log($"  Slenderness ratio: {slenderness:F1} (stocky < 50, slender > 100)");

        float buckleSafety = Pcr / (fUprightLoad * fSafetyFactor + fCuttingForceZ * fSafetyFactor);
        Log($"  Buckling safety factor: {buckleSafety:F1}x (target > {fSafetyFactor:F1})");

        string slendernessVerdict;
        if (slenderness < 50f)
            slendernessVerdict = "stocky (buckling not a concern)";
        else if (slenderness < 100f)
            slendernessVerdict = "intermediate (verify with Johnson formula)";
        else
            slendernessVerdict = "slender (Euler buckling governs)";
        Log($"  Verdict: {slendernessVerdict}");

        // Store results for web API
        s_fUprightBucklingSafety = buckleSafety;
        s_fUprightSlenderness = slenderness;

        Log("");
    }

    // =====================================================================
    // 4. LEAD SCREW ANALYSIS
    //
    // T12 lead screw checks:
    //   1. Critical speed (whirling resonance)
    //   2. Column buckling under axial compression
    // =====================================================================

    static void AnalyzeLeadScrews()
    {
        float d = fLeadScrewDia;  // 12mm nominal
        float L = fBaseOuterY;    // Y-axis screw is longest span

        float E = fYoungsModulusSteel;

        // Circular section properties
        float I = MathF.PI * d * d * d * d / 64f;
        float A = MathF.PI * d * d / 4f;

        // Critical buckling load (Euler, pinned-pinned K = 1.0)
        float K = 1.0f;
        float Pcr = MathF.PI * MathF.PI * E * I / ((K * L) * (K * L));

        // Critical speed (simply supported, first whirling mode)
        // N_cr = (π * d² / (8 * L²)) * sqrt(E / ρ) * 60  [RPM]
        float rho = 7850e-12f; // steel density in kg/mm³
        float Ncr = (MathF.PI * d * d) / (8f * L * L) * MathF.Sqrt(E / rho) * 60f; // RPM

        // Motor axial force estimate
        // NEMA 23 typical: 1.2 Nm holding torque
        // Axial force: F = 2π * η * T / p
        //   η = 0.3 (lead screw efficiency), T = 1.2 Nm, p = 2 mm (T12 pitch)
        float fMotorTorque = 1.2f * 1000f;  // N·mm
        float fLeadPitch = 2.0f;             // mm/rev (T12 typical)
        float fEfficiency = 0.3f;
        float fAxialForce = 2f * MathF.PI * fEfficiency * fMotorTorque / fLeadPitch;

        float fBucklingSafety = Pcr / (fAxialForce * fSafetyFactor);

        Log("=== LEAD SCREW ANALYSIS ===");
        Log($"  T12 screw, {L:F0}mm length (Y-axis — longest span)");
        Log($"  Section: d={d:F0}mm, A={A:F1}mm², I={I:F1}mm⁴");
        Log($"  Critical buckling load: {Pcr:F0} N ({Pcr / 9.81f:F1} kg)");
        Log($"  Critical speed (whirling): {Ncr:F0} RPM");
        Log($"  Motor axial force (1.2Nm @ T12x2): {fAxialForce:F0} N");
        Log($"  Buckling safety factor: {fBucklingSafety:F1}x (target > {fSafetyFactor:F1})");

        // Store results for web API
        s_fLeadScrewBucklingSafety = fBucklingSafety;

        // Practical guidance
        if (Ncr < 500f)
            Log($"  WARNING: Whirling speed below 500 RPM — consider " +
                $"end-bearing or larger screw diameter for high-speed rapids.");
        else
            Log($"  Whirling speed OK for typical stepper motor operation (< {Ncr * 0.8f:F0} RPM safe limit).");

        Log("");
    }
}

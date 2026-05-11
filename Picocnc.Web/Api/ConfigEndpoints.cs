using System.Text.Json;
using PicoGK;

namespace PicoCNCWeb.Api;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this WebApplication app)
    {
        app.MapGet("/api/config", GetConfig);
        app.MapPost("/api/config", PostConfig);
    }

    static IResult GetConfig()
    {
        var groups = new Dictionary<string, List<object>>();

        // Envelope
        groups["Envelope"] = new List<object>
        {
            ParamObj("fWorkAreaX", "Work Area X", Picocnc.fWorkAreaX, "mm", 200, 2000, 50),
            ParamObj("fWorkAreaY", "Work Area Y", Picocnc.fWorkAreaY, "mm", 200, 1500, 50),
            ParamObj("fWorkAreaZ", "Work Area Z", Picocnc.fWorkAreaZ, "mm", 50, 400, 10),
            ParamObj("fBaseOuterZ", "Base Height", Picocnc.fBaseOuterZ, "mm", 50, 300, 10),
        };

        // Wall Thickness
        groups["Wall Thickness"] = new List<object>
        {
            ParamObj("fBaseWallThick", "Base Wall", Picocnc.fBaseWallThick, "mm", 5, 40, 1),
            ParamObj("fRibThick", "Rib Thickness", Picocnc.fRibThick, "mm", 3, 30, 1),
            ParamObj("fGantryWallThick", "Gantry Wall", Picocnc.fGantryWallThick, "mm", 3, 20, 1),
            ParamObj("fRibSpacing", "Rib Spacing", Picocnc.fRibSpacing, "mm", 40, 300, 10),
        };

        // Rails
        groups["Rails"] = new List<object>
        {
            ParamObj("fRailWidth", "Rail Width", Picocnc.fRailWidth, "mm", 10, 45, 1),
            ParamObj("fRailHeight", "Rail Height", Picocnc.fRailHeight, "mm", 10, 45, 1),
            ParamObj("fRailInsetX", "Rail Inset", Picocnc.fRailInsetX, "mm", 10, 100, 5),
            ParamObj("fBoltHoleDia", "Bolt Hole Dia", Picocnc.fBoltHoleDia, "mm", 3, 12, 0.1f),
            ParamObj("fBoltSpacingY", "Bolt Spacing Y", Picocnc.fBoltSpacingY, "mm", 40, 200, 10),
        };

        // Uprights
        groups["Uprights"] = new List<object>
        {
            ParamObj("fUprightX", "Upright X", Picocnc.fUprightX, "mm", 20, 80, 5),
            ParamObj("fUprightY", "Upright Y", Picocnc.fUprightY, "mm", 30, 120, 5),
            ParamObj("fUprightZ", "Upright Z", Picocnc.fUprightZ, "mm", 100, 400, 10),
        };

        // Gantry Bridge
        groups["Gantry Bridge"] = new List<object>
        {
            ParamObj("fGantryBridgeY", "Bridge Y", Picocnc.fGantryBridgeY, "mm", 30, 120, 5),
            ParamObj("fGantryBridgeZ", "Bridge Z", Picocnc.fGantryBridgeZ, "mm", 40, 160, 5),
        };

        // Z-Axis
        groups["Z-Axis"] = new List<object>
        {
            ParamObj("fZPlateX", "Z Plate X", Picocnc.fZPlateX, "mm", 40, 160, 5),
            ParamObj("fZPlateY", "Z Plate Y", Picocnc.fZPlateY, "mm", 5, 30, 1),
            ParamObj("fZPlateZ", "Z Plate Z", Picocnc.fZPlateZ, "mm", 100, 400, 10),
            ParamObj("fZRailSpace", "Z Rail Space", Picocnc.fZRailSpace, "mm", 20, 100, 5),
            ParamObj("fZRailSize", "Z Rail Size", Picocnc.fZRailSize, "mm", 8, 30, 1),
        };

        // Spindle
        groups["Spindle"] = new List<object>
        {
            ParamObj("fSpindleOD", "Spindle OD", Picocnc.fSpindleOD, "mm", 40, 120, 1),
            ParamObj("fClampOD", "Clamp OD", Picocnc.fClampOD, "mm", 50, 140, 1),
            ParamObj("fClampHeight", "Clamp Height", Picocnc.fClampHeight, "mm", 30, 120, 5),
            ParamObj("fClampSlit", "Clamp Slit", Picocnc.fClampSlit, "mm", 1, 8, 0.5f),
        };

        // Motor Mounts
        groups["Motor Mounts"] = new List<object>
        {
            ParamObj("fNema23Width", "NEMA23 Width", Picocnc.fNema23Width, "mm", 40, 86, 1),
            ParamObj("fNema23BoltCircle", "Bolt Circle", Picocnc.fNema23BoltCircle, "mm", 30, 70, 0.1f),
            ParamObj("fNema23ShaftBore", "Shaft Bore", Picocnc.fNema23ShaftBore, "mm", 5, 20, 1),
            ParamObj("fMountPlateThick", "Plate Thick", Picocnc.fMountPlateThick, "mm", 3, 20, 1),
        };

        // Lead Screws
        groups["Lead Screws"] = new List<object>
        {
            ParamObj("fLeadScrewDia", "Screw Dia", Picocnc.fLeadScrewDia, "mm", 8, 25, 1),
            ParamObj("fNutBlockSize", "Nut Size", Picocnc.fNutBlockSize, "mm", 10, 50, 1),
        };

        // T-Slots
        groups["T-Slots"] = new List<object>
        {
            ParamObj("fTSlotUpperW", "Upper Width", Picocnc.fTSlotUpperW, "mm", 8, 40, 1),
            ParamObj("fTSlotLowerW", "Lower Width", Picocnc.fTSlotLowerW, "mm", 4, 20, 1),
            ParamObj("fTSlotDepth", "Slot Depth", Picocnc.fTSlotDepth, "mm", 4, 20, 1),
            ParamObj("fTSlotSpacing", "Slot Spacing", Picocnc.fTSlotSpacing, "mm", 40, 200, 10),
        };

        // Work Bed
        groups["Work Bed"] = new List<object>
        {
            ParamObj("fTableThick", "Table Thickness", Picocnc.fTableThick, "mm", 5, 50, 1),
        };

        // Drag Chains
        groups["Drag Chains"] = new List<object>
        {
            ParamObj("fChainWidth", "Chain Width", Picocnc.fChainWidth, "mm", 10, 60, 5),
            ParamObj("fChainHeight", "Chain Height", Picocnc.fChainHeight, "mm", 10, 40, 5),
        };

        // Resolution
        groups["Resolution"] = new List<object>
        {
            ParamObj("fVoxelSizeMM", "Voxel Size", Picocnc.fVoxelSizeMM, "mm", 0.2f, 5.0f, 0.1f),
        };

        // Material & Budget (enum params)
        groups["Material & Budget"] = new List<object>
        {
            EnumParamObj("eCutMaterial", "Material to Cut",
                Picocnc.eCutMaterial.ToString(),
                new[] { "Wood", "Plastic", "Aluminum", "Steel" }),
            EnumParamObj("eBudgetTier", "Budget Tier",
                Picocnc.eBudgetTier.ToString(),
                new[] { "Budget", "Standard", "Premium" }),
        };

        return Results.Ok(new { parameters = groups });
    }

    static async Task<IResult> PostConfig(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        string json = await reader.ReadToEndAsync();
        var updates = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        if (updates is null)
            return Results.BadRequest(new { error = "Invalid JSON body" });

        foreach (var kv in updates)
        {
            switch (kv.Key)
            {
                case "fWorkAreaX": Picocnc.fWorkAreaX = kv.Value.GetSingle(); break;
                case "fWorkAreaY": Picocnc.fWorkAreaY = kv.Value.GetSingle(); break;
                case "fWorkAreaZ": Picocnc.fWorkAreaZ = kv.Value.GetSingle(); break;
                case "fBaseOuterZ": Picocnc.fBaseOuterZ = kv.Value.GetSingle(); break;
                case "fBaseWallThick": Picocnc.fBaseWallThick = kv.Value.GetSingle(); break;
                case "fRibThick": Picocnc.fRibThick = kv.Value.GetSingle(); break;
                case "fGantryWallThick": Picocnc.fGantryWallThick = kv.Value.GetSingle(); break;
                case "fRibSpacing": Picocnc.fRibSpacing = kv.Value.GetSingle(); break;
                case "fRailWidth": Picocnc.fRailWidth = kv.Value.GetSingle(); break;
                case "fRailHeight": Picocnc.fRailHeight = kv.Value.GetSingle(); break;
                case "fRailInsetX": Picocnc.fRailInsetX = kv.Value.GetSingle(); break;
                case "fBoltHoleDia": Picocnc.fBoltHoleDia = kv.Value.GetSingle(); break;
                case "fBoltSpacingY": Picocnc.fBoltSpacingY = kv.Value.GetSingle(); break;
                case "fUprightX": Picocnc.fUprightX = kv.Value.GetSingle(); break;
                case "fUprightY": Picocnc.fUprightY = kv.Value.GetSingle(); break;
                case "fUprightZ": Picocnc.fUprightZ = kv.Value.GetSingle(); break;
                case "fGantryBridgeY": Picocnc.fGantryBridgeY = kv.Value.GetSingle(); break;
                case "fGantryBridgeZ": Picocnc.fGantryBridgeZ = kv.Value.GetSingle(); break;
                case "fZPlateX": Picocnc.fZPlateX = kv.Value.GetSingle(); break;
                case "fZPlateY": Picocnc.fZPlateY = kv.Value.GetSingle(); break;
                case "fZPlateZ": Picocnc.fZPlateZ = kv.Value.GetSingle(); break;
                case "fZRailSpace": Picocnc.fZRailSpace = kv.Value.GetSingle(); break;
                case "fZRailSize": Picocnc.fZRailSize = kv.Value.GetSingle(); break;
                case "fSpindleOD": Picocnc.fSpindleOD = kv.Value.GetSingle(); break;
                case "fClampOD": Picocnc.fClampOD = kv.Value.GetSingle(); break;
                case "fClampHeight": Picocnc.fClampHeight = kv.Value.GetSingle(); break;
                case "fClampSlit": Picocnc.fClampSlit = kv.Value.GetSingle(); break;
                case "fNema23Width": Picocnc.fNema23Width = kv.Value.GetSingle(); break;
                case "fNema23BoltCircle": Picocnc.fNema23BoltCircle = kv.Value.GetSingle(); break;
                case "fNema23ShaftBore": Picocnc.fNema23ShaftBore = kv.Value.GetSingle(); break;
                case "fMountPlateThick": Picocnc.fMountPlateThick = kv.Value.GetSingle(); break;
                case "fLeadScrewDia": Picocnc.fLeadScrewDia = kv.Value.GetSingle(); break;
                case "fNutBlockSize": Picocnc.fNutBlockSize = kv.Value.GetSingle(); break;
                case "fTSlotUpperW": Picocnc.fTSlotUpperW = kv.Value.GetSingle(); break;
                case "fTSlotLowerW": Picocnc.fTSlotLowerW = kv.Value.GetSingle(); break;
                case "fTSlotDepth": Picocnc.fTSlotDepth = kv.Value.GetSingle(); break;
                case "fTSlotSpacing": Picocnc.fTSlotSpacing = kv.Value.GetSingle(); break;
                case "fTableThick": Picocnc.fTableThick = kv.Value.GetSingle(); break;
                case "fChainWidth": Picocnc.fChainWidth = kv.Value.GetSingle(); break;
                case "fChainHeight": Picocnc.fChainHeight = kv.Value.GetSingle(); break;
                case "fVoxelSizeMM": Picocnc.fVoxelSizeMM = kv.Value.GetSingle(); break;
                case "eCutMaterial": Picocnc.eCutMaterial = Enum.Parse<Picocnc.MaterialToCut>(kv.Value.GetString()!); break;
                case "eBudgetTier": Picocnc.eBudgetTier = Enum.Parse<Picocnc.BudgetTier>(kv.Value.GetString()!); break;
            }
        }

        return Results.Ok(new { status = "ok" });
    }

    static object ParamObj(string key, string label, float value, string unit,
                           float min, float max, float step) => new
    {
        key, label, value, unit, min, max, step
    };

    static object EnumParamObj(string key, string label, string value, string[] options) => new
    {
        key, label, value, options
    };
}

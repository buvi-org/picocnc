using System.Diagnostics;
using System.Text.Json;
using System.Net.WebSockets;
using System.Text;
using PicoGK;
using PicoCNCWeb.Models;

namespace PicoCNCWeb.Services;

public class CncBuilder
{
    readonly string _strScratchDir;

    public CncBuilder()
    {
        _strScratchDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PicoCNC_Web");
        Directory.CreateDirectory(_strScratchDir);
    }

    /// <summary>
    /// Runs the full PicoCNC pipeline and streams progress events over a WebSocket.
    /// </summary>
    public async Task BuildAndStreamAsync(WebSocket ws, float? fVoxelOverride = null)
    {
        float fVoxel = fVoxelOverride ?? Picocnc.fVoxelSizeMM;
        var sw = Stopwatch.StartNew();
        var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        async Task SendEvent(object evt)
        {
            var json = JsonSerializer.Serialize(evt, jsonOpts);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        async Task SendStage(string stage)
        {
            await SendEvent(new { type = "stage", stage });
        }

        try
        {
            await SendStage("Initializing PicoGK engine...");

            using Library oLib = new(fVoxel);
            Library.RegisterGlobalLibrary(oLib);

            try
            {
                // COTS parts selection
                await SendStage("Selecting COTS parts...");

                var req = new Picocnc.CNCRequirements
                {
                    fWorkAreaX = Picocnc.fWorkAreaX,
                    fWorkAreaY = Picocnc.fWorkAreaY,
                    fWorkAreaZ = Picocnc.fWorkAreaZ,
                    eMaterial  = Picocnc.eCutMaterial,
                    eBudget    = Picocnc.eBudgetTier
                };

                Picocnc.CNCSelectedParts parts = Picocnc.SelectParts(req);

                // Build all components
                await SendStage("Building machine geometry (this may take a while)...");
                Voxels voxMachine = Picocnc.voxConstruct();

                // Collision verification
                await SendStage("Verifying collisions...");
                Picocnc.VerifyCollisions();

                // Beam structural analysis
                await SendStage("Running structural analysis...");
                Picocnc.RunBeamAnalysis();

                // Export assembly STL
                await SendStage("Exporting Assembly...");
                string strAssemblyPath = Path.Combine(_strScratchDir, "Assembly.stl");
                voxMachine.mshAsMesh().SaveToStlFile(strAssemblyPath);

                // Export individual components — stream each as it completes
                var aComponents = new List<ComponentInfo>();
                var componentNames = new (string, Func<Voxels>)[] {
                    ("BaseFrame",      Picocnc.voxConstructBaseFrame),
                    ("WorkBed",        Picocnc.voxConstructWorkBed),
                    ("YRails",         Picocnc.voxConstructYRails),
                    ("GantryUprights", Picocnc.voxConstructUprights),
                    ("GantryBridge",   Picocnc.voxConstructGantryBridge),
                    ("XRails",         Picocnc.voxConstructXRails),
                    ("ZAssembly",      Picocnc.voxConstructZAssembly),
                    ("SpindleMount",   Picocnc.voxConstructSpindleMount),
                    ("MotorMounts",    Picocnc.voxConstructMotorMounts),
                    ("LeadScrews",     Picocnc.voxConstructLeadScrews),
                    ("DragChains",     Picocnc.voxConstructDragChains),
                    ("Safety",         Picocnc.voxConstructSafety),
                };

                foreach (var (name, builder) in componentNames)
                {
                    await SendStage($"Exporting {name}...");
                    string strPath = Path.Combine(_strScratchDir, $"{name}.stl");
                    builder().mshAsMesh().SaveToStlFile(strPath);

                    var comp = new ComponentInfo
                    {
                        strName   = name,
                        strStlUrl = $"/api/stl/{name}",
                        bVisible  = true
                    };
                    aComponents.Add(comp);

                    // Send component-ready event so frontend loads it immediately
                    await SendEvent(new {
                        type = "component",
                        name,
                        stlUrl = $"/api/stl/{name}"
                    });
                }

                sw.Stop();

                var result = new BuildResult
                {
                    strStatus          = "ok",
                    fDurationSec        = (float)sw.Elapsed.TotalSeconds,
                    aComponents         = aComponents,
                    strAssemblyStlUrl   = "/api/stl/Assembly",
                    aBom                = BuildBomFromParts(parts),
                    oAnalysis           = GetLatestAnalysis(),
                    oCollisions         = GetLatestCollisions()
                };

                await SendEvent(new { type = "complete", result });
            }
            finally
            {
                Library.UnregisterGlobalLibrary();
            }
        }
        catch (Exception ex)
        {
            await SendEvent(new { type = "error", error = ex.ToString() });
        }
    }

    /// <summary>
    /// Synchronous build for when progress streaming is not needed (kept for compatibility).
    /// </summary>
    public BuildResult Build(float? fVoxelOverride = null)
    {
        float fVoxel = fVoxelOverride ?? Picocnc.fVoxelSizeMM;
        var sw = Stopwatch.StartNew();

        using Library oLib = new(fVoxel);
        Library.RegisterGlobalLibrary(oLib);
        try
        {
            var req = new Picocnc.CNCRequirements
            {
                fWorkAreaX = Picocnc.fWorkAreaX,
                fWorkAreaY = Picocnc.fWorkAreaY,
                fWorkAreaZ = Picocnc.fWorkAreaZ,
                eMaterial  = Picocnc.eCutMaterial,
                eBudget    = Picocnc.eBudgetTier
            };

            Picocnc.CNCSelectedParts parts = Picocnc.SelectParts(req);
            Picocnc.PrintPartsList(parts);

            Voxels voxMachine = Picocnc.voxConstruct();
            Picocnc.VerifyCollisions();
            Picocnc.RunBeamAnalysis();

            string strAssemblyPath = Path.Combine(_strScratchDir, "Assembly.stl");
            voxMachine.mshAsMesh().SaveToStlFile(strAssemblyPath);

            var aComponents = new List<ComponentInfo>();
            ExportComponent("BaseFrame",      Picocnc.voxConstructBaseFrame(),      aComponents);
            ExportComponent("WorkBed",        Picocnc.voxConstructWorkBed(),        aComponents);
            ExportComponent("YRails",         Picocnc.voxConstructYRails(),         aComponents);
            ExportComponent("GantryUprights", Picocnc.voxConstructUprights(),       aComponents);
            ExportComponent("GantryBridge",   Picocnc.voxConstructGantryBridge(),   aComponents);
            ExportComponent("XRails",         Picocnc.voxConstructXRails(),         aComponents);
            ExportComponent("ZAssembly",      Picocnc.voxConstructZAssembly(),      aComponents);
            ExportComponent("SpindleMount",   Picocnc.voxConstructSpindleMount(),   aComponents);
            ExportComponent("MotorMounts",    Picocnc.voxConstructMotorMounts(),    aComponents);
            ExportComponent("LeadScrews",     Picocnc.voxConstructLeadScrews(),     aComponents);
            ExportComponent("DragChains",     Picocnc.voxConstructDragChains(),     aComponents);
            ExportComponent("Safety",         Picocnc.voxConstructSafety(),         aComponents);

            sw.Stop();

            return new BuildResult
            {
                strStatus          = "ok",
                fDurationSec       = (float)sw.Elapsed.TotalSeconds,
                aComponents        = aComponents,
                strAssemblyStlUrl  = "/api/stl/Assembly",
                aBom               = BuildBomFromParts(parts),
                oAnalysis          = GetLatestAnalysis(),
                oCollisions        = GetLatestCollisions()
            };
        }
        finally
        {
            Library.UnregisterGlobalLibrary();
        }
    }

    void ExportComponent(string strName, Voxels vox, List<ComponentInfo> list)
    {
        string strPath = Path.Combine(_strScratchDir, $"{strName}.stl");
        vox.mshAsMesh().SaveToStlFile(strPath);
        list.Add(new ComponentInfo
        {
            strName   = strName,
            strStlUrl = $"/api/stl/{strName}",
            bVisible  = true
        });
    }

    static AnalysisResult? GetLatestAnalysis()
    {
        return new AnalysisResult
        {
            fBridgeDeflectionMm       = Picocnc.s_fBridgeDeflectionMm,
            fBridgeSafetyFactor       = Picocnc.s_fBridgeSafetyFactor,
            fLeadScrewBucklingSafety  = Picocnc.s_fLeadScrewBucklingSafety,
            fUprightBucklingSafety    = Picocnc.s_fUprightBucklingSafety,
            fUprightSlenderness       = Picocnc.s_fUprightSlenderness,
            fBaseRibBucklingSafety    = Picocnc.s_fBaseRibBucklingSafety
        };
    }

    static CollisionResult? GetLatestCollisions()
    {
        return new CollisionResult
        {
            nOverlappingPairs    = Picocnc.s_nOverlappingPairs,
            nUnexpectedWarnings  = Picocnc.s_nUnexpectedWarnings,
            aDetails             = Picocnc.s_aCollisionDetails ?? new List<string>()
        };
    }

    static List<BomItem> BuildBomFromParts(global::PicoGK.Picocnc.CNCSelectedParts parts)
    {
        var bom = new List<BomItem>();

        bom.Add(BomEntry("Y", parts.oYGuideway,  "Guideway",   2));
        bom.Add(BomEntry("X", parts.oXGuideway,  "Guideway",   2));
        bom.Add(BomEntry("Z", parts.oZGuideway,  "Guideway",   1));
        bom.Add(BomEntry("Y", parts.oYDrive,     "Drive",      1));
        bom.Add(BomEntry("X", parts.oXDrive,     "Drive",      1));
        bom.Add(BomEntry("Z", parts.oZDrive,     "Drive",      1));
        bom.Add(BomEntry("Y", parts.oYStepper,   "Stepper",    1));
        bom.Add(BomEntry("X", parts.oXStepper,   "Stepper",    1));
        bom.Add(BomEntry("Z", parts.oZStepper,   "Stepper",    1));
        bom.Add(BomEntry("",  parts.oSpindle,    "Spindle",    1));
        bom.Add(BomEntry("Y", parts.oYCoupling,  "Coupling",   1));
        bom.Add(BomEntry("X", parts.oXCoupling,  "Coupling",   1));
        bom.Add(BomEntry("Z", parts.oZCoupling,  "Coupling",   1));
        bom.Add(BomEntry("Y", parts.oYDragChain, "Drag Chain", 1));
        bom.Add(BomEntry("X", parts.oXDragChain, "Drag Chain", 1));
        bom.Add(BomEntry("",  parts.oLimitSwitch,"Switch",     6));
        bom.Add(BomEntry("",  parts.oFastenerMain,"Fastener",  0));

        return bom;
    }

    static BomItem BomEntry(string strAxis, global::PicoGK.Picocnc.COTSPart part, string strType, int nQty)
    {
        return new BomItem
        {
            strAxis = strAxis,
            strPart = $"{part.strManufacturer} {part.strPartNumber}",
            strType = strType,
            nQty    = nQty,
            strSpec = part.strDescription
        };
    }
}

using System.Text.Json.Serialization;

namespace PicoCNCWeb.Models;

public class BuildResult
{
    [JsonPropertyName("status")]          public string strStatus { get; set; } = "";
    [JsonPropertyName("durationSec")]     public float fDurationSec { get; set; }
    [JsonPropertyName("components")]      public List<ComponentInfo> aComponents { get; set; } = new();
    [JsonPropertyName("assemblyStlUrl")]  public string strAssemblyStlUrl { get; set; } = "";
    [JsonPropertyName("bom")]             public List<BomItem> aBom { get; set; } = new();
    [JsonPropertyName("analysis")]        public AnalysisResult? oAnalysis { get; set; }
    [JsonPropertyName("collisions")]      public CollisionResult? oCollisions { get; set; }
}

public class ComponentInfo
{
    [JsonPropertyName("name")]    public string strName { get; set; } = "";
    [JsonPropertyName("stlUrl")]  public string strStlUrl { get; set; } = "";
    [JsonPropertyName("visible")] public bool bVisible { get; set; } = true;
}

public class BomItem
{
    [JsonPropertyName("axis")] public string strAxis { get; set; } = "";
    [JsonPropertyName("part")] public string strPart { get; set; } = "";
    [JsonPropertyName("type")] public string strType { get; set; } = "";
    [JsonPropertyName("qty")]  public int nQty { get; set; }
    [JsonPropertyName("spec")] public string strSpec { get; set; } = "";
}

public class AnalysisResult
{
    [JsonPropertyName("bridgeDeflectionMm")]      public float fBridgeDeflectionMm { get; set; }
    [JsonPropertyName("bridgeSafetyFactor")]      public float fBridgeSafetyFactor { get; set; }
    [JsonPropertyName("leadScrewBucklingSafety")] public float fLeadScrewBucklingSafety { get; set; }
    [JsonPropertyName("uprightBucklingSafety")]   public float fUprightBucklingSafety { get; set; }
    [JsonPropertyName("baseRibBucklingSafety")]   public float fBaseRibBucklingSafety { get; set; }
    [JsonPropertyName("uprightSlenderness")]      public float fUprightSlenderness { get; set; }
}

public class CollisionResult
{
    [JsonPropertyName("overlappingPairs")]   public int nOverlappingPairs { get; set; }
    [JsonPropertyName("unexpectedWarnings")] public int nUnexpectedWarnings { get; set; }
    [JsonPropertyName("details")]            public List<string> aDetails { get; set; } = new();
}

public class ParamInfo
{
    [JsonPropertyName("key")]     public string key { get; set; } = "";
    [JsonPropertyName("label")]   public string label { get; set; } = "";
    [JsonPropertyName("value")]   public float value { get; set; }
    [JsonPropertyName("unit")]    public string unit { get; set; } = "";
    [JsonPropertyName("min")]     public float min { get; set; }
    [JsonPropertyName("max")]     public float max { get; set; }
    [JsonPropertyName("step")]    public float step { get; set; }
    [JsonPropertyName("options")] public string[]? options { get; set; }
}

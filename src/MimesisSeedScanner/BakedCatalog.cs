using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MimesisSeedScanner
{
    public sealed class ScanCatalogDocument
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("exportedAt")]
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("catalog")]
        public ScanCatalog Catalog { get; set; } = new();
    }

    public sealed class ScanCatalog
    {
        [JsonProperty("tiles")]
        public List<BakedTile> Tiles { get; set; } = [];

        [JsonProperty("flows")]
        public List<BakedFlow> Flows { get; set; } = [];
    }

    public sealed class BakedTile
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("localBounds")]
        public BakedBounds LocalBounds { get; set; } = new();

        [JsonProperty("doorways")]
        public List<BakedDoorway> Doorways { get; set; } = [];

        [JsonProperty("entranceIndices")]
        public List<int> EntranceIndices { get; set; } = [];

        [JsonProperty("exitIndices")]
        public List<int> ExitIndices { get; set; } = [];

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = [];

        [JsonProperty("repeatMode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BakedTileRepeatMode RepeatMode { get; set; } = BakedTileRepeatMode.Allow;

        [JsonProperty("allowRotation")]
        public bool AllowRotation { get; set; } = true;

        [JsonProperty("connectionChance")]
        public float ConnectionChance { get; set; } = 1f;

        [JsonProperty("overrideConnectionChance")]
        public bool OverrideConnectionChance { get; set; }
    }

    public sealed class BakedDoorway
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("localPosition")]
        public BakedVec3 LocalPosition { get; set; } = new();

        [JsonProperty("localRotation")]
        public BakedQuat LocalRotation { get; set; } = BakedQuat.Identity;

        [JsonProperty("socketId")]
        public string SocketId { get; set; } = string.Empty;

        [JsonProperty("isDisabled")]
        public bool IsDisabled { get; set; }
    }

    public sealed class BakedFlow
    {
        [JsonProperty("flowId")]
        public string FlowId { get; set; } = string.Empty;

        [JsonProperty("length")]
        public BakedIntRange Length { get; set; } = new(5, 10);

        [JsonProperty("lengthMultiplier")]
        public float LengthMultiplier { get; set; } = 1f;

        [JsonProperty("branchMode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BakedBranchMode BranchMode { get; set; } = BakedBranchMode.Local;

        [JsonProperty("branchCount")]
        public BakedIntRange BranchCount { get; set; } = new(1, 5);

        [JsonProperty("doorwayConnectionChance")]
        public float DoorwayConnectionChance { get; set; }

        [JsonProperty("restrictConnectionToSameSection")]
        public bool RestrictConnectionToSameSection { get; set; }

        [JsonProperty("tileTagConnectionMode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BakedTagConnectionMode TileTagConnectionMode { get; set; } = BakedTagConnectionMode.Accept;

        [JsonProperty("tileConnectionTags")]
        public List<BakedTagPair> TileConnectionTags { get; set; } = [];

        [JsonProperty("branchPruneTags")]
        public List<string> BranchPruneTags { get; set; } = [];

        [JsonProperty("tileSets")]
        public List<BakedTileSet> TileSets { get; set; } = [];

        [JsonProperty("archetypes")]
        public List<BakedArchetype> Archetypes { get; set; } = [];

        [JsonProperty("lines")]
        public List<BakedGraphLine> Lines { get; set; } = [];

        [JsonProperty("nodes")]
        public List<BakedGraphNode> Nodes { get; set; } = [];

        [JsonProperty("injectionRules")]
        public List<BakedInjectionRule> InjectionRules { get; set; } = [];
    }

    public sealed class BakedInjectionRule
    {
        [JsonProperty("tileSetIndex")]
        public int TileSetIndex { get; set; }

        [JsonProperty("normalizedPathDepth")]
        public BakedFloatRange NormalizedPathDepth { get; set; } = new(0f, 1f);

        [JsonProperty("normalizedBranchDepth")]
        public BakedFloatRange NormalizedBranchDepth { get; set; } = new(0f, 1f);

        [JsonProperty("canAppearOnMainPath")]
        public bool CanAppearOnMainPath { get; set; } = true;

        [JsonProperty("canAppearOnBranchPath")]
        public bool CanAppearOnBranchPath { get; set; }
    }

    public sealed class BakedTagPair
    {
        [JsonProperty("tagA")]
        public string TagA { get; set; } = string.Empty;

        [JsonProperty("tagB")]
        public string TagB { get; set; } = string.Empty;
    }

    public sealed class BakedTileSet
    {
        [JsonProperty("weights")]
        public List<BakedWeightedTile> Weights { get; set; } = [];
    }

    public sealed class BakedWeightedTile
    {
        [JsonProperty("tileId")]
        public int TileId { get; set; }

        [JsonProperty("mainPathWeight")]
        public float MainPathWeight { get; set; } = 1f;

        [JsonProperty("branchPathWeight")]
        public float BranchPathWeight { get; set; } = 1f;

        [JsonProperty("depthWeights")]
        public float[] DepthWeights { get; set; } = [1f];
    }

    public sealed class BakedArchetype
    {
        [JsonProperty("tileSetIndices")]
        public List<int> TileSetIndices { get; set; } = [];

        [JsonProperty("branchStartTileSetIndices")]
        public List<int> BranchStartTileSetIndices { get; set; } = [];

        [JsonProperty("branchStartType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BakedBranchCapType BranchStartType { get; set; }

        [JsonProperty("branchCapTileSetIndices")]
        public List<int> BranchCapTileSetIndices { get; set; } = [];

        [JsonProperty("branchCapType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BakedBranchCapType BranchCapType { get; set; } = BakedBranchCapType.AsWellAs;

        [JsonProperty("branchingDepth")]
        public BakedIntRange BranchingDepth { get; set; } = new(2, 4);

        [JsonProperty("branchCount")]
        public BakedIntRange BranchCount { get; set; } = new(0, 2);

        [JsonProperty("straightenChance")]
        public float StraightenChance { get; set; }

        [JsonProperty("unique")]
        public bool Unique { get; set; }
    }

    public sealed class BakedGraphLine
    {
        [JsonProperty("position")]
        public float Position { get; set; }

        [JsonProperty("length")]
        public float Length { get; set; }

        [JsonProperty("archetypeIndices")]
        public List<int> ArchetypeIndices { get; set; } = [];
    }

    public sealed class BakedGraphNode
    {
        [JsonProperty("position")]
        public float Position { get; set; }

        [JsonProperty("tileSetIndices")]
        public List<int> TileSetIndices { get; set; } = [];

        [JsonProperty("enableBranching")]
        public bool EnableBranching { get; set; }

        [JsonProperty("branchCount")]
        public BakedIntRange BranchCount { get; set; } = new(0, 2);

        [JsonProperty("branchingDepth")]
        public BakedIntRange BranchingDepth { get; set; } = new(2, 4);

        [JsonProperty("branchStartTileSetIndices")]
        public List<int> BranchStartTileSetIndices { get; set; } = [];

        [JsonProperty("branchStartType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BakedBranchCapType BranchStartType { get; set; }

        [JsonProperty("branchCapTileSetIndices")]
        public List<int> BranchCapTileSetIndices { get; set; } = [];

        [JsonProperty("branchCapType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BakedBranchCapType BranchCapType { get; set; } = BakedBranchCapType.AsWellAs;

        [JsonProperty("branchTileSetIndices")]
        public List<int> BranchTileSetIndices { get; set; } = [];
    }

    public readonly struct BakedFloatRange
    {
        public BakedFloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        [JsonProperty("min")]
        public float Min { get; }

        [JsonProperty("max")]
        public float Max { get; }

        public float GetRandom(RandomStream random) =>
            Math.Abs(Min - Max) < 1e-6f ? Min : (float)(random.NextDouble() * (Max - Min) + Min);
    }

    public readonly struct BakedIntRange
    {
        public BakedIntRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        [JsonProperty("min")]
        public int Min { get; }

        [JsonProperty("max")]
        public int Max { get; }

        public int GetRandom(RandomStream random) =>
            Min == Max ? Min : random.Next(Min, Max + 1);
    }

    public readonly struct BakedVec3
    {
        public BakedVec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [JsonProperty("x")]
        public float X { get; }

        [JsonProperty("y")]
        public float Y { get; }

        [JsonProperty("z")]
        public float Z { get; }

        public static BakedVec3 Zero => new(0f, 0f, 0f);

        public static BakedVec3 Up => new(0f, 1f, 0f);
    }

    public readonly struct BakedQuat
    {
        public BakedQuat(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        [JsonProperty("x")]
        public float X { get; }

        [JsonProperty("y")]
        public float Y { get; }

        [JsonProperty("z")]
        public float Z { get; }

        [JsonProperty("w")]
        public float W { get; }

        public static BakedQuat Identity => new(0f, 0f, 0f, 1f);
    }

    public readonly struct BakedBounds
    {
        public BakedBounds(BakedVec3 center, BakedVec3 size)
        {
            Center = center;
            Size = size;
        }

        [JsonProperty("center")]
        public BakedVec3 Center { get; }

        [JsonProperty("size")]
        public BakedVec3 Size { get; }
    }

    public enum BakedTileRepeatMode
    {
        Allow,
        Disallow,
        DisallowImmediate,
    }

    public enum BakedBranchMode
    {
        Local,
        Global,
        Section,
    }

    public enum BakedBranchCapType
    {
        InsteadOf,
        AsWellAs,
    }

    public enum BakedTagConnectionMode
    {
        Accept,
        Reject,
    }
}

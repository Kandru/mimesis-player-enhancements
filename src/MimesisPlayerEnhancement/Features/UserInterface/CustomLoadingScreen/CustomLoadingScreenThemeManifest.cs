using Newtonsoft.Json;

namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal sealed class CustomLoadingScreenThemeManifest
    {
        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }

        [JsonProperty("frameRate")]
        public float? FrameRate { get; set; }

        [JsonProperty("loop")]
        public string? Loop { get; set; }

        [JsonProperty("motion")]
        public CustomLoadingScreenMotionManifest? Motion { get; set; }

        [JsonProperty("backgroundColor")]
        public string? BackgroundColor { get; set; }

        [JsonProperty("phases")]
        public CustomLoadingScreenPhaseManifestMap? Phases { get; set; }
    }

    internal sealed class CustomLoadingScreenMotionManifest
    {
        [JsonProperty("mode")]
        public string? Mode { get; set; }

        [JsonProperty("zoom")]
        public float? Zoom { get; set; }

        [JsonProperty("cycleSeconds")]
        public float? CycleSeconds { get; set; }
    }

    internal sealed class CustomLoadingScreenPhaseManifestMap
    {
        [JsonProperty("loading")]
        public CustomLoadingScreenPhaseManifest? Loading { get; set; }

        [JsonProperty("wait")]
        public CustomLoadingScreenPhaseManifest? Wait { get; set; }

        [JsonProperty("background")]
        public CustomLoadingScreenPhaseManifest? Background { get; set; }
    }

    internal sealed class CustomLoadingScreenPhaseManifest
    {
        [JsonProperty("images")]
        public List<string>? Images { get; set; }

        [JsonProperty("frameRate")]
        public float? FrameRate { get; set; }

        [JsonProperty("loop")]
        public string? Loop { get; set; }

        [JsonProperty("motion")]
        public CustomLoadingScreenMotionManifest? Motion { get; set; }
    }
}

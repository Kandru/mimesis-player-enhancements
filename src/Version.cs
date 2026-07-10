namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Single source of truth for mod and supported game versions.
    /// CI releases are triggered when this file changes on main.
    /// </summary>
    public static class VersionInfo
    {
        public const string ModuleVersion = "26.7.7";

        /// <summary>MIMESIS game version this release was built and tested against.</summary>
        public const string GameVersion = "0.3.0";

        internal const string ReleasesUrl = "https://github.com/Kandru/mimesis-player-enhancements/releases";
    }
}

using MimesisPlayerEnhancement.Features.ModVersionDisplay;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.ModVersionDisplay
{
    public sealed class ModVersionOverlayTextTests
    {
        private const string TestVersion = "99.0.0";
        private const string Prefix = "MimesisPlayerEnhancement v99.0.0";

        [Theory]
        [InlineData("", Prefix)]
        [InlineData("Game 0.3.0", Prefix + "\nGame 0.3.0")]
        [InlineData("MimesisPlayerEnhancement v0.9.0\nGame 0.3.0", Prefix + "\nGame 0.3.0")]
        [InlineData(
            "MimesisPlayerEnhancement v0.8.0\nMimesisPlayerEnhancement v0.9.0\nGame 0.3.0",
            Prefix + "\nGame 0.3.0")]
        public void BuildTargetText_prepends_mod_version_and_strips_existing_prefix(
            string currentText,
            string expected)
        {
            string result = ModVersionDisplayPatchHelpers.BuildTargetText(currentText, TestVersion);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void BuildTargetText_is_idempotent_when_already_overlayed()
        {
            string current = Prefix + "\nGame 0.3.0";

            string result = ModVersionDisplayPatchHelpers.BuildTargetText(current, TestVersion);

            Assert.Equal(current, result);
        }

        [Theory]
        [InlineData("2.0.0", "MimesisPlayerEnhancement v2.0.0\nGame 0.3.0")]
        public void BuildTargetText_uses_injected_module_version(string moduleVersion, string expected)
        {
            string current = "MimesisPlayerEnhancement v0.9.0\nGame 0.3.0";

            string result = ModVersionDisplayPatchHelpers.BuildTargetText(current, moduleVersion);

            Assert.Equal(expected, result);
        }
    }
}

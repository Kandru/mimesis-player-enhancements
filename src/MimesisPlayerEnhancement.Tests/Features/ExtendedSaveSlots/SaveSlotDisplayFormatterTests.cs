using MimesisPlayerEnhancement.Features.ExtendedSaveSlots;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.ExtendedSaveSlots
{
    public sealed class SaveSlotDisplayFormatterTests
    {
        [Fact]
        public void BuildBlinkText_returns_full_text_when_version_warning_disabled()
        {
            var info = new SaveSlotDisplayInfo
            {
                BaseText = "base",
                FullText = "full",
                VersionCheckText = "version mismatch",
            };

            string result = SaveSlotDisplayFormatter.BuildBlinkText(info, showVersionWarning: false);

            Assert.Equal("full", result);
        }

        [Fact]
        public void BuildBlinkText_returns_full_text_when_no_version_check_text()
        {
            var info = new SaveSlotDisplayInfo
            {
                BaseText = "base",
                FullText = "full",
                VersionCheckText = string.Empty,
            };

            string result = SaveSlotDisplayFormatter.BuildBlinkText(info, showVersionWarning: true);

            Assert.Equal("full", result);
        }

        [Fact]
        public void BuildBlinkText_prefixes_base_text_with_red_version_warning()
        {
            var info = new SaveSlotDisplayInfo
            {
                BaseText = "base",
                FullText = "<color=red>warn</color> base",
                VersionCheckText = "warn",
            };

            string result = SaveSlotDisplayFormatter.BuildBlinkText(info, showVersionWarning: true);

            Assert.Equal("<color=red>warn</color> base", result);
        }

        [Fact]
        public void BuildTransparentBlinkText_returns_full_text_when_no_version_check_text()
        {
            var info = new SaveSlotDisplayInfo
            {
                BaseText = "base",
                FullText = "full",
                VersionCheckText = string.Empty,
            };

            string result = SaveSlotDisplayFormatter.BuildTransparentBlinkText(info);

            Assert.Equal("full", result);
        }

        [Fact]
        public void BuildTransparentBlinkText_prefixes_base_text_with_transparent_version_warning()
        {
            var info = new SaveSlotDisplayInfo
            {
                BaseText = "base",
                FullText = "<color=red>warn</color> base",
                VersionCheckText = "warn",
            };

            string result = SaveSlotDisplayFormatter.BuildTransparentBlinkText(info);

            Assert.Equal("<color=#00000000>warn</color> base", result);
        }
    }
}

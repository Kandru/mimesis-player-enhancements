using MelonLoader.Logging;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Logging
{
    public sealed class ModLogFormattingTests
    {
        [Fact]
        public void FeatureSection_formats_melon_bracket_trick()
        {
            string section = ModLog.FeatureSection("DungeonTime", "Patches Applied");

            Assert.Equal("DungeonTime][Patches Applied", section);
        }

        [Fact]
        public void BuildMessageBody_plain_concatenates_segments_in_order()
        {
            (ColorARGB? color, string text)[] segments =
            [
                (ModLog.SuccessGreen, "ok"),
                (null, " neutral"),
                (ModLog.FailureRed, "fail"),
            ];

            string body = ModLog.BuildMessageBody(segments, plain: true);

            Assert.Equal("ok neutralfail", body);
        }

        [Fact]
        public void PickMessageColor_returns_partial_yellow_when_failure_and_success_mixed()
        {
            (ColorARGB? color, string text)[] segments =
            [
                (ModLog.SuccessGreen, "applied"),
                (ModLog.FailureRed, "failed"),
            ];

            ColorARGB result = ModLog.PickMessageColor(segments);

            Assert.Equal(ModLog.PartialYellow, result);
        }

        [Fact]
        public void PickMessageColor_returns_failure_red_when_only_failures()
        {
            (ColorARGB? color, string text)[] segments =
            [
                (ModLog.FailureRed, "miss"),
            ];

            ColorARGB result = ModLog.PickMessageColor(segments);

            Assert.Equal(ModLog.FailureRed, result);
        }

        [Fact]
        public void PickMessageColor_returns_success_green_when_only_successes()
        {
            (ColorARGB? color, string text)[] segments =
            [
                (ModLog.SuccessGreen, "ok"),
            ];

            ColorARGB result = ModLog.PickMessageColor(segments);

            Assert.Equal(ModLog.SuccessGreen, result);
        }

        [Fact]
        public void PickMessageColor_returns_single_distinct_color()
        {
            ColorARGB custom = ColorARGB.Cyan;
            (ColorARGB? color, string text)[] segments =
            [
                (custom, "a"),
                (custom, "b"),
            ];

            ColorARGB result = ModLog.PickMessageColor(segments);

            Assert.Equal(custom, result);
        }

        [Fact]
        public void PickMessageColor_returns_neutral_when_multiple_distinct_colors_without_success_failure()
        {
            (ColorARGB? color, string text)[] segments =
            [
                (ColorARGB.Cyan, "a"),
                (ColorARGB.Magenta, "b"),
            ];

            ColorARGB result = ModLog.PickMessageColor(segments);

            Assert.Equal(ModLog.Neutral, result);
        }

        [Fact]
        public void PickMessageColor_returns_neutral_when_no_colored_segments()
        {
            (ColorARGB? color, string text)[] segments =
            [
                (null, "plain"),
            ];

            ColorARGB result = ModLog.PickMessageColor(segments);

            Assert.Equal(ModLog.Neutral, result);
        }
    }
}

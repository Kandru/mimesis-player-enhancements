using MimesisPlayerEnhancement.Config.QuickSettings;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class QuickSettingsShareCodecTests
    {
        [Fact]
        public void EncodePayload_TryDecodePayload_round_trips_name_and_values()
        {
            Dictionary<string, Dictionary<string, string>> values = QuickSettingsValuesBuilder.CreateMap();
            QuickSettingsValuesBuilder.Set(values, "MimesisPlayerEnhancement_Economy", "StartupMoneyMultiplier", "2.5");
            QuickSettingsValuesBuilder.SetBool(values, "MimesisPlayerEnhancement_Economy", "EnableEconomy", true);

            string share = QuickSettingsShareCodec.EncodePayload("Picnic", values);

            Assert.StartsWith("MPE1:", share);
            Assert.True(QuickSettingsShareCodec.TryDecodePayload(share, out QuickSettingsShareCodec.SharePayload payload, out string? error));
            Assert.Null(error);
            Assert.Equal("Picnic", payload.Name);
            Assert.Equal("2.5", payload.Values["MimesisPlayerEnhancement_Economy"]["StartupMoneyMultiplier"]);
            Assert.Equal("true", payload.Values["MimesisPlayerEnhancement_Economy"]["EnableEconomy"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-share")]
        [InlineData("MPE1:%%%")]
        public void TryDecodePayload_rejects_invalid_strings(string shareString)
        {
            Assert.False(QuickSettingsShareCodec.TryDecodePayload(shareString, out _, out string? error));
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void TryDecodePayload_rejects_empty_values_payload()
        {
            string share = QuickSettingsShareCodec.EncodePayload("Empty", QuickSettingsValuesBuilder.CreateMap());

            Assert.False(QuickSettingsShareCodec.TryDecodePayload(share, out _, out string? error));
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }
}

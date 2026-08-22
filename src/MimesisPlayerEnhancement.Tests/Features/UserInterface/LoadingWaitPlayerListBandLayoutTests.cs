using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class LoadingWaitPlayerListBandLayoutTests
    {
        [Fact]
        public void ResolveBand_at_1080p_matches_design_strip()
        {
            LoadingWaitPlayerListBandLayout.ResolveBand(
                1080f,
                out float bandBottomY,
                out float bandHeight);

            Assert.Equal(LoadingWaitPlayerListBandLayout.BottomInsetPx, bandBottomY, 3);
            Assert.Equal(LoadingWaitPlayerListBandLayout.HeightPx, bandHeight, 3);
        }

        [Fact]
        public void ResolveBand_and_insets_scale_with_resolution()
        {
            LoadingWaitPlayerListBandLayout.ResolveBand(
                540f,
                out float bandBottomY,
                out float bandHeight);

            Assert.Equal(LoadingWaitPlayerListBandLayout.BottomInsetPx * 0.5f, bandBottomY, 3);
            Assert.Equal(LoadingWaitPlayerListBandLayout.HeightPx * 0.5f, bandHeight, 3);
            Assert.Equal(
                LoadingWaitPlayerListBandLayout.HorizontalInsetPx * 0.5f,
                LoadingWaitPlayerListBandLayout.ResolveHorizontalInset(960f),
                3);
        }
    }
}

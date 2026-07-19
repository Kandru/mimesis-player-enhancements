using MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen;
using UnityEngine;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class CustomLoadingScreenImageLayoutTests
    {
        [Theory]
        [InlineData(16f / 9f)]
        public void ResolveMode_uses_cover_below_ultrawide_threshold(float screenAspect)
        {
            Assert.Equal(
                CustomLoadingScreenScaleMode.Cover,
                CustomLoadingScreenImageLayout.ResolveMode(screenAspect));
        }

        [Fact]
        public void ResolveMode_uses_fit_height_at_ultrawide_threshold()
        {
            Assert.Equal(
                CustomLoadingScreenScaleMode.FitHeight,
                CustomLoadingScreenImageLayout.ResolveMode(CustomLoadingScreenConstants.UltrawideAspectThreshold));
        }

        [Fact]
        public void ResolveMode_uses_fit_height_above_ultrawide_threshold()
        {
            float screenAspect = CustomLoadingScreenConstants.UltrawideAspectThreshold + 0.1f;

            CustomLoadingScreenScaleMode mode = CustomLoadingScreenImageLayout.ResolveMode(screenAspect);

            Assert.Equal(CustomLoadingScreenScaleMode.FitHeight, mode);
        }

        [Fact]
        public void ComputeCoverUvRect_returns_full_rect_when_aspects_match()
        {
            Rect uvRect = CustomLoadingScreenImageLayout.ComputeCoverUvRect(16f / 9f, 16f / 9f);

            Assert.Equal(0f, uvRect.x, precision: 3);
            Assert.Equal(0f, uvRect.y, precision: 3);
            Assert.Equal(1f, uvRect.width, precision: 3);
            Assert.Equal(1f, uvRect.height, precision: 3);
        }

        [Fact]
        public void ComputeCoverUvRect_crops_wider_image_horizontally()
        {
            Rect uvRect = CustomLoadingScreenImageLayout.ComputeCoverUvRect(imageAspect: 2f, screenAspect: 1f);

            Assert.True(uvRect.width < 1f);
            Assert.Equal(1f, uvRect.height, precision: 3);
            Assert.Equal((1f - uvRect.width) * 0.5f, uvRect.x, precision: 3);
        }

        [Fact]
        public void ComputeCoverUvRect_crops_taller_image_vertically()
        {
            Rect uvRect = CustomLoadingScreenImageLayout.ComputeCoverUvRect(imageAspect: 0.5f, screenAspect: 1f);

            Assert.Equal(1f, uvRect.width, precision: 3);
            Assert.True(uvRect.height < 1f);
            Assert.Equal((1f - uvRect.height) * 0.5f, uvRect.y, precision: 3);
        }

        [Fact]
        public void ComputePanZoomUvRect_stays_within_base_rect()
        {
            Rect baseUvRect = new Rect(0.1f, 0.2f, 0.8f, 0.6f);

            Rect panZoomRect = CustomLoadingScreenImageLayout.ComputePanZoomUvRect(
                baseUvRect,
                zoom: 1.5f,
                cycleT: 0.25f);

            Assert.True(panZoomRect.x >= baseUvRect.xMin);
            Assert.True(panZoomRect.y >= baseUvRect.yMin);
            Assert.True(panZoomRect.xMax <= baseUvRect.xMax);
            Assert.True(panZoomRect.yMax <= baseUvRect.yMax);
            Assert.True(panZoomRect.width > 0f);
            Assert.True(panZoomRect.height > 0f);
        }

        [Fact]
        public void ComputePanZoomUvRect_uses_full_base_rect_when_zoom_is_one()
        {
            Rect baseUvRect = new Rect(0.1f, 0.2f, 0.8f, 0.6f);

            Rect panZoomRect = CustomLoadingScreenImageLayout.ComputePanZoomUvRect(
                baseUvRect,
                zoom: 1f,
                cycleT: 0f);

            Assert.Equal(baseUvRect.x, panZoomRect.x, precision: 3);
            Assert.Equal(baseUvRect.y, panZoomRect.y, precision: 3);
            Assert.Equal(baseUvRect.width, panZoomRect.width, precision: 3);
            Assert.Equal(baseUvRect.height, panZoomRect.height, precision: 3);
        }
    }
}

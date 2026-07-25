using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;
using UnityEngine;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class LoadingWaitPlayerListTextMeasureTests
    {
        [Fact]
        public void MeasurePreferredSize_uses_character_estimate_for_null_component()
        {
            Vector2 size = LoadingWaitPlayerListTextMeasure.MeasurePreferredSize(null, "abcd", fontSize: 12f);

            Assert.True(size.x > 0f);
            Assert.True(size.y > 0f);
        }
    }
}

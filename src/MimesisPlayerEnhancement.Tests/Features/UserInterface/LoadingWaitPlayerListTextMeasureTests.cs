using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;
using UnityEngine;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class LoadingWaitPlayerListTextMeasureTests
    {
        [Fact]
        public void MeasurePreferredSize_returns_zero_for_null_component()
        {
            Vector2 size = LoadingWaitPlayerListTextMeasure.MeasurePreferredSize(null, "ignored", fontSize: 12f);

            Assert.Equal(Vector2.zero, size);
        }
    }
}

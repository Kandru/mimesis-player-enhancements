using MimesisPlayerEnhancement.Features.LootMultiplicator;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.LootMultiplicator
{
    public sealed class LootMultiplicatorApplierTests
    {
        [Fact]
        public void Apply_returns_false_when_room_is_null()
        {
            Assert.False(LootMultiplicatorApplier.Apply(room: null!));
        }
    }
}

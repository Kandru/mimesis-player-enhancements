using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonDataAccessFilterTests
    {
        [Theory]
        [InlineData(5, new[] { 1, 5, 9 }, true)]
        [InlineData(3, new[] { 1, 5, 9 }, false)]
        [InlineData(5, new int[0], false)]
        public void IsExcluded_detects_matching_exclude_ids(int dungeonId, int[] excludeIds, bool expected)
        {
            bool excluded = DungeonDataAccess.IsExcluded(dungeonId, excludeIds);

            Assert.Equal(expected, excluded);
        }

        [Fact]
        public void FilterExcluded_returns_copy_when_no_excludes()
        {
            List<int> pool = [1, 2, 3];

            List<int> filtered = DungeonDataAccess.FilterExcluded(pool, []);

            Assert.Equal(pool, filtered);
            Assert.NotSame(pool, filtered);
        }

        [Fact]
        public void FilterExcluded_removes_matching_ids()
        {
            List<int> pool = [1, 2, 3, 4, 5];

            List<int> filtered = DungeonDataAccess.FilterExcluded(pool, [2, 4]);

            Assert.Equal([1, 3, 5], filtered);
        }
    }
}

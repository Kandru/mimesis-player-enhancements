using Cysharp.Threading.Tasks;
using MimesisPlayerEnhancement.Features.Privacy.Patches;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Privacy
{
    public sealed class ReplayRecorderUploadBlockHelperTests
    {
        [Fact]
        public void TryBlockUpload_returns_false_and_leaves_result_unchanged_when_not_blocked()
        {
            UniTask result = default;
            UniTask original = result;

            bool blocked = ReplayRecorderUploadBlockHelper.TryBlockUpload(shouldBlock: false, ref result);

            Assert.False(blocked);
            Assert.Equal(original, result);
        }

        [Fact]
        public void TryBlockUpload_returns_true_and_sets_completed_task_when_blocked()
        {
            UniTask result = default;

            bool blocked = ReplayRecorderUploadBlockHelper.TryBlockUpload(shouldBlock: true, ref result);

            Assert.True(blocked);
            Assert.Equal(UniTask.CompletedTask, result);
        }
    }
}

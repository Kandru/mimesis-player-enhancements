using MimesisPlayerEnhancement.Config.HostConfigSync;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class HostConfigMirrorTests
    {
        [Fact]
        public void Clear_on_fresh_mirror_is_noop()
        {
            HostConfigMirror.Clear();
            Assert.False(HostConfigMirror.IsActive);
            Assert.Equal(-1, HostConfigMirror.MirroredSlotId);
            Assert.Equal(0, HostConfigMirror.Revision);
        }
    }
}

using MimesisPlayerEnhancement.Features.Economy;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Economy
{
    public sealed class StartupMoneyLoadGuardTests
    {
        [Fact]
        public void SuppressStartupScale_tracks_nested_enter_exit()
        {
            Assert.False(StartupMoneyLoadGuard.SuppressStartupScale);

            StartupMoneyLoadGuard.EnterSuppressStartupScale();
            Assert.True(StartupMoneyLoadGuard.SuppressStartupScale);

            StartupMoneyLoadGuard.EnterSuppressStartupScale();
            Assert.True(StartupMoneyLoadGuard.SuppressStartupScale);

            StartupMoneyLoadGuard.ExitSuppressStartupScale();
            Assert.True(StartupMoneyLoadGuard.SuppressStartupScale);

            StartupMoneyLoadGuard.ExitSuppressStartupScale();
            Assert.False(StartupMoneyLoadGuard.SuppressStartupScale);
        }

        [Fact]
        public void ExitSuppressStartupScale_is_safe_when_not_entered()
        {
            StartupMoneyLoadGuard.ExitSuppressStartupScale();
            Assert.False(StartupMoneyLoadGuard.SuppressStartupScale);
        }
    }
}

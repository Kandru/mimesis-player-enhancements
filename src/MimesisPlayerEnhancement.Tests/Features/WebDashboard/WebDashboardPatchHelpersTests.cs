using MimesisPlayerEnhancement.Features.WebDashboard;
using ReluProtocol.Enum;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardPatchHelpersTests
    {
        public WebDashboardPatchHelpersTests()
        {
            WebDashboardPatchHelpers.ClearCachedGrades();
        }

        [Fact]
        public void TryGetCachedGrade_returns_false_when_missing()
        {
            bool found = WebDashboardPatchHelpers.TryGetCachedGrade(99, out int grade);

            Assert.False(found);
            Assert.Equal(0, grade);
        }

        [Fact]
        public void UpdateCachedGrades_stores_grade_for_lookup()
        {
            WebDashboardPatchHelpers.UpdateCachedGrades(
                [new KeyValuePair<long, NetworkGrade>(42L, NetworkGrade.Fine)]);

            bool found = WebDashboardPatchHelpers.TryGetCachedGrade(42, out int grade);

            Assert.True(found);
            Assert.Equal((int)NetworkGrade.Fine, grade);
        }

        [Fact]
        public void UpdateCachedGrades_overwrites_existing_grade()
        {
            WebDashboardPatchHelpers.UpdateCachedGrades(
                [new KeyValuePair<long, NetworkGrade>(7L, NetworkGrade.Fine)]);
            WebDashboardPatchHelpers.UpdateCachedGrades(
                [new KeyValuePair<long, NetworkGrade>(7L, NetworkGrade.Medium)]);

            bool found = WebDashboardPatchHelpers.TryGetCachedGrade(7, out int grade);

            Assert.True(found);
            Assert.Equal((int)NetworkGrade.Medium, grade);
        }

        [Fact]
        public void ClearCachedGrades_removes_all_entries()
        {
            WebDashboardPatchHelpers.UpdateCachedGrades(
                [new KeyValuePair<long, NetworkGrade>(1L, NetworkGrade.Fine)]);
            WebDashboardPatchHelpers.ClearCachedGrades();

            Assert.False(WebDashboardPatchHelpers.TryGetCachedGrade(1, out _));
        }
    }
}

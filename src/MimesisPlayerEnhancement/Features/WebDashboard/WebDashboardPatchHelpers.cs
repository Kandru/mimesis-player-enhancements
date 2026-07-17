using System.Collections.Concurrent;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPatchHelpers
    {
        private static readonly ConcurrentDictionary<long, int> GradeByPlayerUid = new();

        internal static bool TryGetCachedGrade(long playerUid, out int grade)
        {
            return GradeByPlayerUid.TryGetValue(playerUid, out grade);
        }

        internal static void UpdateCachedGrades(IEnumerable<KeyValuePair<long, ReluProtocol.Enum.NetworkGrade>> grades)
        {
            bool changed = false;
            foreach (KeyValuePair<long, ReluProtocol.Enum.NetworkGrade> pair in grades)
            {
                int grade = (int)pair.Value;
                if (!GradeByPlayerUid.TryGetValue(pair.Key, out int existing) || existing != grade)
                {
                    GradeByPlayerUid[pair.Key] = grade;
                    changed = true;
                }
            }

            if (changed)
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }

        internal static void ClearCachedGrades()
        {
            GradeByPlayerUid.Clear();
        }
    }
}

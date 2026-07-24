namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal static class SpeechEventArchiveRegistry
    {
        private static readonly List<SpeechEventArchive> Archives = [];
        private static readonly HashSet<SpeechEventArchive> ArchiveSet = [];

        internal static void Register(SpeechEventArchive archive)
        {
            if (archive == null || !ArchiveSet.Add(archive))
            {
                return;
            }

            Archives.Add(archive);
        }

        internal static void Unregister(SpeechEventArchive archive)
        {
            if (archive == null)
            {
                return;
            }

            if (!ArchiveSet.Remove(archive))
            {
                return;
            }

            _ = Archives.Remove(archive);
        }

        internal static void Clear()
        {
            Archives.Clear();
            ArchiveSet.Clear();
        }

        internal static IEnumerable<SpeechEventArchive> EnumerateActive()
        {
            for (int i = Archives.Count - 1; i >= 0; i--)
            {
                SpeechEventArchive archive = Archives[i];
                if (archive == null)
                {
                    Archives.RemoveAt(i);
                    continue;
                }

                yield return archive;
            }
        }
    }
}

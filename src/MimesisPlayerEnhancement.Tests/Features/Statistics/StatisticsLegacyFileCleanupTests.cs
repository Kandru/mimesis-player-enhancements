using System.IO;
using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsLegacyFileCleanupTests
    {
        [Fact]
        public void Retire_archives_main_and_removes_siblings()
        {
            using TempStatsDir dir = new();
            string path = dir.StatsPath;
            File.WriteAllText(path, "main-v1");
            File.WriteAllText(path + AtomicFileIO.BackupSuffix, "bak-v1");
            File.WriteAllText(path + AtomicFileIO.TempSuffix, "tmp");

            StatisticsLegacyFileCleanup.Retire(path, 1, "Statistics");

            string archive = path + ".legacy-v1.bak";
            Assert.True(File.Exists(archive));
            Assert.Equal("main-v1", File.ReadAllText(archive));
            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + AtomicFileIO.BackupSuffix));
            Assert.False(File.Exists(path + AtomicFileIO.TempSuffix));
        }

        [Fact]
        public void Retire_promotes_orphan_backup_when_main_missing()
        {
            using TempStatsDir dir = new();
            string path = dir.StatsPath;
            File.WriteAllText(path + AtomicFileIO.BackupSuffix, "orphan-bak");

            StatisticsLegacyFileCleanup.Retire(path, 1, "Statistics");

            string archive = path + ".legacy-v1.bak";
            Assert.True(File.Exists(archive));
            Assert.Equal("orphan-bak", File.ReadAllText(archive));
            Assert.False(File.Exists(path + AtomicFileIO.BackupSuffix));
        }

        [Fact]
        public void Retire_deletes_duplicate_main_when_archive_exists()
        {
            using TempStatsDir dir = new();
            string path = dir.StatsPath;
            string archive = path + ".legacy-v1.bak";
            File.WriteAllText(archive, "archived");
            File.WriteAllText(path, "duplicate-main");

            StatisticsLegacyFileCleanup.Retire(path, 1, "Statistics");

            Assert.Equal("archived", File.ReadAllText(archive));
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void Retire_is_idempotent()
        {
            using TempStatsDir dir = new();
            string path = dir.StatsPath;
            File.WriteAllText(path, "main-v1");
            File.WriteAllText(path + AtomicFileIO.BackupSuffix, "bak-v1");

            StatisticsLegacyFileCleanup.Retire(path, 1, "Statistics");
            StatisticsLegacyFileCleanup.Retire(path, 1, "Statistics");

            string archive = path + ".legacy-v1.bak";
            Assert.True(File.Exists(archive));
            Assert.Equal("main-v1", File.ReadAllText(archive));
            Assert.False(File.Exists(path + AtomicFileIO.BackupSuffix));
        }

        [Fact]
        public void Retire_on_missing_files_is_noop()
        {
            using TempStatsDir dir = new();
            string path = dir.StatsPath;

            StatisticsLegacyFileCleanup.Retire(path, 1, "Statistics");

            Assert.Empty(Directory.GetFiles(dir.Root));
        }

        private sealed class TempStatsDir : IDisposable
        {
            internal string Root { get; } = Path.Combine(Path.GetTempPath(), "mpe-stats-legacy-" + Guid.NewGuid().ToString("N"));
            internal string StatsPath => Path.Combine(Root, "MMGameData01.mpe-stats.sav");

            internal TempStatsDir()
            {
                Directory.CreateDirectory(Root);
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Root))
                    {
                        Directory.Delete(Root, recursive: true);
                    }
                }
                catch
                {
                    // best-effort cleanup
                }
            }
        }
    }
}

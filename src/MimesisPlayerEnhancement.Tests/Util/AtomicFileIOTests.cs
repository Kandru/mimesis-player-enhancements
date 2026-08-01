using System.Text;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Util
{
    public sealed class AtomicFileIOTests
    {
        [Fact]
        public void DeleteVolatileSiblings_keeps_main_file()
        {
            using TempDir dir = new();
            string path = Path.Combine(dir.Root, "data.sav");
            File.WriteAllText(path, "main");
            File.WriteAllText(path + AtomicFileIO.BackupSuffix, "bak");
            File.WriteAllText(path + AtomicFileIO.TempSuffix, "tmp");

            AtomicFileIO.DeleteVolatileSiblings(path, "Test");

            Assert.True(File.Exists(path));
            Assert.Equal("main", File.ReadAllText(path));
            Assert.False(File.Exists(path + AtomicFileIO.BackupSuffix));
            Assert.False(File.Exists(path + AtomicFileIO.TempSuffix));
        }

        [Fact]
        public void ReadText_prefers_main_over_backup()
        {
            using TempDir dir = new();
            string path = Path.Combine(dir.Root, "data.sav");
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes("from-main"));
            File.WriteAllBytes(path + AtomicFileIO.BackupSuffix, Encoding.UTF8.GetBytes("from-bak"));

            string? text = AtomicFileIO.ReadText(path, "Test");

            Assert.Equal("from-main", text);
        }

        [Fact]
        public void ReadText_falls_back_to_backup_when_main_missing()
        {
            using TempDir dir = new();
            string path = Path.Combine(dir.Root, "data.sav");
            File.WriteAllBytes(path + AtomicFileIO.BackupSuffix, Encoding.UTF8.GetBytes("from-bak"));

            string? text = AtomicFileIO.ReadText(path, "Test");

            Assert.Equal("from-bak", text);
        }

        private sealed class TempDir : IDisposable
        {
            internal string Root { get; } = Path.Combine(Path.GetTempPath(), "mpe-atomic-io-" + Guid.NewGuid().ToString("N"));

            internal TempDir()
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

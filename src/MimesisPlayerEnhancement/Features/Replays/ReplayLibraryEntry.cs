using System.IO;
using ReluReplay.Data;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal sealed class ReplayLibraryEntry
    {
        internal ReplayLibraryEntry(string playFilePath, string fileName, long fileSizeBytes, IReplayHeader header)
        {
            PlayFilePath = playFilePath;
            FileName = fileName;
            FileSizeBytes = fileSizeBytes;
            Header = header;
        }

        internal string PlayFilePath { get; }

        internal string FileName { get; }

        internal long FileSizeBytes { get; }

        internal IReplayHeader Header { get; }

        internal string SndFilePath => ReplayLibrary.GetSndPathForPlayFile(PlayFilePath);

        internal bool HasVoiceFile => File.Exists(SndFilePath);
    }
}

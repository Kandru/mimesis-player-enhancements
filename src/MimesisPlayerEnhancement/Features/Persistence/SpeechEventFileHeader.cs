using System.Text;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal static class SpeechEventFileHeader
    {
        private static readonly byte[] FileMagic = Encoding.ASCII.GetBytes("MPEV");
        private const int MaxEventCount = 100000;

        internal static bool HasMagicHeader(ReadOnlySpan<byte> data)
        {
            return data.Length >= FileMagic.Length
                   && data[0] == FileMagic[0]
                   && data[1] == FileMagic[1]
                   && data[2] == FileMagic[2]
                   && data[3] == FileMagic[3];
        }

        internal static bool TryReadEventCount(ReadOnlySpan<byte> data, out int count)
        {
            count = 0;
            if (data.Length < 4)
            {
                return false;
            }

            if (HasMagicHeader(data))
            {
                if (data.Length < 12)
                {
                    return false;
                }

                count = BitConverter.ToInt32(data.Slice(8, 4));
                if (count <= 0 || count > MaxEventCount)
                {
                    count = 0;
                    return false;
                }

                return true;
            }

            count = BitConverter.ToInt32(data.Slice(0, 4));
            if (count <= 0 || count > MaxEventCount)
            {
                count = 0;
                return false;
            }

            return true;
        }
    }
}

using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement.Config.HostConfigSync
{
    // Fields must be public: serialized via ModJson over the network and in tests.
    internal sealed class HostConfigSyncEnvelope
    {
        public int V;
        public int Rev;
        public int SlotId = -1;
        public SaveConfigProfileState Profile = new();
        public Dictionary<string, Dictionary<string, string>> Values =
          new(StringComparer.OrdinalIgnoreCase);
    }
}

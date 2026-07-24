using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Config.HostConfigSync
{
    internal static class HostConfigSyncTransport
    {
        internal const string CommandHello = "mpe.hello";
        internal const string CommandConfig = "mpe.cfg";

        private const string Feature = "HostConfigSync";

        private static readonly FieldInfo? AdminCommandFuncsField =
            AccessTools.Field(typeof(VWorld), "AdminCommandFuncs");

        private static int _nextHashCode = 0x4D504530;
        private static int _receiveRevision = -1;
        private static int _receiveChunkCount;
        private static readonly Dictionary<int, string> _receiveChunks = [];

        private static readonly PropertyInfo? HubNetmanProperty =
            AccessTools.Property(typeof(Hub), "netman2");

        private static bool _adminCommandsRegistered;

        internal static void RegisterAdminCommands(VWorld vworld)
        {
            if (vworld == null || AdminCommandFuncsField?.GetValue(vworld) is not Dictionary<string, VWorld.AdminCommandFunc> commands)
            {
                ModLog.Warn(Feature, "Failed to register admin commands — AdminCommandFuncs unavailable.");
                return;
            }

            commands[CommandHello] = HandleHello;
            if (!_adminCommandsRegistered)
            {
                _adminCommandsRegistered = true;
                ModLog.Info(Feature, "Registered host config sync admin commands.");
            }
        }

        internal static void ResetSessionState()
        {
            _receiveRevision = -1;
            _receiveChunkCount = 0;
            _receiveChunks.Clear();
        }

        internal static bool TryHandleClientPacket(AdminCommandReq request)
        {
            if (request == null || string.IsNullOrEmpty(request.command))
            {
                return false;
            }

            if (string.Equals(request.command, CommandConfig, StringComparison.Ordinal))
            {
                return TryHandleConfigChunk(request.args);
            }

            return false;
        }

        internal static void SendHello(int attempt = 1)
        {
            if (HostApplyGate.IsParticipantClient())
            {
                return;
            }

            NetworkManagerV2? netman = TryGetNetworkManager();
            if (netman == null)
            {
                ModLog.Debug(Feature, $"Hello send skipped — network manager unavailable (attempt {attempt}).");
                return;
            }

            try
            {
                AdminCommandReq request = new()
                {
                    command = CommandHello,
                    args = $"version={VersionInfo.ModuleVersion}",
                    hashCode = NextHashCode(),
                };
                if (!netman.SendNoCallback(request))
                {
                    ModLog.Debug(Feature, $"Hello send failed — network unavailable (attempt {attempt}).");
                }
                else
                {
                    ModLog.Debug(Feature, $"Sent mod hello — attempt {attempt}.");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Hello send failed — {ex.Message}");
            }
        }

        internal static void SendSnapshot(VPlayer player, int revision)
        {
            if (player == null || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            try
            {
                HostConfigSyncEnvelope snapshot = HostConfigSyncCodec.BuildSnapshot(revision);
                string json = HostConfigSyncCodec.Serialize(snapshot);
                IReadOnlyList<string> chunks = HostConfigSyncCodec.SplitIntoChunks(json);
                for (int i = 0; i < chunks.Count; i++)
                {
                    AdminCommandReq request = new()
                    {
                        command = CommandConfig,
                        args = HostConfigSyncCodec.FormatChunkArgs(revision, i, chunks.Count, chunks[i]),
                        hashCode = NextHashCode(),
                    };
                    player.SendToMe(request);
                }

                ModLog.Debug(Feature, $"Sent config snapshot — uid={player.UID}, rev={revision}, chunks={chunks.Count}.");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Config snapshot send failed — uid={player.UID}, {ex.Message}");
            }
        }

        private static MsgErrorCode HandleHello(VPlayer player, AdminCommandArgs args)
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return MsgErrorCode.InvalidAdminCommandArgument;
            }

            try
            {
                string modVersion = args?.AsString("version", string.Empty) ?? string.Empty;
                HostConfigSyncRuntime.OnClientHello(player, modVersion);
                return MsgErrorCode.Success;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Hello handler failed — uid={player?.UID}, {ex.Message}");
                return MsgErrorCode.InvalidAdminCommandArgument;
            }
        }

        private static bool TryHandleConfigChunk(string? args)
        {
            if (HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return false;
            }

            if (!HostConfigSyncCodec.TryParseChunkArgs(
                    args ?? string.Empty,
                    out int protocolVersion,
                    out int revision,
                    out int chunkIndex,
                    out int chunkCount,
                    out string payload))
            {
                ModLog.Debug(Feature, "Ignored malformed config chunk.");
                return true;
            }

            if (protocolVersion != HostConfigSyncCodec.ProtocolVersion)
            {
                ModLog.Warn(Feature, $"Ignored config chunk — protocol v{protocolVersion}.");
                return true;
            }

            if (_receiveRevision != revision)
            {
                _receiveRevision = revision;
                _receiveChunkCount = chunkCount;
                _receiveChunks.Clear();
                ModLog.Debug(Feature, $"Receiving config snapshot — rev={revision}, chunks={chunkCount}.");
            }
            else if (chunkCount != _receiveChunkCount)
            {
                _receiveChunks.Clear();
                _receiveChunkCount = chunkCount;
            }

            _receiveChunks[chunkIndex] = payload;
            if (_receiveChunks.Count < chunkCount)
            {
                return true;
            }

            if (!HostConfigSyncCodec.TryReassembleChunks(_receiveChunks, revision, chunkCount, out string json))
            {
                ModLog.Warn(Feature, "Config chunk reassembly failed.");
                _receiveChunks.Clear();
                return true;
            }

            _receiveChunks.Clear();
            ModLog.Debug(Feature, $"Config snapshot chunks complete — rev={revision}, bytes={json.Length}.");
            HostConfigSyncEnvelope? envelope = HostConfigSyncCodec.TryDeserialize(json);
            if (envelope == null)
            {
                ModLog.Warn(Feature, "Config snapshot decode failed.");
                return true;
            }

            HostConfigMirror.ApplySnapshot(envelope);
            return true;
        }

        private static int NextHashCode()
        {
            return _nextHashCode++;
        }

        private static NetworkManagerV2? TryGetNetworkManager()
        {
            if (Hub.s == null || HubNetmanProperty == null)
            {
                return null;
            }

            return HubNetmanProperty.GetValue(Hub.s) as NetworkManagerV2;
        }
    }
}

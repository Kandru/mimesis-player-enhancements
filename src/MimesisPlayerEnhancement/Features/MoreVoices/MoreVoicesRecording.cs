namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Scene and possession gates for mimic voice recording beyond vanilla dungeon-only capture.
    /// </summary>
    internal static class MoreVoicesRecording
    {
        private const string Feature = "MoreVoices";

        internal static bool IsFeatureActive() => ModConfig.EnableMoreVoices.Value;

        internal static bool IsMaintenanceRecordingEnabled() =>
            IsFeatureActive() && ModConfig.RecordVoiceInMaintenance.Value;

        internal static bool IsTramRecordingEnabled() =>
            IsFeatureActive() && ModConfig.RecordVoiceInTram.Value;

        internal static bool IsPossessionRecordingEnabled() =>
            IsFeatureActive() && ModConfig.RecordVoiceDuringMimicPossession.Value;

        internal static bool IsLocalPlayerPossessingMimic() =>
            MoreVoicesVoiceAccess.IsLocalPlayerPossessingMimic();

        internal static bool IsLocalPlayerInMaintenanceRoom() =>
            MoreVoicesVoiceAccess.TryGetLocalVPlayer()?.VRoom is MaintenanceRoom;

        internal static bool IsLocalPlayerInTramWaitingRoom()
        {
            if (MoreVoicesVoiceAccess.TryGetLocalVPlayer()?.VRoom is not VWaitingRoom waitingRoom)
            {
                return false;
            }

            return !waitingRoom.BackToMaintenance;
        }

        internal static bool ShouldRecordInCurrentHubScene()
        {
            if (IsLocalPlayerInMaintenanceRoom())
            {
                return IsMaintenanceRecordingEnabled();
            }

            if (IsLocalPlayerInTramWaitingRoom())
            {
                return IsTramRecordingEnabled();
            }

            return false;
        }

        internal static bool ShouldSyncRecordedEvent(bool isForce)
        {
            if (isForce)
            {
                return true;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null)
            {
                return false;
            }

            if (pdata.serverRoomState == Hub.PersistentData.eServerRoomState.InGame)
            {
                return true;
            }

            return pdata.serverRoomState == Hub.PersistentData.eServerRoomState.PreGame
                   && ShouldRecordInCurrentHubScene();
        }

        internal static bool ShouldResumeRecordingAfterPossession()
        {
            VoiceManager? voiceman = MoreVoicesVoiceAccess.TryGetVoiceManager();
            if (!IsPossessionRecordingEnabled() || voiceman == null)
            {
                return false;
            }

            return MoreVoicesVoiceAccess.TryGetVoiceMode(voiceman) == VoiceMode.Player;
        }

        internal static bool VanillaWouldSyncRecordedEvent(bool isForce)
        {
            if (Hub.s == null)
            {
                return false;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null)
            {
                return false;
            }

            return pdata.serverRoomState == Hub.PersistentData.eServerRoomState.InGame || isForce;
        }

        internal static void ApplyRecordingState()
        {
            VoiceManager? voiceman = MoreVoicesVoiceAccess.TryGetVoiceManager();
            if (voiceman == null)
            {
                return;
            }

            VoiceMode? voiceMode = MoreVoicesVoiceAccess.TryGetVoiceMode(voiceman);
            if (voiceMode == null)
            {
                return;
            }

            if (!IsFeatureActive())
            {
                if (voiceMode == VoiceMode.PreGame)
                {
                    MoreVoicesVoiceAccess.StopRecording(voiceman);
                }

                return;
            }

            if (voiceMode == VoiceMode.PreGame)
            {
                string roomLabel = MoreVoicesVoiceAccess.TryGetLocalVPlayer()?.VRoom?.GetType().Name ?? "unknown";
                if (ShouldRecordInCurrentHubScene())
                {
                    MoreVoicesVoiceAccess.StartRecording(voiceman);
                    ModLog.Debug(Feature, $"Hub voice recording started — room={roomLabel}");
                }
                else
                {
                    MoreVoicesVoiceAccess.StopRecording(voiceman);
                    ModLog.Debug(Feature, $"Hub voice recording stopped — room={roomLabel}");
                }

                return;
            }

            if (voiceMode == VoiceMode.Player
                && IsPossessionRecordingEnabled()
                && !IsLocalPlayerPossessingMimic())
            {
                MoreVoicesVoiceAccess.StartRecording(voiceman);
            }
        }
    }
}

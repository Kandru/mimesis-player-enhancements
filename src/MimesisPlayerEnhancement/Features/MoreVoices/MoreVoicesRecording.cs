namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Scene and possession gates for mimic voice recording beyond vanilla dungeon-only capture.
    /// </summary>
    internal static class MoreVoicesRecording
    {
        private const string Feature = "MoreVoices";

        internal static bool IsFeatureActive() => MoreVoicesRuntime.ShouldApply();

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

        internal static bool ShouldSyncRecordedEvent(bool isForce) =>
            ShouldSyncRecordedEvent(
                isForce,
                GameSessionAccess.TryGetPdata()?.serverRoomState,
                ShouldRecordInCurrentHubScene());

        internal static bool ShouldSyncRecordedEvent(
            bool isForce,
            Hub.PersistentData.eServerRoomState? serverRoomState,
            bool shouldRecordInHubScene)
        {
            if (isForce)
            {
                return true;
            }

            if (serverRoomState == null)
            {
                return false;
            }

            if (serverRoomState == Hub.PersistentData.eServerRoomState.InGame)
            {
                return true;
            }

            return serverRoomState == Hub.PersistentData.eServerRoomState.PreGame
                   && shouldRecordInHubScene;
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

        internal static bool VanillaWouldSyncRecordedEvent(bool isForce) =>
            VanillaWouldSyncRecordedEvent(isForce, GameSessionAccess.TryGetPdata()?.serverRoomState);

        internal static bool VanillaWouldSyncRecordedEvent(
            bool isForce,
            Hub.PersistentData.eServerRoomState? serverRoomState)
        {
            if (serverRoomState == null)
            {
                return false;
            }

            return serverRoomState == Hub.PersistentData.eServerRoomState.InGame || isForce;
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

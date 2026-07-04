using MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone;
using MimesisPlayerEnhancement.Features.DeadPlayerFeatures.MimicPossession;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures
{
    internal static class DeadPlayerFeaturesRuntime
    {
        private const string Feature = "DeadPlayerFeatures";

        internal static void OnUpdate()
        {
            UpdateHostTimers();
            UpdatePhoneCamera();
            UpdateClientUi();
            UpdateLocalSessionTimeouts();
        }

        internal static void RefreshFromConfig()
        {
            if (!ModConfig.EnableDeadPlayerFeatures.Value
                || !ModConfig.EnableDeadPlayerPhoneRing.Value)
            {
                ResetPhoneFeatureState(endVoice: true, restoreCamera: true);
            }
        }

        internal static void OnDungeonEnter()
        {
            ResetDungeonState(endVoice: true, restoreCamera: true);
            MimicPossessionResolver.RefreshFromDungeonLifecycle();
            DeadPlayerPhoneResolver.RefreshFromDungeonLifecycle();
            ModLog.Debug(Feature, "Dungeon entered — dead-player feature state initialized");
        }

        internal static void OnDungeonEnd()
        {
            ResetDungeonState(endVoice: true, restoreCamera: true);
            ModLog.Debug(Feature, "Dungeon ended — dead-player feature state reset");
        }

        private static void ResetDungeonState(bool endVoice, bool restoreCamera)
        {
            ResetPhoneFeatureState(endVoice, restoreCamera);
            MimicPossessionSessions.ClearAll();
        }

        private static void ResetPhoneFeatureState(bool endVoice, bool restoreCamera)
        {
            DeadPlayerPhoneServer.ClearAll();

            if (endVoice)
            {
                DeadPlayerPhoneVoice.EndTalk();
            }

            if (restoreCamera && DeadPlayerPhoneCamera.IsLocked)
            {
                DeadPlayerPhoneCamera.Exit();
            }
            else
            {
                DeadPlayerPhoneCamera.ForceReset();
            }

            DeadPlayerPhoneLocalState.Clear();
            DeadPlayerPhoneClient.Reset();
            DeadPlayerPhoneUiSetup.Reset();
        }

        private static void UpdatePhoneCamera()
        {
            if (!DeadPlayerPhoneLocalState.HasActiveLocalSession)
            {
                return;
            }

            DeadPlayerPhoneCamera.UpdateFollow();
        }

        private static void UpdateHostTimers()
        {
            if (!DeadPlayerPhoneResolver.ShouldApplyHost)
            {
                return;
            }

            VWorld? vworld = GameSessionAccess.TryGetVWorld();
            if (vworld == null)
            {
                return;
            }

            if (ReflectionHelper.GetFieldValue(vworld, "_vrooms") is not Dictionary<long, IVroom> rooms)
            {
                return;
            }

            foreach (IVroom room in rooms.Values)
            {
                if (room is DungeonRoom)
                {
                    DeadPlayerPhoneServer.ProcessHostTimers(room);
                }
            }
        }

        private static void UpdateClientUi()
        {
            if (!DeadPlayerPhoneResolver.IsPhoneRingEnabled || Hub.Main == null)
            {
                return;
            }

            UIPrefab_Spectator? spectator = Hub.Main.spectatorui;
            if (spectator == null)
            {
                return;
            }

            DeadPlayerPhoneUi.UpdateSpectatorHints(
                spectator,
                DeadPlayerPhoneUiSetup.GetPossessionKeyText(spectator),
                DeadPlayerPhoneUiSetup.GetOrCreatePhoneKeyText(spectator),
                DeadPlayerPhoneUiSetup.GetProgressFill(spectator));
        }

        private static void UpdateLocalSessionTimeouts()
        {
            if (!DeadPlayerPhoneLocalState.HasActiveLocalSession)
            {
                return;
            }

            if (DeadPlayerPhoneLocalState.GetRemainingSeconds() > 0f)
            {
                return;
            }

            if (DeadPlayerPhoneLocalState.Phase == DeadPlayerPhoneSessionPhase.Talking)
            {
                DeadPlayerPhoneVoice.EndTalk();
            }

            DeadPlayerPhoneLocalSession.Clear(endVoice: false);
        }
    }
}

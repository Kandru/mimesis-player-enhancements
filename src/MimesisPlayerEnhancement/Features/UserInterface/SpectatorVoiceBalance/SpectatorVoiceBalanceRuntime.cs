using System.Reflection;
using MimesisPlayerEnhancement.Features.MoreVoices;
using MimesisPlayerEnhancement.Ui;
using Mimic.Voice;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.SpectatorVoiceBalance
{
    internal static class SpectatorVoiceBalanceRuntime
    {
        private const string Feature = "Ui";
        private const float VolumeApplyEpsilon = 0.001f;

        private static readonly FieldInfo? HubCameramanField =
            typeof(Hub).GetField(
                "cameraman",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly Dictionary<string, float> BaselineByPlayerId = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, float> ContinuityByPlayerId = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, float> LastAppliedByPlayerId = new(StringComparer.Ordinal);

        private static SpectatorVoiceBalanceMode _mode = SpectatorVoiceBalanceMode.Vanilla;
        private static float _attenuation = 0.8f;
        private static float _duckLevel = 0.2f;
        private static bool _wasActive;

        internal static void RefreshFromConfig()
        {
            ReadConfigFromMod();
            if (_mode == SpectatorVoiceBalanceMode.Vanilla && _wasActive)
            {
                RestoreAllAndClear();
            }
        }

        internal static void OnUpdate()
        {
            if (_mode == SpectatorVoiceBalanceMode.Vanilla)
            {
                if (_wasActive)
                {
                    RestoreAllAndClear();
                }

                return;
            }

            VoiceManager? voiceman = MoreVoicesVoiceAccess.TryGetVoiceManager();
            if (voiceman == null)
            {
                if (_wasActive)
                {
                    RestoreAllAndClear();
                }

                return;
            }

            bool isPossessing = MoreVoicesVoiceAccess.IsLocalPlayerPossessingMimic();
            bool isSpectatingDead = !isPossessing && IsLocalSpectatingDead();
            bool active = SpectatorVoiceBalanceResolver.IsFeatureActive(isSpectatingDead, isPossessing);
            if (!active)
            {
                if (_wasActive)
                {
                    RestoreAllAndClear();
                }

                return;
            }

            _wasActive = true;
            bool invertPriority = isPossessing;
            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            float deltaTime = Time.deltaTime;
            bool prioritySpeakingContinuously = false;
            HashSet<string> seenPlayerIds = new(StringComparer.Ordinal);

            foreach (FishNetDissonancePlayer player in voiceman.Players)
            {
                if (!TryGetRemoteActor(player, out bool remoteIsDead))
                {
                    continue;
                }

                SpectatorVoiceGroup group = SpectatorVoiceBalanceResolver.ClassifyGroup(remoteIsDead, invertPriority);
                if (group == SpectatorVoiceGroup.Priority)
                {
                    float continuity = UpdateContinuity(player.PlayerId, IsSpeaking(voiceman, player.PlayerId), deltaTime);
                    if (continuity > SpectatorVoiceBalanceResolver.SpeechContinuityThresholdSeconds)
                    {
                        prioritySpeakingContinuously = true;
                    }
                }
            }

            foreach (FishNetDissonancePlayer player in voiceman.Players)
            {
                if (!TryGetRemoteActor(player, out bool remoteIsDead))
                {
                    continue;
                }

                if (IsMuted(uiManager, player.steamID))
                {
                    continue;
                }

                SpectatorVoiceGroup group = SpectatorVoiceBalanceResolver.ClassifyGroup(remoteIsDead, invertPriority);
                float baseline = ResolveBaseline(voiceman, uiManager, player);
                float multiplier = SpectatorVoiceBalanceResolver.ResolveTargetMultiplier(
                    _mode,
                    group,
                    prioritySpeakingContinuously,
                    _attenuation,
                    _duckLevel);
                ApplyIfChanged(voiceman, player.PlayerId, baseline * multiplier);
                seenPlayerIds.Add(player.PlayerId);
            }

            PruneUntracked(seenPlayerIds);
        }

        internal static void OnSessionEnded()
        {
            try
            {
                RestoreAllAndClear();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Spectator voice balance restore failed on session end — {ex.Message}");
                ClearState();
            }
        }

        private static void ReadConfigFromMod()
        {
            if (!ModConfig.IsInitialized)
            {
                _mode = SpectatorVoiceBalanceMode.Vanilla;
                _attenuation = 0.8f;
                _duckLevel = 0.2f;
                return;
            }

            SpectatorVoiceBalanceResolver.TryParseMode(
                ModConfig.SpectatorVoiceBalanceMode.Value,
                out _mode);
            _attenuation = Mathf.Clamp01(ModConfig.SpectatorVoiceAttenuation.Value);
            _duckLevel = Mathf.Clamp01(ModConfig.SpectatorVoiceDuckLevel.Value);
        }

        private static bool IsLocalSpectatingDead()
        {
            VPlayer? localPlayer = MoreVoicesVoiceAccess.TryGetLocalVPlayer();
            if (localPlayer == null || localPlayer.IsAliveStatus())
            {
                return false;
            }

            if (!TryGetCameraman(out CameraManager? cameraman) || cameraman == null)
            {
                return false;
            }

            return cameraman.IsSpectatorMode
                && cameraman.Mode != CameraManager.CameraMode.MimicPossession;
        }

        private static bool TryGetRemoteActor(FishNetDissonancePlayer player, out bool remoteIsDead)
        {
            remoteIsDead = false;
            if (player == null || player.IsOwner)
            {
                return false;
            }

            if (player.ProtoActorCache == null)
            {
                return false;
            }

            remoteIsDead = player.ProtoActorCache.dead;
            return true;
        }

        private static bool IsSpeaking(VoiceManager voiceman, string playerId) =>
            voiceman.TryGetVoiceAmplitude(playerId, out _, out bool isSpeaking) && isSpeaking;

        private static float UpdateContinuity(string playerId, bool isSpeaking, float deltaTime)
        {
            float continuity = isSpeaking
                ? (ContinuityByPlayerId.TryGetValue(playerId, out float current) ? current : 0f) + deltaTime
                : 0f;
            ContinuityByPlayerId[playerId] = continuity;
            return continuity;
        }

        private static bool IsMuted(UIManager? uiManager, string steamId)
        {
            if (uiManager == null || string.IsNullOrEmpty(steamId))
            {
                return false;
            }

            return uiManager.tempPlayerVolumeMuteDictionary.TryGetValue(steamId, out bool muted) && muted;
        }

        private static float ResolveBaseline(VoiceManager voiceman, UIManager? uiManager, FishNetDissonancePlayer player)
        {
            if (uiManager != null
                && !string.IsNullOrEmpty(player.steamID)
                && uiManager.tempPlayerVolumeDictionary.TryGetValue(player.steamID, out float sliderValue))
            {
                BaselineByPlayerId[player.PlayerId] = sliderValue;
                return sliderValue;
            }

            if (BaselineByPlayerId.TryGetValue(player.PlayerId, out float cached))
            {
                return cached;
            }

            float seeded = voiceman.GetPlayerVolume(player.PlayerId);
            BaselineByPlayerId[player.PlayerId] = seeded;
            return seeded;
        }

        private static void ApplyIfChanged(VoiceManager voiceman, string playerId, float targetVolume)
        {
            if (LastAppliedByPlayerId.TryGetValue(playerId, out float lastApplied)
                && Mathf.Abs(lastApplied - targetVolume) < VolumeApplyEpsilon)
            {
                return;
            }

            voiceman.SetPlayerVolume(playerId, targetVolume);
            LastAppliedByPlayerId[playerId] = targetVolume;
        }

        private static void PruneUntracked(HashSet<string> seenPlayerIds)
        {
            VoiceManager? voiceman = MoreVoicesVoiceAccess.TryGetVoiceManager();
            List<string> departed = new();
            foreach (string playerId in LastAppliedByPlayerId.Keys)
            {
                if (!seenPlayerIds.Contains(playerId))
                {
                    departed.Add(playerId);
                }
            }

            if (voiceman != null)
            {
                foreach (string playerId in departed)
                {
                    if (BaselineByPlayerId.TryGetValue(playerId, out float baseline))
                    {
                        voiceman.SetPlayerVolume(playerId, baseline);
                    }
                }
            }

            foreach (string playerId in departed)
            {
                ContinuityByPlayerId.Remove(playerId);
                LastAppliedByPlayerId.Remove(playerId);
                BaselineByPlayerId.Remove(playerId);
            }
        }

        private static void RestoreAllAndClear()
        {
            VoiceManager? voiceman = MoreVoicesVoiceAccess.TryGetVoiceManager();
            if (voiceman != null)
            {
                foreach (KeyValuePair<string, float> entry in BaselineByPlayerId)
                {
                    voiceman.SetPlayerVolume(entry.Key, entry.Value);
                }
            }

            ClearState();
        }

        private static void ClearState()
        {
            BaselineByPlayerId.Clear();
            ContinuityByPlayerId.Clear();
            LastAppliedByPlayerId.Clear();
            _wasActive = false;
        }

        private static bool TryGetCameraman(out CameraManager? cameraman)
        {
            cameraman = null;
            if (Hub.s == null || HubCameramanField == null)
            {
                return false;
            }

            cameraman = HubCameramanField.GetValue(Hub.s) as CameraManager;
            return cameraman != null;
        }
    }
}

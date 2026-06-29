using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using MelonLoader;
using MimesisPlayerEnhancement.Features.MorePlayers;
using ReluNetwork.ConstEnum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeLobbyController
    {
        private const string Feature = "JoinAnytime";
        private const float RefreshIntervalSeconds = 30f;

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Regex DisplaySuffixPattern = new(
            @"\s*(\[(open|wait \d+ min)\])?\s*\(\d+/\d+\)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly FieldInfo? DispatcherLobbyNameField =
            typeof(SteamInviteDispatcher).GetField("lobbyName", InstanceFlags);

        private static string _baseLobbyName = string.Empty;
        private static string _lastPublishedName = string.Empty;
        private static JoinAnytimeSessionPhase _lastPhase = JoinAnytimeSessionPhase.None;
        private static float _nextRefreshTime;
        private static bool _refreshCoroutineRunning;

        internal static void OnUpdate()
        {
            if (!ModConfig.EnableJoinAnytime.Value || !IsHost())
            {
                return;
            }

            JoinAnytimeSessionPhase phase = JoinAnytimeRoomTools.ResolveHostPhase();
            if (phase != JoinAnytimeSessionPhase.None && phase != _lastPhase)
            {
                RefreshLobbyState(force: true);
            }

            if (Time.time >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.time + RefreshIntervalSeconds;
                RefreshLobbyState(force: false);
            }
        }

        internal static void OnLobbyCreated(SteamInviteDispatcher dispatcher, bool isOpenForRandomMatch)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            CaptureBaseFromDispatcher(dispatcher);
            ModLog.Debug(
                Feature,
                $"Lobby created — publicMatch={isOpenForRandomMatch}, baseName=\"{_baseLobbyName}\"");

            RefreshLobbyState(force: true);
            ScheduleImmediateRefresh();
        }

        internal static void OnSetLobbyPublicCompleted(SteamInviteDispatcher dispatcher, bool requestedPublic)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            bool hostWantsPublic = JoinAnytimeHub.IsHostLobbyPublic(dispatcher);
            ModLog.Debug(
                Feature,
                $"SetLobbyPublic completed — hostWantsPublic={hostWantsPublic}, requested={requestedPublic}");

            ApplyLobbyPresence(dispatcher, hostWantsPublic);
            RefreshLobbyState(force: true);
        }

        internal static void RefreshAfterSteamLobbyDataUpdate()
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            if (dispatcher == null)
            {
                return;
            }

            bool hostWantsPublic = JoinAnytimeHub.IsHostLobbyPublic(dispatcher);
            ModLog.Debug(Feature, $"Steam lobby data refresh — hostWantsPublic={hostWantsPublic}");

            try
            {
                if (hostWantsPublic)
                {
                    dispatcher.SetLobbyPublic(true);
                    ApplyLobbyPresence(dispatcher, wantsPublic: true);
                }
                else
                {
                    dispatcher.SetLobbyPublic(false);
                }
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Lobby visibility refresh failed — {ex.Message}");
            }

            RefreshLobbyState(force: true);
        }

        internal static void OnPublicRoomNameChanged(string rawLobbyName)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            SetBaseLobbyName(rawLobbyName);
            RefreshLobbyState(force: true);
        }

        internal static void SetBaseLobbyName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return;
            }

            _baseLobbyName = StripDisplaySuffix(rawName.Trim());
        }

        internal static void ApplyLobbyPresence(SteamInviteDispatcher dispatcher, bool wantsPublic)
        {
            if (!wantsPublic)
            {
                return;
            }

            JoinAnytimeSessionPhase phase = JoinAnytimeRoomTools.ResolveHostPhase();
            int sessionCount = JoinAnytimeRoomTools.GetSessionPlayerCount();
            int waitingThreshold = Math.Min(4, MorePlayersPatches.GetMaxPlayers());

            if (phase == JoinAnytimeSessionPhase.Maintenance && sessionCount >= waitingThreshold)
            {
                dispatcher.SetPresenceInLobbyPublic();
            }
            else if (phase == JoinAnytimeSessionPhase.Maintenance)
            {
                dispatcher.SetPresenceInLobbyPublicWaiting();
            }
            else
            {
                dispatcher.SetPresenceInLobbyPublic();
            }
        }

        internal static bool ShouldBlockPublicRoomClose()
        {
            if (!ModConfig.EnableJoinAnytime.Value || !IsHost())
            {
                return false;
            }

            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            return dispatcher != null && JoinAnytimeHub.IsHostLobbyPublic(dispatcher);
        }

        internal static void RefreshLobbyState(bool force)
        {
            if (!ModConfig.EnableJoinAnytime.Value || !IsHost())
            {
                return;
            }

            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            if (dispatcher == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_baseLobbyName))
            {
                CaptureBaseFromDispatcher(dispatcher);
            }

            JoinAnytimeSessionPhase phase = JoinAnytimeRoomTools.ResolveHostPhase();
            if (phase == JoinAnytimeSessionPhase.None)
            {
                return;
            }

            string displayName = BuildDisplayLobbyName(phase);
            if (!force && string.Equals(displayName, _lastPublishedName, StringComparison.Ordinal))
            {
                return;
            }

            _lastPhase = phase;
            PublishLobbyState(dispatcher, phase, displayName);
        }

        private static void PublishLobbyState(
            SteamInviteDispatcher dispatcher,
            JoinAnytimeSessionPhase phase,
            string displayName)
        {
            bool joinsOpen = JoinAnytimeRoomTools.AreJoinsOpen();
            string phaseKey = phase switch
            {
                JoinAnytimeSessionPhase.Maintenance => JoinAnytimeLobbyMetadata.PhaseMaintenance,
                JoinAnytimeSessionPhase.Tram => JoinAnytimeLobbyMetadata.PhaseTram,
                JoinAnytimeSessionPhase.Dungeon => JoinAnytimeLobbyMetadata.PhaseDungeon,
                _ => string.Empty,
            };

            try
            {
                dispatcher.UpdateLobbyData(JoinAnytimeLobbyMetadata.JoinPhaseKey, phaseKey);
                dispatcher.UpdateLobbyData(JoinAnytimeLobbyMetadata.JoinOpenKey, joinsOpen.ToString().ToLowerInvariant());
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Lobby metadata update failed — {ex.Message}");
            }

            if (JoinAnytimeHub.IsHostLobbyPublic(dispatcher))
            {
                try
                {
                    dispatcher.SetLobbyPublic(true);
                }
                catch (Exception ex)
                {
                    ModLog.Debug(Feature, $"SetLobbyPublic during refresh failed — {ex.Message}");
                }

                ApplyLobbyPresence(dispatcher, wantsPublic: true);
            }

            _lastPublishedName = displayName;
            try
            {
                dispatcher.SetLobbyName(displayName);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"SetLobbyName failed — {ex.Message}");
            }

            try
            {
                int sessionCount = JoinAnytimeRoomTools.GetSessionPlayerCount();
                dispatcher.UpdatePlayerGroupSize(sessionCount);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"UpdatePlayerGroupSize failed — {ex.Message}");
            }
        }

        private static string BuildDisplayLobbyName(JoinAnytimeSessionPhase phase)
        {
            string baseName = string.IsNullOrEmpty(_baseLobbyName) ? "Train" : _baseLobbyName;
            string tag = phase switch
            {
                JoinAnytimeSessionPhase.Dungeon when JoinAnytimeRoomTools.IsDungeonReadyForLobbyDisplay() =>
                    $" [wait {JoinAnytimeRoomTools.GetDungeonRemainingMinutes()} min]",
                JoinAnytimeSessionPhase.Maintenance
                    or JoinAnytimeSessionPhase.Tram
                    or JoinAnytimeSessionPhase.Dungeon => " [open]",
                _ => string.Empty,
            };

            int current = JoinAnytimeRoomTools.GetSessionPlayerCount();
            int max = MorePlayersPatches.GetMaxPlayers();
            return $"{baseName}{tag} ({current}/{max})";
        }

        internal static void OnHostSceneReady()
        {
            if (!ModConfig.EnableJoinAnytime.Value || !IsHost())
            {
                return;
            }

            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            if (dispatcher == null)
            {
                return;
            }

            CaptureBaseFromDispatcher(dispatcher);
            _lastPhase = JoinAnytimeSessionPhase.None;
            _lastPublishedName = string.Empty;
            RefreshLobbyState(force: true);
            ScheduleImmediateRefresh();
        }

        private static string StripDisplaySuffix(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return DisplaySuffixPattern.Replace(value, string.Empty).TrimEnd();
        }

        private static void CaptureBaseFromDispatcher(SteamInviteDispatcher dispatcher)
        {
            string? raw = DispatcherLobbyNameField?.GetValue(dispatcher) as string;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                SetBaseLobbyName(raw);
            }
        }

        private static void ScheduleImmediateRefresh()
        {
            if (_refreshCoroutineRunning)
            {
                return;
            }

            _refreshCoroutineRunning = true;
            _ = MelonCoroutines.Start(DeferredRefreshCoroutine());
        }

        private static IEnumerator DeferredRefreshCoroutine()
        {
            yield return null;
            _refreshCoroutineRunning = false;
            RefreshLobbyState(force: true);
        }

        private static bool IsHost()
        {
            return JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Host;
        }
    }
}

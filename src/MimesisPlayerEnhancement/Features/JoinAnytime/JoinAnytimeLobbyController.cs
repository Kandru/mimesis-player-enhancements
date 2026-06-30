using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using MelonLoader;
using MimesisPlayerEnhancement.Features.MorePlayers;
using ReluNetwork.ConstEnum;
using UnityEngine;
using UnityEngine.UI;

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

        private static readonly MethodInfo? GetL10NTextMethod =
            typeof(Hub).GetMethod(
                "GetL10NText",
                BindingFlags.Static | BindingFlags.Public,
                null,
                [typeof(string)],
                null);

        private static readonly MethodInfo? GetComponentInChildrenMethod =
            typeof(Component).GetMethod(
                "GetComponentInChildren",
                InstanceFlags,
                null,
                [typeof(Type)],
                null);

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
            bool phaseChanged = phase != JoinAnytimeSessionPhase.None && phase != _lastPhase;
            bool timerDue = Time.time >= _nextRefreshTime;
            if (!phaseChanged && !timerDue)
            {
                return;
            }

            if (timerDue)
            {
                _nextRefreshTime = Time.time + RefreshIntervalSeconds;
            }

            RefreshLobbyState(force: phaseChanged);
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

        internal static void OnPublicRoomNameChanged(UIPrefab_InGameMenu menu, string rawLobbyName)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            SetBaseLobbyName(rawLobbyName);
            RefreshLobbyState(force: true);
            SyncInGameMenuRoomNameField(menu, _lastPublishedName);
        }

        private static void SyncInGameMenuRoomNameField(UIPrefab_InGameMenu menu, string displayName)
        {
            if (menu == null || string.IsNullOrEmpty(displayName) || !IsHost())
            {
                return;
            }

            try
            {
                PropertyInfo? inputFieldProp = typeof(UIPrefab_InGameMenu).GetProperty(
                    "UE_InputFieldRoomName",
                    InstanceFlags);
                object? inputField = inputFieldProp?.GetValue(menu);
                if (inputField != null)
                {
                    PropertyInfo? textProp = inputField.GetType().GetProperty("text");
                    textProp?.SetValue(inputField, displayName);
                }

                ((Selectable)menu.UE_ChangeRoomNameButton).interactable = false;

                Type? tmpTextType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
                if (tmpTextType != null && GetComponentInChildrenMethod != null)
                {
                    object? label = GetComponentInChildrenMethod.Invoke(
                        menu.UE_ChangeRoomNameButton,
                        [tmpTextType]);
                    PropertyInfo? labelTextProp = tmpTextType.GetProperty("text");
                    labelTextProp?.SetValue(label, GetAppliedButtonLabel());
                }
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"In-game menu lobby name sync failed — {ex.Message}");
            }
        }

        private static string GetAppliedButtonLabel()
        {
            return GetL10NTextMethod?.Invoke(null, ["STRING_PUBLIC_TRAM_BUTTON_APPLIED"]) as string
                ?? "Applied";
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

            int waitMinutes = 0;
            if (phase == JoinAnytimeSessionPhase.Dungeon)
            {
                JoinAnytimeRoomTools.TryGetActiveDungeonWaitMinutes(out waitMinutes);
            }

            string displayName = BuildDisplayLobbyName(phase, waitMinutes);
            if (!force && string.Equals(displayName, _lastPublishedName, StringComparison.Ordinal))
            {
                return;
            }

            _lastPhase = phase;
            PublishLobbyState(dispatcher, phase, displayName, JoinAnytimeRoomTools.AreJoinsOpen());
        }

        private static void PublishLobbyState(
            SteamInviteDispatcher dispatcher,
            JoinAnytimeSessionPhase phase,
            string displayName,
            bool joinsOpen)
        {
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

        private static string BuildDisplayLobbyName(JoinAnytimeSessionPhase phase, int waitMinutes)
        {
            string baseName = string.IsNullOrEmpty(_baseLobbyName) ? "Train" : _baseLobbyName;
            string tag = phase == JoinAnytimeSessionPhase.Dungeon && waitMinutes > 0
                ? $" [wait {waitMinutes} min]"
                : phase is JoinAnytimeSessionPhase.Maintenance
                    or JoinAnytimeSessionPhase.Tram
                    or JoinAnytimeSessionPhase.Dungeon
                    ? " [open]"
                    : string.Empty;

            return $"{baseName}{tag} ({JoinAnytimeRoomTools.GetSessionPlayerCount()}/{MorePlayersPatches.GetMaxPlayers()})";
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
            LateJoinManager.OnHostSceneReady();
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

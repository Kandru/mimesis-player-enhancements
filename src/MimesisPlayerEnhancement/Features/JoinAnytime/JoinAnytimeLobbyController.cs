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

        private static readonly PropertyInfo? InGameMenuRoomNameFieldProp =
            typeof(UIPrefab_InGameMenu).GetProperty("UE_InputFieldRoomName", InstanceFlags);

        private static readonly Type? TmpTextType =
            Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");

        private static string _baseLobbyName = string.Empty;
        private static string _lastPublishedName = string.Empty;
        private static JoinAnytimeSessionPhase _lastPhase = JoinAnytimeSessionPhase.None;
        private static float _nextRefreshTime;
        private static bool _refreshCoroutineRunning;
        private static bool _hostWantsPublicMatchmaking;

        internal static bool HostWantsPublicMatchmaking() => _hostWantsPublicMatchmaking;

        internal static void SetHostWantsPublicMatchmaking(bool wantsPublic)
        {
            _hostWantsPublicMatchmaking = wantsPublic;
        }

        /// <summary>
        /// Host explicitly changed the public-room toggle in the ESC menu.
        /// </summary>
        internal static void ApplyUserPublicMatchmakingChoice(bool wantsPublic)
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

            SetHostWantsPublicMatchmaking(wantsPublic);
            ModLog.Debug(Feature, $"Public room toggle — wantsPublic={wantsPublic}");
            dispatcher.SetLobbyPublic(wantsPublic);
        }

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

        internal static void OnLobbyCreated(
            SteamInviteDispatcher dispatcher,
            bool isOpenForRandomMatch,
            bool isRetryAttempt)
        {
            if (!ModConfig.EnableJoinAnytime.Value || isRetryAttempt)
            {
                return;
            }

            if (isOpenForRandomMatch)
            {
                SetHostWantsPublicMatchmaking(true);
            }

            CaptureBaseFromDispatcher(dispatcher);
            ModLog.Debug(
                Feature,
                $"Lobby created — publicMatch={isOpenForRandomMatch}, baseName=\"{_baseLobbyName}\"");

            if (isOpenForRandomMatch)
            {
                ApplyHostPublicLobbyIntent();
            }

            RefreshLobbyState(force: true);
            ScheduleImmediateRefresh();
        }

        internal static void OnSetLobbyPublicCompleted(SteamInviteDispatcher dispatcher, bool requestedPublic)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (!requestedPublic && _hostWantsPublicMatchmaking)
            {
                ModLog.Debug(Feature, "Ignored SetLobbyPublic(false) — host still wants public matchmaking.");
                RestoreToggleDisplay();
                return;
            }

            if (!requestedPublic)
            {
                SetHostWantsPublicMatchmaking(false);
            }

            ModLog.Debug(
                Feature,
                $"SetLobbyPublic completed — requested={requestedPublic}, hostWantsPublic={_hostWantsPublicMatchmaking}");

            RefreshLobbyState(force: true);
        }

        internal static void ApplyHostPublicLobbyIntent()
        {
            if (!ModConfig.EnableJoinAnytime.Value || !IsHost() || !_hostWantsPublicMatchmaking)
            {
                return;
            }

            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            if (dispatcher == null)
            {
                return;
            }

            ModLog.Debug(Feature, "Applying host public lobby intent.");
            RestoreToggleDisplay();
            JoinAnytimeHub.SyncIsPublicRoomField(dispatcher, isPublic: true);
            JoinAnytimeHub.SyncIsPublicLobby(isPublic: true);
            WritePublicRoomSteamData(dispatcher, isPublic: true);
            ApplyLobbyPresence(dispatcher, wantsPublic: true);
            dispatcher.SetLobbyPublic(true);
        }

        private static void RestoreToggleDisplay()
        {
            if (!JoinAnytimeHub.IsHost())
            {
                return;
            }

            UIPrefab_InGameMenu? menu = JoinAnytimeHub.GetInGameMenu();
            if (menu == null)
            {
                return;
            }

            JoinAnytimeInGameMenuTools.SyncToggleDisplay(menu, _hostWantsPublicMatchmaking);
        }

        private static void WritePublicRoomSteamData(SteamInviteDispatcher dispatcher, bool isPublic)
        {
            try
            {
                dispatcher.UpdateLobbyData(
                    SteamInviteDispatcher.IS_PUBLIC_KEY,
                    isPublic.ToString().ToLowerInvariant());
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"PublicRoom lobby data write failed — {ex.Message}");
            }
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
                object? inputField = InGameMenuRoomNameFieldProp?.GetValue(menu);
                if (inputField != null)
                {
                    PropertyInfo? textProp = inputField.GetType().GetProperty("text");
                    textProp?.SetValue(inputField, displayName);
                }

                ((Selectable)menu.UE_ChangeRoomNameButton).interactable = false;

                if (TmpTextType != null && GetComponentInChildrenMethod != null)
                {
                    object? label = GetComponentInChildrenMethod.Invoke(
                        menu.UE_ChangeRoomNameButton,
                        [TmpTextType]);
                    PropertyInfo? labelTextProp = TmpTextType.GetProperty("text");
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

            return _hostWantsPublicMatchmaking;
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

            int sessionCount = JoinAnytimeRoomTools.GetSessionPlayerCount();
            string displayName = BuildDisplayLobbyName(phase, waitMinutes, sessionCount);
            if (!force && string.Equals(displayName, _lastPublishedName, StringComparison.Ordinal))
            {
                return;
            }

            _lastPhase = phase;
            PublishLobbyState(dispatcher, phase, displayName, JoinAnytimeRoomTools.AreJoinsOpen(), sessionCount);
        }

        private static void PublishLobbyState(
            SteamInviteDispatcher dispatcher,
            JoinAnytimeSessionPhase phase,
            string displayName,
            bool joinsOpen,
            int sessionCount)
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
                dispatcher.UpdatePlayerGroupSize(sessionCount);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"UpdatePlayerGroupSize failed — {ex.Message}");
            }
        }

        private static string BuildDisplayLobbyName(
            JoinAnytimeSessionPhase phase,
            int waitMinutes,
            int sessionCount)
        {
            string baseName = string.IsNullOrEmpty(_baseLobbyName) ? "Train" : _baseLobbyName;
            string tag = phase == JoinAnytimeSessionPhase.Dungeon && waitMinutes > 0
                ? $" [join in {waitMinutes} min]"
                : phase is JoinAnytimeSessionPhase.Maintenance
                    or JoinAnytimeSessionPhase.Tram
                    or JoinAnytimeSessionPhase.Dungeon
                    ? " [open]"
                    : string.Empty;

            return $"{baseName}{tag} ({sessionCount}/{MorePlayersPatches.GetMaxPlayers()})";
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

        internal static void ScheduleDeferredLobbyRefresh()
        {
            ScheduleImmediateRefresh();
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

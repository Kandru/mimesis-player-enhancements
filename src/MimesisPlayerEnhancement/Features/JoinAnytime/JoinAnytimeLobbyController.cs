using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using MelonLoader;
using ReluNetwork.ConstEnum;
using Steamworks;
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
            @"\s*(\[(join now|join in \d+ min|open|wait \d+ min)\])?\s*\(\d+/\d+\)$",
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

        private const string DefaultBaseLobbyName = "Train";

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
            if (wantsPublic)
            {
                JoinAnytimeHub.SyncIsPublicRoomField(dispatcher, isPublic: true);
                JoinAnytimeHub.SyncIsPublicLobby(isPublic: true);
                WritePublicRoomSteamData(dispatcher, isPublic: true);
            }
            else
            {
                JoinAnytimeHub.SyncIsPublicRoomField(dispatcher, isPublic: false);
                JoinAnytimeHub.SyncIsPublicLobby(isPublic: false);
                WritePublicRoomSteamData(dispatcher, isPublic: false);
            }

            LogPublicLobbyMessage(
                $"Public room toggle — {FormatLobbyVisibilityStatus(dispatcher)}",
                dispatcher);
            PersistLobbyRuntimeState(isPublicLobby: wantsPublic);
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

            EnsureSidecarLoadedForActiveSlot();

            int slotId = GameSessionAccess.GetSaveSlotId();
            if (!SaveSlotDocumentStore.HasPersistedPublicPreference(slotId) && isOpenForRandomMatch)
            {
                SetHostWantsPublicMatchmaking(true);
            }

            EnsureBaseLobbyName(dispatcher);
            LogPublicLobbyMessage(
                $"Lobby created — {FormatLobbyVisibilityStatus(dispatcher)}, publicMatch={isOpenForRandomMatch}",
                dispatcher);

            RestorePublicLobbyIfNeeded();

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
                ModLog.Debug(Feature, "SetLobbyPublic(false) ignored — lobby remains public");
                RestoreToggleDisplay();
                return;
            }

            if (!requestedPublic)
            {
                SetHostWantsPublicMatchmaking(false);
                JoinAnytimeHub.SyncIsPublicRoomField(dispatcher, isPublic: false);
                JoinAnytimeHub.SyncIsPublicLobby(isPublic: false);
                WritePublicRoomSteamData(dispatcher, isPublic: false);
                PersistLobbyRuntimeState(isPublicLobby: false);
            }

            LogPublicLobbyMessage(
                $"SetLobbyPublic completed — {FormatLobbyVisibilityStatus(dispatcher)}",
                dispatcher);

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

            LogPublicLobbyMessage("Applying public lobby intent", dispatcher);
            RestoreToggleDisplay();
            JoinAnytimeHub.SyncIsPublicRoomField(dispatcher, isPublic: true);
            JoinAnytimeHub.SyncIsPublicLobby(isPublic: true);
            WritePublicRoomSteamData(dispatcher, isPublic: true);
            EnsureLobbySlotAvailable(dispatcher);
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

        internal static void SetBaseLobbyName(string rawName, bool persist = true)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return;
            }

            _baseLobbyName = StripDisplaySuffix(rawName.Trim());
            if (persist)
            {
                PersistLobbyRuntimeState(baseLobbyName: _baseLobbyName);
            }
        }

        internal static void ApplyLobbyPresence(SteamInviteDispatcher dispatcher, bool wantsPublic)
        {
            if (!wantsPublic)
            {
                return;
            }

            JoinAnytimeSessionPhase phase = JoinAnytimeRoomTools.ResolveHostPhase();
            int sessionCount = JoinAnytimeRoomTools.GetSessionPlayerCount();
            int waitingThreshold = Math.Min(4, MorePlayersPatchHelpers.GetMaxPlayers());

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
                EnsureBaseLobbyName(dispatcher);
            }

            JoinAnytimeSessionPhase phase = JoinAnytimeRoomTools.ResolveHostPhase();
            if (phase == JoinAnytimeSessionPhase.None)
            {
                ModLog.Debug(Feature, "Lobby refresh skipped — host phase is None");
                return;
            }

            int waitMinutes = 0;
            if (phase == JoinAnytimeSessionPhase.Dungeon)
            {
                JoinAnytimeRoomTools.TryGetActiveDungeonWaitMinutes(out waitMinutes);
            }

            int advertisedCount = GetAdvertisedPlayerCount();
            string displayName = BuildDisplayLobbyName(phase, waitMinutes);
            bool joinsOpen = JoinAnytimeRoomTools.AreJoinsOpen();
            if (!force && string.Equals(displayName, _lastPublishedName, StringComparison.Ordinal))
            {
                return;
            }

            _lastPhase = phase;
            PublishLobbyState(dispatcher, phase, displayName, joinsOpen, advertisedCount);
        }

        /// <summary>
        /// Player count advertised to Steam via <see cref="SteamInviteDispatcher.UpdatePlayerGroupSize"/>.
        /// Uses the vanilla browse scale from <see cref="JoinAnytimeLobbyDisplay"/> (e.g. 3/4) so
        /// the lobby stays visible in the public room list even when MorePlayers raises the real cap.
        /// </summary>
        private static int GetAdvertisedPlayerCount()
        {
            int sessionCount = JoinAnytimeRoomTools.GetSessionPlayerCount();
            return JoinAnytimeLobbyDisplay.GetBrowsePlayerCount(sessionCount);
        }

        /// <summary>
        /// Steam hides lobbies without free slots from lobby-list results
        /// (AddRequestLobbyListFilterSlotsAvailable). Keep the real member limit one above the
        /// current member count so a full public lobby stays discoverable; the in-game player cap
        /// is still enforced by the session-level checks.
        /// </summary>
        private static void EnsureLobbySlotAvailable(SteamInviteDispatcher dispatcher)
        {
            try
            {
                CSteamID lobbyId = dispatcher.joinedLobbyID;
                if (lobbyId == CSteamID.Nil)
                {
                    return;
                }

                int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                int desiredLimit = Math.Max(MorePlayersPatchHelpers.GetMaxPlayers(), memberCount + 1);
                if (SteamMatchmaking.GetLobbyMemberLimit(lobbyId) != desiredLimit)
                {
                    SteamMatchmaking.SetLobbyMemberLimit(lobbyId, desiredLimit);
                }
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Lobby member limit update failed — {ex.Message}");
            }
        }

        private static void PublishLobbyState(
            SteamInviteDispatcher dispatcher,
            JoinAnytimeSessionPhase phase,
            string displayName,
            bool joinsOpen,
            int advertisedCount)
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
                EnsureLobbySlotAvailable(dispatcher);
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
                dispatcher.UpdatePlayerGroupSize(advertisedCount);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"UpdatePlayerGroupSize failed — {ex.Message}");
            }

            LogPublicLobbyMessage(
                $"Lobby updated — {FormatLobbyVisibilityStatus(dispatcher, joinsOpen)}, phase={phase}",
                dispatcher);
        }

        private static string FormatLobbyVisibilityStatus(SteamInviteDispatcher? dispatcher, bool? joinsOpen = null)
        {
            if (dispatcher == null)
            {
                return "intent=unknown";
            }

            string joins = joinsOpen.HasValue
                ? $", joinsOpen={joinsOpen.Value.ToString().ToLowerInvariant()}"
                : string.Empty;

            return $"intent={(_hostWantsPublicMatchmaking ? "public" : "private")}, "
                + $"steamPublic={JoinAnytimeHub.ReadPublicRoomFromSteam(dispatcher).ToString().ToLowerInvariant()}"
                + joins;
        }

        private static void LogPublicLobbyMessage(string message, SteamInviteDispatcher? dispatcher)
        {
            if (dispatcher != null)
            {
                if (!_hostWantsPublicMatchmaking && JoinAnytimeHub.ReadPublicRoomFromSteam(dispatcher))
                {
                    ModLog.Warn(
                        Feature,
                        "Lobby intent is private but Steam PublicRoom is still true — public browse may still list this lobby");
                }
                else if (_hostWantsPublicMatchmaking
                    && TryReadLobbySnapshot(dispatcher, out LobbySnapshot data)
                    && data.SlotsFree <= 0)
                {
                    ModLog.Warn(
                        Feature,
                        $"Public lobby may be hidden from browse — {data.Format()}");
                }
            }

            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            string snapshot = TryFormatLobbySnapshot(dispatcher);
            ModLog.Debug(Feature, string.IsNullOrEmpty(snapshot) ? message : $"{message} — {snapshot}");
        }

        private static string TryFormatLobbySnapshot(SteamInviteDispatcher? dispatcher)
        {
            return dispatcher != null && TryReadLobbySnapshot(dispatcher, out LobbySnapshot data)
                ? data.Format()
                : string.Empty;
        }

        private static bool TryReadLobbySnapshot(SteamInviteDispatcher dispatcher, out LobbySnapshot snapshot)
        {
            snapshot = default;
            try
            {
                CSteamID lobbyId = dispatcher.joinedLobbyID;
                if (lobbyId == CSteamID.Nil)
                {
                    return false;
                }

                int maxPlayers = MorePlayersPatchHelpers.GetMaxPlayers();
                int steam = JoinAnytimeRoomTools.GetSessionPlayerCount();
                int steamMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                int memberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

                snapshot = new LobbySnapshot(
                    steam,
                    GetAdvertisedPlayerCount(),
                    maxPlayers,
                    memberLimit,
                    memberLimit > 0 ? memberLimit - steamMembers : 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private readonly struct LobbySnapshot
        {
            internal LobbySnapshot(
                int steam,
                int advertised,
                int maxPlayers,
                int limit,
                int slotsFree)
            {
                Steam = steam;
                Advertised = advertised;
                MaxPlayers = maxPlayers;
                Limit = limit;
                SlotsFree = slotsFree;
            }

            internal int Steam { get; }
            internal int Advertised { get; }
            internal int MaxPlayers { get; }
            internal int Limit { get; }
            internal int SlotsFree { get; }

            internal string Format() =>
                $"steam={Steam}, advertised={Advertised}/{JoinAnytimeLobbyDisplay.VanillaBrowseDenominator}, limit={Limit}, slotsFree={SlotsFree}";
        }

        private static string BuildDisplayLobbyName(
            JoinAnytimeSessionPhase phase,
            int waitMinutes)
        {
            int sessionCount = JoinAnytimeRoomTools.GetSessionPlayerCount();
            string baseName = ResolveBaseLobbyName();
            string tag = phase == JoinAnytimeSessionPhase.Dungeon && waitMinutes > 0
                ? $" [join in {waitMinutes} min]"
                : phase is JoinAnytimeSessionPhase.Maintenance
                    or JoinAnytimeSessionPhase.Tram
                    or JoinAnytimeSessionPhase.Dungeon
                    ? " [join now]"
                    : string.Empty;

            return $"{baseName}{tag} ({sessionCount}/{MorePlayersPatchHelpers.GetMaxPlayers()})";
        }

        internal static void OnHostSceneReady()
        {
            if (!ModConfig.EnableJoinAnytime.Value || !IsHost())
            {
                return;
            }

            EnsureSidecarLoadedForActiveSlot();

            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            if (dispatcher == null)
            {
                return;
            }

            EnsureBaseLobbyName(dispatcher);
            _lastPhase = JoinAnytimeSessionPhase.None;
            _lastPublishedName = string.Empty;

            RestorePublicLobbyIfNeeded();

            RefreshLobbyState(force: true);
            LateJoinManager.OnHostSceneReady();
            ScheduleImmediateRefresh();
        }

        internal static void OnSaveSlotSidecarLoaded(int slotId)
        {
            ApplyPersistedLobbySettings(slotId);
        }

        internal static void ApplyPersistedLobbySettings(int slotId)
        {
            _baseLobbyName = string.Empty;
            _lastPublishedName = string.Empty;
            _hostWantsPublicMatchmaking = false;

            if (!MimesisSaveManager.IsValidSaveSlotId(slotId)
                || !SaveSlotDocumentStore.TryReadFromDisk(slotId, out Config.Models.SaveSlotDocument? data)
                || data?.Lobby == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(data.Lobby.BaseLobbyName))
            {
                _baseLobbyName = data.Lobby.BaseLobbyName.Trim();
            }

            if (data.Lobby.IsPublicLobby.HasValue)
            {
                _hostWantsPublicMatchmaking = data.Lobby.IsPublicLobby.Value;
            }
        }

        internal static void OnSessionEnded()
        {
            _baseLobbyName = string.Empty;
            _lastPublishedName = string.Empty;
            _hostWantsPublicMatchmaking = false;
        }

        private static void EnsureBaseLobbyName(SteamInviteDispatcher dispatcher)
        {
            if (!string.IsNullOrEmpty(_baseLobbyName))
            {
                return;
            }

            int slotId = GameSessionAccess.GetSaveSlotId();
            if (MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                string? fromDocument = SaveSlotDocumentStore.TryReadLobbyNameForSlot(slotId);
                if (!string.IsNullOrWhiteSpace(fromDocument))
                {
                    _baseLobbyName = fromDocument;
                    return;
                }
            }

            CaptureBaseFromDispatcher(dispatcher);
        }

        private static void RestorePublicLobbyIfNeeded()
        {
            if (!_hostWantsPublicMatchmaking)
            {
                int slotId = GameSessionAccess.GetSaveSlotId();
                if (MimesisSaveManager.IsValidSaveSlotId(slotId))
                {
                    ApplyPersistedLobbySettings(slotId);
                }
            }

            if (!_hostWantsPublicMatchmaking)
            {
                return;
            }

            ApplyHostPublicLobbyIntent();
            ScheduleDeferredPublicLobbyIntent();
        }

        internal static bool TryExportLobbyState(out string? baseLobbyName, out bool isPublicLobby)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                baseLobbyName = null;
                isPublicLobby = false;
                return false;
            }

            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            if (dispatcher != null)
            {
                EnsureBaseLobbyName(dispatcher);
            }

            baseLobbyName = ResolveBaseLobbyName();
            isPublicLobby = _hostWantsPublicMatchmaking;
            return true;
        }

        private static string ResolveBaseLobbyName()
        {
            return string.IsNullOrWhiteSpace(_baseLobbyName)
                ? DefaultBaseLobbyName
                : _baseLobbyName.Trim();
        }

        private static void PersistLobbyRuntimeState(string? baseLobbyName = null, bool? isPublicLobby = null)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            int slotId = GameSessionAccess.GetSaveSlotId();
            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            SaveSlotDocumentStore.RememberLobbyRuntimeState(slotId, baseLobbyName, isPublicLobby);
        }

        private static void EnsureSidecarLoadedForActiveSlot()
        {
            int slotId = GameSessionAccess.GetSaveSlotId();
            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            SaveSlotSidecarPersistence.EnsureSaveSlotLoaded(slotId);
            ApplyPersistedLobbySettings(slotId);
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
                SetBaseLobbyName(raw, persist: false);
            }
        }

        internal static void OnSessionRosterChanged()
        {
            if (!ModConfig.EnableJoinAnytime.Value || !IsHost())
            {
                return;
            }

            ScheduleDeferredLobbyRefresh();
        }

        internal static void ScheduleDeferredLobbyRefresh()
        {
            ScheduleImmediateRefresh();
        }

        private static void ScheduleDeferredPublicLobbyIntent()
        {
            if (!_hostWantsPublicMatchmaking)
            {
                return;
            }

            _ = MelonCoroutines.Start(DeferredPublicLobbyIntentCoroutine());
        }

        private static IEnumerator DeferredPublicLobbyIntentCoroutine()
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                yield return null;

                if (!ModConfig.EnableJoinAnytime.Value || !IsHost() || !_hostWantsPublicMatchmaking)
                {
                    yield break;
                }

                SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
                if (dispatcher == null)
                {
                    continue;
                }

                ApplyHostPublicLobbyIntent();
                if (JoinAnytimeHub.IsHostLobbyPublic(dispatcher))
                {
                    RefreshLobbyState(force: true);
                    yield break;
                }
            }

            ModLog.Debug(Feature, "Deferred public lobby intent exhausted retries — lobby may still be private.");
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

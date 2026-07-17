using System.Linq;
using System.Reflection;
using MimesisPlayerEnhancement.Features.UserInterface.FpsUi;
using ReluReplay;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplayGameAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? HubReplayManagerField =
            typeof(Hub).GetField("replayManager", InstanceFlags);

        private static readonly FieldInfo? HubCameramanField =
            typeof(Hub).GetField("cameraman", InstanceFlags);

        private static readonly FieldInfo? HubVoicemanField =
            typeof(Hub).GetField("voiceman", InstanceFlags);

        private static readonly FieldInfo? IngameUiField =
            AccessTools.Field(typeof(GameMainBase), "ingameui");

        private static readonly FieldInfo? InventoryUiField =
            AccessTools.Field(typeof(GameMainBase), "inventoryui");

        private static readonly FieldInfo? SpectatorUiField =
            AccessTools.Field(typeof(GameMainBase), "spectatorui");

        private static readonly FieldInfo? SpeechEventArchivesField =
            typeof(VoiceManager).GetField("speechEventArchives", InstanceFlags);

        private static Type? _cachedInputManagerType;
        private static PropertyInfo? _keyboardProperty;
        private static PropertyInfo? _f10KeyProperty;
        private static PropertyInfo? _wasPressedThisFrameProperty;
        private static object? _cachedKeyboard;
        private static object? _cachedF10Key;

        internal static ReplayManager? TryGetReplayManager()
        {
            if (Hub.s == null || HubReplayManagerField == null)
            {
                return null;
            }

            return HubReplayManagerField.GetValue(Hub.s) as ReplayManager;
        }

        internal static CameraManager? TryGetCameraManager()
        {
            if (Hub.s == null || HubCameramanField == null)
            {
                return null;
            }

            return HubCameramanField.GetValue(Hub.s) as CameraManager;
        }

        internal static SpeechEventArchive? TryGetSpeechEventArchive()
        {
            if (Hub.s == null || HubVoicemanField == null || SpeechEventArchivesField == null)
            {
                return null;
            }

            if (HubVoicemanField.GetValue(Hub.s) is not VoiceManager voiceman)
            {
                return null;
            }

            if (SpeechEventArchivesField.GetValue(voiceman) is not List<SpeechEventArchive> archives
                || archives.Count == 0)
            {
                return null;
            }

            return archives[0];
        }

        internal static object? TryGetInputManager() =>
            SaveSlotGameAccess.TryGetInputManager();

        internal static bool WasF10PressedThisFrame()
        {
            object? inputman = TryGetInputManager();
            if (inputman == null)
            {
                return false;
            }

            EnsureF10ReflectionCached(inputman);
            if (_cachedKeyboard == null || _cachedF10Key == null || _wasPressedThisFrameProperty == null)
            {
                return false;
            }

            return _wasPressedThisFrameProperty.GetValue(_cachedF10Key) is true;
        }

        internal static GameMainBase? TryGetMain() => GameSessionAccess.TryGetPdata()?.main;

        internal static bool TryCycleSpectatorTarget(int delta)
        {
            if (delta == 0 || TryGetMain() is not GameMainBase main)
            {
                return false;
            }

            CameraManager? camera = TryGetCameraManager();
            if (camera == null)
            {
                return false;
            }

            if (!TryEnterSpectatorMode())
            {
                return false;
            }

            List<ProtoActor> alivePlayers = main.GetAllPlayers()
                .Where(static player => player != null && !player.dead)
                .OrderBy(static player => player.ActorID)
                .ToList();
            if (alivePlayers.Count == 0)
            {
                return false;
            }

            int currentIndex = -1;
            if (camera.TryGetCurrentSpectatorTarget(out ProtoActor? current) && current != null)
            {
                currentIndex = alivePlayers.FindIndex(player => player.ActorID == current.ActorID);
            }

            int nextIndex = currentIndex < 0
                ? 0
                : (currentIndex + delta + alivePlayers.Count) % alivePlayers.Count;
            if (currentIndex == nextIndex && currentIndex >= 0)
            {
                return false;
            }

            ProtoActor nextTarget = alivePlayers[nextIndex];
            camera.ChangeSpectatorCameraTarget(nextTarget.nickName);
            main.UpdateInventoryUI(nextTarget);
            RefreshSpectatorOverlayState(main);
            return camera.TryGetCurrentSpectatorTarget(out ProtoActor? verified)
                && verified != null
                && verified.ActorID == nextTarget.ActorID;
        }

        internal static bool TryShowSpectatorHud()
        {
            if (TryGetMain() is not GameMainBase main)
            {
                return false;
            }

            main.ShowSpectatorHUD();
            RefreshSpectatorOverlayState(main);
            return true;
        }

        internal static bool TryHideSpectatorPlayerUi()
        {
            if (TryGetMain() is not GameMainBase main)
            {
                return false;
            }

            bool preserveFpsOverlay = FpsUiOverlay.IsEnabled();

            if (SpectatorUiField?.GetValue(main) is UIPrefab_Spectator spectatorUi)
            {
                spectatorUi.Hide();
            }

            if (InventoryUiField?.GetValue(main) is UIPrefab_Inventory inventoryUi)
            {
                if (preserveFpsOverlay)
                {
                    inventoryUi.gameObject.SetActive(false);
                }
                else
                {
                    inventoryUi.Hide();
                }
            }

            if (IngameUiField?.GetValue(main) is UIPrefab_InGame ingameUi)
            {
                if (preserveFpsOverlay)
                {
                    ingameUi.gameObject.SetActive(false);
                }
                else
                {
                    ingameUi.Hide();
                }
            }

            return true;
        }

        private static void RefreshSpectatorOverlayState(GameMainBase main)
        {
            CameraManager? camera = TryGetCameraManager();
            if (camera == null || !camera.TryGetCurrentSpectatorTarget(out ProtoActor? actor) || actor == null)
            {
                return;
            }

            main.UpdateInventoryUI(actor);

            if (IngameUiField?.GetValue(main) is not UIPrefab_InGame ingameUi)
            {
                return;
            }

            if (FpsUiOverlay.IsEnabled())
            {
                FpsUiOverlay.ForceHideOxyGauge(ingameUi);
                FpsUiOverlay.NotifyInventoryShown();
                FpsUiOverlay.UpdateHealth(
                    ingameUi,
                    actor.netSyncActorData.hp,
                    actor.netSyncActorData.maxHP,
                    actor.dead);
                FpsUiOverlay.UpdateConta(
                    ingameUi,
                    actor.netSyncActorData.conta,
                    actor.netSyncActorData.maxConta);
            }
        }

        internal static bool TryEnterSpectatorMode()
        {
            CameraManager? camera = TryGetCameraManager();
            if (camera == null)
            {
                return false;
            }

            if (camera.Mode != CameraManager.CameraMode.Normal && camera.IsSpectatorMode)
            {
                return true;
            }

            return camera.ChangeCameraPlayerToSpectator();
        }

        internal static bool IsSpectatorVerified(CameraManager camera) =>
            camera.Mode == CameraManager.CameraMode.PlayerSpectate && camera.IsSpectatorMode;

        internal static bool TryResolveGameplaySceneName(
            int dungeonMasterId,
            int pickedMapId,
            out string sceneName)
        {
            sceneName = string.Empty;
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return false;
            }

            int resolvedMapId = pickedMapId;
            if (resolvedMapId <= 0)
            {
                DungeonMasterInfo? dungeonInfo = excel.GetDungeonInfo(dungeonMasterId);
                resolvedMapId = dungeonInfo?.PickMapID() ?? 0;
            }

            MapMasterInfo? mapInfo = excel.GetMapInfo(resolvedMapId);
            if (mapInfo == null || string.IsNullOrEmpty(mapInfo.SceneName))
            {
                return false;
            }

            sceneName = mapInfo.SceneName;
            return true;
        }

        private static void EnsureF10ReflectionCached(object inputman)
        {
            Type inputManagerType = inputman.GetType();
            if (_cachedInputManagerType != inputManagerType)
            {
                _cachedInputManagerType = inputManagerType;
                _cachedKeyboard = null;
                _cachedF10Key = null;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
                _keyboardProperty = inputManagerType.GetProperty("keyboard", flags);
                _wasPressedThisFrameProperty = null;
            }

            if (_keyboardProperty == null)
            {
                return;
            }

            if (_cachedKeyboard == null)
            {
                _cachedKeyboard = _keyboardProperty.GetValue(inputman);
                if (_cachedKeyboard == null)
                {
                    return;
                }

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
                _f10KeyProperty = _cachedKeyboard.GetType().GetProperty("f10Key", flags);
            }

            if (_f10KeyProperty == null)
            {
                return;
            }

            if (_cachedF10Key == null)
            {
                _cachedF10Key = _f10KeyProperty.GetValue(_cachedKeyboard);
                if (_cachedF10Key == null)
                {
                    return;
                }

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
                _wasPressedThisFrameProperty =
                    _cachedF10Key.GetType().GetProperty("wasPressedThisFrame", flags);
            }
        }
    }
}

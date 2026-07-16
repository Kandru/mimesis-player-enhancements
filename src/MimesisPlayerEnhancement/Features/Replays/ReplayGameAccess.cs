using System.Reflection;
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
            if (Hub.s == null || HubVoicemanField == null)
            {
                return null;
            }

            if (HubVoicemanField.GetValue(Hub.s) is not VoiceManager voiceman)
            {
                return null;
            }

            FieldInfo? archivesField = typeof(VoiceManager).GetField(
                "speechEventArchives",
                InstanceFlags);
            if (archivesField?.GetValue(voiceman) is not List<SpeechEventArchive> archives
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

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (inputman.GetType().GetProperty("keyboard", flags)?.GetValue(inputman) is not object keyboard)
            {
                return false;
            }

            if (keyboard.GetType().GetProperty("f10Key", flags)?.GetValue(keyboard) is not object f10Key)
            {
                return false;
            }

            return f10Key.GetType().GetProperty("wasPressedThisFrame", flags)?.GetValue(f10Key) is true;
        }

        internal static GameMainBase? TryGetMain() => GameSessionAccess.TryGetPdata()?.main;

        internal static bool TrySetInGameUiVisible(bool visible)
        {
            if (TryGetMain() is not GameMainBase main)
            {
                return false;
            }

            if (IngameUiField?.GetValue(main) is not UIPrefab_InGame ingameUi)
            {
                return false;
            }

            if (visible)
            {
                ingameUi.Show();
            }
            else
            {
                ingameUi.Hide();
            }

            return true;
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
    }
}

using System.Reflection;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotGameAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // game@0.3.1 Assembly-CSharp PlatformMgr.Load (generic)
        private static readonly MethodInfo? LoadSaveMethod =
            AccessTools.Method(typeof(PlatformMgr), "Load")?.MakeGenericMethod(typeof(MMSaveGameData));

        private static UIPrefab_LoadTram? _loadTram;
        private static UIPrefab_NewTram? _newTram;
        private static UIPrefab_NewTramPopUp? _newTramPopUp;

        internal static void ClearCachedUi()
        {
            _loadTram = null;
            _newTram = null;
            _newTramPopUp = null;
        }

        internal static string GetL10NText(string key, params object[] formattingArgs)
        {
            return GameLocaleAccess.GetL10NText(key, formattingArgs);
        }

        internal static UIManager? TryGetUiManager() => Ui.ModUiGameAccess.TryGetUiManager();

        internal static Hub.PersistentData? TryGetPdata()
        {
            return GameSessionAccess.TryGetPdata();
        }

        // game@0.3.1 Assembly-CSharp/Hub.cs inputman
        internal static object? TryGetInputManager()
        {
            if (Hub.s == null)
            {
                return null;
            }

            FieldInfo? field = typeof(Hub).GetField("inputman", InstanceFlags)
                ?? typeof(Hub).GetField("<inputman>k__BackingField", InstanceFlags);
            PropertyInfo? property = typeof(Hub).GetProperty("inputman", InstanceFlags);
            return field?.GetValue(Hub.s) ?? property?.GetValue(Hub.s);
        }

        internal static void TryPlaySfx(string sfxId) => Ui.ModUiGameAccess.TryPlaySfx(sfxId);

        internal static MMSaveGameData? LoadSaveData(PlatformMgr platformMgr, string fileName)
        {
            if (LoadSaveMethod == null)
            {
                return null;
            }

            try
            {
                return LoadSaveMethod.Invoke(platformMgr, [fileName]) as MMSaveGameData;
            }
            catch
            {
                return null;
            }
        }

#pragma warning disable CS0618
        internal static UIPrefab_LoadTram? TryFindHiddenLoadTram()
        {
            if (_loadTram != null)
            {
                return _loadTram;
            }

            _loadTram = UnityEngine.Object.FindObjectOfType<UIPrefab_LoadTram>(true);
            return _loadTram;
        }

        internal static UIPrefab_NewTram? TryFindHiddenNewTram()
        {
            if (_newTram != null)
            {
                return _newTram;
            }

            _newTram = UnityEngine.Object.FindObjectOfType<UIPrefab_NewTram>(true);
            return _newTram;
        }

        internal static UIPrefab_NewTramPopUp? TryFindHiddenNewTramPopUp()
        {
            if (_newTramPopUp != null)
            {
                return _newTramPopUp;
            }

            _newTramPopUp = UnityEngine.Object.FindObjectOfType<UIPrefab_NewTramPopUp>(true);
            return _newTramPopUp;
        }
#pragma warning restore CS0618
    }
}

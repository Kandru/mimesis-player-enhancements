using System.Reflection;
using ReluProtocol;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotGameAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly MethodInfo? GetL10NTextMethod =
            AccessTools.Method(typeof(Hub), "GetL10NText", [typeof(string), typeof(object[])]);

        private static readonly MethodInfo? LoadSaveMethod =
            AccessTools.Method(typeof(PlatformMgr), "Load")?.MakeGenericMethod(typeof(MMSaveGameData));

        internal static string GetL10NText(string key, params object[] formattingArgs)
        {
            if (GetL10NTextMethod != null)
            {
                return GetL10NTextMethod.Invoke(null, [key, formattingArgs]) as string ?? key;
            }

            return key;
        }

        internal static UIManager? TryGetUiManager() => Ui.ModUiGameAccess.TryGetUiManager();

        internal static Hub.PersistentData? TryGetPdata()
        {
            return GameSessionAccess.TryGetPdata();
        }

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
        internal static UIPrefab_LoadTram? TryFindHiddenLoadTram() =>
            Object.FindObjectOfType<UIPrefab_LoadTram>(true);

        internal static UIPrefab_NewTram? TryFindHiddenNewTram() =>
            Object.FindObjectOfType<UIPrefab_NewTram>(true);

        internal static UIPrefab_NewTramPopUp? TryFindHiddenNewTramPopUp() =>
            Object.FindObjectOfType<UIPrefab_NewTramPopUp>(true);
#pragma warning restore CS0618
    }
}

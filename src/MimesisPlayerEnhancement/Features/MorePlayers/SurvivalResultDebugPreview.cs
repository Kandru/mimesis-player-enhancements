using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class SurvivalResultDebugPreview
    {
        private const string Feature = "MorePlayers";
        private const float DebugDialogDurationSeconds = 3600f;

        private static UIPrefabScript? _lastInstance;

        internal static bool Show(IReadOnlyList<string> fakeNames)
        {
            MMUIPrefabTable? uiprefabs = ResolveUiPrefabTable();
            if (uiprefabs == null)
            {
                return false;
            }

            try
            {
                uiprefabs.ShowTimerDialog(
                    "SurvivalResult",
                    DebugDialogDurationSeconds,
                    BuildFakeParameters(fakeNames));
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SurvivalResult debug preview failed — {ex.Message}");
                return false;
            }
        }

        internal static void Hide()
        {
            if (_lastInstance != null)
            {
                UnityEngine.Object.Destroy(_lastInstance.gameObject);
            }

            _lastInstance = null;
        }

        internal static void OnSessionEnded() => Hide();

        internal static void CaptureInstance(object instance)
        {
            _lastInstance = instance as UIPrefabScript;
        }

        private static object[] BuildFakeParameters(IReadOnlyList<string> fakeNames)
        {
            Type? survivalResultType = AccessTools.TypeByName("UIPrefab_SurvivalResult");
            Type? stateEnumType = survivalResultType?.GetNestedType("eActorSurvivalState");

            List<object> parameters = [1, true, fakeNames.Count];
            for (int i = 0; i < fakeNames.Count; i++)
            {
                parameters.Add(fakeNames[i]);
                object state = stateEnumType != null
                    ? Enum.ToObject(stateEnumType, i % 3)
                    : i % 3;
                parameters.Add(state);
                parameters.Add(AwardType.None);
            }

            parameters.Add(0);
            return parameters.ToArray();
        }

        private static MMUIPrefabTable? ResolveUiPrefabTable()
        {
            if (Hub.s == null)
            {
                return null;
            }

            object? tableman = AccessTools.Property(typeof(Hub), "tableman")?.GetValue(Hub.s);
            return tableman == null
                ? null
                : AccessTools.Field(tableman.GetType(), "uiprefabs")?.GetValue(tableman) as MMUIPrefabTable;
        }
    }
}

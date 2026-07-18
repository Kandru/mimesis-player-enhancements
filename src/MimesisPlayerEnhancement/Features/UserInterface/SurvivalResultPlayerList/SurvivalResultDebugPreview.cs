using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.SurvivalResultPlayerList
{
    internal static class SurvivalResultDebugPreview
    {
        private const string Feature = "Ui";
        private const float DebugDialogDurationSeconds = 3600f;

        private static UIPrefabScript? _lastInstance;

        internal static bool Show(IReadOnlyList<string> fakeNames)
        {
            Hide();
            SurvivalResultTimerDialogAccess.ClearSurvivalResultTimerDialogState();

            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            MMUIPrefabTable? uiprefabs = ResolveUiPrefabTable();
            if (uiManager == null || uiprefabs == null)
            {
                return false;
            }

            GameObject? prefab = SurvivalResultTimerDialogAccess.ResolveSurvivalResultPrefab(uiprefabs);
            if (prefab == null)
            {
                ModLog.Warn(Feature, "SurvivalResult debug preview failed — prefab not found");
                return false;
            }

            try
            {
                object[] parameters = BuildFakeParameters(fakeNames);
                UIPrefab_ClosableByTimeBase dialog =
                    uiManager.InstatiateUIPrefab<UIPrefab_ClosableByTimeBase>(prefab, eUIHeight.Main);
                if (dialog == null)
                {
                    return false;
                }

                dialog.openingDurationSec = DebugDialogDurationSeconds;
                dialog.dialogue = true;
                dialog.PatchParameter(parameters);
                dialog.Show();
                SurvivalResultPlayerGrid.RefreshVisibleLayout(dialog);
                _lastInstance = dialog;
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SurvivalResult debug preview failed — {ex.Message}");
                Hide();
                return false;
            }
        }

        internal static void Hide()
        {
            if (_lastInstance != null)
            {
                try
                {
                    _lastInstance.Hide();
                }
                catch
                {
                    // ignore
                }

                if (_lastInstance != null && _lastInstance.gameObject != null)
                {
                    UnityEngine.Object.Destroy(_lastInstance.gameObject);
                }
            }

            _lastInstance = null;
            SurvivalResultTimerDialogAccess.ClearSurvivalResultTimerDialogState();
        }

        internal static void OnSessionEnded() => Hide();

        internal static void CaptureInstance(object instance)
        {
            if (instance is UIPrefabScript script)
            {
                _lastInstance = script;
            }
        }

        private static object[] BuildFakeParameters(IReadOnlyList<string> fakeNames)
        {
            Type? survivalResultType = AccessTools.TypeByName("UIPrefab_SurvivalResult");
            Type? stateEnumType = survivalResultType?.GetNestedType("eActorSurvivalState");

            int playerCount = fakeNames.Count;
            if (ModConfig.EnableMorePlayers.Value && playerCount <= 4)
            {
                playerCount = 5;
            }

            List<object> parameters = [1, true, playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                string name = i < fakeNames.Count ? fakeNames[i] : $"Player {i + 1:00}";
                parameters.Add(name);
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

using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.SurvivalResultPlayerList
{
    internal static class SurvivalResultTimerDialogAccess
    {
        private const string SurvivalResultDialogId = "SurvivalResult";

        private static readonly MethodInfo? FindTimerDialogRowMethod =
            AccessTools.Method(typeof(MMUIPrefabTable), "FindTimerDialogRow");

        internal static GameObject? ResolveSurvivalResultPrefab(MMUIPrefabTable table)
        {
            if (FindTimerDialogRowMethod == null)
            {
                return null;
            }

            object? row = FindTimerDialogRowMethod.Invoke(table, [SurvivalResultDialogId]);
            if (row == null)
            {
                return null;
            }

            foreach (string fieldName in new[] { "prefab", "uiPrefab", "rowPrefab", "gameObject" })
            {
                if (AccessTools.Field(row.GetType(), fieldName)?.GetValue(row) is GameObject prefab)
                {
                    return prefab;
                }
            }

            return null;
        }

        internal static void ClearSurvivalResultTimerDialogState()
        {
            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return;
            }

            try
            {
                ClearCurrentTimerDialog(uiManager);
                ClearQueuedTimerDialogs(uiManager);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Ui", $"SurvivalResult timer dialog cleanup failed — {ex.Message}");
            }
        }

        private static void ClearCurrentTimerDialog(UIManager uiManager)
        {
            FieldInfo? currentField = AccessTools.Field(typeof(UIManager), "currentTimerDialog");
            if (currentField?.GetValue(uiManager) is not ValueTuple<string, GameObject, float, eUIHeight> current)
            {
                return;
            }

            if (current.Item1 == SurvivalResultDialogId)
            {
                currentField.SetValue(uiManager, default(ValueTuple<string, GameObject, float, eUIHeight>));
            }
        }

        private static void ClearQueuedTimerDialogs(UIManager uiManager)
        {
            FieldInfo? queueField = AccessTools.Field(typeof(UIManager), "openWaitingQueue");
            FieldInfo? paramsField = AccessTools.Field(typeof(UIManager), "openWaitingQueueParams");
            object? queueObj = queueField?.GetValue(uiManager);
            if (queueObj == null || paramsField?.GetValue(uiManager) is not Queue paramsQueue)
            {
                return;
            }

            List<object> entries = [];
            foreach (object entry in (IEnumerable)queueObj)
            {
                entries.Add(entry);
            }

            object[] paramEntries = new object[paramsQueue.Count];
            paramsQueue.CopyTo(paramEntries, 0);
            paramsQueue.Clear();

            MethodInfo? clearMethod = queueObj.GetType().GetMethod("Clear");
            MethodInfo? enqueueMethod = queueObj.GetType().GetMethod("Enqueue");
            clearMethod?.Invoke(queueObj, null);
            if (enqueueMethod == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] is ValueTuple<string, GameObject, float, eUIHeight> tuple
                    && tuple.Item1 == SurvivalResultDialogId)
                {
                    continue;
                }

                enqueueMethod.Invoke(queueObj, [entries[i]]);
                if (i < paramEntries.Length)
                {
                    paramsQueue.Enqueue(paramEntries[i]);
                }
            }
        }
    }
}

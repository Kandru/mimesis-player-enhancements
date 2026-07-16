using DunGen.Graph;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MimesisSeedScanner.Mod
{
    internal static class FlowCatalog
    {
        private static readonly FieldInfo? GamePlayFlowTableField =
            AccessTools.Field(typeof(GamePlayScene), "dungenFlowTable");

        private static readonly FieldInfo? FlowTableRowsField =
            AccessTools.Field(typeof(DungenFlowTable), "rows");

        private static List<FlowCatalogEntry>? _cached;

        internal static bool BootstrapInProgress { get; set; }

        internal static bool TryGetFlows(out IReadOnlyList<FlowCatalogEntry> flows)
        {
            if (_cached is { Count: > 0 })
            {
                flows = _cached;
                return true;
            }

            List<FlowCatalogEntry> discovered = CollectFromMemory();
            if (discovered.Count > 0)
            {
                _cached = discovered;
                flows = _cached;
                return true;
            }

            flows = Array.Empty<FlowCatalogEntry>();
            return false;
        }

        internal static IEnumerator BootstrapCoroutine()
        {
            if (TryGetFlows(out _))
            {
                yield break;
            }

            if (HubAccess.TryGetExcelDataManager() == null)
            {
                MelonLogger.Error("Game data not ready — wait until the main menu finishes loading.");
                yield break;
            }

            BootstrapInProgress = true;
            try
            {
                var discovered = new Dictionary<string, FlowCatalogEntry>(StringComparer.Ordinal);
                foreach (string sceneName in GetBootstrapSceneNames())
                {
                    MelonLogger.Msg($"Loading flow assets from '{sceneName}'...");
                    yield return LoadSceneAndCollectCoroutine(sceneName, discovered);
                }

                if (discovered.Count > 0)
                {
                    _cached = discovered.Values.OrderBy(entry => entry.FlowId, StringComparer.Ordinal).ToList();
                    MelonLogger.Msg($"Resolved {_cached.Count} dungeon flow(s) for scanning.");
                    yield break;
                }

                MelonLogger.Error(
                    "Could not resolve dungeon flows — no outer hub scenes exposed flow tables in this build.");
            }
            finally
            {
                BootstrapInProgress = false;
            }
        }

        private static IEnumerator LoadSceneAndCollectCoroutine(
            string sceneName,
            Dictionary<string, FlowCatalogEntry> discovered)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            AsyncOperation? loadOperation = null;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Scene '{sceneName}' is unavailable — {ex.Message}");
                yield break;
            }

            if (loadOperation == null)
            {
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene candidate = SceneManager.GetSceneAt(i);
                    if (candidate.isLoaded && candidate.name.Equals(sceneName, StringComparison.Ordinal))
                    {
                        loadedScene = candidate;
                        break;
                    }
                }
            }

            List<FlowCatalogEntry> sceneFlows = CollectFromMemory();
            foreach (FlowCatalogEntry entry in sceneFlows)
            {
                discovered.TryAdd(entry.FlowId, entry);
            }

            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                AsyncOperation? unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
                if (unloadOperation != null)
                {
                    while (!unloadOperation.isDone)
                    {
                        yield return null;
                    }
                }
            }

            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }
        }

        private static List<FlowCatalogEntry> CollectFromMemory()
        {
            var byId = new Dictionary<string, FlowCatalogEntry>(StringComparer.Ordinal);

            foreach (DungenFlowTable table in Resources.FindObjectsOfTypeAll<DungenFlowTable>())
            {
                AddFromTable(table, byId);
            }

            foreach (GamePlayScene gamePlayScene in Resources.FindObjectsOfTypeAll<GamePlayScene>())
            {
                if (GamePlayFlowTableField?.GetValue(gamePlayScene) is DungenFlowTable table)
                {
                    AddFromTable(table, byId);
                }
            }

            testDunGenEntry? testEntry = Resources.FindObjectsOfTypeAll<testDunGenEntry>().FirstOrDefault();
            DungeonFlow? testFlow = testEntry?.DungeonGenerator?.Generator?.DungeonFlow;
            if (testFlow != null)
            {
                AddFlow(byId, ResolveFlowId(testFlow), testFlow);
            }

            foreach (DungeonFlow flow in Resources.FindObjectsOfTypeAll<DungeonFlow>())
            {
                AddFlow(byId, ResolveFlowId(flow), flow);
            }

            TryAddFlowsFromExcel(byId);
            TryAddFlowsFromResources(byId);

            return byId.Values.OrderBy(entry => entry.FlowId, StringComparer.Ordinal).ToList();
        }

        private static void TryAddFlowsFromExcel(Dictionary<string, FlowCatalogEntry> byId)
        {
            ExcelDataManager? excel = HubAccess.TryGetExcelDataManager();
            if (excel == null)
            {
                return;
            }

            foreach (DungeonMasterInfo info in excel.DungeonInfoDict.Values)
            {
                foreach (string flowId in info.DungenCandidates.Keys)
                {
                    if (byId.ContainsKey(flowId))
                    {
                        continue;
                    }

                    DungeonFlow? loaded = Resources.Load<DungeonFlow>(flowId);
                    if (loaded != null)
                    {
                        AddFlow(byId, flowId, loaded);
                    }
                }
            }
        }

        private static void TryAddFlowsFromResources(Dictionary<string, FlowCatalogEntry> byId)
        {
            foreach (DungeonFlow flow in Resources.LoadAll<DungeonFlow>(string.Empty))
            {
                AddFlow(byId, ResolveFlowId(flow), flow);
            }

            foreach (DungenFlowTable table in Resources.LoadAll<DungenFlowTable>(string.Empty))
            {
                AddFromTable(table, byId);
            }
        }

        private static IEnumerable<string> GetBootstrapSceneNames()
        {
            var scenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ExcelDataManager? excel = HubAccess.TryGetExcelDataManager();
            if (excel != null)
            {
                PropertyInfo? roomInfoDictProperty =
                    typeof(ExcelDataManager).GetProperty("RoomInfoDict", BindingFlags.Public | BindingFlags.Instance);
                if (roomInfoDictProperty?.GetValue(excel) is { } roomInfoDict)
                {
                    PropertyInfo? valuesProperty = roomInfoDict.GetType().GetProperty("Values");
                    if (valuesProperty?.GetValue(roomInfoDict) is IEnumerable mapInfos)
                    {
                        PropertyInfo? sceneNameProperty = null;
                        foreach (object mapInfo in mapInfos)
                        {
                            sceneNameProperty ??= mapInfo.GetType().GetProperty(
                                "SceneName",
                                BindingFlags.Public | BindingFlags.Instance);
                            if (sceneNameProperty?.GetValue(mapInfo) is not string sceneName
                                || string.IsNullOrWhiteSpace(sceneName))
                            {
                                continue;
                            }

                            if (sceneName.StartsWith("scene_Outer", StringComparison.OrdinalIgnoreCase))
                            {
                                scenes.Add(sceneName);
                            }
                        }
                    }
                }
            }

            scenes.Add("scene_Outer_a");
            scenes.Add("scene_outer_b");
            scenes.Add("scene_Outer_c");
            scenes.Add("testDunGen");
            return scenes.OrderBy(name => name, StringComparer.Ordinal);
        }

        private static void AddFromTable(DungenFlowTable table, Dictionary<string, FlowCatalogEntry> byId)
        {
            if (FlowTableRowsField?.GetValue(table) is not IEnumerable<DungenFlowTable.Row> rows)
            {
                return;
            }

            foreach (DungenFlowTable.Row row in rows)
            {
                if (row?.flow == null || string.IsNullOrWhiteSpace(row.id))
                {
                    continue;
                }

                AddFlow(byId, row.id, row.flow);
            }
        }

        private static void AddFlow(Dictionary<string, FlowCatalogEntry> byId, string flowId, DungeonFlow flow)
        {
            if (string.IsNullOrWhiteSpace(flowId))
            {
                return;
            }

            byId.TryAdd(
                flowId,
                new FlowCatalogEntry
                {
                    FlowId = flowId,
                    Flow = flow,
                });
        }

        private static string ResolveFlowId(DungeonFlow flow) =>
            string.IsNullOrWhiteSpace(flow.name) ? "unknown_flow" : flow.name;

        internal static HashSet<string> GetProductionFlowIds()
        {
            var flowIds = new HashSet<string>(StringComparer.Ordinal);
            ExcelDataManager? excel = HubAccess.TryGetExcelDataManager();
            if (excel == null)
            {
                return flowIds;
            }

            foreach (DungeonMasterInfo info in excel.DungeonInfoDict.Values)
            {
                if (!info.IsActive)
                {
                    continue;
                }

                foreach (string flowId in info.DungenCandidates.Keys)
                {
                    if (!string.IsNullOrWhiteSpace(flowId) && ShouldScanFlow(flowId))
                    {
                        flowIds.Add(flowId);
                    }
                }
            }

            return flowIds;
        }

        internal static bool ShouldScanFlow(string flowId)
        {
            if (string.IsNullOrWhiteSpace(flowId))
            {
                return false;
            }

            return !IsVariantScanFlow(flowId);
        }

        private static bool IsVariantScanFlow(string flowId) =>
            flowId.Contains("_jackpot", StringComparison.OrdinalIgnoreCase)
            || flowId.Contains("_DL", StringComparison.OrdinalIgnoreCase)
            || flowId.Contains("devtools", StringComparison.OrdinalIgnoreCase);
    }
}

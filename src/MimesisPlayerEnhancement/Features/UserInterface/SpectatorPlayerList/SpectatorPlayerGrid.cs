using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.SpectatorPlayerList
{
    internal static class SpectatorPlayerGrid
    {
        private const string Feature = "Ui";
        private const int VanillaPlayerRows = 4;
        private const float BottomMargin = 8f;
        private const float RightMargin = 8f;
        private const float ColumnGap = 12f;
        private const float FallbackColumnWidth = 220f;

        private static readonly FieldInfo? SpectatorUiField =
            AccessTools.Field(typeof(GameMainBase), "spectatorui");

        private static readonly FieldInfo? PlayerListViewField =
            AccessTools.Field(typeof(UIPrefab_Spectator), "playerListView");

        private static readonly MethodInfo? GetActorByActorIdMethod =
            AccessTools.Method(typeof(GameMainBase), "GetActorByActorID", [typeof(int)]);

        private static readonly MethodInfo? ResolveNickNameMethod =
            AccessTools.Method(typeof(GameMainBase), "ResolveNickName", [typeof(ProtoActor), typeof(string)]);

        private static readonly FieldInfo? SpectatorIsShowField =
            AccessTools.Field(typeof(UIPrefab_Spectator), "IsShow");

        private static readonly FieldInfo? PrevChangeCameraUiField =
            AccessTools.Field(typeof(UIPrefab_Spectator), "_prevChangeCameraUI");

        private static readonly FieldInfo? NextChangeCameraUiField =
            AccessTools.Field(typeof(UIPrefab_Spectator), "_nextChangeCameraUI");

        private static readonly FieldInfo? PossessionUiField =
            AccessTools.Field(typeof(UIPrefab_Spectator), "_possessionUI");

        private static readonly FieldInfo? PossessableQuitUiField =
            AccessTools.Field(typeof(UIPrefab_Spectator), "_possessableQuitUI");

        private static readonly PropertyInfo? NextChangeTimeTextProperty =
            AccessTools.Property(typeof(UIPrefab_Spectator), "UE_nextChangeTimeText");

        private static readonly Dictionary<int, SpectatorListState> States = [];

        private static readonly System.Random DebugRandom = new();

        private static readonly CancellationTokenSource DebugSpeakCancellation = new();

        private static bool _debugActive;
        private static UIPrefab_Spectator_PlayerListView? _debugListView;
        private static UIPrefab_Spectator? _debugSpectator;
        private static bool _debugSpectatorWasVisible;

        internal static bool IsEnabled() =>
            ModConfig.EnableExtendedSpectatorPlayerList.Value;

        internal static void Initialize(UIPrefab_Spectator_PlayerListView listView)
        {
            SpectatorListState state = GetOrCreateState(listView);
            CacheNativeRows(state);

            if (IsEnabled())
            {
                ApplyExtended(state);
            }
        }

        internal static void Update(
            UIPrefab_Spectator_PlayerListView listView,
            List<Tuple<int, bool, bool, bool>> actorsInfo,
            CancellationToken cancellationToken)
        {
            if (_debugActive)
            {
                return;
            }

            if (actorsInfo.Count == 0 || Hub.Main == null)
            {
                return;
            }

            SpectatorListState state = GetOrCreateState(listView);
            CacheNativeRows(state);

            if (!IsEnabled())
            {
                return;
            }

            if (!state.ExtendedActive)
            {
                ApplyExtended(state);
            }

            RefreshLayoutIfNeeded(state, actorsInfo.Count);
            List<Tuple<int, bool, bool, bool>> visibleActors = SelectVisibleActors(actorsInfo, state.MaxVisibleSlots);
            EnsureCloneRows(state);
            BindRows(state, visibleActors, cancellationToken);
        }

        internal static void RefreshFromConfig()
        {
            bool enabled = IsEnabled();

            foreach (SpectatorListState state in States.Values.ToList())
            {
                if (state.ListView == null)
                {
                    continue;
                }

                if (enabled)
                {
                    ApplyExtended(state);
                }
                else
                {
                    RevertToVanilla(state);
                }
            }

            if (!enabled)
            {
                return;
            }

            foreach (UIPrefab_Spectator_PlayerListView listView in UnityEngine.Object.FindObjectsByType<UIPrefab_Spectator_PlayerListView>(FindObjectsSortMode.None))
            {
                SpectatorListState state = GetOrCreateState(listView);
                CacheNativeRows(state);
                if (!state.ExtendedActive)
                {
                    ApplyExtended(state);
                }
            }
        }

        internal static void HandleDisable(UIPrefab_Spectator_PlayerListView listView)
        {
            if (!States.TryGetValue(listView.GetInstanceID(), out SpectatorListState? state))
            {
                return;
            }

            StopSpeakAnimations(state);
            RevertToVanilla(state);
        }

        internal static bool DebugShow(IReadOnlyList<string> fakeNames)
        {
            EnsureSpectatorHudAvailable();

            UIPrefab_Spectator_PlayerListView? listView = ResolveListView(out UIPrefab_Spectator? spectator);
            if (listView == null)
            {
                return false;
            }

            try
            {
                _debugSpectator = spectator;
                _debugListView = listView;
                _debugSpectatorWasVisible = spectator != null
                    && spectator.gameObject.activeSelf
                    && SpectatorIsShowField?.GetValue(spectator) is true;

                if (spectator != null && !spectator.gameObject.activeSelf)
                {
                    spectator.gameObject.SetActive(true);
                }

                HideSpectatorChrome(spectator);
                listView.gameObject.SetActive(true);
                listView.Show();

                SpectatorListState state = GetOrCreateState(listView);
                CacheNativeRows(state);
                if (!state.ExtendedActive)
                {
                    ApplyExtended(state);
                }
                else
                {
                    RefreshCachedWorldLayout(state);
                }

                List<SpectatorDebugEntry> entries = BuildScrambledDebugEntries(fakeNames);
                RefreshLayoutIfNeeded(state, entries.Count);
                int visibleCount = Math.Min(state.MaxVisibleSlots, entries.Count);
                EnsureCloneRows(state);
                BindDebugRows(state, entries, visibleCount, DebugSpeakCancellation.Token);

                _debugActive = true;
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Spectator debug preview failed — {ex.Message}");
                DebugHide();
                return false;
            }
        }

        internal static void DebugHide()
        {
            _debugActive = false;

            if (_debugListView != null)
            {
                HandleDisable(_debugListView);
                _debugListView.Hide();
            }

            if (_debugSpectator != null)
            {
                if (_debugSpectatorWasVisible)
                {
                    _debugSpectator.Show();
                }
                else
                {
                    _debugSpectator.Hide();
                }
            }

            _debugListView = null;
            _debugSpectator = null;
            _debugSpectatorWasVisible = false;
        }

        internal static void EnsureSpectatorHudAvailable()
        {
            GameMainBase? main = Hub.Main;
            if (main == null || main.spectatorui != null)
            {
                return;
            }

            MethodInfo? createSpectatorHud =
                AccessTools.Method(typeof(GameMainBase), "CreateSpectatorHUD");
            if (createSpectatorHud == null)
            {
                return;
            }

            try
            {
                createSpectatorHud.Invoke(main, null);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Spectator HUD create failed — {ex.Message}");
            }
        }

        private static UIPrefab_Spectator_PlayerListView? ResolveListView(out UIPrefab_Spectator? spectator)
        {
            spectator = null;
            GameMainBase? main = Hub.Main;
            if (main?.spectatorui != null)
            {
                spectator = main.spectatorui;
                if (PlayerListViewField?.GetValue(spectator) is UIPrefab_Spectator_PlayerListView listView)
                {
                    return listView;
                }
            }

            if (main != null && SpectatorUiField?.GetValue(main) is UIPrefab_Spectator spectatorUi)
            {
                spectator = spectatorUi;
                if (PlayerListViewField?.GetValue(spectatorUi) is UIPrefab_Spectator_PlayerListView listView)
                {
                    return listView;
                }
            }

            UIPrefab_Spectator_PlayerListView[] views =
                UnityEngine.Object.FindObjectsByType<UIPrefab_Spectator_PlayerListView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                UIPrefab_Spectator_PlayerListView view = views[i];
                if (view == null)
                {
                    continue;
                }

                UIPrefab_Spectator_PlayerListViewItem[] rows =
                    view.GetComponentsInChildren<UIPrefab_Spectator_PlayerListViewItem>(includeInactive: true);
                if (rows is { Length: > 0 })
                {
                    return view;
                }
            }

            return null;
        }

        private static SpectatorListState GetOrCreateState(UIPrefab_Spectator_PlayerListView listView)
        {
            int id = listView.GetInstanceID();
            if (!States.TryGetValue(id, out SpectatorListState? state))
            {
                state = new SpectatorListState { ListView = listView };
                States[id] = state;
            }

            return state;
        }

        private static void CacheNativeRows(SpectatorListState state)
        {
            if (state.NativeRows.Length > 0)
            {
                return;
            }

            UIPrefab_Spectator_PlayerListViewItem[] rows =
                state.ListView.GetComponentsInChildren<UIPrefab_Spectator_PlayerListViewItem>(includeInactive: true);
            if (rows == null || rows.Length == 0)
            {
                return;
            }

            state.NativeRows = SortNativeRowsTopToBottom(rows);
            SpectatorPlayerRowBinder.CacheColors(state.ListView, out Color liveColor, out Color deadColor);
            state.LiveColor = liveColor;
            state.DeadColor = deadColor;
            RefreshCachedWorldLayout(state);
        }

        private static void ApplyExtended(SpectatorListState state)
        {
            if (state.NativeRows.Length == 0)
            {
                return;
            }

            RefreshLayoutIfNeeded(state);

            foreach (UIPrefab_Spectator_PlayerListViewItem row in state.NativeRows)
            {
                row.gameObject.SetActive(false);
            }

            state.ExtendedActive = true;
        }

        private static void RevertToVanilla(SpectatorListState state)
        {
            StopSpeakAnimations(state);
            DestroyCloneRows(state);

            foreach (UIPrefab_Spectator_PlayerListViewItem row in state.NativeRows)
            {
                if (row != null)
                {
                    row.gameObject.SetActive(true);
                }
            }

            state.ExtendedActive = false;
        }

        private static void RefreshLayoutIfNeeded(SpectatorListState state, int actorCount = 0)
        {
            if (state.NativeRows.Length == 0)
            {
                return;
            }

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            bool screenUnchanged = state.LastScreenWidth == screenWidth
                && state.LastScreenHeight == screenHeight
                && state.RowsPerColumn > 0
                && state.MaxColumns > 0;
            bool actorsDropped = actorCount > state.MaxVisibleSlots;
            bool capacityTooLow = actorCount > 1 && state.RowsPerColumn <= 1;
            bool needsMoreColumns = actorCount > state.RowsPerColumn && state.MaxColumns <= 1;
            if (screenUnchanged && !actorsDropped && !capacityTooLow && !needsMoreColumns)
            {
                return;
            }

            state.LastScreenWidth = screenWidth;
            state.LastScreenHeight = screenHeight;
            RefreshCachedWorldLayout(state);
            state.RowsPerColumn = ComputeRowsPerColumn(state, out float availableHeight);
            state.MaxColumns = ComputeMaxColumns(state, actorCount);
            state.MaxVisibleSlots = state.RowsPerColumn * state.MaxColumns;

            ModLog.Debug(
                Feature,
                $"Spectator list layout — availableHeight={availableHeight:F1}, rowHeight={state.RowHeight:F1}, rowsPerColumn={state.RowsPerColumn}, columns={state.MaxColumns}, maxVisible={state.MaxVisibleSlots}, columnStepPx={state.ColumnStepPixels:F1}, actors={actorCount}");
        }

        private static void RefreshCachedWorldLayout(SpectatorListState state)
        {
            if (state.NativeRows.Length == 0)
            {
                return;
            }

            UIPrefab_Spectator_PlayerListViewItem firstRow = state.NativeRows[0];
            bool restoreFirst = !firstRow.gameObject.activeSelf;
            bool restoreSecond = state.NativeRows.Length > 1 && !state.NativeRows[1].gameObject.activeSelf;
            if (restoreFirst)
            {
                firstRow.gameObject.SetActive(true);
            }

            if (restoreSecond)
            {
                state.NativeRows[1].gameObject.SetActive(true);
            }

            Canvas.ForceUpdateCanvases();
            MeasureRowMetrics(state);

            if (restoreSecond)
            {
                state.NativeRows[1].gameObject.SetActive(false);
            }

            if (restoreFirst)
            {
                firstRow.gameObject.SetActive(false);
            }
        }

        private static UIPrefab_Spectator_PlayerListViewItem[] SortNativeRowsTopToBottom(
            UIPrefab_Spectator_PlayerListViewItem[] rows)
        {
            UIPrefab_Spectator_PlayerListViewItem[] sorted = rows.ToArray();
            Array.Sort(sorted, static (left, right) =>
            {
                float leftY = left.transform is RectTransform leftRect ? leftRect.anchoredPosition.y : 0f;
                float rightY = right.transform is RectTransform rightRect ? rightRect.anchoredPosition.y : 0f;
                return rightY.CompareTo(leftY);
            });
            return sorted;
        }

        private static void MeasureRowMetrics(SpectatorListState state)
        {
            UIPrefab_Spectator_PlayerListViewItem[] nativeRows = state.NativeRows;
            if (nativeRows.Length == 0)
            {
                return;
            }

            if (nativeRows[0].transform is not RectTransform firstRect)
            {
                state.RowHeight = 24f;
                state.OriginPosition = Vector2.zero;
                state.YDirection = -1f;
                state.ColumnStepPixels = FallbackColumnWidth + ColumnGap;
                return;
            }

            state.OriginPosition = firstRect.anchoredPosition;

            if (nativeRows.Length > 1 && nativeRows[1].transform is RectTransform secondRect)
            {
                float deltaY = secondRect.anchoredPosition.y - firstRect.anchoredPosition.y;
                state.YDirection = Mathf.Approximately(deltaY, 0f) ? -1f : Mathf.Sign(deltaY);
                state.RowHeight = Mathf.Abs(deltaY);
            }
            else
            {
                state.YDirection = -1f;
                state.RowHeight = firstRect.rect.height > 1f ? firstRect.rect.height : 24f;
            }

            if (state.RowHeight <= 1f)
            {
                state.RowHeight = 24f;
            }

            float columnWidth = firstRect.sizeDelta.x;
            if (columnWidth <= 1f || columnWidth > 800f)
            {
                columnWidth = FallbackColumnWidth;
            }

            state.ColumnStepPixels = columnWidth + ColumnGap;
        }

        private static void EnsureColumnRoots(SpectatorListState state)
        {
            if (state.NativeRows[0].transform is not RectTransform templateRect)
            {
                return;
            }

            Transform nativeParent = templateRect.transform.parent;

            while (state.ColumnRoots.Count > state.MaxColumns)
            {
                int lastIndex = state.ColumnRoots.Count - 1;
                RectTransform columnRoot = state.ColumnRoots[lastIndex];
                state.ColumnRoots.RemoveAt(lastIndex);
                if (columnRoot != null)
                {
                    UnityEngine.Object.Destroy(columnRoot.gameObject);
                }
            }

            while (state.ColumnRoots.Count < state.MaxColumns)
            {
                GameObject columnObject = new($"MPE_SpectatorColumn_{state.ColumnRoots.Count + 1}");
                RectTransform columnRoot = columnObject.AddComponent<RectTransform>();
                columnRoot.SetParent(nativeParent, worldPositionStays: false);
                state.ColumnRoots.Add(columnRoot);
            }

            for (int columnIndex = 0; columnIndex < state.ColumnRoots.Count; columnIndex++)
            {
                RectTransform columnRoot = state.ColumnRoots[columnIndex];
                columnRoot.anchorMin = templateRect.anchorMin;
                columnRoot.anchorMax = templateRect.anchorMax;
                columnRoot.pivot = templateRect.pivot;
                columnRoot.sizeDelta = Vector2.zero;
                columnRoot.anchoredPosition = state.OriginPosition
                    + new Vector2(columnIndex * state.ColumnStepPixels, 0f);
            }
        }

        private static void DestroyColumnRoots(SpectatorListState state)
        {
            foreach (RectTransform columnRoot in state.ColumnRoots)
            {
                if (columnRoot != null)
                {
                    UnityEngine.Object.Destroy(columnRoot.gameObject);
                }
            }

            state.ColumnRoots.Clear();
        }

        private static void ApplyAnchoredLayout(
            RectTransform rowRect,
            RectTransform templateRect,
            Transform parent,
            Vector2 anchoredPosition)
        {
            if (rowRect.transform.parent != parent)
            {
                rowRect.SetParent(parent, worldPositionStays: false);
            }

            rowRect.anchorMin = templateRect.anchorMin;
            rowRect.anchorMax = templateRect.anchorMax;
            rowRect.pivot = templateRect.pivot;
            rowRect.anchoredPosition = anchoredPosition;
        }

        private static int ComputeRowsPerColumn(SpectatorListState state, out float availableHeight)
        {
            availableHeight = state.RowHeight;
            float canvasHeight = ResolveCanvasHeight(state.ListView);
            float topOffset = Mathf.Abs(state.OriginPosition.y) + (state.RowHeight * 0.5f);
            availableHeight = Mathf.Max(state.RowHeight, canvasHeight - topOffset - BottomMargin);
            int rows = Mathf.FloorToInt(availableHeight / state.RowHeight);
            return Mathf.Max(1, rows);
        }

        private static int ComputeMaxColumns(SpectatorListState state, int actorCount)
        {
            float step = state.ColumnStepPixels;
            if (step <= 1f)
            {
                step = FallbackColumnWidth + ColumnGap;
            }

            float canvasWidth = ResolveCanvasWidth(state.ListView);
            int columnsByWidth = Mathf.Max(1, Mathf.FloorToInt((canvasWidth - RightMargin) / step));
            if (actorCount <= state.RowsPerColumn || state.RowsPerColumn <= 0)
            {
                return columnsByWidth;
            }

            int columnsByActors = Mathf.CeilToInt(actorCount / (float)state.RowsPerColumn);
            return Mathf.Max(1, Mathf.Min(columnsByWidth, columnsByActors));
        }

        private static float ResolveCanvasWidth(UIPrefab_Spectator_PlayerListView listView)
        {
            if (listView.transform.root is RectTransform rootRect && rootRect.rect.width > 1f)
            {
                return rootRect.rect.width;
            }

            return Screen.width;
        }

        private static float ResolveCanvasHeight(UIPrefab_Spectator_PlayerListView listView)
        {
            if (listView.transform.root is RectTransform rootRect && rootRect.rect.height > 1f)
            {
                return rootRect.rect.height;
            }

            return Screen.height;
        }

        private static List<Tuple<int, bool, bool, bool>> SelectVisibleActors(
            List<Tuple<int, bool, bool, bool>> actorsInfo,
            int maxVisible)
        {
            List<(Tuple<int, bool, bool, bool> Actor, int Index, string Name)> indexed = new(actorsInfo.Count);
            for (int index = 0; index < actorsInfo.Count; index++)
            {
                Tuple<int, bool, bool, bool> actor = actorsInfo[index];
                indexed.Add((actor, index, ResolveActorDisplayName(actor.Item1)));
            }

            indexed.Sort(static (left, right) =>
            {
                int deadCompare = left.Actor.Item2.CompareTo(right.Actor.Item2);
                if (deadCompare != 0)
                {
                    return deadCompare;
                }

                int nameCompare = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
                if (nameCompare != 0)
                {
                    return nameCompare;
                }

                return left.Index.CompareTo(right.Index);
            });

            int visibleCount = Math.Min(maxVisible, indexed.Count);
            List<Tuple<int, bool, bool, bool>> visible = new(visibleCount);
            for (int index = 0; index < visibleCount; index++)
            {
                visible.Add(indexed[index].Actor);
            }

            return visible;
        }

        private static string ResolveActorDisplayName(int actorId) =>
            TryResolveActorDisplayName(actorId, out string name) ? name : string.Empty;

        private static bool TryResolveActorDisplayName(int actorId, out string name)
        {
            name = string.Empty;
            GameMainBase? main = Hub.Main;
            if (main == null || GetActorByActorIdMethod == null || ResolveNickNameMethod == null)
            {
                return false;
            }

            if (GetActorByActorIdMethod.Invoke(main, [actorId]) is not ProtoActor actor)
            {
                return false;
            }

            string actorName = actor.netSyncActorData.actorName;
            name = ResolveNickNameMethod.Invoke(main, [actor, actorName]) as string ?? actorName;
            return true;
        }

        private static void EnsureCloneRows(SpectatorListState state)
        {
            if (state.NativeRows.Length == 0)
            {
                return;
            }

            UIPrefab_Spectator_PlayerListViewItem template = state.NativeRows[0];
            int requiredRows = state.RowsPerColumn * state.MaxColumns;

            while (state.CloneRows.Count > requiredRows)
            {
                int lastIndex = state.CloneRows.Count - 1;
                UIPrefab_Spectator_PlayerListViewItem clone = state.CloneRows[lastIndex];
                state.CloneRows.RemoveAt(lastIndex);
                if (clone != null)
                {
                    SpectatorPlayerRowBinder.TurnOffSpeakAnimation(clone);
                    UnityEngine.Object.Destroy(clone.gameObject);
                }
            }

            while (state.CloneRows.Count < requiredRows)
            {
                int slotIndex = state.CloneRows.Count;
                int columnIndex = slotIndex / state.RowsPerColumn;
                Transform columnParent = state.ColumnRoots[columnIndex];
                UIPrefab_Spectator_PlayerListViewItem clone =
                    UnityEngine.Object.Instantiate(template, columnParent);
                clone.gameObject.name = $"MorePlayersSpectatorRow_{state.CloneRows.Count + 1}";
                clone.gameObject.SetActive(true);
                clone.transform.localScale = Vector3.one;
                SpectatorPlayerRowBinder.TrySetRowColor(clone, state.LiveColor);
                SpectatorPlayerRowBinder.TurnOffSpeakAnimation(clone);
                SpectatorPlayerRowBinder.SetPossessorActive(clone, false);
                state.CloneRows.Add(clone);
            }

            EnsureColumnRoots(state);

            for (int slotIndex = 0; slotIndex < state.CloneRows.Count; slotIndex++)
            {
                UIPrefab_Spectator_PlayerListViewItem row = state.CloneRows[slotIndex];
                int columnIndex = slotIndex / state.RowsPerColumn;
                int rowIndex = slotIndex % state.RowsPerColumn;
                PositionRow(state, row, columnIndex, rowIndex);
            }
        }

        private static void PositionRow(
            SpectatorListState state,
            UIPrefab_Spectator_PlayerListViewItem row,
            int columnIndex,
            int rowIndex)
        {
            if (row.transform is not RectTransform rowRect
                || state.NativeRows[0].transform is not RectTransform templateRect)
            {
                return;
            }

            SpectatorPlayerRowBinder.ApplyNormalRowLayout(row);
            if (columnIndex >= state.ColumnRoots.Count)
            {
                return;
            }

            Vector2 rowPosition = new(0f, rowIndex * state.YDirection * state.RowHeight);
            ApplyAnchoredLayout(
                rowRect,
                templateRect,
                state.ColumnRoots[columnIndex],
                rowPosition);
        }

        private static void BindRows(
            SpectatorListState state,
            List<Tuple<int, bool, bool, bool>> visibleActors,
            CancellationToken cancellationToken)
        {
            for (int slotIndex = 0; slotIndex < state.CloneRows.Count; slotIndex++)
            {
                UIPrefab_Spectator_PlayerListViewItem row = state.CloneRows[slotIndex];
                if (slotIndex >= visibleActors.Count)
                {
                    row.gameObject.SetActive(false);
                    SpectatorPlayerRowBinder.SetRowName(row, string.Empty);
                    SpectatorPlayerRowBinder.TurnOffSpeakAnimation(row);
                    SpectatorPlayerRowBinder.SetPossessorActive(row, false);
                    continue;
                }

                row.gameObject.SetActive(true);
                BindRow(state, row, visibleActors[slotIndex], cancellationToken);
            }
        }

        private static void BindRow(
            SpectatorListState state,
            UIPrefab_Spectator_PlayerListViewItem row,
            Tuple<int, bool, bool, bool> actorInfo,
            CancellationToken cancellationToken)
        {
            GameMainBase? main = Hub.Main;
            if (main == null || GetActorByActorIdMethod == null || ResolveNickNameMethod == null)
            {
                return;
            }

            int actorId = actorInfo.Item1;
            bool dead = actorInfo.Item2;
            bool speaking = actorInfo.Item3;
            bool possessor = actorInfo.Item4;

            if (!TryResolveActorDisplayName(actorId, out string name))
            {
                SpectatorPlayerRowBinder.SetRowName(row, string.Empty);
                SpectatorPlayerRowBinder.TurnOffSpeakAnimation(row);
                SpectatorPlayerRowBinder.SetPossessorActive(row, false);
                return;
            }

            BindDebugRow(state, row, name, dead, speaking, possessor, cancellationToken);
        }

        private static void BindDebugRows(
            SpectatorListState state,
            IReadOnlyList<SpectatorDebugEntry> entries,
            int visibleCount,
            CancellationToken cancellationToken)
        {
            for (int slotIndex = 0; slotIndex < state.CloneRows.Count; slotIndex++)
            {
                UIPrefab_Spectator_PlayerListViewItem row = state.CloneRows[slotIndex];
                if (slotIndex >= visibleCount)
                {
                    row.gameObject.SetActive(false);
                    SpectatorPlayerRowBinder.SetRowName(row, string.Empty);
                    SpectatorPlayerRowBinder.TurnOffSpeakAnimation(row);
                    SpectatorPlayerRowBinder.SetPossessorActive(row, false);
                    continue;
                }

                row.gameObject.SetActive(true);
                SpectatorDebugEntry entry = entries[slotIndex];
                BindDebugRow(state, row, entry.DisplayName, entry.Dead, entry.Speaking, possessor: false, cancellationToken);
            }
        }

        private static void BindDebugRow(
            SpectatorListState state,
            UIPrefab_Spectator_PlayerListViewItem row,
            string displayName,
            bool dead,
            bool speaking,
            bool possessor,
            CancellationToken cancellationToken)
        {
            SpectatorPlayerRowBinder.SetRowName(row, displayName);
            SpectatorPlayerRowBinder.TrySetRowColor(row, dead ? state.DeadColor : state.LiveColor);
            SpectatorPlayerRowBinder.EnsureSpeakIconVisible(row);
            SpectatorPlayerRowBinder.BindSpeakState(row, speaking, cancellationToken);
            SpectatorPlayerRowBinder.SetPossessorActive(row, possessor);
        }

        private static List<SpectatorDebugEntry> BuildScrambledDebugEntries(IReadOnlyList<string> fakeNames)
        {
            int count = fakeNames.Count;
            bool[] deadFlags = ScrambleTrueFlags(count, trueRatio: 0.5f);
            bool[] speakingFlags = ScrambleTrueFlags(count, trueRatio: 0.35f, ensureMix: false);

            List<SpectatorDebugEntry> entries = new(count);
            for (int index = 0; index < count; index++)
            {
                entries.Add(new SpectatorDebugEntry
                {
                    DisplayName = fakeNames[index],
                    Dead = deadFlags[index],
                    Speaking = speakingFlags[index],
                });
            }

            entries.Sort(static (left, right) =>
            {
                int deadCompare = left.Dead.CompareTo(right.Dead);
                if (deadCompare != 0)
                {
                    return deadCompare;
                }

                return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            });

            return entries;
        }

        private static bool[] ScrambleTrueFlags(int count, float trueRatio, bool ensureMix = true)
        {
            bool[] flags = new bool[count];
            if (count == 0)
            {
                return flags;
            }

            int trueCount = Mathf.Clamp(Mathf.RoundToInt(count * trueRatio), 0, count);
            if (ensureMix && count >= 2)
            {
                trueCount = Mathf.Clamp(trueCount, 1, count - 1);
            }

            for (int index = 0; index < trueCount; index++)
            {
                flags[index] = true;
            }

            for (int index = count - 1; index > 0; index--)
            {
                int swapIndex = DebugRandom.Next(index + 1);
                (flags[index], flags[swapIndex]) = (flags[swapIndex], flags[index]);
            }

            return flags;
        }

        private static void HideSpectatorChrome(UIPrefab_Spectator? spectator)
        {
            if (spectator == null)
            {
                return;
            }

            SetChromeActive(PrevChangeCameraUiField, spectator, active: false);
            SetChromeActive(NextChangeCameraUiField, spectator, active: false);
            SetChromeActive(PossessionUiField, spectator, active: false);
            SetChromeActive(PossessableQuitUiField, spectator, active: false);
            spectator.SetActiveSpectatedPlayerName(isActive: false);
            if (NextChangeTimeTextProperty?.GetValue(spectator) is Component nextChangeTimeText)
            {
                nextChangeTimeText.gameObject.SetActive(false);
            }
        }

        private static void SetChromeActive(FieldInfo? field, UIPrefab_Spectator spectator, bool active)
        {
            if (field?.GetValue(spectator) is GameObject chrome)
            {
                chrome.SetActive(active);
            }
        }

        private static void StopSpeakAnimations(SpectatorListState state)
        {
            SpectatorPlayerRowBinder.StopSpeakAnimations(state.CloneRows);
        }

        private static void DestroyCloneRows(SpectatorListState state)
        {
            foreach (UIPrefab_Spectator_PlayerListViewItem row in state.CloneRows)
            {
                if (row != null)
                {
                    UnityEngine.Object.Destroy(row.gameObject);
                }
            }

            state.CloneRows.Clear();
            DestroyColumnRoots(state);
        }

        private sealed class SpectatorDebugEntry
        {
            internal string DisplayName = string.Empty;
            internal bool Dead;
            internal bool Speaking;
        }

        private sealed class SpectatorListState
        {
            internal UIPrefab_Spectator_PlayerListView ListView = null!;
            internal UIPrefab_Spectator_PlayerListViewItem[] NativeRows = [];
            internal List<RectTransform> ColumnRoots = [];
            internal List<UIPrefab_Spectator_PlayerListViewItem> CloneRows = [];
            internal Color LiveColor = Color.white;
            internal Color DeadColor = Color.red;
            internal Vector2 OriginPosition;
            internal float ColumnStepPixels = FallbackColumnWidth + ColumnGap;
            internal float RowHeight = 24f;
            internal float YDirection = -1f;
            internal int RowsPerColumn = VanillaPlayerRows;
            internal int MaxColumns = 1;
            internal int MaxVisibleSlots = VanillaPlayerRows;
            internal int LastScreenWidth;
            internal int LastScreenHeight;
            internal bool ExtendedActive;
        }
    }
}

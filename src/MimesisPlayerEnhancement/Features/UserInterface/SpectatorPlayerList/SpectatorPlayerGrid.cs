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
        private const float ColumnGap = 12f;
        private const float BottomMargin = 8f;
        private const float FallbackColumnStep = 220f;

        private static readonly MethodInfo? GetActorByActorIdMethod =
            AccessTools.Method(typeof(GameMainBase), "GetActorByActorID", [typeof(int)]);

        private static readonly MethodInfo? ResolveNickNameMethod =
            AccessTools.Method(typeof(GameMainBase), "ResolveNickName", [typeof(ProtoActor), typeof(string)]);

        private static readonly Dictionary<int, SpectatorListState> States = [];

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
            EnsureCloneRows(state, state.MaxVisibleSlots);
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

            state.NativeRows = rows;
            SpectatorPlayerRowBinder.CacheColors(state.ListView, out Color liveColor, out Color deadColor);
            state.LiveColor = liveColor;
            state.DeadColor = deadColor;
            MeasureRowMetrics(state);
        }

        private static void ApplyExtended(SpectatorListState state)
        {
            if (state.NativeRows.Length == 0)
            {
                return;
            }

            foreach (UIPrefab_Spectator_PlayerListViewItem row in state.NativeRows)
            {
                row.gameObject.SetActive(false);
            }

            RefreshLayoutIfNeeded(state);
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
                && state.RowsPerColumn > 0;
            bool actorsDropped = actorCount > state.MaxVisibleSlots;
            if (screenUnchanged && !actorsDropped)
            {
                return;
            }

            state.LastScreenWidth = screenWidth;
            state.LastScreenHeight = screenHeight;
            MeasureRowMetrics(state);
            state.RowsPerColumn = ComputeRowsPerColumn(state, out float availableHeight);
            state.ColumnStepX = MeasureColumnStepX(state);
            state.MaxVisibleSlots = state.RowsPerColumn * 2;

            ModLog.Debug(
                Feature,
                $"Spectator list layout — availableHeight={availableHeight:F1}, rowHeight={state.RowHeight:F1}, rowsPerColumn={state.RowsPerColumn}, maxVisible={state.MaxVisibleSlots}, actors={actorCount}");
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
        }

        private static int ComputeRowsPerColumn(SpectatorListState state, out float availableHeight)
        {
            availableHeight = state.RowHeight;
            if (state.NativeRows[0].transform is not RectTransform firstRect)
            {
                return 1;
            }

            availableHeight = ResolveAvailableHeight(state, firstRect);
            int rows = Mathf.FloorToInt(availableHeight / state.RowHeight);
            return Mathf.Max(1, rows);
        }

        private static float ResolveAvailableHeight(SpectatorListState state, RectTransform firstRect)
        {
            float canvasHeight = ResolveCanvasHeight(state.ListView);
            float topOffset = Mathf.Abs(firstRect.anchoredPosition.y) + (firstRect.rect.height * 0.5f);
            float available = canvasHeight - topOffset - BottomMargin;
            return Mathf.Max(state.RowHeight, available);
        }

        private static float ResolveCanvasHeight(UIPrefab_Spectator_PlayerListView listView)
        {
            if (listView.transform.root is RectTransform rootRect && rootRect.rect.height > 1f)
            {
                return rootRect.rect.height;
            }

            return Screen.height;
        }

        private static float ResolveCanvasWidth(UIPrefab_Spectator_PlayerListView listView)
        {
            if (listView.transform.root is RectTransform rootRect && rootRect.rect.width > 1f)
            {
                return rootRect.rect.width;
            }

            return Screen.width;
        }

        private static float MeasureColumnStepX(SpectatorListState state)
        {
            if (state.NativeRows[0].transform is not RectTransform firstRect)
            {
                return FallbackColumnStep;
            }

            float rowWidth = firstRect.rect.width;
            if (rowWidth <= 1f)
            {
                rowWidth = FallbackColumnStep - ColumnGap;
            }

            float canvasWidth = ResolveCanvasWidth(state.ListView);
            float leftX = firstRect.anchoredPosition.x;
            float step = canvasWidth - (2f * Mathf.Abs(leftX)) - rowWidth;
            return step > rowWidth * 0.5f ? step : rowWidth + ColumnGap;
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

        private static void EnsureCloneRows(SpectatorListState state, int requiredSlots)
        {
            if (state.NativeRows.Length == 0)
            {
                return;
            }

            Transform rowParent = state.NativeRows[0].transform.parent;
            UIPrefab_Spectator_PlayerListViewItem template = state.NativeRows[0];

            while (state.CloneRows.Count > requiredSlots)
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

            while (state.CloneRows.Count < requiredSlots)
            {
                UIPrefab_Spectator_PlayerListViewItem clone =
                    UnityEngine.Object.Instantiate(template, rowParent);
                clone.gameObject.name = $"MorePlayersSpectatorRow_{state.CloneRows.Count + 1}";
                clone.gameObject.SetActive(true);
                clone.SetColor(state.LiveColor);
                SpectatorPlayerRowBinder.TurnOffSpeakAnimation(clone);
                SpectatorPlayerRowBinder.SetPossessorActive(clone, false);
                state.CloneRows.Add(clone);
            }

            for (int slotIndex = 0; slotIndex < state.CloneRows.Count; slotIndex++)
            {
                PositionCloneRow(state, state.CloneRows[slotIndex], slotIndex);
            }
        }

        private static void PositionCloneRow(
            SpectatorListState state,
            UIPrefab_Spectator_PlayerListViewItem row,
            int slotIndex)
        {
            if (row.transform is not RectTransform rowRect)
            {
                return;
            }

            int columnIndex = slotIndex / state.RowsPerColumn;
            int rowIndex = slotIndex % state.RowsPerColumn;
            Vector2 position = state.OriginPosition
                + new Vector2(columnIndex * state.ColumnStepX, rowIndex * state.YDirection * state.RowHeight);
            rowRect.anchoredPosition = position;
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

            SpectatorPlayerRowBinder.SetRowName(row, name);
            row.SetColor(dead ? state.DeadColor : state.LiveColor);
            SpectatorPlayerRowBinder.BindSpeakState(row, speaking, cancellationToken);
            SpectatorPlayerRowBinder.SetPossessorActive(row, possessor);
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
        }

        private sealed class SpectatorListState
        {
            internal UIPrefab_Spectator_PlayerListView ListView = null!;
            internal UIPrefab_Spectator_PlayerListViewItem[] NativeRows = [];
            internal List<UIPrefab_Spectator_PlayerListViewItem> CloneRows = [];
            internal Color LiveColor = Color.white;
            internal Color DeadColor = Color.red;
            internal Vector2 OriginPosition;
            internal float RowHeight = 24f;
            internal float YDirection = -1f;
            internal float ColumnStepX = FallbackColumnStep;
            internal int RowsPerColumn = VanillaPlayerRows;
            internal int MaxVisibleSlots = VanillaPlayerRows * 2;
            internal int LastScreenWidth;
            internal int LastScreenHeight;
            internal bool ExtendedActive;
        }
    }
}

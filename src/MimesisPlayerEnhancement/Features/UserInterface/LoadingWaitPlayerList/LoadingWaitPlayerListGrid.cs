using System.Reflection;
using System.Threading;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListGrid
    {
        private const string Feature = "Ui";
        private const int VanillaPlayerRows = 4;
        private const float ColumnGap = 12f;
        private const float BottomMargin = 8f;
        private const float FallbackColumnStep = 220f;

        private static readonly FieldInfo? LiveColorField =
            AccessTools.Field(typeof(UIPrefab_Spectator_PlayerListView), "liveColor");

        private static readonly FieldInfo? DeadColorField =
            AccessTools.Field(typeof(UIPrefab_Spectator_PlayerListView), "deadColor");

        private static readonly PropertyInfo? NameTextProperty =
            AccessTools.Property(typeof(UIPrefab_Spectator_PlayerListViewItem), "UE_Name_Text");

        private static readonly PropertyInfo? SpeakAnimationProperty =
            AccessTools.Property(typeof(UIPrefab_Spectator_PlayerListViewItem), "SpriteChangeAnimation");

        private static readonly PropertyInfo? IsPossessorProperty =
            AccessTools.Property(typeof(UIPrefab_Spectator_PlayerListViewItem), "IsPossessor");

        private static readonly MethodInfo? SpeakPlayMethod =
            AccessTools.Method(typeof(SpriteChangeAnimation), "Play", [typeof(CancellationToken)]);

        private static readonly MethodInfo? SpeakTurnOffMethod =
            AccessTools.Method(typeof(SpriteChangeAnimation), "TurnOff");

        private static readonly PropertyInfo? SpeakCanPlayProperty =
            AccessTools.Property(typeof(SpriteChangeAnimation), "CanPlay");

        private static readonly PropertyInfo? SpeakIsPlayingProperty =
            AccessTools.Property(typeof(SpriteChangeAnimation), "IsPlaying");

        internal static bool TryInitialize(
            UIPrefab_Spectator_PlayerListView listView,
            Transform gridRoot,
            out GridState state)
        {
            state = null!;
            UIPrefab_Spectator_PlayerListViewItem[] rows =
                listView.GetComponentsInChildren<UIPrefab_Spectator_PlayerListViewItem>(includeInactive: true);
            if (rows == null || rows.Length == 0)
            {
                return false;
            }

            state = new GridState
            {
                TemplateRow = rows[0],
                GridRoot = gridRoot,
                LiveColor = LiveColorField?.GetValue(listView) is Color live ? live : Color.white,
                DeadColor = DeadColorField?.GetValue(listView) is Color dead ? dead : Color.red,
            };
            MeasureRowMetrics(state);
            RefreshLayoutIfNeeded(state, 0);
            return true;
        }

        internal static void Update(
            GridState state,
            IReadOnlyList<LoadingWaitPlayerEntry> players,
            CancellationToken cancellationToken)
        {
            if (state.TemplateRow == null || state.GridRoot == null)
            {
                return;
            }

            RefreshLayoutIfNeeded(state, players.Count);
            int visibleCount = Math.Min(state.MaxVisibleSlots, players.Count);
            EnsureCloneRows(state, visibleCount);

            for (int slotIndex = 0; slotIndex < state.CloneRows.Count; slotIndex++)
            {
                UIPrefab_Spectator_PlayerListViewItem row = state.CloneRows[slotIndex];
                if (slotIndex >= visibleCount)
                {
                    row.gameObject.SetActive(false);
                    SetRowName(row, string.Empty);
                    TurnOffSpeakAnimation(row);
                    SetPossessorActive(row, false);
                    continue;
                }

                row.gameObject.SetActive(true);
                BindRow(state, row, players[slotIndex], cancellationToken);
            }
        }

        internal static void Destroy(GridState state)
        {
            StopSpeakAnimations(state);
            DestroyCloneRows(state);
        }

        private static void RefreshLayoutIfNeeded(GridState state, int playerCount)
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            bool screenUnchanged = state.LastScreenWidth == screenWidth
                && state.LastScreenHeight == screenHeight
                && state.RowsPerColumn > 0;
            bool playersDropped = playerCount > state.MaxVisibleSlots;
            if (screenUnchanged && !playersDropped)
            {
                return;
            }

            state.LastScreenWidth = screenWidth;
            state.LastScreenHeight = screenHeight;
            MeasureRowMetrics(state);
            state.RowsPerColumn = ComputeRowsPerColumn(state, out _);
            state.ColumnStepX = MeasureColumnStepX(state);
            state.MaxVisibleSlots = state.RowsPerColumn * 2;

            ModLog.Debug(
                Feature,
                $"Loading wait player list layout — rowsPerColumn={state.RowsPerColumn}, maxVisible={state.MaxVisibleSlots}, players={playerCount}");
        }

        private static void MeasureRowMetrics(GridState state)
        {
            if (state.TemplateRow.transform is not RectTransform firstRect)
            {
                state.RowHeight = 24f;
                state.OriginPosition = new Vector2(16f, -BottomMargin);
                state.YDirection = -1f;
                return;
            }

            state.OriginPosition = firstRect.anchoredPosition;

            state.YDirection = -1f;
            state.RowHeight = firstRect.rect.height > 1f ? firstRect.rect.height : 24f;
            if (state.RowHeight <= 1f)
            {
                state.RowHeight = 24f;
            }
        }

        private static int ComputeRowsPerColumn(GridState state, out float availableHeight)
        {
            availableHeight = state.RowHeight;
            float canvasHeight = Screen.height;
            float topOffset = Mathf.Abs(state.OriginPosition.y) + (state.RowHeight * 0.5f);
            availableHeight = Mathf.Max(state.RowHeight, canvasHeight - topOffset - BottomMargin);
            int rows = Mathf.FloorToInt(availableHeight / state.RowHeight);
            return Mathf.Max(1, rows);
        }

        private static float MeasureColumnStepX(GridState state)
        {
            if (state.TemplateRow.transform is not RectTransform firstRect)
            {
                return FallbackColumnStep;
            }

            float rowWidth = firstRect.rect.width;
            if (rowWidth <= 1f)
            {
                rowWidth = FallbackColumnStep - ColumnGap;
            }

            float canvasWidth = Screen.width;
            float leftX = state.OriginPosition.x;
            float step = canvasWidth - (2f * Mathf.Abs(leftX)) - rowWidth;
            return step > rowWidth * 0.5f ? step : rowWidth + ColumnGap;
        }

        private static void EnsureCloneRows(GridState state, int requiredSlots)
        {
            UIPrefab_Spectator_PlayerListViewItem template = state.TemplateRow;
            Transform rowParent = state.GridRoot;

            while (state.CloneRows.Count > requiredSlots)
            {
                int lastIndex = state.CloneRows.Count - 1;
                UIPrefab_Spectator_PlayerListViewItem clone = state.CloneRows[lastIndex];
                state.CloneRows.RemoveAt(lastIndex);
                if (clone != null)
                {
                    TurnOffSpeakAnimation(clone);
                    UnityEngine.Object.Destroy(clone.gameObject);
                }
            }

            while (state.CloneRows.Count < requiredSlots)
            {
                UIPrefab_Spectator_PlayerListViewItem clone =
                    UnityEngine.Object.Instantiate(template, rowParent);
                clone.gameObject.name = $"LoadingWaitPlayerRow_{state.CloneRows.Count + 1}";
                clone.gameObject.SetActive(true);
                clone.SetColor(state.LiveColor);
                TurnOffSpeakAnimation(clone);
                SetPossessorActive(clone, false);
                state.CloneRows.Add(clone);
            }

            for (int slotIndex = 0; slotIndex < state.CloneRows.Count; slotIndex++)
            {
                PositionCloneRow(state, state.CloneRows[slotIndex], slotIndex);
            }
        }

        private static void PositionCloneRow(GridState state, UIPrefab_Spectator_PlayerListViewItem row, int slotIndex)
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

        private static void BindRow(
            GridState state,
            UIPrefab_Spectator_PlayerListViewItem row,
            LoadingWaitPlayerEntry entry,
            CancellationToken cancellationToken)
        {
            SetRowName(row, entry.DisplayName);
            row.SetColor(entry.Loaded ? state.LiveColor : state.DeadColor);

            object? speakAnimation = SpeakAnimationProperty?.GetValue(row);
            if (entry.Speaking)
            {
                if (speakAnimation != null
                    && SpeakCanPlayProperty?.GetValue(speakAnimation) is true
                    && SpeakPlayMethod != null)
                {
                    _ = SpeakPlayMethod.Invoke(speakAnimation, [cancellationToken]);
                }
            }
            else if (speakAnimation == null
                     || SpeakIsPlayingProperty?.GetValue(speakAnimation) is not true)
            {
                TurnOffSpeakAnimation(row);
            }

            SetPossessorActive(row, false);
        }

        private static void SetRowName(UIPrefab_Spectator_PlayerListViewItem row, string text)
        {
            if (NameTextProperty?.GetValue(row) is Component nameText)
            {
                MethodInfo? setText = nameText.GetType().GetMethod("SetText", [typeof(string)]);
                _ = setText?.Invoke(nameText, [text]);
            }
        }

        private static void SetPossessorActive(UIPrefab_Spectator_PlayerListViewItem row, bool active)
        {
            if (IsPossessorProperty?.GetValue(row) is Component possessor)
            {
                possessor.gameObject.SetActive(active);
            }
        }

        private static void TurnOffSpeakAnimation(UIPrefab_Spectator_PlayerListViewItem row)
        {
            object? speakAnimation = SpeakAnimationProperty?.GetValue(row);
            if (speakAnimation != null && SpeakTurnOffMethod != null)
            {
                SpeakTurnOffMethod.Invoke(speakAnimation, null);
            }
        }

        private static void StopSpeakAnimations(GridState state)
        {
            foreach (UIPrefab_Spectator_PlayerListViewItem row in state.CloneRows)
            {
                if (row != null)
                {
                    TurnOffSpeakAnimation(row);
                }
            }
        }

        private static void DestroyCloneRows(GridState state)
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

        internal sealed class GridState
        {
            internal UIPrefab_Spectator_PlayerListViewItem TemplateRow = null!;
            internal Transform GridRoot = null!;
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
        }
    }
}

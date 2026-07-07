using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class SpectatorPlayerGrid
    {
        private const int VanillaPlayerRows = 4;
        private const float ColumnGap = 12f;
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

        private static readonly MethodInfo? GetActorByActorIdMethod =
            AccessTools.Method(typeof(GameMainBase), "GetActorByActorID", [typeof(int)]);

        private static readonly MethodInfo? ResolveNickNameMethod =
            AccessTools.Method(typeof(GameMainBase), "ResolveNickName", [typeof(ProtoActor), typeof(string)]);

        private static readonly Dictionary<int, SpectatorListState> States = [];

        internal static bool IsEnabled() =>
            ModConfig.EnableMorePlayers.Value && ModConfig.EnableExtendedSpectatorPlayerList.Value;

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

            RefreshLayoutIfNeeded(state);
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
            state.LiveColor = LiveColorField?.GetValue(state.ListView) is Color live ? live : Color.white;
            state.DeadColor = DeadColorField?.GetValue(state.ListView) is Color dead ? dead : Color.red;
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

        private static void RefreshLayoutIfNeeded(SpectatorListState state)
        {
            if (state.NativeRows.Length == 0)
            {
                return;
            }

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (state.LastScreenWidth == screenWidth
                && state.LastScreenHeight == screenHeight
                && state.RowsPerColumn > 0)
            {
                return;
            }

            state.LastScreenWidth = screenWidth;
            state.LastScreenHeight = screenHeight;
            MeasureRowMetrics(state);
            state.RowsPerColumn = ComputeRowsPerColumn(state);
            state.ColumnStepX = MeasureColumnStepX(state);
            state.MaxVisibleSlots = state.RowsPerColumn * 2;
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

        private static int ComputeRowsPerColumn(SpectatorListState state)
        {
            if (state.NativeRows[0].transform is not RectTransform firstRect)
            {
                return VanillaPlayerRows;
            }

            float availableHeight = ResolveAvailableHeight(state, firstRect);
            int rows = Mathf.FloorToInt(availableHeight / state.RowHeight);
            return Mathf.Max(VanillaPlayerRows, rows);
        }

        private static float ResolveAvailableHeight(SpectatorListState state, RectTransform firstRect)
        {
            Transform rowParent = firstRect.parent;
            if (rowParent is RectTransform parentRect && parentRect.rect.height > 1f)
            {
                float startOffset = Mathf.Abs(firstRect.anchoredPosition.y);
                float usableHeight = parentRect.rect.height - startOffset;
                if (usableHeight > state.RowHeight)
                {
                    return usableHeight;
                }
            }

            float canvasHeight = ResolveCanvasHeight(state.ListView);
            float topOffset = Mathf.Abs(firstRect.anchoredPosition.y) + (firstRect.rect.height * 0.5f);
            return Mathf.Max(state.RowHeight * VanillaPlayerRows, canvasHeight - topOffset);
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
            if (actorsInfo.Count <= maxVisible)
            {
                return actorsInfo;
            }

            List<(Tuple<int, bool, bool, bool> Actor, int Index)> indexed = actorsInfo
                .Select((actor, index) => (actor, index))
                .ToList();

            indexed.Sort(static (left, right) =>
            {
                int deadCompare = left.Actor.Item2.CompareTo(right.Actor.Item2);
                if (deadCompare != 0)
                {
                    return deadCompare;
                }

                if (left.Actor.Item2)
                {
                    int speakCompare = right.Actor.Item3.CompareTo(left.Actor.Item3);
                    if (speakCompare != 0)
                    {
                        return speakCompare;
                    }
                }

                return left.Index.CompareTo(right.Index);
            });

            return indexed.Take(maxVisible).Select(entry => entry.Actor).ToList();
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
                    TurnOffSpeakAnimation(clone);
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
                TurnOffSpeakAnimation(clone);
                SetPossessorActive(clone, false);
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
                    SetRowName(row, string.Empty);
                    TurnOffSpeakAnimation(row);
                    SetPossessorActive(row, false);
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

            if (GetActorByActorIdMethod.Invoke(main, [actorId]) is not ProtoActor actor)
            {
                SetRowName(row, string.Empty);
                TurnOffSpeakAnimation(row);
                SetPossessorActive(row, false);
                return;
            }

            string actorName = actor.netSyncActorData.actorName;
            string name = ResolveNickNameMethod.Invoke(main, [actor, actorName]) as string ?? actorName;
            SetRowName(row, name);
            row.SetColor(dead ? state.DeadColor : state.LiveColor);

            object? speakAnimation = SpeakAnimationProperty?.GetValue(row);
            if (speaking)
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

            SetPossessorActive(row, possessor);
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

        private static void StopSpeakAnimations(SpectatorListState state)
        {
            foreach (UIPrefab_Spectator_PlayerListViewItem row in state.CloneRows)
            {
                if (row != null)
                {
                    TurnOffSpeakAnimation(row);
                }
            }
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

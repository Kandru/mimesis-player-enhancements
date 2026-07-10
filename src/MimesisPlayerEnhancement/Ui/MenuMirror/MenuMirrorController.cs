using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Ui.MenuMirror
{
    /// <summary>
    /// Rebuilds a vanilla menu's button column whenever features customize it.
    /// Spacing is tuned via <see cref="MainMenuLabelGapPx"/> and
    /// <see cref="InGameMenuLabelGapPx"/>.
    /// </summary>
    internal static class MenuMirrorController
    {
        private const string Feature = "MenuMirror";

        /// <summary>Empty space between stacked menu labels. ~10 matches vanilla spacing.</summary>
        private const float MainMenuLabelGapPx = 10f;

        /// <summary>Empty space between stacked menu labels. ~10 matches vanilla spacing.</summary>
        private const float InGameMenuLabelGapPx = 10f;

        /// <summary>Reference pixel gap that captured vanilla spacing is normalized against.</summary>
        private const float VanillaReferenceGapPx = 11f;

        private static readonly string[] MainMenuColumnIds =
        [
            UIPrefab_MainMenu.UEID_HostButton,
            UIPrefab_MainMenu.UEID_LoadButton,
            UIPrefab_MainMenu.UEID_JoinButton,
            UIPrefab_MainMenu.UEID_SteamInventory,
            UIPrefab_MainMenu.UEID_SettingButton,
            UIPrefab_MainMenu.UEID_QuitButton,
        ];

        private static readonly string[] InGameMenuColumnIds =
        [
            UIPrefab_InGameMenu.UEID_ContinueGameButton,
            UIPrefab_InGameMenu.UEID_InviteFriendsButton,
            UIPrefab_InGameMenu.UEID_FeedbackButton,
            UIPrefab_InGameMenu.UEID_SettingButton,
            UIPrefab_InGameMenu.UEID_QuitButton,
        ];

        private static readonly Dictionary<MenuKind, MenuState> States = new()
        {
            [MenuKind.MainMenu] = new MenuState(),
            [MenuKind.InGameMenu] = new MenuState(),
        };

        private static readonly HashSet<MenuKind> PendingRebuild = [];
        private static readonly HashSet<MenuKind> PendingCapture = [];

        internal static void RefreshFor(MenuKind kind, UIPrefabScript menu, bool allowCapture)
        {
            try
            {
                MenuState state = States[kind];
                if (!ReferenceEquals(state.Menu, menu))
                {
                    ResetState(state);
                    state.Menu = menu;
                }

                if (allowCapture)
                {
                    // Capture one frame late: at Start time the game has not finished
                    // positioning the buttons yet, and a snapshot taken now would
                    // restore a broken (overlapping) layout later.
                    if (PendingCapture.Add(kind))
                    {
                        _ = MelonCoroutines.Start(DeferredCapture(kind, menu));
                    }

                    return;
                }

                RebuildIfReady(kind, "Rebuild");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Refresh for {kind} failed — {ex.Message}");
            }
        }

        internal static void OnRegistryChanged(MenuKind kind)
        {
            MenuState state = States[kind];
            if (state.Menu == null || state.Menu.gameObject == null)
            {
                return;
            }

            // Restoring pure vanilla must happen immediately so tram buttons do not
            // sit in a compacted layout with the wrong labels for a frame.
            if (!MenuMirrorRegistry.HasAny(kind))
            {
                PendingRebuild.Remove(kind);
                RebuildIfReady(kind, "Rebuild");
                return;
            }

            if (PendingRebuild.Add(kind))
            {
                _ = MelonCoroutines.Start(DeferredRebuild(kind));
            }
        }

        private static System.Collections.IEnumerator DeferredCapture(MenuKind kind, UIPrefabScript menu)
        {
            yield return null;

            PendingCapture.Remove(kind);

            MenuState state = States[kind];
            if (!ReferenceEquals(state.Menu, menu) || menu == null || menu.gameObject == null)
            {
                yield break;
            }

            try
            {
                Capture(kind, state);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Capture for {kind} failed — {ex.Message}");
            }

            RebuildIfReady(kind, "Post-capture rebuild");
        }

        private static System.Collections.IEnumerator DeferredRebuild(MenuKind kind)
        {
            yield return null;

            PendingRebuild.Remove(kind);
            RebuildIfReady(kind, "Deferred rebuild");
        }

        private static void RebuildIfReady(MenuKind kind, string context)
        {
            MenuState state = States[kind];
            if (state.Menu == null || state.Menu.gameObject == null || !state.Captured)
            {
                return;
            }

            try
            {
                Rebuild(kind, state);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"{context} for {kind} failed — {ex.Message}");
            }
        }

        private static void Capture(MenuKind kind, MenuState state)
        {
            state.Entries.Clear();
            state.EntriesById.Clear();

            string[] columnIds = kind == MenuKind.MainMenu ? MainMenuColumnIds : InGameMenuColumnIds;
            foreach (string ueid in columnIds)
            {
                Button? button = state.Menu!.PickButton(ueid);
                RectTransform? rect = button != null ? button.GetComponent<RectTransform>() : null;
                if (button == null || rect == null)
                {
                    ModLog.Debug(Feature, $"Column button '{ueid}' not found on {kind}.");
                    continue;
                }

                VanillaEntry entry = new(ueid, button, rect);
                state.Entries.Add(entry);
                state.EntriesById[ueid] = entry;
            }

            if (state.Entries.Count == 0)
            {
                ModLog.Warn(Feature, $"No column buttons captured for {kind}.");
                return;
            }

            state.LabelGapLocal = MeasureVanillaLabelGap(state);
            state.Captured = true;
            ModLog.Debug(
                Feature,
                $"{kind} captured — {state.Entries.Count} buttons, labelGapLocal={state.LabelGapLocal:F2}");
        }

        private static void Rebuild(MenuKind kind, MenuState state)
        {
            DestroyClones(state);
            bool wasMirrored = state.Mirrored;
            RestoreVanilla(state);

            IReadOnlyCollection<MenuCustomization> specs = MenuMirrorRegistry.GetAll(kind);
            if (specs.Count == 0)
            {
                if (wasMirrored)
                {
                    ModLog.Info(Feature, $"{kind} restored to vanilla layout.");
                }

                state.Mirrored = false;
                return;
            }

            HashSet<string> hiddenIds = new();
            List<CustomMenuButton> customs = [];
            foreach (MenuCustomization spec in specs)
            {
                hiddenIds.UnionWith(spec.HiddenButtonIds);
                customs.AddRange(spec.CustomButtons);
            }

            string[] columnIds = kind == MenuKind.MainMenu ? MainMenuColumnIds : InGameMenuColumnIds;
            LayoutCompactColumn(kind, state, columnIds, hiddenIds, customs);
            state.Mirrored = true;
            ModLog.Info(
                Feature,
                $"{kind} rebuilt — {hiddenIds.Count} hidden, {customs.Count} custom.");
        }

        private static void LayoutCompactColumn(
            MenuKind kind,
            MenuState state,
            string[] columnIds,
            HashSet<string> hiddenIds,
            List<CustomMenuButton> customs)
        {
            List<ColumnRow> rows = [];

            foreach (string buttonId in columnIds)
            {
                if (!state.EntriesById.TryGetValue(buttonId, out VanillaEntry? entry))
                {
                    continue;
                }

                if (hiddenIds.Contains(entry.Id))
                {
                    entry.Button.gameObject.SetActive(false);
                    continue;
                }

                entry.Button.gameObject.SetActive(true);
                rows.Add(new ColumnRow(entry.Rect, entry.CapturedAnchoredPosition, entry.Id));
            }

            foreach (CustomMenuButton custom in customs)
            {
                InsertCustomRow(state, rows, custom);
            }

            if (rows.Count == 0)
            {
                return;
            }

            // Measure everything up front in one shared local space, then apply all
            // positions. Measuring row N against the already-moved row N-1 (the old
            // approach) made the result order-dependent and nonlinear in the gap.
            RectTransform? measureSpace = rows[0].Rect.parent as RectTransform;
            if (measureSpace == null)
            {
                return;
            }

            foreach (ColumnRow row in rows)
            {
                row.Measure(measureSpace);
            }

            float gapLocal = ResolveLabelGapLocal(kind, state, measureSpace);

            // Pin the top row at its captured vanilla anchor — never move the column origin.
            rows[0].Rect.anchoredPosition = rows[0].CapturedAnchoredPosition;
            float previousLabelBottomLocal = rows[0].LabelCenterLocalY - rows[0].LabelHalfHeightLocal;

            for (int index = 1; index < rows.Count; index++)
            {
                ColumnRow row = rows[index];
                float targetLabelCenterLocal = previousLabelBottomLocal - gapLocal - row.LabelHalfHeightLocal;
                float deltaLocal = targetLabelCenterLocal - row.LabelCenterLocalY;
                row.Rect.anchoredPosition = new Vector2(
                    row.CapturedAnchoredPosition.x,
                    row.MeasuredAnchoredY + deltaLocal);
                previousLabelBottomLocal = targetLabelCenterLocal - row.LabelHalfHeightLocal;
            }
        }

        /// <summary>
        /// Averages the empty space between adjacent vanilla labels at capture time.
        /// Each menu stores its own value so layout matches that menu's canvas scale.
        /// </summary>
        private static float MeasureVanillaLabelGap(MenuState state)
        {
            if (state.Entries.Count < 2)
            {
                return 0f;
            }

            RectTransform? measureSpace = state.Entries[0].Rect.parent as RectTransform;
            if (measureSpace == null)
            {
                return 0f;
            }

            float totalGap = 0f;
            int samples = 0;
            for (int index = 0; index < state.Entries.Count - 1; index++)
            {
                LabelBounds upper = MeasureLabelBounds(state.Entries[index].Rect, measureSpace);
                LabelBounds lower = MeasureLabelBounds(state.Entries[index + 1].Rect, measureSpace);
                float gap = upper.BottomLocal - lower.TopLocal;
                if (gap > 0.0001f)
                {
                    totalGap += gap;
                    samples++;
                }
            }

            return samples > 0 ? totalGap / samples : 0f;
        }

        private static float ResolveLabelGapLocal(MenuKind kind, MenuState state, RectTransform measureSpace)
        {
            float labelGapPx = kind == MenuKind.MainMenu ? MainMenuLabelGapPx : InGameMenuLabelGapPx;

            // Scale the requested pixel gap through this menu's captured vanilla spacing
            // so the same constant works on both canvas setups.
            if (state.LabelGapLocal > 0.0001f)
            {
                return state.LabelGapLocal * (labelGapPx / VanillaReferenceGapPx);
            }

            return GapLocalForScreenPixels(measureSpace, labelGapPx);
        }

        /// <summary>
        /// Converts a screen-pixel gap into the measuring space's local units. Used as a
        /// fallback when vanilla capture did not produce a measurable label gap.
        /// </summary>
        private static float GapLocalForScreenPixels(RectTransform measureSpace, float screenPixels)
        {
            float gapLocal = Mathf.Abs(
                measureSpace.InverseTransformVector(new Vector3(0f, screenPixels, 0f)).y);
            return gapLocal > 0.0001f ? gapLocal : screenPixels;
        }

        private static LabelBounds MeasureLabelBounds(RectTransform buttonRoot, RectTransform measureSpace)
        {
            RectTransform layoutRect = ResolveLayoutRect(buttonRoot);
            Vector3[] corners = new Vector3[4];
            layoutRect.GetWorldCorners(corners);
            float bottom = measureSpace.InverseTransformPoint(corners[0]).y;
            float top = measureSpace.InverseTransformPoint(corners[1]).y;
            float halfHeight = Mathf.Abs(top - bottom) * 0.5f;
            return new LabelBounds((top + bottom) * 0.5f, halfHeight);
        }

        private static RectTransform ResolveLayoutRect(RectTransform buttonRoot)
        {
            Component? label = ModUiText.FindTextComponent(buttonRoot.gameObject);
            if (label != null && label.transform is RectTransform labelRect)
            {
                return labelRect;
            }

            return buttonRoot;
        }

        private static void InsertCustomRow(MenuState state, List<ColumnRow> rows, CustomMenuButton custom)
        {
            string? anchorId = custom.BeforeButtonId ?? custom.AfterButtonId;
            int insertIndex = rows.Count;

            if (anchorId != null)
            {
                int anchorIndex = rows.FindIndex(row => row.VanillaId == anchorId);
                if (anchorIndex >= 0)
                {
                    insertIndex = custom.BeforeButtonId != null ? anchorIndex : anchorIndex + 1;
                }
            }

            RectTransform styleSource =
                (anchorId != null ? state.EntriesById.GetValueOrDefault(anchorId)?.Rect : null)
                ?? state.Entries[0].Rect;
            GameObject? clone = MenuButtonClone.Create(styleSource.gameObject, custom);
            if (clone == null)
            {
                return;
            }

            state.Clones.Add(clone);
            RectTransform cloneRect = clone.GetComponent<RectTransform>()!;
            rows.Insert(insertIndex, new ColumnRow(cloneRect, styleSource.anchoredPosition, null));
        }

        private static void RestoreVanilla(MenuState state)
        {
            state.Entries.RemoveAll(static entry => entry.Rect == null);
            foreach (VanillaEntry entry in state.Entries)
            {
                entry.Rect.anchoredPosition = entry.CapturedAnchoredPosition;
                // Hidden rows are re-applied by LayoutCompactColumn when specs are active.
                entry.Button.gameObject.SetActive(true);
            }
        }

        private static void DestroyClones(MenuState state)
        {
            foreach (GameObject clone in state.Clones)
            {
                if (clone != null)
                {
                    UnityEngine.Object.Destroy(clone);
                }
            }

            state.Clones.Clear();
        }

        private static void ResetState(MenuState state)
        {
            DestroyClones(state);
            state.Entries.Clear();
            state.EntriesById.Clear();
            state.LabelGapLocal = 0f;
            state.Menu = null;
            state.Captured = false;
            state.Mirrored = false;
        }

        private sealed class MenuState
        {
            internal UIPrefabScript? Menu;

            internal List<VanillaEntry> Entries { get; } = [];

            internal Dictionary<string, VanillaEntry> EntriesById { get; } = new();

            internal List<GameObject> Clones { get; } = [];

            /// <summary>Captured average empty space between vanilla labels (local units).</summary>
            internal float LabelGapLocal;

            internal bool Captured;

            internal bool Mirrored;
        }

        /// <summary>
        /// A vanilla column button with its settled prefab position. The mirror only
        /// ever changes anchored position and active state, so nothing else is stored.
        /// </summary>
        private sealed class VanillaEntry
        {
            internal VanillaEntry(string id, Button button, RectTransform rect)
            {
                Id = id;
                Button = button;
                Rect = rect;
                CapturedAnchoredPosition = rect.anchoredPosition;
            }

            internal string Id { get; }

            internal Button Button { get; }

            internal RectTransform Rect { get; }

            internal Vector2 CapturedAnchoredPosition { get; }
        }

        private sealed class ColumnRow
        {
            internal ColumnRow(RectTransform rect, Vector2 capturedAnchoredPosition, string? vanillaId)
            {
                Rect = rect;
                CapturedAnchoredPosition = capturedAnchoredPosition;
                VanillaId = vanillaId;
            }

            internal RectTransform Rect { get; }

            internal Vector2 CapturedAnchoredPosition { get; }

            internal string? VanillaId { get; }

            internal float LabelCenterLocalY { get; private set; }

            internal float LabelHalfHeightLocal { get; private set; }

            internal float MeasuredAnchoredY { get; private set; }

            /// <summary>
            /// Records the label bounds in the shared measuring space at the row's
            /// current (pre-layout) position. Button roots are tall hit areas, so the
            /// label rect is measured instead of the root.
            /// </summary>
            internal void Measure(RectTransform measureSpace)
            {
                MeasuredAnchoredY = Rect.anchoredPosition.y;
                LabelBounds bounds = MeasureLabelBounds(Rect, measureSpace);
                LabelCenterLocalY = bounds.CenterLocalY;
                LabelHalfHeightLocal = bounds.HalfHeightLocal;
            }
        }

        private readonly struct LabelBounds(float centerLocalY, float halfHeightLocal)
        {
            internal float CenterLocalY { get; } = centerLocalY;

            internal float HalfHeightLocal { get; } = halfHeightLocal;

            internal float BottomLocal => CenterLocalY - HalfHeightLocal;

            internal float TopLocal => CenterLocalY + HalfHeightLocal;
        }
    }
}

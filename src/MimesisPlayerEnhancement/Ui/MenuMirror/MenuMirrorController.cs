using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Ui.MenuMirror
{
    /// <summary>
    /// Rebuilds a vanilla menu's button column whenever features customize it.
    /// Buttons use fixed RectTransform anchors (uGUI, not UI Toolkit flex). The mirror
    /// only adjusts anchored Y so hidden rows leave no gap; anchors/pivots/sizes are
    /// never touched.
    /// </summary>
    internal static class MenuMirrorController
    {
        private const string Feature = "MenuMirror";

        /// <summary>Empty vertical space between stacked menu button labels (screen pixels).</summary>
        private const float ButtonVisualGapPx = 11f;

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

            state.Captured = true;
            ModLog.Debug(Feature, $"{kind} captured — {state.Entries.Count} buttons");
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
            LayoutCompactColumn(state, columnIds, hiddenIds, customs);
            state.Mirrored = true;
            ModLog.Info(
                Feature,
                $"{kind} rebuilt — {hiddenIds.Count} hidden, {customs.Count} custom.");
        }

        private static void LayoutCompactColumn(
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

            float gapLocal = GapLocalForScreenPixels(measureSpace, ButtonVisualGapPx);

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
        /// Converts a screen-pixel gap into the measuring space's local units, computed
        /// once for the whole column so every row uses the exact same gap.
        /// </summary>
        private static float GapLocalForScreenPixels(RectTransform measureSpace, float screenPixels)
        {
            // Screen-space canvases render one world unit per screen pixel, so a
            // world-space vector of the requested length converts directly into the
            // measuring space via the inverse transform.
            float gapLocal = Mathf.Abs(
                measureSpace.InverseTransformVector(new Vector3(0f, screenPixels, 0f)).y);
            return gapLocal > 0.0001f ? gapLocal : screenPixels;
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

                RectTransform layoutRect = ResolveLayoutRect(Rect);
                Vector3[] corners = new Vector3[4];
                layoutRect.GetWorldCorners(corners);
                float bottom = measureSpace.InverseTransformPoint(corners[0]).y;
                float top = measureSpace.InverseTransformPoint(corners[1]).y;
                LabelCenterLocalY = (top + bottom) * 0.5f;
                LabelHalfHeightLocal = Mathf.Abs(top - bottom) * 0.5f;
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
        }
    }
}

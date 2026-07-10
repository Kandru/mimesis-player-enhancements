using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Ui.MenuMirror
{
    /// <summary>
    /// Rebuilds a vanilla menu's button column whenever features customize it.
    /// Buttons use fixed RectTransform anchors (uGUI, not UI Toolkit flex). The mirror
    /// only adjusts anchored Y on the shared parent so hidden rows leave no gap.
    /// </summary>
    internal static class MenuMirrorController
    {
        private const string Feature = "MenuMirror";

        /// <summary>Empty vertical space between stacked menu button labels (screen pixels).</summary>
        private const float ButtonVisualGapPx = 10f;

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

                if (allowCapture && !state.Captured)
                {
                    Capture(kind, state);
                }

                if (!state.Captured)
                {
                    return;
                }

                Rebuild(kind, state);
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

            if (PendingRebuild.Add(kind))
            {
                _ = MelonCoroutines.Start(DeferredRebuild(kind));
            }
        }

        private static System.Collections.IEnumerator DeferredRebuild(MenuKind kind)
        {
            yield return null;

            PendingRebuild.Remove(kind);

            MenuState state = States[kind];
            if (state.Menu == null || state.Menu.gameObject == null)
            {
                yield break;
            }

            try
            {
                if (!state.Captured)
                {
                    Capture(kind, state);
                }

                if (state.Captured)
                {
                    Rebuild(kind, state);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Deferred rebuild for {kind} failed — {ex.Message}");
            }
        }

        private static void Capture(MenuKind kind, MenuState state)
        {
            state.Entries.Clear();

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

                state.Entries.Add(new VanillaEntry(ueid, button, rect));
            }

            if (state.Entries.Count == 0)
            {
                ModLog.Warn(Feature, $"No column buttons captured for {kind}.");
                return;
            }

            state.Captured = true;
            ModLog.Debug(Feature, $"{kind} captured — {state.Entries.Count} buttons");
        }

        private static VanillaEntry? FindEntry(MenuState state, string id)
        {
            foreach (VanillaEntry entry in state.Entries)
            {
                if (entry.Id == id)
                {
                    return entry;
                }
            }

            return null;
        }

        private static void Rebuild(MenuKind kind, MenuState state)
        {
            DestroyClones(state);
            RestoreVanilla(state);

            IReadOnlyCollection<MenuCustomization> specs = MenuMirrorRegistry.GetAll(kind);
            if (specs.Count == 0)
            {
                if (state.Mirrored)
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
                VanillaEntry? entry = FindEntry(state, buttonId);
                if (entry == null)
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

            // Pin the top row at its captured vanilla anchor — never move the column origin.
            rows[0].Rect.anchoredPosition = rows[0].CapturedAnchoredPosition;

            for (int index = 1; index < rows.Count; index++)
            {
                PlaceBelow(rows[index - 1].Rect, rows[index].Rect, ButtonVisualGapPx);
                rows[index].Rect.anchoredPosition = new Vector2(
                    rows[index].CapturedAnchoredPosition.x,
                    rows[index].Rect.anchoredPosition.y);
            }
        }

        /// <summary>
        /// Stack lower under upper using label bounds. Button roots are tall hit areas;
        /// measuring them leaves roughly one extra button height of empty space per row.
        /// </summary>
        private static void PlaceBelow(RectTransform upper, RectTransform lower, float gapPx)
        {
            RectTransform? lowerParent = lower.parent as RectTransform;
            if (lowerParent == null)
            {
                return;
            }

            RectTransform upperLayout = ResolveLayoutRect(upper);
            RectTransform lowerLayout = ResolveLayoutRect(lower);

            Vector3[] upperCorners = new Vector3[4];
            Vector3[] lowerCorners = new Vector3[4];
            upperLayout.GetWorldCorners(upperCorners);
            lowerLayout.GetWorldCorners(lowerCorners);

            float upperBottomLocal = lowerParent.InverseTransformPoint(upperCorners[0]).y;
            float lowerHalfLocal = Mathf.Abs(
                lowerParent.InverseTransformPoint(lowerCorners[1]).y
                - lowerParent.InverseTransformPoint(lowerCorners[0]).y) * 0.5f;
            float gapLocal = GapLocalForScreenPixels(lowerParent, lower, gapPx);
            float targetLabelCenterLocal = upperBottomLocal - gapLocal - lowerHalfLocal;
            float currentLabelCenterLocal = lowerParent.InverseTransformPoint((lowerCorners[0] + lowerCorners[2]) * 0.5f).y;

            Vector2 anchored = lower.anchoredPosition;
            anchored.y += targetLabelCenterLocal - currentLabelCenterLocal;
            lower.anchoredPosition = anchored;
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

        private static float GapLocalForScreenPixels(RectTransform parent, RectTransform reference, float screenPixels)
        {
            Vector3[] corners = new Vector3[4];
            reference.GetWorldCorners(corners);
            float before = parent.InverseTransformPoint(corners[0]).y;
            float after = parent.InverseTransformPoint(corners[0] + new Vector3(0f, -screenPixels, 0f)).y;
            return Mathf.Abs(before - after);
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

            RectTransform styleSource = ResolveStyleSource(state, anchorId) ?? state.Entries[0].Rect;
            GameObject? clone = MenuButtonClone.Create(styleSource.gameObject, custom);
            if (clone == null)
            {
                return;
            }

            state.Clones.Add(clone);
            RectTransform cloneRect = clone.GetComponent<RectTransform>()!;
            Vector2 capturedPos = styleSource.anchoredPosition;
            rows.Insert(insertIndex, new ColumnRow(cloneRect, capturedPos, null));
        }

        private static RectTransform? ResolveStyleSource(MenuState state, string? anchorId)
        {
            if (anchorId == null)
            {
                return null;
            }

            return FindEntry(state, anchorId)?.Rect;
        }

        private static void RestoreVanilla(MenuState state)
        {
            state.Entries.RemoveAll(static entry => entry.Rect == null);
            foreach (VanillaEntry entry in state.Entries)
            {
                entry.RestoreSnapshot();
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
            state.Menu = null;
            state.Captured = false;
            state.Mirrored = false;
        }

        private sealed class MenuState
        {
            internal UIPrefabScript? Menu;

            internal List<VanillaEntry> Entries { get; } = [];

            internal List<GameObject> Clones { get; } = [];

            internal bool Captured;

            internal bool Mirrored;
        }

        private sealed class VanillaEntry
        {
            private readonly Vector2 _anchorMin;
            private readonly Vector2 _anchorMax;
            private readonly Vector2 _pivot;
            private readonly Vector2 _anchoredPosition;
            private readonly Vector2 _sizeDelta;
            private readonly bool _wasActive;

            internal VanillaEntry(string id, Button button, RectTransform rect)
            {
                Id = id;
                Button = button;
                Rect = rect;
                CapturedAnchoredPosition = rect.anchoredPosition;
                _anchorMin = rect.anchorMin;
                _anchorMax = rect.anchorMax;
                _pivot = rect.pivot;
                _anchoredPosition = rect.anchoredPosition;
                _sizeDelta = rect.sizeDelta;
                _wasActive = button.gameObject.activeSelf;
            }

            internal string Id { get; }

            internal Button Button { get; }

            internal RectTransform Rect { get; }

            internal Vector2 CapturedAnchoredPosition { get; }

            internal void RestoreSnapshot()
            {
                Rect.anchorMin = _anchorMin;
                Rect.anchorMax = _anchorMax;
                Rect.pivot = _pivot;
                Rect.anchoredPosition = _anchoredPosition;
                Rect.sizeDelta = _sizeDelta;
                Button.gameObject.SetActive(_wasActive);
            }
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
        }
    }
}

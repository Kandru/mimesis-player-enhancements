using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi
{
    internal static class FpsUiInventoryLayoutHelper
    {
        private static readonly FieldInfo? InventoryUiField =
            AccessTools.Field(typeof(GameMainBase), "inventoryui");

        private static readonly FieldInfo? IngameUiField =
            AccessTools.Field(typeof(GameMainBase), "ingameui");

        private static readonly PropertyInfo? RootNodeProperty =
            AccessTools.Property(typeof(UIPrefab_Inventory), "UE_rootNode");

        private static readonly PropertyInfo? InvenFrame1Property =
            AccessTools.Property(typeof(UIPrefab_Inventory), "UE_InvenFrame1");

        internal static UIPrefab_Inventory? TryGetInventoryUi()
        {
            if (Hub.Main == null || InventoryUiField == null)
            {
                return null;
            }

            return InventoryUiField.GetValue(Hub.Main) as UIPrefab_Inventory;
        }

        internal static UIPrefab_InGame? TryGetIngameUi()
        {
            if (Hub.Main == null || IngameUiField == null)
            {
                return null;
            }

            return IngameUiField.GetValue(Hub.Main) as UIPrefab_InGame;
        }

        internal static RectTransform? TryGetInventoryTransform()
        {
            return TryGetInventoryUi()?.transform as RectTransform;
        }

        internal static RectTransform? TryGetInventoryFrame()
        {
            UIPrefab_Inventory? inventoryUi = TryGetInventoryUi();
            if (InvenFrame1Property?.GetValue(inventoryUi) is Image frame1)
            {
                return frame1.rectTransform;
            }

            if (RootNodeProperty?.GetValue(inventoryUi) is Image rootImage)
            {
                return rootImage.rectTransform;
            }

            return TryGetInventoryTransform();
        }

        internal static bool IsInventoryVisible()
        {
            UIPrefab_Inventory? inventoryUi = TryGetInventoryUi();
            return inventoryUi != null && inventoryUi.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Safe access to the kg label — <see cref="UIPrefab_Inventory.UE_Weight"/> throws if the
        /// inventory prefab has not finished Awake yet (e.g. during mod init / config reload).
        /// </summary>
        internal static Component? TryGetWeightText(UIPrefab_Inventory? inventoryUi)
        {
            if (inventoryUi == null || WeightTextProperty == null)
            {
                return null;
            }

            try
            {
                return WeightTextProperty.GetValue(inventoryUi) as Component;
            }
            catch (TargetInvocationException)
            {
                return null;
            }
        }

        internal static bool IsInventoryReady()
        {
            return TryGetWeightText(TryGetInventoryUi()) != null;
        }

        private static readonly PropertyInfo? WeightTextProperty =
            AccessTools.Property(typeof(UIPrefab_Inventory), "UE_Weight");

        /// <summary>
        /// Themed kg row to clone. Never returns the full inventory pane (<c>rootNode</c>) — that
        /// carries an <see cref="Image"/> but spans the entire hotbar and blows up the clone layout.
        /// </summary>
        internal static RectTransform ResolveWeightRowRect(RectTransform weightAnchor)
        {
            RectTransform? parent = weightAnchor.parent as RectTransform;
            RectTransform? inventoryRoot = TryGetInventoryTransform();
            RectTransform? rootNode = TryGetInventoryRootNode();
            if (parent == null
                || parent == inventoryRoot
                || parent == rootNode)
            {
                return weightAnchor;
            }

            float parentHeight = Mathf.Max(parent.rect.height, 1f);
            float textHeight = Mathf.Max(weightAnchor.rect.height, 1f);
            if (parentHeight > textHeight * 6f)
            {
                return weightAnchor;
            }

            return parent;
        }

        /// <summary>
        /// Place <paramref name="target"/> at the inventory hotbar top, using the same
        /// <see cref="UIPrefab_Inventory.UE_InvenFrame1"/> bounds as the FPS detox line.
        /// Horizontal bounds and row height follow the kg row; bottom padding is mirrored to the top.
        /// </summary>
        internal static bool LayoutRowAtInventoryTop(
            RectTransform sourceRow,
            RectTransform target,
            float topNudgePixels = 0f)
        {
            RectTransform? strip = sourceRow.parent as RectTransform;
            RectTransform? inventoryFrame = TryGetInventoryFrame();
            if (strip == null || inventoryFrame == null)
            {
                return false;
            }

            if (!TryMeasureBoundsInParent(strip, sourceRow, out float rowMinX, out float rowMaxX, out float rowMinY, out float rowMaxY))
            {
                return false;
            }

            if (!TryMeasureBoundsInParent(strip, inventoryFrame, out _, out _, out float frameMinY, out float frameMaxY))
            {
                return false;
            }

            if (target.parent != strip)
            {
                target.SetParent(strip, worldPositionStays: false);
            }

            float rowHeight = Mathf.Max(rowMaxY - rowMinY, sourceRow.rect.height, 1f);
            float bottomPadding = rowMinY - frameMinY;
            float placedMaxY = frameMaxY - Mathf.Max(bottomPadding, 0f) + topNudgePixels;
            float placedMinY = placedMaxY - rowHeight;
            Rect stripRect = strip.rect;

            target.localScale = Vector3.one;
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.zero;
            target.pivot = sourceRow.pivot;
            target.anchoredPosition = Vector2.zero;
            target.sizeDelta = Vector2.zero;
            target.offsetMin = new Vector2(rowMinX - stripRect.xMin, placedMinY - stripRect.yMin);
            target.offsetMax = new Vector2(rowMaxX - stripRect.xMin, placedMaxY - stripRect.yMin);
            target.SetAsLastSibling();
            return true;
        }

        internal static bool TryMeasureBoundsInParent(
            RectTransform parent,
            RectTransform subject,
            out float minX,
            out float maxX,
            out float minY,
            out float maxY)
        {
            minX = 0f;
            maxX = 0f;
            minY = 0f;
            maxY = 0f;
            if (parent == null || subject == null)
            {
                return false;
            }

            Vector3[] corners = new Vector3[4];
            subject.GetWorldCorners(corners);

            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector2 local = parent.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }

            return true;
        }

        private static RectTransform? TryGetInventoryRootNode()
        {
            UIPrefab_Inventory? inventoryUi = TryGetInventoryUi();
            if (RootNodeProperty?.GetValue(inventoryUi) is Image rootImage)
            {
                return rootImage.rectTransform;
            }

            return null;
        }
    }
}

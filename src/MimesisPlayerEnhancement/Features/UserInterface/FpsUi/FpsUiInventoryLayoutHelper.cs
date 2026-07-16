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

        private static readonly PropertyInfo? InvenFrame2Property =
            AccessTools.Property(typeof(UIPrefab_Inventory), "UE_InvenFrame2");

        private static readonly PropertyInfo? InvenFrame3Property =
            AccessTools.Property(typeof(UIPrefab_Inventory), "UE_InvenFrame3");

        private static readonly PropertyInfo? InvenFrame4Property =
            AccessTools.Property(typeof(UIPrefab_Inventory), "UE_InvenFrame4");

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
            if (strip == null)
            {
                return false;
            }

            if (!TryMeasureBoundsInParent(strip, sourceRow, out float rowMinX, out float rowMaxX, out float rowMinY, out float rowMaxY))
            {
                return false;
            }

            if (!TryMeasureInventoryChromeBounds(strip, sourceRow, out float frameMinY, out float frameMaxY))
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
            IgnoreLayoutDrivers(target.gameObject);
            target.SetAsLastSibling();

            Component? weightText = TryGetWeightText(TryGetInventoryUi());
            if (weightText?.transform is RectTransform weightRect)
            {
                return IsRowAboveRow(strip, target, weightRect);
            }

            return true;
        }

        internal static void IgnoreLayoutDrivers(GameObject root)
        {
            foreach (LayoutElement element in root.GetComponentsInChildren<LayoutElement>(true))
            {
                element.ignoreLayout = true;
            }
        }

        internal static bool IsRowAboveRow(
            RectTransform parent,
            RectTransform above,
            RectTransform below,
            float minGapPixels = 1f)
        {
            if (!TryMeasureBoundsInParent(parent, above, out float _, out float _, out float aboveMinY, out float _)
                || !TryMeasureBoundsInParent(parent, below, out float _, out float _, out float _, out float belowMaxY))
            {
                return false;
            }

            return aboveMinY >= belowMaxY - minGapPixels;
        }

        /// <summary>
        /// Vertical chrome for mirror padding: hotbar slot tops plus the kg row at the bottom.
        /// Avoids <c>rootNode</c>, which stretches to the full HUD canvas and skews Y placement.
        /// </summary>
        internal static bool TryMeasureInventoryChromeBounds(
            RectTransform strip,
            RectTransform sourceRow,
            out float frameMinY,
            out float frameMaxY)
        {
            frameMinY = float.PositiveInfinity;
            frameMaxY = float.NegativeInfinity;
            bool measuredHotbar = false;

            UIPrefab_Inventory? inventoryUi = TryGetInventoryUi();
            if (inventoryUi != null)
            {
                foreach (PropertyInfo? frameProperty in new[]
                         {
                             InvenFrame1Property,
                             InvenFrame2Property,
                             InvenFrame3Property,
                             InvenFrame4Property,
                         })
                {
                    if (frameProperty?.GetValue(inventoryUi) is not Image frameImage)
                    {
                        continue;
                    }

                    RectTransform frameRect = frameImage.rectTransform;
                    if (!TryMeasureBoundsInParent(strip, frameRect, out float _, out float _, out float minY, out float maxY))
                    {
                        continue;
                    }

                    frameMinY = Mathf.Min(frameMinY, minY);
                    frameMaxY = Mathf.Max(frameMaxY, maxY);
                    measuredHotbar = true;
                }
            }

            if (!TryMeasureBoundsInParent(strip, sourceRow, out float _, out float _, out float rowMinY, out float _))
            {
                return measuredHotbar;
            }

            if (measuredHotbar)
            {
                // Top edge follows the hotbar; bottom edge includes the kg row below it.
                frameMinY = Mathf.Min(frameMinY, rowMinY);
            }
            else
            {
                frameMinY = rowMinY;
                frameMaxY = rowMinY;
            }

            return frameMaxY > frameMinY;
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

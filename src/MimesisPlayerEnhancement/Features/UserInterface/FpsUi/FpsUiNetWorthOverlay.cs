using System.Reflection;
using Mimic;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi
{
    internal static class FpsUiNetWorthOverlay
    {
        private const string LabelObjectName = "MPE_NetWorth";
        private const float TopEdgeNudgePixels = 0f;
        private const int UnsetTotal = -1;

        private static GameObject? _labelRoot;
        private static Component? _label;
        private static bool _active;
        private static int _resolvedTotal = UnsetTotal;
        private static int _displayedTotal = UnsetTotal;
        private static bool _loggedEnsureFailure;
        private static Vector2 _lastSourceAnchoredPosition = new(float.NaN, float.NaN);
        private static Vector2 _lastSourceRectSize = new(float.NaN, float.NaN);

        internal static bool IsEnabled() => ModConfig.EnableFpsUiInventoryNetWorth.Value;

        internal static void NotifyInventoryShown()
        {
            if (!IsEnabled())
            {
                return;
            }

            _active = true;
            InvalidateLayoutCache();
            Activate();
        }

        internal static void UpdateValue(IReadOnlyList<InventoryItem> inventoryItems)
        {
            if (!IsEnabled())
            {
                return;
            }

            _resolvedTotal = ResolveTotal(inventoryItems);
            if (_label != null)
            {
                ApplyTotal(_resolvedTotal);
            }
            else if (_active)
            {
                Activate();
            }
        }

        internal static void OnUpdate()
        {
            if (!IsEnabled())
            {
                if (_active || _labelRoot != null)
                {
                    OnSessionEnded();
                }

                return;
            }

            if (!_active)
            {
                return;
            }

            if (!FpsUiInventoryLayoutHelper.IsInventoryVisible())
            {
                _labelRoot?.SetActive(false);
                return;
            }

            if (!HasValidWidget())
            {
                Activate();
                return;
            }

            if (!RefreshLayout())
            {
                _labelRoot?.SetActive(false);
                return;
            }

            ApplyTotal(_resolvedTotal == UnsetTotal ? 0 : _resolvedTotal);
            _labelRoot!.SetActive(true);
        }

        internal static void OnInventoryHidden()
        {
            _labelRoot?.SetActive(false);
        }

        internal static void OnSessionEnded()
        {
            _active = false;
            InvalidateLayoutCache();
            ReleaseWidget();
        }

        internal static void RefreshFromConfig()
        {
            if (!IsEnabled())
            {
                OnSessionEnded();
                return;
            }

            _active = true;
            TryRefreshInventoryValue();
            Activate();
        }

        private static void TryRefreshInventoryValue()
        {
            ProtoActor? avatar = Hub.Main?.GetMyAvatar();
            UIPrefab_Inventory? inventoryUi = FpsUiInventoryLayoutHelper.TryGetInventoryUi();
            if (avatar == null || inventoryUi == null)
            {
                return;
            }

            inventoryUi.UpdateSlot(
                avatar.GetInventoryItems(),
                avatar.GetSelectedInventorySlotIndex());
        }

        private static void Activate()
        {
            if (!IsEnabled())
            {
                return;
            }

            if (!FpsUiInventoryLayoutHelper.IsInventoryVisible())
            {
                _labelRoot?.SetActive(false);
                return;
            }

            if (!TryEnsureWidget())
            {
                return;
            }

            if (!RefreshLayout())
            {
                _labelRoot?.SetActive(false);
                return;
            }

            ApplyTotal(_resolvedTotal == UnsetTotal ? 0 : _resolvedTotal);
            _labelRoot!.SetActive(true);
        }

        private static int ResolveTotal(IReadOnlyList<InventoryItem> inventoryItems)
        {
            ProtoActor? avatar = Hub.Main?.GetMyAvatar();
            if (avatar != null)
            {
                return FpsUiInventoryNetWorthCalculator.ComputeTotal(avatar);
            }

            int total = 0;
            foreach (InventoryItem? item in inventoryItems)
            {
                if (item == null || item.IsFake)
                {
                    continue;
                }

                total += FpsUiInventoryNetWorthCalculator.ComputeItemSellPrice(item);
            }

            return total;
        }

        private static bool HasValidWidget()
        {
            return _labelRoot != null
                && _label != null
                && FpsUiInventoryLayoutHelper.TryGetInventoryUi() != null;
        }

        private static bool TryEnsureWidget()
        {
            if (HasValidWidget())
            {
                return true;
            }

            ReleaseWidget();

            UIPrefab_Inventory? inventoryUi = FpsUiInventoryLayoutHelper.TryGetInventoryUi();
            if (!FpsUiInventoryLayoutHelper.IsInventoryReady())
            {
                LogEnsureFailureOnce();
                return false;
            }

            Component? weightText = FpsUiInventoryLayoutHelper.TryGetWeightText(inventoryUi);
            RectTransform? weightAnchor = weightText?.transform as RectTransform;
            RectTransform? sourceRow = weightAnchor != null
                ? FpsUiInventoryLayoutHelper.ResolveWeightRowRect(weightAnchor)
                : null;
            RectTransform? strip = sourceRow?.parent as RectTransform;
            if (weightText == null || weightAnchor == null || sourceRow == null || strip == null)
            {
                LogEnsureFailureOnce();
                return false;
            }

            _loggedEnsureFailure = false;
            _labelRoot = UnityEngine.Object.Instantiate(
                sourceRow.gameObject,
                strip,
                worldPositionStays: false);
            _labelRoot.name = LabelObjectName;

            RectTransform? cloneRect = _labelRoot.GetComponent<RectTransform>();
            if (cloneRect == null)
            {
                ReleaseWidget();
                return false;
            }

            _label = FindMatchingText(_labelRoot, weightText);
            if (_label == null)
            {
                ReleaseWidget();
                return false;
            }

            CopyTextStyle(weightText, _label);
            FpsUiInventoryLayoutHelper.IgnoreLayoutDrivers(_labelRoot);
            _displayedTotal = UnsetTotal;
            InvalidateLayoutCache();
            return true;
        }

        private static bool RefreshLayout()
        {
            if (!HasValidWidget())
            {
                return false;
            }

            UIPrefab_Inventory? inventoryUi = FpsUiInventoryLayoutHelper.TryGetInventoryUi();
            Component? weightText = FpsUiInventoryLayoutHelper.TryGetWeightText(inventoryUi);
            RectTransform? weightAnchor = weightText?.transform as RectTransform;
            RectTransform? sourceRow = weightAnchor != null
                ? FpsUiInventoryLayoutHelper.ResolveWeightRowRect(weightAnchor)
                : null;
            RectTransform? cloneRect = _labelRoot!.GetComponent<RectTransform>();
            if (sourceRow == null || cloneRect == null)
            {
                return false;
            }

            Vector2 anchoredPosition = sourceRow.anchoredPosition;
            Vector2 rectSize = sourceRow.rect.size;
            if (anchoredPosition == _lastSourceAnchoredPosition
                && rectSize == _lastSourceRectSize)
            {
                return true;
            }

            bool laidOut = FpsUiInventoryLayoutHelper.LayoutRowAtInventoryTop(
                sourceRow,
                cloneRect,
                TopEdgeNudgePixels);
            if (laidOut)
            {
                _lastSourceAnchoredPosition = anchoredPosition;
                _lastSourceRectSize = rectSize;
            }

            return laidOut;
        }

        private static void InvalidateLayoutCache()
        {
            _lastSourceAnchoredPosition = new Vector2(float.NaN, float.NaN);
            _lastSourceRectSize = new Vector2(float.NaN, float.NaN);
        }

        private static void ReleaseWidget(bool preserveTotals = false)
        {
            if (_labelRoot != null)
            {
                UnityEngine.Object.Destroy(_labelRoot);
            }

            _labelRoot = null;
            _label = null;
            InvalidateLayoutCache();
            if (!preserveTotals)
            {
                _resolvedTotal = UnsetTotal;
                _displayedTotal = UnsetTotal;
            }
            else
            {
                _displayedTotal = UnsetTotal;
            }
        }

        private static void ApplyTotal(int total)
        {
            if (_label == null)
            {
                return;
            }

            if (_displayedTotal == total)
            {
                return;
            }

            _displayedTotal = total;
            ModUiText.SetText(_label, $"${total}");
        }

        private static void LogEnsureFailureOnce()
        {
            if (_loggedEnsureFailure)
            {
                return;
            }

            _loggedEnsureFailure = true;
            ModLog.Warn("Ui", "Net-worth unavailable — inventory weight row not ready");
        }

        private static Component? FindMatchingText(GameObject cloneRoot, Component sourceText)
        {
            if (cloneRoot == sourceText.gameObject)
            {
                return ModUiText.FindTextComponent(cloneRoot);
            }

            foreach (Transform child in cloneRoot.transform)
            {
                if (child.name.Replace(' ', '_').Equals("Weight", StringComparison.OrdinalIgnoreCase)
                    || child.name.Equals("Weight", StringComparison.OrdinalIgnoreCase))
                {
                    Component? text = ModUiText.FindTextComponent(child.gameObject);
                    if (text != null)
                    {
                        return text;
                    }
                }
            }

            return ModUiText.FindTextComponent(cloneRoot);
        }

        private static void CopyTextStyle(Component source, Component target)
        {
            if (source == null || target == null || source.GetType() != target.GetType())
            {
                return;
            }

            System.Type type = source.GetType();
            foreach (string propertyName in new[]
                     {
                         "font",
                         "fontSharedMaterial",
                         "fontMaterial",
                         "fontSize",
                         "alignment",
                         "color",
                         "colorGradient",
                         "enableVertexGradient",
                     })
            {
                PropertyInfo? property = type.GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public);
                if (property == null || !property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                try
                {
                    object? value = property.GetValue(source, null);
                    property.SetValue(target, value, null);
                }
                catch (TargetInvocationException)
                {
                }
            }
        }
    }
}

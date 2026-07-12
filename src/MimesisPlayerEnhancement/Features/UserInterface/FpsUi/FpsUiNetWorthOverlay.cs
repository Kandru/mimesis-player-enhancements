using System.Reflection;
using MimesisPlayerEnhancement.Ui;
using Mimic;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi
{
    internal static class FpsUiNetWorthOverlay
    {
        private const string LabelObjectName = "MPE_NetWorth";
        private const float TopEdgeNudgePixels = 0f;
        private const int RetryFrames = 90;

        private static GameObject? _labelRoot;
        private static Component? _label;
        private static int _retriesRemaining;
        private static int _lastTotal;

        internal static bool IsEnabled() => ModConfig.EnableFpsUiInventoryNetWorth.Value;

        internal static void NotifyInventoryShown() => Refresh();

        internal static void ScheduleLayoutRetry() => ScheduleRetry();

        internal static void UpdateFromActor(ProtoActor actor)
        {
            if (!IsEnabled() || actor == null)
            {
                return;
            }

            Refresh();
            ApplyValue(FpsUiInventoryNetWorthCalculator.ComputeTotal(actor));
        }

        internal static void UpdateValue(IReadOnlyList<InventoryItem?> inventoryItems)
        {
            if (!IsEnabled())
            {
                return;
            }

            Refresh();

            ProtoActor? avatar = Hub.Main?.GetMyAvatar();
            int total = avatar != null
                ? FpsUiInventoryNetWorthCalculator.ComputeTotal(avatar)
                : FpsUiInventoryNetWorthCalculator.ComputeFromInventoryItems(inventoryItems);
            ApplyValue(total);
        }

        internal static void OnUpdate()
        {
            if (!IsEnabled() || _retriesRemaining <= 0)
            {
                return;
            }

            _retriesRemaining--;
            Refresh();
        }

        internal static void RefreshFromConfig()
        {
            if (!IsEnabled())
            {
                Deactivate();
                return;
            }

            ScheduleRetry();
            if (FpsUiInventoryLayoutHelper.IsInventoryReady())
            {
                Refresh();
            }

            ProtoActor? avatar = Hub.Main?.GetMyAvatar();
            if (avatar != null)
            {
                ApplyValue(FpsUiInventoryNetWorthCalculator.ComputeTotal(avatar));
            }
        }

        private static void Refresh()
        {
            if (!IsEnabled())
            {
                return;
            }

            if (!EnsureWidget())
            {
                return;
            }

            TryRefreshLayout();
        }

        private static void ScheduleRetry() => _retriesRemaining = RetryFrames;

        private static void Deactivate()
        {
            DestroyWidget();
            _retriesRemaining = 0;
            _lastTotal = 0;
        }

        private static bool EnsureWidget()
        {
            if (_labelRoot != null)
            {
                return true;
            }

            UIPrefab_Inventory? inventoryUi = FpsUiInventoryLayoutHelper.TryGetInventoryUi();
            Component? weightText = FpsUiInventoryLayoutHelper.TryGetWeightText(inventoryUi);
            RectTransform? weightAnchor = weightText?.transform as RectTransform;
            RectTransform? sourceRow = weightAnchor != null
                ? FpsUiInventoryLayoutHelper.ResolveWeightRowRect(weightAnchor)
                : null;
            RectTransform? strip = sourceRow?.parent as RectTransform;
            if (weightText == null || weightAnchor == null || sourceRow == null || strip == null)
            {
                ScheduleRetry();
                return false;
            }

            _labelRoot = UnityEngine.Object.Instantiate(sourceRow.gameObject, strip);
            _labelRoot.name = LabelObjectName;

            RectTransform? cloneRect = _labelRoot.GetComponent<RectTransform>();
            if (cloneRect == null)
            {
                DestroyWidget();
                return false;
            }

            _label = FindMatchingText(_labelRoot, weightText);
            if (_label == null)
            {
                DestroyWidget();
                return false;
            }

            CopyTextStyle(weightText, _label);
            ApplyValue(_lastTotal);
            _labelRoot.SetActive(false);
            return true;
        }

        private static bool TryRefreshLayout()
        {
            if (_labelRoot == null && !EnsureWidget())
            {
                return false;
            }

            if (!FpsUiInventoryLayoutHelper.IsInventoryVisible())
            {
                _labelRoot?.SetActive(false);
                return false;
            }

            UIPrefab_Inventory? inventoryUi = FpsUiInventoryLayoutHelper.TryGetInventoryUi();
            Component? weightText = FpsUiInventoryLayoutHelper.TryGetWeightText(inventoryUi);
            RectTransform? weightAnchor = weightText?.transform as RectTransform;
            RectTransform? sourceRow = weightAnchor != null
                ? FpsUiInventoryLayoutHelper.ResolveWeightRowRect(weightAnchor)
                : null;
            RectTransform? cloneRect = _labelRoot?.GetComponent<RectTransform>();
            if (sourceRow == null || cloneRect == null)
            {
                _labelRoot?.SetActive(false);
                ScheduleRetry();
                return false;
            }

            if (!FpsUiInventoryLayoutHelper.LayoutRowAtInventoryTop(
                    sourceRow,
                    cloneRect,
                    TopEdgeNudgePixels))
            {
                _labelRoot?.SetActive(false);
                ScheduleRetry();
                return false;
            }

            if (_label != null && weightText != null)
            {
                CopyTextStyle(weightText, _label);
            }

            _labelRoot!.SetActive(true);
            ApplyValue(_lastTotal);
            return true;
        }

        private static void DestroyWidget()
        {
            if (_labelRoot != null)
            {
                UnityEngine.Object.Destroy(_labelRoot);
            }

            _labelRoot = null;
            _label = null;
        }

        private static void ApplyValue(int total)
        {
            _lastTotal = total;
            if (_label != null)
            {
                ModUiText.SetText(_label, $"${total}");
            }
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

using System.Reflection;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal sealed class WorldOverlayFactory
    {
        private const float CanvasScale = 0.01f;
        private const float BarWidth = 84f;
        private const float BarHeight = 12f;
        private const float BarBorderInset = 1.5f;

        private static readonly Color HealthFill = new(0.82f, 0.18f, 0.18f, 1f);
        private static readonly Color HealthFillFlash = new(1f, 0.42f, 0.42f, 1f);
        private static readonly Color White = Color.white;
        private static readonly Color DamageTextColor = new(1f, 0.35f, 0.35f, 1f);

        private static readonly Type? CanvasType = Type.GetType("UnityEngine.Canvas, UnityEngine.UIModule");
        private static readonly Type? CanvasScalerType = Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
        private static readonly Type? GraphicRaycasterType = Type.GetType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");

        private static WorldOverlayFactory? _instance;

        private readonly GameObject _rootObject;
        private readonly Transform _rootTransform;
        private readonly Stack<HealthBarWidget> _healthBarPool = new();
        private readonly Stack<FloaterWidget> _floaterPool = new();

        internal static WorldOverlayFactory Instance => _instance ??= new WorldOverlayFactory();

        private WorldOverlayFactory()
        {
            _rootObject = new GameObject("MimesisWorldOverlayRoot");
            UnityEngine.Object.DontDestroyOnLoad(_rootObject);
            _rootObject.SetActive(false);

            if (CanvasType != null)
            {
                Component canvas = _rootObject.AddComponent(CanvasType);
                SetEnumProperty(canvas, "renderMode", "UnityEngine.RenderMode, UnityEngine.CoreModule", 2);
            }

            if (CanvasScalerType != null)
            {
                Component scaler = _rootObject.AddComponent(CanvasScalerType);
                SetFloatProperty(scaler, "dynamicPixelsPerUnit", 10f);
                SetFloatProperty(scaler, "referencePixelsPerUnit", 100f);
            }

            if (GraphicRaycasterType != null)
            {
                _ = _rootObject.AddComponent(GraphicRaycasterType);
            }

            _rootTransform = _rootObject.transform;
            _rootTransform.localScale = new Vector3(CanvasScale, CanvasScale, CanvasScale);
        }

        internal void SetRootActive(bool active)
        {
            _rootObject.SetActive(active);
        }

        internal HealthBarWidget RentHealthBar()
        {
            if (_healthBarPool.TryPop(out HealthBarWidget? widget))
            {
                widget.Root.SetActive(true);
                return widget;
            }

            return CreateHealthBar();
        }

        internal void ReturnHealthBar(HealthBarWidget widget)
        {
            widget.Root.SetActive(false);
            widget.Actor = null;
            _healthBarPool.Push(widget);
        }

        internal FloaterWidget RentFloater()
        {
            if (_floaterPool.TryPop(out FloaterWidget? widget))
            {
                widget.Root.SetActive(true);
                return widget;
            }

            return CreateFloater();
        }

        internal void ReturnFloater(FloaterWidget widget)
        {
            widget.Root.SetActive(false);
            _floaterPool.Push(widget);
        }

        internal void TearDownAllPooled()
        {
            while (_healthBarPool.Count > 0)
            {
                HealthBarWidget widget = _healthBarPool.Pop();
                if (widget.Root != null)
                {
                    UnityEngine.Object.Destroy(widget.Root);
                }
            }

            while (_floaterPool.Count > 0)
            {
                FloaterWidget widget = _floaterPool.Pop();
                if (widget.Root != null)
                {
                    UnityEngine.Object.Destroy(widget.Root);
                }
            }

            _rootObject.SetActive(false);
        }

        private HealthBarWidget CreateHealthBar()
        {
            GameObject root = new("HealthBar");
            root.transform.SetParent(_rootTransform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(BarWidth, BarHeight);

            CreateStretchImage(root.transform, "Border", White);

            GameObject fillArea = new("FillArea");
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(BarBorderInset, BarBorderInset);
            fillAreaRect.offsetMax = new Vector2(-BarBorderInset, -BarBorderInset);

            Image fill = CreateStretchImage(fillArea.transform, "Fill", HealthFill);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            GameObject textGo = new("HpText");
            textGo.transform.SetParent(root.transform, false);
            RectTransform textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Component label = ModUiFactory.AddText(textGo, ModUiAssets.Fallback, "100/100", 11f, ModUiFontStyle.Bold);
            ModUiText.SetMiddleCenterAlignment(label);
            ModUiText.SetColor(label, White);
            ModUiText.ConfigureTextLayout(label, wordWrap: false, ModUiText.OverflowOverflow);
            DisableRaycast(root);
            DisableRaycast(fillArea);
            DisableRaycast(textGo);

            return new HealthBarWidget
            {
                Root = root,
                RootTransform = rootRect,
                FillImage = fill,
                TextComponent = label,
                BaseFillColor = HealthFill,
                FlashFillColor = HealthFillFlash,
            };
        }

        private FloaterWidget CreateFloater()
        {
            GameObject root = new("DamageFloater");
            root.transform.SetParent(_rootTransform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(80f, 40f);

            Component label = ModUiFactory.AddText(root, ModUiAssets.Fallback, "0", 22f, ModUiFontStyle.Bold);
            ModUiText.SetMiddleCenterAlignment(label);
            ModUiText.SetColor(label, DamageTextColor);
            ModUiText.ConfigureTextLayout(label, wordWrap: false, ModUiText.OverflowOverflow);
            DisableRaycast(root);

            return new FloaterWidget
            {
                Root = root,
                RootTransform = rootRect,
                TextComponent = label,
                BaseColor = DamageTextColor,
            };
        }

        private static Image CreateStretchImage(Transform parent, string name, Color color)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void DisableRaycast(GameObject go)
        {
            foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        private static void SetFloatProperty(Component component, string propertyName, float value)
        {
            PropertyInfo? property = component.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            property?.SetValue(component, value, null);
        }

        private static void SetEnumProperty(Component component, string propertyName, string enumTypeName, int value)
        {
            Type? enumType = Type.GetType(enumTypeName);
            PropertyInfo? property = component.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (enumType == null || property == null || !enumType.IsEnum)
            {
                return;
            }

            property.SetValue(component, Enum.ToObject(enumType, value), null);
        }

        internal sealed class HealthBarWidget
        {
            internal GameObject Root = null!;
            internal RectTransform RootTransform = null!;
            internal Image FillImage = null!;
            internal Component TextComponent = null!;
            internal Color BaseFillColor;
            internal Color FlashFillColor;
            internal ProtoActor? Actor;
        }

        internal sealed class FloaterWidget
        {
            internal GameObject Root = null!;
            internal RectTransform RootTransform = null!;
            internal Component TextComponent = null!;
            internal Color BaseColor;
        }
    }
}

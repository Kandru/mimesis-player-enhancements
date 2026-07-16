using System.Reflection;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal sealed class WorldOverlayFactory
    {
        private const float CanvasScale = 0.01f;
        internal const float FloaterScale = 0.5f;

        private static readonly Color DamageTextColor = new(1f, 0.35f, 0.35f, 1f);

        private static readonly Type? CanvasType = Type.GetType("UnityEngine.Canvas, UnityEngine.UIModule");
        private static readonly Type? CanvasScalerType = Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
        private static readonly Type? GraphicRaycasterType = Type.GetType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");

        private static WorldOverlayFactory? _instance;

        private readonly GameObject _rootObject;
        private readonly Transform _rootTransform;
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

        internal FloaterWidget RentFloater(float displayScale = 1f)
        {
            FloaterWidget widget;
            if (_floaterPool.TryPop(out FloaterWidget? pooled))
            {
                widget = pooled;
                widget.Root.SetActive(true);
            }
            else
            {
                widget = CreateFloater();
            }

            widget.RootTransform.localScale = new Vector3(displayScale, displayScale, displayScale);
            return widget;
        }

        internal void ReturnFloater(FloaterWidget widget)
        {
            widget.Root.SetActive(false);
            widget.RootTransform.localScale = Vector3.one;
            _floaterPool.Push(widget);
        }

        internal void TearDownAllPooled()
        {
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

        internal sealed class FloaterWidget
        {
            internal GameObject Root = null!;
            internal RectTransform RootTransform = null!;
            internal Component TextComponent = null!;
            internal Color BaseColor;
        }
    }
}

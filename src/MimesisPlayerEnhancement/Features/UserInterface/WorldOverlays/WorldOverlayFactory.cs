using System.Reflection;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal sealed class WorldOverlayFactory
    {
        private const float CanvasScale = 0.01f;
        private const float GlowSize = 140f;
        internal const float FloaterScale = 0.5f;

        private static readonly Color DamageTextColor = new(1f, 0.35f, 0.35f, 1f);

        private static readonly Type? CanvasType = Type.GetType("UnityEngine.Canvas, UnityEngine.UIModule");
        private static readonly Type? CanvasScalerType = Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
        private static readonly Type? GraphicRaycasterType = Type.GetType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");

        private static WorldOverlayFactory? _instance;
        private static Sprite? _glowSprite;

        private readonly GameObject _rootObject;
        private readonly Transform _rootTransform;
        private readonly Stack<HealthGlowWidget> _healthGlowPool = new();
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

        internal HealthGlowWidget RentHealthGlow()
        {
            if (_healthGlowPool.TryPop(out HealthGlowWidget? widget))
            {
                widget.Root.SetActive(true);
                widget.RootTransform.localScale = Vector3.one;
                return widget;
            }

            return CreateHealthGlow();
        }

        internal void ReturnHealthGlow(HealthGlowWidget widget)
        {
            widget.Root.SetActive(false);
            widget.Actor = null;
            widget.RootTransform.localScale = Vector3.one;
            _healthGlowPool.Push(widget);
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
            while (_healthGlowPool.Count > 0)
            {
                HealthGlowWidget widget = _healthGlowPool.Pop();
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

        private HealthGlowWidget CreateHealthGlow()
        {
            GameObject root = new("HealthGlow");
            root.transform.SetParent(_rootTransform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(GlowSize, GlowSize);

            Image glow = root.AddComponent<Image>();
            glow.sprite = GetGlowSprite();
            glow.color = new Color(0.2f, 1f, 0.2f, 0.85f);
            glow.raycastTarget = false;

            DisableRaycast(root);

            return new HealthGlowWidget
            {
                Root = root,
                RootTransform = rootRect,
                GlowImage = glow,
            };
        }

        private static Sprite GetGlowSprite()
        {
            if (_glowSprite != null)
            {
                return _glowSprite;
            }

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, mipChain: false);
            Vector2 center = new(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha;
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            _glowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return _glowSprite;
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

        internal sealed class HealthGlowWidget
        {
            internal GameObject Root = null!;
            internal RectTransform RootTransform = null!;
            internal Image GlowImage = null!;
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

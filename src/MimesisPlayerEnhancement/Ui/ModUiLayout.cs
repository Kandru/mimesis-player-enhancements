using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// RectTransform math shared by mod UI — features should compose these instead of
    /// setting anchors/pivots by hand.
    /// </summary>
    internal static class ModUiLayout
    {
        internal static GameObject CreateChild(string name, Transform parent)
        {
            GameObject go = new(name);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            return go;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void AnchorTopStrip(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        internal static void AnchorBottomStrip(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        internal static void StretchTextToParent(Component? textComponent)
        {
            if (textComponent?.transform is RectTransform textRect)
            {
                Stretch(textRect);
            }
        }

        internal static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static RectTransform CreateBand(
            Transform parent,
            string name,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            GameObject go = CreateChild(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetAnchors(rect, minX, minY, maxX, maxY);
            return rect;
        }

        internal static void PrepareLayoutGroupChild(RectTransform rect)
        {
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void PrepareListRowLayout(RectTransform rect, float height)
        {
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
        }

        // Some UnityEngine.UI enum-typed properties (e.g. childAlignment) are set via reflection
        // because the netstandard reference assemblies expose them inconsistently.
        internal static void SetEnumProperty(object target, string propertyName, int enumValue)
        {
            PropertyInfo? property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.PropertyType.IsEnum)
            {
                return;
            }

            property.SetValue(target, Enum.ToObject(property.PropertyType, enumValue));
        }
    }
}

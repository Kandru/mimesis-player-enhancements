using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// Reflection helpers for TextMeshProUGUI components (TMP is not referenced directly;
    /// it is resolved at runtime via <c>Type.GetType</c>).
    /// </summary>
    internal static class ModUiText
    {
        internal const int OverflowOverflow = 0;
        internal const int OverflowEllipsis = 1;
        internal const int OverflowMasking = 2;

        private static readonly Dictionary<System.Type, (PropertyInfo? Text, PropertyInfo? Color)> PropertyCache = new();

        internal static Component? FindTextComponent(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                if (component.GetType().Name is "TextMeshProUGUI" or "TMP_Text")
                {
                    return component;
                }
            }

            return null;
        }

        internal static string? GetText(Component? textComponent)
        {
            if (textComponent == null)
            {
                return null;
            }

            return GetCachedProperties(textComponent).Text?.GetValue(textComponent) as string;
        }

        internal static void SetText(Component? textComponent, string value)
        {
            if (textComponent == null)
            {
                return;
            }

            PropertyInfo? textProperty = GetCachedProperties(textComponent).Text;
            textProperty?.SetValue(textComponent, value, null);
        }

        internal static void SetColor(Component? textComponent, Color color)
        {
            if (textComponent == null)
            {
                return;
            }

            PropertyInfo? colorProperty = GetCachedProperties(textComponent).Color;
            colorProperty?.SetValue(textComponent, color, null);
        }

        internal static void SetAlignment(Component? textComponent, bool upperLeft)
        {
            SetAlignment(textComponent, upperLeft ? TextAlignment.TopLeft : TextAlignment.Top);
        }

        internal static void SetMiddleCenterAlignment(Component? textComponent)
        {
            SetAlignment(textComponent, TextAlignment.MiddleCenter);
        }

        internal static void SetTopRightAlignment(Component? textComponent)
        {
            if (TrySetAlignmentByName(textComponent, "TopRight"))
            {
                return;
            }

            SetAlignment(textComponent, TextAlignment.TopRight);
        }

        internal static void SetBottomRightAlignment(Component? textComponent)
        {
            if (TrySetAlignmentByName(textComponent, "BottomRight"))
            {
                return;
            }

            SetAlignment(textComponent, TextAlignment.BottomRight);
        }

        private static bool TrySetAlignmentByName(Component? textComponent, string alignmentName)
        {
            if (textComponent == null)
            {
                return false;
            }

            PropertyInfo? alignmentProperty = textComponent.GetType().GetProperty(
                "alignment",
                BindingFlags.Instance | BindingFlags.Public);
            if (alignmentProperty == null || !alignmentProperty.PropertyType.IsEnum)
            {
                return false;
            }

            try
            {
                object value = Enum.Parse(alignmentProperty.PropertyType, alignmentName);
                alignmentProperty.SetValue(textComponent, value, null);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static void SetAlignment(Component? textComponent, TextAlignment alignment)
        {
            if (textComponent == null)
            {
                return;
            }

            PropertyInfo? alignmentProperty = textComponent.GetType().GetProperty(
                "alignment",
                BindingFlags.Instance | BindingFlags.Public);
            if (alignmentProperty == null)
            {
                return;
            }

            object value = Enum.ToObject(alignmentProperty.PropertyType, (int)alignment);
            alignmentProperty.SetValue(textComponent, value, null);
        }

        // TMP TextAlignmentOptions raw values.
        private enum TextAlignment
        {
            TopLeft = 257,
            Top = 258,
            TopRight = 260,
            MiddleCenter = 514,
            BottomRight = 1028,
        }

        internal static void ConfigureTextLayout(Component? textComponent, bool wordWrap, int overflowMode)
        {
            if (textComponent == null)
            {
                return;
            }

            System.Type textType = textComponent.GetType();
            PropertyInfo? wrapProp = textType.GetProperty(
                "enableWordWrapping",
                BindingFlags.Instance | BindingFlags.Public);
            wrapProp?.SetValue(textComponent, wordWrap, null);

            PropertyInfo? overflowProp = textType.GetProperty(
                "overflowMode",
                BindingFlags.Instance | BindingFlags.Public);
            if (overflowProp != null && overflowProp.PropertyType.IsEnum)
            {
                overflowProp.SetValue(textComponent, Enum.ToObject(overflowProp.PropertyType, overflowMode), null);
            }

            PropertyInfo? raycastProp = textType.GetProperty(
                "raycastTarget",
                BindingFlags.Instance | BindingFlags.Public);
            raycastProp?.SetValue(textComponent, false, null);
        }

        internal static void ConfigureTightSingleLine(Component? textComponent)
        {
            if (textComponent == null)
            {
                return;
            }

            System.Type textType = textComponent.GetType();

            PropertyInfo? lineSpacingProp = textType.GetProperty(
                "lineSpacing",
                BindingFlags.Instance | BindingFlags.Public);
            lineSpacingProp?.SetValue(textComponent, 0f, null);

            PropertyInfo? paragraphSpacingProp = textType.GetProperty(
                "paragraphSpacing",
                BindingFlags.Instance | BindingFlags.Public);
            paragraphSpacingProp?.SetValue(textComponent, 0f, null);

            PropertyInfo? marginProp = textType.GetProperty(
                "margin",
                BindingFlags.Instance | BindingFlags.Public);
            marginProp?.SetValue(textComponent, Vector4.zero, null);

            PropertyInfo? extraPaddingProp = textType.GetProperty(
                "enableExtraPadding",
                BindingFlags.Instance | BindingFlags.Public);
            extraPaddingProp?.SetValue(textComponent, false, null);

            PropertyInfo? legacyExtraPaddingProp = textType.GetProperty(
                "extraPadding",
                BindingFlags.Instance | BindingFlags.Public);
            legacyExtraPaddingProp?.SetValue(textComponent, false, null);

            PropertyInfo? maxDescenderProp = textType.GetProperty(
                "useMaxVisibleDescender",
                BindingFlags.Instance | BindingFlags.Public);
            maxDescenderProp?.SetValue(textComponent, false, null);
        }

        internal static void EnableRichText(Component? textComponent)
        {
            if (textComponent == null)
            {
                return;
            }

            PropertyInfo? richTextProp = textComponent.GetType().GetProperty(
                "richText",
                BindingFlags.Instance | BindingFlags.Public);
            richTextProp?.SetValue(textComponent, true, null);
        }

        private static (PropertyInfo? Text, PropertyInfo? Color) GetCachedProperties(Component textComponent)
        {
            System.Type textType = textComponent.GetType();
            if (!PropertyCache.TryGetValue(textType, out (PropertyInfo? Text, PropertyInfo? Color) cached))
            {
                cached = (
                    textType.GetProperty("text", BindingFlags.Instance | BindingFlags.Public),
                    textType.GetProperty("color", BindingFlags.Instance | BindingFlags.Public));
                PropertyCache[textType] = cached;
            }

            return cached;
        }
    }
}

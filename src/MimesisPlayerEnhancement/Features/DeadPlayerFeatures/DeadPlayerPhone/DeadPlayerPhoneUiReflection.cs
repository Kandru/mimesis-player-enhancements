using System;
using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneUiReflection
    {
        private static readonly Type? TmpTextType = AccessTools.TypeByName("TMPro.TextMeshProUGUI");
        private static readonly PropertyInfo? FontWeightProperty =
            TmpTextType?.GetProperty("fontWeight", BindingFlags.Instance | BindingFlags.Public);
        private static readonly PropertyInfo? TextProperty =
            TmpTextType?.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        private static readonly PropertyInfo? FontProperty =
            TmpTextType?.GetProperty("font", BindingFlags.Instance | BindingFlags.Public);
        private static readonly PropertyInfo? FontSizeProperty =
            TmpTextType?.GetProperty("fontSize", BindingFlags.Instance | BindingFlags.Public);
        private static readonly PropertyInfo? AlignmentProperty =
            TmpTextType?.GetProperty("alignment", BindingFlags.Instance | BindingFlags.Public);
        private static readonly PropertyInfo? ColorProperty =
            TmpTextType?.GetProperty("color", BindingFlags.Instance | BindingFlags.Public);

        internal static bool IsTmpText(object? component) =>
            component != null && TmpTextType != null && TmpTextType.IsInstanceOfType(component);

        internal static void SetFontWeight(object? textComponent, bool bold)
        {
            if (textComponent == null || FontWeightProperty == null)
            {
                return;
            }

            object weight = bold ? 700 : 400;
            FontWeightProperty.SetValue(textComponent, weight);
        }

        internal static void SetText(object? textComponent, string text)
        {
            TextProperty?.SetValue(textComponent, text);
        }

        internal static string GetText(object? textComponent) =>
            TextProperty?.GetValue(textComponent) as string ?? string.Empty;

        internal static void CopyStyle(object? source, Component target)
        {
            if (source == null || TmpTextType == null)
            {
                return;
            }

            if (FontProperty?.GetValue(source) is UnityEngine.Object font)
            {
                FontProperty.SetValue(target, font);
            }

            if (FontSizeProperty?.GetValue(source) is float fontSize)
            {
                FontSizeProperty.SetValue(target, fontSize);
            }

            if (AlignmentProperty?.GetValue(source) is object alignment)
            {
                AlignmentProperty.SetValue(target, alignment);
            }

            if (ColorProperty?.GetValue(source) is Color color)
            {
                ColorProperty.SetValue(target, color);
            }
        }

        internal static Component? AddTmpTextComponent(GameObject gameObject)
        {
            return TmpTextType != null ? gameObject.AddComponent(TmpTextType) as Component : null;
        }
    }
}

using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListTextMeasure
    {
        private static MethodInfo? _getPreferredValuesMethod;

        internal static Vector2 MeasurePreferredSize(Component? textComponent, string text, float fontSize)
        {
            if (textComponent == null)
            {
                return Vector2.zero;
            }

            MethodInfo? method = ResolveGetPreferredValuesMethod(textComponent.GetType());
            if (method == null)
            {
                return EstimateFallback(text, fontSize);
            }

            try
            {
                if (method.GetParameters().Length == 1)
                {
                    ModUiText.SetText(textComponent, text);
                }

                object?[] args = method.GetParameters().Length == 1
                    ? [text]
                    : [text, fontSize, 0, 0f];
                object? result = method.Invoke(textComponent, args);
                if (result is Vector2 size)
                {
                    return size;
                }
            }
            catch
            {
                /* reflection mismatch */
            }

            return EstimateFallback(text, fontSize);
        }

        private static MethodInfo? ResolveGetPreferredValuesMethod(System.Type textType)
        {
            if (_getPreferredValuesMethod != null
                && _getPreferredValuesMethod.DeclaringType == textType)
            {
                return _getPreferredValuesMethod;
            }

            MethodInfo? singleArg = textType.GetMethod(
                "GetPreferredValues",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                [typeof(string)],
                null);
            if (singleArg != null)
            {
                _getPreferredValuesMethod = singleArg;
                return _getPreferredValuesMethod;
            }

            _getPreferredValuesMethod = textType.GetMethod(
                "GetPreferredValues",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                [typeof(string), typeof(float), typeof(int), typeof(float)],
                null);
            return _getPreferredValuesMethod;
        }

        private static Vector2 EstimateFallback(string text, float fontSize)
        {
            int length = string.IsNullOrEmpty(text) ? 0 : text.Length;
            return new Vector2(length * fontSize * 0.55f, fontSize * 1.2f);
        }
    }
}

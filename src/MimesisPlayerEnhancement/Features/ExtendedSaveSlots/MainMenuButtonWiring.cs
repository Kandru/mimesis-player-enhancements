using System;
using System.Collections;
using System.Reflection;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    /// <summary>
    /// Registers click handlers using the UI element marker name the game passes to
    /// <see cref="UIPrefabScript"/> OnButtonClick, not just the logical UE id constant.
    /// </summary>
    internal static class MainMenuButtonWiring
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? DictElementsField =
            AccessTools.Field(typeof(UIPrefabScript), "dictElements");

        private static readonly FieldInfo? OnButtonClickField =
            AccessTools.Field(typeof(UIPrefabScript), "onButtonClick");

        private static readonly MethodInfo? SetOnButtonClickMethod =
            AccessTools.Method(typeof(UIPrefabScript), "SetOnButtonClick");

        private static readonly Type? MarkerType = AccessTools.TypeByName("UIElementMarker");

        private static readonly FieldInfo? MarkerButtonField =
            MarkerType != null ? AccessTools.Field(MarkerType, "asButton") : null;

        private static readonly PropertyInfo? MarkerNameProperty =
            MarkerType?.GetProperty("name", InstanceFlags);

        internal static void RegisterHandler(
            UIPrefabScript ui,
            Button button,
            Action<string> handler,
            string logicalId)
        {
            SetOnButtonClickMethod?.Invoke(ui, [logicalId, handler]);

            if (DictElementsField?.GetValue(ui) is not IDictionary dict
                || OnButtonClickField?.GetValue(ui) is not IDictionary onClick
                || MarkerButtonField == null
                || MarkerNameProperty == null)
            {
                return;
            }

            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Value == null)
                {
                    continue;
                }

                object marker = entry.Value;
                if (!ReferenceEquals(MarkerButtonField.GetValue(marker), button))
                {
                    continue;
                }

                string? elementName = MarkerNameProperty.GetValue(marker) as string;
                if (string.IsNullOrEmpty(elementName))
                {
                    continue;
                }

                onClick[elementName] = handler;
                if (entry.Key is string dictKey && dictKey != elementName)
                {
                    onClick[dictKey] = handler;
                }

                return;
            }
        }

        internal static bool IsHostButtonElement(UIPrefabScript ui, string elementId, Button hostButton)
        {
            if (elementId == "HostButton")
            {
                return true;
            }

            if (DictElementsField?.GetValue(ui) is not IDictionary dict || !dict.Contains(elementId))
            {
                return false;
            }

            object? marker = dict[elementId];
            return marker != null
                && MarkerButtonField != null
                && ReferenceEquals(MarkerButtonField.GetValue(marker), hostButton);
        }
    }
}

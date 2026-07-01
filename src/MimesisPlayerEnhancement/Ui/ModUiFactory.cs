using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Ui
{
    internal enum ModUiFontStyle
    {
        Normal = 0,
        Bold = 1,
    }

    /// <summary>
    /// Low-level element creation shared by the mod UI widgets (text, sprites, event triggers).
    /// </summary>
    internal static class ModUiFactory
    {
        private static readonly System.Type? TmpTextType =
            System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

        internal static Component AddText(
            GameObject go,
            ModUiAssets assets,
            string text,
            float fontSize,
            ModUiFontStyle fontStyle)
        {
            Component? label = TmpTextType != null ? go.AddComponent(TmpTextType) as Component : null;
            if (label == null)
            {
                Text fallback = go.AddComponent<Text>();
                fallback.text = text;
                fallback.fontSize = (int)fontSize;
                fallback.color = assets.TextColor;
                return fallback;
            }

            assets.ApplyFont(label);
            ModUiText.SetText(label, text);
            PropertyInfo? sizeProp = label.GetType().GetProperty("fontSize", BindingFlags.Instance | BindingFlags.Public);
            sizeProp?.SetValue(label, fontSize);
            PropertyInfo? styleProp = label.GetType().GetProperty("fontStyle", BindingFlags.Instance | BindingFlags.Public);
            if (styleProp != null && styleProp.PropertyType.IsEnum)
            {
                styleProp.SetValue(label, Enum.ToObject(styleProp.PropertyType, (int)fontStyle));
            }

            return label;
        }

        internal static void ApplySprite(Image image, Sprite? sprite, Image.Type imageType, Color fallbackColor)
        {
            image.preserveAspect = false;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = imageType;
                image.color = Color.white;
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Sliced;
            image.color = fallbackColor;
        }

        internal static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action callback)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
        }

        internal static void PlaySfx(string sfxId) => ModUiGameAccess.TryPlaySfx(sfxId);
    }
}

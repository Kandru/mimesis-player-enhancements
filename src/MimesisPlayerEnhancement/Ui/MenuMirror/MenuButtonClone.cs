using UnityEngine;

namespace MimesisPlayerEnhancement.Ui.MenuMirror
{
    /// <summary>
    /// Clones a vanilla menu button to inherit its sprite/font/size, then strips the
    /// game's wiring (element markers, localization, hover triggers) and rewires
    /// click + hover behavior to match vanilla feel.
    /// </summary>
    internal static class MenuButtonClone
    {
        private const string Feature = "MenuMirror";
        private const string HoverSfxId = "ButtonHover";
        private const string ClickSfxId = "ButtonClick";

        internal static GameObject? Create(GameObject styleSource, CustomMenuButton spec)
        {
            GameObject clone = UnityEngine.Object.Instantiate(styleSource, styleSource.transform.parent);
            clone.name = $"ModMenuButton_{spec.Id}";

            StripGameWiring(clone);

            Button? button = clone.GetComponent<Button>() ?? clone.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                ModLog.Warn(Feature, $"Style source for '{spec.Id}' has no Button component — clone discarded.");
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            Component? label = ModUiText.FindTextComponent(clone);
            Color baseColor = spec.LabelColor ?? Color.white;
            if (label != null)
            {
                ModUiText.SetText(label, spec.Label);
                ModUiText.SetColor(label, baseColor);
            }

            // Replace the event entirely: persistent (serialized) listeners from the
            // vanilla prefab would otherwise fire alongside our handler.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                ModUiGameAccess.TryPlaySfx(ClickSfxId);
                try
                {
                    spec.OnClick();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Custom button '{spec.Id}' click handler failed — {ex.Message}");
                }
            });
            button.interactable = true;

            AddHoverBehavior(clone, button, label, baseColor);

            clone.SetActive(true);
            return clone;
        }

        private static void StripGameWiring(GameObject clone)
        {
            foreach (UIElementMarker marker in clone.GetComponentsInChildren<UIElementMarker>(true))
            {
                UnityEngine.Object.Destroy(marker);
            }

            foreach (UIApplyL10N l10n in clone.GetComponentsInChildren<UIApplyL10N>(true))
            {
                UnityEngine.Object.Destroy(l10n);
            }

            // Vanilla hover triggers capture the source button's label; rebuild fresh ones.
            foreach (EventTrigger trigger in clone.GetComponentsInChildren<EventTrigger>(true))
            {
                UnityEngine.Object.Destroy(trigger);
            }
        }

        private static void AddHoverBehavior(GameObject clone, Button button, Component? label, Color baseColor)
        {
            if (label == null)
            {
                return;
            }

            EventTrigger trigger = clone.AddComponent<EventTrigger>();

            EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (!button.interactable)
                {
                    return;
                }

                ModUiGameAccess.TryPlaySfx(HoverSfxId);
                Color hoverColor = ModUiGameAccess.TryGetUiManager()?.mouseOverTextColor ?? Color.black;
                ModUiText.SetColor(label, hoverColor);
            });
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                if (button.interactable)
                {
                    ModUiText.SetColor(label, baseColor);
                }
            });
            trigger.triggers.Add(exit);
        }
    }
}

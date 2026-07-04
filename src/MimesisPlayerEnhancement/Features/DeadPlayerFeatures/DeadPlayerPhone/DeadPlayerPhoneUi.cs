using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneUi
    {
        internal static void UpdateSpectatorHints(
            UIPrefab_Spectator spectator,
            object? possessionKeyText,
            Component? phoneKeyText,
            Image? progressFill)
        {
            if (!DeadPlayerPhoneResolver.IsPhoneRingEnabled || spectator == null)
            {
                ResetPhoneHint(phoneKeyText);
                return;
            }

            bool mimicAvailable = DeadPlayerPhoneGameAccess.TryGetCameraManager()?.AvailableMimic != null;
            bool phoneAvailable = DeadPlayerPhoneClient.AvailablePhone != null
                || DeadPlayerPhoneLocalState.HasActiveLocalSession;
            PreferredDeadPlayerAction preferred = DeadPlayerPhoneClient.PreferredAction;

            UpdateHintWeight(
                possessionKeyText,
                mimicAvailable,
                preferred == PreferredDeadPlayerAction.Mimic);

            UpdateHintWeight(
                phoneKeyText,
                phoneAvailable,
                preferred == PreferredDeadPlayerAction.Phone);

            if (phoneKeyText != null)
            {
                phoneKeyText.gameObject.SetActive(phoneAvailable || DeadPlayerPhoneLocalState.HasActiveLocalSession);
                if (phoneAvailable)
                {
                    DeadPlayerPhoneUiReflection.SetText(
                        phoneKeyText,
                        GameLocaleAccess.GetL10NText("STRING_CAN_USE_PHONE"));
                }
            }

            UpdateProgressFill(progressFill);
        }

        internal static void UpdateProgressFill(Image? progressFill)
        {
            if (progressFill == null)
            {
                return;
            }

            if (!DeadPlayerPhoneLocalState.HasActiveLocalSession)
            {
                if (!DeadPlayerPhoneResolver.IsPhoneRingEnabled)
                {
                    progressFill.gameObject.SetActive(false);
                }

                return;
            }

            progressFill.gameObject.SetActive(true);
            progressFill.fillAmount = DeadPlayerPhoneLocalState.GetFillAmount();
        }

        private static void UpdateHintWeight(object? text, bool visible, bool preferred)
        {
            if (!visible || text == null)
            {
                return;
            }

            DeadPlayerPhoneUiReflection.SetFontWeight(text, preferred);
        }

        private static void ResetPhoneHint(Component? phoneKeyText)
        {
            if (phoneKeyText != null)
            {
                phoneKeyText.gameObject.SetActive(false);
            }
        }
    }
}

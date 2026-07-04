using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneUiSetup
    {
        private static readonly FieldInfo? PossessionKeyTextField =
            AccessTools.Field(typeof(UIPrefab_Spectator), "_possessionKeyText");

        private static readonly FieldInfo? PossessionUiField =
            AccessTools.Field(typeof(UIPrefab_Spectator), "_possessionUI");

        private static readonly FieldInfo? PossessionKeyCooltimeField =
            AccessTools.Field(typeof(UIPrefab_Spectator), "_possessionKeyCooltime");

        private static Component? _phoneKeyText;

        internal static object? GetPossessionKeyText(UIPrefab_Spectator spectator) =>
            PossessionKeyTextField?.GetValue(spectator);

        internal static Image? GetProgressFill(UIPrefab_Spectator spectator) =>
            PossessionKeyCooltimeField?.GetValue(spectator) as Image;

        internal static Component? GetOrCreatePhoneKeyText(UIPrefab_Spectator spectator)
        {
            if (_phoneKeyText != null)
            {
                return _phoneKeyText;
            }

            object? mimicText = GetPossessionKeyText(spectator);
            if (mimicText is not Component mimicComponent)
            {
                return null;
            }

            GameObject? possessionUi = PossessionUiField?.GetValue(spectator) as GameObject;
            Transform parent = possessionUi != null ? possessionUi.transform : mimicComponent.transform.parent;
            GameObject phoneObj = new GameObject("DeadPlayerPhoneKeyText");
            phoneObj.transform.SetParent(parent, worldPositionStays: false);

            RectTransform mimicRect = mimicComponent.GetComponent<RectTransform>();
            RectTransform phoneRect = phoneObj.AddComponent<RectTransform>();
            phoneRect.anchorMin = mimicRect.anchorMin;
            phoneRect.anchorMax = mimicRect.anchorMax;
            phoneRect.pivot = mimicRect.pivot;
            phoneRect.anchoredPosition = mimicRect.anchoredPosition + new Vector2(0f, -28f);
            phoneRect.sizeDelta = mimicRect.sizeDelta;

            _phoneKeyText = DeadPlayerPhoneUiReflection.AddTmpTextComponent(phoneObj);
            if (_phoneKeyText == null)
            {
                Object.Destroy(phoneObj);
                return null;
            }

            DeadPlayerPhoneUiReflection.CopyStyle(mimicText, _phoneKeyText);
            DeadPlayerPhoneUiReflection.SetText(_phoneKeyText, DeadPlayerPhoneUiReflection.GetText(mimicText));
            _phoneKeyText.gameObject.SetActive(false);
            return _phoneKeyText;
        }

        internal static void Reset()
        {
            if (_phoneKeyText != null)
            {
                Object.Destroy(_phoneKeyText.gameObject);
                _phoneKeyText = null;
            }
        }
    }
}

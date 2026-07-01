using System.Linq;
using MimesisPlayerEnhancement.Ui;
using ReluProtocol;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotDisplayInfo
    {
        internal string BaseText { get; set; } = string.Empty;
        internal string VersionCheckText { get; set; } = string.Empty;
        internal string FullText { get; set; } = string.Empty;
        internal bool IsVersionCompatible { get; set; } = true;
    }

    internal static class SaveSlotDisplayFormatter
    {
        private const int MinimumUsableSaveDataVersion = 1;

        internal static UnityEngine.Color DefaultTextColor => ModUiAssets.DefaultTextColor;

        internal static SaveSlotDisplayInfo Format(MMSaveGameData data)
        {
            string dateText = data.RegDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string cycleText = SaveSlotGameAccess.GetL10NText("STRING_LOAD_SLOT_CYCLE", data.StageCount);
            string stageText = data.StageCount != 1
                ? data.TramRepaired
                    ? SaveSlotGameAccess.GetL10NText("STRING_LOAD_SLOT_REPAIR_AFTER")
                    : SaveSlotGameAccess.GetL10NText("STRING_LOAD_SLOT_REPAIR_BEFORE")
                : SaveSlotGameAccess.GetL10NText("STRING_LOAD_SLOT_START");

            string versionText = data.Version < MinimumUsableSaveDataVersion
                ? SaveSlotGameAccess.GetL10NText("STRING_VERSION_MISSMATCH")
                : string.Empty;

            string playerNames = string.Join(", ", data.PlayerNames.ToArray());
            string baseText = $"[{dateText}], {cycleText}, {stageText}, ${data.Currency}, {playerNames}";
            string fullText = BuildVersionCheckPrefix(versionText, isRed: true) + baseText;

            return new SaveSlotDisplayInfo
            {
                BaseText = baseText,
                VersionCheckText = versionText,
                FullText = fullText,
                IsVersionCompatible = string.IsNullOrEmpty(versionText),
            };
        }

        internal static string FormatAutosaveTitle(MMSaveGameData data)
        {
            return SaveSlotGameAccess.GetL10NText("STRING_LOAD_SLOT_AUTO_SAVED").Replace("{0}", "#" + data.SlotID);
        }

        internal static string BuildBlinkText(SaveSlotDisplayInfo info, bool showVersionWarning)
        {
            if (!showVersionWarning || string.IsNullOrEmpty(info.VersionCheckText))
            {
                return info.FullText;
            }

            return BuildVersionCheckPrefix(info.VersionCheckText, isRed: true) + info.BaseText;
        }

        internal static string BuildTransparentBlinkText(SaveSlotDisplayInfo info)
        {
            if (string.IsNullOrEmpty(info.VersionCheckText))
            {
                return info.FullText;
            }

            return "<color=#00000000>" + info.VersionCheckText + "</color> " + info.BaseText;
        }

        internal static void ApplyDefaultTextColor(Component? textComponent)
        {
            ModUiText.SetColor(textComponent, DefaultTextColor);
        }

        private static string BuildVersionCheckPrefix(string versionCheckText, bool isRed)
        {
            if (string.IsNullOrEmpty(versionCheckText))
            {
                return string.Empty;
            }

            string color = isRed ? "red" : "#FFF0C2";
            return "<color=" + color + ">" + versionCheckText + "</color> ";
        }
    }
}

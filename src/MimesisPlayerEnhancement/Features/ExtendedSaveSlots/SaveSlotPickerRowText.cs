using System.Text;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotPickerRowText
    {
        internal enum Line2Style
        {
            Normal,
            VersionBlink,
            VersionBlinkHidden,
        }

        internal static string Compose(SaveSlotEntry entry, Line2Style line2Style = Line2Style.Normal)
        {
            string line1 = SaveSlotRoomListMapper.FormatLine1(entry);
            string line2 = GetLine2(entry, line2Style);

            StringBuilder sb = new();
            sb.Append("<size=20><b>").Append(EscapeRichText(line1)).Append("</b></size>");
            sb.Append('\n');
            sb.Append("<size=17>").Append(line2).Append("</size>");

            string line3 = SaveSlotPickerExtraStats.FormatLine3(entry.SlotId);
            sb.Append('\n');
            sb.Append("<size=15><color=#B8AE94>").Append(EscapeRichText(line3)).Append("</color></size>");

            return sb.ToString();
        }

        private static string GetLine2(SaveSlotEntry entry, Line2Style style)
        {
            SaveSlotDisplayInfo info = entry.Display;
            return style switch
            {
                Line2Style.VersionBlinkHidden => SaveSlotDisplayFormatter.BuildTransparentBlinkText(info),
                Line2Style.VersionBlink => SaveSlotDisplayFormatter.BuildBlinkText(info, showVersionWarning: true),
                _ => SaveSlotDisplayFormatter.Format(entry.Data).BaseText,
            };
        }

        private static string EscapeRichText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}

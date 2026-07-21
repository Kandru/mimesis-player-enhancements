namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal static class AnnouncementMultiplierFormat
    {
        internal static bool IsDefaultMultiplier(float multiplier)
        {
            return multiplier is >= 0.995f and <= 1.005f;
        }

        internal static string FormatMultiplier(float multiplier)
        {
            return $"×{multiplier:0.##}";
        }

        internal static string FormatBonusSeconds(double bonusSeconds)
        {
            return bonusSeconds.ToString("0.##");
        }
    }
}

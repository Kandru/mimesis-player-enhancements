using System.Linq;
using System.Reflection;
using System.Text;
using MelonLoader;
using MelonLoader.Logging;
using MelonLoader.Pastel;

namespace MimesisPlayerEnhancement;

/// <summary>
/// Central logging. The feature tag is added automatically — do not repeat it in messages.
/// Use em dashes (—) to separate clauses. <see cref="Info"/> for operational events;
/// <see cref="Debug"/> only emits when <see cref="ModConfig.EnableDebugLogging"/> is true.
/// </summary>
public static class ModLog
{
    internal static readonly ColorARGB SuccessGreen = ColorARGB.Green;
    internal static readonly ColorARGB FailureRed = ColorARGB.Red;
    internal static readonly ColorARGB Neutral = MelonLogger.DefaultTextColor;

    private static readonly MethodInfo? PassLogMsgMethod = typeof(MelonLogger).GetMethod(
        "PassLogMsg",
        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
        null,
        new[] { typeof(ColorARGB), typeof(string), typeof(ColorARGB), typeof(string), typeof(string) },
        null);

    /// <summary>
    /// Wine/Proton consoles do not render embedded ANSI (see MelonLoader bootstrap MelonLogger).
    /// Use plain text plus a single ConsoleColor there; use MelonLoader.Pastel elsewhere.
    /// </summary>
    private static bool UseLegacyConsoleColors => MelonUtils.IsUnderWineOrSteamProton();

    /// <summary>
    /// MelonLoader wraps <paramref name="section"/> in brackets; pass <c>Feature][Title</c> for
    /// <c>[Feature][Title]</c> in console and log output.
    /// </summary>
    internal static string FeatureSection(string feature, string title) => $"{feature}][{title}";

    /// <summary>
    /// One native log line with per-segment console colors. Follows MelonLogger.PastelMsg on native
    /// terminals and the bootstrap Wine path (plain message + one ConsoleColor) under Proton.
    /// </summary>
    internal static void PassLogSegmented(
        string section,
        string stripped,
        params (ColorARGB? color, string text)[] segments)
    {
        if (PassLogMsgMethod == null)
        {
            MelonLogger.Msg(stripped);
            return;
        }

        bool plain = UseLegacyConsoleColors;
        string body = BuildMessageBody(segments, plain);
        ColorARGB msgColor = plain ? PickMessageColor(segments) : Neutral;

        PassLogMsgMethod.Invoke(null, new object?[] { msgColor, body, Neutral, section, stripped });
    }

    private static string BuildMessageBody((ColorARGB? color, string text)[] segments, bool plain)
    {
        var sb = new StringBuilder();
        foreach (var (color, text) in segments)
            sb.Append(plain || !color.HasValue ? text : text.Pastel(color.Value));
        return sb.ToString();
    }

    private static ColorARGB PickMessageColor((ColorARGB? color, string text)[] segments)
    {
        bool hasFailure = segments.Any(s => s.color.HasValue && s.color.Value.Equals(FailureRed));
        if (hasFailure)
            return FailureRed;

        bool hasSuccess = segments.Any(s => s.color.HasValue && s.color.Value.Equals(SuccessGreen));
        if (hasSuccess)
            return SuccessGreen;

        var onlyColor = segments
            .Where(s => s.color.HasValue)
            .Select(s => s.color!.Value)
            .Distinct()
            .Take(2)
            .ToArray();

        return onlyColor.Length == 1 ? onlyColor[0] : Neutral;
    }

    public static void Info(string feature, string message) =>
        MelonLogger.Msg($"[{feature}] {message}");

    public static void Warn(string feature, string message) =>
        MelonLogger.Warning($"[{feature}] {message}");

    public static void Error(string feature, string message) =>
        MelonLogger.Error($"[{feature}] {message}");

    public static void Debug(string feature, string message)
    {
        if (ModConfig.EnableDebugLogging.Value)
            MelonLogger.Msg($"[{feature}:debug] {message}");
    }
}

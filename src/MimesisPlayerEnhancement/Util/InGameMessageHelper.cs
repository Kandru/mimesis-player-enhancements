using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Bottom-left in-game toasts via <see cref="UIPrefab_PlayerEnterInfo"/>.
    /// Shared by Statistics, PlayerAnnouncements, and JoinAnytime.
    /// Use <see cref="ShowModMessage"/> for plain English — do not call
    /// <see cref="UIPrefab_PlayerEnterInfo.AddPlayerInfo"/> for mod text; it localizes via
    /// <c>ROOM_ENTER_STRING</c> / <c>ROOM_EXIT_STRING</c> and the <c>[usernickname:]</c> placeholder.
    /// </summary>
    internal static class InGameMessageHelper
    {
        private const string Feature = "InGameMessage";

        internal const string MessagePrefix = "[PlayerEnhancements]";

        /// <summary>Distinct from the game's green enter / red exit toasts (see <see cref="UIPrefab_PlayerEnterInfo"/>).</summary>
        private const string LocalOnlyRichTextColor = "#88CCFF";

        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo EnterColor1Field =
            typeof(UIPrefab_PlayerEnterInfo).GetField("EnterColor1", InstanceMemberFlags);

        private static readonly FieldInfo EnterColor2Field =
            typeof(UIPrefab_PlayerEnterInfo).GetField("EnterColor2", InstanceMemberFlags);

        private static readonly FieldInfo ExitColor1Field =
            typeof(UIPrefab_PlayerEnterInfo).GetField("ExitColor1", InstanceMemberFlags);

        private static readonly FieldInfo ExitColor2Field =
            typeof(UIPrefab_PlayerEnterInfo).GetField("ExitColor2", InstanceMemberFlags);

        private static PropertyInfo? _tmpTextProperty;
        private static PropertyInfo? _tmpColorGradientProperty;
        private static ConstructorInfo? _vertexGradientConstructor;
        private static PropertyInfo? _componentGameObjectProperty;

        /// <param name="localOnly">
        /// True when the toast is shown only to the local player (e.g. session intro).
        /// Wrapped in a TMP color tag so it stands out from join/leave stats everyone sees.
        /// </param>
        /// <param name="ignoreFeatureToggles">
        /// When true, show even if statistics and player-announcement toasts are disabled.
        /// </param>
        internal static void ShowModMessage(
            string message,
            bool isEntering = true,
            bool localOnly = false,
            bool ignoreFeatureToggles = false)
        {
            if (!ignoreFeatureToggles
                && !ModConfig.ShowStatisticsToasts.Value
                && !ModConfig.ShowPlayerAnnouncements.Value)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string formatted = FormatModMessage(message, localOnly);

            try
            {
                if (TryEnqueueRawPlayerInfo(formatted, isEntering))
                {
                    return;
                }

                ModLog.Debug(Feature, "Player enter info UI unavailable for mod toast.");
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Mod toast failed: {ex.Message}");
            }
        }

        internal static string FormatModMessage(string message, bool localOnly)
        {
            string text = $"{MessagePrefix} {message.Trim()}";
            if (localOnly)
            {
                text = $"<color={LocalOnlyRichTextColor}>{text}</color>";
            }

            return text;
        }

        /// <summary>
        /// Replaces the game's <c>UpdatePlayerInfos</c> coroutine so mod toasts can use a longer display time.
        /// Returns null when the required UI fields cannot be resolved so callers can fall back to vanilla.
        /// </summary>
        internal static IEnumerator? TryCreateExtendedPlayerEnterInfoUpdates(UIPrefab_PlayerEnterInfo ui)
        {
            // Resolve fields eagerly (iterator bodies run lazily) so a reflection miss
            // falls back to the vanilla coroutine instead of failing mid-frame.
            IList? currentDisplayed = GetListField(ui, "currentDisplayed");
            IList? displayStartTimeMilliSec = GetListField(ui, "displayStartTimeMilliSec");
            IList? isEnteringFlags = GetListField(ui, "isEnteringFlags");
            IList? playerInfos = GetListField(ui, "PlayerInfos");
            if (currentDisplayed == null || displayStartTimeMilliSec == null || isEnteringFlags == null || playerInfos == null)
            {
                ModLog.Warn(Feature, "Extended toast coroutine unavailable — UIPrefab_PlayerEnterInfo fields not found");
                return null;
            }

            return RunExtendedPlayerEnterInfoUpdates(ui, currentDisplayed, displayStartTimeMilliSec, isEnteringFlags, playerInfos);
        }

        private static IEnumerator RunExtendedPlayerEnterInfoUpdates(
            UIPrefab_PlayerEnterInfo ui,
            IList currentDisplayed,
            IList displayStartTimeMilliSec,
            IList isEnteringFlags,
            IList playerInfos)
        {
            long fadeOutMs = ui.fadeOutDisplayTimeMilliSec;

            Color enterColor1 = (Color)EnterColor1Field.GetValue(ui)!;
            Color enterColor2 = (Color)EnterColor2Field.GetValue(ui)!;
            Color exitColor1 = (Color)ExitColor1Field.GetValue(ui)!;
            Color exitColor2 = (Color)ExitColor2Field.GetValue(ui)!;

            while (true)
            {
                yield return new WaitUntil(() => currentDisplayed.Count > 0);

                long currentTickMilliSec = GetCurrentTickMilliSec();

                while (displayStartTimeMilliSec.Count > 0)
                {
                    long displayMs = GetDisplayDurationMilliSec(ui, 0);
                    long startMs = Convert.ToInt64(displayStartTimeMilliSec[0]!);
                    if (currentTickMilliSec - startMs <= displayMs + fadeOutMs)
                    {
                        break;
                    }

                    currentDisplayed.RemoveAt(0);
                    displayStartTimeMilliSec.RemoveAt(0);
                    isEnteringFlags.RemoveAt(0);
                }

                for (int i = 0; i < playerInfos.Count; i++)
                {
                    object label = playerInfos[i]!;
                    if (i < currentDisplayed.Count)
                    {
                        long displayMs = GetDisplayDurationMilliSec(ui, i);
                        long startMs = Convert.ToInt64(displayStartTimeMilliSec[i]!);
                        float fade = Mathf.Clamp01((currentTickMilliSec - startMs - displayMs) / (float)fadeOutMs);
                        bool entering = (bool)isEnteringFlags[i]!;

                        Color c1 = entering ? enterColor1 : exitColor1;
                        c1.a = 1f - fade;
                        Color c2 = entering ? enterColor2 : exitColor2;
                        c2.a = 1f - fade;

                        SetLabelText(label, (string)currentDisplayed[i]!);
                        SetLabelGradient(label, c1, c2);
                        SetLabelActive(label, true);
                    }
                    else
                    {
                        SetLabelActive(label, false);
                        SetLabelText(label, string.Empty);
                    }
                }

                yield return null;
            }
        }

        internal static long GetDisplayDurationMilliSec(UIPrefab_PlayerEnterInfo ui, int index)
        {
            IList? currentDisplayed = GetListField(ui, "currentDisplayed");
            return currentDisplayed != null
                && index < currentDisplayed.Count
                && currentDisplayed[index] is string text
                && text.Contains(MessagePrefix)
                ? (long)(Mathf.Max(1f, ModConfig.ModToastDurationSeconds.Value) * 1000f)
                : ui.displayTimeSecMilliSec;
        }

        private static void EnsureTmpReflection(object label)
        {
            if (_tmpTextProperty != null)
            {
                return;
            }

            Type labelType = label.GetType();
            _tmpTextProperty = labelType.GetProperty("text", InstanceMemberFlags);
            _tmpColorGradientProperty = labelType.GetProperty("colorGradient", InstanceMemberFlags);
            _componentGameObjectProperty = typeof(Component).GetProperty("gameObject", InstanceMemberFlags);

            Type? vertexGradientType = labelType.Assembly.GetType("TMPro.VertexGradient");
            _vertexGradientConstructor = vertexGradientType?.GetConstructor([typeof(Color), typeof(Color), typeof(Color), typeof(Color)]);
        }

        private static void SetLabelText(object label, string text)
        {
            EnsureTmpReflection(label);
            _tmpTextProperty?.SetValue(label, text);
        }

        private static void SetLabelGradient(object label, Color c1, Color c2)
        {
            EnsureTmpReflection(label);
            if (_vertexGradientConstructor == null || _tmpColorGradientProperty == null)
            {
                return;
            }

            object gradient = _vertexGradientConstructor.Invoke([c1, c1, c2, c2]);
            _tmpColorGradientProperty.SetValue(label, gradient);
        }

        private static void SetLabelActive(object label, bool active)
        {
            EnsureTmpReflection(label);
            if (_componentGameObjectProperty?.GetValue(label) is GameObject gameObject)
            {
                gameObject.SetActive(active);
            }
        }

        private static bool TryEnqueueRawPlayerInfo(string message, bool isEntering)
        {
            if (GetPlayerEnterInfoUi() is not UIPrefab_PlayerEnterInfo ui)
            {
                return false;
            }

            IList? currentDisplayed = GetListField(ui, "currentDisplayed");
            IList? displayStartTime = GetListField(ui, "displayStartTimeMilliSec");
            IList? isEnteringFlags = GetListField(ui, "isEnteringFlags");
            IList? playerInfos = GetListField(ui, "PlayerInfos");
            if (currentDisplayed == null || displayStartTime == null || isEnteringFlags == null || playerInfos == null)
            {
                return false;
            }

            if (playerInfos.Count <= 0)
            {
                return false;
            }

            TrimOldestIfFull(currentDisplayed, displayStartTime, isEnteringFlags, playerInfos.Count);

            _ = currentDisplayed.Add(message);
            _ = displayStartTime.Add(GetCurrentTickMilliSec());
            _ = isEnteringFlags.Add(isEntering);
            return true;
        }

        private static void TrimOldestIfFull(
            IList currentDisplayed,
            IList displayStartTime,
            IList isEnteringFlags,
            int maxVisible)
        {
            while (currentDisplayed.Count >= maxVisible && currentDisplayed.Count > 0)
            {
                currentDisplayed.RemoveAt(0);
                displayStartTime.RemoveAt(0);
                isEnteringFlags.RemoveAt(0);
            }
        }

        private static readonly Dictionary<string, FieldInfo?> ListFieldCache = [];

        private static IList? GetListField(object target, string fieldName)
        {
            if (!ListFieldCache.TryGetValue(fieldName, out FieldInfo? field))
            {
                field = target.GetType().GetField(fieldName, InstanceMemberFlags);
                ListFieldCache[fieldName] = field;
            }

            return field?.GetValue(target) as IList;
        }

        private static MethodInfo? _getTickMethod;
        private static bool _loggedTickFallback;

        private static long GetCurrentTickMilliSec()
        {
            try
            {
                TimeUtil? timeUtil = GameSessionAccess.TryGetTimeUtil();
                if (timeUtil == null)
                {
                    return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                _getTickMethod ??= timeUtil.GetType().GetMethod(
                    "GetCurrentTickMilliSec",
                    InstanceMemberFlags,
                    null,
                    Type.EmptyTypes,
                    null);
                return _getTickMethod == null
                    ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    : Convert.ToInt64(_getTickMethod.Invoke(timeUtil, null));
            }
            catch (Exception ex)
            {
                if (!_loggedTickFallback)
                {
                    _loggedTickFallback = true;
                    ModLog.Warn(Feature, $"Game tick unavailable — falling back to UTC clock for toast timing — {ex.Message}");
                }

                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        private static UIPrefab_PlayerEnterInfo? GetPlayerEnterInfoUi()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null)
            {
                return null;
            }

            object? main = pdata.GetType().GetField("main", InstanceMemberFlags)?.GetValue(pdata);
            return main == null
                ? null
                : main.GetType().GetField("playerEnterInfoUI", InstanceMemberFlags)?.GetValue(main)
                   as UIPrefab_PlayerEnterInfo;
        }
    }
}

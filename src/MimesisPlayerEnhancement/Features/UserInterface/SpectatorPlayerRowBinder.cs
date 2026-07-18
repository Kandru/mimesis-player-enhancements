using System.Reflection;
using System.Threading;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    internal static class SpectatorPlayerRowBinder
    {
        private static readonly FieldInfo? LiveColorField =
            AccessTools.Field(typeof(UIPrefab_Spectator_PlayerListView), "liveColor");

        private static readonly FieldInfo? DeadColorField =
            AccessTools.Field(typeof(UIPrefab_Spectator_PlayerListView), "deadColor");

        private static readonly PropertyInfo? NameTextProperty =
            AccessTools.Property(typeof(UIPrefab_Spectator_PlayerListViewItem), "UE_Name_Text");

        private static readonly PropertyInfo? SpeakAnimationProperty =
            AccessTools.Property(typeof(UIPrefab_Spectator_PlayerListViewItem), "SpriteChangeAnimation");

        private static readonly PropertyInfo? IsPossessorProperty =
            AccessTools.Property(typeof(UIPrefab_Spectator_PlayerListViewItem), "IsPossessor");

        private static readonly MethodInfo? SpeakPlayMethod =
            AccessTools.Method(typeof(SpriteChangeAnimation), "Play", [typeof(CancellationToken)]);

        private static readonly MethodInfo? SpeakTurnOffMethod =
            AccessTools.Method(typeof(SpriteChangeAnimation), "TurnOff");

        private static readonly PropertyInfo? SpeakCanPlayProperty =
            AccessTools.Property(typeof(SpriteChangeAnimation), "CanPlay");

        private static readonly PropertyInfo? SpeakIsPlayingProperty =
            AccessTools.Property(typeof(SpriteChangeAnimation), "IsPlaying");

        internal static void CacheColors(UIPrefab_Spectator_PlayerListView listView, out Color liveColor, out Color deadColor)
        {
            liveColor = LiveColorField?.GetValue(listView) is Color live ? live : Color.white;
            deadColor = DeadColorField?.GetValue(listView) is Color dead ? dead : Color.red;
        }

        internal static void SetRowName(UIPrefab_Spectator_PlayerListViewItem row, string text)
        {
            if (NameTextProperty?.GetValue(row) is Component nameText)
            {
                MethodInfo? setText = nameText.GetType().GetMethod("SetText", [typeof(string)]);
                _ = setText?.Invoke(nameText, [text]);
            }
        }

        internal static void SetPossessorActive(UIPrefab_Spectator_PlayerListViewItem row, bool active)
        {
            if (IsPossessorProperty?.GetValue(row) is Component possessor)
            {
                possessor.gameObject.SetActive(active);
            }
        }

        internal static void TurnOffSpeakAnimation(UIPrefab_Spectator_PlayerListViewItem row)
        {
            object? speakAnimation = SpeakAnimationProperty?.GetValue(row);
            if (speakAnimation != null && SpeakTurnOffMethod != null)
            {
                SpeakTurnOffMethod.Invoke(speakAnimation, null);
            }
        }

        internal static void BindSpeakState(
            UIPrefab_Spectator_PlayerListViewItem row,
            bool speaking,
            CancellationToken cancellationToken)
        {
            object? speakAnimation = SpeakAnimationProperty?.GetValue(row);
            if (speaking)
            {
                if (speakAnimation != null
                    && SpeakCanPlayProperty?.GetValue(speakAnimation) is true
                    && SpeakPlayMethod != null)
                {
                    _ = SpeakPlayMethod.Invoke(speakAnimation, [cancellationToken]);
                }

                return;
            }

            if (speakAnimation == null
                || SpeakIsPlayingProperty?.GetValue(speakAnimation) is not true)
            {
                TurnOffSpeakAnimation(row);
            }
        }

        internal static void StopSpeakAnimations(IEnumerable<UIPrefab_Spectator_PlayerListViewItem> rows)
        {
            foreach (UIPrefab_Spectator_PlayerListViewItem row in rows)
            {
                if (row != null)
                {
                    TurnOffSpeakAnimation(row);
                }
            }
        }
    }
}

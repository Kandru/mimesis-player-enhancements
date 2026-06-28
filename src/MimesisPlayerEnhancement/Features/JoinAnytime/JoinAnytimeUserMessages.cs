using System;
using System.Collections;
using System.Reflection;
using MelonLoader;
using Mimic.Actors;
using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Util;
using ReluProtocol.C2S;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeUserMessages
    {
        private const string Feature = "JoinAnytime";
        private const string LeverBlockedMessage = "Can't depart yet — other players are still in the dungeon.";
        private const float LeverFeedbackDelaySeconds = 0.5f;
        private const float LeverFeedbackDedupSeconds = 5f;

        private static readonly FieldInfo? StartGameSigField =
            typeof(InTramWaitingScene).GetField("startGameSig", BindingFlags.NonPublic | BindingFlags.Instance);

        private static DateTime _lastLeverBlockedShownUtc;

        internal static void OnWaitingRoomStartBlocked(IVroom room, int actorId)
        {
            if (!LateJoinManager.IsEnabled || actorId == 0)
            {
                return;
            }

            VPlayer? player = room.FindPlayerByObjectID(actorId);
            if (player == null)
            {
                return;
            }

            if (LocalPlayerHelper.IsLocalSteamId(player.SteamID))
            {
                ShowLeverBlockedLocal(immediate: true);
            }
        }

        internal static void OnLocalTramLeverOpened(int actorId)
        {
            if (!LateJoinManager.IsEnabled)
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.main is not InTramWaitingScene)
            {
                return;
            }

            ProtoActor? avatar = pdata.main.GetMyAvatar();
            if (avatar == null || avatar.ActorID != actorId)
            {
                return;
            }

            ScheduleLocalLeverPullFeedback();
        }

        internal static void ScheduleLocalLeverPullFeedback()
        {
            if (!LateJoinManager.IsEnabled)
            {
                return;
            }

            _ = MelonCoroutines.Start(ShowLeverBlockedAfterPullIfNeeded());
        }

        private static IEnumerator ShowLeverBlockedAfterPullIfNeeded()
        {
            // Default tram lever opening duration is 3s (openingDurationToMap).
            yield return new WaitForSeconds(3f + LeverFeedbackDelaySeconds);

            if (!LateJoinManager.IsEnabled)
            {
                yield break;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.main is not InTramWaitingScene scene)
            {
                yield break;
            }

            if (HasPendingDungeonStart(scene))
            {
                yield break;
            }

            ShowLeverBlockedLocal(immediate: false);
        }

        private static void ShowLeverBlockedLocal(bool immediate)
        {
            DateTime now = DateTime.UtcNow;
            if ((now - _lastLeverBlockedShownUtc).TotalSeconds < LeverFeedbackDedupSeconds)
            {
                return;
            }

            _lastLeverBlockedShownUtc = now;

            InGameMessageHelper.ShowModMessage(
                LeverBlockedMessage,
                isEntering: false,
                localOnly: true,
                ignoreFeatureToggles: true);

            ModLog.Debug(
                Feature,
                immediate
                    ? "Showed tram lever blocked toast (server)"
                    : "Showed tram lever blocked toast (client feedback)");
        }

        private static bool HasPendingDungeonStart(InTramWaitingScene scene)
        {
            if (StartGameSigField?.GetValue(scene) is MoveToDungeonSig)
            {
                return true;
            }

            return false;
        }
    }
}

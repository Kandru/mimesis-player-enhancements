using System;
using System.Collections;
using System.Reflection;
using MelonLoader;
using ReluProtocol.C2S;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeUserMessages
    {
        private const string Feature = "JoinAnytime";
        private const float LeverFeedbackDelaySeconds = 0.5f;
        private const float LeverFeedbackDedupSeconds = 5f;

        private static string GetMessageForReason(WaitingRoomBlockReason reason) =>
            reason switch
            {
                WaitingRoomBlockReason.PlayersConnecting => ModL10n.Get("joinanytime.blocked_connecting"),
                WaitingRoomBlockReason.ActiveDungeon => ModL10n.Get("joinanytime.blocked_dungeon"),
                _ => ModL10n.Get("joinanytime.blocked_split"),
            };

        private static readonly FieldInfo? StartGameSigField =
            typeof(InTramWaitingScene).GetField("startGameSig", BindingFlags.NonPublic | BindingFlags.Instance);

        private static DateTime _lastLeverBlockedShownUtc;
        private static WaitingRoomBlockReason _lastShownReason = WaitingRoomBlockReason.None;

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
                ShowLeverBlockedLocal(
                    JoinAnytimeRoomTools.GetWaitingRoomBlockReason(),
                    immediate: true);
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

            ShowLeverBlockedLocal(
                JoinAnytimeRoomTools.GetWaitingRoomBlockReason(),
                immediate: false);
        }

        private static void ShowLeverBlockedLocal(WaitingRoomBlockReason reason, bool immediate)
        {
            if (reason == WaitingRoomBlockReason.None)
            {
                reason = WaitingRoomBlockReason.PlayersSplit;
            }

            DateTime now = DateTime.UtcNow;
            if ((now - _lastLeverBlockedShownUtc).TotalSeconds < LeverFeedbackDedupSeconds
                && reason == _lastShownReason)
            {
                return;
            }

            _lastLeverBlockedShownUtc = now;
            _lastShownReason = reason;

            InGameMessageHelper.ShowModMessage(
                GetMessageForReason(reason),
                isEntering: false,
                localOnly: true,
                ignoreFeatureToggles: true);

            ModLog.Debug(Feature, immediate
                    ? $"Showed tram lever blocked toast ({reason}, server)"
                    : $"Showed tram lever blocked toast ({reason}, client feedback)");
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

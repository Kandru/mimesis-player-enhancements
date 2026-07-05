using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneServer
    {
        private const string Feature = "DeadPlayerFeatures";

        internal static bool TryInterceptHandleLevelObject(
            VPlayer player,
            int levelObjectId,
            int state,
            bool occupy,
            out MsgErrorCode result,
            out int fromState,
            out int toState)
        {
            result = MsgErrorCode.Success;
            fromState = -1;
            toState = state;

            if (!DeadPlayerPhoneResolver.IsModRingRequest(state, occupy)
                && !DeadPlayerPhoneResolver.IsModEndRequest(state, occupy))
            {
                return false;
            }

            IVroom? room = player.VRoom;
            if (room == null
                || !DeadPlayerPhoneAccess.TryGetLevelObject(room, levelObjectId, out _))
            {
                return false;
            }

            if (!DeadPlayerPhoneResolver.IsPhoneRingEnabled || !DeadPlayerPhoneResolver.ShouldApplyHost)
            {
                return false;
            }

            if (player.LifeCycle != VCreatureLifeCycle.Dead)
            {
                return false;
            }

            if (DeadPlayerPhoneResolver.IsModEndRequest(state, occupy))
            {
                if (!DeadPlayerPhoneSessions.TryGetActiveSession(out DeadPlayerPhoneSession? session)
                    || session == null
                    || session.DeadPlayerActorId != player.ObjectID
                    || session.PhoneLevelObjectId != levelObjectId)
                {
                    return false;
                }

                result = TryEndInteraction(player, levelObjectId, out fromState, out toState);
                return true;
            }

            if (!DeadPlayerPhoneResolver.IsModRingRequest(state, occupy))
            {
                return false;
            }

            result = TryStartRing(player, levelObjectId, out fromState, out toState);
            return true;
        }

        internal static MsgErrorCode TryStartRing(
            VPlayer player,
            int levelObjectId,
            out int fromState,
            out int toState)
        {
            fromState = -1;
            toState = (int)PhoneState.Ringing;

            if (player.LifeCycle != VCreatureLifeCycle.Dead)
            {
                ModLog.Debug(Feature, $"Ring rejected — player {player.ObjectID} is not dead");
                return MsgErrorCode.CantAction;
            }

            if (DeadPlayerPhoneSessions.HasActiveSession)
            {
                ModLog.Debug(Feature, "Ring rejected — another phone session is active");
                return MsgErrorCode.CantAction;
            }

            if (DeadPlayerPhoneSessions.IsInCooldown(player.ObjectID))
            {
                ModLog.Debug(Feature, $"Ring rejected — player {player.ObjectID} is in cooldown");
                return MsgErrorCode.CantAction;
            }

            IVroom? room = player.VRoom;
            if (room == null)
            {
                return MsgErrorCode.InvalidRoomType;
            }

            if (!DeadPlayerPhoneAccess.TryGetLevelObject(room, levelObjectId, out OccupiedLevelObjectInfo? phoneInfo)
                || phoneInfo == null)
            {
                return MsgErrorCode.LevelObjectNotFound;
            }

            if (phoneInfo.CurrentState != (int)PhoneState.Idle)
            {
                ModLog.Debug(Feature, $"Ring rejected — phone {levelObjectId} state={phoneInfo.CurrentState}");
                return MsgErrorCode.CantAction;
            }

            if (!DeadPlayerPhoneAccess.IsAnyAlivePlayerNearPhone(
                    room,
                    phoneInfo.Pos,
                    DeadPlayerPhoneResolver.MaxDistanceMeters))
            {
                ModLog.Debug(Feature, $"Ring rejected — no alive player near phone {levelObjectId}");
                return MsgErrorCode.CantAction;
            }

            MsgErrorCode error = room.HandleLevelObject(
                player.ObjectID,
                levelObjectId,
                (int)PhoneState.Ringing,
                occupy: false,
                out int prevState);

            fromState = prevState;
            if (error != MsgErrorCode.Success)
            {
                ModLog.Warn(Feature, $"Ring HandleLevelObject failed — phone={levelObjectId}, error={error}");
                return error;
            }

            long ringEndMs = DeadPlayerPhoneAccess.GetCurrentTimeMs()
                + (long)(DeadPlayerPhoneResolver.MaxRingTimeSeconds * 1000f);

            DeadPlayerPhoneSessions.StartRingSession(player.ObjectID, levelObjectId, ringEndMs);
            ModLog.Info(Feature, $"Phone ring started — deadPlayer={player.ObjectID}, phone={levelObjectId}");
            return MsgErrorCode.Success;
        }

        internal static MsgErrorCode TryEndInteraction(
            VPlayer player,
            int levelObjectId,
            out int fromState,
            out int toState)
        {
            fromState = -1;
            toState = -1;

            if (player.LifeCycle != VCreatureLifeCycle.Dead)
            {
                ModLog.Debug(Feature, $"End rejected — player {player.ObjectID} is not dead");
                return MsgErrorCode.CantAction;
            }

            if (!DeadPlayerPhoneSessions.TryGetActiveSession(out DeadPlayerPhoneSession? session)
                || session == null
                || session.DeadPlayerActorId != player.ObjectID
                || session.PhoneLevelObjectId != levelObjectId)
            {
                ModLog.Debug(Feature, $"End rejected — no matching session for phone {levelObjectId}");
                return MsgErrorCode.CantAction;
            }

            IVroom? room = player.VRoom;
            if (room == null)
            {
                return MsgErrorCode.InvalidRoomType;
            }

            if (session.Phase == DeadPlayerPhoneSessionPhase.Ringing)
            {
                MsgErrorCode error = room.HandleLevelObject(
                    0,
                    levelObjectId,
                    (int)PhoneState.Idle,
                    occupy: false,
                    out fromState);
                toState = (int)PhoneState.Idle;
                if (error == MsgErrorCode.Success)
                {
                    ModLog.Info(Feature, $"Phone ring cancelled — deadPlayer={player.ObjectID}, phone={levelObjectId}");
                    EndSession(player.ObjectID, applyCooldown: false);
                }

                return error;
            }

            if (session.Phase == DeadPlayerPhoneSessionPhase.Talking)
            {
                if (DeadPlayerPhoneAccess.TryGetLevelObject(room, levelObjectId, out OccupiedLevelObjectInfo? phoneInfo)
                    && phoneInfo != null)
                {
                    fromState = phoneInfo.CurrentState;
                }

                TryForcePhoneHangup(room, levelObjectId);
                toState = (int)PhoneState.Idle;
                ModLog.Info(Feature, $"Phone talk ended — deadPlayer={player.ObjectID}, phone={levelObjectId}");
                EndSession(player.ObjectID, applyCooldown: true);
                return MsgErrorCode.Success;
            }

            return MsgErrorCode.CantAction;
        }

        internal static void OnPhoneStateChanged(
            IVroom room,
            int phoneLevelObjectId,
            int prevState,
            int currentState,
            int actorId)
        {
            if (!DeadPlayerPhoneResolver.ShouldApplyHost
                || !DeadPlayerPhoneAccess.TryGetLevelObject(room, phoneLevelObjectId, out _)
                || !DeadPlayerPhoneSessions.TryGetActiveSession(out DeadPlayerPhoneSession? session)
                || session == null
                || session.PhoneLevelObjectId != phoneLevelObjectId)
            {
                return;
            }

            if (session.Phase == DeadPlayerPhoneSessionPhase.Ringing
                && prevState == (int)PhoneState.Ringing
                && currentState == (int)PhoneState.OnCall)
            {
                float talkSeconds = DeadPlayerPhoneResolver.RollTalkDurationSeconds();
                long talkEndMs = DeadPlayerPhoneAccess.GetCurrentTimeMs() + (long)(talkSeconds * 1000f);
                DeadPlayerPhoneSessions.StartTalkSession(
                    session.DeadPlayerActorId,
                    phoneLevelObjectId,
                    talkEndMs,
                    talkSeconds);
                ModLog.Info(
                    Feature,
                    $"Phone talk started — deadPlayer={session.DeadPlayerActorId}, phone={phoneLevelObjectId}, duration={talkSeconds:0.##}s");
                return;
            }

            if (currentState is (int)PhoneState.Idle or (int)PhoneState.Busy or (int)PhoneState.BusyWait)
            {
                EndSession(session.DeadPlayerActorId, applyCooldown: session.Phase == DeadPlayerPhoneSessionPhase.Talking);
            }
        }

        internal static void ProcessHostTimers(IVroom? room)
        {
            if (!DeadPlayerPhoneResolver.ShouldApplyHost
                || room == null
                || !DeadPlayerPhoneSessions.TryGetActiveSession(out DeadPlayerPhoneSession? session)
                || session == null)
            {
                return;
            }

            long now = DeadPlayerPhoneAccess.GetCurrentTimeMs();
            if (now < session.PhaseEndTimeMs)
            {
                return;
            }

            if (session.Phase == DeadPlayerPhoneSessionPhase.Ringing)
            {
                TryForcePhoneState(room, session.PhoneLevelObjectId, (int)PhoneState.Idle);
                ModLog.Info(Feature, $"Phone ring timed out — phone={session.PhoneLevelObjectId}");
                EndSession(session.DeadPlayerActorId, applyCooldown: false);
                return;
            }

            if (session.Phase == DeadPlayerPhoneSessionPhase.Talking)
            {
                TryForcePhoneHangup(room, session.PhoneLevelObjectId);
                ModLog.Info(Feature, $"Phone talk timed out — phone={session.PhoneLevelObjectId}");
                EndSession(session.DeadPlayerActorId, applyCooldown: true);
            }
        }

        internal static void EndSession(int deadPlayerActorId, bool applyCooldown)
        {
            if (applyCooldown)
            {
                DeadPlayerPhoneSessions.SetCooldown(
                    deadPlayerActorId,
                    DeadPlayerPhoneResolver.CooldownSeconds);
            }

            DeadPlayerPhoneSessions.ClearActiveSession();
        }

        internal static void ClearAll()
        {
            DeadPlayerPhoneSessions.ClearAll();
        }

        private static void TryForcePhoneState(IVroom room, int phoneLevelObjectId, int targetState)
        {
            if (!DeadPlayerPhoneAccess.TryGetLevelObject(room, phoneLevelObjectId, out OccupiedLevelObjectInfo? phoneInfo)
                || phoneInfo == null)
            {
                return;
            }

            if (phoneInfo.CurrentState == targetState)
            {
                return;
            }

            _ = room.HandleLevelObject(0, phoneLevelObjectId, targetState, occupy: false, out _);
        }

        private static void TryForcePhoneHangup(IVroom room, int phoneLevelObjectId)
        {
            if (!DeadPlayerPhoneAccess.TryGetLevelObject(room, phoneLevelObjectId, out OccupiedLevelObjectInfo? phoneInfo)
                || phoneInfo == null)
            {
                return;
            }

            int state = phoneInfo.CurrentState;
            if (state == (int)PhoneState.OnCall)
            {
                _ = room.HandleLevelObject(0, phoneLevelObjectId, (int)PhoneState.Busy, occupy: false, out _);
                return;
            }

            if (state is (int)PhoneState.Busy or (int)PhoneState.BusyWait or (int)PhoneState.Ringing)
            {
                _ = room.HandleLevelObject(0, phoneLevelObjectId, (int)PhoneState.Idle, occupy: false, out _);
            }
        }
    }
}

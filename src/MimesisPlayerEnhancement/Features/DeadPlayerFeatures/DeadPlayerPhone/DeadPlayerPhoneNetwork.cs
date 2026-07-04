using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneNetwork
    {
        private const string Feature = "DeadPlayerFeatures";

        internal static void TryRingPhone(int levelObjectId)
        {
            GameMainBase? main = DeadPlayerPhoneGameAccess.TryGetMain();
            if (main == null)
            {
                DeadPlayerPhoneClient.ClearPendingRingRequest();
                return;
            }

            main.SendPacketWithCallback<UseLevelObjectRes>(
                new UseLevelObjectReq
                {
                    levelObjectID = levelObjectId,
                    state = (int)PhoneState.Ringing,
                    occupy = true,
                },
                res =>
                {
                    DeadPlayerPhoneClient.ClearPendingRingRequest();
                    if (res == null)
                    {
                        ModLog.Warn(Feature, "Ring phone response is null");
                        return;
                    }

                    if (res.errorCode != MsgErrorCode.Success)
                    {
                        ModLog.Debug(Feature, $"Ring phone rejected — error={res.errorCode}");
                        return;
                    }

                    DeadPlayerPhoneLocalState.StartRing(
                        levelObjectId,
                        DeadPlayerPhoneResolver.MaxRingTimeSeconds);

                    PhoneLevelObject? phone = DeadPlayerPhoneAccess.TryFindClientPhone(levelObjectId);
                    if (phone != null)
                    {
                        DeadPlayerPhoneCamera.Enter(phone);
                    }

                    ModLog.Info(Feature, $"Ring phone accepted — levelObject={levelObjectId}");
                },
                DeadPlayerPhoneGameAccess.GetDestroyToken(main));
        }

        internal static void TryEndPhoneInteraction(
            int levelObjectId,
            DeadPlayerPhoneSessionPhase phase)
        {
            GameMainBase? main = DeadPlayerPhoneGameAccess.TryGetMain();
            if (main == null)
            {
                return;
            }

            PhoneState targetState = phase == DeadPlayerPhoneSessionPhase.Talking
                ? PhoneState.Busy
                : PhoneState.Idle;

            main.SendPacketWithCallback<UseLevelObjectRes>(
                new UseLevelObjectReq
                {
                    levelObjectID = levelObjectId,
                    state = (int)targetState,
                    occupy = false,
                },
                res =>
                {
                    if (res?.errorCode != MsgErrorCode.Success)
                    {
                        ModLog.Debug(
                            Feature,
                            $"End phone rejected — levelObject={levelObjectId}, error={res?.errorCode}");
                    }
                },
                DeadPlayerPhoneGameAccess.GetDestroyToken(main));
        }
    }
}

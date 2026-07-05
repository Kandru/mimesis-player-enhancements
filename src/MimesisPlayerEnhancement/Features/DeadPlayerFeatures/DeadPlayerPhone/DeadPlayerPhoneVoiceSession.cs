namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    /// <summary>
    /// Coordinates mod phone-call voice lifecycle across all clients.
    /// </summary>
    internal static class DeadPlayerPhoneVoiceSession
    {
        private const string Feature = "DeadPlayerFeatures";

        private static readonly Dictionary<int, int> RingInitiatorActorIdByPhoneId = [];

        private static bool _proximitySuppressed;

        private static string? _suppressedDeadPlayerId;

        private static float _suppressedPreviousVolume;

        internal static int PhoneLevelObjectId { get; private set; }

        internal static int DeadCallerActorId { get; private set; }

        internal static int AnswererActorId { get; private set; }

        internal static bool IsModCallActive => PhoneLevelObjectId > 0 && DeadCallerActorId > 0;

        internal static void SetRingInitiator(int phoneLevelObjectId, int actorId)
        {
            if (phoneLevelObjectId <= 0 || actorId <= 0)
            {
                return;
            }

            RingInitiatorActorIdByPhoneId[phoneLevelObjectId] = actorId;
        }

        internal static bool TryGetRingInitiator(int phoneLevelObjectId, out int actorId)
        {
            if (RingInitiatorActorIdByPhoneId.TryGetValue(phoneLevelObjectId, out actorId))
            {
                return actorId > 0;
            }

            actorId = 0;
            return false;
        }

        internal static void ClearRingInitiator(int phoneLevelObjectId)
        {
            if (phoneLevelObjectId > 0)
            {
                _ = RingInitiatorActorIdByPhoneId.Remove(phoneLevelObjectId);
            }
        }

        internal static void BeginTalk(
            PhoneLevelObject phone,
            int deadCallerActorId,
            int answererActorId)
        {
            PhoneLevelObjectId = DeadPlayerPhoneClient.GetLevelObjectId(phone);
            DeadCallerActorId = deadCallerActorId;
            AnswererActorId = answererActorId;

            ApplyProximitySuppressionOnce();
            StartAnswererRelay(phone);
        }

        internal static void StartDeadCallerVoice(PhoneLevelObject phone)
        {
            GameMainBase? main = DeadPlayerPhoneGameAccess.TryGetMain();
            if (main == null || !IsModCallActive)
            {
                return;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null)
            {
                return;
            }

            ProtoActor? myAvatar = main.GetMyAvatar();
            ProtoActor? deadCaller = main.GetActorByActorID(DeadCallerActorId);
            if (myAvatar == null || deadCaller == null || !myAvatar.dead
                || pdata.MyActorID != DeadCallerActorId)
            {
                return;
            }

            DeadPlayerPhoneVoice.StartDeadCallerChannels();

            ProtoActor? answerer = main.GetActorByActorID(AnswererActorId);
            if (answerer != null
                && DeadPlayerPhoneVoice.ConnectRelayToPlayerUid(phone, answerer.UID, answerer))
            {
                ModLog.Info(Feature, $"Dead caller incoming relay connected — answerer={AnswererActorId}");
            }
            else
            {
                ModLog.Warn(Feature, $"Dead caller incoming relay failed — answerer={AnswererActorId}");
            }
        }

        internal static void End()
        {
            if (!IsModCallActive && !_proximitySuppressed
                && !DeadPlayerPhoneVoice.IsModRelayConnected)
            {
                return;
            }

            try
            {
                DeadPlayerPhoneVoice.EndDeadCallerChannels();
                if (IsModCallActive)
                {
                    DeadPlayerPhoneVoice.DisconnectModRelayIfActive();
                }
            }
            finally
            {
                RestoreProximitySuppression();
                PhoneLevelObjectId = 0;
                DeadCallerActorId = 0;
                AnswererActorId = 0;
            }
        }

        internal static void ClearAll()
        {
            End();
            RingInitiatorActorIdByPhoneId.Clear();
        }

        private static void StartAnswererRelay(PhoneLevelObject phone)
        {
            GameMainBase? main = DeadPlayerPhoneGameAccess.TryGetMain();
            if (main == null || AnswererActorId <= 0 || DeadCallerActorId <= 0)
            {
                return;
            }

            ProtoActor? myAvatar = main.GetMyAvatar();
            if (myAvatar == null || myAvatar.dead || myAvatar.ActorID != AnswererActorId)
            {
                return;
            }

            ProtoActor? deadCaller = main.GetActorByActorID(DeadCallerActorId);
            if (deadCaller == null || !deadCaller.dead)
            {
                ModLog.Warn(Feature, $"Answerer relay skipped — dead caller {DeadCallerActorId} not found or alive");
                return;
            }

            if (DeadPlayerPhoneVoice.ConnectRelayToPlayerUid(phone, deadCaller.UID, deadCaller))
            {
                ModLog.Info(Feature, $"Answerer relay connected — deadCaller={DeadCallerActorId}");
            }
            else
            {
                ModLog.Warn(Feature, $"Answerer relay failed — deadCaller={DeadCallerActorId}");
            }
        }

        private static void ApplyProximitySuppressionOnce()
        {
            if (_proximitySuppressed || !IsModCallActive)
            {
                return;
            }

            GameMainBase? main = DeadPlayerPhoneGameAccess.TryGetMain();
            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            if (main == null || voiceman == null)
            {
                return;
            }

            ProtoActor? myAvatar = main.GetMyAvatar();
            ProtoActor? deadCaller = main.GetActorByActorID(DeadCallerActorId);
            if (myAvatar == null || myAvatar.dead || deadCaller == null || !deadCaller.dead)
            {
                return;
            }

            if (myAvatar.ActorID == AnswererActorId)
            {
                return;
            }

            if (!DeadPlayerPhoneVoice.TryResolveDissonancePlayerId(voiceman, deadCaller.UID, out string deadPlayerId))
            {
                return;
            }

            _suppressedPreviousVolume = DeadPlayerPhoneVoice.TryGetPlayerVolume(voiceman, deadPlayerId, out float volume)
                ? volume
                : 1f;
            DeadPlayerPhoneVoice.SetPlayerVolume(voiceman, deadPlayerId, 0f);
            _suppressedDeadPlayerId = deadPlayerId;
            _proximitySuppressed = true;
            ModLog.Debug(Feature, $"Proximity suppressed for dead caller — listener={myAvatar.ActorID}");
        }

        private static void RestoreProximitySuppression()
        {
            if (!_proximitySuppressed || string.IsNullOrEmpty(_suppressedDeadPlayerId))
            {
                _proximitySuppressed = false;
                _suppressedDeadPlayerId = null;
                return;
            }

            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            if (voiceman != null)
            {
                DeadPlayerPhoneVoice.SetPlayerVolume(voiceman, _suppressedDeadPlayerId, _suppressedPreviousVolume);
            }

            _proximitySuppressed = false;
            _suppressedDeadPlayerId = null;
            _suppressedPreviousVolume = 1f;
        }
    }
}

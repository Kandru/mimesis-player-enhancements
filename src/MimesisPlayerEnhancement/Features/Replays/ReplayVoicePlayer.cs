using System.Reflection;
using ReluReplay.Data;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplayVoicePlayer
    {
        private const string Feature = "Replays";

        private static readonly MethodInfo? CreateAudioClipMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "CreateAudioClip");

        private static readonly MethodInfo? PlayVoiceOnActorMethod =
            AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.PlayVoiceOnActor));

        internal static void PlayVoiceEvent(ReplayData replayData, int voiceIndex, bool skipDuringFastForward)
        {
            if (skipDuringFastForward)
            {
                return;
            }

            SndWithTime? snd = replayData.GetVoiceDataByIndex(voiceIndex);
            if (snd == null)
            {
                return;
            }

            try
            {
                if (!Hub.TryGetMain(out GameMainBase? main) || main == null)
                {
                    return;
                }

                ProtoActor? actor = main.GetActorByActorID(snd.ActorID);
                if (actor == null)
                {
                    return;
                }

                SpeechEvent? speechEvent = snd.SpeechEvent;
                if (speechEvent == null)
                {
                    return;
                }

                object? clip = CreateClip(speechEvent);
                if (clip == null || PlayVoiceOnActorMethod == null)
                {
                    return;
                }

                bool isMimic = actor.IsMimic();
                PlayVoiceOnActorMethod.Invoke(
                    actor,
                    [
                        clip,
                        false,
                        isMimic,
                        ReplayGameAccess.TryGetCameraManager()?.IsSpectatorMode == true,
                    ]);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Voice playback failed — actor={snd.ActorID}, {ex.Message}");
            }
        }

        private static object? CreateClip(SpeechEvent speechEvent)
        {
            SpeechEventArchive? archive = ReplayGameAccess.TryGetSpeechEventArchive();
            if (archive == null || CreateAudioClipMethod == null)
            {
                return null;
            }

            return CreateAudioClipMethod.Invoke(archive, [speechEvent]);
        }
    }
}

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class VoiceMimicActorCache
    {
        private const float CacheTtlSec = 1f;

        private static readonly Dictionary<int, ProtoActor> _mimics = [];
        private static float _builtAtTick = -1f;

        internal static void Clear()
        {
            _mimics.Clear();
            _builtAtTick = -1f;
        }

        internal static Dictionary<int, ProtoActor> GetMimicActors()
        {
            float now = GameSessionAccess.GetCurrentTickSec();
            if (_builtAtTick >= 0f && now - _builtAtTick < CacheTtlSec)
            {
                return _mimics;
            }

            _mimics.Clear();
            GameMainBase? main = Hub.Main;
            if (main != null)
            {
                foreach (KeyValuePair<int, ProtoActor> entry in main.GetProtoActorMap())
                {
                    ProtoActor actor = entry.Value;
                    if (actor == null || actor.ActorType != ReluProtocol.Enum.ActorType.Monster || !actor.IsMimic())
                    {
                        continue;
                    }

                    _mimics[entry.Key] = actor;
                }
            }

            _builtAtTick = now;
            return _mimics;
        }
    }
}

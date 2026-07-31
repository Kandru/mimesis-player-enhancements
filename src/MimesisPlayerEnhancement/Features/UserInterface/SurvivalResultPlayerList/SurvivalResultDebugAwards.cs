using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.UserInterface.SurvivalResultPlayerList
{
    /// <summary>Award pool for the debug-only survival result preview.</summary>
    internal static class SurvivalResultDebugAwards
    {
        private static readonly AwardType[] Pool =
        [
            AwardType.None,
            AwardType.BestCarryItem,
            AwardType.BestDamageToAlly,
            AwardType.BestMimicEncounter,
            AwardType.BestCamper,
        ];

        internal static int PoolSize => Pool.Length;

        internal static AwardType Resolve(int roll) => Pool[Math.Abs(roll % Pool.Length)];
    }
}

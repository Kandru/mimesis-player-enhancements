namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/GameSessionInfo.cs:L295-302
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.CanEnterSession))]
    internal static class GameSessionInfoCanEnterSessionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameSessionInfo __instance, ref bool __result)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            __result = JoinAnytimeSessionAdmission.ResolveCanEnter(
                (int)__instance.GameSessionState,
                JoinAnytimeRoomTools.AreJoinsOpen());
            return false;
        }
    }
}

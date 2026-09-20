namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitMovementLock.Patches
{
    // game@0.3.2 Assembly-CSharp/GameMainBase.cs:L2380-2386
    [HarmonyPatch(typeof(GameMainBase), "SetEnableInputForMyAvatar")]
    internal static class GameMainBaseSetEnableInputForMyAvatarMovementLockPatch
    {
        private const string Feature = "Ui";

        private static bool Prefix(GameMainBase __instance)
        {
            try
            {
                if (LoadingWaitMovementLockRuntime.TryDeferUnlock(__instance))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetEnableInputForMyAvatar movement lock patch failed — {ex.Message}");
            }

            return true;
        }
    }

    // game@0.3.2 Assembly-CSharp/GameMainBase.cs:L1221-1231
    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.EndSceneLoading))]
    internal static class GameMainBaseEndSceneLoadingMovementLockPatch
    {
        private const string Feature = "Ui";

        private static void Postfix(GameMainBase __instance)
        {
            try
            {
                LoadingWaitMovementLockRuntime.TryReleaseUnlock(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"EndSceneLoading movement lock patch failed — {ex.Message}");
            }
        }
    }
}

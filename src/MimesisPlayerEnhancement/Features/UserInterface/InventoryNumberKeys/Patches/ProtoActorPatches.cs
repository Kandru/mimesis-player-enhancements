using System.Reflection;
using Mimic.InputSystem;

namespace MimesisPlayerEnhancement.Features.UserInterface.InventoryNumberKeys.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor.cs:L5183-5191
    [HarmonyPatch(typeof(ProtoActor), "ProcessEmoteKey")]
    internal static class ProtoActorProcessEmoteKeyPrefix
    {
        private const string Feature = "Ui";

        private static readonly BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo EmotePlayerField =
            AccessTools.Field(typeof(ProtoActor), "emotePlayer")
            ?? throw new InvalidOperationException("ProtoActor.emotePlayer field not found");

        private static readonly PropertyInfo InputManagerProperty =
            AccessTools.Property(typeof(ProtoActor), "inputman")
            ?? throw new InvalidOperationException("ProtoActor.inputman property not found");

        private static readonly MethodInfo TryStopEmoteByInputMethod =
            AccessTools.Method(typeof(ProtoActor).GetNestedType("EmotePlayer", InstanceFlags)!, "TryStopEmoteByInput")
            ?? throw new InvalidOperationException("EmotePlayer.TryStopEmoteByInput not found");

        [HarmonyPrefix]
        private static bool Prefix(ProtoActor __instance)
        {
            if (!InventoryNumberKeyHotkeys.IsEnabled)
            {
                return true;
            }

            try
            {
                object? emotePlayer = EmotePlayerField.GetValue(__instance);
                if (emotePlayer == null)
                {
                    return true;
                }

                if (TryStopEmoteByInputMethod.Invoke(emotePlayer, null) is true)
                {
                    return false;
                }

                InputManager? inputman = InputManagerProperty.GetValue(__instance) as InputManager;
                if (InventoryNumberKeyHotkeys.ShouldSuppressPhysicalEmote(inputman))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Inventory number key emote suppression failed — {ex.Message}");
            }

            return true;
        }
    }

    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor.cs:L5193-5208
    [HarmonyPatch(typeof(ProtoActor), "ProcessUISelectKey")]
    internal static class ProtoActorProcessUiSelectKeyPrefix
    {
        private const string Feature = "Ui";

        private static readonly PropertyInfo InputManagerProperty =
            AccessTools.Property(typeof(ProtoActor), "inputman")
            ?? throw new InvalidOperationException("ProtoActor.inputman property not found");

        private static readonly PropertyInfo MainProperty =
            AccessTools.Property(typeof(ProtoActor), "main")
            ?? throw new InvalidOperationException("ProtoActor.main property not found");

        [HarmonyPrefix]
        private static void Prefix(ProtoActor __instance)
        {
            if (!InventoryNumberKeyHotkeys.IsEnabled)
            {
                return;
            }

            try
            {
                InputManager? inputman = InputManagerProperty.GetValue(__instance) as InputManager;
                if (!InventoryNumberKeyHotkeys.HasPhysicalHotkeyPress(inputman))
                {
                    return;
                }

                if (MainProperty.GetValue(__instance) is GameMainBase main)
                {
                    main.TryPerformInteractionEnd();
                }

                InventoryNumberKeyHotkeys.SelectInventorySlotsForPhysicalPresses(__instance, inputman);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Inventory number key slot selection failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor.cs:L5193-5208
    [HarmonyPatch(typeof(ProtoActor), "ProcessUISelectKey")]
    internal static class ProtoActorProcessUiSelectKeyPostfix
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            InventoryNumberKeyHotkeys.ClearSyntheticPresses();
        }
    }
}

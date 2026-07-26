using Mimic.InputSystem;

namespace MimesisPlayerEnhancement.Features.UserInterface.InventoryNumberKeys
{
    internal static class InventoryNumberKeyHotkeys
    {
        private static readonly InputAction[] HotkeyActions =
        [
            InputAction.Emote01,
            InputAction.Emote02,
            InputAction.Emote03,
            InputAction.Emote04,
        ];

        private static readonly HashSet<InputAction> SyntheticPresses = [];

        internal static bool IsEnabled =>
            ModConfig.EnableInventoryNumberKeySelection?.Value == true;

        internal static void MarkSyntheticPress(InputAction action)
        {
            if (TryMapToSlot(action, out _))
            {
                SyntheticPresses.Add(action);
            }
        }

        internal static bool IsSyntheticPress(InputAction action) =>
            SyntheticPresses.Contains(action);

        internal static void ClearSyntheticPresses() => SyntheticPresses.Clear();

        internal static bool TryMapToSlot(InputAction action, out int slotIndex)
        {
            switch (action)
            {
                case InputAction.Emote01:
                    slotIndex = 0;
                    return true;
                case InputAction.Emote02:
                    slotIndex = 1;
                    return true;
                case InputAction.Emote03:
                    slotIndex = 2;
                    return true;
                case InputAction.Emote04:
                    slotIndex = 3;
                    return true;
                default:
                    slotIndex = -1;
                    return false;
            }
        }

        internal static bool ShouldSuppressPhysicalEmote(InputManager? inputman)
        {
            if (!IsEnabled || inputman == null)
            {
                return false;
            }

            for (int i = 0; i < HotkeyActions.Length; i++)
            {
                InputAction action = HotkeyActions[i];
                if (inputman.wasPressedThisFrame(action) && !IsSyntheticPress(action))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasPhysicalHotkeyPress(InputManager? inputman)
        {
            if (!IsEnabled || inputman == null)
            {
                return false;
            }

            for (int i = 0; i < HotkeyActions.Length; i++)
            {
                InputAction action = HotkeyActions[i];
                if (inputman.wasPressedThisFrame(action) && !IsSyntheticPress(action))
                {
                    return true;
                }
            }

            return false;
        }

        internal static void SelectInventorySlotsForPhysicalPresses(ProtoActor actor, InputManager? inputman)
        {
            if (!IsEnabled || inputman == null || !actor.AmIAvatar())
            {
                return;
            }

            if (!ProtoActorInventoryAccess.TryGetInventory(actor, out object? inventory) || inventory == null)
            {
                return;
            }

            for (int i = 0; i < HotkeyActions.Length; i++)
            {
                InputAction action = HotkeyActions[i];
                if (!inputman.wasPressedThisFrame(action) || IsSyntheticPress(action))
                {
                    continue;
                }

                if (!TryMapToSlot(action, out int slotIndex))
                {
                    continue;
                }

                ProtoActorInventoryAccess.SelectSlot(inventory, slotIndex);
            }
        }
    }
}

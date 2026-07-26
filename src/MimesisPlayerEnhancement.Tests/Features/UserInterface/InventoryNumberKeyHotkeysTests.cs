using MimesisPlayerEnhancement.Features.UserInterface.InventoryNumberKeys;
using Mimic.InputSystem;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class InventoryNumberKeyHotkeysTests
    {
        [Theory]
        [InlineData(InputAction.Emote01, 0)]
        [InlineData(InputAction.Emote02, 1)]
        [InlineData(InputAction.Emote03, 2)]
        [InlineData(InputAction.Emote04, 3)]
        public void TryMapToSlot_maps_emote_actions_to_inventory_slots(InputAction action, int expectedSlot)
        {
            bool mapped = InventoryNumberKeyHotkeys.TryMapToSlot(action, out int slotIndex);

            Assert.True(mapped);
            Assert.Equal(expectedSlot, slotIndex);
        }

        [Theory]
        [InlineData(InputAction.Emote05)]
        [InlineData(InputAction.EmotePanel)]
        [InlineData(InputAction.UI_PREV)]
        public void TryMapToSlot_returns_false_for_non_hotkey_actions(InputAction action)
        {
            bool mapped = InventoryNumberKeyHotkeys.TryMapToSlot(action, out int slotIndex);

            Assert.False(mapped);
            Assert.Equal(-1, slotIndex);
        }

        [Fact]
        public void Synthetic_press_is_latched_and_cleared()
        {
            InventoryNumberKeyHotkeys.ClearSyntheticPresses();

            InventoryNumberKeyHotkeys.MarkSyntheticPress(InputAction.Emote02);

            Assert.True(InventoryNumberKeyHotkeys.IsSyntheticPress(InputAction.Emote02));
            Assert.False(InventoryNumberKeyHotkeys.IsSyntheticPress(InputAction.Emote01));

            InventoryNumberKeyHotkeys.ClearSyntheticPresses();

            Assert.False(InventoryNumberKeyHotkeys.IsSyntheticPress(InputAction.Emote02));
        }

        [Fact]
        public void MarkSyntheticPress_ignores_non_hotkey_actions()
        {
            InventoryNumberKeyHotkeys.ClearSyntheticPresses();

            InventoryNumberKeyHotkeys.MarkSyntheticPress(InputAction.Emote05);

            Assert.False(InventoryNumberKeyHotkeys.IsSyntheticPress(InputAction.Emote05));
        }
    }
}

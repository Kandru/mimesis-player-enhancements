using System.Reflection;
using MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class InventorySlotOptimizationTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void Inventory_nested_type_has_OnUpdateInvenSig()
        {
            using MimesisMetadataContext context = CreateContext();
            Type inventoryType = RequireInventoryType(context);

            MethodInfo? method = inventoryType.GetMethod("OnUpdateInvenSig", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("OnUpdateInvenSig")]
        [InlineData("OnChangeItemLooksSig")]
        [InlineData("SendChangeActiveInvenSlot")]
        [InlineData("SelectSlot")]
        public void Inventory_nested_type_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type inventoryType = RequireInventoryType(context);

            MethodInfo? method = inventoryType.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("slotItems")]
        [InlineData("selectedSlotIndex")]
        [InlineData("owner")]
        [InlineData("lastChangeActiveSlotTime")]
        public void Inventory_nested_type_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type inventoryType = RequireInventoryType(context);

            FieldInfo? field = inventoryType.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void ResolveDisplaySelectedIndex_empty_raw_zero_highlights_trailing_empty()
        {
            // Raw [empty, item, item, item] packs to [item, item, item, empty].
            // Selected empty at 0 must not light packed[0] (the item).
            bool[] rawOccupied = [false, true, true, true];

            int displayIndex = InventorySlotLayout.ResolveDisplaySelectedIndex(rawOccupied, selectedIndex: 0);

            Assert.Equal(3, displayIndex);
        }

        [Fact]
        public void ResolveDisplaySelectedIndex_manual_empty_slot_still_highlights()
        {
            // Raw [item, item, empty, empty], selected empty at 2 → packed index 2.
            bool[] rawOccupied = [true, true, false, false];

            int displayIndex = InventorySlotLayout.ResolveDisplaySelectedIndex(rawOccupied, selectedIndex: 2);

            Assert.Equal(2, displayIndex);
        }

        [Fact]
        public void ResolveDisplaySelectedIndex_maps_occupied_to_packed_index()
        {
            bool[] rawOccupied = [false, true, false, true];

            Assert.Equal(0, InventorySlotLayout.ResolveDisplaySelectedIndex(rawOccupied, selectedIndex: 1));
            Assert.Equal(1, InventorySlotLayout.ResolveDisplaySelectedIndex(rawOccupied, selectedIndex: 3));
        }

        [Fact]
        public void ResolveDisplaySelectedIndex_invalid_index_returns_no_highlight()
        {
            bool[] rawOccupied = [true, true, true, true];

            Assert.Equal(
                InventorySlotLayout.NoSelectionDisplayIndex,
                InventorySlotLayout.ResolveDisplaySelectedIndex(rawOccupied, selectedIndex: -1));
            Assert.Equal(
                InventorySlotLayout.NoSelectionDisplayIndex,
                InventorySlotLayout.ResolveDisplaySelectedIndex(rawOccupied, selectedIndex: 4));
        }

        [Theory]
        [InlineData(1.0f, 1.0f, false)]
        [InlineData(1.0f, 1.5f, true)]
        [InlineData(float.NegativeInfinity, 0f, true)]
        public void DidSlotChangeSend_compares_timestamps(float before, float after, bool expected)
        {
            Assert.Equal(expected, InventorySlotOptimizer.DidSlotChangeSend(before, after));
        }

        [Fact]
        public void FindFirstOccupiedToRight_skips_to_higher_index()
        {
            const int mask = (1 << 0) | (1 << 2) | (1 << 3);
            Assert.Equal(2, InventorySlotOptimizer.FindFirstOccupiedToRight(mask, 0, 4));
            Assert.Equal(3, InventorySlotOptimizer.FindFirstOccupiedToRight(mask, 2, 4));
            Assert.Equal(-1, InventorySlotOptimizer.FindFirstOccupiedToRight(mask, 3, 4));
        }

        [Fact]
        public void FindFirstOccupiedToLeft_wraps_backward()
        {
            const int mask = (1 << 0) | (1 << 3);
            Assert.Equal(0, InventorySlotOptimizer.FindFirstOccupiedToLeft(mask, 3, 4));
        }

        [Fact]
        public void DidSelectedSlotLoseItem_ignores_scroll_to_empty_slot()
        {
            InventorySlotOptimizer.SlotSnapshot before = new()
            {
                IsValid = true,
                SelectedIndex = 2,
                OccupancyMask = (1 << 0) | (1 << 1),
            };
            const int afterMask = (1 << 0) | (1 << 1);

            Assert.False(InventorySlotOptimizer.DidSelectedSlotLoseItem(before, afterMask));
        }

        [Fact]
        public void DidSelectedSlotLoseItem_detects_drop_from_selected_slot()
        {
            InventorySlotOptimizer.SlotSnapshot before = new()
            {
                IsValid = true,
                SelectedIndex = 1,
                OccupancyMask = (1 << 0) | (1 << 1),
            };
            const int afterMask = 1 << 0;

            Assert.True(InventorySlotOptimizer.DidSelectedSlotLoseItem(before, afterMask));
        }

        [Theory]
        [InlineData("Always", true, true)]
        [InlineData("Always", false, true)]
        [InlineData("WeaponsOnly", true, true)]
        [InlineData("WeaponsOnly", false, false)]
        [InlineData("Vanilla", true, false)]
        [InlineData("Vanilla", false, false)]
        public void ShouldSelectPickup_matches_mode(string mode, bool isWeapon, bool expected)
        {
            bool actual = InventorySlotOptimizer.ShouldSelectPickup(mode, isWeapon);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsSlotOccupied_reads_mask_bits()
        {
            const int mask = (1 << 1) | (1 << 3);
            Assert.True(InventorySlotOptimizer.IsSlotOccupied(mask, 1));
            Assert.False(InventorySlotOptimizer.IsSlotOccupied(mask, 2));
        }

        private static Type RequireInventoryType(MimesisMetadataContext context)
        {
            Type protoActor = context.RequireType("ProtoActor");
            Type? nested = protoActor.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(candidate => string.Equals(candidate.Name, "Inventory", StringComparison.Ordinal));

            return nested ?? throw new InvalidOperationException("Nested type not found: ProtoActor+Inventory");
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}

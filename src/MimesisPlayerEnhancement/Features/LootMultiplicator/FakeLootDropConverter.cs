using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    /// <summary>
    /// Mimics and other AI actors carry fake items (decoys). On death those are dropped with
    /// <see cref="ReasonOfSpawn.ActorDying"/> but still marked fake — pickup destroys them
    /// (<c>MsgErrorCode.FakeItemRemoved</c>). Monster drop-table loot already uses real items.
    /// </summary>
    internal static class FakeLootDropConverter
    {
        internal static void TryConvertActorDyingDrop(IVroom? vroom, ref ItemElement? element, ReasonOfSpawn reasonOfSpawn)
        {
            if (vroom == null
                || element == null
                || !element.IsFake
                || !reasonOfSpawn.Equals(ReasonOfSpawn.ActorDying))
            {
                return;
            }

            int chancePercent = ModConfig.ConvertFakeActorDyingDropChancePercent.Value;
            if (chancePercent <= 0 || !RollConversion(chancePercent))
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            ItemElement? real = CreateRealCopy(vroom, element);
            if (real == null)
            {
                return;
            }

            element = real;

            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(
                    "LootMultiplicator",
                    $"Converted fake ActorDying drop to real — master={real.ItemMasterID}, type={real.ItemType}, chance={chancePercent}%");
            }
        }

        private static bool RollConversion(int chancePercent)
        {
            if (chancePercent >= 100)
            {
                return true;
            }

            return SimpleRandUtil.Next(0, 10000) < chancePercent * 100;
        }

        private static ItemElement? CreateRealCopy(IVroom vroom, ItemElement template)
        {
            try
            {
                ItemInfo info = template.toItemInfo();
                return vroom.GetNewItemElement(
                    info.itemMasterID,
                    isFake: false,
                    ItemElementStackHelper.GetStackCount(template),
                    info.durability,
                    info.remainGauge,
                    info.price);
            }
            catch
            {
                return null;
            }
        }
    }
}

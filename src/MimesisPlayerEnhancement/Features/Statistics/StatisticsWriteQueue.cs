using System;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsWriteQueue
    {
        private static int _loadedSlotId = -999;
        private static Func<IReadOnlyDictionary<ulong, PlayerStatisticsDocument>>? _allPlayersResolver;

        internal static void Configure(
            int slotId,
            Func<IReadOnlyDictionary<ulong, PlayerStatisticsDocument>> allPlayersResolver)
        {
            _loadedSlotId = slotId;
            _allPlayersResolver = allPlayersResolver;
        }

        internal static void Clear()
        {
            _loadedSlotId = -999;
            _allPlayersResolver = null;
        }

        internal static void SaveLoadedSlot(bool waitForCompletion)
        {
            if (_loadedSlotId < 0 || _allPlayersResolver == null)
            {
                return;
            }

            StatisticsStore.SaveSlot(_loadedSlotId, _allPlayersResolver(), waitForCompletion);
        }

        internal static void FlushAllSync()
        {
            StatisticsTracker.PersistLoadedSlot(waitForCompletion: true);
            StatisticsStore.FlushAllSync();
            BackgroundFileWriteQueue.FlushAllSync();
        }
    }
}

namespace MimesisSeedScanner.Cli.Engine
{
    internal static class OfflineBranchCountHelper
    {
        internal static void Compute(
            BakedFlow flow,
            RandomStream random,
            OfflineDungeon dungeon,
            int[] mainPathBranches)
        {
            switch (flow.BranchMode)
            {
                case BakedBranchMode.Global:
                    ComputeGlobal(flow, random, dungeon, mainPathBranches);
                    break;
                default:
                    ComputeLocal(flow, random, dungeon, mainPathBranches);
                    break;
            }
        }

        private static void ComputeLocal(
            BakedFlow flow,
            RandomStream random,
            OfflineDungeon dungeon,
            int[] mainPathBranches)
        {
            int count = dungeon.MainPathTiles.Count;
            for (int i = 0; i < count; i++)
            {
                OfflineTile tile = dungeon.MainPathTiles[i];
                int unused = CountUnusedDoorways(tile);
                if (tile.ArchetypeIndex == null)
                {
                    if (tile.NodeIndex.HasValue && flow.Nodes[tile.NodeIndex.Value].EnableBranching)
                    {
                        mainPathBranches[i] = Math.Min(
                            flow.Nodes[tile.NodeIndex.Value].BranchCount.GetRandom(random),
                            unused);
                    }
                }
                else
                {
                    mainPathBranches[i] = Math.Min(
                        flow.Archetypes[tile.ArchetypeIndex.Value].BranchCount.GetRandom(random),
                        unused);
                }
            }
        }

        private static void ComputeGlobal(
            BakedFlow flow,
            RandomStream random,
            OfflineDungeon dungeon,
            int[] mainPathBranches)
        {
            int total = flow.BranchCount.GetRandom(random);
            int branchable = 0;
            foreach (OfflineTile tile in dungeon.MainPathTiles)
            {
                if ((tile.ArchetypeIndex != null
                     || (tile.NodeIndex.HasValue && flow.Nodes[tile.NodeIndex.Value].EnableBranching))
                    && CountUnusedDoorways(tile) > 0)
                {
                    branchable++;
                }
            }

            if (branchable == 0)
            {
                return;
            }

            float perTile = (float)total / branchable;
            float remainder = perTile;
            int remaining = total;
            int mainPathCount = dungeon.MainPathTiles.Count;
            for (int i = 0; i < mainPathCount && remaining > 0; i++)
            {
                OfflineTile tile = dungeon.MainPathTiles[i];
                bool branchableTile = tile.ArchetypeIndex != null
                                      || (tile.NodeIndex.HasValue && flow.Nodes[tile.NodeIndex.Value].EnableBranching);
                if (!branchableTile || CountUnusedDoorways(tile) == 0)
                {
                    continue;
                }

                int maxUnused = CountUnusedDoorways(tile);
                int maxByArchetype = tile.ArchetypeIndex.HasValue
                    ? flow.Archetypes[tile.ArchetypeIndex.Value].BranchCount.Max
                    : tile.NodeIndex.HasValue && flow.Nodes[tile.NodeIndex.Value].EnableBranching
                        ? flow.Nodes[tile.NodeIndex.Value].BranchCount.Max
                        : 0;
                int count = (int)Math.Floor(remainder);
                count = Math.Min(count, maxUnused);
                count = Math.Min(count, maxByArchetype);
                count = Math.Min(count, remaining);
                if (count < maxUnused && count < remaining && random.NextDouble() < remainder - count)
                {
                    count++;
                }

                remainder -= count;
                remainder += perTile;
                remaining -= count;
                mainPathBranches[i] = count;
            }
        }

        private static int CountUnusedDoorways(OfflineTile tile)
        {
            int count = 0;
            foreach (OfflineDoorway doorway in tile.Doorways)
            {
                if (!doorway.Used && !doorway.IsDisabled)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

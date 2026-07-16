namespace MimesisSeedScanner.Cli.Engine
{
    internal sealed class GenerationScratch
    {
        internal readonly List<int> VisitedNodes = [];
        internal readonly List<List<int>> TileSetsPerStep = [];
        internal readonly List<int?> ArchetypeIndicesPerStep = [];
        internal readonly List<int?> NodesPerStep = [];
        internal readonly List<int?> LinesPerStep = [];
        internal readonly List<int> UsedArchetypeIndices = [];
        internal readonly List<int> TileSetScratch = [];
        internal readonly List<OfflineWeightedTile> WeightScratch = [];
        internal readonly List<OfflineDoorwayPair> PairScratch = [];
        internal readonly List<PendingInjection> PendingInjections = [];
        internal readonly Dictionary<OfflineTile, PendingInjection> InjectedTiles = [];
        internal readonly List<OfflineDoorway> DoorwayScratch = [];
        internal readonly Stack<OfflineTile> PruneStack = [];
        internal readonly List<int> ArchetypePickScratch = [];
        internal int[] BranchCounts = [];

        internal void ClearStepPlanning()
        {
            VisitedNodes.Clear();
            TileSetsPerStep.Clear();
            ArchetypeIndicesPerStep.Clear();
            NodesPerStep.Clear();
            LinesPerStep.Clear();
            UsedArchetypeIndices.Clear();
        }

        internal void EnsureBranchCounts(int count)
        {
            if (BranchCounts.Length != count)
            {
                BranchCounts = new int[count];
            }
            else
            {
                Array.Clear(BranchCounts, 0, count);
            }
        }
    }

    internal static class OfflineDungeonGenerator
    {
        private const float OverlapThreshold = 0.01f;
        private const float Padding = 0f;
        private const int MaxAttemptCount = 20;

        [ThreadStatic]
        private static GenerationScratch? _scratch;

        private static GenerationScratch Scratch => _scratch ??= new GenerationScratch();

        internal static bool TryGenerateMetrics(
            ScanCatalog catalog,
            BakedFlow flow,
            int seed,
            out GenerationMetrics metrics)
        {
            metrics = default;
            int totalRetries = 0;
            var random = new RandomStream(seed);

            for (int attempt = 0; attempt <= MaxAttemptCount; attempt++)
            {
                if (attempt > 0)
                {
                    int chosenSeed = random.Next();
                    random = new RandomStream(chosenSeed);
                    totalRetries++;
                }

                if (TryGenerateOnce(catalog, flow, seed, random, out OfflineDungeon dungeon))
                {
                    metrics = BuildMetrics(dungeon, totalRetries);
                    return true;
                }
            }

            return false;
        }

        private static bool TryGenerateOnce(
            ScanCatalog catalog,
            BakedFlow flow,
            int seed,
            RandomStream random,
            out OfflineDungeon dungeon)
        {
            dungeon = new OfflineDungeon(catalog);
            GenerationScratch scratch = Scratch;
            scratch.PendingInjections.Clear();
            scratch.InjectedTiles.Clear();
            GatherTilesToInject(flow, seed, scratch);

            int targetLength = Math.Max(2, (int)Math.Round(flow.Length.GetRandom(random) * flow.LengthMultiplier));
            if (!TryGenerateMainPath(flow, dungeon, random, ref targetLength, scratch))
            {
                return false;
            }

            GenerateBranchPaths(flow, dungeon, random, scratch);
            PruneBranches(flow, dungeon, scratch);
            ConnectOverlappingDoorways(flow, dungeon, random, scratch);
            return dungeon.AllTiles.Count > 0;
        }

        private static void GatherTilesToInject(BakedFlow flow, int seed, GenerationScratch scratch)
        {
            var injectionRandom = new RandomStream(seed);
            foreach (BakedInjectionRule rule in flow.InjectionRules)
            {
                if (rule.TileSetIndex < 0 || rule.TileSetIndex >= flow.TileSets.Count)
                {
                    continue;
                }

                bool isOnMainPath = !rule.CanAppearOnBranchPath
                                    || (rule.CanAppearOnMainPath && injectionRandom.NextDouble() > 0.5);
                scratch.PendingInjections.Add(new PendingInjection
                {
                    TileSetIndex = rule.TileSetIndex,
                    NormalizedPathDepth = rule.NormalizedPathDepth.GetRandom(injectionRandom),
                    NormalizedBranchDepth = rule.NormalizedBranchDepth.GetRandom(injectionRandom),
                    IsOnMainPath = isOnMainPath,
                });
            }
        }

        private static bool TryGenerateMainPath(
            BakedFlow flow,
            OfflineDungeon dungeon,
            RandomStream random,
            ref int targetLength,
            GenerationScratch scratch)
        {
            scratch.ClearStepPlanning();
            int? currentArchetypeIndex = null;
            int? previousLineIndex = null;

            for (int step = 0; step < targetLength; step++)
            {
                float normalizedDepth = targetLength <= 1 ? 0f : (float)step / (targetLength - 1);
                int? lineIndex = GetLineIndexAtDepth(flow, normalizedDepth);
                if (lineIndex == null)
                {
                    return false;
                }

                if (lineIndex != previousLineIndex)
                {
                    currentArchetypeIndex = PickRandomArchetypeIndex(flow, lineIndex.Value, random, scratch.UsedArchetypeIndices, scratch.ArchetypePickScratch);
                    previousLineIndex = lineIndex;
                }

                int? nodeIndex = PickGraphNode(flow, normalizedDepth, scratch.VisitedNodes);
                if (nodeIndex.HasValue)
                {
                    scratch.TileSetsPerStep.Add(new List<int>(flow.Nodes[nodeIndex.Value].TileSetIndices));
                    scratch.ArchetypeIndicesPerStep.Add(null);
                    scratch.NodesPerStep.Add(nodeIndex);
                    scratch.LinesPerStep.Add(null);
                }
                else
                {
                    scratch.TileSetScratch.Clear();
                    if (currentArchetypeIndex.HasValue)
                    {
                        scratch.TileSetScratch.AddRange(flow.Archetypes[currentArchetypeIndex.Value].TileSetIndices);
                    }

                    scratch.TileSetsPerStep.Add(new List<int>(scratch.TileSetScratch));
                    scratch.ArchetypeIndicesPerStep.Add(currentArchetypeIndex);
                    scratch.NodesPerStep.Add(null);
                    scratch.LinesPerStep.Add(lineIndex);
                    if (currentArchetypeIndex.HasValue)
                    {
                        scratch.UsedArchetypeIndices.Add(currentArchetypeIndex.Value);
                    }
                }
            }

            int tileRetryCount = 0;
            int totalForLoopRetryCount = 0;
            for (int i = 0; i < scratch.TileSetsPerStep.Count; i++)
            {
                OfflineTile? previous = dungeon.MainPathTiles.Count > 0
                    ? dungeon.MainPathTiles[^1]
                    : null;
                float normalizedDepth = scratch.TileSetsPerStep.Count <= 1
                    ? 0f
                    : (float)i / (scratch.TileSetsPerStep.Count - 1);
                OfflineTile? placed = TryAddTile(
                    flow,
                    dungeon,
                    random,
                    previous,
                    scratch.TileSetsPerStep[i],
                    normalizedDepth,
                    scratch.ArchetypeIndicesPerStep[i],
                    scratch.NodesPerStep[i],
                    scratch.LinesPerStep[i],
                    isMainPath: true,
                    scratch);
                if (i > 5 && placed == null && tileRetryCount < 5 && totalForLoopRetryCount < 20)
                {
                    if (dungeon.MainPathTiles.Count > 0)
                    {
                        OfflineTile removed = dungeon.MainPathTiles[^1];
                        if (scratch.InjectedTiles.Remove(removed, out PendingInjection? injection) && injection != null)
                        {
                            scratch.PendingInjections.Add(injection);
                        }

                        dungeon.RemoveLastConnection();
                        dungeon.RemoveLastTile();
                    }

                    i -= 2;
                    tileRetryCount++;
                    totalForLoopRetryCount++;
                    continue;
                }

                if (placed == null)
                {
                    return false;
                }

                placed.PathDepth = previous == null ? 0 : previous.PathDepth + 1;
                tileRetryCount = 0;
            }

            return true;
        }

        private static void GenerateBranchPaths(
            BakedFlow flow,
            OfflineDungeon dungeon,
            RandomStream random,
            GenerationScratch scratch)
        {
            scratch.EnsureBranchCounts(dungeon.MainPathTiles.Count);
            OfflineBranchCountHelper.Compute(flow, random, dungeon, scratch.BranchCounts);
            int branchId = 0;
            for (int mainIndex = 0; mainIndex < dungeon.MainPathTiles.Count; mainIndex++)
            {
                OfflineTile anchor = dungeon.MainPathTiles[mainIndex];
                int branchCount = scratch.BranchCounts[mainIndex];
                bool nodeBranching = anchor.ArchetypeIndex == null
                                     && anchor.NodeIndex.HasValue
                                     && flow.Nodes[anchor.NodeIndex.Value].EnableBranching;
                if ((anchor.ArchetypeIndex == null && !nodeBranching) || branchCount == 0)
                {
                    continue;
                }

                for (int branch = 0; branch < branchCount; branch++)
                {
                    int branchDepth = nodeBranching
                        ? flow.Nodes[anchor.NodeIndex!.Value].BranchingDepth.GetRandom(random)
                        : flow.Archetypes[anchor.ArchetypeIndex!.Value].BranchingDepth.GetRandom(random);
                    OfflineTile previous = anchor;
                    for (int depth = 0; depth < branchDepth; depth++)
                    {
                        scratch.TileSetScratch.Clear();
                        if (nodeBranching)
                        {
                            FillNodeBranchTileSets(flow, anchor.NodeIndex!.Value, depth, branchDepth, scratch.TileSetScratch);
                        }
                        else
                        {
                            FillArchetypeBranchTileSets(flow, anchor.ArchetypeIndex!.Value, depth, branchDepth, scratch.TileSetScratch);
                        }

                        float normalizedBranchDepth = branchDepth <= 1 ? 1f : (float)depth / (branchDepth - 1);
                        OfflineTile? placed = TryAddTile(
                            flow,
                            dungeon,
                            random,
                            previous,
                            scratch.TileSetScratch,
                            normalizedBranchDepth,
                            anchor.ArchetypeIndex,
                            anchor.NodeIndex,
                            anchor.LineIndex,
                            isMainPath: false,
                            scratch);
                        if (placed == null)
                        {
                            break;
                        }

                        placed.PathDepth = previous.PathDepth;
                        placed.BranchDepth = depth;
                        placed.BranchId = branchId;
                        previous = placed;
                    }

                    branchId++;
                }
            }
        }

        private static void PruneBranches(BakedFlow flow, OfflineDungeon dungeon, GenerationScratch scratch)
        {
            if (flow.BranchPruneTags.Count == 0)
            {
                return;
            }

            scratch.PruneStack.Clear();
            foreach (OfflineTile tile in dungeon.BranchPathTiles)
            {
                bool hasDeeperBranch = false;
                foreach (OfflineDoorway doorway in tile.Doorways)
                {
                    if (!doorway.Used || doorway.Connected == null)
                    {
                        continue;
                    }

                    OfflineTile other = doorway.Connected.Owner;
                    if (!other.IsOnMainPath && other.BranchDepth > tile.BranchDepth)
                    {
                        hasDeeperBranch = true;
                        break;
                    }
                }

                if (!hasDeeperBranch)
                {
                    scratch.PruneStack.Push(tile);
                }
            }

            while (scratch.PruneStack.Count > 0)
            {
                OfflineTile tile = scratch.PruneStack.Pop();
                if (tile.IsInjected || !ShouldPruneTile(flow, tile))
                {
                    continue;
                }

                OfflineDoorway? parentDoor = null;
                foreach (OfflineDoorway doorway in tile.Doorways)
                {
                    if (!doorway.Used || doorway.Connected == null)
                    {
                        continue;
                    }

                    OfflineTile other = doorway.Connected.Owner;
                    if (other.IsOnMainPath || other.BranchDepth < tile.BranchDepth)
                    {
                        parentDoor = doorway;
                        break;
                    }
                }

                if (parentDoor?.Connected == null)
                {
                    continue;
                }

                OfflineTile parentTile = parentDoor.Connected.Owner;
                dungeon.RemoveTile(tile);
                if (!parentTile.IsOnMainPath)
                {
                    scratch.PruneStack.Push(parentTile);
                }
            }
        }

        private static bool ShouldPruneTile(BakedFlow flow, OfflineTile tile)
        {
            foreach (string tag in flow.BranchPruneTags)
            {
                if (tile.Template.Tags.Contains(tag))
                {
                    return true;
                }
            }

            return false;
        }

        private static OfflineTile? TryAddTile(
            BakedFlow flow,
            OfflineDungeon dungeon,
            RandomStream random,
            OfflineTile? previous,
            List<int> tileSetIndices,
            float normalizedDepth,
            int? archetypeIndex,
            int? nodeIndex,
            int? lineIndex,
            bool isMainPath,
            GenerationScratch scratch)
        {
            PendingInjection? injection = TryTakeInjection(flow, scratch, isMainPath, normalizedDepth, previous);
            CollectWeights(flow, tileSetIndices, isMainPath, normalizedDepth, injection, scratch.WeightScratch);
            if (scratch.WeightScratch.Count == 0)
            {
                return null;
            }

            OfflineDoorwayPairFinder.FillPairs(
                flow,
                dungeon,
                random,
                previous,
                scratch.WeightScratch,
                isMainPath,
                normalizedDepth,
                archetypeIndex,
                scratch.PairScratch);

            foreach (OfflineDoorwayPair pair in scratch.PairScratch)
            {
                if (TryPlaceTile(flow, dungeon, previous, pair, archetypeIndex, nodeIndex, lineIndex, isMainPath, injection, out OfflineTile? placed))
                {
                    if (injection != null && placed != null)
                    {
                        placed.IsInjected = true;
                        scratch.InjectedTiles[placed] = injection;
                    }

                    return placed;
                }
            }

            return null;
        }

        private static PendingInjection? TryTakeInjection(
            BakedFlow flow,
            GenerationScratch scratch,
            bool isMainPath,
            float normalizedDepth,
            OfflineTile? previous)
        {
            if (scratch.PendingInjections.Count == 0)
            {
                return null;
            }

            bool archetypeNode = isMainPath && previous?.ArchetypeIndex == null && previous?.NodeIndex != null;
            if (archetypeNode)
            {
                return null;
            }

            float pathDepth = isMainPath
                ? normalizedDepth
                : previous == null ? 0f : (float)previous.PathDepth / Math.Max(1, flow.Length.Max - 1);
            float branchDepth = isMainPath ? 0f : normalizedDepth;
            for (int i = 0; i < scratch.PendingInjections.Count; i++)
            {
                PendingInjection candidate = scratch.PendingInjections[i];
                if (candidate.ShouldInject(isMainPath, pathDepth, branchDepth))
                {
                    scratch.PendingInjections.RemoveAt(i);
                    return candidate;
                }
            }

            return null;
        }

        private static bool TryPlaceTile(
            BakedFlow flow,
            OfflineDungeon dungeon,
            OfflineTile? previous,
            OfflineDoorwayPair pair,
            int? archetypeIndex,
            int? nodeIndex,
            int? lineIndex,
            bool isMainPath,
            PendingInjection? injection,
            out OfflineTile? placed)
        {
            placed = new OfflineTile(dungeon.Catalog.Tiles[pair.TileId]);
            placed.ArchetypeIndex = archetypeIndex;
            placed.NodeIndex = nodeIndex;
            placed.LineIndex = lineIndex;
            placed.IsOnMainPath = isMainPath;

            if (previous != null)
            {
                placed.PositionBySocket(placed.Doorways[pair.NextDoorwayIndex], previous.Doorways[pair.PreviousDoorwayIndex]);
                if (IsColliding(dungeon, placed, previous))
                {
                    placed = null;
                    return false;
                }

                dungeon.Connect(previous.Doorways[pair.PreviousDoorwayIndex], placed.Doorways[pair.NextDoorwayIndex]);
            }

            dungeon.AddTile(placed);
            return true;
        }

        private static bool IsColliding(OfflineDungeon dungeon, OfflineTile newTile, OfflineTile previousTile)
        {
            foreach (OfflineTile other in dungeon.AllTiles)
            {
                if (other == newTile)
                {
                    continue;
                }

                float threshold = other == previousTile ? OverlapThreshold : -Padding;
                if (LBounds.AreOverlapping(newTile.WorldBounds, other.WorldBounds, threshold))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConnectOverlappingDoorways(
            BakedFlow flow,
            OfflineDungeon dungeon,
            RandomStream random,
            GenerationScratch scratch)
        {
            scratch.DoorwayScratch.Clear();
            foreach (OfflineTile tile in dungeon.AllTiles)
            {
                scratch.DoorwayScratch.AddRange(tile.Doorways);
            }

            for (int ai = 0; ai < scratch.DoorwayScratch.Count; ai++)
            {
                OfflineDoorway a = scratch.DoorwayScratch[ai];
                for (int bi = ai + 1; bi < scratch.DoorwayScratch.Count; bi++)
                {
                    OfflineDoorway b = scratch.DoorwayScratch[bi];
                    if (a.Used || b.Used || ReferenceEquals(a.Owner, b.Owner))
                    {
                        continue;
                    }

                    if (!CanDoorwaysConnect(flow, dungeon, a, b))
                    {
                        continue;
                    }

                    LVec3 delta = a.WorldPosition - b.WorldPosition;
                    if (delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z >= 1e-5f)
                    {
                        continue;
                    }

                    if (flow.RestrictConnectionToSameSection)
                    {
                        bool sameSection = a.Owner.LineIndex == b.Owner.LineIndex && a.Owner.LineIndex != null;
                        if (!sameSection)
                        {
                            continue;
                        }
                    }

                    float chance = flow.DoorwayConnectionChance;
                    BakedTile tileA = a.Owner.Template;
                    BakedTile tileB = b.Owner.Template;
                    if (tileA.OverrideConnectionChance && tileB.OverrideConnectionChance)
                    {
                        chance = Math.Min(tileA.ConnectionChance, tileB.ConnectionChance);
                    }
                    else if (tileA.OverrideConnectionChance)
                    {
                        chance = tileA.ConnectionChance;
                    }
                    else if (tileB.OverrideConnectionChance)
                    {
                        chance = tileB.ConnectionChance;
                    }

                    if (chance > 0f && random.NextDouble() < chance)
                    {
                        dungeon.Connect(a, b);
                    }
                }
            }
        }

        private static bool CanDoorwaysConnect(BakedFlow flow, OfflineDungeon dungeon, OfflineDoorway a, OfflineDoorway b)
        {
            if (a.IsDisabled || b.IsDisabled)
            {
                return false;
            }

            if (!string.Equals(a.SocketId, b.SocketId, StringComparison.Ordinal))
            {
                return false;
            }

            return CanTilesConnect(flow, dungeon.Catalog.Tiles[a.Owner.Template.Id], dungeon.Catalog.Tiles[b.Owner.Template.Id]);
        }

        private static bool CanTilesConnect(BakedFlow flow, BakedTile a, BakedTile b)
        {
            if (flow.TileConnectionTags.Count == 0)
            {
                return true;
            }

            bool hasMatch = flow.TileConnectionTags.Any(pair =>
                (a.Tags.Contains(pair.TagA) && b.Tags.Contains(pair.TagB))
                || (b.Tags.Contains(pair.TagA) && a.Tags.Contains(pair.TagB)));

            return flow.TileTagConnectionMode == BakedTagConnectionMode.Accept ? hasMatch : !hasMatch;
        }

        private static void CollectWeights(
            BakedFlow flow,
            List<int> tileSetIndices,
            bool isMainPath,
            float normalizedDepth,
            PendingInjection? injection,
            List<OfflineWeightedTile> output)
        {
            output.Clear();
            if (injection != null)
            {
                foreach (BakedWeightedTile weight in flow.TileSets[injection.TileSetIndex].Weights)
                {
                    output.Add(new OfflineWeightedTile(weight.TileId, weight.MainPathWeight));
                }

                return;
            }

            foreach (int tileSetIndex in tileSetIndices)
            {
                foreach (BakedWeightedTile weight in flow.TileSets[tileSetIndex].Weights)
                {
                    float value = (isMainPath ? weight.MainPathWeight : weight.BranchPathWeight)
                                  * LayoutMath.EvaluateDepthWeight(weight.DepthWeights, normalizedDepth);
                    if (value > 0f)
                    {
                        output.Add(new OfflineWeightedTile(weight.TileId, value));
                    }
                }
            }
        }

        private static int? GetLineIndexAtDepth(BakedFlow flow, float normalizedDepth)
        {
            normalizedDepth = LayoutMath.Clamp01(normalizedDepth);
            if (flow.Lines.Count == 0)
            {
                return null;
            }

            if (normalizedDepth <= 0f)
            {
                return 0;
            }

            if (normalizedDepth >= 1f)
            {
                return flow.Lines.Count - 1;
            }

            for (int i = 0; i < flow.Lines.Count; i++)
            {
                BakedGraphLine line = flow.Lines[i];
                if (normalizedDepth >= line.Position && normalizedDepth < line.Position + line.Length)
                {
                    return i;
                }
            }

            return null;
        }

        private static int? PickGraphNode(BakedFlow flow, float normalizedDepth, List<int> visitedNodes)
        {
            for (int i = 0; i < flow.Nodes.Count; i++)
            {
                if (normalizedDepth >= flow.Nodes[i].Position && !visitedNodes.Contains(i))
                {
                    visitedNodes.Add(i);
                    return i;
                }
            }

            return null;
        }

        private static int? PickRandomArchetypeIndex(
            BakedFlow flow,
            int lineIndex,
            RandomStream random,
            List<int> usedArchetypeIndices,
            List<int> scratchPool)
        {
            List<int> candidates = flow.Lines[lineIndex].ArchetypeIndices;
            if (candidates.Count == 0)
            {
                return null;
            }

            scratchPool.Clear();
            foreach (int index in candidates)
            {
                if (!flow.Archetypes[index].Unique || !usedArchetypeIndices.Contains(index))
                {
                    scratchPool.Add(index);
                }
            }

            if (scratchPool.Count == 0)
            {
                scratchPool.AddRange(candidates);
            }

            return scratchPool[random.Next(0, scratchPool.Count)];
        }

        private static void FillArchetypeBranchTileSets(
            BakedFlow flow,
            int archetypeIndex,
            int depthIndex,
            int totalDepth,
            List<int> output)
        {
            output.Clear();
            BakedArchetype archetype = flow.Archetypes[archetypeIndex];
            if (depthIndex == 0 && archetype.BranchStartTileSetIndices.Count > 0)
            {
                if (archetype.BranchStartType == BakedBranchCapType.InsteadOf)
                {
                    output.AddRange(archetype.BranchStartTileSetIndices);
                }
                else
                {
                    output.AddRange(archetype.TileSetIndices);
                    output.AddRange(archetype.BranchStartTileSetIndices);
                }

                return;
            }

            if (depthIndex == totalDepth - 1 && archetype.BranchCapTileSetIndices.Count > 0)
            {
                if (archetype.BranchCapType == BakedBranchCapType.InsteadOf)
                {
                    output.AddRange(archetype.BranchCapTileSetIndices);
                }
                else
                {
                    output.AddRange(archetype.TileSetIndices);
                    output.AddRange(archetype.BranchCapTileSetIndices);
                }

                return;
            }

            output.AddRange(archetype.TileSetIndices);
        }

        private static void FillNodeBranchTileSets(
            BakedFlow flow,
            int nodeIndex,
            int depthIndex,
            int totalDepth,
            List<int> output)
        {
            output.Clear();
            BakedGraphNode node = flow.Nodes[nodeIndex];
            if (depthIndex == 0 && node.BranchStartTileSetIndices.Count > 0)
            {
                if (node.BranchStartType == BakedBranchCapType.InsteadOf)
                {
                    output.AddRange(node.BranchStartTileSetIndices);
                }
                else
                {
                    output.AddRange(node.BranchTileSetIndices);
                    output.AddRange(node.BranchStartTileSetIndices);
                }

                return;
            }

            if (depthIndex == totalDepth - 1 && node.BranchCapTileSetIndices.Count > 0)
            {
                if (node.BranchCapType == BakedBranchCapType.InsteadOf)
                {
                    output.AddRange(node.BranchCapTileSetIndices);
                }
                else
                {
                    output.AddRange(node.BranchTileSetIndices);
                    output.AddRange(node.BranchCapTileSetIndices);
                }

                return;
            }

            output.AddRange(node.BranchTileSetIndices);
        }

        private static GenerationMetrics BuildMetrics(OfflineDungeon dungeon, int totalRetries)
        {
            int unusedDoorways = 0;
            foreach (OfflineTile tile in dungeon.AllTiles)
            {
                foreach (OfflineDoorway doorway in tile.Doorways)
                {
                    if (!doorway.Used)
                    {
                        unusedDoorways++;
                    }
                }
            }

            LBounds bounds = default;
            bool hasBounds = false;
            int maxBranchDepth = 0;
            foreach (OfflineTile tile in dungeon.AllTiles)
            {
                if (!hasBounds)
                {
                    bounds = tile.WorldBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(tile.WorldBounds);
                }

                if (tile.BranchDepth > maxBranchDepth)
                {
                    maxBranchDepth = tile.BranchDepth;
                }
            }

            if (!hasBounds)
            {
                bounds = new LBounds(LVec3.Zero, new LVec3(1f, 1f, 1f));
            }

            float volume = Math.Max(1f, bounds.Size.X * bounds.Size.Y * bounds.Size.Z);
            return new GenerationMetrics(
                dungeon.MainPathTiles.Count,
                dungeon.BranchPathTiles.Count,
                dungeon.AllTiles.Count,
                maxBranchDepth,
                totalRetries,
                dungeon.Connections.Count,
                volume,
                unusedDoorways,
                generationFailed: false);
        }
    }
}

namespace MimesisSeedScanner.Cli.Engine
{
    internal static class OfflineDoorwayPairFinder
    {
        internal static void FillPairs(
            BakedFlow flow,
            OfflineDungeon dungeon,
            RandomStream random,
            OfflineTile? previous,
            List<OfflineWeightedTile> weights,
            bool isMainPath,
            float normalizedDepth,
            int? archetypeIndex,
            List<OfflineDoorwayPair> output)
        {
            output.Clear();
            List<OfflineWeightedTile> tileOrder = CalculateOrderedListOfTiles(weights, isMainPath, normalizedDepth, random);
            if (previous == null)
            {
                foreach (OfflineWeightedTile weight in tileOrder)
                {
                    BakedTile template = dungeon.Catalog.Tiles[weight.TileId];
                    float rankWeight = weight.Weight * (float)random.NextDouble();
                    for (int i = 0; i < template.Doorways.Count; i++)
                    {
                        if (template.Doorways[i].IsDisabled)
                        {
                            continue;
                        }

                        float doorwayWeight = (float)random.NextDouble();
                        output.Add(new OfflineDoorwayPair(weight.TileId, -1, template.Doorways[i].Index, rankWeight, doorwayWeight));
                    }
                }
            }
            else
            {
                bool restrictExits = previous.Template.ExitIndices.Count > 0;
                foreach (OfflineDoorway previousDoor in previous.Doorways)
                {
                    if (previousDoor.Used || previousDoor.IsDisabled)
                    {
                        continue;
                    }

                    if (restrictExits && !previous.Template.ExitIndices.Contains(previousDoor.Index))
                    {
                        continue;
                    }

                    foreach (OfflineWeightedTile weight in tileOrder)
                    {
                        if (!IsTileAllowed(dungeon, previous, weight.TileId))
                        {
                            continue;
                        }

                        BakedTile template = dungeon.Catalog.Tiles[weight.TileId];
                        int rank = tileOrder.Count - tileOrder.IndexOf(weight);
                        float rankWeight = rank;
                        bool restrictEntrances = template.EntranceIndices.Count > 0;
                        foreach (BakedDoorway doorway in template.Doorways)
                        {
                            if (doorway.IsDisabled)
                            {
                                continue;
                            }

                            if (restrictEntrances && !template.EntranceIndices.Contains(doorway.Index))
                            {
                                continue;
                            }

                            if (template.ExitIndices.Contains(doorway.Index))
                            {
                                continue;
                            }

                            if (!CanConnect(flow, dungeon, previousDoor, template, doorway, archetypeIndex, isMainPath, random, ref rankWeight))
                            {
                                continue;
                            }

                            float doorwayWeight = (float)random.NextDouble();
                            output.Add(new OfflineDoorwayPair(
                                weight.TileId,
                                previousDoor.Index,
                                doorway.Index,
                                rankWeight,
                                doorwayWeight));
                        }
                    }
                }
            }

            output.Sort(static (a, b) =>
            {
                int cmp = b.TileWeight.CompareTo(a.TileWeight);
                return cmp != 0 ? cmp : b.DoorwayWeight.CompareTo(a.DoorwayWeight);
            });
        }

        private static List<OfflineWeightedTile> CalculateOrderedListOfTiles(
            List<OfflineWeightedTile> weights,
            bool isMainPath,
            float normalizedDepth,
            RandomStream random)
        {
            var remaining = new List<OfflineWeightedTile>(weights);
            var ordered = new List<OfflineWeightedTile>(weights.Count);
            while (remaining.Count > 0)
            {
                float total = 0f;
                foreach (OfflineWeightedTile w in remaining)
                {
                    total += w.Weight;
                }

                if (total <= 0f)
                {
                    break;
                }

                float roll = (float)random.NextDouble() * total;
                float cumulative = 0f;
                int pick = remaining.Count - 1;
                for (int i = 0; i < remaining.Count; i++)
                {
                    cumulative += remaining[i].Weight;
                    if (roll <= cumulative)
                    {
                        pick = i;
                        break;
                    }
                }

                ordered.Add(remaining[pick]);
                remaining.RemoveAt(pick);
            }

            return ordered;
        }

        private static bool IsTileAllowed(OfflineDungeon dungeon, OfflineTile previous, int tileId)
        {
            BakedTile candidate = dungeon.Catalog.Tiles[tileId];
            return candidate.RepeatMode switch
            {
                BakedTileRepeatMode.Allow => true,
                BakedTileRepeatMode.DisallowImmediate => previous.Template.Id != tileId,
                BakedTileRepeatMode.Disallow => !dungeon.AllTiles.Any(tile => tile.Template.Id == tileId),
                _ => true,
            };
        }

        private static bool CanConnect(
            BakedFlow flow,
            OfflineDungeon dungeon,
            OfflineDoorway previousDoor,
            BakedTile nextTemplate,
            BakedDoorway nextDoorway,
            int? archetypeIndex,
            bool isMainPath,
            RandomStream random,
            ref float weight)
        {
            if (!string.Equals(previousDoor.SocketId, nextDoorway.SocketId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!CanTilesConnect(flow, dungeon.Catalog.Tiles[previousDoor.Owner.Template.Id], nextTemplate))
            {
                return false;
            }

            LQuat nextRotation = LQuat.Identity;
            LVec3 nextForward = LQuat.Rotate(LQuat.Multiply(nextRotation, ToLQuat(nextDoorway.LocalRotation)), new LVec3(0f, 0f, 1f));
            bool disallowRotation = !nextTemplate.AllowRotation;
            LVec3? requiredForward = null;
            if (LVec3.Angle(previousDoor.WorldForward, LVec3.Up) < 1f)
            {
                requiredForward = new LVec3(0f, -1f, 0f);
            }
            else if (LVec3.Angle(previousDoor.WorldForward, new LVec3(0f, -1f, 0f)) < 1f)
            {
                requiredForward = LVec3.Up;
            }
            else if (disallowRotation)
            {
                requiredForward = previousDoor.WorldForward * -1f;
            }

            if (requiredForward.HasValue && LVec3.Angle(requiredForward.Value, nextForward) > 1f)
            {
                return false;
            }

            if (archetypeIndex.HasValue && isMainPath)
            {
                float straighten = flow.Archetypes[archetypeIndex.Value].StraightenChance;
                int usedCount = 0;
                OfflineDoorway? firstUsed = null;
                foreach (OfflineDoorway d in previousDoor.Owner.Doorways)
                {
                    if (d.Used)
                    {
                        usedCount++;
                        firstUsed ??= d;
                    }
                }

                if (straighten > 0f
                    && usedCount == 1
                    && firstUsed != null
                    && firstUsed.WorldForward.X == -nextForward.X
                    && firstUsed.WorldForward.Y == -nextForward.Y
                    && firstUsed.WorldForward.Z == -nextForward.Z
                    && random.NextDouble() < straighten)
                {
                    weight += 100f;
                }
            }

            return true;
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

        private static LQuat ToLQuat(BakedQuat q) => new(q.X, q.Y, q.Z, q.W);
    }
}

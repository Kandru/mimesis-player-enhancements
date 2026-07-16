using DunGen;
using DunGen.Graph;
using DunGen.Tags;
using MimesisSeedScanner;
using UnityEngine;

namespace MimesisSeedScanner.Mod
{
    internal static class CatalogExporter
    {
        private static readonly Vector3 UpVector = Vector3.up;

        internal static ScanCatalog Export(IReadOnlyList<FlowCatalogEntry> flows)
        {
            var catalog = new ScanCatalog();
            var tileByPrefab = new Dictionary<GameObject, int>();

            foreach (FlowCatalogEntry entry in flows)
            {
                if (entry.Flow == null)
                {
                    continue;
                }

                foreach (TileSet tileSet in entry.Flow.GetUsedTileSets())
                {
                    RegisterTileSetTiles(tileSet, catalog, tileByPrefab);
                }

                foreach (TileInjectionRule rule in entry.Flow.TileInjectionRules)
                {
                    if (rule.TileSet != null)
                    {
                        RegisterTileSetTiles(rule.TileSet, catalog, tileByPrefab);
                    }
                }
            }

            foreach (FlowCatalogEntry entry in flows)
            {
                if (entry.Flow == null)
                {
                    continue;
                }

                catalog.Flows.Add(ExportFlow(entry.FlowId, entry.Flow, catalog, tileByPrefab));
            }

            MelonLogger.Msg(
                $"Exported scan catalog — {catalog.Tiles.Count} tile template(s), {catalog.Flows.Count} flow(s).");
            return catalog;
        }

        private static void RegisterTileSetTiles(
            TileSet tileSet,
            ScanCatalog catalog,
            Dictionary<GameObject, int> tileByPrefab)
        {
            foreach (GameObjectChance weight in tileSet.TileWeights.Weights)
            {
                if (weight.Value == null || tileByPrefab.ContainsKey(weight.Value))
                {
                    continue;
                }

                BakedTile baked = BakeTile(weight.Value, catalog.Tiles.Count);
                tileByPrefab[weight.Value] = baked.Id;
                catalog.Tiles.Add(baked);
            }
        }

        private static BakedTile BakeTile(GameObject prefab, int id)
        {
            var proxy = new TileProxy(prefab, ignoreSpriteRendererBounds: true, UpVector);
            var baked = new BakedTile
            {
                Id = id,
                Name = prefab.name,
                LocalBounds = ToBounds(proxy.Placement.LocalBounds),
                RepeatMode = MapRepeatMode(proxy.PrefabTile.RepeatMode),
                AllowRotation = proxy.PrefabTile.AllowRotation,
                ConnectionChance = proxy.PrefabTile.ConnectionChance,
                OverrideConnectionChance = proxy.PrefabTile.OverrideConnectionChance,
            };

            foreach (Tag tag in proxy.Tags.Tags)
            {
                if (!string.IsNullOrWhiteSpace(tag.Name))
                {
                    baked.Tags.Add(tag.Name);
                }
            }

            foreach (DoorwayProxy doorway in proxy.Doorways)
            {
                baked.Doorways.Add(new BakedDoorway
                {
                    Index = doorway.Index,
                    LocalPosition = ToVec3(doorway.LocalPosition),
                    LocalRotation = ToQuat(doorway.LocalRotation),
                    SocketId = ResolveSocketId(doorway.Socket),
                    IsDisabled = doorway.IsDisabled,
                });
            }

            foreach (DoorwayProxy entrance in proxy.Entrances)
            {
                baked.EntranceIndices.Add(entrance.Index);
            }

            foreach (DoorwayProxy exit in proxy.Exits)
            {
                baked.ExitIndices.Add(exit.Index);
            }

            return baked;
        }

        private static BakedFlow ExportFlow(
            string flowId,
            DungeonFlow flow,
            ScanCatalog catalog,
            Dictionary<GameObject, int> tileByPrefab)
        {
            var bakedFlow = new BakedFlow
            {
                FlowId = flowId,
                Length = new BakedIntRange(flow.Length.Min, flow.Length.Max),
                LengthMultiplier = 1f,
                BranchMode = MapBranchMode(flow.BranchMode),
                BranchCount = new BakedIntRange(flow.BranchCount.Min, flow.BranchCount.Max),
                DoorwayConnectionChance = flow.DoorwayConnectionChance,
                RestrictConnectionToSameSection = flow.RestrictConnectionToSameSection,
                TileTagConnectionMode = MapTagConnectionMode(flow.TileTagConnectionMode),
            };

            foreach (Tag tag in flow.BranchPruneTags)
            {
                if (!string.IsNullOrWhiteSpace(tag.Name))
                {
                    bakedFlow.BranchPruneTags.Add(tag.Name);
                }
            }

            foreach (TagPair pair in flow.TileConnectionTags)
            {
                bakedFlow.TileConnectionTags.Add(new BakedTagPair
                {
                    TagA = pair.TagA?.Name ?? string.Empty,
                    TagB = pair.TagB?.Name ?? string.Empty,
                });
            }

            var tileSetIndexByRef = new Dictionary<TileSet, int>();
            foreach (TileSet tileSet in flow.GetUsedTileSets())
            {
                EnsureTileSetExported(tileSet, bakedFlow, tileSetIndexByRef, tileByPrefab);
            }

            var archetypeIndexByRef = new Dictionary<DungeonArchetype, int>();
            foreach (DungeonArchetype archetype in flow.GetUsedArchetypes())
            {
                EnsureArchetypeTileSetsExported(archetype, bakedFlow, tileSetIndexByRef, tileByPrefab);
                archetypeIndexByRef[archetype] = bakedFlow.Archetypes.Count;
                bakedFlow.Archetypes.Add(ExportArchetype(archetype, bakedFlow, tileSetIndexByRef));
            }

            foreach (GraphLine line in flow.Lines)
            {
                var bakedLine = new BakedGraphLine
                {
                    Position = line.Position,
                    Length = line.Length,
                };
                foreach (DungeonArchetype archetype in line.DungeonArchetypes)
                {
                    if (archetypeIndexByRef.TryGetValue(archetype, out int index))
                    {
                        bakedLine.ArchetypeIndices.Add(index);
                    }
                }

                bakedFlow.Lines.Add(bakedLine);
            }

            foreach (GraphNode node in flow.Nodes.OrderBy(n => n.Position))
            {
                foreach (TileSet tileSet in node.TileSets
                             .Concat(node.BranchStartTileSets)
                             .Concat(node.BranchCapTileSets)
                             .Concat(node.GetBranchTileSets()))
                {
                    EnsureTileSetExported(tileSet, bakedFlow, tileSetIndexByRef, tileByPrefab);
                }

                var bakedNode = new BakedGraphNode
                {
                    Position = node.Position,
                    EnableBranching = node.EnableBranching,
                    BranchCount = new BakedIntRange(node.BranchCount.Min, node.BranchCount.Max),
                    BranchingDepth = new BakedIntRange(node.BranchingDepth.Min, node.BranchingDepth.Max),
                    BranchStartType = MapBranchCapType(node.BranchStartType),
                    BranchCapType = MapBranchCapType(node.BranchCapType),
                };

                AddTileSetIndices(node.TileSets, bakedNode.TileSetIndices, tileSetIndexByRef);
                AddTileSetIndices(node.BranchStartTileSets, bakedNode.BranchStartTileSetIndices, tileSetIndexByRef);
                AddTileSetIndices(node.BranchCapTileSets, bakedNode.BranchCapTileSetIndices, tileSetIndexByRef);
                AddTileSetIndices(node.GetBranchTileSets(), bakedNode.BranchTileSetIndices, tileSetIndexByRef);
                bakedFlow.Nodes.Add(bakedNode);
            }

            foreach (TileInjectionRule rule in flow.TileInjectionRules)
            {
                if (rule.TileSet == null)
                {
                    continue;
                }

                EnsureTileSetExported(rule.TileSet, bakedFlow, tileSetIndexByRef, tileByPrefab);
                if (!tileSetIndexByRef.TryGetValue(rule.TileSet, out int tileSetIndex))
                {
                    continue;
                }

                bakedFlow.InjectionRules.Add(new BakedInjectionRule
                {
                    TileSetIndex = tileSetIndex,
                    NormalizedPathDepth = new BakedFloatRange(rule.NormalizedPathDepth.Min, rule.NormalizedPathDepth.Max),
                    NormalizedBranchDepth = new BakedFloatRange(rule.NormalizedBranchDepth.Min, rule.NormalizedBranchDepth.Max),
                    CanAppearOnMainPath = rule.CanAppearOnMainPath,
                    CanAppearOnBranchPath = rule.CanAppearOnBranchPath,
                });
            }

            return bakedFlow;
        }

        private static void EnsureArchetypeTileSetsExported(
            DungeonArchetype archetype,
            BakedFlow bakedFlow,
            Dictionary<TileSet, int> tileSetIndexByRef,
            Dictionary<GameObject, int> tileByPrefab)
        {
            foreach (TileSet tileSet in archetype.TileSets
                         .Concat(archetype.BranchStartTileSets)
                         .Concat(archetype.BranchCapTileSets))
            {
                EnsureTileSetExported(tileSet, bakedFlow, tileSetIndexByRef, tileByPrefab);
            }
        }

        private static BakedArchetype ExportArchetype(
            DungeonArchetype archetype,
            BakedFlow bakedFlow,
            Dictionary<TileSet, int> tileSetIndexByRef)
        {
            var baked = new BakedArchetype
            {
                BranchStartType = MapBranchCapType(archetype.BranchStartType),
                BranchCapType = MapBranchCapType(archetype.BranchCapType),
                BranchingDepth = new BakedIntRange(archetype.BranchingDepth.Min, archetype.BranchingDepth.Max),
                BranchCount = new BakedIntRange(archetype.BranchCount.Min, archetype.BranchCount.Max),
                StraightenChance = archetype.StraightenChance,
                Unique = archetype.Unique,
            };

            AddTileSetIndices(archetype.TileSets, baked.TileSetIndices, tileSetIndexByRef);
            AddTileSetIndices(archetype.BranchStartTileSets, baked.BranchStartTileSetIndices, tileSetIndexByRef);
            AddTileSetIndices(archetype.BranchCapTileSets, baked.BranchCapTileSetIndices, tileSetIndexByRef);
            return baked;
        }

        private static void EnsureTileSetExported(
            TileSet tileSet,
            BakedFlow bakedFlow,
            Dictionary<TileSet, int> tileSetIndexByRef,
            Dictionary<GameObject, int> tileByPrefab)
        {
            if (tileSet == null || tileSetIndexByRef.ContainsKey(tileSet))
            {
                return;
            }

            var bakedSet = new BakedTileSet();
            foreach (GameObjectChance weight in tileSet.TileWeights.Weights)
            {
                if (weight.Value == null || !tileByPrefab.TryGetValue(weight.Value, out int tileId))
                {
                    continue;
                }

                bakedSet.Weights.Add(new BakedWeightedTile
                {
                    TileId = tileId,
                    MainPathWeight = weight.MainPathWeight,
                    BranchPathWeight = weight.BranchPathWeight,
                    DepthWeights = SampleDepthCurve(weight.DepthWeightScale),
                });
            }

            tileSetIndexByRef[tileSet] = bakedFlow.TileSets.Count;
            bakedFlow.TileSets.Add(bakedSet);
        }

        private static void AddTileSetIndices(
            IEnumerable<TileSet> tileSets,
            List<int> target,
            Dictionary<TileSet, int> tileSetIndexByRef)
        {
            foreach (TileSet tileSet in tileSets)
            {
                if (tileSet == null || !tileSetIndexByRef.TryGetValue(tileSet, out int index))
                {
                    continue;
                }

                if (!target.Contains(index))
                {
                    target.Add(index);
                }
            }
        }

        private static float[] SampleDepthCurve(AnimationCurve curve)
        {
            var samples = new float[11];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = curve.Evaluate(i / 10f);
            }

            return samples;
        }

        private static string ResolveSocketId(DoorwaySocket? socket) =>
            socket == null ? string.Empty : socket.name;

        private static BakedVec3 ToVec3(Vector3 v) => new(v.x, v.y, v.z);

        private static BakedQuat ToQuat(Quaternion q) => new(q.x, q.y, q.z, q.w);

        private static BakedBounds ToBounds(Bounds b) => new(ToVec3(b.center), ToVec3(b.size));

        private static BakedTileRepeatMode MapRepeatMode(TileRepeatMode mode) => mode switch
        {
            TileRepeatMode.Disallow => BakedTileRepeatMode.Disallow,
            TileRepeatMode.DisallowImmediate => BakedTileRepeatMode.DisallowImmediate,
            _ => BakedTileRepeatMode.Allow,
        };

        private static BakedBranchMode MapBranchMode(BranchMode mode) => mode switch
        {
            BranchMode.Global => BakedBranchMode.Global,
            BranchMode.Section => BakedBranchMode.Section,
            _ => BakedBranchMode.Local,
        };

        private static BakedBranchCapType MapBranchCapType(BranchCapType type) =>
            type == BranchCapType.InsteadOf ? BakedBranchCapType.InsteadOf : BakedBranchCapType.AsWellAs;

        private static BakedTagConnectionMode MapTagConnectionMode(DungeonFlow.TagConnectionMode mode) =>
            mode == DungeonFlow.TagConnectionMode.Reject
                ? BakedTagConnectionMode.Reject
                : BakedTagConnectionMode.Accept;
    }
}

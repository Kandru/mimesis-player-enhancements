using MimesisSeedScanner;

namespace MimesisSeedScanner.Cli.Engine
{
    internal sealed class OfflineDungeon
    {
        internal OfflineDungeon(ScanCatalog catalog)
        {
            Catalog = catalog;
        }

        internal ScanCatalog Catalog { get; }

        internal List<OfflineTile> AllTiles { get; } = [];

        internal List<OfflineTile> MainPathTiles { get; } = [];

        internal List<OfflineTile> BranchPathTiles { get; } = [];

        internal List<(OfflineDoorway A, OfflineDoorway B)> Connections { get; } = [];

        internal void AddTile(OfflineTile tile)
        {
            AllTiles.Add(tile);
            if (tile.IsOnMainPath)
            {
                MainPathTiles.Add(tile);
            }
            else
            {
                BranchPathTiles.Add(tile);
            }
        }

        internal void RemoveLastTile()
        {
            if (MainPathTiles.Count == 0)
            {
                return;
            }

            OfflineTile tile = MainPathTiles[^1];
            MainPathTiles.RemoveAt(MainPathTiles.Count - 1);
            AllTiles.Remove(tile);
        }

        internal void RemoveLastConnection()
        {
            if (Connections.Count == 0)
            {
                return;
            }

            (OfflineDoorway a, OfflineDoorway b) = Connections[^1];
            Connections.RemoveAt(Connections.Count - 1);
            a.Connected = null;
            b.Connected = null;
        }

        internal void Connect(OfflineDoorway a, OfflineDoorway b)
        {
            a.Connected = b;
            b.Connected = a;
            Connections.Add((a, b));
        }

        internal void RemoveTile(OfflineTile tile)
        {
            AllTiles.Remove(tile);
            MainPathTiles.Remove(tile);
            BranchPathTiles.Remove(tile);
            for (int i = Connections.Count - 1; i >= 0; i--)
            {
                (OfflineDoorway a, OfflineDoorway b) = Connections[i];
                if (a.Owner == tile || b.Owner == tile)
                {
                    a.Connected = null;
                    b.Connected = null;
                    Connections.RemoveAt(i);
                }
            }
        }
    }

    internal sealed class OfflineTile
    {
        internal OfflineTile(BakedTile template)
        {
            Template = template;
            Position = LVec3.Zero;
            Rotation = LQuat.Identity;
            Doorways = new OfflineDoorway[template.Doorways.Count];
            for (int i = 0; i < template.Doorways.Count; i++)
            {
                Doorways[i] = new OfflineDoorway(this, template.Doorways[i]);
            }
        }

        internal BakedTile Template { get; }

        internal OfflineDoorway[] Doorways { get; }

        internal LVec3 Position { get; private set; }

        internal LQuat Rotation { get; private set; }

        internal bool IsOnMainPath { get; set; }

        internal int PathDepth { get; set; }

        internal int BranchDepth { get; set; }

        internal int BranchId { get; set; } = -1;

        internal int? ArchetypeIndex { get; set; }

        internal int? NodeIndex { get; set; }

        internal int? LineIndex { get; set; }

        internal bool IsInjected { get; set; }

        internal LBounds WorldBounds
        {
            get
            {
                BakedBounds local = Template.LocalBounds;
                LVec3 center = Position + LQuat.Rotate(Rotation, ToLVec3(local.Center));
                return new LBounds(center, ToLVec3(local.Size));
            }
        }

        internal void PositionBySocket(OfflineDoorway myDoorway, OfflineDoorway otherDoorway)
        {
            LQuat rotation = LQuat.LookRotation(otherDoorway.WorldForward * -1f, otherDoorway.WorldUp);
            LQuat localRotWorld = LQuat.Multiply(Rotation, myDoorway.LocalRotation);
            Rotation = LQuat.Multiply(rotation, LQuat.Inverse(localRotWorld));
            Position = otherDoorway.WorldPosition - (myDoorway.WorldPosition - Position);
        }

        private static LVec3 ToLVec3(BakedVec3 v) => new(v.X, v.Y, v.Z);
    }

    internal sealed class OfflineDoorway
    {
        internal OfflineDoorway(OfflineTile owner, BakedDoorway template)
        {
            Owner = owner;
            Index = template.Index;
            LocalPosition = ToLVec3(template.LocalPosition);
            LocalRotation = ToLQuat(template.LocalRotation);
            SocketId = template.SocketId;
            IsDisabled = template.IsDisabled;
        }

        internal OfflineTile Owner { get; }

        internal int Index { get; }

        internal LVec3 LocalPosition { get; }

        internal LQuat LocalRotation { get; }

        internal string SocketId { get; }

        internal bool IsDisabled { get; }

        internal OfflineDoorway? Connected { get; set; }

        internal bool Used => Connected != null;

        internal LQuat LocalRotationWorld => LQuat.Multiply(Owner.Rotation, LocalRotation);

        internal LVec3 WorldForward => LQuat.Rotate(LocalRotationWorld, new LVec3(0f, 0f, 1f));

        internal LVec3 WorldUp => LQuat.Rotate(LocalRotationWorld, new LVec3(0f, 1f, 0f));

        internal LVec3 WorldPosition => Owner.Position + LQuat.Rotate(Owner.Rotation, LocalPosition);

        private static LVec3 ToLVec3(BakedVec3 v) => new(v.X, v.Y, v.Z);

        private static LQuat ToLQuat(BakedQuat q) => new(q.X, q.Y, q.Z, q.W);
    }

    internal readonly struct OfflineWeightedTile
    {
        internal OfflineWeightedTile(int tileId, float weight)
        {
            TileId = tileId;
            Weight = weight;
        }

        internal int TileId { get; }

        internal float Weight { get; }
    }

    internal readonly struct OfflineDoorwayPair
    {
        internal OfflineDoorwayPair(
            int tileId,
            int previousDoorwayIndex,
            int nextDoorwayIndex,
            float tileWeight,
            float doorwayWeight)
        {
            TileId = tileId;
            PreviousDoorwayIndex = previousDoorwayIndex;
            NextDoorwayIndex = nextDoorwayIndex;
            TileWeight = tileWeight;
            DoorwayWeight = doorwayWeight;
        }

        internal int TileId { get; }

        internal int PreviousDoorwayIndex { get; }

        internal int NextDoorwayIndex { get; }

        internal float TileWeight { get; }

        internal float DoorwayWeight { get; }
    }

    internal sealed class PendingInjection
    {
        internal int TileSetIndex { get; init; }

        internal float NormalizedPathDepth { get; init; }

        internal float NormalizedBranchDepth { get; init; }

        internal bool IsOnMainPath { get; init; }

        internal bool ShouldInject(bool onMainPath, float pathDepth, float branchDepth)
        {
            if (IsOnMainPath != onMainPath)
            {
                return false;
            }

            if (NormalizedPathDepth > pathDepth)
            {
                return false;
            }

            if (onMainPath)
            {
                return true;
            }

            return NormalizedBranchDepth <= branchDepth;
        }
    }
}

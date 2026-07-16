using ReluReplay.Data;
using ReluReplay.Serializer;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal sealed class ReplayLevelObjectRemapper
    {
        private readonly Dictionary<int, int> _recordedSequentialToStable = new();
        private readonly Dictionary<int, int> _stableToCurrentSequential = new();

        internal void BuildFromHeader(ReplayHeader? header)
        {
            _recordedSequentialToStable.Clear();
            _stableToCurrentSequential.Clear();
            if (header?.LevelObjectMappings == null)
            {
                return;
            }

            foreach (LevelObjectMapping mapping in header.LevelObjectMappings)
            {
                _recordedSequentialToStable[mapping.SequentialID] = mapping.StableID;
            }

            if (HubGameDataAccess.DynamicData == null)
            {
                return;
            }

            Dictionary<int, LevelObject> allLevelObjects =
                HubGameDataAccess.DynamicData.GetAllLevelObjects(excludeClientOnly: false);
            foreach (KeyValuePair<int, LevelObject> pair in allLevelObjects)
            {
                LevelObject levelObject = pair.Value;
                if (levelObject == null)
                {
                    continue;
                }

                int stableId = LevelObjectStableIDUtility.ComputeStableID(
                    levelObject.gameObject.name,
                    levelObject.transform.position);
                _stableToCurrentSequential[stableId] = pair.Key;
            }
        }

        internal int Remap(int recordedSequentialId)
        {
            if (!_recordedSequentialToStable.TryGetValue(recordedSequentialId, out int stableId))
            {
                return recordedSequentialId;
            }

            if (_stableToCurrentSequential.TryGetValue(stableId, out int currentId))
            {
                return currentId;
            }

            return recordedSequentialId;
        }
    }
}

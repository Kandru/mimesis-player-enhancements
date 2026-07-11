namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class VoiceClipCache
    {
        private sealed class CacheNode
        {
            internal long Id;
            internal UnityEngine.Object? Clip;
        }

        private static readonly Dictionary<long, LinkedListNode<CacheNode>> _nodes = [];
        private static readonly LinkedList<CacheNode> _lru = new();

        internal static bool TryGet(long speechEventId, out UnityEngine.Object? clip)
        {
            clip = null;
            if (!_nodes.TryGetValue(speechEventId, out LinkedListNode<CacheNode>? node))
            {
                return false;
            }

            clip = node.Value.Clip;
            if (clip == null)
            {
                _ = _nodes.Remove(speechEventId);
                _lru.Remove(node);
                return false;
            }

            _lru.Remove(node);
            _lru.AddFirst(node);
            return true;
        }

        internal static void Store(long speechEventId, UnityEngine.Object clip)
        {
            if (clip == null)
            {
                return;
            }

            if (_nodes.TryGetValue(speechEventId, out LinkedListNode<CacheNode>? existing))
            {
                if (!ReferenceEquals(existing.Value.Clip, clip))
                {
                    DestroyClip(existing.Value.Clip);
                    existing.Value.Clip = clip;
                }

                _lru.Remove(existing);
                _lru.AddFirst(existing);
                return;
            }

            LinkedListNode<CacheNode> node = _lru.AddFirst(new CacheNode { Id = speechEventId, Clip = clip });
            _nodes[speechEventId] = node;
            TrimToMax();
        }

        internal static void RemoveClipsForArchive(IEnumerable<long> eventIds)
        {
            foreach (long eventId in eventIds)
            {
                Remove(eventId);
            }
        }

        internal static void ClearAll()
        {
            foreach (CacheNode node in _lru)
            {
                DestroyClip(node.Clip);
            }

            _lru.Clear();
            _nodes.Clear();
        }

        private static void Remove(long speechEventId)
        {
            if (!_nodes.TryGetValue(speechEventId, out LinkedListNode<CacheNode>? node))
            {
                return;
            }

            DestroyClip(node.Value.Clip);
            _lru.Remove(node);
            _ = _nodes.Remove(speechEventId);
        }

        private static void TrimToMax()
        {
            int maxEntries = VoicePerformanceRuntime.ClipCacheMaxEntries;
            while (_lru.Count > maxEntries)
            {
                LinkedListNode<CacheNode>? last = _lru.Last;
                if (last == null)
                {
                    break;
                }

                DestroyClip(last.Value.Clip);
                _ = _nodes.Remove(last.Value.Id);
                _lru.RemoveLast();
            }
        }

        private static void DestroyClip(UnityEngine.Object? clip)
        {
            if (clip != null)
            {
                UnityEngine.Object.Destroy(clip);
            }
        }
    }
}

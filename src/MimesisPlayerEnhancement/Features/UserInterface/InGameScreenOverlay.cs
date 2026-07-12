using MimesisPlayerEnhancement.Ui;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    /// <summary>
    /// Shared full-screen in-game HUD overlay on the Main UI layer. Features register as
    /// consumers; the root stays active while any consumer needs it.
    /// </summary>
    internal static class InGameScreenOverlay
    {
        private const string Feature = "Ui";
        private const string RootName = "MPE_InGameScreenOverlay";

        private static readonly HashSet<string> Consumers = new(System.StringComparer.Ordinal);
        private static readonly Vector3[] CornerBuffer = new Vector3[4];

        private static RectTransform? _root;
        private static bool _loggedUnavailable;

        internal static RectTransform? Root => _root;

        internal static RectTransform? EnsureRoot()
        {
            if (_root != null)
            {
                return _root;
            }

            Transform? main = ModUiRoot.GetMain();
            if (main == null)
            {
                LogUnavailableOnce("Main UI layer unavailable");
                return null;
            }

            GameObject rootObject = ModUiRoot.CreateUiRoot(main, RootName);
            rootObject.transform.SetAsLastSibling();
            _root = rootObject.GetComponent<RectTransform>();
            _root.gameObject.SetActive(false);
            return _root;
        }

        internal static void Register(string consumerId)
        {
            Consumers.Add(consumerId);
            EnsureRoot();
            RefreshVisibility();
        }

        internal static void Unregister(string consumerId)
        {
            Consumers.Remove(consumerId);
            RefreshVisibility();

            if (Consumers.Count == 0)
            {
                DestroyRoot();
            }
        }

        internal static bool TryProjectBounds(
            RectTransform target,
            out float leftX,
            out float bottomY,
            out float topY)
        {
            leftX = 0f;
            bottomY = 0f;
            topY = 0f;

            RectTransform? overlay = EnsureRoot();
            if (overlay == null)
            {
                return false;
            }

            target.GetWorldCorners(CornerBuffer);

            Vector2 bottomLeft = overlay.InverseTransformPoint(CornerBuffer[0]);
            Vector2 topLeft = overlay.InverseTransformPoint(CornerBuffer[1]);
            leftX = bottomLeft.x;
            bottomY = bottomLeft.y;
            topY = topLeft.y;
            return topY > bottomY;
        }

        private static void RefreshVisibility()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(Consumers.Count > 0);
            }
        }

        private static void DestroyRoot()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }

            _root = null;
        }

        private static void LogUnavailableOnce(string reason)
        {
            if (_loggedUnavailable)
            {
                return;
            }

            _loggedUnavailable = true;
            ModLog.Warn(Feature, $"In-game screen overlay unavailable — {reason}");
        }
    }
}

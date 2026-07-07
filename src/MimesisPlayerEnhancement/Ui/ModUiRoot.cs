using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// Attach points into the game's UI hierarchy (<c>UIManager.nodes[eUIHeight.Top]</c>).
    /// </summary>
    internal static class ModUiRoot
    {
        internal static Transform? GetTop()
        {
            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return null;
            }

            FieldInfo? nodesField = typeof(UIManager).GetField(
                "nodes",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (nodesField?.GetValue(uiManager) is not Transform[] nodes || nodes.Length == 0)
            {
                return null;
            }

            int topIndex = (int)Enum.Parse(typeof(eUIHeight), "Top");
            if (topIndex < 0 || topIndex >= nodes.Length)
            {
                topIndex = Math.Min(1, nodes.Length - 1);
            }

            return nodes[topIndex];
        }

        internal static GameObject CreateUiRoot(Transform parent, string name)
        {
            GameObject root = new(name);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            ModUiLayout.Stretch(rect);
            return root;
        }
    }
}

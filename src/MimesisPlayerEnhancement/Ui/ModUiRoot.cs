using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// Attach points into the game's UI hierarchy (<c>UIManager.nodes[eUIHeight]</c>).
    /// </summary>
    internal static class ModUiRoot
    {
        internal static Transform? GetTop()
        {
            return GetUiHeightNode("Top", fallbackIndex: 1);
        }

        internal static Transform? GetMain()
        {
            return GetUiHeightNode("Main", fallbackIndex: 2);
        }

        internal static GameObject CreateUiRoot(Transform parent, string name)
        {
            GameObject root = new(name);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            ModUiLayout.Stretch(rect);
            return root;
        }

        private static Transform? GetUiHeightNode(string heightName, int fallbackIndex)
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

            int index = (int)Enum.Parse(typeof(eUIHeight), heightName);
            if (index < 0 || index >= nodes.Length)
            {
                index = Math.Min(fallbackIndex, nodes.Length - 1);
            }

            return nodes[index];
        }
    }
}

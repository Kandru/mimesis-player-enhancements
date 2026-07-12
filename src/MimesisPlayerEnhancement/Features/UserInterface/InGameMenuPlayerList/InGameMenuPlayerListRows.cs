using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.UserInterface.InGameMenuPlayerList
{
    /// <summary>
    /// Row sizing for the ESC menu player list overlay. Operates on vanilla
    /// <see cref="UIPrefab_InGameMenu.playerUIElements"/> containers.
    /// </summary>
    internal static class InGameMenuPlayerListRows
    {
        private const float DefaultRowHeight = 56f;

        internal static float MeasureRowHeight(UIPrefab_InGameMenu menu)
        {
            if (menu.playerUIElements.Count >= 2)
            {
                RectTransform? first = menu.playerUIElements[0].container.GetComponent<RectTransform>();
                RectTransform? second = menu.playerUIElements[1].container.GetComponent<RectTransform>();
                if (first != null && second != null)
                {
                    float step = Mathf.Abs(first.anchoredPosition.y - second.anchoredPosition.y);
                    if (step >= 1f)
                    {
                        return step;
                    }

                    if (first.rect.height >= 1f)
                    {
                        return first.rect.height;
                    }
                }
            }

            return DefaultRowHeight;
        }

        internal static void ApplyRowLayoutElements(UIPrefab_InGameMenu menu, float rowHeight)
        {
            foreach (UIPrefab_InGameMenu.PlayerUIElement row in menu.playerUIElements)
            {
                ApplyRowLayoutElement(row, rowHeight);
            }
        }

        private static void ApplyRowLayoutElement(UIPrefab_InGameMenu.PlayerUIElement row, float rowHeight)
        {
            if (row.container == null)
            {
                return;
            }

            LayoutElement layoutElement = row.container.GetComponent<LayoutElement>()
                ?? row.container.AddComponent<LayoutElement>();
            layoutElement.minHeight = rowHeight;
            layoutElement.preferredHeight = rowHeight;
        }
    }
}

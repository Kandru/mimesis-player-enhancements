using UnityEngine;

namespace MimesisPlayerEnhancement.Ui.MenuMirror
{
    internal enum MenuKind
    {
        MainMenu,
        InGameMenu,
    }

    /// <summary>
    /// Declarative button changes a feature wants applied to a vanilla menu.
    /// Features never touch menu buttons directly; the mirror rebuilds the
    /// column from all active customizations.
    /// </summary>
    internal sealed class MenuCustomization
    {
        internal List<string> HiddenButtonIds { get; } = [];

        internal List<CustomMenuButton> CustomButtons { get; } = [];

        internal bool IsEmpty => HiddenButtonIds.Count == 0 && CustomButtons.Count == 0;

        internal MenuCustomization HideVanilla(string ueid)
        {
            if (!HiddenButtonIds.Contains(ueid))
            {
                HiddenButtonIds.Add(ueid);
            }

            return this;
        }

        internal MenuCustomization AddCustom(CustomMenuButton button)
        {
            CustomButtons.Add(button);
            return this;
        }
    }

    /// <summary>
    /// A mod-provided button cloned from a vanilla button's style, placed relative
    /// to a vanilla button id (<see cref="BeforeButtonId"/> wins over <see cref="AfterButtonId"/>).
    /// </summary>
    internal sealed class CustomMenuButton
    {
        internal CustomMenuButton(string id, string label, Action onClick)
        {
            Id = id;
            Label = label;
            OnClick = onClick;
        }

        internal string Id { get; }

        internal string Label { get; }

        internal Action OnClick { get; }

        internal Color? LabelColor { get; set; }

        /// <summary>Insert directly above this vanilla button id.</summary>
        internal string? BeforeButtonId { get; set; }

        /// <summary>Insert directly below this vanilla button id.</summary>
        internal string? AfterButtonId { get; set; }
    }
}

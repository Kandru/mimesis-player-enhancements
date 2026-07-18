using System.Reflection;
using System.Runtime.CompilerServices;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal readonly struct SurvivalResultRowSlot
    {
        internal SurvivalResultRowSlot(
            Transform rowRoot,
            Component nameText,
            GameObject survivalIcon,
            GameObject killedIcon,
            GameObject wastedIcon,
            Component awardText)
        {
            RowRoot = rowRoot;
            NameText = nameText;
            SurvivalIcon = survivalIcon;
            KilledIcon = killedIcon;
            WastedIcon = wastedIcon;
            AwardText = awardText;
        }

        internal Transform RowRoot { get; }
        internal Component NameText { get; }
        internal GameObject SurvivalIcon { get; }
        internal GameObject KilledIcon { get; }
        internal GameObject WastedIcon { get; }
        internal Component AwardText { get; }
    }

    internal static class SurvivalResultPlayerGrid
    {
        private const string Feature = "MorePlayers";
        private const int RowsPerColumn = 4;
        private const float ColumnGap = 12f;

        private static readonly ConditionalWeakTable<object, SurvivalResultGridState> GridStates = new();

        private static readonly MemberInfo? RandDungeonSeedMember =
            (MemberInfo?)AccessTools.Field(typeof(Hub.PersistentData), "randDungeonSeed")
            ?? AccessTools.Property(typeof(Hub.PersistentData), "randDungeonSeed");

        private static string GetL10NText(string key, params object[] formattingArgs)
        {
            return GameLocaleAccess.GetL10NText(key, formattingArgs);
        }

        private sealed class SurvivalResultGridState
        {
            internal SurvivalResultRowSlot[] NativeRows = [];
            internal readonly List<SurvivalResultRowSlot> ExtendedRows = [];
            internal float ColumnStep;
            internal bool ColumnStepMeasured;
        }

        internal static void ApplyPatchParameter(object ui, object[] parameters)
        {
            int cycleCount = (int)parameters[0];
            bool success = (bool)parameters[1];
            int playerCount = (int)parameters[2];
            int maxDisplay = Math.Min(playerCount, MorePlayersPatchHelpers.GetMaxPlayers());

            SetTitle(ui, GetL10NText("UI_PREFAB_SURVIVAL_RESULT_TITLE", cycleCount));
            SurvivalResultRowSlot[] slots = EnsureSlots(ui, maxDisplay);
            ResetSlots(slots);

            for (int i = 0; i < maxDisplay; i++)
            {
                PopulateRow(slots[i], parameters, i);
            }

            for (int i = maxDisplay; i < slots.Length; i++)
            {
                slots[i].RowRoot.gameObject.SetActive(false);
            }

            ApplyLostScrapSection(ui, parameters, success, playerCount);
            AppendRandomSeed(ui);
        }

        private static SurvivalResultGridState GetOrCreateState(object ui)
        {
            return GridStates.GetValue(ui, _ => new SurvivalResultGridState());
        }

        private static SurvivalResultRowSlot[] EnsureSlots(object ui, int requiredSlots)
        {
            SurvivalResultGridState state = GetOrCreateState(ui);

            if (state.NativeRows.Length == 0)
            {
                state.NativeRows = BuildNativeSlots(ui);
            }

            if (requiredSlots <= RowsPerColumn)
            {
                TrimExtendedRows(state, 0);
                return state.NativeRows;
            }

            if (!state.ColumnStepMeasured)
            {
                state.ColumnStep = MeasureColumnStep(state.NativeRows);
                state.ColumnStepMeasured = true;
            }

            int extendedRequired = requiredSlots - RowsPerColumn;
            EnsureExtendedRows(state, extendedRequired);

            SurvivalResultRowSlot[] all = new SurvivalResultRowSlot[requiredSlots];
            Array.Copy(state.NativeRows, all, RowsPerColumn);
            for (int i = 0; i < extendedRequired; i++)
            {
                all[RowsPerColumn + i] = state.ExtendedRows[i];
            }

            return all;
        }

        private static void EnsureExtendedRows(SurvivalResultGridState state, int extendedRequired)
        {
            TrimExtendedRows(state, extendedRequired);

            Transform rowParent = state.NativeRows[0].RowRoot.parent;

            while (state.ExtendedRows.Count < extendedRequired)
            {
                int slotIndex = RowsPerColumn + state.ExtendedRows.Count;
                int columnIndex = slotIndex / RowsPerColumn;
                int rowIndex = slotIndex % RowsPerColumn;
                SurvivalResultRowSlot template = state.NativeRows[rowIndex];
                Transform cloneRoot = UnityEngine.Object.Instantiate(template.RowRoot, rowParent);
                cloneRoot.gameObject.SetActive(true);
                cloneRoot.name = $"MorePlayersRow_{slotIndex + 1}";

                PositionExtendedRow(cloneRoot, template.RowRoot, columnIndex, state.ColumnStep);
                state.ExtendedRows.Add(BindRow(cloneRoot));
            }

            for (int i = 0; i < state.ExtendedRows.Count; i++)
            {
                int slotIndex = RowsPerColumn + i;
                int columnIndex = slotIndex / RowsPerColumn;
                int rowIndex = slotIndex % RowsPerColumn;
                PositionExtendedRow(
                    state.ExtendedRows[i].RowRoot,
                    state.NativeRows[rowIndex].RowRoot,
                    columnIndex,
                    state.ColumnStep);
            }
        }

        private static void TrimExtendedRows(SurvivalResultGridState state, int extendedRequired)
        {
            while (state.ExtendedRows.Count > extendedRequired)
            {
                int lastIndex = state.ExtendedRows.Count - 1;
                SurvivalResultRowSlot slot = state.ExtendedRows[lastIndex];
                state.ExtendedRows.RemoveAt(lastIndex);
                if (slot.RowRoot != null)
                {
                    UnityEngine.Object.Destroy(slot.RowRoot.gameObject);
                }
            }
        }

        private static void PositionExtendedRow(
            Transform cloneRoot,
            Transform templateRoot,
            int columnIndex,
            float columnStep)
        {
            if (cloneRoot is RectTransform cloneRect && templateRoot is RectTransform templateRect)
            {
                Vector2 anchor = templateRect.anchoredPosition;
                cloneRect.anchoredPosition = anchor + new Vector2(columnIndex * columnStep, 0f);
            }
        }

        private static SurvivalResultRowSlot[] BuildNativeSlots(object ui)
        {
            SurvivalResultRowSlot[] rows = new SurvivalResultRowSlot[RowsPerColumn];
            for (int i = 0; i < RowsPerColumn; i++)
            {
                Component nameText = GetUiText(ui, $"P{i + 1}Name");
                rows[i] = BindRow(nameText.transform.parent);
            }

            return rows;
        }

        private static SurvivalResultRowSlot BindRow(Transform rowRoot)
        {
            Component? nameText = null;
            GameObject? survival = null;
            GameObject? killed = null;
            GameObject? wasted = null;
            Component? award = null;

            foreach (Component component in rowRoot.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    continue;
                }

                string name = component.name;
                if (name.EndsWith("Name", StringComparison.Ordinal) && nameText == null && HasSetText(component))
                {
                    nameText = component;
                }
                else if (name.EndsWith("Survival", StringComparison.Ordinal))
                {
                    survival = component.gameObject;
                }
                else if (name.EndsWith("Killed", StringComparison.Ordinal))
                {
                    killed = component.gameObject;
                }
                else if (name.EndsWith("Wasted", StringComparison.Ordinal))
                {
                    wasted = component.gameObject;
                }
                else if (name.EndsWith("Award", StringComparison.Ordinal) && award == null && HasSetText(component))
                {
                    award = component;
                }
            }

            if (nameText == null || survival == null || killed == null || wasted == null || award == null)
            {
                throw new InvalidOperationException($"SurvivalResult row under {rowRoot.name} is missing expected UI elements.");
            }

            return new SurvivalResultRowSlot(rowRoot, nameText, survival, killed, wasted, award);
        }

        private static float MeasureColumnStep(SurvivalResultRowSlot[] nativeRows)
        {
            if (nativeRows[0].RowRoot is not RectTransform firstRect)
            {
                return 220f;
            }

            float width = firstRect.rect.width;
            if (width <= 1f && nativeRows[0].NameText.transform is RectTransform nameRect)
            {
                width = nameRect.rect.width;
            }

            return width > 1f ? width + ColumnGap : 220f;
        }

        private static void ResetSlots(SurvivalResultRowSlot[] slots)
        {
            foreach (SurvivalResultRowSlot slot in slots)
            {
                slot.RowRoot.gameObject.SetActive(true);
                slot.SurvivalIcon.SetActive(false);
                slot.KilledIcon.SetActive(false);
                slot.WastedIcon.SetActive(false);
                SetText(slot.AwardText, string.Empty);
                slot.AwardText.gameObject.SetActive(false);
            }
        }

        private static void PopulateRow(SurvivalResultRowSlot slot, object[] parameters, int index)
        {
            slot.RowRoot.gameObject.SetActive(true);
            SetText(slot.NameText, parameters[(3 * index) + 3]?.ToString() ?? string.Empty);

            int survivalState = Convert.ToInt32(parameters[(3 * index) + 4]);
            switch (survivalState)
            {
                case 0:
                    slot.SurvivalIcon.SetActive(true);
                    break;
                case 1:
                    slot.WastedIcon.SetActive(true);
                    break;
                case 2:
                    slot.KilledIcon.SetActive(true);
                    break;
                default:
                    ModLog.Warn(Feature, $"Unknown survival state {survivalState} for {GetText(slot.NameText)}");
                    break;
            }

            ApplyAward(slot, (AwardType)parameters[(3 * index) + 5]);
        }

        private static void ApplyAward(SurvivalResultRowSlot slot, AwardType awardType)
        {
            slot.AwardText.gameObject.SetActive(true);
            switch (awardType)
            {
                case AwardType.None:
                    SetText(slot.AwardText, string.Empty);
                    break;
                case AwardType.BestCarryItem:
                    SetText(slot.AwardText, GetL10NText("STRING_SETTLEMENT_REPORT_CASE_1"));
                    break;
                case AwardType.BestDamageToAlly:
                    SetText(slot.AwardText, GetL10NText("STRING_SETTLEMENT_REPORT_CASE_2"));
                    break;
                case AwardType.BestMimicEncounter:
                    SetText(slot.AwardText, GetL10NText("STRING_SETTLEMENT_REPORT_CASE_3"));
                    break;
                case AwardType.BestCamper:
                    SetText(slot.AwardText, GetL10NText("STRING_SETTLEMENT_REPORT_CASE_4"));
                    break;
            }
        }

        private static void ApplyLostScrapSection(object ui, object[] parameters, bool success, int playerCount)
        {
            GameObject lostAllScrab = GetUiObject(ui, "UE_LostAllScrab");
            GameObject lostScrabs = GetUiObject(ui, "UE_LostScrabs");
            Component[] scrabs =
            [
                GetUiText(ui, "Scrab1"),
                GetUiText(ui, "Scrab2"),
                GetUiText(ui, "Scrab3"),
            ];

            if (!success)
            {
                lostScrabs.SetActive(false);
                lostAllScrab.SetActive(true);
                return;
            }

            lostAllScrab.SetActive(false);
            int scrapCount = (int)parameters[(playerCount * 3) + 3];
            if (scrapCount == 0)
            {
                lostScrabs.SetActive(false);
                return;
            }

            lostScrabs.SetActive(true);
            for (int j = 0; j < scrabs.Length; j++)
            {
                if (j >= scrapCount)
                {
                    scrabs[j].gameObject.SetActive(false);
                    continue;
                }

                string key = parameters[(playerCount * 3) + 4 + (j * 2)]?.ToString() ?? string.Empty;
                int amount = (int)parameters[(playerCount * 3) + 4 + (j * 2) + 1];
                scrabs[j].gameObject.SetActive(true);
                SetText(scrabs[j], $"\\ {GetL10NText(key),-15} : ${amount,-5}\n");
            }
        }

        private static void AppendRandomSeed(object ui)
        {
            Component? randomSeed = GetUiText(ui, "RandomSeed");
            if (randomSeed == null)
            {
                return;
            }

            SetTextAppend(randomSeed, $"{GetRandomDungeonSeed()}");
        }

        private static void SetTitle(object ui, string text)
        {
            SetText(GetUiText(ui, "title"), text);
        }

        private static Component GetUiText(object ui, string propertyName)
        {
            PropertyInfo? property = AccessTools.Property(ui.GetType(), "UE_" + propertyName)
                ?? AccessTools.Property(ui.GetType(), propertyName);
            if (property?.GetValue(ui) is not Component component)
            {
                throw new InvalidOperationException($"SurvivalResult UI property UE_{propertyName} not found.");
            }

            return component;
        }

        private static GameObject GetUiObject(object ui, string propertyName)
        {
            PropertyInfo? property = AccessTools.Property(ui.GetType(), propertyName);
            object? value = property?.GetValue(ui);
            return value switch
            {
                GameObject go => go,
                Component component => component.gameObject,
                _ => throw new InvalidOperationException($"SurvivalResult UI property {propertyName} not found."),
            };
        }

        private static bool HasSetText(Component component) =>
            component.GetType().GetMethod("SetText", [typeof(string)]) != null;

        private static void SetText(Component textComponent, string value)
        {
            MethodInfo? setText = textComponent.GetType().GetMethod("SetText", [typeof(string)]);
            if (setText != null)
            {
                _ = setText.Invoke(textComponent, [value]);
                return;
            }

            PropertyInfo? textProperty = textComponent.GetType().GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(textComponent, value);
            }
        }

        private static void SetTextAppend(Component textComponent, string suffix)
        {
            PropertyInfo? textProperty = textComponent.GetType().GetProperty("text");
            if (textProperty?.GetValue(textComponent) is string current)
            {
                textProperty.SetValue(textComponent, current + suffix);
            }
        }

        private static string GetText(Component textComponent)
        {
            PropertyInfo? textProperty = textComponent.GetType().GetProperty("text");
            return textProperty?.GetValue(textComponent) as string ?? string.Empty;
        }

        private static object? GetRandomDungeonSeed()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null || RandDungeonSeedMember == null)
            {
                return string.Empty;
            }

            return RandDungeonSeedMember switch
            {
                FieldInfo field => field.GetValue(pdata),
                PropertyInfo property => property.GetValue(pdata),
                _ => string.Empty,
            };
        }
    }
}

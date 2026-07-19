using System.Reflection;
using Bifrost.ConstEnum;
using Bifrost.Cooked;
using Bifrost.MonsterData;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    internal static class MonsterInfoTestFactory
    {
        private static readonly FieldInfo MonsterTypeValueField =
            typeof(MonsterType).GetField("value__", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MonsterType.value__ not found");

        internal static MonsterInfo Create(
            MonsterType monsterType,
            string name = "enemy",
            string? puppetName = null,
            string? btName = null)
        {
            int typeValue = (int)MonsterTypeValueField.GetValue(monsterType)!;
            var masterData = new MonsterData_MasterData
            {
                name = name,
                monster_type = typeValue,
                btname = btName ?? string.Empty,
            };

            MonsterInfo info = new(masterData);

            if (!string.IsNullOrEmpty(puppetName))
            {
                SetField(info, nameof(MonsterInfo.PuppetName), puppetName);
            }

            return info;
        }

        internal static MonsterInfo CreateNamed(string name)
        {
            return Create(MonsterType.Boss, name: name);
        }

        private static void SetField(MonsterInfo info, string fieldName, object? value)
        {
            FieldInfo? field = typeof(MonsterInfo).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            field?.SetValue(info, value);
        }
    }
}

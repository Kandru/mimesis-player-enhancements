namespace MimesisSeedScanner.Mod
{
    internal static class HubAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? HubVworldField =
            typeof(Hub).GetField("vworld", InstanceFlags);

        private static readonly PropertyInfo? HubVworldProperty =
            typeof(Hub).GetProperty("vworld", InstanceFlags);

        private static readonly FieldInfo? HubPdataField =
            typeof(Hub).GetField("pdata", InstanceFlags);

        private static readonly PropertyInfo? HubDatamanProperty =
            typeof(Hub).GetProperty("dataman", InstanceFlags);

        internal static VWorld? TryGetVWorld()
        {
            if (Hub.s == null)
            {
                return null;
            }

            return HubVworldField?.GetValue(Hub.s) as VWorld
                   ?? HubVworldProperty?.GetValue(Hub.s) as VWorld;
        }

        internal static Hub.PersistentData? TryGetPdata()
        {
            if (Hub.s == null || HubPdataField == null)
            {
                return null;
            }

            return HubPdataField.GetValue(Hub.s) as Hub.PersistentData;
        }

        internal static ExcelDataManager? TryGetExcelDataManager()
        {
            if (Hub.s == null || HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataManager)
            {
                return null;
            }

            return dataManager.ExcelDataManager;
        }
    }
}

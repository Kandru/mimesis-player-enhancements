using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    internal static class HubGameDataAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? HubDatamanProperty =
            typeof(Hub).GetProperty("dataman", InstanceFlags);

        private static readonly PropertyInfo? HubDynamicDataManProperty =
            typeof(Hub).GetProperty("dynamicDataMan", InstanceFlags);

        internal static ExcelDataManager? Excel
        {
            get
            {
                if (Hub.s == null || HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataman)
                {
                    return null;
                }

                return dataman.ExcelDataManager;
            }
        }

        internal static DynamicDataManager? DynamicData
        {
            get
            {
                if (Hub.s == null || HubDynamicDataManProperty?.GetValue(Hub.s) is not DynamicDataManager dynamicDataMan)
                {
                    return null;
                }

                return dynamicDataMan;
            }
        }
    }
}

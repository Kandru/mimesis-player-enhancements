using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    // Shared reflection access to scene root transforms used by the minimap pipeline.
    internal static class WebDashboardSceneRoots
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? BgRootField =
            typeof(GameMainBase).GetField("BGRoot", InstanceFlags);

        private static readonly FieldInfo? TramConsoleField =
            typeof(GameMainBase).GetField("tramConsole", InstanceFlags);

        private static readonly FieldInfo? MaintenanceRoomRootField =
            typeof(MaintenanceScene).GetField("maintenanceRoomRoot", InstanceFlags);

        internal static Transform? TryGetBgRoot(GameMainBase? main)
        {
            return main != null ? BgRootField?.GetValue(main) as Transform : null;
        }

        internal static Component? TryGetTramConsole(GameMainBase? main)
        {
            return main != null ? TramConsoleField?.GetValue(main) as Component : null;
        }

        internal static Transform? TryGetMaintenanceRoomRoot(MaintenanceScene scene)
        {
            return MaintenanceRoomRootField?.GetValue(scene) as Transform;
        }
    }
}

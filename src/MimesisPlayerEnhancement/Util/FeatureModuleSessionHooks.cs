namespace MimesisPlayerEnhancement.Util
{
    internal enum SessionScope
    {
        Global = 0,
        HostOnly = 1,
        ClientAllowed = 2,
    }

    internal static class FeatureModuleSessionHooks
    {
        private static readonly HashSet<string> _activeModules = [];

        internal static bool IsModuleActive(string moduleName)
        {
            return _activeModules.Contains(moduleName);
        }

        internal static void InvokeSessionStarted(SessionRole role, int slotId)
        {
            _activeModules.Clear();
            foreach (FeatureModule module in FeatureModules.All)
            {
                if (!ShouldActivate(module.SessionScope, role))
                {
                    continue;
                }

                _ = _activeModules.Add(module.Name);
                InvokeSafely(module.Name, () => module.InvokeSessionStarted(role, slotId));
            }
        }

        internal static void InvokeSessionEnded()
        {
            foreach (FeatureModule module in FeatureModules.All)
            {
                if (!_activeModules.Contains(module.Name))
                {
                    continue;
                }

                InvokeSafely(module.Name, module.InvokeSessionEnded);
            }

            FeatureModules.ResetSharedSessionState();
            _activeModules.Clear();
        }

        private static bool ShouldActivate(SessionScope scope, SessionRole role)
        {
            return scope switch
            {
                SessionScope.Global => true,
                SessionScope.HostOnly => role == SessionRole.Host,
                SessionScope.ClientAllowed => role == SessionRole.Host || role == SessionRole.Client,
                _ => false,
            };
        }

        private static void InvokeSafely(string moduleName, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ModLog.Warn("Session", $"Module {moduleName} session hook failed — {ex.Message}");
            }
        }
    }
}

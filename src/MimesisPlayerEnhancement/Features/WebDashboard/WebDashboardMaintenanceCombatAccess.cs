using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMaintenanceCombatAccess
    {
        private static readonly FieldInfo? StatManagerSelfField =
            AccessTools.Field(typeof(StatManager), "_self");

        [ThreadStatic]
        private static VActor? _currentDamageVictim;

        internal static VActor? CurrentDamageVictim => _currentDamageVictim;

        internal readonly struct DamageVictimScope : IDisposable
        {
            private readonly bool _active;
            private readonly VActor? _previous;

            private DamageVictimScope(bool active, VActor? previous)
            {
                _active = active;
                _previous = previous;
            }

            internal static DamageVictimScope Push(VActor? victim)
            {
                DamageVictimScope scope = new(true, _currentDamageVictim);
                _currentDamageVictim = victim;
                return scope;
            }

            public void Dispose()
            {
                if (_active)
                {
                    _currentDamageVictim = _previous;
                }
            }
        }

        internal static DamageVictimScope PushDamageVictim(VActor? victim) => DamageVictimScope.Push(victim);

        internal static DamageVictimScope PushDamageVictim(StatManager manager)
        {
            if (StatManagerSelfField?.GetValue(manager) is VActor self)
            {
                return DamageVictimScope.Push(self);
            }

            return default;
        }

        internal static bool IsDashboardSpawnedMonster(VActor? actor)
        {
            return actor is VMonster monster
                && monster.VRoom is MaintenanceRoom
                && monster.ReasonOfSpawn.Equals(ReasonOfSpawn.Admin);
        }
    }
}

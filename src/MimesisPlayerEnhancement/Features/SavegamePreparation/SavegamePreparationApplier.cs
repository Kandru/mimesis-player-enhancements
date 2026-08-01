using System.Reflection;

namespace MimesisPlayerEnhancement.Features.SavegamePreparation
{
    internal static class SavegamePreparationApplier
    {
        private const string Feature = "SavegamePreparation";

        private static readonly PropertyInfo? GameSessionStageCountSetter =
            typeof(GameSessionInfo).GetProperty(
                nameof(GameSessionInfo.StageCount),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly PropertyInfo? PlayReportAccumulatedStageCountSetter =
            typeof(PlayReportManager).GetProperty(
                "AccumulatedStageCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static bool _tramUpgradesLogged;

        internal static void OnCreateNewGameInSlot()
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            SavegamePreparationNewGameGate.Arm();
            _tramUpgradesLogged = false;

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            TrySeedPdataTramUpgradeIds(pdata);

            int zone = SavegamePreparationResolver.ResolveStartingZone();
            if (zone <= 1 || pdata == null)
            {
                return;
            }

            pdata.StageCount = zone;
        }

        internal static void TryApplyStartingZoneToGameSession(GameSessionInfo session)
        {
            if (!SavegamePreparationNewGameGate.IsArmed
                || !SavegamePreparationResolver.ShouldApplyStartingZone())
            {
                return;
            }

            int zone = SavegamePreparationResolver.ResolveStartingZone();
            if (zone <= 1 || session.StageCount == zone)
            {
                return;
            }

            try
            {
                if (GameSessionStageCountSetter?.GetSetMethod(nonPublic: true) is MethodInfo setter)
                {
                    setter.Invoke(session, [zone]);
                }

                session.RefreshTargetCurrency(zone);
                TrySetPlayReportAccumulatedStageCount(session.PlayReportManager, zone);
                SavegamePreparationLog.InfoStartingZoneApplied(zone);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Starting zone apply failed — {ex.Message}");
            }
        }

        internal static void TryApplyStartupTramUpgrades(GameSessionInfo session)
        {
            if (!SavegamePreparationNewGameGate.IsArmed
                || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            try
            {
                List<int> configured = SavegamePreparationResolver.ResolveStartupTramUpgradeIds();
                if (configured.Count == 0 || session.TramUpgradeList is not List<int> list)
                {
                    return;
                }

                List<int> added = [];
                foreach (int id in configured)
                {
                    if (list.Contains(id))
                    {
                        continue;
                    }

                    list.Add(id);
                    added.Add(id);
                }

                if (added.Count > 0 && !_tramUpgradesLogged)
                {
                    _tramUpgradesLogged = true;
                    SavegamePreparationLog.InfoStartupTramUpgradesApplied(list);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Startup tram upgrades apply failed — {ex.Message}");
            }
        }

        internal static void TryApplyStartupMoney(ref int currency)
        {
            if (!SavegamePreparationNewGameGate.IsArmed
                || StartupMoneyLoadGuard.IsActive
                || StartupMoneyLoadGuard.SuppressStartupScale)
            {
                return;
            }

            if (!EconomyApplier.TryGetVanillaInitialMoney(out int vanillaInitial) || currency != vanillaInitial)
            {
                return;
            }

            int configured = SavegamePreparationResolver.ResolveStartupMoney();
            if (configured == currency)
            {
                return;
            }

            currency = configured;
            SavegamePreparationLog.InfoStartupMoneyApplied(vanillaInitial, configured);
        }

        internal static void OnFirstSaveWritten()
        {
            if (SavegamePreparationNewGameGate.IsArmed)
            {
                SavegamePreparationNewGameGate.Disarm();
            }
        }

        internal static void OnSessionEnded()
        {
            SavegamePreparationNewGameGate.Reset();
            _tramUpgradesLogged = false;
        }

        private static void TrySeedPdataTramUpgradeIds(Hub.PersistentData? pdata)
        {
            try
            {
                List<int> seed = SavegamePreparationResolver.ResolveStartupTramUpgradeIds();
                if (seed.Count == 0 || pdata?.TramUpgradeIDs is not List<int> ids)
                {
                    return;
                }

                ids.Clear();
                ids.AddRange(seed);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Startup tram upgrade pdata seed failed — {ex.Message}");
            }
        }

        private static void TrySetPlayReportAccumulatedStageCount(PlayReportManager playReport, int zone)
        {
            if (PlayReportAccumulatedStageCountSetter?.GetSetMethod(nonPublic: true) is not MethodInfo setter)
            {
                return;
            }

            setter.Invoke(playReport, [zone]);
        }
    }
}

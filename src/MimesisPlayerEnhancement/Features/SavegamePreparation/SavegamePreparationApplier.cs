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

        internal static void OnCreateNewGameInSlot()
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            SavegamePreparationNewGameGate.Arm();

            int zone = SavegamePreparationResolver.ResolveStartingZone();
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
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

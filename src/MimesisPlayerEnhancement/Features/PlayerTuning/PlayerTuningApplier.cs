using System.Reflection;

namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal static class PlayerTuningApplier
    {
        private const string Feature = "PlayerTuning";

        private static readonly FieldInfo? RunStaminaConsumeValueField =
            AccessTools.Field(typeof(DataConsts), "C_RunStaminaConsumeValue");

        private static readonly FieldInfo? StaminaRegenValueField =
            AccessTools.Field(typeof(DataConsts), "C_StaminaRegenValue");

        private static readonly FieldInfo? StaminaRegenDelayEmptyField =
            AccessTools.Field(typeof(DataConsts), "C_StaminaRegenDelayEmpty");

        private static readonly FieldInfo? StaminaRegenDelayRemainField =
            AccessTools.Field(typeof(DataConsts), "C_StaminaRegenDelayRemain");

        private static readonly FieldInfo? MaxCarryWeightField =
            AccessTools.Field(typeof(DataConsts), "C_MaxCarryWeight");

        private static readonly FieldInfo? InventorySelfField =
            AccessTools.Field(typeof(InventoryController), "_self");

        private static readonly FieldInfo? VPlayerDictField =
            AccessTools.Field(typeof(IVroom), "_vPlayerDict");

        private static readonly FieldInfo? StatManagerField =
            AccessTools.Field(typeof(StatController), "StatManager");

        private static bool _vanillaCached;
        private static int _vanillaMaxCarryWeight;
        private static long _vanillaRunStaminaConsumeValue;
        private static long _vanillaStaminaRegenValue;
        private static long _vanillaStaminaRegenDelayEmpty;
        private static long _vanillaStaminaRegenDelayRemain;
        private static bool _runtimeTuningApplied;
        private static bool _wasApplying;

        internal static bool ShouldApply =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => PlayerTuningResolver.IsFeatureEnabled);

        internal static void RefreshFromConfig()
        {
            if (ShouldApply)
            {
                ApplyRuntimeTuning();
                RefreshAllPlayers();
                _wasApplying = true;
            }
            else if (_wasApplying || _runtimeTuningApplied)
            {
                RestoreRuntimeTuning("feature disabled");
                RefreshAllPlayers();
                _wasApplying = false;
            }

            PlayerTuningCollision.RefreshFromConfig();
        }

        internal static void RestoreOnShutdown()
        {
            if (_runtimeTuningApplied)
            {
                RestoreRuntimeTuning("mod shutdown");
            }
        }

        private static int GetEffectiveMaxCarryWeight()
        {
            EnsureVanillaCached();
            return ScalingMath.ScaleCount(_vanillaMaxCarryWeight, PlayerTuningResolver.MaxCarryWeightMultiplier);
        }

        private static int ComputeMoveSpeedDecreaseRateByWeight(int totalWeight)
        {
            if (totalWeight <= 0)
            {
                return 0;
            }

            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return 0;
            }

            int effectiveMax = ShouldApply
                ? GetEffectiveMaxCarryWeight()
                : _vanillaMaxCarryWeight;

            if (effectiveMax <= 0)
            {
                return 0;
            }

            double x = Math.Min((double)totalWeight / effectiveMax, 1.0);
            double thresholdFactor = 1.0 - excel.Consts.C_MinThresholdMoveSpeedRate * 0.0001;
            return (int)(Math.Min(Math.Pow(x, 3.0) * thresholdFactor, thresholdFactor) * 10000.0);
        }

        internal static void ApplyMappedPlayerStats(MappedStats mappedStats)
        {
            if (!ShouldApply)
            {
                return;
            }

            if (!_runtimeTuningApplied)
            {
                ApplyRuntimeTuning();
            }

            ScaleStatElement(mappedStats, StatType.MoveSpeedWalk, PlayerTuningResolver.MoveSpeedMultiplier);
            ScaleStatElement(mappedStats, StatType.MoveSpeedRun, PlayerTuningResolver.MoveSpeedMultiplier);
            ScaleStatElement(mappedStats, StatType.Stamina, PlayerTuningResolver.MaxStaminaMultiplier);
        }

        internal static void ApplyInventoryWeightPenalty(InventoryController inventory)
        {
            if (!ShouldApply)
            {
                return;
            }

            int rate = ComputeMoveSpeedDecreaseRateByWeight(inventory.TotalWeight);
            if (InventorySelfField?.GetValue(inventory) is VCreature creature)
            {
                creature.StatControlUnit?.SetMoveSpeedDecreaseRateByWeight(rate);
            }
        }

        private static void ScaleStatElement(MappedStats mappedStats, StatType statType, float multiplier)
        {
            if (multiplier == 1f)
            {
                return;
            }

            long vanilla = mappedStats.elements[statType].Value;
            long scaled = ScalingMath.ScaleCount((int)vanilla, multiplier);
            mappedStats.elements[statType].Set(scaled);
        }

        private static void ApplyRuntimeTuning()
        {
            if (!ShouldApply || !EnsureVanillaCached())
            {
                return;
            }

            DataConsts? consts = HubGameDataAccess.Excel?.Consts;
            if (consts == null)
            {
                PlayerTuningLog.DebugSkipped("ExcelDataManager.Consts unavailable");
                return;
            }

            SetConstLong(consts, RunStaminaConsumeValueField, ScaleLong(
                _vanillaRunStaminaConsumeValue,
                PlayerTuningResolver.StaminaDrainMultiplier));
            SetConstLong(consts, StaminaRegenValueField, ScaleLong(
                _vanillaStaminaRegenValue,
                PlayerTuningResolver.StaminaRegenMultiplier));
            SetConstLong(consts, StaminaRegenDelayEmptyField, ScaleLong(
                _vanillaStaminaRegenDelayEmpty,
                PlayerTuningResolver.StaminaRegenDelayMultiplier));
            SetConstLong(consts, StaminaRegenDelayRemainField, ScaleLong(
                _vanillaStaminaRegenDelayRemain,
                PlayerTuningResolver.StaminaRegenDelayMultiplier));

            _runtimeTuningApplied = true;
            PlayerTuningLog.InfoAppliedRuntimeTuning();
        }

        private static void RestoreRuntimeTuning(string reason)
        {
            if (!EnsureVanillaCached())
            {
                return;
            }

            DataConsts? consts = HubGameDataAccess.Excel?.Consts;
            if (consts == null)
            {
                return;
            }

            SetConstLong(consts, RunStaminaConsumeValueField, _vanillaRunStaminaConsumeValue);
            SetConstLong(consts, StaminaRegenValueField, _vanillaStaminaRegenValue);
            SetConstLong(consts, StaminaRegenDelayEmptyField, _vanillaStaminaRegenDelayEmpty);
            SetConstLong(consts, StaminaRegenDelayRemainField, _vanillaStaminaRegenDelayRemain);
            _runtimeTuningApplied = false;
            PlayerTuningLog.DebugRestoredRuntimeTuning(reason);
        }

        private static bool EnsureVanillaCached()
        {
            if (_vanillaCached)
            {
                return true;
            }

            DataConsts? consts = HubGameDataAccess.Excel?.Consts;
            if (consts == null)
            {
                return false;
            }

            _vanillaMaxCarryWeight = ReadConstInt(consts, MaxCarryWeightField);
            _vanillaRunStaminaConsumeValue = ReadConstLong(consts, RunStaminaConsumeValueField);
            _vanillaStaminaRegenValue = ReadConstLong(consts, StaminaRegenValueField);
            _vanillaStaminaRegenDelayEmpty = ReadConstLong(consts, StaminaRegenDelayEmptyField);
            _vanillaStaminaRegenDelayRemain = ReadConstLong(consts, StaminaRegenDelayRemainField);
            _vanillaCached = true;
            return true;
        }

        private static long ScaleLong(long vanilla, float multiplier)
        {
            return ScalingMath.ScaleCount((int)vanilla, multiplier);
        }

        private static void SetConstLong(DataConsts consts, FieldInfo? field, long value)
        {
            field?.SetValue(consts, value);
        }

        private static long ReadConstLong(DataConsts consts, FieldInfo? field)
        {
            return field?.GetValue(consts) is long value ? value : 0L;
        }

        private static int ReadConstInt(DataConsts consts, FieldInfo? field)
        {
            return field?.GetValue(consts) is int value ? value : 0;
        }

        private static void RefreshAllPlayers()
        {
            VWorld? vworld = GameSessionAccess.TryGetVWorld();
            VRoomManager? vroomManager = vworld?.VRoomManager;
            if (vroomManager == null)
            {
                return;
            }

            if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not Dictionary<long, IVroom> rooms)
            {
                return;
            }

            int refreshed = 0;
            List<IVroom> roomSnapshot = [.. rooms.Values];
            foreach (IVroom room in roomSnapshot)
            {
                refreshed += RefreshPlayersInRoom(room);
            }

            if (refreshed > 0)
            {
                PlayerTuningLog.DebugRefreshedPlayers(refreshed);
            }
        }

        private static int RefreshPlayersInRoom(IVroom room)
        {
            if (VPlayerDictField?.GetValue(room) is not VActorDict<int, VPlayer> players)
            {
                return 0;
            }

            int count = 0;
            List<VPlayer> playerSnapshot = [.. players.Values];
            foreach (VPlayer player in playerSnapshot)
            {
                if (player == null)
                {
                    continue;
                }

                try
                {
                    ReloadImmutableStats(player);
                    player.InventoryControlUnit?.OnChangeInventory();
                    count++;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Failed to refresh player stats — {ex.Message}");
                }
            }

            return count;
        }

        // Must NOT call StatController.LoadStats(reload: true) here: it runs
        // StatManager.InitMutableStats, which re-adds the room's PlayerTransitionData conta
        // (IncreaseConta adds instead of setting) and resets HP to a stale snapshot. Repeated
        // config changes would ratchet conta to max, killing the player and spawning a mimic.
        // Instead re-run only the immutable/mapped stat loads so the MappedStats Harmony
        // postfix re-applies the tuning multipliers without touching HP/conta/stamina.
        private static void ReloadImmutableStats(VPlayer player)
        {
            if (player.StatControlUnit is not StatController statController
                || StatManagerField?.GetValue(statController) is not StatManager statManager)
            {
                return;
            }

            statManager.LoadMappedStat();
            statManager.LoadEventStats();
            statManager.LoadImmutableStats();
            statManager.SyncImmutableStats();
        }
    }
}

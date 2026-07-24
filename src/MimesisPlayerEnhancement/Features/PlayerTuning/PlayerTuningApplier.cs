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
        private static float _appliedMoveSpeedMultiplier = 1f;
        private static float _appliedMaxStaminaMultiplier = 1f;
        private static float _appliedStaminaDrainMultiplier = 1f;
        private static float _appliedStaminaRegenMultiplier = 1f;
        private static float _appliedStaminaRegenDelayMultiplier = 1f;
        private static float _appliedMaxCarryWeightMultiplier = 1f;

        internal static bool ShouldApply =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => PlayerTuningResolver.IsFeatureEnabled);

        internal static void RefreshFromConfig()
        {
            PlayerTuningConfigSnapshot config = PlayerTuningConfigSnapshot.CaptureFromModConfig();
            bool shouldApply = HostApplyGate.ShouldApplyHostOnlyFeature(
                () => PlayerTuningResolver.GetIsFeatureEnabled(config));

            if (shouldApply)
            {
                if (TryApplyRuntimeTuning(config))
                {
                    RefreshAllPlayers();
                }

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

        internal static void OnSessionEnded()
        {
            if (_runtimeTuningApplied)
            {
                if (TryWriteVanillaConsts())
                {
                    _runtimeTuningApplied = false;
                    ClearAppliedMultipliers();
                    _vanillaCached = false;
                    PlayerTuningLog.DebugRestoredRuntimeTuning("session ended");
                }
                else
                {
                    // Keep cached vanilla so a later re-read cannot treat mutated consts as baseline.
                    // Clear applied so the next session re-writes from the preserved vanilla cache.
                    _runtimeTuningApplied = false;
                    ClearAppliedMultipliers();
                    PlayerTuningLog.DebugSkipped("session ended — restore deferred, Consts unavailable");
                }
            }
            else
            {
                _vanillaCached = false;
            }

            _wasApplying = false;
        }

        private static int GetEffectiveMaxCarryWeight(PlayerTuningConfigSnapshot config)
        {
            EnsureVanillaCached();
            return ScalingMath.ScaleCount(
                _vanillaMaxCarryWeight,
                PlayerTuningResolver.GetMaxCarryWeightMultiplier(config));
        }

        private static int ComputeMoveSpeedDecreaseRateByWeight(
            int totalWeight,
            PlayerTuningConfigSnapshot config)
        {
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return 0;
            }

            int effectiveMax = ShouldApply
                ? GetEffectiveMaxCarryWeight(config)
                : _vanillaMaxCarryWeight;

            return PlayerTuningWeightPenaltyLogic.ComputeRate(
                totalWeight,
                effectiveMax,
                excel.Consts.C_MinThresholdMoveSpeedRate);
        }

        internal static void ApplyMappedPlayerStats(MappedStats mappedStats)
        {
            if (!ShouldApply)
            {
                return;
            }

            PlayerTuningConfigSnapshot config = PlayerTuningConfigSnapshot.CaptureFromModConfig();
            if (!_runtimeTuningApplied)
            {
                TryApplyRuntimeTuning(config);
            }

            float moveSpeed = PlayerTuningResolver.GetMoveSpeedMultiplier(config);
            float maxStamina = PlayerTuningResolver.GetMaxStaminaMultiplier(config);
            ScaleStatElement(mappedStats, StatType.MoveSpeedWalk, moveSpeed);
            ScaleStatElement(mappedStats, StatType.MoveSpeedRun, moveSpeed);
            ScaleStatElement(mappedStats, StatType.Stamina, maxStamina);
        }

        internal static void ApplyInventoryWeightPenalty(InventoryController inventory)
        {
            if (!ShouldApply)
            {
                return;
            }

            PlayerTuningConfigSnapshot config = PlayerTuningConfigSnapshot.CaptureFromModConfig();
            int rate = ComputeMoveSpeedDecreaseRateByWeight(inventory.TotalWeight, config);
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

        /// <summary>
        /// Writes scaled consts when needed. Returns true when player stats should be refreshed
        /// (first apply or any host-gated multiplier changed).
        /// </summary>
        private static bool TryApplyRuntimeTuning(PlayerTuningConfigSnapshot config)
        {
            if (!ShouldApply || !EnsureVanillaCached())
            {
                return false;
            }

            float moveSpeed = PlayerTuningResolver.GetMoveSpeedMultiplier(config);
            float maxStamina = PlayerTuningResolver.GetMaxStaminaMultiplier(config);
            float drain = PlayerTuningResolver.GetStaminaDrainMultiplier(config);
            float regen = PlayerTuningResolver.GetStaminaRegenMultiplier(config);
            float regenDelay = PlayerTuningResolver.GetStaminaRegenDelayMultiplier(config);
            float carryWeight = PlayerTuningResolver.GetMaxCarryWeightMultiplier(config);

            bool multipliersUnchanged = _runtimeTuningApplied
                && _appliedMoveSpeedMultiplier == moveSpeed
                && _appliedMaxStaminaMultiplier == maxStamina
                && _appliedStaminaDrainMultiplier == drain
                && _appliedStaminaRegenMultiplier == regen
                && _appliedStaminaRegenDelayMultiplier == regenDelay
                && _appliedMaxCarryWeightMultiplier == carryWeight;
            if (multipliersUnchanged)
            {
                return false;
            }

            DataConsts? consts = HubGameDataAccess.Excel?.Consts;
            if (consts == null)
            {
                PlayerTuningLog.DebugSkipped("ExcelDataManager.Consts unavailable");
                return false;
            }

            SetConstLong(consts, RunStaminaConsumeValueField, ScaleLong(_vanillaRunStaminaConsumeValue, drain));
            SetConstLong(consts, StaminaRegenValueField, ScaleLong(_vanillaStaminaRegenValue, regen));
            SetConstLong(consts, StaminaRegenDelayEmptyField, ScaleLong(_vanillaStaminaRegenDelayEmpty, regenDelay));
            SetConstLong(consts, StaminaRegenDelayRemainField, ScaleLong(_vanillaStaminaRegenDelayRemain, regenDelay));
            SetConstInt(consts, MaxCarryWeightField, ScalingMath.ScaleCount(_vanillaMaxCarryWeight, carryWeight));

            _runtimeTuningApplied = true;
            _appliedMoveSpeedMultiplier = moveSpeed;
            _appliedMaxStaminaMultiplier = maxStamina;
            _appliedStaminaDrainMultiplier = drain;
            _appliedStaminaRegenMultiplier = regen;
            _appliedStaminaRegenDelayMultiplier = regenDelay;
            _appliedMaxCarryWeightMultiplier = carryWeight;
            PlayerTuningLog.InfoAppliedRuntimeTuning(config);
            return true;
        }

        private static void RestoreRuntimeTuning(string reason)
        {
            if (!EnsureVanillaCached())
            {
                return;
            }

            if (!TryWriteVanillaConsts())
            {
                PlayerTuningLog.DebugSkipped($"{reason} — restore deferred, Consts unavailable");
                return;
            }

            _runtimeTuningApplied = false;
            ClearAppliedMultipliers();
            PlayerTuningLog.DebugRestoredRuntimeTuning(reason);
        }

        private static bool TryWriteVanillaConsts()
        {
            if (!_vanillaCached)
            {
                return false;
            }

            DataConsts? consts = HubGameDataAccess.Excel?.Consts;
            if (consts == null)
            {
                return false;
            }

            SetConstLong(consts, RunStaminaConsumeValueField, _vanillaRunStaminaConsumeValue);
            SetConstLong(consts, StaminaRegenValueField, _vanillaStaminaRegenValue);
            SetConstLong(consts, StaminaRegenDelayEmptyField, _vanillaStaminaRegenDelayEmpty);
            SetConstLong(consts, StaminaRegenDelayRemainField, _vanillaStaminaRegenDelayRemain);
            SetConstInt(consts, MaxCarryWeightField, _vanillaMaxCarryWeight);
            return true;
        }

        private static void ClearAppliedMultipliers()
        {
            _appliedMoveSpeedMultiplier = 1f;
            _appliedMaxStaminaMultiplier = 1f;
            _appliedStaminaDrainMultiplier = 1f;
            _appliedStaminaRegenMultiplier = 1f;
            _appliedStaminaRegenDelayMultiplier = 1f;
            _appliedMaxCarryWeightMultiplier = 1f;
        }

        private static bool EnsureVanillaCached()
        {
            if (_vanillaCached)
            {
                return true;
            }

            // Never sample consts as vanilla while a prior apply may still be live in memory
            // without a successful write-back (session-end deferral keeps the prior cache).
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

        private static void SetConstInt(DataConsts consts, FieldInfo? field, int value)
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

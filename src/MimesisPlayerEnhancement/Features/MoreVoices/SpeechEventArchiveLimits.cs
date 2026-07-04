using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Maps mod config to <see cref="SpeechEventArchive"/> voice pool fields.
    /// Game logic: indoor cap = maxEvents - maxDeathMatchEvents - maxOutDoorEvents
    /// (see deps/decompiled/.../SpeechEventArchive.cs, RemoveLowerValueEventsIfExceeded).
    /// </summary>
    internal static class SpeechEventArchiveLimits
    {
        private const string Feature = "MoreVoices";

        // Vanilla SerializeField defaults on SpeechEventArchive.
        internal const int VanillaMaxEvents = 128;
        internal const int VanillaMaxDeathMatchEvents = 20;
        internal const int VanillaMaxOutDoorEvents = 30;

        private static readonly FieldInfo? MaxEventsField =
            AccessTools.Field(typeof(SpeechEventArchive), "maxEvents");

        private static readonly FieldInfo? MaxDeathMatchField =
            AccessTools.Field(typeof(SpeechEventArchive), "maxDeathMatchEvents");

        private static readonly FieldInfo? MaxOutDoorField =
            AccessTools.Field(typeof(SpeechEventArchive), "maxOutDoorEvents");

        private static readonly MethodInfo? RemoveLowerValueEventsIfExceededMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded");

        internal readonly struct PoolLimits
        {
            internal PoolLimits(int maxEvents, int maxDeathMatch, int maxOutdoor)
            {
                MaxEvents = maxEvents;
                MaxDeathMatch = maxDeathMatch;
                MaxOutdoor = maxOutdoor;
            }

            internal int MaxEvents { get; }
            internal int MaxDeathMatch { get; }
            internal int MaxOutdoor { get; }
            internal int IndoorCap => MaxEvents - MaxDeathMatch - MaxOutdoor;
        }

        internal readonly struct EffectiveCaps
        {
            internal EffectiveCaps(int indoor, int deathMatch, int outdoor)
            {
                Indoor = indoor;
                DeathMatch = deathMatch;
                Outdoor = outdoor;
            }

            internal int Indoor { get; }
            internal int DeathMatch { get; }
            internal int Outdoor { get; }

            internal bool AnyDecreasedComparedTo(EffectiveCaps other)
            {
                return Indoor < other.Indoor
                       || DeathMatch < other.DeathMatch
                       || Outdoor < other.Outdoor;
            }
        }

        internal static bool FieldsAvailable =>
            MaxEventsField != null && MaxDeathMatchField != null && MaxOutDoorField != null;

        internal static PoolLimits? ResolveFromConfig()
        {
            if (!ModConfig.EnableMoreVoices.Value)
            {
                return null;
            }

            int indoor = ModConfig.MaxIndoorVoiceEvents.Value;
            int deathMatch = ModConfig.MaxDeathMatchVoiceEvents.Value;
            int outdoor = ModConfig.MaxOutdoorVoiceEvents.Value;
            return new PoolLimits(indoor + deathMatch + outdoor, deathMatch, outdoor);
        }

        internal static EffectiveCaps ReadEffectiveCaps(SpeechEventArchive archive)
        {
            int maxEvents = ReadField(archive, MaxEventsField);
            int maxDeathMatch = ReadField(archive, MaxDeathMatchField);
            int maxOutdoor = ReadField(archive, MaxOutDoorField);
            return new EffectiveCaps(
                maxEvents - maxDeathMatch - maxOutdoor,
                maxDeathMatch,
                maxOutdoor);
        }

        internal static EffectiveCaps ToEffectiveCaps(PoolLimits limits)
        {
            return new EffectiveCaps(limits.IndoorCap, limits.MaxDeathMatch, limits.MaxOutdoor);
        }

        internal static string FormatEffectiveCaps(EffectiveCaps caps)
        {
            return $"indoor={caps.Indoor}, deathmatch={caps.DeathMatch}, outdoor={caps.Outdoor}";
        }

        internal static bool TryRestoreVanilla(SpeechEventArchive archive, bool retrimOnDecrease)
        {
            if (archive == null || !FieldsAvailable)
            {
                return false;
            }

            var limits = new PoolLimits(
                VanillaMaxEvents,
                VanillaMaxDeathMatchEvents,
                VanillaMaxOutDoorEvents);

            try
            {
                EffectiveCaps before = ReadEffectiveCaps(archive);
                if (!WriteFields(archive, limits))
                {
                    return false;
                }

                EffectiveCaps after = ToEffectiveCaps(limits);
                if (retrimOnDecrease && after.AnyDecreasedComparedTo(before))
                {
                    TryRetrimLocalArchive(archive);
                }

                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Failed to restore vanilla voice limits: {ex.Message}");
                return false;
            }
        }

        /// <summary>Writes current config limits to archive fields. No re-trim.</summary>
        internal static bool TryApplyFields(SpeechEventArchive archive)
        {
            PoolLimits? limits = ResolveFromConfig();
            if (limits == null || archive == null)
            {
                return false;
            }

            return WriteFields(archive, limits.Value);
        }

        internal static bool TryApply(SpeechEventArchive archive, bool retrimOnDecrease)
        {
            if (archive == null)
            {
                return false;
            }

            PoolLimits? limits = ResolveFromConfig();
            if (limits == null)
            {
                return false;
            }

            try
            {
                EffectiveCaps before = ReadEffectiveCaps(archive);
                if (!WriteFields(archive, limits.Value))
                {
                    return false;
                }

                EffectiveCaps after = ToEffectiveCaps(limits.Value);
                if (retrimOnDecrease && before.AnyDecreasedComparedTo(after))
                {
                    TryRetrimLocalArchive(archive);
                }

                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Failed to apply voice limits: {ex.Message}");
                return false;
            }
        }

        internal static bool TryRetrimLocalArchive(SpeechEventArchive archive)
        {
            if (archive == null || RemoveLowerValueEventsIfExceededMethod == null)
            {
                return false;
            }

            bool isLocal;
            try
            {
                isLocal = archive.IsLocal;
            }
            catch
            {
                return false;
            }

            if (!isLocal)
            {
                return false;
            }

            try
            {
                if (RemoveLowerValueEventsIfExceededMethod.Invoke(archive, null) is not List<long> removed
                    || removed.Count == 0)
                {
                    return false;
                }

                ModLog.Info(Feature, $"Re-trimmed {removed.Count} voice event(s) after limit decrease — " +
                    $"{VoiceEventStats.DescribePlayer(archive)}");
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Voice pool re-trim failed: {ex.Message}");
                return false;
            }
        }

        private static bool WriteFields(SpeechEventArchive archive, PoolLimits limits)
        {
            if (!FieldsAvailable)
            {
                throw new MissingFieldException(typeof(SpeechEventArchive).FullName, "voice limit field");
            }

            MaxEventsField!.SetValue(archive, limits.MaxEvents);
            MaxDeathMatchField!.SetValue(archive, limits.MaxDeathMatch);
            MaxOutDoorField!.SetValue(archive, limits.MaxOutdoor);
            return true;
        }

        private static int ReadField(SpeechEventArchive archive, FieldInfo? field)
        {
            if (field == null)
            {
                throw new MissingFieldException(typeof(SpeechEventArchive).FullName, "voice limit field");
            }

            return (int)field.GetValue(archive)!;
        }
    }
}

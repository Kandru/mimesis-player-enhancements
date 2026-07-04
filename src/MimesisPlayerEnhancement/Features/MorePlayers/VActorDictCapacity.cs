using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    /// <summary>
    /// IVroom stores players in <c>VActorDict&lt;int, VPlayer&gt;(10)</c> — a hard cap unrelated to
    /// <c>C_MaxPlayerCount</c>. MorePlayers raises session/network limits but must also raise this.
    /// </summary>
    internal static class VActorDictCapacity
    {
        private const string Feature = "MorePlayers";
        private const int VanillaRoomPlayerDictCap = 10;

        private static readonly FieldInfo? VPlayerDictField =
            AccessTools.Field(typeof(IVroom), "_vPlayerDict");

        private static readonly FieldInfo? MaxCountField =
            typeof(VActorDict<int, VPlayer>).GetField(
                "m_MaxCount",
                BindingFlags.Instance | BindingFlags.NonPublic);

        internal static int ResolveCap()
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return VanillaRoomPlayerDictCap;
            }

            return Math.Max(VanillaRoomPlayerDictCap, MorePlayersPatches.GetMaxPlayers());
        }

        internal static void ApplyToRoom(IVroom? room)
        {
            if (room == null || VPlayerDictField == null || MaxCountField == null)
            {
                return;
            }

            if (VPlayerDictField.GetValue(room) is not VActorDict<int, VPlayer> players)
            {
                return;
            }

            int cap = ResolveCap();
            if (MaxCountField.GetValue(players) is int current && current >= cap)
            {
                return;
            }

            MaxCountField.SetValue(players, cap);
            ModLog.Debug(Feature, $"Room _vPlayerDict cap set to {cap} (room={room.RoomID}).");
        }

        internal static void ApplyToAllRooms()
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return;
            }

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

            foreach (IVroom room in rooms.Values)
            {
                ApplyToRoom(room);
            }
        }
    }
}

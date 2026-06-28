using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ReluNetwork.ConstEnum;
using ReluProtocol;
using ReluProtocol.C2S;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Late-join flow adapted from Shlygly/MimesisJoinAnytime for MIMESIS 0.3.0.
    /// </summary>
    internal static class LateJoinManager
    {
        private const string Feature = "JoinAnytime";

        internal static LateJoinRedirectState RedirectState { get; set; } = LateJoinRedirectState.None;
        internal static string PendingSceneName { get; set; } = string.Empty;

        private static bool voiceCheckRunning;
        private static readonly HashSet<long> SentPlayingStateUids = [];

        internal static bool IsEnabled => ModConfig.EnableJoinAnytime.Value;

        internal static void OnServerLogin(SessionContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.ClientMode != NetworkClientMode.Host)
            {
                return;
            }

            if (pdata.main is not GamePlayScene gps)
            {
                return;
            }

            ModLog.Debug(
                Feature,
                $"Login while in-game — uid={context.GetPlayerUID()} dungeon={gps.DungeonMasterID} seed={gps.RandDungeonSeed}");

            JoinAnytimeNetworkTools.SendOnPlayingStateToClient(context);
        }

        internal static void OnServerPlayerCreated(VPlayer player)
        {
            if (!IsEnabled)
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.ClientMode != NetworkClientMode.Host || player.IsHost)
            {
                return;
            }

            ModLog.Debug(Feature, $"VPlayer created: uid={player.UID} room={player.VRoom?.GetType().Name} main={pdata.main?.GetType().Name}");

            if (pdata.main is InTramWaitingScene
                && player.VRoom is MaintenanceRoom)
            {
                ModLog.Info(Feature, $"Pre-game join detected, sending MoveToWaitingRoomSig to uid={player.UID}");
                _ = player.SendToMe(new MoveToWaitingRoomSig());
                return;
            }

            if (pdata.main is GamePlayScene
                && player.VRoom is MaintenanceRoom)
            {
                JoinAnytimeNetworkTools.SendOnPlayingStateToClient(player);
            }
        }

        internal static bool OnClientPacket(IMsg msg)
        {
            if (!IsEnabled)
            {
                return true;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();

            switch (msg)
            {
                case AllMemberEnterRoomSig all:
                    if (ShouldIgnoreEarlyGameAllMemberEnterRoomSig(pdata, all))
                    {
                        return false;
                    }

                    break;

                case MoveToWaitingRoomSig:
                    if (pdata?.ClientMode != NetworkClientMode.Participant)
                    {
                        return true;
                    }

                    ModLog.Debug(Feature, $"Pre-game redirect signal received. main={pdata.main?.GetType().Name ?? "null"}");

                    RedirectState = LateJoinRedirectState.PendingWaitingRoomRedirect;
                    PendingSceneName = "InTramWaitingScene";

                    if (pdata.main is MaintenanceScene)
                    {
                        RedirectState = LateJoinRedirectState.None;
                        Hub.LoadScene("InTramWaitingScene");
                    }

                    break;

                case MoveToDungeonSig moveToDungeonSig:
                    if (pdata == null)
                    {
                        return true;
                    }

                    pdata.dungeonMasterID = moveToDungeonSig.selectedDungeonMasterID;
                    pdata.randDungeonSeed = moveToDungeonSig.randDungeonSeed;
                    pdata.PickedMapID = moveToDungeonSig.pickedMapID;
                    ModLog.Debug(
                        Feature,
                        $"MoveToDungeonSig: dungeon={pdata.dungeonMasterID} map={pdata.PickedMapID} seed={pdata.randDungeonSeed}");
                    break;

                case MoveToMaintenanceRoomSig:
                case ReturnMembertoMaintenenceRoomSig:
                case DungeonResultSig:
                    ResetJoinState();
                    break;

                case MakeRoomCompleteSig completeSig:
                    if (pdata == null)
                    {
                        return true;
                    }

                    pdata.completeMakingRoomSig = completeSig;

                    if (completeSig.nextRoomInfo.roomMasterID != 0)
                    {
                        pdata.dungeonMasterID = completeSig.nextRoomInfo.roomMasterID;
                    }

                    if (completeSig.nextRoomInfo.roomType != VRoomType.Game)
                    {
                        ModLog.Debug(
                            Feature,
                            $"MakeRoomCompleteSig roomType={completeSig.nextRoomInfo.roomType} — skipping dungeon redirect");
                        if (completeSig.nextRoomInfo.roomType == VRoomType.Maintenance)
                        {
                            ResetJoinState();
                        }

                        break;
                    }

                    if (pdata.main is GamePlayScene)
                    {
                        ModLog.Debug(Feature, "MakeRoomCompleteSig for Game room while already in GamePlayScene — no redirect");
                        break;
                    }

                    string sceneName = JoinAnytimeRoomTools.GetSceneNameFromDungeon(
                        pdata.dungeonMasterID,
                        pdata.PickedMapID);
                    ModLog.Debug(
                        Feature,
                        $"MakeRoomCompleteSig — roomUID={completeSig.nextRoomInfo.roomUID}, map={pdata.PickedMapID}, scene={sceneName}, main={pdata.main?.GetType().Name ?? "null"}");

                    PendingSceneName = sceneName;

                    if (pdata.ClientMode == NetworkClientMode.Participant)
                    {
                        if (pdata.main is MaintenanceScene && !string.IsNullOrEmpty(sceneName))
                        {
                            ModLog.Info(Feature, $"Redirect now -> {sceneName}");
                            RedirectState = LateJoinRedirectState.EnteringDungeon;
                            Hub.LoadScene(sceneName);
                        }
                        else if (pdata.main is not GamePlayScene)
                        {
                            RedirectState = LateJoinRedirectState.PendingDungeonRedirect;
                        }
                    }

                    break;
            }

            return true;
        }

        private static bool ShouldIgnoreEarlyGameAllMemberEnterRoomSig(
            Hub.PersistentData? pdata,
            AllMemberEnterRoomSig sig)
        {
            if (pdata?.ClientMode != NetworkClientMode.Participant)
            {
                return false;
            }

            if (pdata.main is not GamePlayScene gamePlayScene)
            {
                return false;
            }

            FieldInfo enteringCompleteAllField = typeof(GamePlayScene).GetField(
                "EnteringCompleteAll",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (enteringCompleteAllField == null)
            {
                ModLog.Warn(Feature, "EnteringCompleteAll field not found on GamePlayScene");
                return false;
            }

            if (enteringCompleteAllField.GetValue(gamePlayScene) is true)
            {
                return false;
            }

            if (RedirectState != LateJoinRedirectState.EnteringDungeon)
            {
                return false;
            }

            bool hasArriveCutscene =
                sig.enterCutsceneNames != null
                && sig.enterCutsceneNames.Contains("ArriveCutScene");

            if (hasArriveCutscene)
            {
                ResetJoinState();
                return false;
            }

            ModLog.Debug(Feature, $"Ignoring early AllMemberEnterRoomSig names={string.Join(",", sig.enterCutsceneNames ?? [])}");
            return true;
        }

        internal static void OnLoadScene(string sceneName)
        {
            if (!IsEnabled || RedirectState == LateJoinRedirectState.None)
            {
                return;
            }

            if (RedirectState == LateJoinRedirectState.EnteringDungeon
                && sceneName == PendingSceneName)
            {
                return;
            }

            if (sceneName != PendingSceneName)
            {
                ModLog.Warn(Feature, $"Redirect aborted — loaded {sceneName}, expected {PendingSceneName}");
                ResetJoinState();
            }
        }

        internal static void ResetJoinState()
        {
            RedirectState = LateJoinRedirectState.None;
            PendingSceneName = string.Empty;
            voiceCheckRunning = false;
            SentPlayingStateUids.Clear();
        }

        internal static void OnMaintenanceSceneStart()
        {
            if (!IsEnabled)
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata == null)
            {
                return;
            }

            if (RedirectState == LateJoinRedirectState.PendingWaitingRoomRedirect)
            {
                ModLog.Info(Feature, "Pending pre-game redirect -> InTramWaitingScene");
                RedirectState = LateJoinRedirectState.None;
                Hub.LoadScene("InTramWaitingScene");
            }
            else if (RedirectState == LateJoinRedirectState.PendingDungeonRedirect)
            {
                if (string.IsNullOrEmpty(PendingSceneName))
                {
                    ModLog.Warn(Feature, "Pending in-game redirect skipped — scene name is empty");
                    return;
                }

                ModLog.Info(Feature, $"Pending in-game redirect -> {PendingSceneName}");
                RedirectState = LateJoinRedirectState.EnteringDungeon;
                Hub.LoadScene(PendingSceneName);
            }
        }

        internal static void EnsureVoiceConnected(VoiceManager voiceManager, VoiceMode mode)
        {
            if (!IsEnabled || voiceCheckRunning)
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.ClientMode != NetworkClientMode.Participant)
            {
                return;
            }

            bool sceneAllowed = pdata.main switch
            {
                InTramWaitingScene => true,
                GamePlayScene => true,
                DeathMatchScene => true,
                _ => false,
            };

            if (!sceneAllowed || voiceManager.IsConnected())
            {
                return;
            }

            ModLog.Debug(Feature, $"Voice reconnect: addr={pdata.GameServerAddressOrSteamId} relay={pdata.WithRelay}");
            _ = voiceManager.StartCoroutine(ConnectVoiceDelayed(voiceManager));
        }

        private static IEnumerator ConnectVoiceDelayed(VoiceManager voiceManager)
        {
            voiceCheckRunning = true;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                yield return new WaitForSeconds(0.5f * attempt);

                Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
                if (pdata == null || voiceManager == null || voiceManager.IsConnected())
                {
                    break;
                }

                voiceManager.TryRecoverMicrophone();
                voiceManager.ConnectVoiceChat(pdata.GameServerAddressOrSteamId, pdata.WithRelay);

                yield return new WaitForSeconds(1f);
                if (voiceManager.IsConnected())
                {
                    ModLog.Info(Feature, $"Voice reconnected on attempt {attempt}");
                    break;
                }

                if (attempt == 3)
                {
                    ModLog.Warn(Feature, "Voice reconnect failed after 3 attempts");
                }
            }

            voiceCheckRunning = false;
        }

        internal static void OnServerEnterWaitingRoom(SessionContext context)
        {
            if (!IsEnabled || context == null || !context.ExistPlayer())
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (context.GetVRoomType() == VRoomType.Maintenance
                && pdata?.main is InTramWaitingScene)
            {
                ModLog.Debug(Feature, "Moving player snapshot Maintenance -> Waiting");
                JoinAnytimeRoomTools.MoveCurrentPlayerToSnapshot(context);
            }
        }

        internal static void OnServerEnterDungeon(SessionContext context, long roomUid)
        {
            if (!IsEnabled || context == null || !context.ExistPlayer())
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (context.GetVRoomType() == VRoomType.Maintenance
                && pdata?.main is GamePlayScene)
            {
                ModLog.Debug(Feature, $"Moving player snapshot Maintenance -> Dungeon, roomUID={roomUid}");
                JoinAnytimeRoomTools.MoveCurrentPlayerToSnapshot(context);
            }
        }

        internal static void OnServerEnterMaintenance(SessionContext context)
        {
            if (!IsEnabled || context == null || !context.ExistPlayer())
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (context.GetVRoomType() == VRoomType.Game
                && pdata?.main is MaintenanceScene)
            {
                ModLog.Debug(Feature, "Moving player snapshot Dungeon -> Maintenance");
                JoinAnytimeRoomTools.MoveCurrentPlayerToSnapshot(context);
            }
        }

        internal static bool TryMarkPlayingStateSent(long uid)
        {
            if (!SentPlayingStateUids.Add(uid))
            {
                ModLog.Debug(Feature, $"Skipping duplicate in-game state signal for uid={uid}");
                return false;
            }

            return true;
        }

        internal static void RefreshLobbyVisibilityAfterSteamUpdate()
        {
            LobbyVisibilityHelper.RefreshAfterLobbyDataUpdate();
        }
    }
}

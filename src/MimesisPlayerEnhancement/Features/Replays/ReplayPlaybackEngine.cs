using System.Collections;
using System.IO;
using System.Reflection;
using MelonLoader;
using ReluNetwork.ConstEnum;
using ReluProtocol.Enum;
using ReluReplay.Data;
using ReluReplay.Shared;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal enum ReplayUiVisibilityMode
    {
        BothVisible = 0,
        HudHidden = 1,
        BothHidden = 2,
    }

    internal static class ReplayPlaybackEngine
    {
        private const string Feature = "Replays";
        private const string MainMenuSceneName = "MainMenuScene";

        private static ReplayData? _replayData;
        private static ReplayLevelObjectRemapper _remapper = new();
        private static readonly Queue<IMsg> _pendingMessages = new();
        private static readonly HashSet<long> _processedTimelineIndices = new();

        private static bool _isActive;
        private static bool _isPaused;
        private static bool _isFastForwarding;
        private static bool _sceneReady;
        private static bool _spectatorStarted;
        private static bool _dataLoaded;
        private static int _startSpawnIndex;
        private static int _timelineIndex;
        private static long _currentAbsoluteTimeMs;
        private static long _recordStartTimeMs;
        private static long _recordEndTimeMs;
        private static float _playbackSpeed = 1f;
        private static string? _pendingPlayPath;
        private static ReplayHudUi? _hud;
        private static bool _sceneLoadedHandled;
        private static int _loadGeneration;
        private static int _seekReloadGeneration;
        private static long _pendingSeekTargetTime = -1;
        private static bool _presentationScheduled;
        private static ReplayUiVisibilityMode _uiVisibilityMode = ReplayUiVisibilityMode.BothVisible;

        private static readonly MethodInfo? ChangeSpectatorTargetMethod =
            AccessTools.Method(typeof(CameraManager), "ChangeSpectatorCameraTarget", [typeof(int)]);

        internal static bool IsActive => _isActive;

        internal static bool IsPaused => _isPaused;

        internal static float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Mathf.Clamp(value, 0.25f, 4f);
        }

        internal static ReplayLevelObjectRemapper Remapper => _remapper;

        internal static void BeginPlayback(string playFilePath)
        {
            if (!ReplaysRuntime.IsEnabled)
            {
                return;
            }

            IReplayHeader? header = ReplayData.LoadReplayHeaderData(playFilePath);
            if (header == null)
            {
                ModLog.Warn(Feature, $"Failed to load replay header — {playFilePath}");
                return;
            }

            if (header.GetReplayType() != E_GAME_MODE.INGAME)
            {
                ModLog.Warn(Feature, "Only dungeon (InGame) replays are supported.");
                return;
            }

            StopPlayback(silent: true);
            _pendingPlayPath = playFilePath;
            _recordStartTimeMs = header.GetReplayRecordStartTime();
            _recordEndTimeMs = header.GetReplayRecordEndTime();
            _dataLoaded = false;
            _sceneReady = false;
            _spectatorStarted = false;
            _sceneLoadedHandled = false;
            _timelineIndex = 0;
            _startSpawnIndex = 0;
            _processedTimelineIndices.Clear();
            _pendingMessages.Clear();
            _isPaused = false;
            _isFastForwarding = false;
            _playbackSpeed = 1f;
            _currentAbsoluteTimeMs = _recordStartTimeMs;
            _uiVisibilityMode = ReplayUiVisibilityMode.BothVisible;
            _presentationScheduled = false;

            ReplaySharedData.Clear();
            ReplaySharedData.SetPlayMode();

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null)
            {
                ModLog.Warn(Feature, "Player data unavailable — cannot start replay.");
                StopPlayback(silent: true);
                return;
            }

            pdata.dungeonMasterID = header.GetDungeonMasterID();
            pdata.randDungeonSeed = header.GetDungeonRandSeed();
            pdata.PickedMapID = header.GetPickedMapID();
            pdata.TramUpgradeIDs = header.GetSaveInfo()?.TramUpgradeIDs != null
                ? new List<int>(header.GetSaveInfo()!.TramUpgradeIDs)
                : [];
            pdata.ClientMode = NetworkClientMode.Host;
            pdata.lastResponseError = MsgErrorCode.Success;
            pdata.completeMakingRoomSig = null;

            if (!ReplayGameAccess.TryResolveGameplaySceneName(
                    pdata.dungeonMasterID,
                    pdata.PickedMapID,
                    out string sceneName))
            {
                ModLog.Warn(
                    Feature,
                    $"Gameplay scene missing for replay — masterId={pdata.dungeonMasterID}, mapId={pdata.PickedMapID}");
                StopPlayback(silent: true);
                return;
            }

            int generation = ++_loadGeneration;
            _replayData = new ReplayData(playFilePath);
            _replayData.LoadReplayData(() => OnReplayDataLoaded(generation, playFilePath));
            _isActive = true;
            ModLog.Info(Feature, $"Loading replay — {Path.GetFileName(playFilePath)}");
            ModLog.Info(Feature, $"Loading replay scene — {sceneName}");
            Hub.LoadScene(sceneName);
        }

        internal static void OnUpdate()
        {
            if (!ReplaysRuntime.IsEnabled || !_isActive)
            {
                return;
            }

            if (ReplayGameAccess.WasF10PressedThisFrame())
            {
                CycleUiVisibility();
            }

            if (_replayData == null || !_dataLoaded || !_sceneReady)
            {
                return;
            }

            if (!_isPaused && !_isFastForwarding)
            {
                long deltaMs = (long)(Time.unscaledDeltaTime * 1000f * _playbackSpeed);
                _currentAbsoluteTimeMs = Math.Min(_recordEndTimeMs, _currentAbsoluteTimeMs + deltaMs);
            }

            PumpTimeline(upToTime: _currentAbsoluteTimeMs, skipVoice: _isFastForwarding);
            UpdateSharedTimeSlider();
            _hud?.Refresh();
            TryEnsureSpectator();
            EnforceUiVisibility();
        }

        internal static void ScheduleScenePresentationReady()
        {
            if (!_isActive || _sceneLoadedHandled || _presentationScheduled)
            {
                return;
            }

            _presentationScheduled = true;
            MelonCoroutines.Start(WaitForScenePresentationReady());
        }

        internal static bool TryDequeueMessage(out IMsg msg)
        {
            msg = null!;
            if (!_isActive || _replayData == null || !_sceneReady)
            {
                return false;
            }

            if (_pendingMessages.Count == 0)
            {
                PumpTimeline(upToTime: _currentAbsoluteTimeMs, skipVoice: _isFastForwarding);
            }

            if (_pendingMessages.Count == 0)
            {
                return false;
            }

            msg = _pendingMessages.Dequeue();
            return true;
        }

        internal static void OnScenePresentationReady()
        {
            if (!_isActive || _replayData == null || _sceneLoadedHandled || !_dataLoaded)
            {
                return;
            }

            GameMainBase? main = ReplayGameAccess.TryGetMain();
            if (main is not GamePlayScene and not DeathMatchScene)
            {
                return;
            }

            _sceneLoadedHandled = true;
            ReplayHeader? header = _replayData.ReplayHeader as ReplayHeader;
            if (header?.BTActionNameTable != null)
            {
                BTActionRegistry.InitializeFromTable(header.BTActionNameTable);
            }

            _remapper.BuildFromHeader(header);
            ReplaySharedData.SetEmotionData(_replayData.DebugMimicVoiceEmotionData);
            _sceneReady = true;
            _timelineIndex = 0;
            _startSpawnIndex = 0;
            _processedTimelineIndices.Clear();
            _pendingMessages.Clear();

            long resumeTime = _pendingSeekTargetTime >= _recordStartTimeMs
                ? _pendingSeekTargetTime
                : _recordStartTimeMs;
            _pendingSeekTargetTime = -1;
            _currentAbsoluteTimeMs = resumeTime;

            PumpStartSpawnData();
            if (resumeTime > _recordStartTimeMs)
            {
                _isFastForwarding = true;
                PumpTimeline(upToTime: resumeTime, skipVoice: true);
                _isFastForwarding = false;
            }

            UpdateSharedTimeSlider();
            _hud?.Destroy();
            _hud = ReplayHudUi.Create();
            ApplyUiVisibility();
            MelonCoroutines.Start(RetrySpectatorCoroutine());
            ModLog.Info(Feature, "Replay playback started.");
            _isFastForwarding = false;
        }

        internal static void OnSceneLoadedComplete() => ScheduleScenePresentationReady();

        internal static void CycleUiVisibility()
        {
            _uiVisibilityMode = (ReplayUiVisibilityMode)(((int)_uiVisibilityMode + 1) % 3);
            ApplyUiVisibility();
        }

        internal static void OnPlayerSpawned(ProtoActor actor)
        {
            if (!_isActive || actor == null || !actor.IsPlayer())
            {
                return;
            }

            TryEnsureSpectator();
        }

        internal static int GetReplayDungeonMasterId() => _replayData?.NextDungeonMasterID ?? -1;

        internal static int GetReplayRandSeed() => _replayData?.RandDungeonSeed ?? -1;

        internal static int GetReplayPickedMapId() => _replayData?.PickedMapID ?? 0;

        internal static void TogglePause() => _isPaused = !_isPaused;

        internal static void CycleSpectatorTarget(int delta)
        {
            if (!_isActive || delta == 0)
            {
                return;
            }

            CameraManager? camera = ReplayGameAccess.TryGetCameraManager();
            if (camera == null || ChangeSpectatorTargetMethod == null)
            {
                return;
            }

            bool enteredSpectator = ReplayGameAccess.TryEnterSpectatorMode();
            if (enteredSpectator)
            {
                _spectatorStarted = true;
            }

            if (camera.Mode == CameraManager.CameraMode.Normal)
            {
                return;
            }

            ChangeSpectatorTargetMethod.Invoke(camera, [delta]);
        }

        internal static void SeekToNormalized(float normalized)
        {
            if (_replayData == null || _recordEndTimeMs <= _recordStartTimeMs)
            {
                return;
            }

            long targetTime = _recordStartTimeMs
                + (long)((_recordEndTimeMs - _recordStartTimeMs) * Mathf.Clamp01(normalized));
            SeekToAbsoluteTime(targetTime);
        }

        internal static void StopPlayback(bool silent = false)
        {
            bool wasActive = _isActive;
            _loadGeneration++;
            _seekReloadGeneration++;
            _isActive = false;
            _sceneReady = false;
            _dataLoaded = false;
            _spectatorStarted = false;
            _sceneLoadedHandled = false;
            _isPaused = false;
            _isFastForwarding = false;
            _pendingPlayPath = null;
            _pendingSeekTargetTime = -1;
            _presentationScheduled = false;
            _uiVisibilityMode = ReplayUiVisibilityMode.BothVisible;
            _replayData = null;
            _timelineIndex = 0;
            _startSpawnIndex = 0;
            _currentAbsoluteTimeMs = 0;
            _recordStartTimeMs = 0;
            _recordEndTimeMs = 0;
            _pendingMessages.Clear();
            _processedTimelineIndices.Clear();
            _remapper = new ReplayLevelObjectRemapper();
            _hud?.Destroy();
            _hud = null;
            ReplaySharedData.Clear();
            ReplaySharedData.SetNormalMode();

            if (wasActive && !silent)
            {
                ModLog.Info(Feature, "Replay playback stopped.");
            }
        }

        internal static void ExitToMainMenu()
        {
            StopPlayback();
            Hub.LoadScene(MainMenuSceneName);
        }

        internal static float GetProgressNormalized()
        {
            if (_recordEndTimeMs <= _recordStartTimeMs)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                (float)(_currentAbsoluteTimeMs - _recordStartTimeMs)
                / (_recordEndTimeMs - _recordStartTimeMs));
        }

        internal static string GetTimeLabel()
        {
            long elapsed = Math.Max(0, _currentAbsoluteTimeMs - _recordStartTimeMs);
            long total = Math.Max(0, _recordEndTimeMs - _recordStartTimeMs);
            return $"{FormatDuration(elapsed)} / {FormatDuration(total)}";
        }

        internal static string GetPreviewTimeLabel(float normalized)
        {
            if (_recordEndTimeMs <= _recordStartTimeMs)
            {
                return "0:00 / 0:00";
            }

            long total = _recordEndTimeMs - _recordStartTimeMs;
            long elapsed = (long)(total * Mathf.Clamp01(normalized));
            return $"{FormatDuration(elapsed)} / {FormatDuration(total)}";
        }

        private static void OnReplayDataLoaded(int generation, string playFilePath)
        {
            if (generation != _loadGeneration || !_isActive || playFilePath != _pendingPlayPath)
            {
                return;
            }

            if (_replayData == null)
            {
                ModLog.Warn(Feature, "Replay data load callback with null data.");
                StopPlayback();
                return;
            }

            _dataLoaded = _replayData.IsLoaded;
            if (!_dataLoaded)
            {
                ModLog.Warn(Feature, $"Replay data failed to load — {playFilePath}");
                StopPlayback();
            }
        }

        private static void PumpStartSpawnData()
        {
            if (_replayData == null)
            {
                return;
            }

            List<MsgWithTime> startSpawn = _replayData.StartSpawnData;
            for (int i = _startSpawnIndex; i < startSpawn.Count; i++)
            {
                MsgWithTime entry = startSpawn[i];
                if (entry?.msg != null)
                {
                    _pendingMessages.Enqueue(entry.msg);
                }
            }

            _startSpawnIndex = startSpawn.Count;
        }

        private static void PumpTimeline(long upToTime, bool skipVoice)
        {
            if (_replayData == null)
            {
                return;
            }

            while (_timelineIndex < _replayData.PlayLoopDataCount)
            {
                (long timeKey, List<ReplayData.PlayLoopData>? loopItems) =
                    _replayData.GetPlayLoopDataByIndex(_timelineIndex);
                if (timeKey < 0 || loopItems == null)
                {
                    _timelineIndex++;
                    continue;
                }

                if (timeKey > upToTime)
                {
                    break;
                }

                foreach (ReplayData.PlayLoopData loopData in loopItems)
                {
                    long key = ((long)_timelineIndex << 32) | (uint)loopData.index;
                    if (!_processedTimelineIndices.Add(key))
                    {
                        continue;
                    }

                    if (loopData.Type == ReplayData.REPLAY_DATA_TYPE.PLAY)
                    {
                        MsgWithTime? playMsg = _replayData.GetPlayDataByIndex(loopData.index);
                        if (playMsg?.msg != null)
                        {
                            _pendingMessages.Enqueue(playMsg.msg);
                        }
                    }
                    else if (loopData.Type == ReplayData.REPLAY_DATA_TYPE.VOICE)
                    {
                        ReplayVoicePlayer.PlayVoiceEvent(_replayData, loopData.index, skipVoice);
                    }
                }

                _timelineIndex++;
            }
        }

        private static void SeekToAbsoluteTime(long targetTime)
        {
            if (_replayData == null)
            {
                return;
            }

            if (targetTime < _currentAbsoluteTimeMs)
            {
                ReloadSceneForSeek(targetTime);
                return;
            }

            _isFastForwarding = true;
            _currentAbsoluteTimeMs = targetTime;
            PumpTimeline(upToTime: targetTime, skipVoice: true);
            _isFastForwarding = false;
            UpdateSharedTimeSlider();
        }

        private static void ReloadSceneForSeek(long targetTime)
        {
            if (string.IsNullOrEmpty(_pendingPlayPath) || _replayData == null)
            {
                return;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null)
            {
                ModLog.Warn(Feature, "Player data unavailable — cannot seek replay.");
                StopPlayback();
                return;
            }

            if (!ReplayGameAccess.TryResolveGameplaySceneName(
                    pdata.dungeonMasterID,
                    pdata.PickedMapID,
                    out string sceneName))
            {
                ModLog.Warn(Feature, "Gameplay scene missing — cannot seek replay.");
                StopPlayback();
                return;
            }

            _pendingSeekTargetTime = targetTime;
            _timelineIndex = 0;
            _startSpawnIndex = 0;
            _processedTimelineIndices.Clear();
            _pendingMessages.Clear();
            _sceneReady = false;
            _spectatorStarted = false;
            _sceneLoadedHandled = false;
            _presentationScheduled = false;
            _isFastForwarding = true;

            Hub.LoadScene(sceneName);
        }

        private static IEnumerator WaitForScenePresentationReady()
        {
            try
            {
                const int maxFrames = 600;
                for (int frame = 0; frame < maxFrames && _isActive && !_sceneLoadedHandled; frame++)
                {
                    GameMainBase? main = ReplayGameAccess.TryGetMain();
                    if (_dataLoaded && main is GamePlayScene or DeathMatchScene)
                    {
                        break;
                    }

                    yield return null;
                }

                if (!_isActive || _sceneLoadedHandled || !_dataLoaded)
                {
                    yield break;
                }

                for (int frame = 0; frame < 5; frame++)
                {
                    yield return null;
                }

                OnScenePresentationReady();
            }
            finally
            {
                _presentationScheduled = false;
            }
        }

        private static void ApplyUiVisibility()
        {
            bool showHud = _uiVisibilityMode == ReplayUiVisibilityMode.BothVisible;
            bool showPlayerUi = _uiVisibilityMode != ReplayUiVisibilityMode.BothHidden;

            if (showHud)
            {
                _hud?.Show();
            }
            else
            {
                _hud?.Hide();
            }

            ReplayGameAccess.TrySetInGameUiVisible(showPlayerUi);
        }

        private static void EnforceUiVisibility()
        {
            if (_uiVisibilityMode == ReplayUiVisibilityMode.BothHidden)
            {
                ReplayGameAccess.TrySetInGameUiVisible(false);
            }
        }

        private static void TryEnsureSpectator()
        {
            if (!_isActive || _spectatorStarted)
            {
                return;
            }

            if (ReplayGameAccess.TryEnterSpectatorMode())
            {
                _spectatorStarted = true;
            }
        }

        private static IEnumerator RetrySpectatorCoroutine()
        {
            for (int attempt = 0; attempt < 20 && _isActive && !_spectatorStarted; attempt++)
            {
                TryEnsureSpectator();
                if (_spectatorStarted)
                {
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }

            _isFastForwarding = false;
        }

        private static void UpdateSharedTimeSlider()
        {
            long total = Math.Max(1, _recordEndTimeMs - _recordStartTimeMs);
            long elapsed = Math.Max(0, _currentAbsoluteTimeMs - _recordStartTimeMs);
            ReplaySharedData.SetTimeSliderMaxValue(total);
            ReplaySharedData.SetTimeSliderValue(elapsed);
        }

        private static string FormatDuration(long milliseconds)
        {
            TimeSpan span = TimeSpan.FromMilliseconds(milliseconds);
            return span.TotalHours >= 1
                ? span.ToString(@"h\:mm\:ss")
                : span.ToString(@"m\:ss");
        }
    }
}

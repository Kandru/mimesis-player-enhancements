using System.Collections;
using System.IO;
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
        private const float HudRefreshIntervalSeconds = 0.25f;
        private const float BootstrapWatchdogSeconds = 15f;

        private static ReplayData? _replayData;
        private static ReplayLevelObjectRemapper _remapper = new();
        private static readonly Queue<IMsg> _pendingMessages = new();

        private static ReplaySessionState _state = ReplaySessionState.Idle;
        private static bool _isPaused;
        private static bool _isFastForwarding;
        private static bool _dataReady;
        private static bool _gameplaySceneReadyPending;
        private static bool _bootstrapScheduled;
        private static int _bootstrapWatchdogGeneration;
        private static bool _spectatorVerified;
        private static int _startSpawnIndex;
        private static int _timelineIndex;
        private static long _currentAbsoluteTimeMs;
        private static double _clockMs;
        private static long _recordStartTimeMs;
        private static long _recordEndTimeMs;
        private static float _playbackSpeed = 1f;
        private static string? _pendingPlayPath;
        private static ReplayHudUi? _hud;
        private static int _loadGeneration;
        private static long _pendingSeekTargetTime = -1;
        private static ReplayUiVisibilityMode _uiVisibilityMode = ReplayUiVisibilityMode.BothVisible;
        private static float _nextHudRefreshTime;
        private static float _nextMenuEnforceTime;

        internal static bool IsActive => _state != ReplaySessionState.Idle;

        internal static bool IsPaused => _isPaused;

        internal static bool IsRewinding => _state == ReplaySessionState.Rewinding;

        internal static bool BlockVanillaPlayerUiShow =>
            _state != ReplaySessionState.Idle
            && _uiVisibilityMode == ReplayUiVisibilityMode.BothHidden;

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
            _dataReady = false;
            _gameplaySceneReadyPending = false;
            _bootstrapScheduled = false;
            _bootstrapWatchdogGeneration++;
            _spectatorVerified = false;
            _timelineIndex = 0;
            _startSpawnIndex = 0;
            _pendingMessages.Clear();
            _isPaused = false;
            _isFastForwarding = false;
            _playbackSpeed = 1f;
            _currentAbsoluteTimeMs = _recordStartTimeMs;
            _clockMs = _recordStartTimeMs;
            _uiVisibilityMode = ReplayUiVisibilityMode.BothVisible;
            _nextHudRefreshTime = 0f;

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
            _state = ReplaySessionState.LoadingData;
            LogStateTransition(ReplaySessionState.LoadingData, "BeginPlayback");

            ReplayMenuSession.HideForPlayback();

            ModLog.Info(Feature, $"Loading replay — {Path.GetFileName(playFilePath)}");
            ModLog.Info(Feature, $"Loading replay scene — {sceneName}");
            Hub.LoadScene(sceneName);
        }

        internal static void OnUpdate()
        {
            if (_state == ReplaySessionState.Idle)
            {
                return;
            }

            if (!ReplaysRuntime.IsEnabled)
            {
                StopPlayback();
                return;
            }

            if (_state is ReplaySessionState.Playing or ReplaySessionState.Rewinding)
            {
                if (ReplayGameAccess.WasF10PressedThisFrame())
                {
                    CycleUiVisibility();
                }
            }

            if (_state == ReplaySessionState.Rewinding)
            {
                _hud?.Refresh();
                EnforceMenuHiddenThrottled();
                return;
            }

            if (_state != ReplaySessionState.Playing || _replayData == null)
            {
                if (_state is ReplaySessionState.Bootstrapping or ReplaySessionState.LoadingData)
                {
                    EnforceMenuHiddenThrottled();
                }

                return;
            }

            EnforceMenuHiddenThrottled();

            if (!_isPaused && !_isFastForwarding)
            {
                _clockMs += Time.unscaledDeltaTime * 1000.0 * _playbackSpeed;
                _currentAbsoluteTimeMs = (long)Math.Min(_recordEndTimeMs, _clockMs);
            }

            PumpTimeline(upToTime: _currentAbsoluteTimeMs, skipVoice: _isFastForwarding);

            if (Time.unscaledTime >= _nextHudRefreshTime)
            {
                _nextHudRefreshTime = Time.unscaledTime + HudRefreshIntervalSeconds;
                UpdateSharedTimeSlider();
                _hud?.Refresh();
            }
            else
            {
                _hud?.RefreshSliderOnly();
            }
        }

        internal static void NotifyGameplaySceneReady()
        {
            if (_state is ReplaySessionState.Idle or ReplaySessionState.Playing)
            {
                return;
            }

            ModLog.Debug(Feature, "Gameplay scene ready signal received.");
            _gameplaySceneReadyPending = true;
            TryScheduleBootstrapComplete();
        }

        internal static bool TryDequeueMessage(out IMsg msg)
        {
            msg = null!;
            if (_state != ReplaySessionState.Playing || _replayData == null)
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

        internal static void CycleUiVisibility()
        {
            _uiVisibilityMode = (ReplayUiVisibilityMode)(((int)_uiVisibilityMode + 1) % 3);
            ApplyUiVisibility();
        }

        internal static void OnPlayerSpawned(ProtoActor actor)
        {
            if (_state != ReplaySessionState.Playing || actor == null || !actor.IsPlayer())
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
            if (_state != ReplaySessionState.Playing || delta == 0)
            {
                return;
            }

            if (!ReplayGameAccess.TryCycleSpectatorTarget(delta))
            {
                return;
            }

            if (ReplayGameAccess.TryGetCameraManager() is CameraManager camera
                && ReplayGameAccess.IsSpectatorVerified(camera))
            {
                _spectatorVerified = true;
            }

            ApplyUiVisibility();
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
            bool wasActive = _state != ReplaySessionState.Idle;
            _loadGeneration++;
            _bootstrapWatchdogGeneration++;
            SetState(ReplaySessionState.Idle, silent ? "StopPlayback(silent)" : "StopPlayback");
            _dataReady = false;
            _gameplaySceneReadyPending = false;
            _bootstrapScheduled = false;
            _spectatorVerified = false;
            _isPaused = false;
            _isFastForwarding = false;
            _pendingPlayPath = null;
            _pendingSeekTargetTime = -1;
            _uiVisibilityMode = ReplayUiVisibilityMode.BothVisible;
            _replayData = null;
            _timelineIndex = 0;
            _startSpawnIndex = 0;
            _currentAbsoluteTimeMs = 0;
            _clockMs = 0;
            _recordStartTimeMs = 0;
            _recordEndTimeMs = 0;
            _pendingMessages.Clear();
            _remapper = new ReplayLevelObjectRemapper();
            _hud?.Destroy();
            _hud = null;
            ReplayMenuSession.Clear();
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
            if (_state == ReplaySessionState.Rewinding)
            {
                return "Rewinding…";
            }

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
            if (generation != _loadGeneration || _state == ReplaySessionState.Idle || playFilePath != _pendingPlayPath)
            {
                return;
            }

            if (_replayData == null)
            {
                ModLog.Warn(Feature, "Replay data load callback with null data.");
                StopPlayback();
                return;
            }

            if (!_replayData.IsLoaded)
            {
                ModLog.Warn(Feature, $"Replay data failed to load — {playFilePath}");
                StopPlayback();
                return;
            }

            _dataReady = true;
            if (_state == ReplaySessionState.LoadingData)
            {
                SetState(ReplaySessionState.Bootstrapping, "Replay data loaded");
                StartBootstrapWatchdog();
            }

            TryScheduleBootstrapComplete();
        }

        private static void SetState(ReplaySessionState newState, string reason)
        {
            if (_state == newState)
            {
                return;
            }

            ModLog.Debug(Feature, $"State {_state} → {newState} — {reason}");
            _state = newState;
        }

        private static void LogStateTransition(ReplaySessionState newState, string reason) =>
            SetState(newState, reason);

        private static void StartBootstrapWatchdog()
        {
            int generation = ++_bootstrapWatchdogGeneration;
            MelonCoroutines.Start(BootstrapWatchdogCoroutine(generation));
        }

        private static IEnumerator BootstrapWatchdogCoroutine(int generation)
        {
            yield return new WaitForSecondsRealtime(BootstrapWatchdogSeconds);

            if (generation != _bootstrapWatchdogGeneration || _state == ReplaySessionState.Idle)
            {
                yield break;
            }

            if (_state == ReplaySessionState.Playing)
            {
                yield break;
            }

            GameMainBase? main = ReplayGameAccess.TryGetMain();
            ModLog.Warn(
                Feature,
                $"Replay bootstrap stalled — state={_state}, dataReady={_dataReady}, "
                + $"sceneReadyPending={_gameplaySceneReadyPending}, main={main?.GetType().Name ?? "null"}");
        }

        private static void TryScheduleBootstrapComplete()
        {
            if (!_dataReady || !_gameplaySceneReadyPending || _bootstrapScheduled)
            {
                return;
            }

            if (_state is not (ReplaySessionState.Bootstrapping or ReplaySessionState.Rewinding))
            {
                return;
            }

            _bootstrapScheduled = true;
            ModLog.Debug(Feature, "Scheduling replay bootstrap complete.");
            MelonCoroutines.Start(BootstrapCompleteCoroutine());
        }

        private static IEnumerator BootstrapCompleteCoroutine()
        {
            try
            {
                yield return null;

                if (_state is ReplaySessionState.Idle)
                {
                    yield break;
                }

                OnSceneBootstrapComplete();
            }
            finally
            {
                _bootstrapScheduled = false;
            }
        }

        private static void OnSceneBootstrapComplete()
        {
            if (_state is ReplaySessionState.Idle || _replayData == null || !_dataReady)
            {
                return;
            }

            GameMainBase? main = ReplayGameAccess.TryGetMain();
            if (main is not GamePlayScene and not DeathMatchScene)
            {
                return;
            }

            ReplayHeader? header = _replayData.ReplayHeader as ReplayHeader;
            if (header?.BTActionNameTable != null)
            {
                BTActionRegistry.InitializeFromTable(header.BTActionNameTable);
            }

            _remapper.BuildFromHeader(header);
            ReplaySharedData.SetEmotionData(_replayData.DebugMimicVoiceEmotionData);
            _timelineIndex = 0;
            _startSpawnIndex = 0;
            _pendingMessages.Clear();
            _spectatorVerified = false;
            _gameplaySceneReadyPending = false;
            _bootstrapWatchdogGeneration++;

            ReplayMenuSession.ClearBlockingOverlays();

            long resumeTime = _pendingSeekTargetTime >= _recordStartTimeMs
                ? _pendingSeekTargetTime
                : _recordStartTimeMs;
            _pendingSeekTargetTime = -1;
            _currentAbsoluteTimeMs = resumeTime;
            _clockMs = resumeTime;

            PumpStartSpawnData();
            if (resumeTime > _recordStartTimeMs)
            {
                _isFastForwarding = true;
                PumpTimeline(upToTime: resumeTime, skipVoice: true);
                _isFastForwarding = false;
            }

            SetState(ReplaySessionState.Playing, "Bootstrap complete");
            UpdateSharedTimeSlider();
            _hud?.Destroy();
            _hud = ReplayHudUi.Create();
            ApplyUiVisibility();
            MelonCoroutines.Start(RetrySpectatorCoroutine());
            _nextHudRefreshTime = 0f;
            ModLog.Info(Feature, "Replay playback started.");
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

            targetTime = Math.Clamp(targetTime, _recordStartTimeMs, _recordEndTimeMs);

            if (targetTime < _currentAbsoluteTimeMs)
            {
                BeginSilentRewind(targetTime);
                return;
            }

            _isFastForwarding = true;
            _currentAbsoluteTimeMs = targetTime;
            _clockMs = targetTime;
            PumpTimeline(upToTime: targetTime, skipVoice: true);
            _isFastForwarding = false;
            UpdateSharedTimeSlider();
            _hud?.Refresh();
        }

        private static void BeginSilentRewind(long targetTime)
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

            SetState(ReplaySessionState.Rewinding, "Backward seek");
            _pendingSeekTargetTime = targetTime;
            _timelineIndex = 0;
            _startSpawnIndex = 0;
            _pendingMessages.Clear();
            _spectatorVerified = false;
            _gameplaySceneReadyPending = false;
            _bootstrapScheduled = false;
            _bootstrapWatchdogGeneration++;
            _isFastForwarding = true;
            _hud?.Refresh();

            Hub.LoadScene(sceneName);
            SetState(ReplaySessionState.Bootstrapping, "Silent rewind scene load");
            StartBootstrapWatchdog();
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

            if (showPlayerUi)
            {
                ReplayGameAccess.TryShowSpectatorHud();
            }
            else
            {
                ReplayGameAccess.TryHideSpectatorPlayerUi();
            }
        }

        private static void TryEnsureSpectator()
        {
            if (_spectatorVerified)
            {
                return;
            }

            ReplayGameAccess.TryEnterSpectatorMode();
            CameraManager? camera = ReplayGameAccess.TryGetCameraManager();
            if (camera == null || !ReplayGameAccess.IsSpectatorVerified(camera))
            {
                return;
            }

            _spectatorVerified = true;
            ReplayGameAccess.TryShowSpectatorHud();
            ApplyUiVisibility();
        }

        private static void EnforceMenuHiddenThrottled()
        {
            if (Time.unscaledTime < _nextMenuEnforceTime)
            {
                return;
            }

            _nextMenuEnforceTime = Time.unscaledTime + 0.25f;
            ReplayMenuSession.ClearBlockingOverlays();
        }

        private static IEnumerator RetrySpectatorCoroutine()
        {
            for (int attempt = 0; attempt < 20 && _state == ReplaySessionState.Playing && !_spectatorVerified; attempt++)
            {
                ReplayMenuSession.ClearBlockingOverlays();
                TryEnsureSpectator();
                if (_spectatorVerified)
                {
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }
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

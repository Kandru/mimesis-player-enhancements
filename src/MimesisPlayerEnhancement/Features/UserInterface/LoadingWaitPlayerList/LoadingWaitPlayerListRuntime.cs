using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListRuntime
    {
        private const string Feature = "Ui";
        private const string WaitTextKey = "STRING_LOADING_WAIT";
        private const float RefreshIntervalSeconds = 0.15f;
        private const float FadeInSeconds = 0.75f;
        private const float DefaultFadeOutSeconds = 1f;
        private const int DebugCanvasSortOrder = 32000;

        private static LoadingWaitPlayerListOverlay? _overlay;
        private static GameObject? _debugHost;
        private static bool _waitActive;
        private static bool _debugActive;
        private static bool _loggedShow;
        private static float _nextRefreshTime;

        private static bool _fading;
        private static bool _hideAfterFade;
        private static float _fadeElapsed;
        private static float _fadeDuration;
        private static float _fadeFrom;
        private static float _fadeTo;

        internal static bool IsVisible =>
            _overlay?.Root != null
            && _overlay.Root.activeSelf
            && (_waitActive || _debugActive || (_fading && _hideAfterFade));

        internal static bool IsEnabled() =>
            ModConfig.IsInitialized && ModConfig.EnableLoadingWaitPlayerList.Value;

        internal static void OnLoadingText(UIPrefab_Scene_Loading loading, string textKey)
        {
            if (!IsEnabled())
            {
                return;
            }

            if (string.Equals(textKey, WaitTextKey, StringComparison.Ordinal))
            {
                if (!SessionPlayerCountHelper.IsMultiplayerLobby())
                {
                    ModLog.Debug(Feature, "Skipping loading wait player list — solo lobby");
                    return;
                }

                Show(loading);
                return;
            }

            if (_waitActive || (_fading && _hideAfterFade))
            {
                HideImmediate();
            }
        }

        internal static void Show(UIPrefab_Scene_Loading loading)
        {
            if (!IsEnabled() || loading == null)
            {
                return;
            }

            _overlay ??= new LoadingWaitPlayerListOverlay();
            if (!_overlay.TryEnsure(loading.transform))
            {
                ModLog.Debug(Feature, "Skipping loading wait player list — spectator row template unavailable");
                return;
            }

            _waitActive = true;
            _debugActive = false;
            BeginFade(from: 0f, to: 1f, FadeInSeconds, hideAfter: false);
            Refresh(force: true);

            if (!_loggedShow)
            {
                _loggedShow = true;
                ModLog.Info(Feature, "Loading wait player list shown");
            }
        }

        internal static void Hide()
        {
            if (_overlay == null || (!_waitActive && !_debugActive))
            {
                return;
            }

            if (_debugActive)
            {
                HideImmediate();
                return;
            }

            _waitActive = false;
            float from = _overlay.CanvasGroup != null ? _overlay.CanvasGroup.alpha : 1f;
            BeginFade(from, to: 0f, ResolveFadeOutSeconds(), hideAfter: true);
        }

        internal static void HideImmediate()
        {
            _waitActive = false;
            _debugActive = false;
            _loggedShow = false;
            _fading = false;
            _hideAfterFade = false;
            _overlay?.Destroy();
            _overlay = null;
            DestroyDebugHost();
        }

        internal static void OnSessionEnded() => HideImmediate();

        internal static bool DebugShow(IReadOnlyList<string> fakeNames)
        {
            SpectatorPlayerGrid.EnsureSpectatorHudAvailable();

            Transform parent = EnsureDebugHost();
            _overlay ??= new LoadingWaitPlayerListOverlay();
            if (!_overlay.TryEnsure(parent))
            {
                DestroyDebugHost();
                return false;
            }

            _waitActive = false;
            _debugActive = true;
            _fading = false;
            _overlay.ApplyAlpha(1f);

            try
            {
                LoadingWaitPlayerListGrid.Update(
                    _overlay.GridState!,
                    LoadingWaitPlayerListDebugEntries.BuildScrambled(fakeNames));
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Loading wait debug preview failed — {ex.Message}");
                HideImmediate();
                return false;
            }

            return true;
        }

        internal static void DebugHide() => HideImmediate();

        internal static void RefreshFromConfig()
        {
            if (!IsEnabled())
            {
                HideImmediate();
            }
        }

        internal static void OnUpdate()
        {
            if (_fading)
            {
                TickFade();
            }

            if (_debugActive || !_waitActive || _overlay?.GridState == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
            Refresh(force: false);
        }

        private static void Refresh(bool force)
        {
            if (_overlay?.GridState == null || !_waitActive)
            {
                return;
            }

            List<LoadingWaitPlayerEntry> players = LoadingWaitPlayerListPlayerSource.CollectPlayers();
            if (players.Count == 0 && !force)
            {
                return;
            }

            try
            {
                LoadingWaitPlayerListGrid.Update(_overlay.GridState, players);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Loading wait player list refresh failed — {ex.Message}");
            }
        }

        private static Transform EnsureDebugHost()
        {
            DestroyDebugHost();

            _debugHost = new GameObject("MPE_LoadingWaitPlayerListDebugHost");
            UnityEngine.Object.DontDestroyOnLoad(_debugHost);

            RectTransform hostRect = _debugHost.AddComponent<RectTransform>();
            ModUiLayout.Stretch(hostRect);

            Canvas canvas = _debugHost.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = DebugCanvasSortOrder;
            return _debugHost.transform;
        }

        private static void DestroyDebugHost()
        {
            if (_debugHost == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(_debugHost);
            _debugHost = null;
        }

        private static void BeginFade(float from, float to, float duration, bool hideAfter)
        {
            _fading = true;
            _hideAfterFade = hideAfter;
            _fadeElapsed = 0f;
            _fadeDuration = Mathf.Max(duration, 0.05f);
            _fadeFrom = from;
            _fadeTo = to;
            _overlay?.ApplyAlpha(from);
        }

        private static void TickFade()
        {
            _fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / _fadeDuration);
            _overlay?.ApplyAlpha(Mathf.Lerp(_fadeFrom, _fadeTo, t));
            if (t < 1f)
            {
                return;
            }

            _fading = false;
            if (_hideAfterFade)
            {
                HideImmediate();
            }
        }

        private static float ResolveFadeOutSeconds()
        {
            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return DefaultFadeOutSeconds;
            }

            float seconds = uiManager.InGameFadeInSec > 0.05f
                ? uiManager.InGameFadeInSec
                : uiManager.WaitingRoomFadeInSec;
            return Mathf.Max(seconds, 0.05f);
        }
    }
}

using System.Threading;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListRuntime
    {
        private const string Feature = "Ui";
        private const float RefreshIntervalSeconds = 0.15f;

        private static readonly CancellationTokenSource SpeakCancellation = new();

        private static UIPrefab_Scene_Loading? _activeLoading;
        private static LoadingWaitPlayerListOverlay? _overlay;
        private static bool _waitTextActive;
        private static bool _dismissing;
        private static bool _loggedShow;
        private static float _nextRefreshTime;
        private static float _vanillaFadeStartAlpha = 1f;
        private static float _vanillaFadeElapsed;
        private static float _vanillaFadeDuration;
        private static bool _vanillaFadingOut;

        internal static bool IsVisible =>
            _overlay?.Root != null && _overlay.Root.activeSelf && (_waitTextActive || _dismissing);

        internal static bool IsEnabled() =>
            ModConfig.IsInitialized && ModConfig.EnableLoadingWaitPlayerList.Value;

        internal static void OnLoadingText(UIPrefab_Scene_Loading loading, string textKey)
        {
            if (!IsEnabled())
            {
                return;
            }

            if (string.Equals(textKey, CustomLoadingScreenConstants.WaitTextKey, StringComparison.Ordinal))
            {
                if (!SessionPlayerCountHelper.TryResolveExactFromSession(out int playerCount) || playerCount <= 1)
                {
                    ModLog.Debug(Feature, "Skipping loading wait player list — solo lobby");
                    return;
                }

                Show(loading);
                return;
            }

            if (_waitTextActive || _dismissing)
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

            Transform? parent = ResolveParent(loading);
            if (parent == null)
            {
                ModLog.Debug(Feature, "Skipping loading wait player list — no overlay parent");
                return;
            }

            _overlay ??= new LoadingWaitPlayerListOverlay();
            if (!_overlay.TryEnsure(loading, parent))
            {
                ModLog.Debug(Feature, "Skipping loading wait player list — spectator row template unavailable");
                return;
            }

            _activeLoading = loading;
            _waitTextActive = true;
            _dismissing = false;
            _vanillaFadingOut = false;
            ApplyOverlayAlpha(1f);
            Refresh(force: true);

            if (!_loggedShow)
            {
                _loggedShow = true;
                ModLog.Info(Feature, "Loading wait player list shown");
            }
        }

        internal static void OnLoadingDismissStarted()
        {
            if (_overlay == null || (!_waitTextActive && !_dismissing))
            {
                return;
            }

            _waitTextActive = false;
            _dismissing = true;

            if (!CustomLoadingScreenSession.IsActive)
            {
                BeginVanillaFadeOut();
            }
        }

        internal static void Hide(bool fadeWithOverlay = true)
        {
            if (_overlay == null || (!_waitTextActive && !_dismissing))
            {
                return;
            }

            if (fadeWithOverlay)
            {
                OnLoadingDismissStarted();
                return;
            }

            HideImmediate();
        }

        internal static void HideImmediate()
        {
            _waitTextActive = false;
            _dismissing = false;
            _loggedShow = false;
            _vanillaFadingOut = false;
            _activeLoading = null;
            _overlay?.Destroy();
            _overlay = null;
        }

        internal static void OnSessionEnded() => HideImmediate();

        internal static void RefreshFromConfig()
        {
            if (!IsEnabled())
            {
                HideImmediate();
            }
        }

        internal static void ApplyOverlayAlpha(float alpha)
        {
            _overlay?.ApplyAlpha(alpha);
            if (_dismissing && alpha <= 0.001f)
            {
                HideImmediate();
            }
        }

        internal static void OnUpdate()
        {
            if (_vanillaFadingOut)
            {
                TickVanillaFade();
            }

            if (!_waitTextActive || _overlay?.GridState == null)
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
            if (_overlay?.GridState == null || !_waitTextActive)
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
                LoadingWaitPlayerListGrid.Update(
                    _overlay.GridState,
                    players,
                    SpeakCancellation.Token);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Loading wait player list refresh failed — {ex.Message}");
            }
        }

        private static Transform? ResolveParent(UIPrefab_Scene_Loading loading)
        {
            Transform? customOverlay = loading.transform.Find(CustomLoadingScreenConstants.OverlayObjectName);
            if (customOverlay != null)
            {
                return customOverlay;
            }

            return loading.transform;
        }

        private static void BeginVanillaFadeOut()
        {
            _vanillaFadingOut = true;
            _vanillaFadeElapsed = 0f;
            _vanillaFadeStartAlpha = _overlay?.CanvasGroup != null ? _overlay.CanvasGroup.alpha : 1f;
            _vanillaFadeDuration = ResolveVanillaFadeSeconds();
            if (_vanillaFadeDuration <= 0.05f)
            {
                HideImmediate();
            }
        }

        private static void TickVanillaFade()
        {
            _vanillaFadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_vanillaFadeElapsed / _vanillaFadeDuration);
            float alpha = Mathf.Lerp(_vanillaFadeStartAlpha, 0f, t);
            _overlay?.ApplyAlpha(alpha);
            if (t >= 1f)
            {
                HideImmediate();
            }
        }

        private static float ResolveVanillaFadeSeconds()
        {
            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return CustomLoadingScreenConstants.DefaultArrivalFadeSeconds;
            }

            float seconds = uiManager.InGameFadeInSec > 0.05f
                ? uiManager.InGameFadeInSec
                : uiManager.WaitingRoomFadeInSec;
            return Mathf.Max(seconds, 0.05f);
        }
    }
}

using System.Threading;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListRuntime
    {
        private const string Feature = "Ui";
        private const float RefreshIntervalSeconds = 0.15f;

        private static readonly CancellationTokenSource SpeakCancellation = new();
        private static readonly System.Random DebugRandom = new();

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
        private static bool _debugActive;

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
            if (!_overlay.TryEnsure(parent))
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
            _debugActive = false;
            _waitTextActive = false;
            _dismissing = false;
            _loggedShow = false;
            _vanillaFadingOut = false;
            _activeLoading = null;
            _overlay?.Destroy();
            _overlay = null;
        }

        internal static void OnSessionEnded() => HideImmediate();

        internal static bool DebugShow(IReadOnlyList<string> fakeNames)
        {
            SpectatorPlayerGrid.EnsureSpectatorHudAvailable();

            UIPrefab_Scene_Loading? loading = ModUiGameAccess.TryGetUiManager()?.ui_sceneloading;
            if (loading == null)
            {
                return false;
            }

            if (!CustomLoadingScreenApplier.DebugBeginWaitPreview(loading))
            {
                return false;
            }

            Transform? parent = ResolveParent(loading);
            if (parent == null)
            {
                CustomLoadingScreenApplier.DebugEndWaitPreview(loading);
                return false;
            }

            _overlay ??= new LoadingWaitPlayerListOverlay();
            if (!_overlay.TryEnsure(parent))
            {
                CustomLoadingScreenApplier.DebugEndWaitPreview(loading);
                return false;
            }

            _activeLoading = loading;
            _waitTextActive = true;
            _dismissing = false;
            _vanillaFadingOut = false;
            _debugActive = true;
            ApplyOverlayAlpha(1f);

            List<LoadingWaitPlayerEntry> entries = BuildScrambledDebugEntries(fakeNames);

            try
            {
                LoadingWaitPlayerListGrid.Update(
                    _overlay.GridState!,
                    entries,
                    SpeakCancellation.Token);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Loading wait debug preview failed — {ex.Message}");
                DebugHide();
                return false;
            }

            return true;
        }

        internal static void DebugHide()
        {
            UIPrefab_Scene_Loading? loading = _activeLoading
                ?? ModUiGameAccess.TryGetUiManager()?.ui_sceneloading;
            HideImmediate();
            CustomLoadingScreenApplier.DebugEndWaitPreview(loading);
        }

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

            if (_debugActive)
            {
                return;
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

        private static List<LoadingWaitPlayerEntry> BuildScrambledDebugEntries(IReadOnlyList<string> fakeNames)
        {
            int count = fakeNames.Count;
            bool[] loadedFlags = ScrambleTrueFlags(count, trueRatio: 0.5f);

            List<LoadingWaitPlayerEntry> entries = new(count);
            for (int index = 0; index < count; index++)
            {
                entries.Add(new LoadingWaitPlayerEntry
                {
                    PlayerUid = -(index + 1),
                    DisplayName = fakeNames[index],
                    Loaded = loadedFlags[index],
                });
            }

            entries.Sort(static (left, right) =>
            {
                int loadedCompare = right.Loaded.CompareTo(left.Loaded);
                if (loadedCompare != 0)
                {
                    return loadedCompare;
                }

                return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            });

            return entries;
        }

        private static bool[] ScrambleTrueFlags(int count, float trueRatio, bool ensureMix = true)
        {
            bool[] flags = new bool[count];
            if (count == 0)
            {
                return flags;
            }

            int trueCount = Mathf.Clamp(Mathf.RoundToInt(count * trueRatio), 0, count);
            if (ensureMix && count >= 2)
            {
                trueCount = Mathf.Clamp(trueCount, 1, count - 1);
            }

            for (int index = 0; index < trueCount; index++)
            {
                flags[index] = true;
            }

            for (int index = count - 1; index > 0; index--)
            {
                int swapIndex = DebugRandom.Next(index + 1);
                (flags[index], flags[swapIndex]) = (flags[swapIndex], flags[index]);
            }

            return flags;
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

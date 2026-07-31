using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitMovementLock
{
    internal static class LoadingWaitMovementLockRuntime
    {
        private const string Feature = "Ui";

        private static readonly MethodInfo? SetEnableInputForMyAvatarMethod =
            AccessTools.Method(typeof(GameMainBase), "SetEnableInputForMyAvatar");

        private static bool _pendingUnlock;
        private static float _deferredAt;
        private static GameMainBase? _deferredMain;
        private static bool _loggedDefer;
        private static bool _loggedRelease;

        internal static bool TryDeferUnlock(GameMainBase main)
        {
            if (!ShouldDefer(main))
            {
                return false;
            }

            _pendingUnlock = true;
            _deferredAt = Time.unscaledTime;
            _deferredMain = main;
            _loggedRelease = false;

            if (!_loggedDefer)
            {
                _loggedDefer = true;
                ModLog.Info(Feature, "Loading wait movement lock — deferring input unlock until scene loading ends");
            }

            return true;
        }

        internal static void TryReleaseUnlock(GameMainBase? main)
        {
            if (!_pendingUnlock)
            {
                return;
            }

            if (main != null && _deferredMain != null && main != _deferredMain)
            {
                return;
            }

            ReleaseNow("EndSceneLoading");
        }

        internal static void OnUpdate()
        {
            if (!_pendingUnlock)
            {
                return;
            }

            if (Time.unscaledTime - _deferredAt >= LoadingWaitMovementLockLogic.MaxDeferSeconds)
            {
                ReleaseNow("timeout");
            }
        }

        internal static void OnSessionEnded()
        {
            if (_pendingUnlock)
            {
                ResetState();
            }
        }

        private static bool ShouldDefer(GameMainBase main)
        {
            bool loadingVisible = IsLoadingScreenVisible();
            int playerCount = SessionPlayerCountHelper.TryResolveLobbyPlayerCount(out int count)
                ? count
                : 0;

            return LoadingWaitMovementLockLogic.ShouldDeferInputUnlock(
                main is GamePlayScene,
                loadingVisible,
                playerCount);
        }

        private static bool IsLoadingScreenVisible()
        {
            UIPrefab_Scene_Loading? loading = ModUiGameAccess.TryGetUiManager()?.ui_sceneloading;
            return loading != null && loading.gameObject.activeSelf;
        }

        private static void ReleaseNow(string reason)
        {
            GameMainBase? main = _deferredMain;
            bool wasPending = _pendingUnlock;

            ResetState();

            if (!wasPending || main == null)
            {
                return;
            }

            try
            {
                SetEnableInputForMyAvatarMethod?.Invoke(main, null);
                if (!_loggedRelease)
                {
                    _loggedRelease = true;
                    ModLog.Info(Feature, $"Loading wait movement lock — released input unlock ({reason})");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Loading wait movement lock release failed — {ex.Message}");
            }
        }

        private static void ResetState()
        {
            _pendingUnlock = false;
            _deferredAt = 0f;
            _deferredMain = null;
            _loggedDefer = false;
            _loggedRelease = false;
        }
    }
}

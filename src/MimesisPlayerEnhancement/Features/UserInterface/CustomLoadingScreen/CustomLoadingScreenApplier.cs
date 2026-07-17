using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenApplier
    {
        private static readonly FieldInfo? LoadingSceneDataListField =
            AccessTools.Field(typeof(UIPrefab_Scene_Loading), "loadingSceneDataList");

        private static readonly FieldInfo? CurrentLoadingSceneDataField =
            AccessTools.Field(typeof(UIPrefab_Scene_Loading), "currentLoadingSceneData");

        private static UIPrefab_Scene_Loading? _activeLoading;
        private static bool _rootNodeHidden;
        private static bool _allowVanillaHide;

        internal static void ApplyScene(UIPrefab_Scene_Loading loading, string loadingSceneKey)
        {
            if (loading == null)
            {
                return;
            }

            CustomLoadingScreenContext context = CustomLoadingScreenContextUtil.FromLoadingSceneKey(
                loadingSceneKey,
                CustomLoadingScreenSession.LastTransitionContext);
            CustomLoadingScreenSession.TrackTransition(context);

            if (!CustomLoadingScreenResolver.ShouldApplyReplacement())
            {
                Restore(loading, fadeOut: false);
                return;
            }

            // The early scene-change apply may already show this context (e.g. predicted from
            // Hub.LoadScene). Keep the picked theme instead of re-rolling a random one.
            if (CustomLoadingScreenSession.IsActive && CustomLoadingScreenSession.Context == context)
            {
                return;
            }

            string? theme = CustomLoadingScreenResolver.ResolveThemeForContext(context);
            if (string.IsNullOrWhiteSpace(theme))
            {
                Restore(loading, fadeOut: false);
                return;
            }

            CustomLoadingScreenSession.Begin(context, theme);
            HideVanillaVariants(loading);
            HideVanillaRootNode(loading);
            ApplyPhase(loading, CustomLoadingScreenPhase.Loading);
            WarmWaitPhase(context, theme);

            ModLog.Debug(CustomLoadingScreenConstants.Feature,
                $"Custom loading screen applied — context={context}, theme={theme}");
        }

        private static readonly FieldInfo? EndSessionSigField =
            AccessTools.Field(typeof(InTramWaitingScene), "endSessionSig");

        private static readonly FieldInfo? LeverDestinationField =
            AccessTools.Field(typeof(NewTramLeverLevelObject), "_currentDestination");

        private static readonly FieldInfo? FadeInImageField =
            AccessTools.Field(typeof(UIManager), "fadeInImage");

        private static readonly FieldInfo? VideoPlayLayerField =
            AccessTools.Field(typeof(UIManager), "videoPlayLayer");

        /// <summary>Lever reached <c>Open</c> — departure is committed. Show the custom screen
        /// immediately (with a short fade-in) and hold it through the exiting cutscene, which
        /// otherwise calls <c>EndSceneLoading</c> and would tear the overlay down.</summary>
        internal static void ApplyOnLeverOpen(NewTramLeverLevelObject lever)
        {
            CustomLoadingScreenContext? context = PredictContextFromLever(lever);
            if (context == null)
            {
                return;
            }

            ApplyPredictedContext(
                context.Value,
                $"lever context={context}",
                holdThroughDeparture: true,
                fadeIn: true);
        }

        /// <summary>Fallback when the game-status UI hides at the start of a lever departure
        /// sequence (covers paths where the lever patch did not run).</summary>
        internal static void ApplyOnDepartureStart()
        {
            CustomLoadingScreenContext? context = PredictDepartureContext();
            if (context == null)
            {
                return;
            }

            ApplyPredictedContext(
                context.Value,
                $"departure context={context}",
                holdThroughDeparture: true,
                fadeIn: true);
        }

        /// <summary>Called right before <c>Hub.LoadScene</c>. Backup for non-lever transitions;
        /// snaps visible if the early apply already ran.</summary>
        internal static void ApplyBeforeSceneLoad(string? sceneName)
        {
            CustomLoadingScreenContext? predicted = PredictContext(sceneName);
            if (predicted == null)
            {
                return;
            }

            ApplyPredictedContext(
                predicted.Value,
                $"scene={sceneName}, context={predicted}",
                holdThroughDeparture: true,
                fadeIn: false);
        }

        /// <summary>After the synchronous scene switch, the next <c>EndSceneLoading</c> is the
        /// real one for the destination scene — allow Hide/Restore again.</summary>
        internal static void OnSceneLoadCompleted()
        {
            CustomLoadingScreenSession.ReleaseDepartureHold();
        }

        /// <summary>Session ended (e.g. host left the lobby mid-departure before <c>Hub.LoadScene</c>
        /// completed). Force the overlay down and clear all hold/dismiss state so it can never
        /// stick — the normal <c>ReleaseDepartureHold</c> path may never run in this case.</summary>
        internal static void ForceReset()
        {
            // Session is over — a stale DungeonStart must not leak into the next keyless Show
            // (EnsureAppliedBeforeShow falls back to LastTransitionContext ?? FirstEnter).
            CustomLoadingScreenSession.ResetTransitionContext();

            if (!CustomLoadingScreenSession.IsActive
                && !CustomLoadingScreenSession.HoldThroughDeparture
                && _activeLoading == null)
            {
                return;
            }

            UIPrefab_Scene_Loading? loading =
                _activeLoading ?? ModUiGameAccess.TryGetUiManager()?.ui_sceneloading;
            FinishDismiss(loading);

            ModLog.Debug(CustomLoadingScreenConstants.Feature,
                "Custom loading screen force-reset — session ended");
        }

        private static CustomLoadingScreenContext? PredictContextFromLever(NewTramLeverLevelObject lever)
        {
            TramLeverDestination destination = TramLeverDestination.ToMap;
            if (LeverDestinationField?.GetValue(lever) is TramLeverDestination parsed)
            {
                destination = parsed;
            }

            return Hub.Main switch
            {
                InTramWaitingScene tram => IsTramMaintenanceDeparture(tram, destination)
                    ? CustomLoadingScreenContext.Maintenance
                    : CustomLoadingScreenContext.DungeonStart,
                GamePlayScene => CustomLoadingScreenContext.DungeonEnd,
                MaintenanceScene => CustomLoadingScreenContext.TramScene,
                _ => null,
            };
        }

        private static CustomLoadingScreenContext? PredictDepartureContext()
        {
            return Hub.Main switch
            {
                InTramWaitingScene tram => IsTramMaintenanceDeparture(tram)
                    ? CustomLoadingScreenContext.Maintenance
                    : CustomLoadingScreenContext.DungeonStart,
                GamePlayScene => CustomLoadingScreenContext.DungeonEnd,
                MaintenanceScene => CustomLoadingScreenContext.TramScene,
                _ => null,
            };
        }

        private static bool IsTramSessionEnd(InTramWaitingScene tram) =>
            EndSessionSigField?.GetValue(tram) != null;

        /// <summary>Matches tram lever UI: maintenance when the maintenance map slot is selected
        /// (<c>dungeonIndex == 2</c>), session end is signaled, or the lever destination is set.</summary>
        private static bool IsTramMaintenanceDeparture(
            InTramWaitingScene tram,
            TramLeverDestination? leverDestination = null)
        {
            if (IsTramSessionEnd(tram))
            {
                return true;
            }

            if (tram.GetDungeonIndex() == 2)
            {
                return true;
            }

            if (leverDestination == TramLeverDestination.ToMaintenance)
            {
                return true;
            }

            return false;
        }

        private static void ApplyPredictedContext(
            CustomLoadingScreenContext context,
            string logDetail,
            bool holdThroughDeparture,
            bool fadeIn)
        {
            if (!CustomLoadingScreenResolver.ShouldApplyReplacement())
            {
                return;
            }

            if (CustomLoadingScreenSession.IsActive && CustomLoadingScreenSession.Context == context)
            {
                if (holdThroughDeparture)
                {
                    CustomLoadingScreenSession.RequestDepartureHold();
                }

                UIPrefab_Scene_Loading? activeLoading = ModUiGameAccess.TryGetUiManager()?.ui_sceneloading;
                if (activeLoading != null)
                {
                    EnsureLoadingVisible(activeLoading, fadeIn: false);
                }

                return;
            }

            if (CustomLoadingScreenSession.IsActive)
            {
                if (!holdThroughDeparture)
                {
                    return;
                }

                UIPrefab_Scene_Loading? activeLoading = ModUiGameAccess.TryGetUiManager()?.ui_sceneloading;
                if (activeLoading == null)
                {
                    return;
                }

                string? switchTheme = CustomLoadingScreenResolver.ResolveThemeForContext(context);
                if (string.IsNullOrWhiteSpace(switchTheme))
                {
                    return;
                }

                CustomLoadingScreenContext previousContext = CustomLoadingScreenSession.Context;
                ModLog.Debug(CustomLoadingScreenConstants.Feature,
                    $"Custom loading screen context corrected — {previousContext} -> {context}, {logDetail}");

                ApplyPredictedContextToLoading(
                    activeLoading,
                    context,
                    switchTheme,
                    holdThroughDeparture,
                    fadeIn: false,
                    logDetail);
                return;
            }

            UIPrefab_Scene_Loading? loading = ModUiGameAccess.TryGetUiManager()?.ui_sceneloading;
            if (loading == null)
            {
                return;
            }

            string? theme = CustomLoadingScreenResolver.ResolveThemeForContext(context);
            if (string.IsNullOrWhiteSpace(theme))
            {
                return;
            }

            ApplyPredictedContextToLoading(loading, context, theme, holdThroughDeparture, fadeIn, logDetail);
        }

        private static void ApplyPredictedContextToLoading(
            UIPrefab_Scene_Loading loading,
            CustomLoadingScreenContext context,
            string theme,
            bool holdThroughDeparture,
            bool fadeIn,
            string logDetail)
        {
            CustomLoadingScreenSession.Begin(context, theme, holdThroughDeparture);
            CustomLoadingScreenSession.TrackTransition(context);
            HideVanillaVariants(loading);
            HideVanillaRootNode(loading);
            ApplyPhase(loading, CustomLoadingScreenPhase.Loading);
            WarmWaitPhase(context, theme);
            SuppressVanillaFullscreenCovers();
            EnsureLoadingVisible(loading, fadeIn);

            ModLog.Debug(CustomLoadingScreenConstants.Feature,
                $"Custom loading screen pre-applied — {logDetail}, theme={theme}");
        }

        private static void EnsureLoadingVisible(UIPrefab_Scene_Loading loading, bool fadeIn)
        {
            CustomLoadingScreenOverlayAnimator? animator = GetAnimator(loading);
            ElevateOverlayAboveVanillaCovers(loading);
            if (fadeIn)
            {
                UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
                float duration = uiManager != null
                    ? Mathf.Max(uiManager.WaitingRoomFadeOutSec, 0.05f)
                    : CustomLoadingScreenConstants.DefaultDepartureFadeSeconds;
                animator?.BeginFadeIn(duration);
            }
            else
            {
                animator?.SnapVisible();
            }

            if (!loading.gameObject.activeSelf)
            {
                loading.Show();
            }

            // Show() can reshuffle siblings — re-assert after activating.
            ElevateOverlayAboveVanillaCovers(loading);
            SuppressVanillaFullscreenCovers();
        }

        /// <summary>Called from the FadeOut patch and while holding departure — keeps the game's
        /// black fade / cutscene video layer from covering the custom image.</summary>
        internal static void SuppressVanillaFullscreenCovers()
        {
            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return;
            }

            if (FadeInImageField?.GetValue(uiManager) is Image fadeImage && fadeImage != null)
            {
                Color color = fadeImage.color;
                fadeImage.color = new Color(color.r, color.g, color.b, 0f);
                fadeImage.gameObject.SetActive(false);
            }

            if (VideoPlayLayerField?.GetValue(uiManager) is Transform videoLayer && videoLayer != null)
            {
                videoLayer.gameObject.SetActive(false);
            }
        }

        /// <summary>Raise the custom overlay above <c>fadeInImage</c> / video layers via a nested
        /// canvas with a high sort order, and keep it last among its siblings.</summary>
        internal static void ElevateOverlayAboveVanillaCovers(UIPrefab_Scene_Loading? loading)
        {
            Transform? overlay = loading != null
                ? loading.transform.Find(CustomLoadingScreenConstants.OverlayObjectName)
                : null;
            if (overlay == null)
            {
                return;
            }

            Canvas overlayCanvas = overlay.GetComponent<Canvas>() ?? overlay.gameObject.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = CustomLoadingScreenConstants.OverlayCanvasSortOrder;
            overlay.SetAsLastSibling();
        }

        private static CustomLoadingScreenContext? PredictContext(string? sceneName)
        {
            string name = sceneName?.Trim() ?? "";
            if (name.Length == 0)
            {
                return null;
            }

            if (name.Contains("InTramWaiting", StringComparison.OrdinalIgnoreCase)
                || name.Contains("tram", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenSession.LastTransitionContext == CustomLoadingScreenContext.DungeonStart
                    ? CustomLoadingScreenContext.DungeonEnd
                    : CustomLoadingScreenContext.TramScene;
            }

            if (name.Contains("Maintenance", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenContext.Maintenance;
            }

            // Explicit non-gameplay destinations are never a dungeon map — e.g. returning to the
            // main menu (CorGoBackToMainMenu) tears down the session but leaves InTramWaitingScene
            // alive until the scene switch, which would otherwise trip the tram heuristic below.
            if (name.Contains("MainMenu", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Quit", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Leaving the tram waiting room for an unknown map means a dungeon is loading — but only
            // while a session is actually live. Post-session LoadScene (leave/quit) must not re-apply.
            if (IsGameSessionLive()
                && UnityEngine.Object.FindAnyObjectByType<InTramWaitingScene>() != null)
            {
                return CustomLoadingScreenContext.DungeonStart;
            }

            return null;
        }

        private static bool IsGameSessionLive()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            return pdata != null && pdata.SessionJoined;
        }

        /// <summary>Called right before the loading UI's open animation. Ensures the custom art
        /// is already in place so the screen fades directly into it. Covers call sites that show
        /// the loading UI without a scene key (e.g. joining from the main menu).</summary>
        internal static void EnsureAppliedBeforeShow(UIPrefab_Scene_Loading loading)
        {
            if (loading == null || !CustomLoadingScreenResolver.ShouldApplyReplacement())
            {
                return;
            }

            if (CustomLoadingScreenSession.IsActive)
            {
                PositionOverlay(loading);
                GetAnimator(loading)?.RefreshLayout();
                return;
            }

            // No scene key yet — fall back to the last known transition, or FirstEnter for a
            // fresh join. SetLoadingScene refines this as soon as the target scene starts.
            CustomLoadingScreenContext context =
                CustomLoadingScreenSession.LastTransitionContext ?? CustomLoadingScreenContext.FirstEnter;
            string? theme = CustomLoadingScreenResolver.ResolveThemeForContext(context);
            if (string.IsNullOrWhiteSpace(theme))
            {
                return;
            }

            CustomLoadingScreenSession.Begin(context, theme);
            HideVanillaVariants(loading);
            HideVanillaRootNode(loading);
            ApplyPhase(loading, CustomLoadingScreenPhase.Loading);
        }

        internal static void ApplyTextPhase(UIPrefab_Scene_Loading loading, string textKey)
        {
            if (loading == null
                || !CustomLoadingScreenSession.IsActive
                || CustomLoadingScreenSession.Phase == CustomLoadingScreenPhase.Wait
                || !string.Equals(textKey, CustomLoadingScreenConstants.WaitTextKey, StringComparison.Ordinal))
            {
                return;
            }

            // Wait art is only for multiplayer lobbies — solo runs stay on loading/background.
            if (!HasMultipleLobbyPlayers())
            {
                ModLog.Debug(CustomLoadingScreenConstants.Feature,
                    "Skipping wait image — solo lobby");
                return;
            }

            CustomLoadingScreenResolvedPhase? waitPresentation =
                CustomLoadingScreenResolver.ResolveDedicatedWaitPresentation(
                    CustomLoadingScreenSession.Context,
                    CustomLoadingScreenSession.Theme);
            if (waitPresentation == null)
            {
                ModLog.Debug(CustomLoadingScreenConstants.Feature,
                    "Skipping wait image — no dedicated wait.png for theme");
                return;
            }

            CustomLoadingScreenSession.SetPhase(CustomLoadingScreenPhase.Wait);
            ApplyPhase(loading, waitPresentation, crossfade: true);
        }

        internal static void RefreshMotionFromConfig()
        {
            if (!CustomLoadingScreenSession.IsActive || _activeLoading == null)
            {
                return;
            }

            GetAnimator(_activeLoading)?.RefreshMotion(CustomLoadingScreenResolver.IsMotionEnabled());
        }

        internal static void ReapplyActivePhaseIfNeeded()
        {
            if (!CustomLoadingScreenSession.IsActive || _activeLoading == null)
            {
                return;
            }

            ApplyPhase(_activeLoading, CustomLoadingScreenSession.Phase);
        }

        /// <summary>Fade the custom overlay out into the loaded scene, then tear it down.
        /// Returns true when vanilla <c>Hide</c> must be deferred until the fade finishes.</summary>
        internal static bool Restore(UIPrefab_Scene_Loading? loading, bool fadeOut = true)
        {
            if (_allowVanillaHide)
            {
                return false;
            }

            if (CustomLoadingScreenSession.IsDismissing)
            {
                return fadeOut;
            }

            if (!CustomLoadingScreenSession.IsActive)
            {
                return false;
            }

            UIPrefab_Scene_Loading? target = loading ?? _activeLoading;
            CustomLoadingScreenOverlayAnimator? animator = target != null ? GetAnimator(target) : null;
            if (!fadeOut || target == null || animator == null)
            {
                FinishDismiss(target);
                return false;
            }

            CustomLoadingScreenSession.BeginDismiss();
            HideLoadingChromeExceptOverlay(target);
            ElevateOverlayAboveVanillaCovers(target);
            float duration = ResolveArrivalFadeSeconds();
            animator.BeginFadeOut(duration, () => FinishDismiss(target));
            return true;
        }

        /// <summary>While dissolving into the scene, only the custom overlay should remain so the
        /// game is revealed underneath — not leftover loading chrome.</summary>
        private static void HideLoadingChromeExceptOverlay(UIPrefab_Scene_Loading loading)
        {
            for (int i = 0; i < loading.transform.childCount; i++)
            {
                Transform child = loading.transform.GetChild(i);
                if (child != null
                    && !string.Equals(child.name, CustomLoadingScreenConstants.OverlayObjectName, StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static float ResolveArrivalFadeSeconds()
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

        private static void FinishDismiss(UIPrefab_Scene_Loading? loading)
        {
            CustomLoadingScreenSession.Clear();
            _activeLoading = null;
            if (loading == null)
            {
                return;
            }

            GetAnimator(loading)?.Stop();
            HideOverlay(loading);
            ShowVanillaRootNode(loading);
            ShowVanillaCurrent(loading);

            if (loading.gameObject.activeSelf)
            {
                _allowVanillaHide = true;
                try
                {
                    loading.Hide();
                }
                finally
                {
                    _allowVanillaHide = false;
                }
            }
        }

        private static void ApplyPhase(UIPrefab_Scene_Loading loading, CustomLoadingScreenPhase phase)
        {
            CustomLoadingScreenResolvedPhase? presentation = CustomLoadingScreenResolver.ResolvePhasePresentation(
                CustomLoadingScreenSession.Context,
                CustomLoadingScreenSession.Theme,
                phase);
            if (presentation == null)
            {
                ModLog.Warn(CustomLoadingScreenConstants.Feature,
                    $"Custom loading screen has no images — context={CustomLoadingScreenSession.Context}, theme={CustomLoadingScreenSession.Theme}, phase={phase}");
                Restore(loading, fadeOut: false);
                return;
            }

            ApplyPhase(loading, presentation, crossfade: false);
        }

        private static void ApplyPhase(
            UIPrefab_Scene_Loading loading,
            CustomLoadingScreenResolvedPhase presentation,
            bool crossfade)
        {
            CustomLoadingScreenOverlayAnimator animator = GetOrCreateAnimator(loading);
            bool motionEnabled = CustomLoadingScreenResolver.IsMotionEnabled();
            if (crossfade)
            {
                animator.CrossfadeTo(
                    presentation,
                    motionEnabled,
                    CustomLoadingScreenConstants.DefaultPhaseCrossfadeSeconds);
            }
            else
            {
                animator.Play(presentation, motionEnabled);
            }

            animator.gameObject.SetActive(true);
            PositionOverlay(loading);
            ElevateOverlayAboveVanillaCovers(loading);
            _activeLoading = loading;

            ModLog.Debug(CustomLoadingScreenConstants.Feature,
                $"Custom loading screen phase applied — phase={CustomLoadingScreenSession.Phase}, crossfade={crossfade}, images={string.Join(", ", presentation.ImagePaths)}");
        }

        private static bool HasMultipleLobbyPlayers()
        {
            // Prefer exact roster size — VanillaPlayerBaseline must not force wait art on solo.
            return SessionPlayerCountHelper.TryResolveExactFromSession(out int playerCount)
                   && playerCount > 1;
        }

        /// <summary>Decodes dedicated wait-phase textures up front so the loading→wait crossfade
        /// does not stall while the dungeon is generating.</summary>
        private static void WarmWaitPhase(CustomLoadingScreenContext context, string theme)
        {
            if (context != CustomLoadingScreenContext.DungeonStart)
            {
                return;
            }

            CustomLoadingScreenResolvedPhase? waitPhase =
                CustomLoadingScreenResolver.ResolveDedicatedWaitPresentation(context, theme);
            if (waitPhase != null)
            {
                _ = CustomLoadingScreenTextureCache.TryGetTextures(waitPhase.ImagePaths);
            }
        }

        private static CustomLoadingScreenOverlayAnimator GetOrCreateAnimator(UIPrefab_Scene_Loading loading)
        {
            Transform? existing = loading.transform.Find(CustomLoadingScreenConstants.OverlayObjectName);
            if (existing != null
                && existing.TryGetComponent(out CustomLoadingScreenOverlayAnimator cachedAnimator)
                && TryGetOverlayImages(
                    existing,
                    out RawImage? cachedBackground,
                    out RawImage? cachedContent,
                    out RawImage? cachedCrossfade))
            {
                cachedAnimator.Initialize(cachedBackground, cachedContent, cachedCrossfade);
                cachedAnimator.RefreshLayout();
                return cachedAnimator;
            }

            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            GameObject overlayObject = new(CustomLoadingScreenConstants.OverlayObjectName);
            overlayObject.transform.SetParent(loading.transform, worldPositionStays: false);
            CustomLoadingScreenImageLayout.StretchRect(overlayObject.AddComponent<RectTransform>());

            RawImage backgroundImage = CreateStretchRawImage(
                overlayObject.transform,
                CustomLoadingScreenConstants.OverlayBackgroundObjectName);
            backgroundImage.texture = Texture2D.whiteTexture;
            backgroundImage.color = Color.black;

            RawImage contentImage = CreateStretchRawImage(
                overlayObject.transform,
                CustomLoadingScreenConstants.OverlayImageObjectName);
            contentImage.color = Color.white;

            RawImage crossfadeImage = CreateStretchRawImage(
                overlayObject.transform,
                CustomLoadingScreenConstants.OverlayCrossfadeObjectName);
            crossfadeImage.color = new Color(1f, 1f, 1f, 0f);
            crossfadeImage.gameObject.SetActive(false);

            CustomLoadingScreenOverlayAnimator animator = overlayObject.AddComponent<CustomLoadingScreenOverlayAnimator>();
            animator.Initialize(backgroundImage, contentImage, crossfadeImage);
            return animator;
        }

        private static void PositionOverlay(UIPrefab_Scene_Loading loading)
        {
            Transform? overlay = loading.transform.Find(CustomLoadingScreenConstants.OverlayObjectName);
            if (overlay == null)
            {
                return;
            }

            // Keep the overlay below the loading/percent text but above the vanilla art.
            Transform? textAnchor = loading.transform.Find("LoadingPercentText");
            if (textAnchor != null)
            {
                overlay.SetSiblingIndex(textAnchor.GetSiblingIndex());
            }
            else
            {
                overlay.SetAsLastSibling();
            }
        }

        private static bool TryGetOverlayImages(
            Transform overlayRoot,
            out RawImage backgroundImage,
            out RawImage contentImage,
            out RawImage? crossfadeImage)
        {
            backgroundImage = null!;
            contentImage = null!;
            crossfadeImage = null;
            Transform? backgroundTransform = overlayRoot.Find(CustomLoadingScreenConstants.OverlayBackgroundObjectName);
            Transform? contentTransform = overlayRoot.Find(CustomLoadingScreenConstants.OverlayImageObjectName);
            if (backgroundTransform == null
                || contentTransform == null
                || !backgroundTransform.TryGetComponent(out backgroundImage)
                || !contentTransform.TryGetComponent(out contentImage))
            {
                return false;
            }

            Transform? crossfadeTransform = overlayRoot.Find(CustomLoadingScreenConstants.OverlayCrossfadeObjectName);
            if (crossfadeTransform != null)
            {
                _ = crossfadeTransform.TryGetComponent(out crossfadeImage);
            }

            return true;
        }

        private static RawImage CreateStretchRawImage(Transform parent, string objectName)
        {
            GameObject imageObject = new(objectName);
            imageObject.transform.SetParent(parent, worldPositionStays: false);
            CustomLoadingScreenImageLayout.StretchRect(imageObject.AddComponent<RectTransform>());

            RawImage rawImage = imageObject.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            return rawImage;
        }

        private static CustomLoadingScreenOverlayAnimator? GetAnimator(UIPrefab_Scene_Loading loading)
        {
            Transform? existing = loading.transform.Find(CustomLoadingScreenConstants.OverlayObjectName);
            return existing != null && existing.TryGetComponent(out CustomLoadingScreenOverlayAnimator animator)
                ? animator
                : null;
        }

        private static void HideOverlay(UIPrefab_Scene_Loading loading)
        {
            Transform? existing = loading.transform.Find(CustomLoadingScreenConstants.OverlayObjectName);
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }
        }

        private static void HideVanillaRootNode(UIPrefab_Scene_Loading loading)
        {
            Image? rootNode = loading.UE_rootNode;
            if (rootNode == null)
            {
                return;
            }

            _rootNodeHidden = rootNode.gameObject.activeSelf;
            rootNode.gameObject.SetActive(false);
        }

        private static void ShowVanillaRootNode(UIPrefab_Scene_Loading loading)
        {
            if (!_rootNodeHidden)
            {
                return;
            }

            _rootNodeHidden = false;
            Image? rootNode = loading.UE_rootNode;
            if (rootNode != null)
            {
                rootNode.gameObject.SetActive(true);
            }
        }

        private static void HideVanillaVariants(UIPrefab_Scene_Loading loading)
        {
            if (LoadingSceneDataListField?.GetValue(loading)
                is not List<UIPrefab_Scene_Loading.LoadingSceneData> dataList)
            {
                return;
            }

            for (int i = 0; i < dataList.Count; i++)
            {
                SetLoadingSceneTransformsActive(dataList[i], active: false);
            }
        }

        private static void ShowVanillaCurrent(UIPrefab_Scene_Loading loading)
        {
            if (CurrentLoadingSceneDataField?.GetValue(loading)
                is not UIPrefab_Scene_Loading.LoadingSceneData current)
            {
                return;
            }

            SetLoadingSceneTransformsActive(current, active: true);
        }

        private static void SetLoadingSceneTransformsActive(
            UIPrefab_Scene_Loading.LoadingSceneData loadingSceneData,
            bool active)
        {
            List<Transform>? transforms = loadingSceneData?.loadingScenes;
            if (transforms == null)
            {
                return;
            }

            for (int i = 0; i < transforms.Count; i++)
            {
                Transform transform = transforms[i];
                if (transform != null)
                {
                    transform.gameObject.SetActive(active);
                }
            }
        }
    }
}

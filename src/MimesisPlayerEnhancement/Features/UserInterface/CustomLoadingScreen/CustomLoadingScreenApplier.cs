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
                Restore(loading);
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
                Restore(loading);
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

        private static CustomLoadingScreenContext? PredictContextFromLever(NewTramLeverLevelObject lever)
        {
            TramLeverDestination destination = TramLeverDestination.ToMap;
            if (LeverDestinationField?.GetValue(lever) is TramLeverDestination parsed)
            {
                destination = parsed;
            }

            return Hub.Main switch
            {
                InTramWaitingScene => destination == TramLeverDestination.ToMaintenance
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
                InTramWaitingScene tram => IsTramSessionEnd(tram)
                    ? CustomLoadingScreenContext.Maintenance
                    : CustomLoadingScreenContext.DungeonStart,
                GamePlayScene => CustomLoadingScreenContext.DungeonEnd,
                MaintenanceScene => CustomLoadingScreenContext.TramScene,
                _ => null,
            };
        }

        private static bool IsTramSessionEnd(InTramWaitingScene tram) =>
            EndSessionSigField?.GetValue(tram) != null;

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

            // Leaving the tram waiting room for anything else means a dungeon map is loading.
            if (UnityEngine.Object.FindAnyObjectByType<InTramWaitingScene>() != null)
            {
                return CustomLoadingScreenContext.DungeonStart;
            }

            return null;
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

            CustomLoadingScreenSession.SetPhase(CustomLoadingScreenPhase.Wait);
            ApplyPhase(loading, CustomLoadingScreenPhase.Wait);
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

        internal static void Restore(UIPrefab_Scene_Loading? loading)
        {
            bool wasActive = CustomLoadingScreenSession.IsActive;
            CustomLoadingScreenSession.Clear();
            _activeLoading = null;
            if (loading == null || !wasActive)
            {
                return;
            }

            GetAnimator(loading)?.Stop();
            HideOverlay(loading);
            ShowVanillaRootNode(loading);
            ShowVanillaCurrent(loading);
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
                Restore(loading);
                return;
            }

            CustomLoadingScreenOverlayAnimator animator = GetOrCreateAnimator(loading);
            animator.Play(presentation, CustomLoadingScreenResolver.IsMotionEnabled());
            animator.gameObject.SetActive(true);
            PositionOverlay(loading);
            ElevateOverlayAboveVanillaCovers(loading);
            _activeLoading = loading;

            ModLog.Debug(CustomLoadingScreenConstants.Feature,
                $"Custom loading screen phase applied — phase={phase}, images={string.Join(", ", presentation.ImagePaths)}");
        }

        /// <summary>Decodes the wait-phase textures up front so the loading→wait switch does not
        /// stall or pop while the dungeon is generating.</summary>
        private static void WarmWaitPhase(CustomLoadingScreenContext context, string theme)
        {
            if (context != CustomLoadingScreenContext.DungeonStart)
            {
                return;
            }

            CustomLoadingScreenResolvedPhase? waitPhase = CustomLoadingScreenResolver.ResolvePhasePresentation(
                context,
                theme,
                CustomLoadingScreenPhase.Wait);
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
                && TryGetOverlayImages(existing, out RawImage? cachedBackground, out RawImage? cachedContent))
            {
                cachedAnimator.Initialize(cachedBackground, cachedContent);
                return cachedAnimator;
            }

            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            GameObject overlayObject = new(CustomLoadingScreenConstants.OverlayObjectName);
            overlayObject.transform.SetParent(loading.transform, worldPositionStays: false);
            StretchRect(overlayObject.AddComponent<RectTransform>());

            RawImage backgroundImage = CreateStretchRawImage(
                overlayObject.transform,
                CustomLoadingScreenConstants.OverlayBackgroundObjectName);
            backgroundImage.texture = Texture2D.whiteTexture;
            backgroundImage.color = Color.black;

            RawImage contentImage = CreateStretchRawImage(
                overlayObject.transform,
                CustomLoadingScreenConstants.OverlayImageObjectName);
            contentImage.color = Color.white;

            CustomLoadingScreenOverlayAnimator animator = overlayObject.AddComponent<CustomLoadingScreenOverlayAnimator>();
            animator.Initialize(backgroundImage, contentImage);
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
            out RawImage contentImage)
        {
            backgroundImage = null!;
            contentImage = null!;
            Transform? backgroundTransform = overlayRoot.Find(CustomLoadingScreenConstants.OverlayBackgroundObjectName);
            Transform? contentTransform = overlayRoot.Find(CustomLoadingScreenConstants.OverlayImageObjectName);
            return backgroundTransform != null
                   && contentTransform != null
                   && backgroundTransform.TryGetComponent(out backgroundImage)
                   && contentTransform.TryGetComponent(out contentImage);
        }

        private static RawImage CreateStretchRawImage(Transform parent, string objectName)
        {
            GameObject imageObject = new(objectName);
            imageObject.transform.SetParent(parent, worldPositionStays: false);
            StretchRect(imageObject.AddComponent<RectTransform>());

            RawImage rawImage = imageObject.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            return rawImage;
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
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

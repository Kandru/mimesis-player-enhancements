using System.Collections;
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

        internal static void ApplyScene(UIPrefab_Scene_Loading loading, string loadingSceneKey)
        {
            if (loading == null)
            {
                return;
            }

            if (!CustomLoadingScreenResolver.ShouldApplyReplacement())
            {
                Restore(loading);
                return;
            }

            CustomLoadingScreenContext context = CustomLoadingScreenContextUtil.FromLoadingSceneKey(loadingSceneKey);
            string? theme = CustomLoadingScreenResolver.ResolveThemeForContext(context);
            if (string.IsNullOrWhiteSpace(theme))
            {
                Restore(loading);
                return;
            }

            CustomLoadingScreenSession.Begin(context, theme);
            HideVanillaVariants(loading);
            ApplyPhaseTexture(loading, CustomLoadingScreenPhase.Loading);

            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(CustomLoadingScreenConstants.Feature,
                    $"Custom loading screen applied — context={context}, theme={theme}");
            }
        }

        internal static void ApplyTextPhase(UIPrefab_Scene_Loading loading, string textKey)
        {
            if (loading == null || !CustomLoadingScreenSession.IsActive)
            {
                return;
            }

            if (!string.Equals(textKey, CustomLoadingScreenConstants.WaitTextKey, StringComparison.Ordinal))
            {
                return;
            }

            CustomLoadingScreenSession.SetPhase(CustomLoadingScreenPhase.Wait);
            ApplyPhaseTexture(loading, CustomLoadingScreenPhase.Wait);
        }

        internal static void Restore(UIPrefab_Scene_Loading? loading)
        {
            CustomLoadingScreenSession.Clear();
            if (loading == null)
            {
                return;
            }

            HideOverlay(loading);
            ShowVanillaCurrent(loading);
        }

        private static void ApplyPhaseTexture(UIPrefab_Scene_Loading loading, CustomLoadingScreenPhase phase)
        {
            if (!CustomLoadingScreenSession.IsActive)
            {
                return;
            }

            string? relativePath = CustomLoadingScreenResolver.ResolveImageRelativePath(
                CustomLoadingScreenSession.Context,
                CustomLoadingScreenSession.Theme,
                phase);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                Restore(loading);
                return;
            }

            Texture2D? texture = CustomLoadingScreenTextureCache.TryGetTexture(relativePath);
            if (texture == null)
            {
                Restore(loading);
                return;
            }

            RawImage overlay = GetOrCreateOverlay(loading);
            overlay.texture = texture;
            overlay.gameObject.SetActive(true);
        }

        private static RawImage GetOrCreateOverlay(UIPrefab_Scene_Loading loading)
        {
            Transform? existing = loading.transform.Find(CustomLoadingScreenConstants.OverlayObjectName);
            if (existing != null && existing.TryGetComponent(out RawImage cached))
            {
                return cached;
            }

            GameObject overlayObject = new(CustomLoadingScreenConstants.OverlayObjectName);
            overlayObject.transform.SetParent(loading.transform, worldPositionStays: false);
            overlayObject.transform.SetAsFirstSibling();

            RectTransform rect = overlayObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            RawImage rawImage = overlayObject.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            rawImage.color = Color.white;
            return rawImage;
        }

        private static void HideOverlay(UIPrefab_Scene_Loading loading)
        {
            Transform? existing = loading.transform.Find(CustomLoadingScreenConstants.OverlayObjectName);
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }
        }

        private static void HideVanillaVariants(UIPrefab_Scene_Loading loading)
        {
            if (LoadingSceneDataListField?.GetValue(loading) is not IEnumerable dataList)
            {
                return;
            }

            foreach (object? dataEntry in dataList)
            {
                if (dataEntry == null)
                {
                    continue;
                }

                DeactivateLoadingSceneTransforms(dataEntry);
            }
        }

        private static void ShowVanillaCurrent(UIPrefab_Scene_Loading loading)
        {
            object? current = CurrentLoadingSceneDataField?.GetValue(loading);
            if (current == null)
            {
                return;
            }

            ActivateLoadingSceneTransforms(current);
        }

        private static void DeactivateLoadingSceneTransforms(object loadingSceneData)
        {
            if (!TryGetLoadingSceneTransforms(loadingSceneData, out IEnumerable? transforms))
            {
                return;
            }

            foreach (object? transformObj in transforms!)
            {
                if (transformObj is Transform transform && transform != null)
                {
                    transform.gameObject.SetActive(false);
                }
            }
        }

        private static void ActivateLoadingSceneTransforms(object loadingSceneData)
        {
            if (!TryGetLoadingSceneTransforms(loadingSceneData, out IEnumerable? transforms))
            {
                return;
            }

            foreach (object? transformObj in transforms!)
            {
                if (transformObj is Transform transform && transform != null)
                {
                    transform.gameObject.SetActive(true);
                }
            }
        }

        private static bool TryGetLoadingSceneTransforms(object loadingSceneData, out IEnumerable? transforms)
        {
            transforms = null;
            FieldInfo? field = AccessTools.Field(loadingSceneData.GetType(), "loadingScenes");
            if (field == null)
            {
                return false;
            }

            transforms = field.GetValue(loadingSceneData) as IEnumerable;
            return transforms != null;
        }
    }
}

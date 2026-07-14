using System.Collections;
using System.Reflection;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi
{
    /// <summary>
    /// Screen-space FPS vitals on the Main UI layer, positioned from measured inventory coordinates.
    /// </summary>
    internal static class FpsUiOverlay
    {
        private const string Feature = "Ui";
        private const string OverlayRootName = "MPE_FpsVitals";
        private const float LayoutGapPixels = 20f;
        private const float LabelWidthPixels = 132f;
        private const float HealthFontSize = 50f;
        private const float ToxicFontSize = 30f;
        private const float ToxicPercentWidthPixels = 72f;
        private const float ToxicIconHeightPixels = ToxicFontSize * 0.85f;
        private const float ToxicRowSpacingPixels = 4f;
        private const float HealthNudgeDownPixels = 4f;
        private const float ToxicNudgeUpPixels = 4f;

        private static readonly Color32 HealthLivingColor = new(255, 80, 80, 255);
        private static readonly Color32 HealthDeadColor = new(180, 50, 50, 255);
        private static readonly Color32 ToxicPercentGreen = new(120, 220, 120, 255);
        private static readonly Color32 ToxicPercentYellowGreen = new(180, 230, 120, 255);
        private static readonly Color32 ToxicPercentOrange = new(255, 210, 80, 255);
        private static readonly Color32 ToxicPercentRed = new(255, 90, 90, 255);

        private static readonly PropertyInfo? CurrencyTextProperty =
            AccessTools.Property(typeof(UIPrefab_InGame), "UE_Currency");

        private static readonly PropertyInfo? KillCountTextProperty =
            AccessTools.Property(typeof(UIPrefab_InGame), "UE_KillCount");

        private static readonly FieldInfo? OxyGaugeField =
            AccessTools.Field(typeof(UIPrefab_InGame), "oxyGauge");

        private static readonly FieldInfo? IconSpritesField =
            AccessTools.Field(typeof(UIPrefab_ProgressBar), "iconSprites");

        private static UIPrefab_InGame? _ingameUi;
        private static SessionState _state = new();
        private static GameObject? _overlayRoot;
        private static RectTransform? _healthRect;
        private static RectTransform? _toxicRect;
        private static Component? _healthLabel;
        private static Component? _toxicPercentLabel;
        private static Image? _toxicIconImage;
        private static Sprite? _toxicIconSprite;
        private static bool _loggedOverlayFailure;
        private static bool _loggedToxicIconFailure;

        // Per-frame layout state — avoids rect rewrites and corner-array allocations while stable.
        private static readonly Vector3[] _cornersBuffer = new Vector3[4];
        private static Vector2 _lastMeasuredLeft = new(float.NaN, float.NaN);
        private static Vector2 _lastMeasuredTop;
        private static Vector2 _lastMeasuredBottom;

        internal static bool IsEnabled() => ModConfig.EnableFpsUi.Value;

        internal static void NotifyInventoryShown()
        {
            if (!IsEnabled())
            {
                return;
            }

            ResolveIngameUi();
            Activate();
            RefreshLayout();
        }

        internal static void OnUpdate()
        {
            if (!IsEnabled() || !_state.Active)
            {
                return;
            }

            if (!IsInventoryVisibleForOverlay())
            {
                _overlayRoot?.SetActive(false);
                return;
            }

            RefreshLayout();
        }

        internal static void Attach(UIPrefab_InGame ingameUi)
        {
            if (!IsEnabled())
            {
                return;
            }

            _ingameUi = ingameUi;
            Activate();
        }

        internal static void UpdateHealth(UIPrefab_InGame ingameUi, long curr, long maxHp, bool isDead)
        {
            _ingameUi = ingameUi;
            _state.LastHp = curr;
            _state.LastMaxHp = maxHp;
            _state.LastIsDead = isDead;

            if (!IsEnabled())
            {
                return;
            }

            if (!IsInventoryVisibleForOverlay())
            {
                _overlayRoot?.SetActive(false);
                return;
            }

            if (!TryEnsureOverlay(ingameUi))
            {
                return;
            }

            if (!_state.Active)
            {
                HideVanillaVitals(ingameUi);
                _state.Active = true;
                RefreshLayout();
                UpdateOverlayVisibility();
            }

            if (_healthLabel == null)
            {
                return;
            }

            long displayHp = isDead ? 0L : curr;
            ModUiText.SetText(_healthLabel, displayHp.ToString());
            ModUiText.SetColor(_healthLabel, ResolveHealthColor(isDead));
        }

        internal static void UpdateConta(UIPrefab_InGame ingameUi, long curr, long maxConta)
        {
            _ingameUi = ingameUi;
            _state.LastConta = curr;
            _state.LastMaxConta = maxConta;

            if (!IsEnabled())
            {
                return;
            }

            if (!IsInventoryVisibleForOverlay())
            {
                _overlayRoot?.SetActive(false);
                return;
            }

            if (!TryEnsureOverlay(ingameUi))
            {
                return;
            }

            if (!_state.Active)
            {
                HideVanillaVitals(ingameUi);
                _state.Active = true;
                RefreshLayout();
                UpdateOverlayVisibility();
            }

            if (_toxicPercentLabel == null)
            {
                return;
            }

            float percent = maxConta <= 0L ? 0f : (float)curr / maxConta * 100f;
            float displayPercent = Mathf.Min(percent, 100f);
            string toxicText = displayPercent >= 100f ? "100%" : $"{displayPercent:F1}%";
            ModUiText.SetText(_toxicPercentLabel, toxicText);
            ModUiText.SetColor(_toxicPercentLabel, ResolveToxicPercentColor(percent));
            ApplyToxicIcon(ingameUi, percent);
        }

        internal static void RefreshFromConfig()
        {
            if (!IsEnabled())
            {
                Deactivate();
                return;
            }

            ResolveIngameUi();
            if (_ingameUi != null)
            {
                Attach(_ingameUi);
            }

            foreach (UIPrefab_InGame ingameUi in UnityEngine.Object.FindObjectsByType<UIPrefab_InGame>(FindObjectsSortMode.None))
            {
                Attach(ingameUi);
            }
        }

        internal static void OnSessionEnded() => Deactivate();

        internal static void OnInventoryHidden()
        {
            _state.Active = false;
            _overlayRoot?.SetActive(false);
        }

        internal static void ForceHideOxyGauge(UIPrefab_InGame ingameUi)
        {
            if (IsEnabled())
            {
                ingameUi.SetVisibleOxyGauge(visible: false);
            }
        }

        private static void ResolveIngameUi()
        {
            _ingameUi ??= FpsUiInventoryLayoutHelper.TryGetIngameUi();
        }

        private static void Activate()
        {
            if (!IsEnabled())
            {
                return;
            }

            if (!IsInventoryVisibleForOverlay())
            {
                _overlayRoot?.SetActive(false);
                return;
            }

            ResolveIngameUi();
            if (_ingameUi == null || !TryEnsureOverlay(_ingameUi))
            {
                return;
            }

            if (!_state.Active)
            {
                HideVanillaVitals(_ingameUi);
                _state.Active = true;
            }

            RefreshLayout();
            ReplayCachedValues();
            UpdateOverlayVisibility();
        }

        private static void Deactivate()
        {
            if (_ingameUi != null && _state.Active)
            {
                RestoreVanillaVitals(_ingameUi);
            }

            _state.Active = false;
            _ingameUi = null;
            DestroyOverlay();
        }

        internal static void RefreshLayout()
        {
            if (!IsEnabled() || _overlayRoot == null || _healthRect == null || _toxicRect == null)
            {
                return;
            }

            if (!TryMeasureInventoryScreenPosition(out Vector2 leftLocal, out Vector2 topLocal, out Vector2 bottomLocal))
            {
                return;
            }

            // Skip the rect rewrites (and layout dirtying) while inventory geometry is stable.
            if (leftLocal == _lastMeasuredLeft && topLocal == _lastMeasuredTop && bottomLocal == _lastMeasuredBottom)
            {
                return;
            }

            _lastMeasuredLeft = leftLocal;
            _lastMeasuredTop = topLocal;
            _lastMeasuredBottom = bottomLocal;

            float x = leftLocal.x - LayoutGapPixels;

            _toxicRect.anchorMin = new Vector2(0.5f, 0.5f);
            _toxicRect.anchorMax = new Vector2(0.5f, 0.5f);
            _toxicRect.pivot = new Vector2(1f, 1f);
            _toxicRect.anchoredPosition = new Vector2(x, topLocal.y + ToxicNudgeUpPixels);
            _toxicRect.sizeDelta = new Vector2(LabelWidthPixels, ToxicFontSize);

            _healthRect.anchorMin = new Vector2(0.5f, 0.5f);
            _healthRect.anchorMax = new Vector2(0.5f, 0.5f);
            _healthRect.pivot = new Vector2(1f, 0f);
            _healthRect.anchoredPosition = new Vector2(x, bottomLocal.y - HealthNudgeDownPixels);
            _healthRect.sizeDelta = new Vector2(LabelWidthPixels, HealthFontSize);

            ApplyToxicRowLayoutSettings();
        }

        private static void ApplyToxicRowLayoutSettings()
        {
            if (_toxicRect == null || !_toxicRect.TryGetComponent<HorizontalLayoutGroup>(out HorizontalLayoutGroup toxicLayout))
            {
                return;
            }

            ModUiLayout.SetEnumProperty(toxicLayout, "childAlignment", 8);
            toxicLayout.childForceExpandWidth = false;
            toxicLayout.childForceExpandHeight = false;
        }

        private static bool TryEnsureOverlay(UIPrefab_InGame ingameUi)
        {
            if (_overlayRoot != null && _healthRect != null && _toxicRect != null
                && _healthLabel != null && _toxicPercentLabel != null)
            {
                TryEnsureToxicIcon(ingameUi);
                return true;
            }

            Transform? main = ModUiRoot.GetMain();
            if (main == null)
            {
                LogOverlayFailureOnce("Main UI layer unavailable");
                return false;
            }

            ModUiAssets assets = CaptureAssets(ingameUi);
            _overlayRoot = ModUiRoot.CreateUiRoot(main, OverlayRootName);
            _overlayRoot.transform.SetAsLastSibling();

            GameObject healthGo = ModUiLayout.CreateChild("Health", _overlayRoot.transform);
            _healthRect = healthGo.GetComponent<RectTransform>();
            _healthRect.localScale = Vector3.one;
            _healthLabel = ModUiFactory.AddText(healthGo, assets, "100", HealthFontSize, ModUiFontStyle.Bold);
            ModUiText.SetBottomRightAlignment(_healthLabel);
            ModUiText.ConfigureTextLayout(_healthLabel, wordWrap: false, ModUiText.OverflowOverflow);
            StretchTextToParent(_healthLabel);

            GameObject toxicRowGo = ModUiLayout.CreateChild("ToxicRow", _overlayRoot.transform);
            _toxicRect = toxicRowGo.GetComponent<RectTransform>();
            _toxicRect.localScale = Vector3.one;

            HorizontalLayoutGroup toxicLayout = toxicRowGo.AddComponent<HorizontalLayoutGroup>();
            toxicLayout.spacing = ToxicRowSpacingPixels;
            ModUiLayout.SetEnumProperty(toxicLayout, "childAlignment", 8);
            toxicLayout.childControlWidth = true;
            toxicLayout.childControlHeight = true;
            toxicLayout.childForceExpandWidth = false;
            toxicLayout.childForceExpandHeight = false;

            GameObject toxicPercentGo = ModUiLayout.CreateChild("ToxicPercent", toxicRowGo.transform);
            ModUiLayout.PrepareLayoutGroupChild(toxicPercentGo.GetComponent<RectTransform>());
            LayoutElement percentLayout = toxicPercentGo.AddComponent<LayoutElement>();
            percentLayout.preferredWidth = ToxicPercentWidthPixels;
            percentLayout.preferredHeight = ToxicFontSize;
            percentLayout.flexibleWidth = 0f;
            _toxicPercentLabel = ModUiFactory.AddText(toxicPercentGo, assets, "0.0%", ToxicFontSize, ModUiFontStyle.Normal);
            ModUiText.SetTopRightAlignment(_toxicPercentLabel);
            ModUiText.ConfigureTextLayout(_toxicPercentLabel, wordWrap: false, ModUiText.OverflowOverflow);
            StretchTextToParent(_toxicPercentLabel);

            TryEnsureToxicIcon(ingameUi);

            _overlayRoot.SetActive(false);
            return true;
        }

        private static void DestroyOverlay()
        {
            if (_overlayRoot != null)
            {
                UnityEngine.Object.Destroy(_overlayRoot);
            }

            _overlayRoot = null;
            _healthRect = null;
            _toxicRect = null;
            _healthLabel = null;
            _toxicPercentLabel = null;
            _toxicIconImage = null;
            _lastMeasuredLeft = new Vector2(float.NaN, float.NaN);
            if (_toxicIconSprite != null)
            {
                UnityEngine.Object.Destroy(_toxicIconSprite);
                _toxicIconSprite = null;
            }
        }

        private static void UpdateOverlayVisibility()
        {
            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(
                    IsEnabled()
                    && _state.Active
                    && IsInventoryVisibleForOverlay());
            }
        }

        private static bool IsInventoryVisibleForOverlay() =>
            FpsUiInventoryLayoutHelper.IsInventoryVisible();

        private static bool TryMeasureInventoryScreenPosition(
            out Vector2 leftLocal,
            out Vector2 topLocal,
            out Vector2 bottomLocal)
        {
            leftLocal = Vector2.zero;
            topLocal = Vector2.zero;
            bottomLocal = Vector2.zero;

            if (_overlayRoot == null)
            {
                return false;
            }

            RectTransform? anchorRect = FpsUiInventoryLayoutHelper.TryGetInventoryFrame();
            if (anchorRect == null)
            {
                return false;
            }

            Vector3[] corners = _cornersBuffer;
            anchorRect.GetWorldCorners(corners);

            RectTransform overlayRect = _overlayRoot.GetComponent<RectTransform>();
            bottomLocal = overlayRect.InverseTransformPoint(corners[0]);
            topLocal = overlayRect.InverseTransformPoint(corners[1]);
            leftLocal = bottomLocal;
            return true;
        }

        private static void ReplayCachedValues()
        {
            if (_ingameUi == null)
            {
                return;
            }

            UpdateHealth(_ingameUi, _state.LastHp, _state.LastMaxHp, _state.LastIsDead || _ingameUi.isDead);
            UpdateConta(_ingameUi, _state.LastConta, _state.LastMaxConta);
        }

        private static ModUiAssets CaptureAssets(UIPrefab_InGame ingameUi)
        {
            Component? template = CurrencyTextProperty?.GetValue(ingameUi) as Component
                ?? KillCountTextProperty?.GetValue(ingameUi) as Component;
            return ModUiAssets.FromTextSource(template?.gameObject ?? ingameUi.gameObject);
        }

        private static void HideVanillaVitals(UIPrefab_InGame ingameUi)
        {
            SetActive(ingameUi.UE_HP_bg?.gameObject, false);
            SetActive(ingameUi.UE_HP_bar?.gameObject, false);
            SetActive(ingameUi.UE_HP_icon?.gameObject, false);
            SetActive(ingameUi.UE_Fx_HPGauge?.gameObject, false);
            ingameUi.SetVisibleOxyGauge(visible: false);
        }

        private static void RestoreVanillaVitals(UIPrefab_InGame ingameUi)
        {
            SetActive(ingameUi.UE_HP_bg?.gameObject, true);
            SetActive(ingameUi.UE_HP_bar?.gameObject, true);
            SetActive(ingameUi.UE_HP_icon?.gameObject, true);
            ingameUi.SetVisibleOxyGauge(visible: true);

            if (_state.LastMaxHp > 0L || _state.LastHp > 0L || _state.LastIsDead)
            {
                ingameUi.OnHpChanged(_state.LastIsDead ? 1L : _state.LastHp, Math.Max(1L, _state.LastMaxHp));
            }

            if (_state.LastMaxConta > 0L)
            {
                ingameUi.OnContaChanged(_state.LastConta, _state.LastMaxConta);
            }
        }

        private static void LogOverlayFailureOnce(string reason)
        {
            if (_loggedOverlayFailure)
            {
                return;
            }

            _loggedOverlayFailure = true;
            ModLog.Warn(Feature, $"FPS UI overlay unavailable — {reason}");
        }

        private static void StretchTextToParent(Component? textComponent)
        {
            if (textComponent?.transform is RectTransform textRect)
            {
                ModUiLayout.Stretch(textRect);
            }
        }

        private static Color ResolveHealthColor(bool isDead) =>
            isDead ? HealthDeadColor : HealthLivingColor;

        private static Color ResolveToxicPercentColor(float percent)
        {
            int tier = Mathf.Clamp((int)(percent / 10f), 0, 9);
            return tier switch
            {
                <= 2 => ToxicPercentGreen,
                <= 5 => ToxicPercentYellowGreen,
                <= 7 => ToxicPercentOrange,
                _ => ToxicPercentRed,
            };
        }

        private static void TryEnsureToxicIcon(UIPrefab_InGame ingameUi)
        {
            if (_toxicRect == null || _toxicIconImage != null)
            {
                return;
            }

            if (TryGetOxyGauge(ingameUi) is not UIPrefab_ProgressBar oxyGauge)
            {
                LogToxicIconFailureOnce("oxyGauge reference missing on in-game UI");
                return;
            }

            float percent = _state.LastMaxConta <= 0L
                ? 0f
                : (float)_state.LastConta / _state.LastMaxConta * 100f;
            Sprite? sprite = TryResolveToxicIconSprite(oxyGauge, percent);
            if (sprite == null)
            {
                LogToxicIconFailureOnce("iconSprites list empty or unavailable on oxy gauge");
                return;
            }

            float height = ToxicIconHeightPixels;
            float width = height * (sprite.rect.width / Mathf.Max(1f, sprite.rect.height));

            GameObject slotGo = ModUiLayout.CreateChild("ToxicIcon", _toxicRect);
            ModUiLayout.PrepareLayoutGroupChild(slotGo.GetComponent<RectTransform>());
            LayoutElement slotLayout = slotGo.AddComponent<LayoutElement>();
            slotLayout.preferredWidth = width;
            slotLayout.preferredHeight = height;

            GameObject visualGo = ModUiLayout.CreateChild("IconVisual", slotGo.transform);
            RectTransform visualRect = visualGo.GetComponent<RectTransform>();
            visualRect.anchorMin = new Vector2(0.5f, 0f);
            visualRect.anchorMax = new Vector2(0.5f, 0f);
            visualRect.pivot = new Vector2(0.5f, 0f);

            _toxicIconImage = visualGo.AddComponent<Image>();
            _toxicIconImage.color = Color.white;
            _toxicIconImage.preserveAspect = true;
            _toxicIconImage.raycastTarget = false;
            ApplyToxicIconSprite(sprite);
        }

        private static UIPrefab_ProgressBar? TryGetOxyGauge(UIPrefab_InGame ingameUi)
        {
            if (OxyGaugeField?.GetValue(ingameUi) is UIPrefab_ProgressBar gauge)
            {
                return gauge;
            }

            return ingameUi.GetComponentInChildren<UIPrefab_ProgressBar>(true);
        }

        private static Sprite? TryResolveToxicIconSprite(UIPrefab_ProgressBar oxyGauge, float percent)
        {
            int tier = Mathf.Clamp((int)(percent / 10f), 0, 9);
            if (IconSpritesField?.GetValue(oxyGauge) is IList sprites && sprites.Count > 0)
            {
                return sprites[Mathf.Min(tier, sprites.Count - 1)] as Sprite;
            }

            return null;
        }

        private static void ApplyToxicIconSprite(Sprite source)
        {
            if (_toxicIconImage == null)
            {
                return;
            }

            if (_toxicIconImage.sprite == _toxicIconSprite && _toxicIconSprite != null)
            {
                _toxicIconImage.gameObject.SetActive(true);
                return;
            }

            if (_toxicIconSprite != null)
            {
                UnityEngine.Object.Destroy(_toxicIconSprite);
            }

            try
            {
                _toxicIconSprite = Sprite.Create(
                    source.texture,
                    source.textureRect,
                    new Vector2(0.5f, 0.5f),
                    source.pixelsPerUnit);
                _toxicIconImage.sprite = _toxicIconSprite;
            }
            catch
            {
                _toxicIconSprite = null;
                _toxicIconImage.sprite = source;
            }

            _toxicIconImage.SetNativeSize();
            float nativeHeight = _toxicIconImage.rectTransform.sizeDelta.y;
            if (nativeHeight > 0f)
            {
                float scale = ToxicIconHeightPixels / nativeHeight;
                _toxicIconImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            _toxicIconImage.gameObject.SetActive(true);
        }

        private static void ApplyToxicIcon(UIPrefab_InGame ingameUi, float percent)
        {
            if (_toxicIconImage == null)
            {
                TryEnsureToxicIcon(ingameUi);
            }

            if (_toxicIconImage == null)
            {
                return;
            }

            if (TryGetOxyGauge(ingameUi) is not UIPrefab_ProgressBar oxyGauge)
            {
                return;
            }

            Sprite? sprite = TryResolveToxicIconSprite(oxyGauge, percent);
            if (sprite == null)
            {
                _toxicIconImage.gameObject.SetActive(false);
                return;
            }

            ApplyToxicIconSprite(sprite);
        }

        private static void LogToxicIconFailureOnce(string reason)
        {
            if (_loggedToxicIconFailure)
            {
                return;
            }

            _loggedToxicIconFailure = true;
            ModLog.Warn(Feature, $"FPS UI toxic icon unavailable — {reason}");
        }

        private static void SetActive(GameObject? go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        private sealed class SessionState
        {
            internal bool Active;
            internal long LastHp;
            internal long LastMaxHp = 1L;
            internal long LastConta;
            internal long LastMaxConta = 1L;
            internal bool LastIsDead;
        }
    }
}

using System.Reflection;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;

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
        private const float LabelWidthPixels = 112f;
        private const float HealthFontSize = 50f;
        private const float ToxicFontSize = 30f;
        private const float HealthNudgeDownPixels = 4f;
        private const float ToxicNudgeUpPixels = 4f;

        private static readonly PropertyInfo? CurrencyTextProperty =
            AccessTools.Property(typeof(UIPrefab_InGame), "UE_Currency");

        private static readonly PropertyInfo? KillCountTextProperty =
            AccessTools.Property(typeof(UIPrefab_InGame), "UE_KillCount");

        private static UIPrefab_InGame? _ingameUi;
        private static SessionState _state = new();
        private static GameObject? _overlayRoot;
        private static RectTransform? _healthRect;
        private static RectTransform? _toxicRect;
        private static Component? _healthLabel;
        private static Component? _toxicLabel;
        private static bool _loggedOverlayFailure;

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

        internal static void ScheduleLayoutRetry() => RefreshLayout();

        internal static void OnUpdate()
        {
            if (!IsEnabled() || !_state.Active)
            {
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
            ModUiText.SetColor(_healthLabel, ResolveHealthColor(curr, maxHp, isDead));
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

            if (_toxicLabel == null)
            {
                return;
            }

            float percent = maxConta <= 0L ? 0f : (float)curr / maxConta * 100f;
            ModUiText.SetText(_toxicLabel, $"{percent:F0}%");
            ModUiText.SetColor(_toxicLabel, ResolveToxicColor(percent));
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
        }

        private static bool TryEnsureOverlay(UIPrefab_InGame ingameUi)
        {
            if (_overlayRoot != null && _healthRect != null && _toxicRect != null
                && _healthLabel != null && _toxicLabel != null)
            {
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

            GameObject toxicGo = ModUiLayout.CreateChild("Toxic", _overlayRoot.transform);
            _toxicRect = toxicGo.GetComponent<RectTransform>();
            _toxicRect.localScale = Vector3.one;
            _toxicLabel = ModUiFactory.AddText(toxicGo, assets, "0%", ToxicFontSize, ModUiFontStyle.Normal);
            ModUiText.SetTopRightAlignment(_toxicLabel);
            ModUiText.ConfigureTextLayout(_toxicLabel, wordWrap: false, ModUiText.OverflowOverflow);
            StretchTextToParent(_toxicLabel);

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
            _toxicLabel = null;
        }

        private static void UpdateOverlayVisibility()
        {
            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(IsEnabled() && _state.Active);
            }
        }

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

            Vector3[] corners = new Vector3[4];
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

        private static Color ResolveHealthColor(long curr, long maxHp, bool isDead)
        {
            if (isDead)
            {
                return new Color32(180, 50, 50, 255);
            }

            float ratio = maxHp <= 0L ? 0f : (float)curr / maxHp;
            return ratio > 0.6f
                ? new Color32(220, 255, 220, 255)
                : ratio > 0.3f
                    ? new Color32(255, 220, 80, 255)
                    : new Color32(255, 80, 80, 255);
        }

        private static Color ResolveToxicColor(float percent) => percent switch
        {
            <= 10f => new Color32(120, 220, 200, 255),
            <= 40f => new Color32(180, 230, 120, 255),
            <= 70f => new Color32(255, 210, 80, 255),
            _ => new Color32(255, 90, 90, 255),
        };

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

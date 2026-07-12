using System.Linq;
using System.Reflection;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.UserInterface.FpsVitalsHud
{
    /// <summary>
    /// Screen-space FPS vitals HUD. Uses a full-screen Main-layer overlay and places labels at
    /// measured inventory coordinates, similar to the ESC menu player list overlay pattern.
    /// </summary>
    internal static class FpsVitalsHudOverlay
    {
        private const string Feature = "Ui";
        private const string OverlayRootName = "MPE_FpsVitalsHud";
        private const float LayoutGapPixels = 20f;
        private const float LabelWidthPixels = 112f;
        private const float HealthFontSize = 50f;
        private const float ToxicFontSize = 30f;
        private const float HealthNudgeDownPixels = 4f;
        private const float ToxicNudgeUpPixels = 4f;

        private static readonly FieldInfo? InventoryUiField =
            AccessTools.Field(typeof(GameMainBase), "inventoryui");

        private static readonly PropertyInfo? InvenFrame1Property =
            AccessTools.Property(typeof(UIPrefab_Inventory), "UE_InvenFrame1");

        private static readonly PropertyInfo? CurrencyTextProperty =
            AccessTools.Property(typeof(UIPrefab_InGame), "UE_Currency");

        private static readonly PropertyInfo? KillCountTextProperty =
            AccessTools.Property(typeof(UIPrefab_InGame), "UE_KillCount");

        private static readonly Dictionary<UIPrefab_InGame, VitalsState> States = [];

        private static GameObject? _overlayRoot;
        private static RectTransform? _healthRect;
        private static RectTransform? _toxicRect;
        private static Component? _healthLabel;
        private static Component? _toxicLabel;
        private static bool _loggedOverlayFailure;

        internal static bool IsEnabled() => ModConfig.EnableFpsVitalsHud.Value;

        internal static void Attach(UIPrefab_InGame ingameUi)
        {
            if (!IsEnabled())
            {
                return;
            }

            VitalsState state = GetOrCreateState(ingameUi);
            if (!TryEnsureOverlay(ingameUi))
            {
                return;
            }

            if (!state.Active)
            {
                HideVanillaVitals(ingameUi);
                state.Active = true;
            }

            RefreshLayout();
            ReplayCachedValues(state, ingameUi);
            UpdateOverlayVisibility();
        }

        internal static void UpdateHealth(UIPrefab_InGame ingameUi, long curr, long maxHp, bool isDead)
        {
            VitalsState state = GetOrCreateState(ingameUi);
            state.LastHp = curr;
            state.LastMaxHp = maxHp;
            state.LastIsDead = isDead;

            if (!IsEnabled())
            {
                return;
            }

            if (!state.Active && !TryEnsureOverlay(ingameUi))
            {
                return;
            }

            if (!state.Active)
            {
                HideVanillaVitals(ingameUi);
                state.Active = true;
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
            VitalsState state = GetOrCreateState(ingameUi);
            state.LastConta = curr;
            state.LastMaxConta = maxConta;

            if (!IsEnabled())
            {
                return;
            }

            if (!state.Active && !TryEnsureOverlay(ingameUi))
            {
                return;
            }

            if (!state.Active)
            {
                HideVanillaVitals(ingameUi);
                state.Active = true;
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

        internal static void RefreshFromConfig()
        {
            bool enabled = IsEnabled();

            if (!enabled)
            {
                foreach (VitalsState state in States.Values.ToList())
                {
                    if (state.IngameUi != null)
                    {
                        RevertToVanilla(state.IngameUi);
                    }
                }

                DestroySharedOverlay();
                return;
            }

            foreach (VitalsState state in States.Values.ToList())
            {
                if (state.IngameUi == null)
                {
                    continue;
                }

                Attach(state.IngameUi);
            }

            foreach (UIPrefab_InGame ingameUi in UnityEngine.Object.FindObjectsByType<UIPrefab_InGame>(FindObjectsSortMode.None))
            {
                Attach(ingameUi);
            }
        }

        internal static void RevertToVanilla(UIPrefab_InGame ingameUi)
        {
            if (!States.TryGetValue(ingameUi, out VitalsState? state))
            {
                return;
            }

            RestoreVanillaVitals(ingameUi, state);
            state.Active = false;

            if (States.Values.All(static s => !s.Active))
            {
                DestroySharedOverlay();
            }
            else
            {
                UpdateOverlayVisibility();
            }
        }

        internal static void ForceHideOxyGauge(UIPrefab_InGame ingameUi)
        {
            if (!IsEnabled())
            {
                return;
            }

            ingameUi.SetVisibleOxyGauge(visible: false);
        }

        private static bool TryEnsureOverlay(UIPrefab_InGame ingameUi)
        {
            if (_overlayRoot != null && _healthRect != null && _toxicRect != null && _healthLabel != null && _toxicLabel != null)
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
            ModUiText.SetTopRightAlignment(_healthLabel);
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

        private static void DestroySharedOverlay()
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
            if (_overlayRoot == null)
            {
                return;
            }

            bool anyActive = States.Values.Any(static state => state.Active);
            _overlayRoot.SetActive(IsEnabled() && anyActive);
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

            RectTransform? anchorRect = TryGetInventoryAnchorRect();
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

        private static RectTransform? TryGetInventoryAnchorRect()
        {
            UIPrefab_Inventory? inventoryUi = TryGetInventoryUi();
            if (inventoryUi == null)
            {
                return null;
            }

            if (InvenFrame1Property?.GetValue(inventoryUi) is Image frame1)
            {
                return frame1.rectTransform;
            }

            return inventoryUi.transform as RectTransform;
        }

        private static ModUiAssets CaptureAssets(UIPrefab_InGame ingameUi)
        {
            Component? template = CurrencyTextProperty?.GetValue(ingameUi) as Component
                ?? KillCountTextProperty?.GetValue(ingameUi) as Component;
            return ModUiAssets.FromTextSource(template?.gameObject ?? ingameUi.gameObject);
        }

        private static VitalsState GetOrCreateState(UIPrefab_InGame ingameUi)
        {
            if (!States.TryGetValue(ingameUi, out VitalsState? state))
            {
                state = new VitalsState { IngameUi = ingameUi };
                States[ingameUi] = state;
            }

            return state;
        }

        private static void HideVanillaVitals(UIPrefab_InGame ingameUi)
        {
            SetActiveIfPresent(ingameUi.UE_HP_bg?.gameObject, active: false);
            SetActiveIfPresent(ingameUi.UE_HP_bar?.gameObject, active: false);
            SetActiveIfPresent(ingameUi.UE_HP_icon?.gameObject, active: false);
            SetActiveIfPresent(ingameUi.UE_Fx_HPGauge?.gameObject, active: false);
            ingameUi.SetVisibleOxyGauge(visible: false);
        }

        private static void LogOverlayFailureOnce(string reason)
        {
            if (_loggedOverlayFailure)
            {
                return;
            }

            _loggedOverlayFailure = true;
            ModLog.Warn(Feature, $"FPS vitals HUD overlay unavailable — {reason}");
        }

        private static void ReplayCachedValues(VitalsState state, UIPrefab_InGame ingameUi)
        {
            UpdateHealth(ingameUi, state.LastHp, state.LastMaxHp, state.LastIsDead || ingameUi.isDead);
            UpdateConta(ingameUi, state.LastConta, state.LastMaxConta);
        }

        private static void RestoreVanillaVitals(UIPrefab_InGame ingameUi, VitalsState state)
        {
            SetActiveIfPresent(ingameUi.UE_HP_bg?.gameObject, active: true);
            SetActiveIfPresent(ingameUi.UE_HP_bar?.gameObject, active: true);
            SetActiveIfPresent(ingameUi.UE_HP_icon?.gameObject, active: true);
            ingameUi.SetVisibleOxyGauge(visible: true);

            if (state.LastMaxHp > 0L || state.LastHp > 0L || state.LastIsDead)
            {
                ingameUi.OnHpChanged(state.LastIsDead ? 1L : state.LastHp, Math.Max(1L, state.LastMaxHp));
            }

            if (state.LastMaxConta > 0L)
            {
                ingameUi.OnContaChanged(state.LastConta, state.LastMaxConta);
            }
        }

        private static Color ResolveHealthColor(long curr, long maxHp, bool isDead)
        {
            if (isDead)
            {
                return new Color32(180, 50, 50, 255);
            }

            float ratio = maxHp <= 0L ? 0f : (float)curr / maxHp;
            if (ratio > 0.6f)
            {
                return new Color32(220, 255, 220, 255);
            }

            if (ratio > 0.3f)
            {
                return new Color32(255, 220, 80, 255);
            }

            return new Color32(255, 80, 80, 255);
        }

        private static Color ResolveToxicColor(float percent)
        {
            if (percent <= 10f)
            {
                return new Color32(120, 220, 200, 255);
            }

            if (percent <= 40f)
            {
                return new Color32(180, 230, 120, 255);
            }

            if (percent <= 70f)
            {
                return new Color32(255, 210, 80, 255);
            }

            return new Color32(255, 90, 90, 255);
        }

        private static void SetActiveIfPresent(GameObject? go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        private static void StretchTextToParent(Component? textComponent)
        {
            if (textComponent == null || textComponent.transform is not RectTransform textRect)
            {
                return;
            }

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static UIPrefab_Inventory? TryGetInventoryUi()
        {
            GameMainBase? main = Hub.Main;
            if (main == null || InventoryUiField == null)
            {
                return null;
            }

            return InventoryUiField.GetValue(main) as UIPrefab_Inventory;
        }

        private sealed class VitalsState
        {
            internal UIPrefab_InGame? IngameUi;
            internal bool Active;
            internal long LastHp;
            internal long LastMaxHp = 1L;
            internal long LastConta;
            internal long LastMaxConta = 1L;
            internal bool LastIsDead;
        }
    }
}

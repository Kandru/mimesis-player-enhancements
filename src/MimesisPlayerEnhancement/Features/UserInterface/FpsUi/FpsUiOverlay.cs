using System.Reflection;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi
{
    internal static class FpsUiOverlay
    {
        private const string ConsumerId = "FpsUi";
        private const float LayoutGapPixels = 20f;
        private const float LabelWidthPixels = 112f;
        private const float HealthFontSize = 50f;
        private const float ToxicFontSize = 30f;
        private const int LayoutRetryFrames = 90;

        private static readonly FieldInfo? InventoryUiField =
            AccessTools.Field(typeof(GameMainBase), "inventoryui");

        private static readonly PropertyInfo? StackCountTextProperty =
            AccessTools.Property(typeof(UIPrefab_Inventory), "UE_stackCount1");

        private static UIPrefab_InGame? _ingameUi;
        private static SessionState _state = new();
        private static RectTransform? _vitalsRoot;
        private static RectTransform? _toxicBand;
        private static RectTransform? _healthBand;
        private static Component? _toxicLabel;
        private static Component? _healthLabel;
        private static int _layoutRetriesRemaining;

        internal static bool IsEnabled() => ModConfig.EnableFpsUi.Value;

        internal static void Attach(UIPrefab_InGame ingameUi)
        {
            if (!IsEnabled())
            {
                return;
            }

            _ingameUi = ingameUi;
            Activate();
        }

        internal static void ScheduleLayoutRetry() => _layoutRetriesRemaining = LayoutRetryFrames;

        internal static void OnUpdate()
        {
            if (!IsEnabled() || _layoutRetriesRemaining <= 0)
            {
                return;
            }

            _layoutRetriesRemaining--;
            TryRefreshLayout();
        }

        internal static void UpdateHealth(UIPrefab_InGame ingameUi, long curr, long maxHp, bool isDead)
        {
            _state.LastHp = curr;
            _state.LastMaxHp = maxHp;
            _state.LastIsDead = isDead;
            if (IsEnabled() && TryActivate(ingameUi))
            {
                ApplyValues();
            }
        }

        internal static void UpdateConta(UIPrefab_InGame ingameUi, long curr, long maxConta)
        {
            _state.LastConta = curr;
            _state.LastMaxConta = maxConta;
            if (IsEnabled() && TryActivate(ingameUi))
            {
                ApplyValues();
            }
        }

        internal static void RefreshFromConfig()
        {
            if (!IsEnabled())
            {
                Deactivate();
                return;
            }

            if (_ingameUi != null)
            {
                Activate();
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

        private static bool TryActivate(UIPrefab_InGame ingameUi)
        {
            _ingameUi = ingameUi;
            if (_state.Active && _vitalsRoot != null)
            {
                return true;
            }

            Activate();
            return _state.Active;
        }

        private static void Activate()
        {
            if (!IsEnabled() || _ingameUi == null)
            {
                return;
            }

            InGameScreenOverlay.Register(ConsumerId);
            HideVanillaVitals(_ingameUi);

            if (!EnsureWidgets(_ingameUi))
            {
                ScheduleLayoutRetry();
                return;
            }

            _state.Active = true;
            ApplyValues();

            if (!TryRefreshLayout())
            {
                ScheduleLayoutRetry();
            }
        }

        private static void Deactivate()
        {
            if (_ingameUi != null && _state.Active)
            {
                RestoreVanillaVitals(_ingameUi);
            }

            _state.Active = false;
            DestroyWidgets();
            InGameScreenOverlay.Unregister(ConsumerId);
            _layoutRetriesRemaining = 0;
        }

        private static bool TryRefreshLayout()
        {
            if (_vitalsRoot == null || _toxicBand == null || _healthBand == null)
            {
                return false;
            }

            RectTransform? frame = TryGetInventoryFrame();
            if (frame == null
                || !InGameScreenOverlay.TryProjectBounds(frame, out float leftX, out float bottomY, out float topY))
            {
                return false;
            }

            float x = leftX - LayoutGapPixels;
            float height = topY - bottomY;

            _vitalsRoot.anchorMin = _vitalsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _vitalsRoot.pivot = new Vector2(1f, 0.5f);
            _vitalsRoot.anchoredPosition = new Vector2(x, (topY + bottomY) * 0.5f);
            _vitalsRoot.sizeDelta = new Vector2(LabelWidthPixels, height);

            ModUiLayout.AnchorTopStrip(_toxicBand, ToxicFontSize);
            ModUiLayout.AnchorBottomStrip(_healthBand, HealthFontSize);
            _vitalsRoot.gameObject.SetActive(true);
            return true;
        }

        private static bool EnsureWidgets(UIPrefab_InGame ingameUi)
        {
            if (_vitalsRoot != null)
            {
                return true;
            }

            RectTransform? overlay = InGameScreenOverlay.EnsureRoot();
            if (overlay == null)
            {
                return false;
            }

            ModUiAssets assets = CaptureAssets(ingameUi);
            _vitalsRoot = ModUiLayout.CreateChild("FpsVitals", overlay).GetComponent<RectTransform>();
            _vitalsRoot.localScale = Vector3.one;
            _vitalsRoot.gameObject.SetActive(false);

            _toxicBand = CreateBand("Toxic", ToxicFontSize, alignTop: true, assets, "0%", out _toxicLabel);
            _healthBand = CreateBand("Health", HealthFontSize, alignTop: false, assets, "100", out _healthLabel);
            return _toxicLabel != null && _healthLabel != null;
        }

        private static RectTransform CreateBand(
            string name,
            float bandHeight,
            bool alignTop,
            ModUiAssets assets,
            string initialText,
            out Component? label)
        {
            RectTransform band = ModUiLayout.CreateChild(name, _vitalsRoot!).GetComponent<RectTransform>();
            band.localScale = Vector3.one;
            label = ModUiFactory.AddText(
                band.gameObject,
                assets,
                initialText,
                bandHeight,
                alignTop ? ModUiFontStyle.Normal : ModUiFontStyle.Bold);
            ModUiText.ConfigureHudLabel(label, alignTop);
            return band;
        }

        private static void DestroyWidgets()
        {
            if (_vitalsRoot != null)
            {
                UnityEngine.Object.Destroy(_vitalsRoot.gameObject);
            }

            _vitalsRoot = null;
            _toxicBand = null;
            _healthBand = null;
            _toxicLabel = null;
            _healthLabel = null;
        }

        private static void ApplyValues()
        {
            if (_healthLabel != null)
            {
                long displayHp = _state.LastIsDead ? 0L : _state.LastHp;
                ModUiText.SetText(_healthLabel, displayHp.ToString());
                ModUiText.SetColor(_healthLabel, ResolveHealthColor(_state.LastHp, _state.LastMaxHp, _state.LastIsDead));
            }

            if (_toxicLabel != null)
            {
                float percent = _state.LastMaxConta <= 0L ? 0f : (float)_state.LastConta / _state.LastMaxConta * 100f;
                ModUiText.SetText(_toxicLabel, $"{percent:F0}%");
                ModUiText.SetColor(_toxicLabel, ResolveToxicColor(percent));
            }
        }

        private static ModUiAssets CaptureAssets(UIPrefab_InGame ingameUi)
        {
            UIPrefab_Inventory? inventoryUi = TryGetInventoryUi();
            Component? template = StackCountTextProperty?.GetValue(inventoryUi) as Component;
            return ModUiAssets.FromTextSource(template?.gameObject ?? ingameUi.gameObject);
        }

        private static RectTransform? TryGetInventoryFrame()
        {
            UIPrefab_Inventory? inventoryUi = TryGetInventoryUi();
            return inventoryUi?.UE_InvenFrame1?.rectTransform
                ?? inventoryUi?.transform as RectTransform;
        }

        private static UIPrefab_Inventory? TryGetInventoryUi()
        {
            if (Hub.Main == null || InventoryUiField == null)
            {
                return null;
            }

            return InventoryUiField.GetValue(Hub.Main) as UIPrefab_Inventory;
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

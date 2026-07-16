using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal sealed class ReplayHudUi : MonoBehaviour
    {
        private ModUiAssets _assets = ModUiAssets.Fallback;
        private Slider _slider = null!;
        private Component _timeLabel = null!;
        private Component _pauseLabel = null!;
        private Button _speedButton = null!;
        private Component _speedLabel = null!;

        private static readonly float[] SpeedSteps = [0.5f, 1f, 1.5f, 2f];
        private int _speedIndex = 1;
        private bool _dragging;

        internal static ReplayHudUi? Create()
        {
            Transform? parent = ModUiRoot.GetTop();
            if (parent == null)
            {
                return null;
            }

            GameObject rootGo = ModUiRoot.CreateUiRoot(parent, "ReplayHudUi");
            ReplayHudUi hud = rootGo.AddComponent<ReplayHudUi>();
            hud.Build(rootGo.transform);
            return hud;
        }

        internal void Show() => gameObject.SetActive(true);

        internal void Hide() => gameObject.SetActive(false);

        internal void Refresh()
        {
            if (_dragging)
            {
                ModUiText.SetText(_timeLabel, ReplayPlaybackEngine.GetPreviewTimeLabel(_slider.value));
                return;
            }

            _slider.SetValueWithoutNotify(ReplayPlaybackEngine.GetProgressNormalized());
            ModUiText.SetText(_timeLabel, ReplayPlaybackEngine.GetTimeLabel());
            ModUiText.SetText(_pauseLabel, ReplayPlaybackEngine.IsPaused ? "Resume" : "Pause");
        }

        internal void Destroy()
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        private void Build(Transform root)
        {
#pragma warning disable CS0618
            UIPrefab_MainMenu? mainMenu = UnityEngine.Object.FindObjectOfType<UIPrefab_MainMenu>();
#pragma warning restore CS0618
            UIPrefab_LoadTram? loadTram = SaveSlotGameAccess.TryFindHiddenLoadTram();
            if (mainMenu != null && loadTram != null
                && ModUiAssets.TryCaptureFromMainMenu(mainMenu, loadTram, out ModUiAssets assets))
            {
                _assets = assets;
            }

            RectTransform panel = ModUiLayout.CreateBand(root, "ReplayHudPanel", 0.18f, 0f, 0.82f, 0.095f);

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.78f);

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 4f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            GameObject timeRow = ModUiLayout.CreateChild("TimeRow", panel);
            LayoutElement timeRowLayout = timeRow.AddComponent<LayoutElement>();
            timeRowLayout.preferredHeight = 20f;
            HorizontalLayoutGroup timeHBox = timeRow.AddComponent<HorizontalLayoutGroup>();
            ModUiLayout.SetEnumProperty(timeHBox, "childAlignment", 3);
            timeHBox.childControlWidth = true;
            timeHBox.childForceExpandWidth = true;

            GameObject timeLabelGo = ModUiLayout.CreateChild("TimeLabel", timeRow.transform);
            _timeLabel = ModUiFactory.AddText(timeLabelGo, _assets, "0:00 / 0:00", 15f, ModUiFontStyle.Normal);
            ModUiText.SetColor(_timeLabel, _assets.TextColor);

            GameObject sliderGo = ModUiLayout.CreateChild("SeekSlider", panel);
            LayoutElement sliderLayout = sliderGo.AddComponent<LayoutElement>();
            sliderLayout.preferredHeight = 10f;
            sliderLayout.minHeight = 10f;
            sliderLayout.flexibleHeight = 0f;
            _slider = sliderGo.AddComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.wholeNumbers = false;

            const float trackHeight = 6f;

            GameObject sliderBg = ModUiLayout.CreateChild("Background", sliderGo.transform);
            RectTransform sliderBgRect = sliderBg.GetComponent<RectTransform>();
            ConfigureThinHorizontalTrack(sliderBgRect, trackHeight);
            Image sliderBgImage = sliderBg.AddComponent<Image>();
            sliderBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            _slider.targetGraphic = sliderBgImage;

            GameObject fillArea = ModUiLayout.CreateChild("Fill Area", sliderGo.transform);
            ConfigureThinHorizontalTrack(fillArea.GetComponent<RectTransform>(), trackHeight);
            GameObject fill = ModUiLayout.CreateChild("Fill", fillArea.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.75f, 0.55f, 0.15f, 1f);
            _slider.fillRect = fillRect;

            GameObject handleSlideArea = ModUiLayout.CreateChild("Handle Slide Area", sliderGo.transform);
            ModUiLayout.Stretch(handleSlideArea.GetComponent<RectTransform>());
            GameObject handle = ModUiLayout.CreateChild("Handle", handleSlideArea.transform);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(8f, 12f);
            _slider.handleRect = handleRect;

            _slider.onValueChanged.AddListener(OnSliderChanged);

            EventTrigger sliderTrigger = sliderGo.AddComponent<EventTrigger>();
            ModUiFactory.AddTrigger(sliderTrigger, EventTriggerType.PointerDown, () => _dragging = true);
            ModUiFactory.AddTrigger(sliderTrigger, EventTriggerType.PointerUp, () =>
            {
                _dragging = false;
                ReplayPlaybackEngine.SeekToNormalized(_slider.value);
            });

            GameObject buttonRowGo = ModUiLayout.CreateChild("ButtonRow", panel);
            LayoutElement buttonRowLayout = buttonRowGo.AddComponent<LayoutElement>();
            buttonRowLayout.preferredHeight = 30f;
            HorizontalLayoutGroup buttonHBox = buttonRowGo.AddComponent<HorizontalLayoutGroup>();
            buttonHBox.spacing = 4f;
            buttonHBox.padding = new RectOffset(0, 0, 0, 0);
            buttonHBox.childControlWidth = true;
            buttonHBox.childForceExpandWidth = true;

            Button pauseButton = ModButton.Create(
                buttonRowGo.transform,
                _assets,
                "Pause",
                expandWidth: true,
                () => ReplayPlaybackEngine.TogglePause());
            SetCompactButton(pauseButton);
            _pauseLabel = ModUiText.FindTextComponent(pauseButton.gameObject)!;
            _speedButton = ModButton.Create(
                buttonRowGo.transform,
                _assets,
                "1x",
                expandWidth: true,
                ToggleSpeed);
            SetCompactButton(_speedButton);
            SetCompactButton(ModButton.Create(
                buttonRowGo.transform,
                _assets,
                "Prev",
                expandWidth: true,
                () => ReplayPlaybackEngine.CycleSpectatorTarget(-1)));
            SetCompactButton(ModButton.Create(
                buttonRowGo.transform,
                _assets,
                "Next",
                expandWidth: true,
                () => ReplayPlaybackEngine.CycleSpectatorTarget(1)));
            SetCompactButton(ModButton.Create(
                buttonRowGo.transform,
                _assets,
                "Exit",
                expandWidth: true,
                () => ReplayPlaybackEngine.ExitToMainMenu()));

            _speedLabel = ModUiText.FindTextComponent(_speedButton.gameObject)!;
            Refresh();
        }

        private static void SetCompactButton(Button button)
        {
            LayoutElement? layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                return;
            }

            layout.preferredHeight = 28f;
            layout.minHeight = 28f;
        }

        private static void ConfigureThinHorizontalTrack(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, -height * 0.5f);
            rect.offsetMax = new Vector2(0f, height * 0.5f);
        }

        private void OnSliderChanged(float value)
        {
            if (_dragging)
            {
                ModUiText.SetText(_timeLabel, ReplayPlaybackEngine.GetPreviewTimeLabel(value));
            }
        }

        private void ToggleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % SpeedSteps.Length;
            float speed = SpeedSteps[_speedIndex];
            ReplayPlaybackEngine.PlaybackSpeed = speed;
            ModUiText.SetText(_speedLabel, $"{speed:0.#}x");
        }
    }
}

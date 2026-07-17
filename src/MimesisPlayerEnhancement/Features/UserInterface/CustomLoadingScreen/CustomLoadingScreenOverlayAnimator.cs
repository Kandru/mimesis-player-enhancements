using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal sealed class CustomLoadingScreenOverlayAnimator : MonoBehaviour
    {
        private RawImage _backgroundImage = null!;
        private RawImage _contentImage = null!;
        private RawImage? _crossfadeImage;
        private CustomLoadingScreenResolvedPhase? _phase;
        private Texture2D[] _textures = [];
        private bool _motionEnabled;
        private int _frameIndex;
        private int _frameDirection = 1;
        private float _frameTimer;
        private bool _playing;
        private bool _fading;
        private bool _fadeOut;
        private bool _crossfading;
        private float _fadeDuration = CustomLoadingScreenConstants.DefaultDepartureFadeSeconds;
        private float _fadeElapsed;
        private float _fadeStartAlpha = 1f;
        private float _overlayAlpha = 1f;
        private float _crossfadeElapsed;
        private float _crossfadeDuration = CustomLoadingScreenConstants.DefaultPhaseCrossfadeSeconds;
        private Color _backgroundOpaque = Color.black;
        private Color _contentOpaque = Color.white;
        private Action? _onFadeComplete;

        internal void Initialize(RawImage backgroundImage, RawImage contentImage, RawImage? crossfadeImage = null)
        {
            _backgroundImage = backgroundImage;
            _contentImage = contentImage;
            _crossfadeImage = crossfadeImage;
            _backgroundImage.texture = Texture2D.whiteTexture;
            if (_crossfadeImage != null)
            {
                _crossfadeImage.gameObject.SetActive(false);
                _crossfadeImage.texture = null;
            }
        }

        internal void Play(CustomLoadingScreenResolvedPhase phase, bool motionEnabled)
        {
            CancelCrossfade();
            _onFadeComplete = null;
            _fadeOut = false;
            CommitPhase(phase, motionEnabled, resetFrame: true);

            if (!_fading)
            {
                ApplyFadeAlpha(1f);
            }
        }

        /// <summary>Crossfade the content layer from the current image to <paramref name="phase"/>
        /// while keeping the black letterbox fully opaque.</summary>
        internal void CrossfadeTo(CustomLoadingScreenResolvedPhase phase, bool motionEnabled, float duration)
        {
            Texture2D[] incoming = CustomLoadingScreenTextureCache.TryGetTextures(phase.ImagePaths);
            if (incoming.Length == 0)
            {
                Play(phase, motionEnabled);
                return;
            }

            if (!_playing || _textures.Length == 0 || _contentImage.texture == null)
            {
                Play(phase, motionEnabled);
                return;
            }

            if (SameImagePaths(_phase, phase))
            {
                Play(phase, motionEnabled);
                return;
            }

            RawImage crossfade = EnsureCrossfadeImage();
            CancelCrossfade(keepLayer: true);

            _phase = phase;
            _motionEnabled = motionEnabled;
            _textures = incoming;
            _frameIndex = 0;
            _frameDirection = 1;
            _frameTimer = 0f;
            _playing = true;

            _backgroundOpaque = phase.BackgroundColor;
            _backgroundOpaque.a = 1f;
            _contentOpaque = Color.white;
            _backgroundImage.texture = Texture2D.whiteTexture;

            crossfade.texture = incoming[0];
            crossfade.uvRect = new Rect(0f, 0f, 1f, 1f);
            crossfade.gameObject.SetActive(true);
            ApplyContentLayerAlpha(_contentImage, 1f);
            ApplyContentLayerAlpha(crossfade, 0f);

            _crossfadeDuration = Mathf.Max(duration, 0.05f);
            _crossfadeElapsed = 0f;
            _crossfading = true;
            ResetUvRect();
        }

        internal void BeginFadeIn(float duration)
        {
            CancelCrossfade();
            _onFadeComplete = null;
            _fadeOut = false;
            _fadeDuration = Mathf.Max(duration, 0.05f);
            _fadeElapsed = 0f;
            _fadeStartAlpha = 0f;
            _fading = true;
            ApplyFadeAlpha(0f);
        }

        internal void BeginFadeOut(float duration, Action? onComplete)
        {
            CancelCrossfade();
            _fadeOut = true;
            _fadeDuration = Mathf.Max(duration, 0.05f);
            _fadeElapsed = 0f;
            _fadeStartAlpha = _overlayAlpha;
            if (_fadeStartAlpha <= 0.001f)
            {
                _fading = false;
                onComplete?.Invoke();
                return;
            }

            _onFadeComplete = onComplete;
            _fading = true;
            ApplyFadeAlpha(_fadeStartAlpha);
        }

        internal void SnapVisible()
        {
            CancelCrossfade();
            _onFadeComplete = null;
            _fadeOut = false;
            _fading = false;
            ApplyFadeAlpha(1f);
        }

        internal void Stop()
        {
            _playing = false;
            _fading = false;
            _fadeOut = false;
            _onFadeComplete = null;
            CancelCrossfade();
            _phase = null;
            _textures = [];
            _contentImage.texture = null;
            ApplyFadeAlpha(1f);
            ResetUvRect();
        }

        internal void RefreshMotion(bool motionEnabled)
        {
            _motionEnabled = motionEnabled;
            if (!_motionEnabled || _textures.Length != 1 || !ShouldUsePanZoom())
            {
                ResetUvRect();
            }
        }

        private void Update()
        {
            if (CustomLoadingScreenSession.HoldThroughDeparture)
            {
                // Cutscenes keep trying to put black/video layers above the loading UI.
                CustomLoadingScreenApplier.SuppressVanillaFullscreenCovers();
                transform.SetAsLastSibling();
            }

            if (_fading)
            {
                _fadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_fadeElapsed / _fadeDuration);
                float alpha = _fadeOut
                    ? Mathf.Lerp(_fadeStartAlpha, 0f, t)
                    : Mathf.Lerp(_fadeStartAlpha, 1f, t);
                ApplyFadeAlpha(alpha);
                if (t >= 1f)
                {
                    _fading = false;
                    Action? complete = _onFadeComplete;
                    _onFadeComplete = null;
                    complete?.Invoke();
                }
            }

            if (_crossfading)
            {
                _crossfadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_crossfadeElapsed / _crossfadeDuration);
                ApplyContentLayerAlpha(_contentImage, 1f - t);
                if (_crossfadeImage != null)
                {
                    ApplyContentLayerAlpha(_crossfadeImage, t);
                }

                if (t >= 1f)
                {
                    FinishCrossfade();
                }
            }

            if (!_playing || _phase == null || _textures.Length == 0 || _crossfading)
            {
                return;
            }

            if (_textures.Length > 1)
            {
                AdvanceFrames();
            }
            else if (ShouldUsePanZoom())
            {
                ApplyPanZoom();
            }
        }

        private void CommitPhase(CustomLoadingScreenResolvedPhase phase, bool motionEnabled, bool resetFrame)
        {
            _phase = phase;
            _motionEnabled = motionEnabled;
            _textures = CustomLoadingScreenTextureCache.TryGetTextures(phase.ImagePaths);
            if (resetFrame)
            {
                _frameIndex = 0;
                _frameDirection = 1;
                _frameTimer = 0f;
            }

            _playing = _textures.Length > 0;

            _backgroundOpaque = phase.BackgroundColor;
            _backgroundOpaque.a = 1f;
            _contentOpaque = Color.white;
            _backgroundImage.texture = Texture2D.whiteTexture;

            if (_textures.Length > 0)
            {
                _contentImage.texture = _textures[0];
            }
            else
            {
                _contentImage.texture = null;
                ModLog.Warn(CustomLoadingScreenConstants.Feature,
                    $"Custom loading screen has no decodable images — {string.Join(", ", phase.ImagePaths)}");
            }

            ResetUvRect();
        }

        private void FinishCrossfade()
        {
            if (_crossfadeImage != null)
            {
                _contentImage.texture = _crossfadeImage.texture;
                _crossfadeImage.texture = null;
                _crossfadeImage.gameObject.SetActive(false);
            }

            _crossfading = false;
            ApplyContentLayerAlpha(_contentImage, 1f);
            ResetUvRect();
        }

        private void CancelCrossfade(bool keepLayer = false)
        {
            if (!_crossfading && (_crossfadeImage == null || !_crossfadeImage.gameObject.activeSelf))
            {
                _crossfading = false;
                return;
            }

            if (_crossfading && _crossfadeImage != null && _crossfadeImage.texture != null)
            {
                // Keep whichever side is more visible so a cancel mid-blend does not flash.
                float incomingAlpha = _crossfadeImage.color.a;
                if (incomingAlpha >= 0.5f)
                {
                    _contentImage.texture = _crossfadeImage.texture;
                }
            }

            _crossfading = false;
            if (_crossfadeImage != null)
            {
                _crossfadeImage.texture = null;
                if (!keepLayer)
                {
                    _crossfadeImage.gameObject.SetActive(false);
                }
            }

            ApplyContentLayerAlpha(_contentImage, 1f);
        }

        private RawImage EnsureCrossfadeImage()
        {
            if (_crossfadeImage != null)
            {
                return _crossfadeImage;
            }

            GameObject imageObject = new(CustomLoadingScreenConstants.OverlayCrossfadeObjectName);
            imageObject.transform.SetParent(transform, worldPositionStays: false);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            RawImage rawImage = imageObject.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            rawImage.gameObject.SetActive(false);
            _crossfadeImage = rawImage;
            return rawImage;
        }

        private void AdvanceFrames()
        {
            if (_phase == null)
            {
                return;
            }

            float frameRate = Mathf.Clamp(_phase.FrameRate, CustomLoadingScreenConstants.MinFrameRate,
                CustomLoadingScreenConstants.MaxFrameRate);
            _frameTimer += Time.unscaledDeltaTime;
            float frameInterval = 1f / frameRate;
            while (_frameTimer >= frameInterval)
            {
                _frameTimer -= frameInterval;
                StepFrame();
            }
        }

        private void StepFrame()
        {
            if (_phase == null || _textures.Length <= 1)
            {
                return;
            }

            int nextIndex = _frameIndex + _frameDirection;
            if (nextIndex >= _textures.Length)
            {
                switch (_phase.Loop)
                {
                    case CustomLoadingScreenLoopMode.PingPong:
                        _frameDirection = -1;
                        nextIndex = _textures.Length - 2;
                        break;
                    case CustomLoadingScreenLoopMode.Once:
                        nextIndex = _textures.Length - 1;
                        break;
                    default:
                        nextIndex = 0;
                        break;
                }
            }
            else if (nextIndex < 0)
            {
                if (_phase.Loop == CustomLoadingScreenLoopMode.PingPong)
                {
                    _frameDirection = 1;
                    nextIndex = 1;
                }
                else
                {
                    nextIndex = 0;
                }
            }

            _frameIndex = Mathf.Clamp(nextIndex, 0, _textures.Length - 1);
            _contentImage.texture = _textures[_frameIndex];
            ResetUvRect();
        }

        private void ApplyPanZoom()
        {
            if (_phase == null)
            {
                return;
            }

            CustomLoadingScreenMotionSettings motion = _phase.Motion;
            float cycleSeconds = Mathf.Max(motion.CycleSeconds, 0.1f);
            float zoom = Mathf.Max(motion.Zoom, 1f);
            float t = (Time.unscaledTime % cycleSeconds) / cycleSeconds;
            float size = 1f / zoom;
            float baseOffset = (1f - size) * 0.5f;
            float panX = baseOffset + Mathf.Sin(t * Mathf.PI * 2f) * 0.02f;
            float panY = baseOffset + Mathf.Cos(t * Mathf.PI * 2f) * 0.015f;
            _contentImage.uvRect = new Rect(panX, panY, size, size);
        }

        private bool ShouldUsePanZoom() =>
            _phase != null && _textures.Length == 1 && _phase.Motion.IsPanZoomEnabled(_motionEnabled);

        private void ApplyFadeAlpha(float alpha)
        {
            _overlayAlpha = alpha;

            Color background = _backgroundOpaque;
            background.a = alpha;
            _backgroundImage.color = background;

            if (_crossfading && _crossfadeImage != null)
            {
                float t = Mathf.Clamp01(_crossfadeElapsed / _crossfadeDuration);
                ApplyContentLayerAlpha(_contentImage, 1f - t);
                ApplyContentLayerAlpha(_crossfadeImage, t);
            }
            else
            {
                ApplyContentLayerAlpha(_contentImage, 1f);
                if (_crossfadeImage != null && _crossfadeImage.gameObject.activeSelf)
                {
                    ApplyContentLayerAlpha(_crossfadeImage, 0f);
                }
            }
        }

        private void ApplyContentLayerAlpha(RawImage image, float relativeAlpha)
        {
            Color content = _contentOpaque;
            content.a = _overlayAlpha * Mathf.Clamp01(relativeAlpha);
            image.color = content;
        }

        private void ResetUvRect()
        {
            _contentImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        private static bool SameImagePaths(
            CustomLoadingScreenResolvedPhase? left,
            CustomLoadingScreenResolvedPhase right)
        {
            if (left == null || left.ImagePaths.Count != right.ImagePaths.Count)
            {
                return false;
            }

            for (int i = 0; i < left.ImagePaths.Count; i++)
            {
                if (!string.Equals(left.ImagePaths[i], right.ImagePaths[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

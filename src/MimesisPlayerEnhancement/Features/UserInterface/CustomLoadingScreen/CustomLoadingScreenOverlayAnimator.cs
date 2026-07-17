using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal sealed class CustomLoadingScreenOverlayAnimator : MonoBehaviour
    {
        private RawImage _backgroundImage = null!;
        private RawImage _contentImage = null!;
        private CustomLoadingScreenResolvedPhase? _phase;
        private Texture2D[] _textures = [];
        private bool _motionEnabled;
        private int _frameIndex;
        private int _frameDirection = 1;
        private float _frameTimer;
        private bool _playing;
        private bool _fading;
        private bool _fadeOut;
        private float _fadeDuration = CustomLoadingScreenConstants.DefaultDepartureFadeSeconds;
        private float _fadeElapsed;
        private float _fadeStartAlpha = 1f;
        private Color _backgroundOpaque = Color.black;
        private Color _contentOpaque = Color.white;
        private Action? _onFadeComplete;

        internal void Initialize(RawImage backgroundImage, RawImage contentImage)
        {
            _backgroundImage = backgroundImage;
            _contentImage = contentImage;
            _backgroundImage.texture = Texture2D.whiteTexture;
        }

        internal void Play(CustomLoadingScreenResolvedPhase phase, bool motionEnabled)
        {
            _onFadeComplete = null;
            _fadeOut = false;
            _phase = phase;
            _motionEnabled = motionEnabled;
            _textures = CustomLoadingScreenTextureCache.TryGetTextures(phase.ImagePaths);
            _frameIndex = 0;
            _frameDirection = 1;
            _frameTimer = 0f;
            _playing = _textures.Length > 0;

            _backgroundOpaque = phase.BackgroundColor;
            _backgroundOpaque.a = 1f;
            _contentOpaque = Color.white;
            _backgroundImage.texture = Texture2D.whiteTexture;

            if (!_fading)
            {
                _backgroundImage.color = _backgroundOpaque;
                _contentImage.color = _contentOpaque;
            }

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

        internal void BeginFadeIn(float duration)
        {
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
            _fadeOut = true;
            _fadeDuration = Mathf.Max(duration, 0.05f);
            _fadeElapsed = 0f;
            _fadeStartAlpha = _contentImage != null ? _contentImage.color.a : 1f;
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

            if (!_playing || _phase == null || _textures.Length == 0)
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
            Color background = _backgroundOpaque;
            background.a = alpha;
            _backgroundImage.color = background;

            Color content = _contentOpaque;
            content.a = alpha;
            _contentImage.color = content;
        }

        private void ResetUvRect()
        {
            _contentImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}

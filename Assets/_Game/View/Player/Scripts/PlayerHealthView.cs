using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Seventh.Core.Events;
using Seventh.Core.Services;
using DG.Tweening;

namespace Seventh.View.Player
{
    [RequireComponent(typeof(Slider))]
    public class PlayerHealthView : MonoBehaviour
    {
        private Slider _healthSlider;
        private Tween _fillTween;

        [Header("Animation Settings")]
        [SerializeField] private float _fillDuration = 0.25f;

        [Header("Face Icon Settings")]
        [SerializeField] private Image _faceImage;
        [SerializeField] private Sprite _healthySprite;
        [SerializeField] private Sprite _mediumSprite;
        [SerializeField] private Sprite _dangerSprite;
        [Range(0f, 1f)][SerializeField] private float _dangerThreshold = 0.3f;
        [Range(0f, 1f)][SerializeField] private float _mediumThreshold = 0.7f;

        [Header("Juice - Panel Shake Settings")]
        [SerializeField] private RectTransform _shakePanel;
        [SerializeField] private float _shakeDuration = 0.2f;
        [SerializeField] private float _shakeStrength = 8f;
        [SerializeField] private int _shakeVibrato = 15;

        [Header("Juice - Face Icon Punch Settings")]
        [SerializeField] private float _facePunchDuration = 0.3f;
        [SerializeField] private float _facePunchScaleMultiplier = 0.2f;

        [Header("Juice - Critical Pulse Settings")]
        [SerializeField] private float _criticalPulseScaleMultiplier = 0.15f;
        [SerializeField] private float _criticalPulseDuration = 0.5f;

        [Header("Juice - Healing Flash Settings")]
        [SerializeField] private Color _healFlashColor = new Color(0f, 1f, 0.5f, 1f);
        [SerializeField] private float _healFlashDuration = 0.15f;

        [Header("Juice - URP Vignette Settings")]
        [SerializeField] private Volume _sceneVolume;
        [SerializeField] private bool _useVignetteJuice = true;
        [SerializeField] private Color _damageVignetteColor = Color.red;
        [Range(0f, 1f)][SerializeField] private float _maxVignetteIntensity = 0.5f;
        [Range(0f, 1f)][SerializeField] private float _vignetteStartThreshold = 0.6f;
        [SerializeField] private float _vignetteTransitionDuration = 0.3f;

        private RectTransform _shakePanelRect;
        private Vector3 _originalPanelLocalPos;
        private Vector3 _originalFaceLocalScale;
        private Image _sliderFillImage;
        private Color _originalFillColor;

        private Tween _shakeTween;
        private Tween _facePunchTween;
        private Tween _pulseTween;
        private Tween _healFlashTween;

        // URP Vignette Tweens and Cache
        private Vignette _vignette;
        private Color _originalVignetteColor;
        private float _originalVignetteIntensity;
        private bool _originalVignetteIntensityEnabled;
        private bool _originalVignetteColorEnabled;
        private Tween _vignetteFlashTween;
        private Tween _vignetteColorTween;

        private float _lastHealthPercentage = 1f;
        private bool _isInitialized = false;

        private void Awake()
        {
            _healthSlider = GetComponent<Slider>();
        }

        private void Start()
        {
            ServiceLocator.Get<IEventBus>().Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);

            if (_faceImage != null && _healthySprite != null)
            {
                _faceImage.sprite = _healthySprite;
            }

            // Cache original positions and scales
            _shakePanelRect = _shakePanel != null ? _shakePanel : GetComponent<RectTransform>();
            if (_shakePanelRect != null)
            {
                _originalPanelLocalPos = _shakePanelRect.localPosition;
            }

            if (_faceImage != null)
            {
                _originalFaceLocalScale = _faceImage.transform.localScale;
            }

            if (_healthSlider != null && _healthSlider.fillRect != null)
            {
                _sliderFillImage = _healthSlider.fillRect.GetComponent<Image>();
                if (_sliderFillImage != null)
                {
                    _originalFillColor = _sliderFillImage.color;
                }
            }

            // Cache URP Vignette
            if (_useVignetteJuice)
            {
                if (_sceneVolume == null)
                {
                    _sceneVolume = FindAnyObjectByType<Volume>();
                }

                if (_sceneVolume != null && _sceneVolume.profile != null)
                {
                    if (_sceneVolume.profile.TryGet(out _vignette))
                    {
                        _originalVignetteColor = _vignette.color.value;
                        _originalVignetteIntensity = _vignette.intensity.value;
                        _originalVignetteIntensityEnabled = _vignette.intensity.overrideState;
                        _originalVignetteColorEnabled = _vignette.color.overrideState;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Get<IEventBus>()?.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);

            _fillTween?.Kill();
            _shakeTween?.Kill();
            _facePunchTween?.Kill();
            _pulseTween?.Kill();
            _healFlashTween?.Kill();
            _vignetteFlashTween?.Kill();
            _vignetteColorTween?.Kill();

            // Restore colors and transforms in case of destruction in intermediate states
            if (_sliderFillImage != null)
            {
                _sliderFillImage.color = _originalFillColor;
            }
            if (_faceImage != null)
            {
                _faceImage.transform.localScale = _originalFaceLocalScale;
            }
            if (_shakePanelRect != null)
            {
                _shakePanelRect.localPosition = _originalPanelLocalPos;
            }

            // Restore URP Vignette
            if (_vignette != null)
            {
                _vignette.color.value = _originalVignetteColor;
                _vignette.intensity.value = _originalVignetteIntensity;
                _vignette.intensity.overrideState = _originalVignetteIntensityEnabled;
                _vignette.color.overrideState = _originalVignetteColorEnabled;
            }
        }

        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            if (evt.MaxHealth <= 0) return;

            float targetValue = (float)evt.CurrentHealth / evt.MaxHealth;

            if (!_isInitialized)
            {
                _lastHealthPercentage = targetValue;
                _isInitialized = true;

                if (_healthSlider != null)
                {
                    _healthSlider.value = targetValue;
                }

                UpdateFaceSprite(targetValue);
                UpdateCriticalPulse(targetValue);
                UpdateVignetteState(targetValue);
                return;
            }

            // Damage feedback
            if (targetValue < _lastHealthPercentage)
            {
                // UI Shake
                if (_shakePanelRect != null)
                {
                    _shakeTween?.Kill();
                    _shakePanelRect.localPosition = _originalPanelLocalPos;
                    _shakeTween = _shakePanelRect.DOShakePosition(_shakeDuration, _shakeStrength, _shakeVibrato, fadeOut: true);
                }

                // Face Punch
                if (_faceImage != null)
                {
                    _facePunchTween?.Kill();
                    _faceImage.transform.localScale = _originalFaceLocalScale;
                    _facePunchTween = _faceImage.transform.DOPunchScale(_originalFaceLocalScale * _facePunchScaleMultiplier, _facePunchDuration, 5, 0.5f);
                }
            }
            // Healing feedback
            else if (targetValue > _lastHealthPercentage)
            {
                // Celebratory punch & flash if transitioning back to good health (> 70%)
                if (_lastHealthPercentage < _mediumThreshold && targetValue >= _mediumThreshold)
                {
                    if (_faceImage != null)
                    {
                        _facePunchTween?.Kill();
                        _faceImage.transform.localScale = _originalFaceLocalScale;
                        _facePunchTween = _faceImage.transform.DOPunchScale(_originalFaceLocalScale * (_facePunchScaleMultiplier * 1.5f), _facePunchDuration * 1.5f, 8, 0.5f);
                    }

                    if (_sliderFillImage != null)
                    {
                        _healFlashTween?.Kill();
                        _sliderFillImage.color = _originalFillColor;
                        _healFlashTween = _sliderFillImage.DOColor(_healFlashColor, _healFlashDuration)
                            .SetLoops(4, LoopType.Yoyo)
                            .OnComplete(() => _sliderFillImage.color = _originalFillColor);
                    }
                }
            }

            _lastHealthPercentage = targetValue;

            if (_healthSlider != null)
            {
                _fillTween?.Kill();
                _fillTween = _healthSlider.DOValue(targetValue, _fillDuration)
                    .SetEase(Ease.OutQuad);
            }

            UpdateFaceSprite(targetValue);
            UpdateCriticalPulse(targetValue);
            UpdateVignetteState(targetValue);
        }

        private void UpdateFaceSprite(float healthPercentage)
        {
            if (_faceImage == null) return;

            Sprite selectedSprite = null;
            if (healthPercentage <= _dangerThreshold)
            {
                selectedSprite = _dangerSprite;
            }
            else if (healthPercentage <= _mediumThreshold)
            {
                selectedSprite = _mediumSprite;
            }
            else
            {
                selectedSprite = _healthySprite;
            }

            if (selectedSprite != null)
            {
                _faceImage.sprite = selectedSprite;
            }
        }

        private void UpdateCriticalPulse(float healthPercentage)
        {
            if (healthPercentage <= _dangerThreshold)
            {
                if (_pulseTween == null && _faceImage != null)
                {
                    _pulseTween = _faceImage.transform.DOScale(_originalFaceLocalScale * (1f + _criticalPulseScaleMultiplier), _criticalPulseDuration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                }
            }
            else
            {
                if (_pulseTween != null)
                {
                    _pulseTween.Kill();
                    _pulseTween = null;
                    if (_faceImage != null)
                    {
                        _faceImage.transform.localScale = _originalFaceLocalScale;
                    }
                }
            }
        }

        private void UpdateVignetteState(float healthPercentage)
        {
            if (_vignette == null || !_useVignetteJuice) return;

            float t = 0f;
            if (healthPercentage < _vignetteStartThreshold)
            {
                // Map healthPercentage from [_vignetteStartThreshold, 0] to [0, 1]
                t = 1f - (healthPercentage / _vignetteStartThreshold);
            }

            float targetIntensity = Mathf.Lerp(_originalVignetteIntensity, _maxVignetteIntensity, t);
            Color targetColor = Color.Lerp(_originalVignetteColor, _damageVignetteColor, t);

            _vignette.intensity.overrideState = true;
            _vignette.color.overrideState = true;

            _vignetteFlashTween?.Kill();
            _vignetteColorTween?.Kill();

            _vignetteFlashTween = DOTween.To(() => _vignette.intensity.value, x => _vignette.intensity.value = x, targetIntensity, _vignetteTransitionDuration)
                .SetEase(Ease.OutQuad);

            _vignetteColorTween = DOTween.To(() => _vignette.color.value, x => _vignette.color.value = x, targetColor, _vignetteTransitionDuration)
                .SetEase(Ease.OutQuad);
        }
    }
}

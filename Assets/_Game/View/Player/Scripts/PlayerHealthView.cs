using UnityEngine;
using UnityEngine.UI;
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
        }

        private void OnDestroy()
        {
            ServiceLocator.Get<IEventBus>()?.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);

            _fillTween?.Kill();
        }

        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            if (evt.MaxHealth <= 0) return;

            float targetValue = (float)evt.CurrentHealth / evt.MaxHealth;

            if (_healthSlider != null)
            {
                _fillTween?.Kill();
                _fillTween = _healthSlider.DOValue(targetValue, _fillDuration)
                    .SetEase(Ease.OutQuad);
            }

            UpdateFaceSprite(targetValue);
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
    }
}

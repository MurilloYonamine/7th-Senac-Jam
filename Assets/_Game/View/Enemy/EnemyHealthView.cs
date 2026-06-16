using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Seventh.Gameplay.Health;

namespace Seventh.View.Enemy
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Slider))]
    public class EnemyHealthView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider _healthSlider;

        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _visibleDuration = 3f;
        [SerializeField] private float _fillDuration = 0.2f;
        [SerializeField] private bool _hideWhenFull = true;

        private HealthBase _health;
        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;
        private Tween _fillTween;
        private Tween _alphaTween;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _health = GetComponentInParent<HealthBase>();
            _healthSlider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnHealthChanged += HandleHealthChanged;
                _health.OnDeath += HandleDeath;
            }

            if (_hideWhenFull)
            {
                _canvasGroup.alpha = 0f;
            }
            else
            {
                _canvasGroup.alpha = 1f;
            }

            if (_health != null && _healthSlider != null)
            {
                _healthSlider.value = (float)_health.CurrentHealth / _health.MaxHealth;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleHealthChanged;
                _health.OnDeath -= HandleDeath;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _fillTween?.Kill();
            _alphaTween?.Kill();
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (_healthSlider == null || max <= 0) return;

            float targetValue = (float)current / max;
            _fillTween?.Kill();
            _fillTween = _healthSlider.DOValue(targetValue, _fillDuration)
                .SetEase(Ease.OutQuad);

            if (_hideWhenFull)
            {
                if (_fadeCoroutine != null)
                {
                    StopCoroutine(_fadeCoroutine);
                }
                _fadeCoroutine = StartCoroutine(ShowAndHideRoutine());
            }
        }

        private void HandleDeath()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _alphaTween?.Kill();
            _alphaTween = _canvasGroup.DOFade(0f, _fadeDuration);
        }

        private IEnumerator ShowAndHideRoutine()
        {
            _alphaTween?.Kill();
            _alphaTween = _canvasGroup.DOFade(1f, _fadeDuration);

            yield return new WaitForSeconds(_visibleDuration);

            if (_health != null && _health.CurrentHealth > 0)
            {
                _alphaTween?.Kill();
                _alphaTween = _canvasGroup.DOFade(0f, _fadeDuration);
            }

            _fadeCoroutine = null;
        }
    }
}

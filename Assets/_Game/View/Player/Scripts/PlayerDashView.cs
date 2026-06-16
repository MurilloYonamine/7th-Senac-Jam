using UnityEngine;
using UnityEngine.UI;
using Seventh.Core.Events;
using Seventh.Core.Services;
using DG.Tweening; 

namespace Seventh.View.Player
{
    [RequireComponent(typeof(Slider))]
    [RequireComponent(typeof(CanvasGroup))] 
    public class PlayerDashView : MonoBehaviour
    {
        private Slider _dashCooldownSlider;
        private CanvasGroup _canvasGroup;
        private Tween _fadeTween;

        private void Awake()
        {
            _dashCooldownSlider = GetComponent<Slider>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            ServiceLocator.Get<IEventBus>().Subscribe<PlayerDashCooldownEvent>(OnDashCooldownChanged);
            
            _canvasGroup.alpha = 0f;
            _dashCooldownSlider.value = 1f;
        }

        private void OnDestroy()
        {
            var eventBus = ServiceLocator.Get<IEventBus>();
            if (eventBus != null)
            {
                eventBus.Unsubscribe<PlayerDashCooldownEvent>(OnDashCooldownChanged);
            }

            _fadeTween?.Kill();
        }

        private void OnDashCooldownChanged(PlayerDashCooldownEvent evt)
        {
            if (evt.IsOnCooldown)
            {
                if (_canvasGroup.alpha < 1f && (_fadeTween == null || !_fadeTween.IsActive()))
                {
                    _fadeTween?.Kill(); 
                    _fadeTween = _canvasGroup.DOFade(1f, 0.15f);
                }

                float progress = (evt.CooldownMaxTime - evt.CooldownTimeRemaining) / evt.CooldownMaxTime;
                _dashCooldownSlider.value = progress;
            }
            else
            {
                _dashCooldownSlider.value = 1f;

                _fadeTween?.Kill();
                _fadeTween = _canvasGroup.DOFade(0f, 0.4f); 
            }
        }
    }
}
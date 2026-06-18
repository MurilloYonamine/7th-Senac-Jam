using System.Collections;
using UnityEngine;
using DG.Tweening;
using Seventh.Core.Events;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;
using Seventh.Gameplay.Health;

namespace Seventh.Gameplay.Player
{
    public class PlayerHealth : Health.HealthBase
    {
        [Header("Eternal Poison Settings")]
        [SerializeField] private int _poisonDamage = 5;
        [SerializeField] private float _poisonInterval = 15f;

        [Header("Heal Settings")]
        [SerializeField] private bool _healOnKill = true; // Se true, cura por abate. Se false, cura por tapa.
        [SerializeField] private int _healPerDefeat = 10;
        [SerializeField] private int _healPerHit = 2;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _hurtSFX;
        [Range(0f, 1f)][SerializeField] private float _hurtSFXVolume = 1f;
        [SerializeField] private AudioClip _healSFX;
        [Range(0f, 1f)][SerializeField] private float _healSFXVolume = 1f;
        [SerializeField] private AudioClip _deathSFX;
        [Range(0f, 1f)][SerializeField] private float _deathSFXVolume = 1f;
        [SerializeField] private AudioClip _lowHealthWarningSFX;
        [Range(0f, 1f)][SerializeField] private float _lowHealthWarningSFXVolume = 1f;
        [Range(0f, 1f)][SerializeField] private float _lowHealthWarningThreshold = 0.5f;

        [Header("Juice Settings")]
        [SerializeField] private float _flashDuration = 0.08f;
        [SerializeField] private float _shakeDuration = 0.15f;
        [SerializeField] private float _shakeStrength = 0.15f;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _visualModel;

        private IEventBus _eventBus;
        private IAudioService _audioService;
        private Coroutine _poisonCoroutine;
        private Coroutine _flashCoroutine;
        private bool _isLowHealthWarningPlaying;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
            _audioService = ServiceLocator.Get<IAudioService>();
            PublishHealth();
            UpdateLowHealthWarning();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (_visualModel == null)
            {
                _visualModel = _spriteRenderer != null ? _spriteRenderer.transform : transform;
            }

            _eventBus?.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            _poisonCoroutine = StartCoroutine(PoisonRoutine());
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);

            if (_poisonCoroutine != null)
            {
                StopCoroutine(_poisonCoroutine);
                _poisonCoroutine = null;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
        }

        protected override void HandleHealthChanged(int current, int max)
        {
            base.HandleHealthChanged(current, max);
            PublishHealth();
            UpdateLowHealthWarning();
        }

        private void PublishHealth()
        {
            _eventBus?.Publish(new PlayerHealthChangedEvent(CurrentHealth, MaxHealth));
        }

        private IEnumerator PoisonRoutine()
        {
            var wait = new WaitForSeconds(_poisonInterval);
            while (true)
            {
                yield return wait;
                if (CurrentHealth > 0)
                {
                    TakeDamage(new DamageInfo(_poisonDamage, 0f, Vector2.zero, null, HitIntensity.Light, isSilent: true));
                }
            }
        }

        public void OnSuccessfulHit()
        {
            if (!_healOnKill)
            {
                Heal(_healPerHit);
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            if (_healOnKill)
            {
                Heal(_healPerDefeat);
            }
        }

        public override void TakeDamage(DamageInfo damageInfo)
        {
            int prevHealth = CurrentHealth;
            base.TakeDamage(damageInfo);
            if (CurrentHealth < prevHealth && CurrentHealth > 0)
            {
                if (!damageInfo.IsSilent && _audioService != null && _hurtSFX != null)
                {
                    _audioService.PlaySFX(_hurtSFX, new AudioSettings(volumeOffset: _hurtSFXVolume - 1f));
                }

                // Apply Red Flash Effect
                if (!damageInfo.IsSilent && _spriteRenderer != null)
                {
                    if (_flashCoroutine != null)
                    {
                        StopCoroutine(_flashCoroutine);
                    }
                    _flashCoroutine = StartCoroutine(RedFlashRoutine(_flashDuration));
                }

                // Apply Shake Effect
                if (!damageInfo.IsSilent && _visualModel != null)
                {
                    _visualModel.DOKill();
                    _visualModel.DOShakePosition(_shakeDuration, _shakeStrength, 15, 90f, false, true);
                }
            }
        }

        public override void Heal(int amount)
        {
            int prevHealth = CurrentHealth;
            base.Heal(amount);
            if (CurrentHealth > prevHealth && _audioService != null && _healSFX != null)
            {
                _audioService.PlaySFX(_healSFX, new AudioSettings(volumeOffset: _healSFXVolume - 1f));
            }
        }

        protected override void Die()
        {
            base.Die();
            if (_isLowHealthWarningPlaying && _audioService != null && _lowHealthWarningSFX != null)
            {
                _isLowHealthWarningPlaying = false;
                _audioService.StopSFX(_lowHealthWarningSFX);
            }
            if (_audioService != null && _deathSFX != null)
            {
                _audioService.PlaySFX(_deathSFX, new AudioSettings(volumeOffset: _deathSFXVolume - 1f));
            }
        }

        private void UpdateLowHealthWarning()
        {
            if (_audioService == null || _lowHealthWarningSFX == null) return;

            float healthPercentage = MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 1f;

            if (CurrentHealth > 0 && healthPercentage <= _lowHealthWarningThreshold)
            {
                if (!_isLowHealthWarningPlaying)
                {
                    _isLowHealthWarningPlaying = true;
                    _audioService.PlaySFX(_lowHealthWarningSFX, new AudioSettings(
                        volumeOffset: _lowHealthWarningSFXVolume - 1f,
                        loop: true
                    ));
                }
            }
            else
            {
                if (_isLowHealthWarningPlaying)
                {
                    _isLowHealthWarningPlaying = false;
                    _audioService.StopSFX(_lowHealthWarningSFX);
                }
            }
        }

        private IEnumerator RedFlashRoutine(float duration)
        {
            if (_spriteRenderer == null) yield break;

            _spriteRenderer.color = new Color(1f, 0.2f, 0.2f, 1f); // Red tint
            yield return new WaitForSeconds(duration);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white; // Restore default
            }
            _flashCoroutine = null;
        }
    }
}

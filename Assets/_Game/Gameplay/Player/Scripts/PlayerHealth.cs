using System.Collections;
using UnityEngine;
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

        private IEventBus _eventBus;
        private IAudioService _audioService;
        private Coroutine _poisonCoroutine;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
            _audioService = ServiceLocator.Get<IAudioService>();
            PublishHealth();

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
        }

        protected override void HandleHealthChanged(int current, int max)
        {
            base.HandleHealthChanged(current, max);
            PublishHealth();
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
            if (_audioService != null && _deathSFX != null)
            {
                _audioService.PlaySFX(_deathSFX, new AudioSettings(volumeOffset: _deathSFXVolume - 1f));
            }
        }
    }
}

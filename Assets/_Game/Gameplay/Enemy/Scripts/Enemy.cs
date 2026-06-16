using UnityEngine;
using Seventh.Gameplay.Health;
using Seventh.Core.Events;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;

namespace Seventh.Gameplay.Enemies
{
    [RequireComponent(typeof(HealthBase))]
    public class Enemy : MonoBehaviour
    {
        protected HealthBase HealthComponent { get; private set; }

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _deathSFX;
        [Range(0f, 1f)][SerializeField] private float _deathSFXVolume = 1f;

        protected virtual void Awake()
        {
            HealthComponent = GetComponent<HealthBase>();
        }

        protected virtual void OnEnable()
        {
            if (HealthComponent != null)
            {
                HealthComponent.OnDamageTaken += HandleDamageTaken;
                HealthComponent.OnDeath += HandleDeath;
            }
        }

        protected virtual void OnDisable()
        {
            if (HealthComponent != null)
            {
                HealthComponent.OnDamageTaken -= HandleDamageTaken;
                HealthComponent.OnDeath -= HandleDeath;
            }
        }

        protected virtual void HandleDamageTaken(DamageInfo damageInfo)
        {
        }

        protected virtual void HandleDeath()
        {
            Debug.Log($"{gameObject.name} has died!");

            var audioService = ServiceLocator.Get<IAudioService>();
            if (audioService != null && _deathSFX != null)
            {
                audioService.PlaySFX(_deathSFX, new AudioSettings(volumeOffset: _deathSFXVolume - 1f, spatialPosition: transform.position));
            }

            ServiceLocator.Get<IEventBus>()?.Publish(new EnemyDefeatedEvent(gameObject));
        }
    }
}

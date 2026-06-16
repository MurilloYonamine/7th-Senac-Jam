using UnityEngine;
using Seventh.Gameplay.Health;
using Seventh.Core.Events;
using Seventh.Core.Services;

namespace Seventh.Gameplay.Enemies
{
    [RequireComponent(typeof(HealthBase))]
    public class Enemy : MonoBehaviour
    {
        protected HealthBase HealthComponent { get; private set; }

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
            ServiceLocator.Get<IEventBus>()?.Publish(new EnemyDefeatedEvent(gameObject));
        }
    }
}

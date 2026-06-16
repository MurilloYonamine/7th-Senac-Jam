using System;
using UnityEngine;

namespace Seventh.Gameplay.Health
{
    public class HealthBase : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private int _currentHealth;

        public int MaxHealth => _maxHealth;
        public int CurrentHealth => _currentHealth;

        public event Action<int, int> OnHealthChanged; 
        public event Action<DamageInfo> OnDamageTaken;
        public event Action OnDeath;

        private bool _isDead;

        protected virtual void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public virtual void TakeDamage(DamageInfo damageInfo)
        {
            if (_isDead) return;

            int damage = damageInfo.DamageAmount;
            if (damage <= 0) return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);

            OnDamageTaken?.Invoke(damageInfo);
            HandleHealthChanged(_currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        public virtual void Heal(int amount)
        {
            if (_isDead || amount <= 0) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            HandleHealthChanged(_currentHealth, _maxHealth);
        }

        protected virtual void HandleHealthChanged(int current, int max)
        {
            OnHealthChanged?.Invoke(current, max);
        }

        protected virtual void Die()
        {
            _isDead = true;
            OnDeath?.Invoke();
        }

        public virtual void ResetHealth()
        {
            _currentHealth = _maxHealth;
            _isDead = false;
            HandleHealthChanged(_currentHealth, _maxHealth);
        }
    }
}

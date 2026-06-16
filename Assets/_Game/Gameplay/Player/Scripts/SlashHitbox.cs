using UnityEngine;
using Seventh.Gameplay.Health;

namespace Seventh.Gameplay.Player
{
    [RequireComponent(typeof(Collider2D))]
    public class SlashHitbox : MonoBehaviour
    {
        private int _comboStep = 1;
        private int _damage = 0;
        private float _knockback = 0f;
        private GameObject _attacker;

        public void Initialize(int comboStep, int damage, float knockback, GameObject attacker)
        {
            _comboStep = comboStep;
            _damage = damage;
            _knockback = knockback;
            _attacker = attacker;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Evita que o atacante cause dano a si mesmo
            if (_attacker != null && (other.gameObject == _attacker || other.transform.IsChildOf(_attacker.transform)))
            {
                return;
            }

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector2 pushDirection = transform.right;

                HitIntensity intensity;
                switch (_comboStep)
                {
                    case 3: intensity = HitIntensity.Heavy; break;
                    case 2: intensity = HitIntensity.Medium; break;
                    default: intensity = HitIntensity.Light; break;
                }

                DamageInfo damageInfo = new DamageInfo(_damage, _knockback, pushDirection, _attacker, intensity);
                damageable.TakeDamage(damageInfo);

                // Se o atacante for o Player, notifica o acerto para cura por hit
                if (_attacker != null)
                {
                    PlayerHealth playerHealth = _attacker.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.OnSuccessfulHit();
                    }
                }
            }
        }
    }
}

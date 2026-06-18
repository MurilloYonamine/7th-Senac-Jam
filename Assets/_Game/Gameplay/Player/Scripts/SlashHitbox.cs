using UnityEngine;
using Seventh.Gameplay.Health;

namespace Seventh.Gameplay.Player
{
    [RequireComponent(typeof(Collider2D))]
    public class SlashHitbox : MonoBehaviour
    {
        private int _damage = 0;
        private float _knockback = 0f;
        private GameObject _attacker;

        public void Initialize(int damage, float knockback, GameObject attacker)
        {
            _damage = damage;
            _knockback = knockback;
            _attacker = attacker;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_attacker != null && (other.gameObject == _attacker || other.transform.IsChildOf(_attacker.transform)))
            {
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Vector2 pushDirection = transform.right;

                DamageInfo damageInfo = new DamageInfo(_damage, _knockback, pushDirection, _attacker, HitIntensity.Light);
                damageable.TakeDamage(damageInfo);

                if (_attacker != null)
                {
                    PlayerHealth playerHealth = _attacker.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.OnSuccessfulHit();
                    }

                    PlayerAttack playerAttack = _attacker.GetComponent<PlayerAttack>();
                    if (playerAttack != null)
                    {
                        playerAttack.OnHitEnemy();
                    }
                }
            }
        }
    }
}
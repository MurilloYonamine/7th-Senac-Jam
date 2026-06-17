using Seventh.Gameplay.Health;
using UnityEngine;

namespace Seventh.Gameplay.Enemy
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 10f;
        [SerializeField] private int _damage = 1;
        [SerializeField] private float _knockback = 5f;
        
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            _rb.linearVelocity = transform.right * _speed;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out IDamageable damageable))
            {
                DamageInfo damageInfo = new DamageInfo(
                    _damage, 
                    _knockback, 
                    transform.right, 
                    gameObject, 
                    HitIntensity.Light, 
                    false);

                damageable.TakeDamage(damageInfo);
            }
            
            // Destroy on wall or player
            Destroy(gameObject);
        }
    }
}

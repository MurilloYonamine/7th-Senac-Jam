using DG.Tweening;
using Seventh.Gameplay.Health;
using UnityEngine;

namespace Seventh.Gameplay.Enemy
{
    [RequireComponent(typeof(LineRenderer))]
    public class EnemyHitscanShooter : MonoBehaviour
    {
        [SerializeField] private Transform _firePoint;
        [SerializeField] private float _fireRate = 3f;
        [SerializeField] private float _maxDistance = 15f;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private int _damage = 2;
        [SerializeField] private float _knockback = 8f;

        [Header("Visuals")]
        [SerializeField] private float _laserDuration = 0.15f;
        
        private LineRenderer _lineRenderer;
        private float _fireTimer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;
        }

        [SerializeField] private bool _autoShoot = false;

        private void Update()
        {
            if (!_autoShoot) return;

            _fireTimer += Time.deltaTime;

            if (_fireTimer >= _fireRate)
            {
                _fireTimer = 0f;
                Shoot();
            }
        }

        private void Shoot()
        {
            if (_firePoint == null) return;

            Vector2 origin = _firePoint.position;
            Vector2 direction = _firePoint.right;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, _maxDistance, _hitMask);
            
            Vector2 endPosition = origin + (direction * _maxDistance);

            if (hit.collider != null)
            {
                endPosition = hit.point;

                if (hit.collider.TryGetComponent(out IDamageable damageable))
                {
                    DamageInfo damageInfo = new DamageInfo(
                        _damage,
                        _knockback,
                        direction,
                        gameObject,
                        HitIntensity.Medium,
                        false);

                    damageable.TakeDamage(damageInfo);
                }
            }

            AnimateLaser(origin, endPosition);
        }

        private void AnimateLaser(Vector3 startPos, Vector3 endPos)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);

            Color startColor = _lineRenderer.startColor;
            Color endColor = _lineRenderer.endColor;
            
            startColor.a = 1f;
            endColor.a = 1f;
            _lineRenderer.startColor = startColor;
            _lineRenderer.endColor = endColor;

            _lineRenderer.DOKill();

            DOVirtual.Float(1f, 0f, _laserDuration, (alpha) =>
            {
                startColor.a = alpha;
                endColor.a = alpha;
                _lineRenderer.startColor = startColor;
                _lineRenderer.endColor = endColor;
            }).OnComplete(() =>
            {
                _lineRenderer.enabled = false;
            });
        }
    }
}

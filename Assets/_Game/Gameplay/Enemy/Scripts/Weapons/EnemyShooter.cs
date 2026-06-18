using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Seventh.Gameplay.Enemy
{
    [RequireComponent(typeof(LineRenderer))]
    public class EnemyShooter : MonoBehaviour
    {
        [SerializeField] private Transform _firePoint;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _fireRate = 2f;
        
        [Header("Laser Warning Settings")]
        [SerializeField] private float _warningDuration = 0.4f;
        [SerializeField] private float _maxLaserDistance = 15f;
        [SerializeField] private LayerMask _laserMask;
        [SerializeField] private Color _laserColor = new Color(1f, 0f, 0f, 0.4f); // Red warning color
        [SerializeField] private float _laserWidth = 0.05f;

        [Header("Recoil Animation")]
        [SerializeField] private Transform _enemyModelTransform;
        [SerializeField] private float _recoilDistance = 0.2f;
        [SerializeField] private float _recoilDuration = 0.15f;

        private LineRenderer _lineRenderer;
        private float _fireTimer;
        private bool _isShootingSequence;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer != null)
            {
                _lineRenderer.startWidth = _laserWidth;
                _lineRenderer.endWidth = _laserWidth;
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                _lineRenderer.startColor = _laserColor;
                _lineRenderer.endColor = _laserColor;
                _lineRenderer.enabled = false;
            }
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
            if (_isShootingSequence) return;
            StartCoroutine(ShootSequenceRoutine());
        }

        private IEnumerator ShootSequenceRoutine()
        {
            if (_firePoint == null || _projectilePrefab == null) yield break;

            _isShootingSequence = true;
            
            // Phase 1: Show warning laser
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = true;
                float elapsed = 0f;
                while (elapsed < _warningDuration)
                {
                    if (_firePoint == null) break;
                    Vector2 origin = _firePoint.position;
                    Vector2 direction = _firePoint.right;
                    RaycastHit2D hit = Physics2D.Raycast(origin, direction, _maxLaserDistance, _laserMask);
                    Vector3 endPosition = (hit.collider != null) ? (Vector3)hit.point : (Vector3)(origin + direction * _maxLaserDistance);

                    _lineRenderer.SetPosition(0, origin);
                    _lineRenderer.SetPosition(1, endPosition);

                    elapsed += Time.deltaTime;
                    yield return null;
                }
                if (_lineRenderer != null)
                {
                    _lineRenderer.enabled = false;
                }
            }
            else
            {
                yield return new WaitForSeconds(_warningDuration);
            }

            // Phase 2: Fire physical projectile
            if (_firePoint != null)
            {
                Instantiate(_projectilePrefab, _firePoint.position, _firePoint.rotation);
                ApplyRecoil();
            }

            _isShootingSequence = false;
        }

        private void ApplyRecoil()
        {
            if (_enemyModelTransform == null) return;

            _enemyModelTransform.DOKill(true);
            Vector3 recoilPunch = -_firePoint.right * _recoilDistance;
            _enemyModelTransform.DOPunchPosition(recoilPunch, _recoilDuration, vibrato: 1, elasticity: 0.5f);
        }
    }
}

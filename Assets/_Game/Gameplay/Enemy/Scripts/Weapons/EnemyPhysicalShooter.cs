using DG.Tweening;
using UnityEngine;

namespace Seventh.Gameplay.Enemy
{
    public class EnemyPhysicalShooter : MonoBehaviour
    {
        [SerializeField] private Transform _firePoint;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _fireRate = 2f;
        
        [Header("Recoil Animation")]
        [SerializeField] private Transform _enemyModelTransform;
        [SerializeField] private float _recoilDistance = 0.2f;
        [SerializeField] private float _recoilDuration = 0.15f;

        private float _fireTimer;
        private void Start()
        {
            // O DOPunchPosition cuida da posição original automaticamente
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
            if (_projectilePrefab != null && _firePoint != null)
            {
                Instantiate(_projectilePrefab, _firePoint.position, _firePoint.rotation);
                ApplyRecoil();
            }
        }

        private void ApplyRecoil()
        {
            if (_enemyModelTransform == null) return;

            _enemyModelTransform.DOKill(true); // Complete any previous recoil before applying a new one
            
            // DOPunchPosition aplica o tranco na direção oposta ao tiro de forma relativa e aditiva,
            // então não interfere com os pulos no eixo Y e sempre volta pro lugar certo!
            Vector3 recoilPunch = -_firePoint.right * _recoilDistance;
            _enemyModelTransform.DOPunchPosition(recoilPunch, _recoilDuration, vibrato: 1, elasticity: 0.5f);
        }
    }
}

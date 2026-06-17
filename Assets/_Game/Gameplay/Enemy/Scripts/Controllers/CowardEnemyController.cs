using UnityEngine;
using Seventh.Gameplay.Player;
using Seventh.Core.Services;

namespace Seventh.Gameplay.Enemy
{
    [RequireComponent(typeof(EnemyHopMovement))]
    public class CowardEnemyController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _safeDistance = 7f; 
        
        [Header("Shooting")]
        [SerializeField] private Transform _weaponPivot;
        [SerializeField] private float _shootCooldown = 2f;
        [Tooltip("Delay in seconds when it stops to shoot before fleeing again")]
        [SerializeField] private float _shootStateDuration = 0.5f;

        private EnemyStateMachine _stateMachine;
        private Transform _playerTransform;
        private EnemyHopMovement _hopMovement;

        public float MoveSpeed => _moveSpeed;
        public float SafeDistance => _safeDistance;
        public float ShootCooldown => _shootCooldown;
        public float ShootStateDuration => _shootStateDuration;
        public Transform WeaponPivot => _weaponPivot;
        public Transform PlayerTransform => _playerTransform;
        public EnemyHopMovement HopMovement => _hopMovement;

        private void Awake()
        {
            _hopMovement = GetComponent<EnemyHopMovement>();
            _stateMachine = new EnemyStateMachine();
        }

        private void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }

            _stateMachine.ChangeState(new CowardFleeState(this, _stateMachine));
        }

        private void Update()
        {
            _stateMachine.Update();
        }

        public void TriggerShoot()
        {
            SendMessage("Shoot", SendMessageOptions.DontRequireReceiver);
        }
    }
}

using UnityEngine;
using Seventh.Gameplay.Player;
using Seventh.Core.Services;
using Seventh.Gameplay.Environment;
using Seventh.Gameplay.Enemies;

namespace Seventh.Gameplay.Enemy
{
    public enum ShootAimMode
    {
        TrackPlayer,
        FixedUp,
        FixedDown,
        FixedLeft,
        FixedRight
    }

    [RequireComponent(typeof(EnemyHopMovement))]
    [RequireComponent(typeof(Seventh.Gameplay.Enemies.Enemy))]
    public class CowardEnemyController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _safeDistance = 7f; 
        
        [Header("Shooting")]
        [SerializeField] private bool _canShoot = true;
        [SerializeField] private ShootAimMode _aimMode = ShootAimMode.TrackPlayer;
        [SerializeField] private Transform _weaponPivot;
        [SerializeField] private float _shootCooldown = 2f;
        [Tooltip("Delay in seconds when it stops to shoot before fleeing again")]
        [SerializeField] private float _shootStateDuration = 0.5f;

        private EnemyStateMachine _stateMachine;
        private Transform _playerTransform;
        private EnemyHopMovement _hopMovement;
        private RoomTrigger _myRoom;
        private Enemies.Enemy _enemy;

        private Vector3 _debugTargetPosition;
        private System.Collections.Generic.List<Vector3> _debugPath;

        public bool CanShoot => _canShoot;
        public ShootAimMode AimMode => _aimMode;
        public float MoveSpeed => _moveSpeed;
        public float SafeDistance => _safeDistance;
        public float ShootCooldown => _shootCooldown;
        public float ShootStateDuration => _shootStateDuration;
        public Transform WeaponPivot => _weaponPivot;
        public Transform PlayerTransform => _playerTransform;
        public EnemyHopMovement HopMovement => _hopMovement;
        public RoomTrigger MyRoom => _myRoom;

        private Vector3 _originalWeaponPivotLocalPos;

        private void Awake()
        {
            _hopMovement = GetComponent<EnemyHopMovement>();
            _enemy = GetComponent<Enemies.Enemy>();
            _stateMachine = new EnemyStateMachine();
        }

        public void ResetControllerState()
        {
            _stateMachine = new EnemyStateMachine();
            _stateMachine.ChangeState(new CowardFleeState(this, _stateMachine));
            if (_hopMovement != null)
            {
                _hopMovement.Stop();
            }
        }

        public void SetAimDirection(Vector2 aimDirection)
        {
            if (_enemy != null)
            {
                _enemy.UpdateAnimatorParameters(aimDirection);
            }

            if (_weaponPivot != null)
            {
                _weaponPivot.right = aimDirection;

                Vector3 localPos = _originalWeaponPivotLocalPos;
                if (aimDirection.x < -0.01f)
                {
                    localPos.x = -Mathf.Abs(_originalWeaponPivotLocalPos.x);
                }
                else if (aimDirection.x > 0.01f)
                {
                    localPos.x = Mathf.Abs(_originalWeaponPivotLocalPos.x);
                }
                _weaponPivot.localPosition = localPos;
            }
        }

        public void AimWeapon(Vector2 toPlayer)
        {
            Vector2 aimDirection;

            switch (_aimMode)
            {
                case ShootAimMode.FixedUp:
                    aimDirection = Vector2.up;
                    break;
                case ShootAimMode.FixedDown:
                    aimDirection = Vector2.down;
                    break;
                case ShootAimMode.FixedLeft:
                    aimDirection = Vector2.left;
                    break;
                case ShootAimMode.FixedRight:
                    aimDirection = Vector2.right;
                    break;
                case ShootAimMode.TrackPlayer:
                default:
                    if (Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y))
                    {
                        aimDirection = toPlayer.x > 0 ? Vector2.right : Vector2.left;
                    }
                    else
                    {
                        aimDirection = toPlayer.y > 0 ? Vector2.up : Vector2.down;
                    }
                    break;
            }

            SetAimDirection(aimDirection);
        }

        private void Start()
        {
            if (_weaponPivot != null)
            {
                _originalWeaponPivotLocalPos = _weaponPivot.localPosition;
            }

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }

            // Detect which room this enemy spawned in
            foreach (var room in FindObjectsByType<RoomTrigger>(FindObjectsInactive.Exclude))
            {
                if (room.RoomCollider != null && room.RoomCollider.OverlapPoint(transform.position))
                {
                    _myRoom = room;
                    break;
                }
            }

            _stateMachine.ChangeState(new CowardFleeState(this, _stateMachine));
        }

        private void Update()
        {
            _stateMachine.Update();
        }

        public void TriggerShoot()
        {
            if (_enemy != null && _enemy.Animator != null)
            {
                _enemy.Animator.SetTrigger("Attack");
            }
            SendMessage("Shoot", SendMessageOptions.DontRequireReceiver);
        }

        public void SetDebugPath(System.Collections.Generic.List<Vector3> path, Vector3 targetPosition)
        {
            _debugPath = path;
            _debugTargetPosition = targetPosition;
        }

        private void OnDrawGizmos()
        {
            // Draw Target Position in Red
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_debugTargetPosition, 0.3f);

            // Draw Path in Yellow
            if (_debugPath != null && _debugPath.Count > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < _debugPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(_debugPath[i], _debugPath[i + 1]);
                }
                foreach (var node in _debugPath)
                {
                    Gizmos.DrawWireCube(node, Vector3.one * 0.2f);
                }
            }
        }
    }
}

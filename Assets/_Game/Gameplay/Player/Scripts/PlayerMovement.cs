using DG.Tweening;
using Seventh.Core.Services;
using UnityEngine;
using UnityEngine.VFX;

namespace Seventh.Gameplay.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        private IInputService _inputService;
        private IAudioService _audioService;

        private PlayerAnimator _animator;

        [Header("Movement Settings")]
        [SerializeField] private AudioClip[] _walkSFX;
        [SerializeField] private float _moveSpeed = 5f;
        private Vector2 _movementInput;

        public Vector2 FacingDirection { get; private set; } = Vector2.right;

        [Header("VFX Settings")]
        [SerializeField] private VisualEffect _moveDustVFX;
        [SerializeField] private float _dustSpawnRate = 15f;

        private void Awake()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            _audioService = ServiceLocator.Get<IAudioService>();
            _animator = GetComponent<PlayerAnimator>();
        }

        private void Update()
        {
            Move();
        }

        private void Move()
        {
            _movementInput = _inputService.GetMovementInput();

            if (_movementInput.sqrMagnitude > 0.01f)
            {
                FacingDirection = _movementInput.normalized;
            }

            Vector3 movement = new Vector3(_movementInput.x, _movementInput.y) * _moveSpeed * Time.deltaTime;
            transform.position += movement;

            _animator.UpdateMovementAnimation(_movementInput);
            UpdateMoveDustVFX();
        }

        private void UpdateMoveDustVFX()
        {
            if (!_moveDustVFX) return;

            if (_movementInput.magnitude > 0.1f)
            {
                float currentRate = _movementInput.magnitude * _dustSpawnRate;
                _moveDustVFX.SetFloat("SpawnRate", currentRate);
            }
            else
            {
                _moveDustVFX.SetFloat("SpawnRate", 0f);
            }
        }

        public void PlayMoveSFX()
        {
            if (_walkSFX.Length == 0) return;

            int index = Random.Range(0, _walkSFX.Length);
            _audioService.PlaySFX(_walkSFX[index]);
        }
    }
}

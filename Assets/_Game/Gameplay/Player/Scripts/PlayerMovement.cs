using Seventh.Core.Services;
using Seventh.Gameplay.Player.Effects;
using AudioSettings = Seventh.Core.Services.AudioSettings;
using UnityEngine;
using UnityEngine.VFX;

namespace Seventh.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        private IInputService _inputService;
        private IAudioService _audioService;

        private PlayerAnimator _animator;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        private Vector2 _movementInput;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip[] _walkSFX;
        [Range(0f, 1f)][SerializeField] private float _walkSFXVolume = 1f;

        public Vector2 FacingDirection { get; private set; } = Vector2.right;

        [Header("VFX Settings")]
        [SerializeField] private VisualEffect _moveDustVFX;
        [SerializeField] private float _dustSpawnRate = 15f;

        [Header("DOTween Walk Visuals")]
        [SerializeField] private Transform _visualModel;
        [SerializeField] private bool _useBobbing = true;
        [SerializeField] private float _bobHeight = 0.15f;
        [SerializeField] private float _bobDuration = 0.3f;
        [SerializeField] private bool _useSquashAndStretch = true;
        [SerializeField] private Vector2 _squashScale = new Vector2(1.1f, 0.85f);

        public Transform VisualModel => _visualModel;

        private IGameStateService _gameStateService;
        private PlayerDash _dash;
        private Rigidbody2D _rb;
        private PlayerWalkVisualEffect _walkVisualEffect;

        private void Awake()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            _audioService = ServiceLocator.Get<IAudioService>();
            _animator = GetComponent<PlayerAnimator>();
            _gameStateService = ServiceLocator.Get<IGameStateService>();
            _dash = GetComponent<PlayerDash>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            _walkVisualEffect = new PlayerWalkVisualEffect(
                _visualModel,
                _useBobbing,
                _bobHeight,
                _bobDuration,
                _useSquashAndStretch,
                _squashScale
            );
        }

        private void FixedUpdate()
        {
            _rb.linearVelocity = _movementInput * _moveSpeed;
        }

        private void Update()
        {
            bool canMove = (_gameStateService == null || _gameStateService.CurrentGameState == Seventh.Core.Constants.GameState.Playing)
                           && (_dash == null || !_dash.IsDashing);

            if (canMove)
            {
                Move();
            }
            else
            {
                _movementInput = Vector2.zero;
                _animator.UpdateMovementAnimation(_movementInput);
                UpdateMoveDustVFX();
            }

            UpdateWalkAnimationState();
        }

        private void OnDestroy()
        {
            _walkVisualEffect?.CleanUp();
        }

        private void Move()
        {
            _movementInput = _inputService.GetMovementInput();

            if (_movementInput.sqrMagnitude > 0.01f)
            {
                FacingDirection = _movementInput.normalized;
            }

            _animator.UpdateMovementAnimation(_movementInput);
            UpdateMoveDustVFX();
        }

        private void UpdateWalkAnimationState()
        {
            if (_walkVisualEffect == null) return;

            bool isCurrentlyMoving = _movementInput.sqrMagnitude > 0.01f;

            if (isCurrentlyMoving)
            {
                _walkVisualEffect.Play();
            }
            else
            {
                _walkVisualEffect.Stop();
            }
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
            if (_walkSFX.Length == 0 || _audioService == null) return;

            int index = Random.Range(0, _walkSFX.Length);
            _audioService.PlaySFX(_walkSFX[index], new AudioSettings(volumeOffset: _walkSFXVolume - 1f));
        }
    }
}

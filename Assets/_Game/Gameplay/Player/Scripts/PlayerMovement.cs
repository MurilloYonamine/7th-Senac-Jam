using DG.Tweening;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;
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
        private Vector3 _initialLocalPosition;
        private Vector3 _initialLocalScale;
        private Sequence _walkSequence;
        private bool _animatingWalk;

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
            if (_visualModel != null)
            {
                _initialLocalPosition = _visualModel.localPosition;
                _initialLocalScale = _visualModel.localScale;
            }
        }

        private void FixedUpdate()
        {
            if (_rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = Vector2.zero;
#else
                _rb.velocity = Vector2.zero;
#endif
            }
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
            _walkSequence?.Kill();
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

        private void UpdateWalkAnimationState()
        {
            if (_visualModel == null) return;

            bool isCurrentlyMoving = _movementInput.sqrMagnitude > 0.01f;

            if (isCurrentlyMoving && !_animatingWalk)
            {
                StartWalkAnimation();
            }
            else if (!isCurrentlyMoving && _animatingWalk)
            {
                StopWalkAnimation();
            }
        }

        private void StartWalkAnimation()
        {
            _animatingWalk = true;
            PlayWalkStep();
        }

        private void PlayWalkStep()
        {
            if (!_animatingWalk || _visualModel == null) return;

            _walkSequence = DOTween.Sequence();

            if (_useBobbing)
            {
                _walkSequence.Append(_visualModel.DOLocalMoveY(_initialLocalPosition.y + _bobHeight, _bobDuration / 2f).SetEase(Ease.OutQuad));
                _walkSequence.Append(_visualModel.DOLocalMoveY(_initialLocalPosition.y, _bobDuration / 2f).SetEase(Ease.InQuad));
            }

            if (_useSquashAndStretch)
            {
                Sequence scaleSeq = DOTween.Sequence();
                scaleSeq.Append(_visualModel.DOScale(new Vector3(_initialLocalScale.x * _squashScale.x, _initialLocalScale.y * _squashScale.y, _initialLocalScale.z), _bobDuration * 0.3f).SetEase(Ease.OutQuad));
                scaleSeq.Append(_visualModel.DOScale(_initialLocalScale, _bobDuration * 0.7f).SetEase(Ease.InOutQuad));

                if (_useBobbing)
                {
                    _walkSequence.Join(scaleSeq);
                }
                else
                {
                    _walkSequence.Append(scaleSeq);
                }
            }

            // Fallback se nenhum estiver ativado
            if (!_useBobbing && !_useSquashAndStretch)
            {
                _walkSequence.AppendInterval(_bobDuration);
            }

            _walkSequence.OnComplete(() =>
            {
                PlayWalkStep();
            });
        }

        private void StopWalkAnimation()
        {
            _animatingWalk = false;
            _walkSequence?.Kill();

            if (_visualModel != null)
            {
                _visualModel.DOKill();
                _visualModel.localPosition = _initialLocalPosition;
                _visualModel.localScale = _initialLocalScale;
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

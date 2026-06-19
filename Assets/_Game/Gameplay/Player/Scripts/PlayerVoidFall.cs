using System.Collections.Generic;
using UnityEngine;

using Seventh.Core.Services;
using Seventh.Gameplay.Player.Effects;
using Seventh.Core.Constants;
using Seventh.Gameplay.Health;
using Seventh.Gameplay.Environment;

namespace Seventh.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerVoidFall : MonoBehaviour
    {
        [Header("Void Fall Settings")]
        [SerializeField] private float _requiredHoldTime = 0.4f;
        [SerializeField] private float _fallDuration = 0.8f;
        [SerializeField] private int _damageOnFall = 20;
        [SerializeField] private float _safePositionInterval = 0.3f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _voidFallSFX;
        [Range(0f, 1f)][SerializeField] private float _voidFallVolume = 1f;

        private PlayerMovement _movement;
        private PlayerHealth _health;
        private PlayerDash _dash;
        private IInputService _inputService;
        private IGameStateService _gameStateService;

        private bool _isInsideVoid;
        private float _holdTimer;
        private Vector3 _lastSafePosition;
        private float _safePositionTimer;

        private Vector3 _originalVisualScale;
        private Quaternion _originalVisualRotation;
        private bool _isFalling;
        private Collider2D _activeVoidCollider;
        private float _graceTimer;

        private List<Collider2D> _disabledColliders = new List<Collider2D>();
        private PlayerVoidFallVisualEffect _voidFallVisualEffect;

        private bool _isInitialized;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _health = GetComponent<PlayerHealth>();
            _dash = GetComponent<PlayerDash>();
            EnsureInitialized();
        }

        private void Start()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            _gameStateService = ServiceLocator.Get<IGameStateService>();
            _lastSafePosition = transform.position;
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            ResetVoidFallState();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;
            if (_movement == null) _movement = GetComponent<PlayerMovement>();

            if (_movement != null && _movement.VisualModel != null)
            {
                _originalVisualScale = _movement.VisualModel.localScale;
                _originalVisualRotation = _movement.VisualModel.localRotation;
            }
            else
            {
                _originalVisualScale = Vector3.one;
                _originalVisualRotation = Quaternion.identity;
            }

            Transform visual = _movement != null && _movement.VisualModel != null ? _movement.VisualModel : transform;
            _voidFallVisualEffect = new PlayerVoidFallVisualEffect(
                transform,
                visual,
                _originalVisualScale,
                _originalVisualRotation,
                _fallDuration
            );

            _isInitialized = true;
        }

        public void ResetVoidFallState()
        {
            _isFalling = false;
            _isInsideVoid = false;
            _holdTimer = 0f;
            _graceTimer = 0f;
            _activeVoidCollider = null;

            // Restore colliders if reset occurs mid-fall
            foreach (var col in _disabledColliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
            _disabledColliders.Clear();

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = Vector2.zero;
            }

            if (_movement != null)
            {
                _gameStateService?.ChangeGameState(GameState.Playing);
                _voidFallVisualEffect?.ResetVisuals();
            }
        }

        private void Update()
        {
            if (_graceTimer > 0f)
            {
                _graceTimer -= Time.deltaTime;
            }

            if (_isFalling) return;

            if (_dash != null && _dash.IsDashing)
            {
                _holdTimer = 0f;
                return;
            }

            bool isPlaying = _gameStateService == null || _gameStateService.CurrentGameState == GameState.Playing;
            if (!_isInsideVoid && isPlaying && _graceTimer <= 0f)
            {
                _safePositionTimer += Time.deltaTime;
                if (_safePositionTimer >= _safePositionInterval)
                {
                    _safePositionTimer = 0f;
                    _lastSafePosition = transform.position;
                }
            }

            if (_isInsideVoid && _graceTimer <= 0f)
            {
                Vector2 input = _inputService != null ? _inputService.GetMovementInput() : Vector2.zero;
                bool isMoving = input.sqrMagnitude > 0.01f;

                if (isMoving)
                {
                    _holdTimer += Time.deltaTime;
                    if (_holdTimer >= _requiredHoldTime)
                    {
                        StartFallSequence();
                    }
                }
                else
                {
                    _holdTimer = Mathf.Max(0f, _holdTimer - Time.deltaTime * 2f);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<VoidArea>() != null || other.CompareTag("Void"))
            {
                _isInsideVoid = true;
                _activeVoidCollider = other;
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.GetComponent<VoidArea>() != null || other.CompareTag("Void"))
            {
                _isInsideVoid = true;
                _activeVoidCollider = other;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<VoidArea>() != null || other.CompareTag("Void"))
            {
                _isInsideVoid = false;
                _holdTimer = 0f;
                _activeVoidCollider = null;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<VoidArea>() != null || collision.gameObject.CompareTag("Void"))
            {
                _isInsideVoid = true;
                _activeVoidCollider = collision.collider;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<VoidArea>() != null || collision.gameObject.CompareTag("Void"))
            {
                _isInsideVoid = true;
                _activeVoidCollider = collision.collider;
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<VoidArea>() != null || collision.gameObject.CompareTag("Void"))
            {
                _isInsideVoid = false;
                _holdTimer = 0f;
                _activeVoidCollider = null;
            }
        }

        private void StartFallSequence()
        {
            _isFalling = true;
            if (_gameStateService != null)
            {
                _gameStateService.ChangeGameState(GameState.Cutscene);
            }

            if (_voidFallSFX != null)
            {
                var audioService = ServiceLocator.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.PlaySFX(_voidFallSFX, new Core.Services.AudioSettings(volumeOffset: _voidFallVolume - 1f));
                }
            }

            if (_dash != null && _dash.IsDashing)
            {
                _dash.CancelDash();
            }

            // Disable player colliders during the fall sequence
            _disabledColliders.Clear();
            var colliders = GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                if (col.enabled)
                {
                    col.enabled = false;
                    _disabledColliders.Add(col);
                }
            }



            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }

            Transform visual = _movement != null && _movement.VisualModel != null ? _movement.VisualModel : transform;
            _voidFallVisualEffect?.PlayFall(_activeVoidCollider, () =>
            {
                HandleFallComplete(visual);
            });
        }

        private void HandleFallComplete(Transform visual)
        {
            // Re-enable colliders
            foreach (var col in _disabledColliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
            _disabledColliders.Clear();

            // Apply damage
            _health.TakeDamage(new DamageInfo(_damageOnFall, 0f, Vector2.zero, null, HitIntensity.Light, isSilent: false));

            if (_health.CurrentHealth > 0)
            {
                // Teleport to safety
                transform.position = _lastSafePosition;

                var rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.linearVelocity = Vector2.zero;
                }

                // Reset states immediately to prevent physics/timer lock
                _isInsideVoid = false;
                _activeVoidCollider = null;
                _holdTimer = 0f;
                _graceTimer = 0.5f;

                _voidFallVisualEffect?.PlayRespawnFadeIn(() =>
                {
                    if (_gameStateService != null)
                    {
                        _gameStateService.ChangeGameState(GameState.Playing);
                    }
                    _isFalling = false;
                });
            }
            else
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.linearVelocity = Vector2.zero;
                }

                // Player died, keep disabled and let death handles run
                _isFalling = false;
            }
        }

        private void OnDestroy()
        {
            _voidFallVisualEffect?.CleanUp();
        }
    }
}

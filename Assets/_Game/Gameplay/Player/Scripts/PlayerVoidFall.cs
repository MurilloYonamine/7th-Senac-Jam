using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Seventh.Core.Services;
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

        private struct SpriteColorInfo
        {
            public SpriteRenderer Renderer;
            public Color OriginalColor;
        }
        private List<SpriteColorInfo> _spriteColors = new List<SpriteColorInfo>();

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

            CacheSpriteRenderers();
            _isInitialized = true;
        }

        private void CacheSpriteRenderers()
        {
            _spriteColors.Clear();
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
            {
                _spriteColors.Add(new SpriteColorInfo { Renderer = r, OriginalColor = r.color });
            }
        }

        public void ResetVoidFallState()
        {
            _isFalling = false;
            _isInsideVoid = false;
            _holdTimer = 0f;
            _graceTimer = 0f;
            _activeVoidCollider = null;

            if (_movement != null)
            {
                _gameStateService?.ChangeGameState(GameState.Playing);
                if (_movement.VisualModel != null)
                {
                    _movement.VisualModel.DOKill();
                    _movement.VisualModel.localScale = _originalVisualScale;
                    _movement.VisualModel.localRotation = _originalVisualRotation;
                }
            }

            // Reset sprite colors/alphas
            foreach (var info in _spriteColors)
            {
                if (info.Renderer != null)
                {
                    info.Renderer.DOKill();
                    info.Renderer.color = info.OriginalColor;
                }
            }
        }

        private void Update()
        {
            if (_graceTimer > 0f)
            {
                _graceTimer -= Time.deltaTime;
            }

            if (_isFalling) return;

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

                if (_dash != null && _dash.IsDashing && _graceTimer <= 0f)
                {
                    StartFallSequence();
                }
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

                if (_dash != null && _dash.IsDashing && _graceTimer <= 0f)
                {
                    StartFallSequence();
                }
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

            if (_dash != null && _dash.IsDashing)
            {
                _dash.CancelDash();
            }

            CacheSpriteRenderers();

            Transform visual = _movement.VisualModel != null ? _movement.VisualModel : transform;

            visual.DOKill();
            transform.DOKill();

            Sequence fallSeq = DOTween.Sequence();

            if (_activeVoidCollider != null)
            {
                Vector3 targetCenter = _activeVoidCollider.bounds.center;
                // Maintain current Z coordinate
                targetCenter.z = transform.position.z;
                fallSeq.Join(transform.DOMove(targetCenter, _fallDuration).SetEase(Ease.OutQuad));
            }

            fallSeq.Join(visual.DORotate(new Vector3(0, 0, 720f), _fallDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));

            fallSeq.Join(visual.DOScale(Vector3.zero, _fallDuration).SetEase(Ease.InQuad));

            foreach (var info in _spriteColors)
            {
                if (info.Renderer == null) continue;
                fallSeq.Join(info.Renderer.DOColor(Color.black, _fallDuration * 0.5f).SetEase(Ease.InQuad));
                fallSeq.Join(info.Renderer.DOFade(0f, _fallDuration).SetEase(Ease.InQuad));
            }

            fallSeq.OnComplete(() =>
            {
                HandleFallComplete(visual);
            });
        }

        private void HandleFallComplete(Transform visual)
        {
            // Apply damage
            _health.TakeDamage(new DamageInfo(_damageOnFall, 0f, Vector2.zero, null, HitIntensity.Light, isSilent: false));

            if (_health.CurrentHealth > 0)
            {
                // Teleport to safety
                transform.position = _lastSafePosition;

                var rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector2.zero;
#else
                    rb.velocity = Vector2.zero;
#endif
                }

                // Reset visual properties
                visual.localScale = _originalVisualScale;
                visual.localRotation = _originalVisualRotation;

                foreach (var info in _spriteColors)
                {
                    if (info.Renderer == null) continue;
                    info.Renderer.color = info.OriginalColor;
                }

                // Reset states immediately to prevent physics/timer lock
                _isInsideVoid = false;
                _activeVoidCollider = null;
                _holdTimer = 0f;
                _graceTimer = 0.5f;

                // Brief fade-in effect to feel premium
                visual.localScale = Vector3.zero;
                visual.DOScale(_originalVisualScale, 0.25f).SetEase(Ease.OutBack).OnComplete(() =>
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
                // Player died, keep disabled and let death handles run
                _isFalling = false;
            }
        }

        private void OnDestroy()
        {
            if (_movement != null && _movement.VisualModel != null)
            {
                _movement.VisualModel.DOKill();
            }
        }
    }
}

using System.Collections;
using DG.Tweening;
using Seventh.Core.Events;
using Seventh.Core.Services;
using Seventh.Gameplay.Player.Effects;
using AudioSettings = Seventh.Core.Services.AudioSettings;
using UnityEngine;
using Unity.Cinemachine;

namespace Seventh.Gameplay.Player
{
    public class PlayerDash : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private IInputService _inputService;
        private IAudioService _audioService;
        private CinemachineImpulseSource _impulseSource;
        private Coroutine _flashCoroutine;
        private Material _originalMaterial;
        private Material _currentFlashMaterial;
        private PlayerDashVisualEffect _dashVisualEffect;

        [Header("Dash Settings")]
        [SerializeField] private float _dashDistance = 5;
        [SerializeField] private float _dashDuration = 0.2f;
        [SerializeField] private float _dashCooldown = 1f;
        [SerializeField] private LayerMask _dashObstacleLayers;
        [SerializeField] private float _dashSkinWidth = 0.2f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _dashSFX;
        [Range(0f, 1f)][SerializeField] private float _dashSFXVolume = 1f;
        private float _dashCooldownTimer = 0f;
        private Vector2 _movementInput;

        [Header("Afterimage Settings")]
        [SerializeField] private Color _ghostColor = new Color(0f, 0.7f, 1f, 0.5f); 
        [SerializeField] private float _ghostDelay = 0.04f;
        [SerializeField] private float _ghostFadeDuration = 0.3f; 

        public bool IsDashing { get; private set; } 

        private void Start()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            _audioService = ServiceLocator.Get<IAudioService>();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            
            _impulseSource = GetComponent<CinemachineImpulseSource>();
            if (_spriteRenderer != null)
            {
                _originalMaterial = _spriteRenderer.material;
            }

            _dashVisualEffect = new PlayerDashVisualEffect(_spriteRenderer);
        }

        private void Update()
        {
            if (IsOnCooldown())
            {
                UpdateCooldown();
                return;
            }

            _inputService.GetDashInput(out bool isDashing);
            if (isDashing)
            {
                StartDash();
            }
        }

        private bool IsOnCooldown()
        {
            return _dashCooldownTimer > 0f;
        }

        private void UpdateCooldown()
        {
            _dashCooldownTimer -= Time.deltaTime;
            float remainingTime = Mathf.Max(0f, _dashCooldownTimer);
            bool isOnCooldown = remainingTime > 0f;
            
            ServiceLocator.Get<IEventBus>().Publish(new PlayerDashCooldownEvent(isOnCooldown, remainingTime, _dashCooldown));
        }

        private void StartDash()
        {
            Vector3 direction = GetDashDirection();
            
            if (_audioService != null && _dashSFX != null)
            {
                _audioService.PlaySFX(_dashSFX, new AudioSettings(volumeOffset: _dashSFXVolume - 1f));
            }
            
            ExecuteDashMovement(direction);
            ApplyDashEffects(direction);
            StartCooldown();
        }

        private Vector3 GetDashDirection()
        {
            _movementInput = _inputService.GetMovementInput();
            Vector3 direction = new Vector3(_movementInput.x, _movementInput.y, 0f).normalized;
            return direction == Vector3.zero ? Vector3.right : direction;
        }

        private void ExecuteDashMovement(Vector3 direction)
        {
            float targetDistance = _dashDistance;

            if (TryGetComponent<Collider2D>(out var col))
            {
                Vector2 boxSize = col.bounds.size;
                RaycastHit2D hit = Physics2D.BoxCast(transform.position, boxSize, 0f, direction, _dashDistance, _dashObstacleLayers);
                if (hit.collider != null)
                {
                    targetDistance = Mathf.Max(0f, hit.distance - _dashSkinWidth);
                }
            }

            Vector3 targetPosition = transform.position + (direction * targetDistance);
            
            IsDashing = true;
            transform.DOMove(targetPosition, _dashDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => 
                {
                    IsDashing = false;
                    _dashVisualEffect?.ResetScale();
                });
        }

        public void CancelDash()
        {
            if (!IsDashing) return;
            IsDashing = false;
            transform.DOKill();
            _dashVisualEffect?.ResetScaleImmediately();
            if (_spriteRenderer != null)
            {
                if (_originalMaterial != null)
                {
                    _spriteRenderer.material = _originalMaterial;
                }
            }
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            if (_currentFlashMaterial != null)
            {
                Destroy(_currentFlashMaterial);
                _currentFlashMaterial = null;
            }
        }

        private void ApplyDashEffects(Vector3 direction)
        {
            _dashVisualEffect?.ApplySquashAndStretch(direction);
            
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                if (_spriteRenderer != null && _originalMaterial != null)
                {
                    _spriteRenderer.material = _originalMaterial;
                }
                if (_currentFlashMaterial != null)
                {
                    Destroy(_currentFlashMaterial);
                    _currentFlashMaterial = null;
                }
            }
            _flashCoroutine = StartCoroutine(FlashRoutine(0.06f));
            
            StartCoroutine(SpawnAfterimageRoutine());
            _impulseSource?.GenerateImpulse();
        }



        private void StartCooldown()
        {
            _dashCooldownTimer = _dashCooldown;
            ServiceLocator.Get<IEventBus>().Publish(new PlayerDashCooldownEvent(true, _dashCooldownTimer, _dashCooldown));
        }

        private IEnumerator FlashRoutine(float duration)
        {
            if (_spriteRenderer == null) yield break;

            _currentFlashMaterial = new Material(Shader.Find("GUI/Text Shader"));
            _currentFlashMaterial.color = Color.white;

            _spriteRenderer.material = _currentFlashMaterial;
            yield return new WaitForSeconds(duration);

            if (_spriteRenderer != null && _originalMaterial != null)
            {
                _spriteRenderer.material = _originalMaterial;
            }

            if (_currentFlashMaterial != null)
            {
                Destroy(_currentFlashMaterial);
                _currentFlashMaterial = null;
            }
            _flashCoroutine = null;
        }

        private void OnDestroy()
        {
            _dashVisualEffect?.CleanUp();
            if (_currentFlashMaterial != null)
            {
                Destroy(_currentFlashMaterial);
            }
        }

        private IEnumerator SpawnAfterimageRoutine()
        {
            float elapsed = 0f;

            while (elapsed < _dashDuration)
            {
                GameObject ghostObj = new GameObject("DashGhost");
                ghostObj.transform.SetParent(transform.parent);
                
                AfterimageGhost ghost = ghostObj.AddComponent<AfterimageGhost>();
                
                ghost.Init(
                    _spriteRenderer.sprite,
                    transform.position,
                    transform.rotation,
                    _spriteRenderer.transform.localScale,
                    _ghostColor,
                    _ghostFadeDuration,
                    _spriteRenderer.sortingOrder - 1,
                    _spriteRenderer.sortingLayerName
                );

                yield return new WaitForSeconds(_ghostDelay);
                elapsed += _ghostDelay;
            }
        }
    }
}
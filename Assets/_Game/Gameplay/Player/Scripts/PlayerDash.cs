using System.Collections;
using DG.Tweening;
using Seventh.Core.Events;
using Seventh.Core.Services;
using UnityEngine;
using Unity.Cinemachine;

namespace Seventh.Gameplay.Player
{
    public class PlayerDash : MonoBehaviour
    {
        private IInputService _inputService;
        private IAudioService _audioService;
        private SpriteRenderer _spriteRenderer;
        private CinemachineImpulseSource _impulseSource;

        [Header("Dash Settings")]
        [SerializeField] private AudioClip _dashSFX;
        [SerializeField] private float _dashDistance = 5;
        [SerializeField] private float _dashDuration = 0.2f;
        [SerializeField] private float _dashCooldown = 1f;
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
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _impulseSource = GetComponent<CinemachineImpulseSource>();
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
            Vector3 targetPosition = transform.position + (direction * _dashDistance);
            
            IsDashing = true;
            transform.DOMove(targetPosition, _dashDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => 
                {
                    IsDashing = false;
                    _spriteRenderer.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutElastic);
                });
        }

        private void ApplyDashEffects(Vector3 direction)
        {
            ApplySquashAndStretch(direction);
            StartCoroutine(FlashRoutine(0.06f));
            StartCoroutine(SpawnAfterimageRoutine());

            _impulseSource?.GenerateImpulse();
        }

        private void ApplySquashAndStretch(Vector3 direction)
        {
            Vector3 dashScale = Vector3.one;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                dashScale = new Vector3(1.3f, 0.7f, 1f);
            }
            else
            {
                dashScale = new Vector3(0.7f, 1.3f, 1f);
            }

            _spriteRenderer.transform.DOScale(dashScale, 0.05f);
        }

        private void StartCooldown()
        {
            _dashCooldownTimer = _dashCooldown;
            ServiceLocator.Get<IEventBus>().Publish(new PlayerDashCooldownEvent(true, _dashCooldownTimer, _dashCooldown));
        }

        private IEnumerator FlashRoutine(float duration)
        {
            Material originalMaterial = _spriteRenderer.material;
            Material flashMaterial = new Material(Shader.Find("GUI/Text Shader"));
            flashMaterial.color = Color.white;

            _spriteRenderer.material = flashMaterial;
            yield return new WaitForSeconds(duration);
            _spriteRenderer.material = originalMaterial;

            Destroy(flashMaterial);
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
                    _ghostFadeDuration
                );

                yield return new WaitForSeconds(_ghostDelay);
                elapsed += _ghostDelay;
            }
        }
    }
}
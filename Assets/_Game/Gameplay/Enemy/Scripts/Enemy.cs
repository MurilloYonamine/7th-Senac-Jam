using System.Collections;
using UnityEngine;
using DG.Tweening;
using Seventh.Gameplay.Health;
using Seventh.Core.Events;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;

namespace Seventh.Gameplay.Enemies
{
    [RequireComponent(typeof(HealthBase))]
    public class Enemy : MonoBehaviour
    {
        protected HealthBase HealthComponent { get; private set; }

        [Header("Hit Feedback Settings")]
        [SerializeField] protected float _knockbackDuration = 0.12f;
        [SerializeField] protected float _flashDuration = 0.08f;
        [SerializeField] protected float _hitstopDuration = 0.07f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _deathSFX;
        [Range(0f, 1f)][SerializeField] private float _deathSFXVolume = 1f;

        [Header("VFX Settings")]
        [SerializeField] protected GameObject _deathVFXPrefab;

        protected Animator AnimatorComponent;
        protected SpriteRenderer SpriteRendererComponent;

        public Animator Animator => AnimatorComponent;
        public SpriteRenderer SpriteRenderer => SpriteRendererComponent;

        private Coroutine _flashCoroutine;
        private Coroutine _hitstopCoroutine;
        private Material _originalMaterial;
        private Material _currentFlashMaterial;

        private float _stunTimer;
        public bool IsStunned => _stunTimer > 0f;

        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private static readonly System.Collections.Generic.List<Enemy> AllEnemies = new System.Collections.Generic.List<Enemy>();

        [Header("Orientation Settings")]
        [SerializeField] private bool _flipOnRightAim = true;

        public void UpdateAnimatorParameters(Vector2 direction)
        {
            if (AnimatorComponent == null) return;

            // Update Blend Tree parameters
            AnimatorComponent.SetFloat("MoveX", direction.x);
            AnimatorComponent.SetFloat("MoveY", direction.y);

            // Flip the sprite depending on aiming direction and configuration
            if (SpriteRendererComponent != null)
            {
                if (direction.x > 0.01f)
                {
                    SpriteRendererComponent.flipX = _flipOnRightAim;
                }
                else if (direction.x < -0.01f)
                {
                    SpriteRendererComponent.flipX = !_flipOnRightAim;
                }
            }
        }

        protected virtual void Update()
        {
            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.deltaTime;
            }
        }

        protected virtual void Awake()
        {
            HealthComponent = GetComponent<HealthBase>();
            AnimatorComponent = GetComponentInChildren<Animator>();
            SpriteRendererComponent = GetComponentInChildren<SpriteRenderer>();

            if (SpriteRendererComponent != null)
            {
                _originalMaterial = SpriteRendererComponent.material;
            }

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            AllEnemies.Add(this);
        }

        protected virtual void OnEnable()
        {
            if (HealthComponent != null)
            {
                HealthComponent.OnDamageTaken += HandleDamageTaken;
                HealthComponent.OnDeath += HandleDeath;
            }
        }

        protected virtual void OnDisable()
        {
            if (HealthComponent != null)
            {
                HealthComponent.OnDamageTaken -= HandleDamageTaken;
                HealthComponent.OnDeath -= HandleDeath;
            }
        }

        protected virtual void OnDestroy()
        {
            AllEnemies.Remove(this);

            if (_currentFlashMaterial != null)
            {
                Destroy(_currentFlashMaterial);
            }

            // Kill all active DOTween tweens running on this object or its children
            transform.DOKill();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                t.DOKill();
            }
            foreach (var cg in GetComponentsInChildren<CanvasGroup>(true))
            {
                cg.DOKill();
            }
        }

        protected virtual void HandleDamageTaken(DamageInfo damageInfo)
        {
            _stunTimer = Mathf.Max(_knockbackDuration, _hitstopDuration);

            // Interrupt any active jumping/hopping movement
            var hopMovement = GetComponent<Seventh.Gameplay.Enemy.EnemyHopMovement>();
            if (hopMovement != null)
            {
                hopMovement.Interrupt();
            }

            // Apply Knockback
            if (damageInfo.KnockbackForce > 0f)
            {
                transform.DOKill(); // Stop active movement tweens
                transform.DOMove(transform.position + new Vector3(damageInfo.HitDirection.x, damageInfo.HitDirection.y, 0f) * damageInfo.KnockbackForce, _knockbackDuration)
                    .SetEase(Ease.OutQuad);
            }

            // Apply Flash Effect
            if (SpriteRendererComponent != null)
            {
                if (_flashCoroutine != null)
                {
                    StopCoroutine(_flashCoroutine);
                    if (_originalMaterial != null)
                    {
                        SpriteRendererComponent.material = _originalMaterial;
                    }
                    if (_currentFlashMaterial != null)
                    {
                        Destroy(_currentFlashMaterial);
                        _currentFlashMaterial = null;
                    }
                }
                _flashCoroutine = StartCoroutine(FlashRoutine(_flashDuration));
            }

            // Apply Hitstop (VFX freeze)
            if (AnimatorComponent != null)
            {
                if (_hitstopCoroutine != null)
                {
                    StopCoroutine(_hitstopCoroutine);
                }
                _hitstopCoroutine = StartCoroutine(HitstopRoutine(_hitstopDuration));
            }
        }

        protected virtual void HandleDeath()
        {
            Debug.Log($"{gameObject.name} has died!");

            var audioService = ServiceLocator.Get<IAudioService>();
            if (audioService != null && _deathSFX != null)
            {
                audioService.PlaySFX(_deathSFX, new AudioSettings(volumeOffset: _deathSFXVolume - 1f, spatialPosition: transform.position));
            }

            if (_deathVFXPrefab != null)
            {
                var vfxInstance = Instantiate(_deathVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfxInstance, 1.5f); // Automatically clean up the VFX object after 3 seconds
            }

            ServiceLocator.Get<IEventBus>()?.Publish(new EnemyDefeatedEvent(gameObject));

            gameObject.SetActive(false);
        }

        public static void ReactivateAllEnemies()
        {
            var enemiesCopy = new System.Collections.Generic.List<Enemy>(AllEnemies);
            foreach (var enemy in enemiesCopy)
            {
                if (enemy == null) continue;

                enemy.gameObject.SetActive(true);
                enemy.transform.position = enemy._spawnPosition;
                enemy.transform.rotation = enemy._spawnRotation;

                if (enemy.HealthComponent != null)
                {
                    enemy.HealthComponent.ResetHealth();
                }

                var cowardController = enemy.GetComponent<Seventh.Gameplay.Enemy.CowardEnemyController>();
                if (cowardController != null)
                {
                    cowardController.ResetControllerState();
                }
            }
        }

        private IEnumerator FlashRoutine(float duration)
        {
            if (SpriteRendererComponent == null) yield break;

            _currentFlashMaterial = new Material(Shader.Find("GUI/Text Shader"));
            _currentFlashMaterial.color = Color.white;

            SpriteRendererComponent.material = _currentFlashMaterial;
            yield return new WaitForSeconds(duration);

            if (SpriteRendererComponent != null && _originalMaterial != null)
            {
                SpriteRendererComponent.material = _originalMaterial;
            }

            if (_currentFlashMaterial != null)
            {
                Destroy(_currentFlashMaterial);
                _currentFlashMaterial = null;
            }
            _flashCoroutine = null;
        }

        private IEnumerator HitstopRoutine(float duration)
        {
            if (AnimatorComponent == null) yield break;

            float originalSpeed = AnimatorComponent.speed;
            AnimatorComponent.speed = 0f;
            yield return new WaitForSeconds(duration);
            AnimatorComponent.speed = originalSpeed;
            _hitstopCoroutine = null;
        }
    }
}

using System.Collections;
using DG.Tweening;
using Seventh.Gameplay.Health;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;
using UnityEngine;

namespace Seventh.Gameplay.Enemies
{
    [RequireComponent(typeof(Animator))]
    public class Dummy : Enemy
    {
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        private readonly int _isLightHash = Animator.StringToHash("IsLight");
        private readonly int _isMediumHash = Animator.StringToHash("IsMedium");
        private readonly int _isHeavyHash = Animator.StringToHash("IsHeavy");

        private bool _hasLightParam;
        private bool _hasMediumParam;
        private bool _hasHeavyParam;

        private Coroutine _resetCoroutine;
        private Coroutine _flashCoroutine;
        private Coroutine _hitstopCoroutine;

        [Header("Knockback Settings")]
        [SerializeField] private float _knockbackDuration = 0.12f;

        [Header("Juice Settings")]
        [SerializeField] private float _flashDuration = 0.08f;
        [SerializeField] private float _hitstopDuration = 0.07f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _hitLightSFX;
        [Range(0f, 1f)][SerializeField] private float _hitLightSFXVolume = 1f;
        [SerializeField] private AudioClip _hitMediumSFX;
        [Range(0f, 1f)][SerializeField] private float _hitMediumSFXVolume = 1f;
        [SerializeField] private AudioClip _hitHeavySFX;
        [Range(0f, 1f)][SerializeField] private float _hitHeavySFXVolume = 1f;

        private IAudioService _audioService;
        private Material _originalMaterial;
        private Material _currentFlashMaterial;

        protected override void Awake()
        {
            base.Awake();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            _audioService = ServiceLocator.Get<IAudioService>();
            _hasLightParam = HasParameter("IsLight");
            _hasMediumParam = HasParameter("IsMedium");
            _hasHeavyParam = HasParameter("IsHeavy");
            if (_spriteRenderer != null)
            {
                _originalMaterial = _spriteRenderer.material;
            }
        }

        private bool HasParameter(string paramName)
        {
            if (_animator == null) return false;
            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
        }

        protected override void HandleDamageTaken(DamageInfo damageInfo)
        {
            base.HandleDamageTaken(damageInfo);

            if (_audioService != null)
            {
                AudioClip sfxToPlay = null;
                float volume = 1f;

                if (damageInfo.Intensity == HitIntensity.Heavy)
                {
                    sfxToPlay = _hitHeavySFX;
                    volume = _hitHeavySFXVolume;
                }
                else if (damageInfo.Intensity == HitIntensity.Medium)
                {
                    sfxToPlay = _hitMediumSFX;
                    volume = _hitMediumSFXVolume;
                }
                else
                {
                    sfxToPlay = _hitLightSFX;
                    volume = _hitLightSFXVolume;
                }

                if (sfxToPlay != null)
                {
                    _audioService.PlaySFX(sfxToPlay, new AudioSettings(volumeOffset: volume - 1f, spatialPosition: transform.position));
                }
            }

            if (_animator == null) return;

            if (_resetCoroutine != null)
            {
                StopCoroutine(_resetCoroutine);
            }
            ResetAllBools();

            // Apply Knockback
            if (damageInfo.KnockbackForce > 0f)
            {
                transform.DOKill(); // Stop any active movement tweens
                transform.DOMove(transform.position + new Vector3(damageInfo.HitDirection.x, damageInfo.HitDirection.y, 0f) * damageInfo.KnockbackForce, _knockbackDuration)
                    .SetEase(Ease.OutQuad);
            }

            // Apply Flash Effect
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
            _flashCoroutine = StartCoroutine(FlashRoutine(_flashDuration));

            // Apply Hitstop (VFX freeze)
            if (_hitstopCoroutine != null)
            {
                StopCoroutine(_hitstopCoroutine);
            }
            _hitstopCoroutine = StartCoroutine(HitstopRoutine(_hitstopDuration));

            int targetHash = -1;
            bool hasParam = false;

            if (damageInfo.Intensity == HitIntensity.Heavy)
            {
                targetHash = _isHeavyHash;
                hasParam = _hasHeavyParam;
            }
            else if (damageInfo.Intensity == HitIntensity.Medium)
            {
                if (_hasMediumParam)
                {
                    targetHash = _isMediumHash;
                    hasParam = _hasMediumParam;
                }
                else
                {
                    targetHash = _isLightHash;
                    hasParam = _hasLightParam;
                }
            }
            else // HitIntensity.Light
            {
                targetHash = _isLightHash;
                hasParam = _hasLightParam;
            }

            // Set parameter to true and schedule it to go false
            if (targetHash != -1 && hasParam)
            {
                _animator.SetBool(targetHash, true);
                _resetCoroutine = StartCoroutine(ResetBoolsAfterDelay(0.15f));
            }
        }



        private void ResetAllBools()
        {
            if (_hasLightParam) _animator.SetBool(_isLightHash, false);
            if (_hasMediumParam) _animator.SetBool(_isMediumHash, false);
            if (_hasHeavyParam) _animator.SetBool(_isHeavyHash, false);
        }

        private IEnumerator ResetBoolsAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetAllBools();
            _resetCoroutine = null;
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
            if (_currentFlashMaterial != null)
            {
                Destroy(_currentFlashMaterial);
            }
        }

        private IEnumerator HitstopRoutine(float duration)
        {
            if (_animator == null) yield break;

            float originalSpeed = _animator.speed;
            _animator.speed = 0f;
            yield return new WaitForSeconds(duration);
            _animator.speed = originalSpeed;
            _hitstopCoroutine = null;
        }
    }
}

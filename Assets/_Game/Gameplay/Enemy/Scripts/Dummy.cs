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

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _hitLightSFX;
        [Range(0f, 1f)][SerializeField] private float _hitLightSFXVolume = 1f;
        [SerializeField] private AudioClip _hitMediumSFX;
        [Range(0f, 1f)][SerializeField] private float _hitMediumSFXVolume = 1f;
        [SerializeField] private AudioClip _hitHeavySFX;
        [Range(0f, 1f)][SerializeField] private float _hitHeavySFXVolume = 1f;

        private IAudioService _audioService;

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
            // Call base class to handle common knockback, hit flash, and hitstop!
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
    }
}

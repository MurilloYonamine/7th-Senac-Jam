using System.Collections;
using DG.Tweening;
using Seventh.Gameplay.Health;
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
        [SerializeField] private float _heavyKnockback = 0.6f;
        [SerializeField] private float _knockbackDuration = 0.12f;

        [Header("Juice Settings")]
        [SerializeField] private float _flashDuration = 0.08f;
        [SerializeField] private float _hitstopDuration = 0.07f;

        protected override void Awake()
        {
            base.Awake();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
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
            base.HandleDamageTaken(damageInfo);

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

            Material originalMaterial = _spriteRenderer.material;
            Material flashMaterial = new Material(Shader.Find("GUI/Text Shader"));
            flashMaterial.color = Color.white;

            _spriteRenderer.material = flashMaterial;
            yield return new WaitForSeconds(duration);
            _spriteRenderer.material = originalMaterial;

            Destroy(flashMaterial);
            _flashCoroutine = null;
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

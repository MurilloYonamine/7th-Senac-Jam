using System.Collections;
using Seventh.Core.Services;
using UnityEngine;
using UnityEngine.VFX;
using Unity.Cinemachine;

namespace Seventh.Gameplay.Player
{
    public class PlayerAttack : MonoBehaviour
    {
        private IInputService _inputService;
        private IAudioService _audioService;

        private PlayerMovement _movement;
        private PlayerAnimator _animator;
        private PlayerDash _dash;

        [Header("Attack Settings")]
        [SerializeField] private GameObject _slashVFXPrefab;
        [SerializeField] private float _attackCooldown = 0.35f;
        [SerializeField] private float _vfxDuration = 0.5f;
        [SerializeField] private float _vfxSpawnOffset = 0.6f;
        [SerializeField] private bool _parentVFXToPlayer = true;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _attackSFXNormal;
        [SerializeField] private AudioClip _attackSFXHeavy;

        [Header("Combo Chain Settings")]
        [SerializeField] private float _comboResetTime = 1.0f;
        [SerializeField] private float _combo3ScaleMultiplier = 1.5f;
        [SerializeField] private float _combo3OffsetMultiplier = 1.2f;

        [Header("Damage Settings")]
        [SerializeField] private int _combo1Damage = 15;
        [SerializeField] private int _combo2Damage = 20;
        [SerializeField] private int _combo3Damage = 35;
        [SerializeField] private float _combo1Knockback = 0f;
        [SerializeField] private float _combo2Knockback = 0f;
        [SerializeField] private float _combo3Knockback = 0.6f;

        [Header("Combo 3 VFX Glow (HDR)")]
        [SerializeField] private string _vfxColorParameterName = "Color";
        [ColorUsage(true, true)][SerializeField] private Color _defaultVFXColor = Color.white;
        [ColorUsage(true, true)][SerializeField] private Color _combo3VFXColor = new Color(3.0f, 3.0f, 3.0f, 1.0f);

        [Header("Screen Shake (Cinemachine)")]
        [SerializeField] private float _normalShakeForce = 0.4f;
        [SerializeField] private float _combo3ShakeForce = 1.0f;

        [Header("Controller Rumble Settings")]
        [SerializeField] private float _normalRumbleIntensity = 0.5f;
        [SerializeField] private float _combo3RumbleIntensity = 0.75f;
        [SerializeField] private float _rumbleDuration = 0.15f;

        private float _cooldownTimer = 0f;
        private float _comboTimer = 0f;
        private int _comboStep = 0;

        private CinemachineImpulseSource _impulseSource;
        private Coroutine _rumbleCoroutine;

        private void Start()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            _audioService = ServiceLocator.Get<IAudioService>();

            _movement = GetComponent<PlayerMovement>();
            _animator = GetComponent<PlayerAnimator>();
            _dash = GetComponent<PlayerDash>();
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            if (_comboTimer > 0f)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0f)
                {
                    _comboStep = 0;
                }
            }

            _inputService.GetAttackInput(out bool isAttacking, out _);
            if (isAttacking && _cooldownTimer <= 0f && (_dash == null || !_dash.IsDashing))
            {
                Attack();
            }
        }

        private void Attack()
        {
            _cooldownTimer = _attackCooldown;

            // Increment combo step (cycles 1, 2, 3)
            _comboStep = (_comboStep % 3) + 1;
            _comboTimer = _comboResetTime;

            // Determine attack direction based on movement facing direction
            Vector2 attackDir = _movement != null ? _movement.FacingDirection : Vector2.right;

            // Trigger animator state
            if (_animator != null)
            {
                _animator.PlayAttackAnimation();
                _animator.SetComboIndex(_comboStep);
                _animator.SetAlternateAttackParameter(_comboStep == 2);
            }

            // Spawn VFX at offset in attack direction and rotate towards it
            if (_slashVFXPrefab != null)
            {
                float finalOffset = _vfxSpawnOffset;
                if (_comboStep == 3)
                {
                    finalOffset *= _combo3OffsetMultiplier;
                }

                Vector3 spawnPos = transform.position + new Vector3(attackDir.x, attackDir.y, 0f) * finalOffset;
                float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
                Quaternion spawnRot = Quaternion.Euler(0f, 0f, angle);

                GameObject vfxInstance = Instantiate(_slashVFXPrefab, spawnPos, spawnRot, _parentVFXToPlayer ? transform : null);

                // Adjust scale and orientation based on combo step
                Vector3 baseScale = vfxInstance.transform.localScale;
                if (_comboStep == 3)
                {
                    vfxInstance.transform.localScale = baseScale * _combo3ScaleMultiplier;
                }
                else if (_comboStep == 2)
                {
                    vfxInstance.transform.localScale = new Vector3(baseScale.x, -baseScale.y, baseScale.z);
                }

                // Initialize the hitbox component for collisions
                SlashHitbox hitbox = vfxInstance.GetComponent<SlashHitbox>();
                if (hitbox == null)
                {
                    hitbox = vfxInstance.AddComponent<SlashHitbox>();
                }
                int damage = _comboStep == 1 ? _combo1Damage : (_comboStep == 2 ? _combo2Damage : _combo3Damage);
                float knockback = _comboStep == 1 ? _combo1Knockback : (_comboStep == 2 ? _combo2Knockback : _combo3Knockback);
                hitbox.Initialize(_comboStep, damage, knockback, gameObject);

                VisualEffect vfx = vfxInstance.GetComponentInChildren<VisualEffect>();
                if (vfx != null)
                {
                    if (!string.IsNullOrEmpty(_vfxColorParameterName))
                    {
                        Color finalColor = _comboStep == 3 ? _combo3VFXColor : _defaultVFXColor;
                        vfx.SetVector4(_vfxColorParameterName, finalColor);
                    }

                    vfx.Play();
                }

                Destroy(vfxInstance, _vfxDuration);
            }

            // Play SFX
            AudioClip sfxToPlay = (_comboStep == 3 && _attackSFXHeavy != null) ? _attackSFXHeavy : _attackSFXNormal;
            if (sfxToPlay != null && _audioService != null)
            {
                _audioService.PlaySFX(sfxToPlay);
            }

            // Screen Shake (Cinemachine)
            if (_impulseSource != null)
            {
                float shakeForce = _comboStep == 3 ? _combo3ShakeForce : _normalShakeForce;
                _impulseSource.GenerateImpulse(shakeForce);
            }

            // Gamepad Rumble
            TriggerGamepadRumble();
        }

        private void TriggerGamepadRumble()
        {
            if (_rumbleCoroutine != null)
            {
                StopCoroutine(_rumbleCoroutine);
            }

            float leftMotor = 0f;
            float rightMotor = 0f;
            float duration = _rumbleDuration;

            if (_comboStep == 3)
            {
                // Vibrate both motors at maximum intensity
                leftMotor = _combo3RumbleIntensity;
                rightMotor = _combo3RumbleIntensity;
                duration = _rumbleDuration * 1.5f; // Finisher is slightly longer
            }
            else if (_comboStep == 2)
            {
                // Vibrate left motor
                leftMotor = _normalRumbleIntensity;
            }
            else // _comboStep == 1
            {
                // Vibrate right motor
                rightMotor = _normalRumbleIntensity;
            }

            _rumbleCoroutine = StartCoroutine(RumbleRoutine(leftMotor, rightMotor, duration));
        }

        private IEnumerator RumbleRoutine(float leftSpeed, float rightSpeed, float duration)
        {
            UnityEngine.InputSystem.Gamepad gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(leftSpeed, rightSpeed);
                yield return new WaitForSeconds(duration);
                gamepad.SetMotorSpeeds(0f, 0f);
            }
            _rumbleCoroutine = null;
        }

        private void OnDisable()
        {
            StopRumble();
        }

        private void OnDestroy()
        {
            StopRumble();
        }

        private void StopRumble()
        {
            if (_rumbleCoroutine != null)
            {
                StopCoroutine(_rumbleCoroutine);
                _rumbleCoroutine = null;
            }

            UnityEngine.InputSystem.Gamepad gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }
        }
    }
}

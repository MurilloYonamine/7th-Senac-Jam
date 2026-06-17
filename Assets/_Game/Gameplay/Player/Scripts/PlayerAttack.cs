using System.Collections;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;
using UnityEngine;
using UnityEngine.VFX;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

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
        [SerializeField] private AudioClip _attackSFX1CLip;
        [SerializeField] private AudioClip _attackSFX2CLip;
        [Range(0f, 1f)]
        [SerializeField] private float _attackSFXVolume = 1f;

        [Header("Damage Settings")]
        [SerializeField] private int _attackDamage = 15;
        [SerializeField] private float _attackKnockback = 0f;

        [Header("VFX Glow (HDR)")]
        [SerializeField] private string _vfxColorParameterName = "Color";
        [ColorUsage(true, true)]
        [SerializeField] private Color _defaultVFXColor = Color.white;

        [Header("Screen Shake (Cinemachine)")]
        [SerializeField] private float _normalShakeForce = 0.4f;

        [Header("Controller Rumble Settings")]
        [SerializeField] private float _normalRumbleIntensity = 0.5f;
        [SerializeField] private float _rumbleDuration = 0.15f;

        private float _cooldownTimer = 0f;

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

            _inputService.GetAttackInput(out bool isAttacking, out _);

            if (isAttacking &&
                _cooldownTimer <= 0f &&
                (_dash == null || !_dash.IsDashing))
            {
                Attack();
            }
        }

        private void Attack()
        {
            _cooldownTimer = _attackCooldown;

            if (_animator != null)
            {
                _animator.PlayAttackAnimation();
            }
        }

        public void PerformAttackHit()
        {
            Vector2 attackDir = _movement != null
                ? _movement.FacingDirection
                : Vector2.right;

            if (_slashVFXPrefab != null)
            {
                Vector3 spawnPos =
                    transform.position +
                    new Vector3(attackDir.x, attackDir.y, 0f) * _vfxSpawnOffset;

                float angle =
                    Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;

                Quaternion spawnRot =
                    Quaternion.Euler(0f, 0f, angle);

                GameObject vfxInstance = Instantiate(
                    _slashVFXPrefab,
                    spawnPos,
                    spawnRot,
                    _parentVFXToPlayer ? transform : null);

                SlashHitbox hitbox = vfxInstance.GetComponent<SlashHitbox>();

                if (hitbox == null)
                {
                    hitbox = vfxInstance.AddComponent<SlashHitbox>();
                }

                hitbox.Initialize(
                    _attackDamage,
                    _attackKnockback,
                    gameObject);

                VisualEffect vfx =
                    vfxInstance.GetComponentInChildren<VisualEffect>();

                if (vfx != null)
                {
                    if (!string.IsNullOrEmpty(_vfxColorParameterName))
                    {
                        vfx.SetVector4(
                            _vfxColorParameterName,
                            _defaultVFXColor);
                    }

                    vfx.Play();
                }

                Destroy(vfxInstance, _vfxDuration);
            }

        }

        public void OnHitEnemy()
        {
            // --- Screen Shake (tela vibrando) ---
            if (_impulseSource != null)
            {
                _impulseSource.GenerateImpulse(_normalShakeForce);
            }

            TriggerGamepadRumble();
        }

        private void TriggerGamepadRumble()
        {
            if (_rumbleCoroutine != null)
            {
                StopCoroutine(_rumbleCoroutine);
            }

            _rumbleCoroutine = StartCoroutine(
                RumbleRoutine(
                    _normalRumbleIntensity,
                    _normalRumbleIntensity,
                    _rumbleDuration));
        }

        public void PlayAttack1SFX()
        {
            if (_audioService != null && _attackSFX1CLip != null)
            {
                _audioService.PlaySFX(
                    _attackSFX1CLip,
                    new AudioSettings(
                        volumeOffset: _attackSFXVolume - 1f));
            }
        }

        public void PlayAttack2SFX()
        {
            if (_audioService != null && _attackSFX2CLip != null)
            {
                _audioService.PlaySFX(
                    _attackSFX2CLip,
                    new AudioSettings(
                        volumeOffset: _attackSFXVolume - 1f));
            }
        }

        private IEnumerator RumbleRoutine(
            float leftSpeed,
            float rightSpeed,
            float duration)
        {
            Gamepad gamepad = Gamepad.current;

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

            Gamepad.current?.SetMotorSpeeds(0f, 0f);
        }
    }
}
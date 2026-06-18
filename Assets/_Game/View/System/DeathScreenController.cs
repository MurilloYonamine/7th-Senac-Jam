using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using Seventh.Core.Services;
using Seventh.Core.Constants;
using Seventh.Gameplay.Player;

namespace Seventh.View
{
    public class DeathScreenController : MonoBehaviour
    {
        [Header("Player Reference")]
        [SerializeField] private PlayerHealth _playerHealth;

        [Header("UI References")]
        [SerializeField] private CanvasGroup _deathScreenCanvasGroup;
        [SerializeField] private TextMeshProUGUI _youDiedText;

        [Header("Timing Configurations")]
        [SerializeField] private float _delayBeforeShow = 0.5f;
        [SerializeField] private float _bgFadeDuration = 2.0f;
        [SerializeField] private float _textFadeDuration = 1.5f;
        [SerializeField] private float _textAnimationDuration = 4.0f;
        [SerializeField] private float _delayBeforeRestart = 4.5f;

        [Header("Text Zoom Settings")]
        [SerializeField] private float _startScale = 0.8f;
        [SerializeField] private float _targetScale = 1.15f;
        [SerializeField] private float _startCharacterSpacing = 0f;
        [SerializeField] private float _targetCharacterSpacing = 20f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _deathSFX;
        [Range(0f, 1f)][SerializeField] private float _sfxVolume = 1f;

        [Header("Respawn Settings")]
        [SerializeField] private Transform _respawnPoint;

        private bool _isDying = false;
        private Vector3 _initialSpawnPosition;
        private Tween _canvasFadeTween;
        private Tween _textFadeTween;
        private Tween _textScaleTween;
        private Tween _textSpacingTween;

        private void Awake()
        {
            // Auto-locate player health in scene if not assigned
            if (_playerHealth == null)
            {
                _playerHealth = FindAnyObjectByType<PlayerHealth>();
            }

            // Hide death screen UI initially
            if (_deathScreenCanvasGroup != null)
            {
                _deathScreenCanvasGroup.alpha = 0f;
                _deathScreenCanvasGroup.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (_playerHealth != null)
            {
                _initialSpawnPosition = _playerHealth.transform.position;
            }
        }

        private void OnEnable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath += OnPlayerDeath;
            }
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath -= OnPlayerDeath;
            }
            KillAllTweens();
        }

        private void OnPlayerDeath()
        {
            if (_isDying) return;
            _isDying = true;

            if (_deathScreenCanvasGroup != null)
            {
                _deathScreenCanvasGroup.gameObject.SetActive(true);
            }
            StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            // 1. Pause gameplay by setting state to Cutscene (if GameStateService exists)
            if (ServiceLocator.TryGet<IGameStateService>(out var gameStateService))
            {
                gameStateService.ChangeGameState(GameState.Cutscene);
            }

            // 2. Play dramatic death sound effect
            if (_deathSFX != null)
            {
                var audioService = ServiceLocator.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.PlaySFX(_deathSFX, new Core.Services.AudioSettings(volumeOffset: _sfxVolume - 1f));
                }
                else
                {
                    AudioSource.PlayClipAtPoint(_deathSFX, Camera.main != null ? Camera.main.transform.position : transform.position, _sfxVolume);
                }
            }

            // 3. Reset UI states
            _deathScreenCanvasGroup.alpha = 0f;
            if (_youDiedText != null)
            {
                _youDiedText.alpha = 0f;
                _youDiedText.characterSpacing = _startCharacterSpacing;
                _youDiedText.transform.localScale = Vector3.one * _startScale;
            }

            // 4. Fade in Background
            _canvasFadeTween = _deathScreenCanvasGroup.DOFade(1f, _bgFadeDuration).SetEase(Ease.OutQuad);

            // 5. Animate Text (Fade, Zoom & Spacing)
            if (_youDiedText != null)
            {
                _textFadeTween = _youDiedText.DOFade(1f, _textFadeDuration)
                    .SetDelay(_delayBeforeShow)
                    .SetEase(Ease.OutQuad);

                _textScaleTween = _youDiedText.transform.DOScale(_targetScale, _textAnimationDuration)
                    .SetDelay(_delayBeforeShow)
                    .SetEase(Ease.OutCubic);

                _textSpacingTween = DOTween.To(
                    () => _youDiedText.characterSpacing,
                    x => _youDiedText.characterSpacing = x,
                    _targetCharacterSpacing,
                    _textAnimationDuration
                )
                .SetDelay(_delayBeforeShow)
                .SetEase(Ease.OutCubic);
            }

            // 6. Wait before respawn sequence
            yield return new WaitForSeconds(_delayBeforeRestart);

            // 7. Respawn the player (resetting health, scale, rotation and color)
            Vector3 respawnPos = _respawnPoint != null ? _respawnPoint.position : _initialSpawnPosition;
            if (_playerHealth != null)
            {
                _playerHealth.Respawn(respawnPos);
            }

            // 8. Restore GameState to Playing
            if (ServiceLocator.TryGet<IGameStateService>(out var playStateService))
            {
                playStateService.ChangeGameState(GameState.Playing);
            }

            // 9. Fade out the death screen and hide it
            _canvasFadeTween = _deathScreenCanvasGroup.DOFade(0f, 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _deathScreenCanvasGroup.gameObject.SetActive(false);
                    _isDying = false;
                });
        }

        private void KillAllTweens()
        {
            _canvasFadeTween?.Kill();
            _textFadeTween?.Kill();
            _textScaleTween?.Kill();
            _textSpacingTween?.Kill();
        }
    }
}

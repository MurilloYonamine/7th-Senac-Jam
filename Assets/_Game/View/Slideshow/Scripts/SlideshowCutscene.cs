using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using Seventh.Core.Services;
using Seventh.Core.Constants;

namespace Seventh.View
{
    [System.Serializable]
    public struct CutsceneSlide
    {
        public Sprite sprite;
        [TextArea(3, 10)]
        public string dialogueText;
    }

    public class SlideshowCutscene : MonoBehaviour
    {
        [Header("Slides Configuration")]
        [SerializeField] private CutsceneSlide[] _slides;

        [Header("UI References")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TextMeshProUGUI _dialogueTextElement;
        [SerializeField] private CanvasGroup _mainCanvasGroup;
        [SerializeField] private CanvasGroup _contentCanvasGroup;

        private CanvasGroup TransitionCanvasGroup => _contentCanvasGroup != null ? _contentCanvasGroup : _mainCanvasGroup;

        [Header("Input")]
        [SerializeField] private InputActionReference _advanceAction;

        [Header("Timing and Speeds")]
        [SerializeField] private float _charactersPerSecond = 30f;
        [SerializeField] private float _canvasFadeDuration = 0.3f;
        [SerializeField] private float _textFadeDuration = 0.5f;


        [Header("Scene Loading on End")]
        [SerializeField] private bool _loadSceneOnEnd = false;
        [SerializeField] private string _sceneNameToLoad = "MainMenu";

        [Header("Events")]
        public UnityEngine.Events.UnityEvent _onCutsceneStart;
        public UnityEngine.Events.UnityEvent _onCutsceneEnd;

        private int _currentSlideIndex = 0;
        private bool _isTyping = false;
        private bool _isTransitioning = false;
        private bool _isPlaying = false;
        private string _currentTargetText = "";

        private Tween _typewriterTween;
        private Tween _textFadeTween;
        private Tween _canvasFadeTween;

        private void Awake()
        {
            if (_mainCanvasGroup != null)
            {
                _mainCanvasGroup.alpha = 0f;
            }
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_advanceAction != null)
            {
                _advanceAction.action.performed += OnAdvancePerformed;
                _advanceAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_advanceAction != null)
            {
                _advanceAction.action.performed -= OnAdvancePerformed;
            }
            KillAllTweens();
        }

        /// <summary>
        /// Starts the slideshow cutscene from the first slide.
        /// </summary>
        public void StartCutscene()
        {
            if (_slides == null || _slides.Length == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            _mainCanvasGroup.gameObject.SetActive(true);

            gameObject.SetActive(true);
            _isPlaying = true;
            _currentSlideIndex = 0;
            _isTransitioning = true;
            _isTyping = false;

            KillAllTweens();
            _onCutsceneStart?.Invoke();

            if (ServiceLocator.TryGet<IGameStateService>(out var gameStateService))
            {
                gameStateService.ChangeGameState(GameState.Cutscene);
            }

            _mainCanvasGroup.alpha = 0f;
            if (_contentCanvasGroup != null)
            {
                _contentCanvasGroup.alpha = 1f;
            }

            if (_backgroundImage != null)
            {
                if (_slides[0].sprite != null)
                {
                    _backgroundImage.sprite = _slides[0].sprite;
                    _backgroundImage.gameObject.SetActive(true);
                }
                else
                {
                    _backgroundImage.gameObject.SetActive(false);
                }
            }

            _currentTargetText = _slides[0].dialogueText;
            _dialogueTextElement.text = _currentTargetText;
            _dialogueTextElement.maxVisibleCharacters = 0;
            _dialogueTextElement.alpha = 0f;

            _canvasFadeTween = _mainCanvasGroup.DOFade(1f, _canvasFadeDuration)
                .OnComplete(() =>
                {
                    _isTransitioning = false;
                    StartTyping();
                });
        }

        private void OnAdvancePerformed(InputAction.CallbackContext context)
        {
            if (!_isPlaying || _isTransitioning) return;

            if (_isTyping)
            {
                CompleteTyping();
            }
            else
            {
                AdvanceToNextSlide();
            }
        }

        private void StartTyping()
        {
            if (_dialogueTextElement == null) return;

            _isTyping = true;
            _textFadeTween = _dialogueTextElement.DOFade(1f, _textFadeDuration);

            float duration = _charactersPerSecond > 0 ? (_currentTargetText.Length / _charactersPerSecond) : 0f;

            _typewriterTween = DOTween.To(
                () => _dialogueTextElement.maxVisibleCharacters,
                x => _dialogueTextElement.maxVisibleCharacters = x,
                _currentTargetText.Length,
                duration
            )
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                _isTyping = false;
            });
        }

        private void CompleteTyping()
        {
            KillTween(ref _typewriterTween);
            KillTween(ref _textFadeTween);

            if (_dialogueTextElement != null)
            {
                _dialogueTextElement.maxVisibleCharacters = _currentTargetText.Length;
                _dialogueTextElement.alpha = 1f;
            }

            _isTyping = false;
        }

        private void AdvanceToNextSlide()
        {
            _currentSlideIndex++;

            if (_currentSlideIndex >= _slides.Length)
            {
                EndCutscene();
            }
            else
            {
                TransitionToSlide(_currentSlideIndex);
            }
        }

        private void TransitionToSlide(int index)
        {
            _isTransitioning = true;
            KillAllTweens();

            _canvasFadeTween = TransitionCanvasGroup.DOFade(0f, _canvasFadeDuration)
                .OnComplete(() =>
                {
                    if (_backgroundImage != null)
                    {
                        if (_slides[index].sprite != null)
                        {
                            _backgroundImage.sprite = _slides[index].sprite;
                            _backgroundImage.gameObject.SetActive(true);
                        }
                        else
                        {
                            _backgroundImage.gameObject.SetActive(false);
                        }
                    }

                    _currentTargetText = _slides[index].dialogueText;
                    _dialogueTextElement.text = _currentTargetText;
                    _dialogueTextElement.maxVisibleCharacters = 0;
                    _dialogueTextElement.alpha = 0f;

                    _canvasFadeTween = TransitionCanvasGroup.DOFade(1f, _canvasFadeDuration)
                        .OnComplete(() =>
                        {
                            _isTransitioning = false;
                            StartTyping();
                        });
                });
        }

        public void EndCutscene()
        {
            _isPlaying = false;
            _isTransitioning = true;
            KillAllTweens();

            _canvasFadeTween = _mainCanvasGroup.DOFade(0f, _canvasFadeDuration)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);

                    if (_loadSceneOnEnd && !string.IsNullOrEmpty(_sceneNameToLoad))
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(_sceneNameToLoad);
                        return;
                    }

                    if (ServiceLocator.TryGet<IGameStateService>(out var gameStateService))
                    {
                        gameStateService.ChangeGameState(GameState.Playing);
                    }

                    _onCutsceneEnd?.Invoke();
                    _isTransitioning = false;
                });
        }

        private void KillAllTweens()
        {
            KillTween(ref _typewriterTween);
            KillTween(ref _textFadeTween);
            KillTween(ref _canvasFadeTween);
        }

        private void KillTween(ref Tween tween)
        {
            if (tween != null)
            {
                if (tween.IsActive())
                {
                    tween.Kill();
                }
                tween = null;
            }
        }
    }
}

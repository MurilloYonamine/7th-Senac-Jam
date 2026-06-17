using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Seventh.Core.Constants;
using Seventh.Core.Events;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;

namespace Seventh.View.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuState : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private Button _btnResume;
        private Button _btnSettings;
        private Button _btnMainMenu;
        private Button _btnQuit;
        private IInputService _inputService;
        private IGameStateService _gameStateService;
        private IAudioService _audioService;
        private Tween _fadeTween;
        private bool _allowHoverSound;
        private bool _restoreFocusToSettings;

        [SerializeField] private SettingsMenuState _settingsState;
        [Header("Configurações de Áudio")]
        [SerializeField] private AudioClip _clickSFX;
        [Range(0f, 1f)] [SerializeField] private float _clickSFXVolume = 1f;
        [SerializeField] private AudioClip _transitionSFX;
        [Range(0f, 1f)] [SerializeField] private float _transitionSFXVolume = 1f;
        [SerializeField] private AudioClip _hoverSFX;
        [Range(0f, 1f)] [SerializeField] private float _hoverSFXVolume = 1f;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            _inputService = ServiceLocator.Get<IInputService>();
            _gameStateService = ServiceLocator.Get<IGameStateService>();
            _audioService = ServiceLocator.Get<IAudioService>();
        }

        private void OnEnable()
        {
            Time.timeScale = 0f;
            if (_gameStateService != null)
            {
                _gameStateService.ChangeGameState(GameState.Paused);
            }

            var eventBus = ServiceLocator.Get<IEventBus>();
            if (eventBus != null)
            {
                eventBus.Subscribe<PlayerMenuPressedEvent>(HandleMenuPressed);
            }

            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _btnResume = root.Q<Button>("BtnResume");
            _btnSettings = root.Q<Button>("BtnSettings");
            _btnMainMenu = root.Q<Button>("BtnMainMenu");
            _btnQuit = root.Q<Button>("BtnQuit");

            if (_btnResume != null)
            {
                _btnResume.focusable = true;
                _btnResume.clicked += OnResumeClicked;
            }
            if (_btnSettings != null)
            {
                _btnSettings.focusable = true;
                _btnSettings.clicked += OnSettingsClicked;
            }
            if (_btnMainMenu != null)
            {
                _btnMainMenu.focusable = true;
                _btnMainMenu.clicked += OnMainMenuClicked;
            }
            if (_btnQuit != null)
            {
                _btnQuit.focusable = true;
                _btnQuit.clicked += OnQuitClicked;
            }

            _allowHoverSound = false;
            root.Query<Button>(className: "menu-button").ForEach(RegisterButtonEvents);

            PlayTransitionSound();

            FadeIn(() =>
            {
                if (_restoreFocusToSettings && _btnSettings != null)
                {
                    _btnSettings.Focus();
                }
                else
                {
                    _btnResume?.Focus();
                }
                _restoreFocusToSettings = false;
                _allowHoverSound = true;
            });
        }

        private void OnDisable()
        {
            _fadeTween?.Kill();

            var eventBus = ServiceLocator.Get<IEventBus>();
            if (eventBus != null)
            {
                eventBus.Unsubscribe<PlayerMenuPressedEvent>(HandleMenuPressed);
            }

            if (_btnResume != null) _btnResume.clicked -= OnResumeClicked;
            if (_btnSettings != null) _btnSettings.clicked -= OnSettingsClicked;
            if (_btnMainMenu != null) _btnMainMenu.clicked -= OnMainMenuClicked;
            if (_btnQuit != null) _btnQuit.clicked -= OnQuitClicked;

            if (_uiDocument != null)
            {
                var root = _uiDocument.rootVisualElement;
                if (root != null)
                {
                    root.Query<Button>(className: "menu-button").ForEach(UnregisterButtonEvents);
                }
            }
        }

        public void PauseGame()
        {
            gameObject.SetActive(true);
        }

        private void HandleMenuPressed(PlayerMenuPressedEvent evt)
        {
            ResumeGame();
        }

        private void ResumeGame()
        {
            Time.timeScale = 1f;
            FadeOut(() => gameObject.SetActive(false));
            _gameStateService?.ChangeGameState(GameState.Playing);
        }

        private void OnResumeClicked()
        {
            PlayClickSound();
            ResumeGame();
        }

        private void OnSettingsClicked()
        {
            PlayClickSound();
            if (_settingsState != null)
            {
                _settingsState.Open(this);
                FadeOut(() => gameObject.SetActive(false));
            }
        }

        private void OnMainMenuClicked()
        {
            PlayClickSound();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnQuitClicked()
        {
            PlayClickSound();
            Application.Quit();
        }

        public void ReturnFromState(MonoBehaviour finishedState)
        {
            _restoreFocusToSettings = true;
            gameObject.SetActive(true);
        }

        private void PlayClickSound()
        {
            if (_audioService != null && _clickSFX != null)
            {
                _audioService.PlaySFX(_clickSFX, new AudioSettings(volumeOffset: _clickSFXVolume - 1f));
            }
        }

        private void PlayTransitionSound()
        {
            if (_audioService != null && _transitionSFX != null)
            {
                _audioService.PlaySFX(_transitionSFX, new AudioSettings(volumeOffset: _transitionSFXVolume - 1f));
            }
        }

        private void PlayHoverSound()
        {
            if (_audioService != null && _hoverSFX != null)
            {
                _audioService.PlaySFX(_hoverSFX, new AudioSettings(volumeOffset: _hoverSFXVolume - 1f));
            }
        }

        private void RegisterButtonEvents(Button button)
        {
            if (button == null) return;
            button.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            button.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            button.RegisterCallback<FocusInEvent>(OnFocusIn);
        }

        private void UnregisterButtonEvents(Button button)
        {
            if (button == null) return;
            button.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            button.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            button.UnregisterCallback<FocusInEvent>(OnFocusIn);
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                button.Focus();
            }
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                button.Blur();
            }
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            if (_allowHoverSound)
            {
                PlayHoverSound();
            }
        }

        private void FadeIn(System.Action onComplete = null)
        {
            _fadeTween?.Kill();

            var root = _uiDocument.rootVisualElement;
            if (root != null)
            {
                root.style.opacity = 0f;
                _fadeTween = DOTween.To(() => root.style.opacity.value, x => root.style.opacity = x, 1f, 0.25f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private void FadeOut(System.Action onComplete = null)
        {
            _fadeTween?.Kill();

            var root = _uiDocument.rootVisualElement;
            if (root != null)
            {
                _fadeTween = DOTween.To(() => root.style.opacity.value, x => root.style.opacity = x, 0f, 0.2f)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}

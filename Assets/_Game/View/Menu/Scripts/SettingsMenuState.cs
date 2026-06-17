using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;

namespace Seventh.View.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class SettingsMenuState : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private Button _btnBack;
        private MainMenuState _mainState;
        private Tween _fadeTween;
        private IAudioService _audioService;
        private bool _allowHoverSound;
        private readonly HashSet<Slider> _activeSliders = new HashSet<Slider>();

        // Elementos de UI - Áudio
        private Slider _sliderMusic;
        private Slider _sliderSFX;
        private Slider _sliderAmbient;

        // Elementos de UI - Vídeo
        private DropdownField _dropdownResolution;
        private Toggle _toggleFullscreen;
        private Toggle _toggleVSync;

        private readonly List<string> _resolutions169 = new List<string>
        {
            "1920 x 1080",
            "1600 x 900",
            "1366 x 768",
            "1280 x 720"
        };

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
        }

        private void Start()
        {
            _audioService = ServiceLocator.Get<IAudioService>();
        }

        public void Open(MainMenuState mainState)
        {
            _mainState = mainState;
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            if (root.panel != null)
            {
                int sheetCount = root.styleSheets.count;
                for (int i = 0; i < sheetCount; i++)
                {
                    var sheet = root.styleSheets[i];
                    if (sheet != null && !root.panel.visualTree.styleSheets.Contains(sheet))
                    {
                        root.panel.visualTree.styleSheets.Add(sheet);
                    }
                }
            }

            _btnBack = root.Q<Button>("BtnBack");
            
            _sliderMusic = root.Q<Slider>("SliderMusic");
            _sliderSFX = root.Q<Slider>("SliderSFX");
            _sliderAmbient = root.Q<Slider>("SliderAmbient");
            
            _dropdownResolution = root.Q<DropdownField>("DropdownResolution");
            _toggleFullscreen = root.Q<Toggle>("ToggleFullscreen");
            _toggleVSync = root.Q<Toggle>("ToggleVSync");

            _activeSliders.Clear();
            if (_sliderMusic != null) _sliderMusic.focusable = true;
            if (_sliderSFX != null) _sliderSFX.focusable = true;
            if (_sliderAmbient != null) _sliderAmbient.focusable = true;
            if (_dropdownResolution != null) _dropdownResolution.focusable = true;
            if (_toggleFullscreen != null) _toggleFullscreen.focusable = true;
            if (_toggleVSync != null) _toggleVSync.focusable = true;
            if (_btnBack != null) _btnBack.focusable = true;

            SetupSliderInteraction(_sliderMusic);
            SetupSliderInteraction(_sliderSFX);
            SetupSliderInteraction(_sliderAmbient);

            SetupExplicitNavigation();

            _sliderMusic?.Focus();

            if (_btnBack != null)
            {
                _btnBack.clicked += OnBackClicked;
            }

            InitializeAudioSettings();

            InitializeVideoSettings();

            _allowHoverSound = false;
            root.Query<Button>(className: "menu-button").ForEach(RegisterButtonEvents);

            PlayTransitionSound();

            FadeIn(() =>
            {
                if (_sliderMusic != null)
                {
                    _sliderMusic.Focus();
                }
                else
                {
                    _btnBack?.Focus();
                }
                _allowHoverSound = true;
            });
        }

        private void OnDisable()
        {
            _fadeTween?.Kill();

            if (_btnBack != null) _btnBack.clicked -= OnBackClicked;

            if (_sliderMusic != null) _sliderMusic.UnregisterValueChangedCallback(OnMusicVolumeChanged);
            if (_sliderSFX != null) _sliderSFX.UnregisterValueChangedCallback(OnSFXVolumeChanged);
            if (_sliderAmbient != null) _sliderAmbient.UnregisterValueChangedCallback(OnAmbientVolumeChanged);

            if (_dropdownResolution != null) _dropdownResolution.UnregisterValueChangedCallback(OnResolutionChanged);
            if (_toggleFullscreen != null) _toggleFullscreen.UnregisterValueChangedCallback(OnFullscreenChanged);
            if (_toggleVSync != null) _toggleVSync.UnregisterValueChangedCallback(OnVSyncChanged);

            if (_uiDocument != null)
            {
                var root = _uiDocument.rootVisualElement;
                if (root != null)
                {
                    root.Query<Button>(className: "menu-button").ForEach(UnregisterButtonEvents);
                }
            }
        }

        private void InitializeAudioSettings()
        {
            if (_audioService == null) return;

            if (_sliderMusic != null)
            {
                _sliderMusic.value = _audioService.MusicVolume;
                _sliderMusic.RegisterValueChangedCallback(OnMusicVolumeChanged);
            }

            if (_sliderSFX != null)
            {
                _sliderSFX.value = _audioService.SfxVolume;
                _sliderSFX.RegisterValueChangedCallback(OnSFXVolumeChanged);
            }

            if (_sliderAmbient != null)
            {
                _sliderAmbient.value = _audioService.AmbientVolume;
                _sliderAmbient.RegisterValueChangedCallback(OnAmbientVolumeChanged);
            }
        }

        private void InitializeVideoSettings()
        {
            if (_dropdownResolution != null)
            {
                _dropdownResolution.choices = _resolutions169;
                string currentResStr = $"{Screen.width} x {Screen.height}";

                if (_resolutions169.Contains(currentResStr))
                {
                    _dropdownResolution.value = currentResStr;
                }
                else
                {
                    _dropdownResolution.value = _resolutions169[0];
                }

                _dropdownResolution.RegisterValueChangedCallback(OnResolutionChanged);
            }

            if (_toggleFullscreen != null)
            {
                _toggleFullscreen.value = Screen.fullScreen;
                _toggleFullscreen.RegisterValueChangedCallback(OnFullscreenChanged);
            }

            if (_toggleVSync != null)
            {
                _toggleVSync.value = QualitySettings.vSyncCount > 0;
                _toggleVSync.RegisterValueChangedCallback(OnVSyncChanged);
            }
        }

        private void OnMusicVolumeChanged(ChangeEvent<float> evt)
        {
            _audioService?.SetMusicVolume(evt.newValue);
        }

        private void OnSFXVolumeChanged(ChangeEvent<float> evt)
        {
            _audioService?.SetSFXVolume(evt.newValue);
        }

        private void OnAmbientVolumeChanged(ChangeEvent<float> evt)
        {
            _audioService?.SetAmbientVolume(evt.newValue);
        }

        private void OnResolutionChanged(ChangeEvent<string> evt)
        {
            string selection = evt.newValue;
            if (string.IsNullOrEmpty(selection)) return;

            string[] parts = selection.Split('x');
            if (parts.Length == 2 && 
                int.TryParse(parts[0].Trim(), out int width) && 
                int.TryParse(parts[1].Trim(), out int height))
            {
                Screen.SetResolution(width, height, Screen.fullScreen);
            }
        }

        private void OnFullscreenChanged(ChangeEvent<bool> evt)
        {
            Debug.Log($"[SettingsMenuState] Alterando Tela Cheia para: {evt.newValue}");
            Screen.fullScreen = evt.newValue;
        }

        private void OnVSyncChanged(ChangeEvent<bool> evt)
        {
            Debug.Log($"[SettingsMenuState] Alterando V-Sync para: {evt.newValue}");
            QualitySettings.vSyncCount = evt.newValue ? 1 : 0;
        }

        private void OnBackClicked()
        {
            PlayClickSound();

            if (_mainState != null)
            {
                _mainState.ReturnFromState(this);
            }

            FadeOut(() => gameObject.SetActive(false));
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

        private void FadeIn(System.Action onComplete = null)
        {
            _fadeTween?.Kill();

            var root = _uiDocument.rootVisualElement;
            if (root != null)
            {
                root.style.opacity = 0f;
                _fadeTween = DOTween.To(() => root.style.opacity.value, x => root.style.opacity = x, 1f, 0.25f)
                    .SetEase(Ease.OutQuad)
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
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
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

        private void SetupExplicitNavigation()
        {
            RegisterNavigation(_sliderMusic, down: _sliderSFX);
            RegisterNavigation(_sliderSFX, up: _sliderMusic, down: _sliderAmbient);
            RegisterNavigation(_sliderAmbient, up: _sliderSFX, down: _btnBack);

            RegisterNavigation(_dropdownResolution, left: _sliderMusic, down: _toggleFullscreen);
            RegisterNavigation(_toggleFullscreen, left: _sliderSFX, up: _dropdownResolution, down: _toggleVSync);
            RegisterNavigation(_toggleVSync, left: _sliderAmbient, up: _toggleFullscreen, down: _btnBack);

            RegisterNavigation(_btnBack, up: _sliderAmbient, left: _sliderAmbient, right: _toggleVSync);
        }

        private void RegisterNavigation(Focusable element, Focusable up = null, Focusable down = null, Focusable left = null, Focusable right = null)
        {
            if (element == null) return;

            element.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                Focusable target = null;
                switch (evt.direction)
                {
                    case NavigationMoveEvent.Direction.Up:
                        target = up;
                        break;
                    case NavigationMoveEvent.Direction.Down:
                        target = down;
                        break;
                    case NavigationMoveEvent.Direction.Left:
                        target = left;
                        break;
                    case NavigationMoveEvent.Direction.Right:
                        target = right;
                        break;
                }

                if (target != null)
                {
                    target.Focus();
                    evt.StopPropagation();
                }
            });
        }

        private void SetupSliderInteraction(Slider slider)
        {
            if (slider == null) return;

            slider.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                if (!_activeSliders.Contains(slider))
                {
                    ActivateSlider(slider);
                }
                else
                {
                    DeactivateSlider(slider);
                }
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);

            slider.RegisterCallback<NavigationCancelEvent>(evt =>
            {
                if (_activeSliders.Contains(slider))
                {
                    DeactivateSlider(slider);
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);

            slider.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                bool isActive = _activeSliders.Contains(slider);

                if (!isActive)
                {
                    if (evt.direction == NavigationMoveEvent.Direction.Right)
                    {
                        MoveFocusFromSlider(slider);
                        evt.StopImmediatePropagation();
                    }
                    else if (evt.direction == NavigationMoveEvent.Direction.Left)
                    {
                        evt.StopImmediatePropagation();
                    }
                }
                else
                {
                    if (evt.direction == NavigationMoveEvent.Direction.Up || evt.direction == NavigationMoveEvent.Direction.Down)
                    {
                        DeactivateSlider(slider);
                    }
                }
            }, TrickleDown.TrickleDown);

            slider.RegisterCallback<KeyDownEvent>(evt =>
            {
                bool isActive = _activeSliders.Contains(slider);

                if (!isActive)
                {
                    if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.RightArrow ||
                        evt.keyCode == KeyCode.Keypad4 || evt.keyCode == KeyCode.Keypad6)
                    {
                        evt.StopImmediatePropagation();

                        if (evt.keyCode == KeyCode.RightArrow || evt.keyCode == KeyCode.Keypad6)
                        {
                            MoveFocusFromSlider(slider);
                        }
                    }

                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Space)
                    {
                        ActivateSlider(slider);
                        evt.StopImmediatePropagation();
                    }
                }
                else
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Escape)
                    {
                        DeactivateSlider(slider);
                        evt.StopImmediatePropagation();
                    }
                }
            }, TrickleDown.TrickleDown);
        }

        private void ActivateSlider(Slider slider)
        {
            if (slider == null) return;
            _activeSliders.Add(slider);
            slider.AddToClassList("slider-active");
        }

        private void DeactivateSlider(Slider slider)
        {
            if (slider == null) return;
            _activeSliders.Remove(slider);
            slider.RemoveFromClassList("slider-active");
        }

        private void MoveFocusFromSlider(Slider slider)
        {
            if (slider == _sliderMusic && _dropdownResolution != null)
                _dropdownResolution.Focus();
            else if (slider == _sliderSFX && _toggleFullscreen != null)
                _toggleFullscreen.Focus();
            else if (slider == _sliderAmbient && _toggleVSync != null)
                _toggleVSync.Focus();
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;
using UnityEngine.SceneManagement;

namespace Seventh.View.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuState : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private Button _btnPlay;
        private Button _btnSettings;
        private Button _btnCredits;
        private Button _btnQuit;
        private IAudioService _audioService;

        [Header("Telas de Destino (Estados)")]
        [SerializeField] private SettingsMenuState _settingsState;
        [SerializeField] private CreditsMenuState _creditsState;

        [Header("Configurações de Áudio")]
        [SerializeField] private AudioClip _clickSFX;
        [Range(0f, 1f)] [SerializeField] private float _clickSFXVolume = 1f;
        [SerializeField] private AudioClip _transitionSFX;
        [Range(0f, 1f)] [SerializeField] private float _transitionSFXVolume = 1f;

        private bool _restoringFocus;
        private string _buttonToFocusName;
        private Tween _fadeTween;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            _audioService = ServiceLocator.Get<IAudioService>();
            
            // Caso comece ativo na cena, garante que toca o som inicial
            if (gameObject.activeInHierarchy)
            {
                PlayTransitionSound();
            }
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            // Busca os botões principais pelo ID do UXML
            _btnPlay = root.Q<Button>("BtnPlay");
            _btnSettings = root.Q<Button>("BtnSettings");
            _btnCredits = root.Q<Button>("BtnCredits");
            _btnQuit = root.Q<Button>("BtnQuit");

            // Inscreve as funções nos cliques dos botões
            if (_btnPlay != null) _btnPlay.clicked += OnPlayClicked;
            if (_btnSettings != null) _btnSettings.clicked += OnSettingsClicked;
            if (_btnCredits != null) _btnCredits.clicked += OnCreditsClicked;
            if (_btnQuit != null) _btnQuit.clicked += OnQuitClicked;

            // Toca som de transição de estado ao abrir
            PlayTransitionSound();

            // Inicia a animação de Fade In
            FadeIn(() =>
            {
                // Restaura o foco apropriado após o fade-in para suporte a teclado/gamepad
                if (_restoringFocus)
                {
                    var targetBtn = root.Q<Button>(_buttonToFocusName);
                    if (targetBtn != null)
                    {
                        targetBtn.Focus();
                    }
                    else
                    {
                        _btnPlay?.Focus();
                    }
                    _restoringFocus = false;
                }
                else
                {
                    _btnPlay?.Focus();
                }
            });
        }

        private void OnDisable()
        {
            _fadeTween?.Kill();

            if (_btnPlay != null) _btnPlay.clicked -= OnPlayClicked;
            if (_btnSettings != null) _btnSettings.clicked -= OnSettingsClicked;
            if (_btnCredits != null) _btnCredits.clicked -= OnCreditsClicked;
            if (_btnQuit != null) _btnQuit.clicked -= OnQuitClicked;
        }

        private void OnPlayClicked()
        {
            PlayClickSound();
            SceneManager.LoadScene("GameScene");
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

        private void OnCreditsClicked()
        {
            PlayClickSound();
            if (_creditsState != null)
            {
                _creditsState.Open(this);
                FadeOut(() => gameObject.SetActive(false));
            }
        }

        private void OnQuitClicked()
        {
            PlayClickSound();
            Debug.Log("[MainMenuState] Sair clicado!");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
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

        public void ReturnFromState(MonoBehaviour finishedState)
        {
            _restoringFocus = true;

            if (finishedState is SettingsMenuState)
            {
                _buttonToFocusName = "BtnSettings";
            }
            else if (finishedState is CreditsMenuState)
            {
                _buttonToFocusName = "BtnCredits";
            }
            else
            {
                _buttonToFocusName = "BtnPlay";
            }

            gameObject.SetActive(true);
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
    }
}

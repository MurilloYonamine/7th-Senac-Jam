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

        [Header("Configurações de Áudio")]
        [SerializeField] private AudioClip _clickSFX;
        [Range(0f, 1f)] [SerializeField] private float _clickSFXVolume = 1f;
        [SerializeField] private AudioClip _transitionSFX;
        [Range(0f, 1f)] [SerializeField] private float _transitionSFXVolume = 1f;

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

            // Busca o botão Voltar pelo ID do UXML
            _btnBack = root.Q<Button>("BtnBack");
            if (_btnBack != null)
            {
                _btnBack.clicked += OnBackClicked;
            }

            // Toca som de transição de estado ao abrir
            PlayTransitionSound();

            // Inicia animação de Fade In
            FadeIn(() =>
            {
                _btnBack?.Focus(); // Dá foco imediato para suporte a controle
            });
        }

        private void OnDisable()
        {
            _fadeTween?.Kill();

            // Desinscreve para evitar leaks
            if (_btnBack != null)
            {
                _btnBack.clicked -= OnBackClicked;
            }
        }

        private void OnBackClicked()
        {
            PlayClickSound();

            if (_mainState != null)
            {
                _mainState.ReturnFromState(this);
            }

            // Inicia fade out e desativa o GameObject no final da animação
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
    }
}

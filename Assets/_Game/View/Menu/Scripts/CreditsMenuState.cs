using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;

namespace Seventh.View.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class CreditsMenuState : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private Button _btnBack;
        private MainMenuState _mainState;
        private Tween _fadeTween;
        private IAudioService _audioService;

        private readonly List<VisualElement> _spawnedTags = new List<VisualElement>();

        private readonly string[] _creditsNames = new string[]
        {
            "Murillo Yonamine\nProgramador",
            "Vitoria Harumi\nArtista 2D",
            "Guilherme Mitsuo\nGame & Sound\nDesign",
            "Luan Neves\nArtista 2D"
        };

        [Header("Configurações de Áudio")]
        [SerializeField] private AudioClip _clickSFX;
        [Range(0f, 1f)] [SerializeField] private float _clickSFXVolume = 1f;
        [SerializeField] private AudioClip _jumpSFX;
        [Range(0f, 1f)] [SerializeField] private float _jumpSFXVolume = 1f;
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

            // Inicia animação de Fade In de toda a tela
            FadeIn(() =>
            {
                _btnBack?.Focus(); // Dá foco imediato para suporte a controle
            });

            // Cria e anima a queda dos cartões de nomes
            SpawnAndAnimateCredits(root);
        }

        private void OnDisable()
        {
            _fadeTween?.Kill();

            // Limpa as animações dos cartões para evitar leaks
            foreach (var tag in _spawnedTags)
            {
                DOTween.Kill(tag);
            }

            // Desinscreve o botão voltar
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

        private void PlayJumpSound()
        {
            if (_audioService != null && _jumpSFX != null)
            {
                _audioService.PlaySFX(_jumpSFX, new AudioSettings(volumeOffset: _jumpSFXVolume - 1f));
            }
        }

        private void PlayTransitionSound()
        {
            if (_audioService != null && _transitionSFX != null)
            {
                _audioService.PlaySFX(_transitionSFX, new AudioSettings(volumeOffset: _transitionSFXVolume - 1f));
            }
        }

        private void SpawnAndAnimateCredits(VisualElement root)
        {
            // Limpa qualquer cartão órfão anterior
            foreach (var tag in _spawnedTags)
            {
                tag.parent?.Remove(tag);
            }
            _spawnedTags.Clear();

            float startY = -25f; // Começa acima da tela (25% acima do topo)
            float targetY = 38f; // Posição final (altura no centro-meio da tela)

            for (int i = 0; i < _creditsNames.Length; i++)
            {
                var label = new Label();
                label.text = _creditsNames[i];
                label.AddToClassList("credit-text-only");
                label.AddToClassList("grape-soda-font");
                label.style.whiteSpace = WhiteSpace.Normal; // Habilita quebra de linha

                // Define a posição horizontal (distribuídos entre 15% e 85% da tela)
                float percentLeft = 15f + i * (70f / (_creditsNames.Length - 1));
                label.style.left = Length.Percent(percentLeft);
                label.style.top = Length.Percent(startY);

                root.Add(label);
                _spawnedTags.Add(label);

                // Animação de queda com física de Bounce (quicando)
                var currentLabel = label;
                float delay = i * 0.15f; // Efeito cascata

                DOTween.To(() => currentLabel.style.top.value.value, y => currentLabel.style.top = Length.Percent(y), targetY, 1.1f)
                    .SetEase(Ease.OutBounce)
                    .SetDelay(delay);

                // Interatividade ao clicar: dá um "pulo" e quica de novo
                bool isJumping = false;
                currentLabel.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (isJumping) return;
                    isJumping = true;

                    // Toca o áudio do pulo do card
                    PlayJumpSound();

                    // Interrompe o movimento atual daquela caixa específica
                    DOTween.Kill(currentLabel);

                    float jumpHeight = 15f; // Altura do pulo (15% da tela)

                    DOTween.Sequence()
                        .Append(DOTween.To(() => currentLabel.style.top.value.value, y => currentLabel.style.top = Length.Percent(y), targetY - jumpHeight, 0.3f).SetEase(Ease.OutQuad))
                        .Append(DOTween.To(() => currentLabel.style.top.value.value, y => currentLabel.style.top = Length.Percent(y), targetY, 0.5f).SetEase(Ease.OutBounce))
                        .OnComplete(() => isJumping = false);
                });
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

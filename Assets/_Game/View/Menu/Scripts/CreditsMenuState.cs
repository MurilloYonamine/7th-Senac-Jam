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
        private bool _allowHoverSound;

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

            _btnBack = root.Q<Button>("BtnBack");
            if (_btnBack != null)
            {
                _btnBack.clicked += OnBackClicked;
            }

            _allowHoverSound = false;
            root.Query<Button>(className: "menu-button").ForEach(RegisterButtonEvents);

            PlayTransitionSound();

            FadeIn(() =>
            {
                _btnBack?.Focus();
                _allowHoverSound = true;
            });

            SpawnAndAnimateCredits(root);
        }

        private void OnDisable()
        {
            _fadeTween?.Kill();

            foreach (var tag in _spawnedTags)
            {
                DOTween.Kill(tag);
            }

            if (_btnBack != null)
            {
                _btnBack.clicked -= OnBackClicked;
            }

            if (_uiDocument != null)
            {
                var root = _uiDocument.rootVisualElement;
                if (root != null)
                {
                    root.Query<Button>(className: "menu-button").ForEach(UnregisterButtonEvents);
                }
            }
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
            foreach (var tag in _spawnedTags)
            {
                tag.parent?.Remove(tag);
            }
            _spawnedTags.Clear();

            float startY = -25f;
            float targetY = 38f;

            for (int i = 0; i < _creditsNames.Length; i++)
            {
                var label = new Label();
                label.text = _creditsNames[i];
                label.AddToClassList("credit-text-only");
                label.AddToClassList("grape-soda-font");
                label.style.whiteSpace = WhiteSpace.Normal; 

                float percentLeft = 15f + i * (70f / (_creditsNames.Length - 1));
                label.style.left = Length.Percent(percentLeft);
                label.style.top = Length.Percent(startY);

                root.Add(label);
                _spawnedTags.Add(label);

                var currentLabel = label;
                float delay = i * 0.15f;

                DOTween.To(() => currentLabel.style.top.value.value, y => currentLabel.style.top = Length.Percent(y), targetY, 1.1f)
                    .SetEase(Ease.OutBounce)
                    .SetDelay(delay);
                bool isJumping = false;
                currentLabel.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (isJumping) return;
                    isJumping = true;

                    PlayJumpSound();

                    DOTween.Kill(currentLabel);

                    float jumpHeight = 15f;

                    DOTween.Sequence()
                        .Append(DOTween.To(() => currentLabel.style.top.value.value, y => currentLabel.style.top = Length.Percent(y), targetY - jumpHeight, 0.3f)
                        .SetEase(Ease.OutQuad))
                        .Append(DOTween.To(() => currentLabel.style.top.value.value, y => currentLabel.style.top = Length.Percent(y), targetY, 0.5f)
                        .SetEase(Ease.OutBounce))
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
    }
}

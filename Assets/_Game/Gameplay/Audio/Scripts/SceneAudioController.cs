using UnityEngine;
using Seventh.Core.Services;
using Seventh.Core.Events;
using Seventh.Core.Constants;
using AudioSettings = Seventh.Core.Services.AudioSettings;

namespace Seventh.Gameplay.Audio
{
    public class SceneAudioController : MonoBehaviour
    {
        [Header("Music Settings")]
        [SerializeField] private AudioClip _musicTrack;
        [Range(0f, 1f)]
        [SerializeField] private float _musicVolume = 1f;

        [Header("Ambient Settings")]
        [SerializeField] private AudioClip _ambientTrack;
        [Range(0f, 1f)]
        [SerializeField] private float _ambientVolume = 1f;

        private IAudioService _audioService;
        private IEventBus _eventBus;
        private bool _isAudioPlaying = false;

        private void Start()
        {
            _audioService = ServiceLocator.Get<IAudioService>();

            if (_audioService == null)
            {
                Debug.LogWarning("<color=red><b>[SceneAudioController]</b></color> IAudioService não pôde ser encontrado no ServiceLocator.");
                return;
            }

            PlaySceneAudio();
        }

        private void OnEnable()
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
            if (_eventBus != null)
            {
                _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            }
        }

        private void OnDisable()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            }
        }

        private void OnDestroy()
        {
            StopSceneAudio();
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.CurrentState == GameState.Cutscene)
            {
                StopSceneAudio();
            }
            else if (evt.CurrentState == GameState.Playing && evt.PreviousState == GameState.Cutscene)
            {
                PlaySceneAudio();
            }
        }

        private void PlaySceneAudio()
        {
            if (_audioService == null) return;

            if (_musicTrack != null)
            {
                var musicSettings = new AudioSettings(
                    pitch: 1f,
                    volumeOffset: _musicVolume - 1f,
                    loop: true
                );
                _audioService.PlayTrack(_musicTrack, musicSettings);
            }

            if (_ambientTrack != null)
            {
                var ambientSettings = new AudioSettings(
                    pitch: 1f,
                    volumeOffset: _ambientVolume - 1f,
                    loop: true
                );
                _audioService.PlayAmbient(_ambientTrack, ambientSettings);
            }

            _isAudioPlaying = true;
        }

        private void StopSceneAudio()
        {
            if (_audioService == null || !_isAudioPlaying) return;

            if (_musicTrack != null)
            {
                _audioService.StopTrack(_musicTrack);
            }

            if (_ambientTrack != null)
            {
                _audioService.StopAmbient(_ambientTrack);
            }

            _isAudioPlaying = false;
        }
    }
}

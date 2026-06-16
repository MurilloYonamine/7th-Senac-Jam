using UnityEngine;
using Seventh.Core.Services;
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

        private void PlaySceneAudio()
        {
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
        }
    }
}

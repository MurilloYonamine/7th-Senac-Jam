using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using Seventh.Core.Services;
using AudioSettings = Seventh.Core.Services.AudioSettings;

namespace Seventh.Core.Audio
{
    public class AudioService : MonoBehaviour, IAudioService
    {
        [Header("Audio Mixer Config")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _ambientGroup;

        [Header("Settings Config")]
        [SerializeField] private int _initialSfxPoolSize = 10;
        [SerializeField] private float _musicCrossfadeDuration = 1.0f;
        [SerializeField] private float _ambientFadeDuration = 0.5f;

        // Paths inside Resources folder
        private const string MusicPath = "Audio/Music/";
        private const string SfxPath = "Audio/SFX/";
        private const string AmbientPath = "Audio/Ambient/";

        // Audio Sources
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private bool _isUsingSourceA = true;
        private AudioSource _ambientSource;
        private List<AudioSource> _sfxPool = new List<AudioSource>();

        // Coroutines
        private Coroutine _musicCrossfadeRoutine;
        private Coroutine _ambientFadeRoutine;

        // Properties (IAudioService)
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public float AmbientVolume { get; private set; } = 1f;

        private void Awake()
        {
            ServiceLocator.Register<IAudioService>(this);

            // Initialize AudioSources
            _musicSourceA = CreateAudioSource("MusicSource_A", _musicGroup);
            _musicSourceB = CreateAudioSource("MusicSource_B", _musicGroup);
            _ambientSource = CreateAudioSource("AmbientSource", _ambientGroup);

            // Pre-warm the SFX pool
            for (int i = 0; i < _initialSfxPoolSize; i++)
            {
                GetAvailableSFXSource();
            }

            // Load saved volumes
            LoadVolumes();
        }

        // ============ HELPER INITIALIZERS ============

        private AudioSource CreateAudioSource(string sourceName, AudioMixerGroup group)
        {
            GameObject go = new GameObject(sourceName);
            go.transform.SetParent(this.transform);
            AudioSource source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = group;
            source.playOnAwake = false;
            return source;
        }

        private AudioSource GetAvailableSFXSource()
        {
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                if (_sfxPool[i] != null && !_sfxPool[i].isPlaying)
                {
                    return _sfxPool[i];
                }
            }

            // Instantiate a new source if all are playing
            AudioSource newSource = CreateAudioSource($"SFXSource_{_sfxPool.Count}", _sfxGroup);
            _sfxPool.Add(newSource);
            return newSource;
        }

        private AudioClip LoadClip(string basePath, string clipName)
        {
            string fullPath = basePath + clipName;
            AudioClip clip = Resources.Load<AudioClip>(fullPath);
            if (clip == null)
            {
                Debug.LogWarning($"<b><color=#FF5555>[AudioService]</color></b> AudioClip não encontrado no caminho: <b>Resources/{fullPath}</b>");
            }
            return clip;
        }

        private AudioSettings ResolveDefaultSettings(AudioSettings settings, bool defaultLoop)
        {
            // If settings.Pitch is 0, it means default(AudioSettings) was used.
            // We initialize it with sensible default values.
            if (settings.Pitch == 0f)
            {
                settings.Pitch = 1f;
                settings.Loop = defaultLoop;
            }
            return settings;
        }

        // ============ TRACK (MUSIC) ============

        public void PlayTrack(string trackName, AudioSettings settings = default(AudioSettings))
        {
            AudioClip clip = LoadClip(MusicPath, trackName);
            if (clip != null)
            {
                PlayTrack(clip, settings);
            }
        }

        public void PlayTrack(AudioClip clip, AudioSettings settings = default(AudioSettings))
        {
            if (clip == null) return;

            settings = ResolveDefaultSettings(settings, defaultLoop: true);

            AudioSource activeSource = _isUsingSourceA ? _musicSourceA : _musicSourceB;
            AudioSource targetSource = _isUsingSourceA ? _musicSourceB : _musicSourceA;

            // If the same clip is already playing on the active source, just keep it playing
            if (activeSource.clip == clip && activeSource.isPlaying)
            {
                activeSource.loop = settings.Loop;
                activeSource.pitch = settings.Pitch;
                activeSource.volume = 1f + settings.VolumeOffset;
                return;
            }

            _isUsingSourceA = !_isUsingSourceA;

            targetSource.clip = clip;
            targetSource.loop = settings.Loop;
            targetSource.pitch = settings.Pitch;
            
            float targetVolume = Mathf.Clamp01(1f + settings.VolumeOffset);

            if (_musicCrossfadeRoutine != null)
            {
                StopCoroutine(_musicCrossfadeRoutine);
            }
            _musicCrossfadeRoutine = StartCoroutine(MusicCrossfadeRoutine(activeSource, targetSource, targetVolume, _musicCrossfadeDuration));
        }

        public void StopTrack(string trackName)
        {
            AudioSource activeSource = _isUsingSourceA ? _musicSourceA : _musicSourceB;
            if (activeSource.clip != null && activeSource.clip.name == trackName)
            {
                StopTrack(activeSource.clip);
            }
        }

        public void StopTrack(AudioClip clip)
        {
            if (clip == null) return;

            AudioSource activeSource = _isUsingSourceA ? _musicSourceA : _musicSourceB;
            if (activeSource.clip == clip && activeSource.isPlaying)
            {
                if (_musicCrossfadeRoutine != null)
                {
                    StopCoroutine(_musicCrossfadeRoutine);
                }
                _musicCrossfadeRoutine = StartCoroutine(FadeOutAndStopRoutine(activeSource, _musicCrossfadeDuration));
            }
        }

        private IEnumerator MusicCrossfadeRoutine(AudioSource activeSource, AudioSource targetSource, float targetMaxVolume, float duration)
        {
            targetSource.volume = 0f;
            targetSource.Play();

            float elapsed = 0f;
            float startActiveVolume = activeSource.isPlaying ? activeSource.volume : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (activeSource.isPlaying)
                {
                    activeSource.volume = Mathf.Lerp(startActiveVolume, 0f, t);
                }
                targetSource.volume = Mathf.Lerp(0f, targetMaxVolume, t);

                yield return null;
            }

            activeSource.volume = 0f;
            activeSource.Stop();
            targetSource.volume = targetMaxVolume;
            _musicCrossfadeRoutine = null;
        }

        // ============ SFX ============

        public void PlaySFX(string sfxName, AudioSettings settings = default(AudioSettings))
        {
            AudioClip clip = LoadClip(SfxPath, sfxName);
            if (clip != null)
            {
                PlaySFX(clip, settings);
            }
        }

        public void PlaySFX(AudioClip clip, AudioSettings settings = default(AudioSettings))
        {
            if (clip == null) return;

            settings = ResolveDefaultSettings(settings, defaultLoop: false);
            AudioSource source = GetAvailableSFXSource();

            source.clip = clip;
            source.loop = settings.Loop;
            source.pitch = settings.Pitch;
            source.volume = Mathf.Clamp01(1f + settings.VolumeOffset);

            // Handle 3D / Spatial Position
            if (settings.SpatialPosition.HasValue)
            {
                source.spatialBlend = 1f; // 3D Spatial
                source.transform.position = settings.SpatialPosition.Value;
            }
            else
            {
                source.spatialBlend = 0f; // 2D flat
            }

            source.Play();
        }

        public void StopSFX(string sfxName)
        {
            foreach (var source in _sfxPool)
            {
                if (source.isPlaying && source.clip != null && source.clip.name == sfxName)
                {
                    source.Stop();
                }
            }
        }

        public void StopSFX(AudioClip clip)
        {
            if (clip == null) return;
            foreach (var source in _sfxPool)
            {
                if (source.isPlaying && source.clip == clip)
                {
                    source.Stop();
                }
            }
        }

        // ============ AMBIENT ============

        public void PlayAmbient(string ambientName, AudioSettings settings = default(AudioSettings))
        {
            AudioClip clip = LoadClip(AmbientPath, ambientName);
            if (clip != null)
            {
                PlayAmbient(clip, settings);
            }
        }

        public void PlayAmbient(AudioClip clip, AudioSettings settings = default(AudioSettings))
        {
            if (clip == null) return;

            settings = ResolveDefaultSettings(settings, defaultLoop: true);

            // Keep ambient playing if it's already running
            if (_ambientSource.clip == clip && _ambientSource.isPlaying)
            {
                _ambientSource.loop = settings.Loop;
                _ambientSource.pitch = settings.Pitch;
                _ambientSource.volume = 1f + settings.VolumeOffset;
                return;
            }

            if (_ambientFadeRoutine != null)
            {
                StopCoroutine(_ambientFadeRoutine);
            }

            float targetVolume = Mathf.Clamp01(1f + settings.VolumeOffset);
            _ambientFadeRoutine = StartCoroutine(AmbientTransitionRoutine(clip, settings.Loop, settings.Pitch, targetVolume, _ambientFadeDuration));
        }

        public void StopAmbient(string ambientName)
        {
            if (_ambientSource.clip != null && _ambientSource.clip.name == ambientName)
            {
                StopAmbient(_ambientSource.clip);
            }
        }

        public void StopAmbient(AudioClip clip)
        {
            if (clip == null) return;

            if (_ambientSource.clip == clip && _ambientSource.isPlaying)
            {
                if (_ambientFadeRoutine != null)
                {
                    StopCoroutine(_ambientFadeRoutine);
                }
                _ambientFadeRoutine = StartCoroutine(FadeOutAndStopRoutine(_ambientSource, _ambientFadeDuration));
            }
        }

        private IEnumerator AmbientTransitionRoutine(AudioClip newClip, bool loop, float pitch, float targetVolume, float duration)
        {
            // Fade out current
            if (_ambientSource.isPlaying)
            {
                float startVolume = _ambientSource.volume;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _ambientSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                    yield return null;
                }
                _ambientSource.Stop();
            }

            // Set new and fade in
            _ambientSource.clip = newClip;
            _ambientSource.loop = loop;
            _ambientSource.pitch = pitch;
            _ambientSource.volume = 0f;
            _ambientSource.Play();

            float elapsedIn = 0f;
            while (elapsedIn < duration)
            {
                elapsedIn += Time.deltaTime;
                _ambientSource.volume = Mathf.Lerp(0f, targetVolume, elapsedIn / duration);
                yield return null;
            }

            _ambientSource.volume = targetVolume;
            _ambientFadeRoutine = null;
        }

        private IEnumerator FadeOutAndStopRoutine(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
        }

        // ============ VOLUME CONTROL ============

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            ApplyVolumeToMixer(IAudioService.MUSIC_MIXER_PARAMETER, MusicVolume);
            PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
            PlayerPrefs.Save();
        }

        public void SetSFXVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            ApplyVolumeToMixer(IAudioService.SFX_MIXER_PARAMETER, SfxVolume);
            PlayerPrefs.SetFloat("SFXVolume", SfxVolume);
            PlayerPrefs.Save();
        }

        public void SetAmbientVolume(float volume)
        {
            AmbientVolume = Mathf.Clamp01(volume);
            ApplyVolumeToMixer(IAudioService.AMBIENT_MIXER_PARAMETER, AmbientVolume);
            PlayerPrefs.SetFloat("AmbientVolume", AmbientVolume);
            PlayerPrefs.Save();
        }

        private void ApplyVolumeToMixer(string parameterName, float volumeValue)
        {
            if (_audioMixer == null) return;

            // Convert linear 0..1 to decibels -80..20
            float decibels = -80f;
            if (volumeValue > 0.0001f)
            {
                decibels = Mathf.Log10(volumeValue) * 20f;
            }
            _audioMixer.SetFloat(parameterName, decibels);
        }

        private void LoadVolumes()
        {
            MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            SfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            AmbientVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);

            ApplyVolumeToMixer(IAudioService.MUSIC_MIXER_PARAMETER, MusicVolume);
            ApplyVolumeToMixer(IAudioService.SFX_MIXER_PARAMETER, SfxVolume);
            ApplyVolumeToMixer(IAudioService.AMBIENT_MIXER_PARAMETER, AmbientVolume);
        }
    }
}

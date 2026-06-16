using UnityEngine;

namespace Seventh.Core.Services
{
    public interface IAudioService
    {
        // ============ VOLUMES ============
        float MusicVolume { get; }
        float SfxVolume { get; }
        float AmbientVolume { get; }

        // ============ MIXER PARAMETERS ============
        const string MUSIC_MIXER_PARAMETER = "MusicVolume";
        const string SFX_MIXER_PARAMETER = "SFXVolume";
        const string AMBIENT_MIXER_PARAMETER = "AmbientVolume";

        // ============ TRACK ============
        void PlayTrack(string trackName, AudioSettings settings = default(AudioSettings));
        void PlayTrack(AudioClip clip, AudioSettings settings = default(AudioSettings));
        void StopTrack(string trackName);
        void StopTrack(AudioClip clip);

        // ============ SFX ============
        void PlaySFX(string sfxName, AudioSettings settings = default(AudioSettings));
        void PlaySFX(AudioClip clip, AudioSettings settings = default(AudioSettings));
        void StopSFX(string sfxName);
        void StopSFX(AudioClip clip);

        // ============ AMBIENT ============
        void PlayAmbient(string ambientName, AudioSettings settings = default(AudioSettings));
        void PlayAmbient(AudioClip clip, AudioSettings settings = default(AudioSettings));
        void StopAmbient(string ambientName);
        void StopAmbient(AudioClip clip);

        // ============ VOLUME CONTROL ============
        void SetMusicVolume(float volume);
        void SetSFXVolume(float volume);
        void SetAmbientVolume(float volume);
    }

    public struct AudioSettings
    {
        public float Pitch; // Valor de pitch para a reprodução do áudio
        public float VolumeOffset; // ajuste relativo aplicado sobre o volume base de um som, em vez de definir um valor absoluto.
        public bool Loop; // Indica se o áudio deve ser reproduzido em loop
        public Vector3? SpatialPosition; // Posição no espaço 3D para áudio posicional. Null para 2D.

        public AudioSettings(float pitch = 1f, float volumeOffset = 0f, bool loop = false, Vector3? spatialPosition = null)
        {
            Pitch = pitch;
            VolumeOffset = volumeOffset;
            Loop = loop;
            SpatialPosition = spatialPosition;
        }
    }
}

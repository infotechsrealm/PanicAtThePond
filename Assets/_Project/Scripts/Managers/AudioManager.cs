using System.Collections.Generic;
using UnityEngine;

namespace PanicAtThePond.Managers
{
    /// <summary>
    /// Central audio authority. Gameplay code calls <see cref="PlaySfx"/> / <see cref="PlayMusic"/>
    /// instead of driving <c>AudioSource</c> directly.
    ///
    /// Volume keys intentionally match the ones the existing settings UI already writes
    /// (<c>MasterVolume</c>, <c>MusicVolume</c>, <c>SFXVolume</c>), so this manager and the current
    /// <c>GS</c>/<c>SettingsMenu</c> volume handling stay consistent while call sites migrate over.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public const string MASTER_VOLUME_KEY = "MasterVolume";
        public const string MUSIC_VOLUME_KEY = "MusicVolume";
        public const string SFX_VOLUME_KEY = "SFXVolume";

        private const float DEFAULT_VOLUME = 1f;
        private const int DEFAULT_SFX_VOICE_COUNT = 8;

        [SerializeField] private bool _keepAliveAcrossScenes = true;
        [SerializeField] private int _sfxVoiceCount = DEFAULT_SFX_VOICE_COUNT;
        [SerializeField] private AudioSource _musicSource;

        private readonly List<AudioSource> _sfxVoices = new List<AudioSource>();
        private int _nextVoiceIndex;
        private float _masterVolume = DEFAULT_VOLUME;
        private float _musicVolume = DEFAULT_VOLUME;
        private float _sfxVolume = DEFAULT_VOLUME;

        /// <summary>Singleton access point. Null until the manager's <c>Awake</c> has run.</summary>
        public static AudioManager Instance { get; private set; }

        /// <summary>Effective music volume, already multiplied by the master volume.</summary>
        public float EffectiveMusicVolume => _masterVolume * _musicVolume;

        /// <summary>Effective SFX volume, already multiplied by the master volume.</summary>
        public float EffectiveSfxVolume => _masterVolume * _sfxVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_keepAliveAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeSources();
            ReloadVolumesFromPrefs();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Cleanup();
        }

        private void InitializeSources()
        {
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
            }

            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            int voices = Mathf.Max(1, _sfxVoiceCount);
            for (int i = 0; i < voices; i++)
            {
                AudioSource voice = gameObject.AddComponent<AudioSource>();
                voice.loop = false;
                voice.playOnAwake = false;
                _sfxVoices.Add(voice);
            }
        }

        /// <summary>
        /// Re-reads the three volume values from <c>PlayerPrefs</c> and applies them to live sources.
        /// Call this after the settings UI changes a slider.
        /// </summary>
        public void ReloadVolumesFromPrefs()
        {
            _masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_VOLUME);
            _musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_VOLUME);
            _sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_VOLUME);
            ApplyVolumes();
        }

        /// <summary>Plays <paramref name="clip"/> once on the next free SFX voice.</summary>
        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _sfxVoices.Count == 0)
            {
                return;
            }

            AudioSource voice = _sfxVoices[_nextVoiceIndex];
            _nextVoiceIndex = (_nextVoiceIndex + 1) % _sfxVoices.Count;

            voice.volume = EffectiveSfxVolume * Mathf.Clamp01(volumeScale);
            voice.PlayOneShot(clip);
        }

        /// <summary>Starts looping <paramref name="clip"/> as background music.</summary>
        public void PlayMusic(AudioClip clip, bool restartIfSame = false)
        {
            if (clip == null || _musicSource == null)
            {
                return;
            }

            if (!restartIfSame && _musicSource.clip == clip && _musicSource.isPlaying)
            {
                return;
            }

            _musicSource.clip = clip;
            _musicSource.volume = EffectiveMusicVolume;
            _musicSource.Play();
        }

        /// <summary>Stops background music playback.</summary>
        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = EffectiveMusicVolume;
            }

            for (int i = 0; i < _sfxVoices.Count; i++)
            {
                if (_sfxVoices[i] != null)
                {
                    _sfxVoices[i].volume = EffectiveSfxVolume;
                }
            }
        }

        private void Cleanup()
        {
            _sfxVoices.Clear();
            _musicSource = null;
        }
    }
}

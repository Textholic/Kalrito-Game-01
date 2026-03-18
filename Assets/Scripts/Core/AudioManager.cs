// ============================================================
// AudioManager.cs
// 씬 전환에도 유지되는 오디오 관리자.
// BGM 음량 / SFX 음량 PlayerPrefs 영구 저장.
// ============================================================
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    private const string PREFS_BGM = "audio_bgm";
    private const string PREFS_SFX = "audio_sfx";

    public float BgmVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;

    public event System.Action<float> OnBgmVolumeChanged;
    public event System.Action<float> OnSfxVolumeChanged;

    void Awake()
    {
        _bgmSource        = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop   = true;
        _bgmSource.playOnAwake = false;

        _sfxSource        = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop   = false;
        _sfxSource.playOnAwake = false;

        BgmVolume = PlayerPrefs.GetFloat(PREFS_BGM, 0.7f);
        SfxVolume = PlayerPrefs.GetFloat(PREFS_SFX, 0.8f);
        _bgmSource.volume = BgmVolume;
        _sfxSource.volume = SfxVolume;
    }

    // ── BGM ──────────────────────────────────────────────────────────────────
    public void PlayBgm(AudioClip clip)
    {
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void StopBgm()
    {
        _bgmSource.Stop();
        _bgmSource.clip = null;
    }

    public void SetBgmVolume(float vol)
    {
        BgmVolume = Mathf.Clamp01(vol);
        _bgmSource.volume = BgmVolume;
        PlayerPrefs.SetFloat(PREFS_BGM, BgmVolume);
        PlayerPrefs.Save();
        OnBgmVolumeChanged?.Invoke(BgmVolume);
    }

    // ── SFX ──────────────────────────────────────────────────────────────────
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, SfxVolume);
    }

    public void SetSfxVolume(float vol)
    {
        SfxVolume = Mathf.Clamp01(vol);
        _sfxSource.volume = SfxVolume;
        PlayerPrefs.SetFloat(PREFS_SFX, SfxVolume);
        PlayerPrefs.Save();
        OnSfxVolumeChanged?.Invoke(SfxVolume);
    }
}

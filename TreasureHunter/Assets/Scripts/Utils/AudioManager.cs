using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 사운드 매니저 v1.04.0 made by JJSmith (Curookie)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{

    [SerializeField]
    [TextArea(5, 10)]
    string _사용법 =
@"1) Playlist에 재생할 배경음/효과음들을 넣는다. (Resources, URL 지원)
2) 어떤 스크립트에서든 재생하려면 AudioManager.Inst.PlayBGM(""클립이름""); AudioManager.Inst.PlaySFX(""클립이름""); (반복재생, OneShot 등 지원)
3) 배경음은 페이드 없음(Swift), 페이드 아웃/인(LinearFade), 크로스 페이드(CrossFade) 3가지 페이드 설정이 있다. (playback 쓰려면 Swift로 해야함)
4) PlayerPrefabs으로 설정을 저장하며 토글 속성 있다. ex)IsMusicOn 배경음, IsSoundOn 효과음";


    [Header("배경음 설정")]

    [Tooltip("배경음 On/Off")]
    [SerializeField] bool _musicOn = true;

    [Tooltip("배경음 볼륨")]
    [Range(0, 1)]
    [SerializeField] float _musicVolume = 1.0f;

    [Tooltip("시작 시 배경음 사용여부")]
    [SerializeField] bool _useMusicVolOnStart = false;

    [Tooltip("Target Group 배경음 신호를 위한 설정, 사용 안 할경우 비워놓으면 됨.")]
    [SerializeField] AudioMixerGroup _musicMixerGroup = null;

    [Tooltip("배경음 볼륨믹서 명")]
    [SerializeField] string _volumeOfMusicMixer = string.Empty;

    [Space(3)]

    [Header("효과음 설정")]

    [Tooltip("효과음 On/Off")]
    [SerializeField] bool _soundFxOn = true;

    [Tooltip("효과음 볼륨")]
    [Range(0, 1)]
    [SerializeField] float _soundFxVolume = 1f;

    [Tooltip("시작 시 효과음 사용여부")]
    [SerializeField] bool _useSfxVolOnStart = false;

    [Tooltip("Target Group 효과음 신호를 위한 설정, 사용 안 할경우 비워놓으면 됨.")]
    [SerializeField] AudioMixerGroup _soundFxMixerGroup = null;

    [Tooltip("효과음 볼륨믹서 명")]
    [SerializeField] string _volumeOfSFXMixer = string.Empty;

    [Space(3)]

    [Tooltip("모든 오디오 클립은 여기에 넣으면 됨.")]
    [SerializeField] List<AudioClip> _playlist = new List<AudioClip>();

    // 효과음 풀링을 위한 리스트
    List<SoundEffect> sfxPool = new List<SoundEffect>();
    // 오디오 매니저 배경음
    static BackgroundMusic backgroundMusic;
    // 현재 오디오소스와 페이드를 위한 다음 오디오소스
    static AudioSource musicSource = null, crossfadeSource = null;
    // 현재 볼륨들과 제한 수치용 변수
    static float currentMusicVol = 0, currentSfxVol = 0, musicVolCap = 0, savedPitch = 1f;
    // On/Off 변수
    static bool musicOn = false, sfxOn = false;
    // 전환시간 변수
    static float transitionTime;

    // PlayerPrefabs 저장을 위한 키
    static readonly string BgMusicVolKey = "BGMVol";
    static readonly string SoundFxVolKey = "SFXVol";
    static readonly string BgMusicMuteKey = "BGMMute";
    static readonly string SoundFxMuteKey = "SFXMute";

    // 유일한 인스턴스 변수
    private static AudioManager inst;
    // 앱 켜졌는지 여부용
    private static bool alive = true;

    /// <summary>
    /// 속성 싱글톤 패턴으로 구현
    /// </summary>
    public static AudioManager Inst
    {
        get
        {
            // 앱이 꺼젔거나 Destroy됬는지 체크
            if (!alive)
            {
                Debug.LogWarning(typeof(AudioManager) + "' is already destroyed on application quit.");
                return null;
            }

            //C# 2.0 Null 병합연산자
            return inst ?? FindObjectOfType<AudioManager>();
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        SaveAllPreferences();
    }

    void OnApplicationExit()
    {
        alive = false;
    }

    /// <summary>
    /// 오디오매니저 초기화 함수
    /// </summary>
    void Initialise()
    {
        gameObject.name = "AudioManager";

        // PlayerPrefs에서 값 가져오기
        //_musicOn = LoadBGMMuteStatus();
        //_soundFxOn = LoadSFXMuteStatus();
        //_soundFxVolume = _useSfxVolOnStart ? _soundFxVolume : LoadSFXVolume();
        _musicVolume = 0.6f;

        // 기존 오디오소스 컴포넌트 장착
        if (musicSource == null)
        {
            musicSource = gameObject.GetComponent<AudioSource>();
            // 오디오소스 컴포넌트 없으면 생성해서 부착
            musicSource = musicSource ?? gameObject.AddComponent<AudioSource>();
        }

        musicSource = ConfigureAudioSource(musicSource);

        // 씬 전환시에도 파괴되지 않도록 설정
        DontDestroyOnLoad(this.gameObject);
    }

    void Awake()
    {
        if (inst == null)
        {
            inst = this;
            Initialise();
        }
        else if (inst != this)
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        if (musicSource != null)
        {
            StartCoroutine(OnUpdate());
        }
    }

    /// <summary>
    /// 내부 설정에 기반해서 2D용 오디오소스 생성하는 함수
    /// </summary>
    /// <returns>An AudioSource with 2D features</returns>
    AudioSource ConfigureAudioSource(AudioSource audioSource)
    {
        audioSource.outputAudioMixerGroup = _musicMixerGroup;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;   //2D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.loop = true;
        // PlayerPrefs에서 값 가져오기
        audioSource.volume = LoadBGMVolume();
        audioSource.mute = !_musicOn;

        return audioSource;
    }

    /// <summary>
    /// 효과음 풀에 있는 효과음을 관리하는 함수  
    /// OnUpdate함수에서 불러온다.
    /// </summary>
    private void ManageSoundEffects()
    {
        for (int i = sfxPool.Count - 1; i >= 0; i--)
        {
            SoundEffect sfx = sfxPool[i];
            // 재생 중
            if (sfx.Source.isPlaying && !float.IsPositiveInfinity(sfx.Time))
            {
                sfx.Time -= Time.deltaTime;
                sfxPool[i] = sfx;
            }

            // 끝났을 때
            if (sfxPool[i].Time <= 0.0001f || HasPossiblyFinished(sfxPool[i]))
            {
                sfxPool[i].Source.Stop();
                // 콜백함수 실행
                if (sfxPool[i].Callback != null)
                {
                    sfxPool[i].Callback.Invoke();
                }

                // 클립 제거 후
                Destroy(sfxPool[i].gameObject);

                // 풀에서 항목빼기
                sfxPool.RemoveAt(i);
                break;
            }
        }
    }

    // 완전히 끝났는 지 체크용 함수
    bool HasPossiblyFinished(SoundEffect soundEffect)
    {
        return !soundEffect.Source.isPlaying && FloatEquals(soundEffect.PlaybackPosition, 0) && soundEffect.Time <= 0.09f;
    }

    bool FloatEquals(float num1, float num2, float threshold = .0001f)
    {
        return Math.Abs(num1 - num2) < threshold;
    }

    /// <summary>
    /// 배경음 볼륨 상태가 변했는지 체크하는 함수
    /// </summary>
    private bool IsMusicAltered()
    {
        bool flag = musicOn != _musicOn || musicOn != !musicSource.mute || !FloatEquals(currentMusicVol, _musicVolume);

        // 믹서 그룹을 사용할 경우
        if (_musicMixerGroup != null && !string.IsNullOrEmpty(_volumeOfMusicMixer.Trim()))
        {
            float vol;
            _musicMixerGroup.audioMixer.GetFloat(_volumeOfMusicMixer, out vol);
            vol = NormaliseVolume(vol);

            return flag || !FloatEquals(currentMusicVol, vol);
        }

        return flag;
    }

    /// <summary>
    /// 효과음 볼륨 상태가 변했는지 체크하는 함수
    /// </summary>
    private bool IsSoundFxAltered()
    {
        bool flag = _soundFxOn != sfxOn || !FloatEquals(currentSfxVol, _soundFxVolume);

        // 믹서 그룹을 사용할 경우
        if (_soundFxMixerGroup != null && !string.IsNullOrEmpty(_volumeOfSFXMixer.Trim()))
        {
            float vol;
            _soundFxMixerGroup.audioMixer.GetFloat(_volumeOfSFXMixer, out vol);
            vol = NormaliseVolume(vol);

            return flag || !FloatEquals(currentSfxVol, vol);
        }

        return flag;
    }

    /// <summary>
    /// 크로스 페이드 인 아웃 함수
    /// </summary>
    private void CrossFadeBackgroundMusic()
    {
        if (backgroundMusic.MusicTransition == MusicTransition.CrossFade)
        {
            // 전환이 진행중일 경우
            if (musicSource.clip.name != backgroundMusic.NextClip.name)
            {
                transitionTime -= Time.deltaTime;

                musicSource.volume = Mathf.Lerp(0, musicVolCap, transitionTime / backgroundMusic.TransitionDuration);

                crossfadeSource.volume = Mathf.Clamp01(musicVolCap - musicSource.volume);
                crossfadeSource.mute = musicSource.mute;

                if (musicSource.volume <= 0.00f)
                {
                    SetBGMVolume(musicVolCap);
                    PlayBackgroundMusic(backgroundMusic.NextClip, crossfadeSource.time, crossfadeSource.pitch);
                }
            }
        }
    }

    /// <summary>
    /// 페이드 인/아웃 함수
    /// </summary>
    private void FadeOutFadeInBackgroundMusic()
    {
        if (backgroundMusic.MusicTransition == MusicTransition.LinearFade)
        {
            // 페이드 인
            if (musicSource.clip.name == backgroundMusic.NextClip.name)
            {
                transitionTime += Time.deltaTime;

                musicSource.volume = Mathf.Lerp(0, musicVolCap, transitionTime / backgroundMusic.TransitionDuration);

                if (musicSource.volume >= musicVolCap)
                {
                    SetBGMVolume(musicVolCap);
                    PlayBackgroundMusic(backgroundMusic.NextClip, musicSource.time, savedPitch);
                }
            }
            // 페이드 아웃
            else
            {
                transitionTime -= Time.deltaTime;

                musicSource.volume = Mathf.Lerp(0, musicVolCap, transitionTime / backgroundMusic.TransitionDuration);

                // 페이드 아웃 끝나는 시점 페이드 인 시작
                if (musicSource.volume <= 0.00f)
                {
                    musicSource.volume = transitionTime = 0;
                    PlayMusicFromSource(ref musicSource, backgroundMusic.NextClip, 0, musicSource.pitch);
                }
            }
        }
    }

    /// <summary>
    /// 업데이트 함수 용 Enumerator
    /// </summary>
    IEnumerator OnUpdate()
    {
        while (alive)
        {
            ManageSoundEffects();

            // 배경음 볼륨 바뀌었나 체크
            if (IsMusicAltered())
            {
                ToggleBGMMute(!musicOn);

                if (!FloatEquals(currentMusicVol, _musicVolume))
                {
                    currentMusicVol = _musicVolume;
                }

                if (_musicMixerGroup != null && !string.IsNullOrEmpty(_volumeOfMusicMixer))
                {
                    float vol;
                    _musicMixerGroup.audioMixer.GetFloat(_volumeOfMusicMixer, out vol);
                    vol = NormaliseVolume(vol);
                    currentMusicVol = vol;
                }

                SetBGMVolume(currentMusicVol);
            }

            // 효과음 볼륨 바뀌었나 체크
            if (IsSoundFxAltered())
            {
                ToggleSFXMute(!sfxOn);

                if (!FloatEquals(currentSfxVol, _soundFxVolume))
                {
                    currentSfxVol = _soundFxVolume;
                }

                if (_soundFxMixerGroup != null && !string.IsNullOrEmpty(_volumeOfSFXMixer))
                {
                    float vol;
                    _soundFxMixerGroup.audioMixer.GetFloat(_volumeOfSFXMixer, out vol);
                    vol = NormaliseVolume(vol);
                    currentSfxVol = vol;
                }

                SetSFXVolume(currentSfxVol);
            }

            // 크로스 페이드일 경우
            if (crossfadeSource != null)
            {
                CrossFadeBackgroundMusic();

                yield return null;
            }
            else
            {
                // 페이드 인/ 아웃일 경우
                if (backgroundMusic.NextClip != null)
                {
                    FadeOutFadeInBackgroundMusic();

                    yield return null;
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    #region 스트링 처리

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법 </param>
    /// <param name="transition_duration">전환시간</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="playback_position">시작시점</param>
    public void PlayBGM(string clip, MusicTransition transition, float transition_duration, float volume, float pitch, float playback_position = 0)
    {
        PlayBGM(GetClipFromPlaylist(clip), transition, transition_duration, volume, pitch, playback_position);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법</param>
    /// <param name="transition_duration">전환시간</param>
    /// <param name="volume">사운드 크기</param>
    public void PlayBGM(string clip, MusicTransition transition, float transition_duration, float volume)
    {
        PlayBGM(GetClipFromPlaylist(clip), transition, transition_duration, volume, 1f);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법</param>
    /// <param name="transition_duration">전환시간</param>
    public void PlayBGM(string clip, MusicTransition transition, float transition_duration)
    {
        PlayBGM(GetClipFromPlaylist(clip), transition, transition_duration, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법</param>
    public void PlayBGM(string clip, MusicTransition transition)
    {
        PlayBGM(GetClipFromPlaylist(clip), transition, 1f, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 바로 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    public void PlayBGM(string clip)
    {
        PlayBGM(GetClipFromPlaylist(clip), MusicTransition.Swift, 1f, _musicVolume, 1f);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="duration">재생시간</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlaySFX(string clip, Vector2 location, float duration, float volume, bool singleton = false, float pitch = 1f, Action callback = null)
    {
        return PlaySFX(GetClipFromPlaylist(clip), location, duration, volume, singleton, pitch, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="duration">재생시간</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlaySFX(string clip, Vector2 location, float duration, bool singleton = false, Action callback = null)
    {
        return PlaySFX(GetClipFromPlaylist(clip), location, duration, _soundFxVolume, singleton, 1.0f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="duration">재생시간</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlaySFX(string clip, float duration, bool singleton = false, Action callback = null)
    {
        return PlaySFX(GetClipFromPlaylist(clip), Vector2.zero, duration, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlaySFX(string clip, bool singleton = false, Action callback = null)
    {
        var aClip = GetClipFromPlaylist(clip);
        return PlaySFX(aClip, Vector2.zero, aClip.length, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 횟수만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="repeat">클립을 얼마나 반복할지 정한다. 무한은 음수를 입력하면 됨.</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource RepeatSFX(string clip, Vector2 location, int repeat, float volume, bool singleton = false, float pitch = 1f, Action callback = null)
    {
        return RepeatSFX(GetClipFromPlaylist(clip), location, repeat, volume, singleton, pitch, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 횟수만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="repeat">클립을 얼마나 반복할지 정한다. 무한은 음수를 입력하면 됨.</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource RepeatSFX(string clip, Vector2 location, int repeat, bool singleton = false, Action callback = null)
    {
        return RepeatSFX(GetClipFromPlaylist(clip), location, repeat, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 횟수만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="repeat">클립을 얼마나 반복할지 정한다. 무한은 음수를 입력하면 됨.</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource RepeatSFX(string clip, int repeat, bool singleton = false, Action callback = null)
    {
        return RepeatSFX(GetClipFromPlaylist(clip), Vector2.zero, repeat, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayOneShot(string clip, Vector2 location, float volume, float pitch = 1f, Action callback = null)
    {
        return PlayOneShot(GetClipFromPlaylist(clip), location, volume, pitch, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayOneShot(string clip, Vector2 location, Action callback = null)
    {
        return PlayOneShot(GetClipFromPlaylist(clip), location, _soundFxVolume, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayOneShot(string clip, Action callback = null)
    {
        return PlayOneShot(GetClipFromPlaylist(clip), Vector2.zero, _soundFxVolume, 1f, callback);
    }


    # endregion


    /// <summary>
    /// 특정한 오디오소스에서 클립을 재생하는 함수   
    /// </summary>
    /// <param name="audio_source">참조하는 오디오소스/ 채널</param>
    /// <param name="clip">재생할 클립</param>
    /// <param name="playback_position">시작시점</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    private void PlayMusicFromSource(ref AudioSource audio_source, AudioClip clip, float playback_position, float pitch)
    {
        try
        {
            audio_source.clip = clip;
            audio_source.time = playback_position;
            audio_source.pitch = pitch = Mathf.Clamp(pitch, -3f, 3f);
            audio_source.Play();
        }
        catch (NullReferenceException nre)
        {
            Debug.LogError(nre.Message);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// 현재 오디오소스에서 배경음 클립을 재생하는 함수 (내장함수)
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="playback_position">시작시점</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    private void PlayBackgroundMusic(AudioClip clip, float playback_position, float pitch)
    {
        PlayMusicFromSource(ref musicSource, clip, playback_position, pitch);
        // 다음 클립변수에 있는 클립 제거
        backgroundMusic.NextClip = null;
        // 현재 클립변수에 넣어두기
        backgroundMusic.CurrentClip = clip;
        // 크로스페이드에 있는 클립도 비우기
        if (crossfadeSource != null)
        {
            Destroy(crossfadeSource);
            crossfadeSource = null;
        }
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법 </param>
    /// <param name="transition_duration">전환시간</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="playback_position">시작시점</param>
    public void PlayBGM(AudioClip clip, MusicTransition transition, float transition_duration, float volume, float pitch, float playback_position = 0)
    {
        // 요구클립이 없거나 똑같은 클립이면 재생하지 않음.
        if (clip == null)
        {
            return;
        }

        // 첫 번째로 플레이한 음악이거나 전환시간이 0이면 - 전환효과 없는 케이스
        if (backgroundMusic.CurrentClip == null || transition_duration <= 0)
        {
            transition = MusicTransition.Swift;
        }

        // 전환효과 없는 케이스 시작
        if (transition == MusicTransition.Swift)
        {
            PlayBackgroundMusic(clip, playback_position, pitch);
            SetBGMVolume(volume);
        }
        else
        {
            // 전환효과 진행중일 때 막음
            if (backgroundMusic.NextClip != null)
            {
                Debug.LogWarning("Trying to perform a transition on the background music while one is still active");
                return;
            }

            // 전환효과 변수에 전환방법대로 지정, 그 외 변수들도..
            backgroundMusic.MusicTransition = transition;
            transitionTime = backgroundMusic.TransitionDuration = transition_duration;
            musicVolCap = _musicVolume;
            backgroundMusic.NextClip = clip;

            // 크로스페이드 처리
            if (backgroundMusic.MusicTransition == MusicTransition.CrossFade)
            {
                // 전환효과 진행중일 때 막음
                if (crossfadeSource != null)
                {
                    Debug.LogWarning("Trying to perform a transition on the background music while one is still active");
                    return;
                }

                // 크로스페이드 오디오 초기화
                crossfadeSource = ConfigureAudioSource(gameObject.AddComponent<AudioSource>());

                crossfadeSource.volume = Mathf.Clamp01(musicVolCap - currentMusicVol);
                crossfadeSource.priority = 0;

                PlayMusicFromSource(ref crossfadeSource, backgroundMusic.NextClip, 0, pitch);
            }
        }
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법</param>
    /// <param name="transition_duration">전환시간</param>
    /// <param name="volume">사운드 크기</param>
    public void PlayBGM(AudioClip clip, MusicTransition transition, float transition_duration, float volume)
    {
        PlayBGM(clip, transition, transition_duration, volume, 1f);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법</param>
    /// <param name="transition_duration">전환시간</param>
    public void PlayBGM(AudioClip clip, MusicTransition transition, float transition_duration)
    {
        PlayBGM(clip, transition, transition_duration, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법</param>
    public void PlayBGM(AudioClip clip, MusicTransition transition)
    {
        PlayBGM(clip, transition, 1f, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 바로 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    public void PlayBGM(AudioClip clip)
    {
        PlayBGM(clip, MusicTransition.Swift, 1f, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip_path">Resources 폴더에 있는 클립 경로</param>
    /// <param name="transition">전환방법 </param>
    /// <param name="transition_duration">전환시간</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="playback_position">시작시점</param>
    public void PlayBGMFromPath(string clip_path, MusicTransition transition, float transition_duration, float volume, float pitch, float playback_position = 0)
    {
        PlayBGM(LoadClip(clip_path), transition, transition_duration, volume, pitch, playback_position);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip_path">Resources 폴더에 있는 클립 경로</param>
    /// <param name="transition">전환방법 </param>
    /// <param name="transition_duration">전환시간</param>
    /// <param name="volume">사운드 크기</param>
    public void PlayBGMFromPath(string clip_path, MusicTransition transition, float transition_duration, float volume)
    {
        PlayBGM(LoadClip(clip_path), transition, transition_duration, volume, 1f);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip_path">Resources 폴더에 있는 클립 경로</param>
    /// <param name="transition">전환방법 </param>
    /// <param name="transition_duration">전환시간</param>
    public void PlayBGMFromPath(string clip_path, MusicTransition transition, float transition_duration)
    {
        PlayBGM(LoadClip(clip_path), transition, transition_duration, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip_path">Resources 폴더에 있는 클립 경로</param>
    /// <param name="transition">전환방법 </param>
    public void PlayBGMFromPath(string clip_path, MusicTransition transition)
    {
        PlayBGM(LoadClip(clip_path), transition, 1f, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 바로 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip_path">Resources 폴더에 있는 클립 경로</param>
    public void PlayBGMFromPath(string clip_path)
    {
        PlayBGM(LoadClip(clip_path), MusicTransition.Swift, 1f, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 중지
    /// </summary>
    public void StopBGM()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// 배경음 일시정지
    /// </summary>
    public void PauseBGM()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// 배경음 다시재생
    /// </summary>
    public void ResumeBGM()
    {
        if (!musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }

    /// <summary>
    /// 모든 효과음에서 사용되는 내장 기본함수
    /// 효과음에 대한 특정 항목을 초기화함.
    /// </summary>
    /// <param name="audio_clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <returns>Newly created gameobject with sound effect and audio source attached</returns>
    private GameObject CreateSoundFx(AudioClip audio_clip, Vector2 location)
    {
        // 임시 오브젝트
        GameObject host = new GameObject("TempAudio");
        host.transform.position = location;
        host.transform.SetParent(transform);
        host.AddComponent<SoundEffect>();

        // 오디오소스 추가
        AudioSource audioSource = host.AddComponent<AudioSource>() as AudioSource;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        // 믹서 그룹을 사용할 경우
        audioSource.outputAudioMixerGroup = _soundFxMixerGroup;

        audioSource.clip = audio_clip;
        audioSource.mute = !_soundFxOn;

        return host;
    }

    /// <summary>
    /// 효과음이 효과음 풀에 존재하면 인덱스 알려주는 함수
    /// </summary>
    /// <param name="name">효과음 이름</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <returns>Index of sound effect or -1 is none exists</returns>
    public int IndexOfSoundFxPool(string name, bool singleton = false)
    {
        int index = 0;
        while (index < sfxPool.Count)
        {
            if (sfxPool[index].Name == name && singleton == sfxPool[index].Singleton)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="duration">재생시간</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlaySFX(AudioClip clip, Vector2 location, float duration, float volume, bool singleton = false, float pitch = 1f, Action callback = null)
    {
        if (duration <= 0 || clip == null)
        {
            return null;
        }

        int index = IndexOfSoundFxPool(clip.name, true);

        if (index >= 0)
        {
            // 효과음 풀에 존재하면 재생시간 재설정해서 내보냄
            SoundEffect singletonSFx = sfxPool[index];
            singletonSFx.Duration = singletonSFx.Time = duration;
            sfxPool[index] = singletonSFx;

            return sfxPool[index].Source;
        }

        GameObject host = null;
        AudioSource source = null;

        host = CreateSoundFx(clip, location);
        source = host.GetComponent<AudioSource>();
        source.loop = duration > clip.length;
        source.volume = _soundFxVolume * volume;
        source.pitch = pitch;

        // 재사용 가능한 사운드 생성
        SoundEffect sfx = host.GetComponent<SoundEffect>();
        sfx.Singleton = singleton;
        sfx.Source = source;
        sfx.OriginalVolume = volume;
        sfx.Duration = sfx.Time = duration;
        sfx.Callback = callback;

        // 풀에 넣는다.
        sfxPool.Add(sfx);

        source.Play();

        return source;
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="duration">재생시간</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlaySFX(AudioClip clip, Vector2 location, float duration, bool singleton = false, Action callback = null)
    {
        return PlaySFX(clip, location, duration, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="duration">재생시간</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlaySFX(AudioClip clip, float duration, bool singleton = false, Action callback = null)
    {
        return PlaySFX(clip, Vector2.zero, duration, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 횟수만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="repeat">클립을 얼마나 반복할지 정한다. 무한은 음수를 입력하면 됨.</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource RepeatSFX(AudioClip clip, Vector2 location, int repeat, float volume, bool singleton = false, float pitch = 1f, Action callback = null)
    {
        if (clip == null)
        {
            return null;
        }

        if (repeat != 0)
        {
            int index = IndexOfSoundFxPool(clip.name, true);

            if (index >= 0)
            {
                // 효과음 풀에 존재하면 재생시간 재설정해서 내보냄
                SoundEffect singletonSFx = sfxPool[index];
                singletonSFx.Duration = singletonSFx.Time = repeat > 0 ? clip.length * repeat : float.PositiveInfinity;
                sfxPool[index] = singletonSFx;

                return sfxPool[index].Source;
            }

            GameObject host = CreateSoundFx(clip, location);
            AudioSource source = host.GetComponent<AudioSource>();
            source.loop = repeat != 0;
            source.volume = _soundFxVolume * volume;
            source.pitch = pitch;

            // 재사용 가능한 사운드 생성
            SoundEffect sfx = host.GetComponent<SoundEffect>();
            sfx.Singleton = singleton;
            sfx.Source = source;
            sfx.OriginalVolume = volume;
            sfx.Duration = sfx.Time = repeat > 0 ? clip.length * repeat : float.PositiveInfinity;
            sfx.Callback = callback;

            // 풀에 넣는다.
            sfxPool.Add(sfx);

            source.Play();

            return source;
        }

        // repeat 길이가 1보다 작거나 같으면 재생
        return PlayOneShot(clip, location, volume, pitch, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 횟수만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="repeat">클립을 얼마나 반복할지 정한다. 무한은 음수를 입력하면 됨.</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource RepeatSFX(AudioClip clip, Vector2 location, int repeat, bool singleton = false, Action callback = null)
    {
        return RepeatSFX(clip, location, repeat, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 횟수만큼 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="repeat">클립을 얼마나 반복할지 정한다. 무한은 음수를 입력하면 됨.</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource RepeatSFX(AudioClip clip, int repeat, bool singleton = false, Action callback = null)
    {
        return RepeatSFX(clip, Vector2.zero, repeat, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayOneShot(AudioClip clip, Vector2 location, float volume, float pitch = 1f, Action callback = null)
    {
        if (clip == null)
        {
            return null;
        }

        GameObject host = CreateSoundFx(clip, location);
        AudioSource source = host.GetComponent<AudioSource>();
        source.loop = false;
        source.volume = _soundFxVolume * volume;
        source.pitch = pitch;

        // 재사용 가능한 사운드 생성
        SoundEffect sfx = host.GetComponent<SoundEffect>();
        sfx.Singleton = false;
        sfx.Source = source;
        sfx.OriginalVolume = volume;
        sfx.Duration = sfx.Time = clip.length;
        sfx.Callback = callback;

        // 풀에 넣는다.
        sfxPool.Add(sfx);

        source.Play();

        return source;
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayOneShot(AudioClip clip, Vector2 location, Action callback = null)
    {
        return PlayOneShot(clip, location, _soundFxVolume, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayOneShot(AudioClip clip, Action callback = null)
    {
        return PlayOneShot(clip, Vector2.zero, _soundFxVolume, 1f, callback);
    }

    /// <summary>
    /// 모든 효과음을 일시정지
    /// </summary>
    public void PauseAllSFX()
    {
        // SoundEffect 다 돌기
        foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
        {
            if (sfx.Source.isPlaying) sfx.Source.Pause();
        }
    }

    /// <summary>
    /// 모든 효과음을 다시재생
    /// </summary>
    public void ResumeAllSFX()
    {
        foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
        {
            if (!sfx.Source.isPlaying) sfx.Source.UnPause();
        }
    }

    /// <summary>
    /// 모든 효과음을 중지
    /// </summary>
    public void StopAllSFX()
    {
        foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
        {
            if (sfx.Source)
            {
                sfx.Source.Stop();
                Destroy(sfx.gameObject);
            }
        }

        sfxPool.Clear();
    }

    /// <summary>
    /// Resources 폴더에서 오디오 클립을 가져오는 함수
    /// </summary>
    /// <param name="path">Resources 폴더의 클립 경로</param>
    /// <param name="add_to_playlist">로드한 클립을 나중에 참조를 위해서 플레이 리스트에 추가하는 옵션</param>
    /// <returns>The Audioclip from the resource folder</returns>
    public AudioClip LoadClip(string path, bool add_to_playlist = false)
    {
        AudioClip clip = Resources.Load(path) as AudioClip;
        if (clip == null)
        {
            Debug.LogError(string.Format("AudioClip '{0}' not found at location {1}", path, System.IO.Path.Combine(Application.dataPath, "/Resources/" + path)));
            return null;
        }

        if (add_to_playlist)
        {
            AddToPlaylist(clip);
        }

        return clip;
    }

    /// <summary>
    /// URL 경로로 오디오 클립을 가져오는 함수
    /// </summary>
    /// <param name="path">오디오 클립 다운로드 URL. 예: 'http://www.my-server.com/audio.ogg'</param>
    /// <param name="audio_type">다운로드를 위한 오디오 인코딩 타입. AudioType 참고</param>
    /// <param name="add_to_playlist">로드한 클립을 나중에 참조를 위해서 플레이 리스트에 추가하는 옵션</param>
    /// <param name="callback">로드가 완료되면 콜백할 액션.</param>
    public void LoadClip(string path, AudioType audio_type, bool add_to_playlist, Action<AudioClip> callback)
    {
        StartCoroutine(LoadAudioClipFromUrl(path, audio_type, (downloadedContent) => {
            if (downloadedContent != null && add_to_playlist)
            {
                AddToPlaylist(downloadedContent);
            }

            callback.Invoke(downloadedContent);
        }));
    }

    /// <summary>
    /// URL 경로로 오디오 클립 가져오는 내장 함수
    /// </summary>
    /// <returns>The audio clip from URL.</returns>
    /// <param name="audio_url">오디오 URL</param>
    /// <param name="audio_type">오디오 타입</param>
    /// <param name="callback">콜백 액션</param>
    IEnumerator LoadAudioClipFromUrl(string audio_url, AudioType audio_type, Action<AudioClip> callback)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(audio_url, audio_type))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.Log(string.Format("Error downloading audio clip at {0} : {1}", audio_url, www.error));
            }

            callback.Invoke(UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www));
        }
    }

    /// <summary>
    /// 배경음, 효과음 On/Off 토글 함수
    /// </summary>
    /// <param name="flag">On - true, Off - false</param>
    private void ToggleMute(bool flag)
    {
        ToggleBGMMute(flag);
        ToggleSFXMute(flag);
    }

    /// <summary>
    /// 배경음 On/Off 토글 함수
    /// </summary>
    /// <param name="flag">On - true, Off - false</param>
    private void ToggleBGMMute(bool flag)
    {
        musicOn = _musicOn = flag;
        musicSource.mute = !musicOn;
    }

    /// <summary>
    /// 효과음 On/Off 토글 함수
    /// </summary>
    /// <param name="flag">On - true, Off - false</param>
    private void ToggleSFXMute(bool flag)
    {
        sfxOn = _soundFxOn = flag;

        foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
        {
            sfx.Source.mute = !sfxOn;
        }
    }

    /// <summary>
    /// 배경음 사운드 크기조정 함수
    /// </summary>
    /// <param name="volume">New volume of the background music.</param>
    private void SetBGMVolume(float volume)
    {
        try
        {
            volume = Mathf.Clamp01(volume);
            // 모든 사운드 크기 변수에 할당
            musicSource.volume = currentMusicVol = _musicVolume = volume;

            if (_musicMixerGroup != null && !string.IsNullOrEmpty(_volumeOfMusicMixer.Trim()))
            {
                float mixerVol = -80f + (volume * 100f);
                _musicMixerGroup.audioMixer.SetFloat(_volumeOfMusicMixer, mixerVol);
            }
        }
        catch (NullReferenceException nre)
        {
            Debug.LogError(nre.Message);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// 효과음 사운드 크기조정 함수
    /// </summary>
    /// <param name="volume">New volume for all the sound effects.</param>
    private void SetSFXVolume(float volume)
    {
        try
        {
            volume = Mathf.Clamp01(volume);
            currentSfxVol = _soundFxVolume = volume;

            foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect>())
            {
                sfx.Source.volume = _soundFxVolume * sfx.OriginalVolume;
                sfx.Source.mute = !_soundFxOn;
            }

            if (_soundFxMixerGroup != null && !string.IsNullOrEmpty(_volumeOfSFXMixer.Trim()))
            {
                float mixerVol = -80f + (volume * 100f);
                _soundFxMixerGroup.audioMixer.SetFloat(_volumeOfSFXMixer, mixerVol);
            }
        }
        catch (NullReferenceException nre)
        {
            Debug.LogError(nre.Message);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// 오디오 관리자 사운드 크기를 0- 1 로 정규화하는 함수
    /// </summary>
    /// <returns>The normalised volume between the range of zero and one.</returns>
    /// <param name="vol">사운드 크기</param>
    private float NormaliseVolume(float vol)
    {
        vol += 80f;
        vol /= 100f;
        return vol;
    }

    /// <summary>
    /// 배경음 사운드 크기를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns></returns>
    private float LoadBGMVolume()
    {
        return PlayerPrefs.HasKey(BgMusicVolKey) ? PlayerPrefs.GetFloat(BgMusicVolKey) : _musicVolume;
    }

    /// <summary>
    /// 효과음 사운드 크기를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns></returns>
    private float LoadSFXVolume()
    {
        return PlayerPrefs.HasKey(SoundFxVolKey) ? PlayerPrefs.GetFloat(SoundFxVolKey) : _soundFxVolume;
    }

    /// <summary>
    /// int값을 bool값으로 변환하는 함수
    /// </summary>
    private bool ToBool(int integer)
    {
        return integer == 0 ? false : true;
    }

    /// <summary>
    /// 배경음 On/Off 여부를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns>Returns the value of the background music mute key from the saved preferences if it exists or the defaut value if it does not</returns>
    private bool LoadBGMMuteStatus()
    {
        return PlayerPrefs.HasKey(BgMusicMuteKey) ? ToBool(PlayerPrefs.GetInt(BgMusicMuteKey)) : _musicOn;
    }

    /// <summary>
    /// 효과음 On/Off 여부를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns>Returns the value of the sound effect mute key from the saved preferences if it exists or the defaut value if it does not</returns>
    private bool LoadSFXMuteStatus()
    {
        return PlayerPrefs.HasKey(SoundFxMuteKey) ? ToBool(PlayerPrefs.GetInt(SoundFxMuteKey)) : _soundFxOn;
    }

    /// <summary>
    /// 배경음 On/Off 여부와 사운드 크기를 PlayerPrefs에 저장하는 함수
    /// </summary>
    public void SaveBGMPreferences()
    {
        PlayerPrefs.SetInt(BgMusicMuteKey, _musicOn ? 1 : 0);
        PlayerPrefs.SetFloat(BgMusicVolKey, _musicVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 효과음 On/Off 여부와 사운드 크기를 PlayerPrefs에 저장하는 함수
    /// </summary>
    public void SaveSFXPreferences()
    {
        PlayerPrefs.SetInt(SoundFxMuteKey, _soundFxOn ? 1 : 0);
        PlayerPrefs.SetFloat(SoundFxVolKey, _soundFxVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 모든 PlayerPrefs 초기화 하는 함수
    /// </summary>
    public void ClearAllPreferences()
    {
        PlayerPrefs.DeleteKey(BgMusicVolKey);
        PlayerPrefs.DeleteKey(SoundFxVolKey);
        PlayerPrefs.DeleteKey(BgMusicMuteKey);
        PlayerPrefs.DeleteKey(SoundFxMuteKey);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 모든 사운드 옵션을 PlayerPrefs에 저장하는 함수
    /// </summary>
    public void SaveAllPreferences()
    {
        PlayerPrefs.SetFloat(SoundFxVolKey, _soundFxVolume);
        PlayerPrefs.SetFloat(BgMusicVolKey, _musicVolume);
        PlayerPrefs.SetInt(SoundFxMuteKey, _soundFxOn ? 1 : 0);
        PlayerPrefs.SetInt(BgMusicMuteKey, _musicOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 오디오 클립 리스트를 초기화하는 함수
    /// </summary>
    public void EmptyPlaylist()
    {
        _playlist.Clear();
    }

    /// <summary>
    /// 오디오 클립 리스트에 오디오 클립을 추가하는 함수
    /// </summary>
    /// <param name="clip">오디오 클립</param>
    public void AddToPlaylist(AudioClip clip)
    {
        if (clip != null)
        {
            _playlist.Add(clip);
        }
    }

    /// <summary>
    /// 오디오 클립 리스트에 오디오 클립을 제거하는 함수
    /// </summary>
    /// <param name="clip">오디오 클립</param>
    public void RemoveFromPlaylist(AudioClip clip)
    {
        if (clip != null && GetClipFromPlaylist(clip.name))
        {
            _playlist.Remove(clip);
            _playlist.Sort((x, y) => x.name.CompareTo(y.name));
        }
    }

    /// <summary>
    /// 오디오 이름으로 오디오 클립 리스트에서 오디오 클립 가져오는 함수
    /// </summary>
    /// <param name="clip_name">클립 이름</param>
    /// <returns>The AudioClip from the pool or null if no matching name can be found</returns>
    public AudioClip GetClipFromPlaylist(string clip_name)
    {
        for (int i = 0; i < _playlist.Count; i++)
        {
            if (clip_name == _playlist[i].name)
            {
                return _playlist[i];
            }
        }

        Debug.LogWarning(clip_name + " does not exist in the playlist.");
        return null;
    }

    /// <summary>
    /// Resources 폴더 경로에 있는 모든 오디오 클립을 오디오 클립 리스트에 가져오는 함수
    /// </summary>
    /// <param name="path">Resoures 폴더 내 폴더경로 예) "" 입력 시 Resources 내 모든 클립을 가져옴.</param>
    /// <param name="overwrite">덮어씌울지 여부, true - 리스트 덮어씌움, false - 리스트에 연달아서 추가</param>
    public void LoadPlaylist(string path, bool overwrite)
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>(path);

        // 새로운 리스트로 덮어씌울지 체크
        if (clips != null && clips.Length > 0 && overwrite)
        {
            _playlist.Clear();
        }

        for (int i = 0; i < clips.Length; i++)
        {
            _playlist.Add(clips[i]);
        }
    }

    /// <summary>
    /// 현재 배경음 클립을 가져오는 속성
    /// </summary>
    /// <value>The current music clip.</value>
    public AudioClip CurrentMusicClip
    {
        get { return backgroundMusic.CurrentClip; }
    }

    /// <summary>
    /// 효과음 풀을 가져오는 속성
    /// </summary>
    public List<SoundEffect> SoundFxPool
    {
        get { return sfxPool; }
    }

    /// <summary>
    /// 오디오 매니저의 클립 리스트를 가져오는 속성
    /// </summary>
    public List<AudioClip> Playlist
    {
        get { return _playlist; }
    }

    /// <summary>
    /// 배경음이 재생중인지 체크하는 속성
    /// </summary>
    public bool IsMusicPlaying
    {
        get { return musicSource != null && musicSource.isPlaying; }
    }

    /// <summary>
    /// 배경음 사운드 크기를 가져오거나 지정하는 속성
    /// </summary>
    /// <value>사운드 크기</value>
    public float MusicVolume
    {
        get { return _musicVolume; }
        set { SetBGMVolume(value); }
    }

    /// <summary>
    /// 효과음 사운드 크기를 가져오거나 지정하는 속성
    /// </summary>
    /// <value>사운드 크기</value>
    public float SoundVolume
    {
        get { return _soundFxVolume; }
        set { SetSFXVolume(value); }
    }

    /// <summary>
    /// 배경음 On/Off 체크하거나 지정하는 속성
    /// </summary>
    /// <value><c>true</c> - BGM On; <c>false</c> - BGM Off</value>
    public bool IsMusicOn
    {
        get { return _musicOn; }
        set { ToggleBGMMute(value); }
    }

    /// <summary>
    /// 효과음 On/Off 체크하거나 지정하는 속성
    /// </summary>
    /// <value><c>true</c> - SFX On; <c>false</c> - SFX Off</value>
    public bool IsSoundOn
    {
        get { return _soundFxOn; }
        set { ToggleSFXMute(value); }
    }

    /// <summary>
    /// 배경음과 효과음 On/Off 체크하거나 지정하는 속성
    /// </summary>
    /// <value><c>true</c> - BGM+SFX On; <c>false</c> - BGM+SFX Off</value>
    public bool IsMasterMute
    {
        get { return !_musicOn && !_soundFxOn; }
        set { ToggleMute(value); }
    }

}

/// <summary>
/// 전환효과
/// </summary>
public enum MusicTransition
{
    /// <summary>
    /// (없음) 다음음악이 즉시 재생
    /// </summary>
    Swift,
    /// <summary>
    /// (페이드 인/아웃) 페이드 아웃되고 다음 음악 페이드 인
    /// </summary>
    LinearFade,
    /// <summary>
    /// (크로스) 현재음악과 다음음악이 크로스
    /// </summary>
    CrossFade
}

/// <summary>
/// 배경음 설정
/// </summary>
[System.Serializable]
public struct BackgroundMusic
{
    /// <summary>
    /// 배경음 현재 클립
    /// </summary>
    public AudioClip CurrentClip;
    /// <summary>
    /// 배경음 다음 클립
    /// </summary>
    public AudioClip NextClip;
    /// <summary>
    /// 전환효과
    /// </summary>
    public MusicTransition MusicTransition;
    /// <summary>
    /// 전환효과 시간
    /// </summary>
    public float TransitionDuration;
}

/// <summary>
/// 효과음 구조와 설정
/// </summary>
[System.Serializable]
public class SoundEffect : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float originalVolume;
    [SerializeField] private float duration;
    [SerializeField] private float playbackPosition;
    [SerializeField] private float time;
    [SerializeField] private Action callback;
    [SerializeField] private bool singleton;

    /// <summary>
    /// 효과음 이름 속성
    /// </summary>
    /// <value>이름</value>
    public string Name
    {
        get { return audioSource.clip.name; }
    }

    /// <summary>
    /// 효과음 길이 속성 (초 단위)
    /// </summary>
    /// <value>길이</value>
    public float Length
    {
        get { return audioSource.clip.length; }
    }

    /// <summary>
    /// 효과음 재생된 시간 속성 (초 단위)
    /// </summary>
    /// <value>재생된 시간</value>
    public float PlaybackPosition
    {
        get { return audioSource.time; }
    }

    /// <summary>
    /// 효과음 클립 속성
    /// </summary>
    /// <value>오디오 클립</value>
    public AudioSource Source
    {
        get { return audioSource; }
        set { audioSource = value; }
    }

    /// <summary>
    /// 효과음 원본 볼륨 속성
    /// </summary>
    /// <value>원본 사운드 크기</value>
    public float OriginalVolume
    {
        get { return originalVolume; }
        set { originalVolume = value; }
    }

    /// <summary>
    /// 효과음 총 재생시간 속성 (초단위)
    /// </summary>
    /// <value>총 재생시간</value>
    public float Duration
    {
        get { return duration; }
        set { duration = value; }
    }

    /// <summary>
    /// 효과음 남은 재생시간 속성 (초단위)
    /// </summary>
    /// <value>남은 재생시간</value>
    public float Time
    {
        get { return time; }
        set { time = value; }
    }

    /// <summary>
    /// 효과음 정규화된 재생진행도 속성 (정규화 0~1)
    /// </summary>
    /// <value>정규화된 재생진행도</value>
    public float NormalisedTime
    {
        get { return Time / Duration; }
    }

    /// <summary>
    /// 효과음 완료 시 콜백 액션 속성
    /// </summary>
    /// <value>콜백 액션</value>
    public Action Callback
    {
        get { return callback; }
        set { callback = value; }
    }

    /// <summary>
    /// 효과음 반복 시 싱글톤 여부, 반복할 경우에 true 아니면 false
    /// </summary>
    /// <value><c>true</c> 반복 시; 그 외, <c>false</c>.</value>
    public bool Singleton
    {
        get { return singleton; }
        set { singleton = value; }
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM 종류 (bgmClips 리스트의 인덱스와 1:1 대응)
/// </summary>
public enum BGMType
{
    None = -1,
    MainLobby = 0,      // 메인 화면
    GameSelect = 1,     // 게임 선택 화면
    InGame = 2,         // 게임 진행 중 (위기 시 피치 상승)
    Result = 3,         // 게임 종료 후 스코어 화면
}

/// <summary>
/// SFX 종류 (sfxClips 리스트의 인덱스와 1:1 대응)
/// </summary>
public enum SFXType
{
    ButtonClick = 0,    // 버튼 클릭
    ScoreUp = 1,        // 점수 증가 (띠링)
    ScoreDown = 2,      // 점수 감소 (삐-)
    CardFlip = 3,       // [0] 카드 뒤집기
    TrashPick = 4,      // [1] 쓰레기 집기 (꾸깃)
    TrashDrop = 5,      // [1] 쓰레기 넣기 (탱그랑)
    FruitWhoosh = 6,    // [2] 과일 날아오기 (휙)
    FruitSlice = 7,     // [2] 과일 썰기 (촥)
    Whistle = 8,        // 게임 시작 및 종료 시 호루라기
}

/// <summary>
/// 씬 전환에도 파괴되지 않는 간단한 사운드 매니저
/// - BGM : 2채널 크로스페이드
/// - SFX : 1채널 PlayOneShot (겹쳐 재생 가능)
/// 클립은 인스펙터 리스트에 enum 순서대로 넣으면 됩니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM 클립 (0 메인 / 1 게임선택 / 2 게임중 / 3 결과)")]
    [SerializeField] private List<AudioClip> bgmClips = new List<AudioClip>();

    [Header("SFX 클립 (0 버튼 / 1 점수+ / 2 점수- / 3 카드 / 4 쓰레기집기 / 5 쓰레기넣기 / 6 과일휙 / 7 과일촥 / 8 호루라기)")]
    [SerializeField] private List<AudioClip> sfxClips = new List<AudioClip>();

    [Header("볼륨")]
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private AudioSource sfxSource;
    private bool isUsingSourceA = true;

    public BGMType CurrentBGM { get; private set; } = BGMType.None;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            InitAudioSources();
        }
        else
        {
            // 각 씬에 프리팹이 하나씩 배치되어 있으므로, 이미 있으면 새로 생긴 쪽을 제거
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void InitAudioSources()
    {
        bgmSourceA = CreateSource(true);
        bgmSourceB = CreateSource(true);
        sfxSource = CreateSource(false);
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        return src;
    }

    #region BGM 제어

    /// <summary>
    /// BGM을 크로스페이드하며 루프 재생합니다. 같은 BGM이 재생 중이면 무시합니다.
    /// </summary>
    public void PlayBGM(BGMType type, float fadeDuration = 0.5f)
    {
        if (CurrentBGM == type && type != BGMType.None) return;

        AudioClip clip = GetClip(bgmClips, (int)type);
        if (clip == null)
        {
            StopBGM(fadeDuration);
            CurrentBGM = type; // 클립이 비어 있어도 상태는 기록 (중복 호출 방지)
            return;
        }

        CurrentBGM = type;

        AudioSource currentSource = isUsingSourceA ? bgmSourceA : bgmSourceB;
        AudioSource newSource = isUsingSourceA ? bgmSourceB : bgmSourceA;
        DOTweenManager.PitchAudio(newSource, 1f, 0f); // 새 BGM은 항상 원래 속도로 시작
        isUsingSourceA = !isUsingSourceA;

        newSource.Stop();
        newSource.clip = clip;
        DOTweenManager.AudioFadeIn(newSource, bgmVolume, fadeDuration);
        DOTweenManager.AudioFadeOut(currentSource, fadeDuration);
    }

    /// <summary>
    /// 인스펙터(UnityEvent)에서 int로 호출할 때 사용
    /// </summary>
    public void PlayBGMByIndex(int index)
    {
        PlayBGM((BGMType)index);
    }

    public void StopBGM(float fadeDuration = 0.5f)
    {
        CurrentBGM = BGMType.None;
        DOTweenManager.AudioFadeOut(bgmSourceA, fadeDuration);
        DOTweenManager.AudioFadeOut(bgmSourceB, fadeDuration);
    }

    /// <summary>
    /// 현재 재생 중인 BGM의 피치(속도 + 음높이)를 변경합니다.
    /// </summary>
    public void SetBGMPitch(float pitch, float duration = 0.5f)
    {
        AudioSource activeSource = isUsingSourceA ? bgmSourceA : bgmSourceB;
        DOTweenManager.PitchAudio(activeSource, pitch, duration);
    }

    #endregion

    #region SFX 제어

    /// <summary>
    /// 효과음을 1회 재생합니다. (여러 개 겹쳐서 재생 가능)
    /// </summary>
    public void PlaySFX(SFXType type, float volumeScale = 1f)
    {
        AudioClip clip = GetClip(sfxClips, (int)type);
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    /// <summary>
    /// 인스펙터(UnityEvent)에서 int로 호출할 때 사용
    /// </summary>
    public void PlaySFXByIndex(int index)
    {
        PlaySFX((SFXType)index);
    }

    #endregion

    private AudioClip GetClip(List<AudioClip> list, int index)
    {
        if (list == null || index < 0 || index >= list.Count) return null;
        return list[index];
    }

#if UNITY_EDITOR
    // 인스펙터에서 리스트 칸 수가 enum 개수보다 적으면 빈 칸(None)을 자동으로 채워 순서를 맞춰줌
    private void OnValidate()
    {
        int bgmCount = System.Enum.GetValues(typeof(BGMType)).Length - 1; // None 제외
        int sfxCount = System.Enum.GetValues(typeof(SFXType)).Length;
        if (bgmClips == null) bgmClips = new List<AudioClip>();
        if (sfxClips == null) sfxClips = new List<AudioClip>();
        while (bgmClips.Count < bgmCount) bgmClips.Add(null);
        while (sfxClips.Count < sfxCount) sfxClips.Add(null);
    }
#endif
}

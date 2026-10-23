using UnityEngine;

/// <summary>
/// 게임 씬 전용 BGM 컨트롤러
/// GameStateManager의 상태를 보고 BGM을 자동으로 바꿔줍니다. (GameStateManager 코드는 건드리지 않음)
///  - 씬 시작                     : InGame (피치 1.0)
///  - 10초 남음 / 마지막 목숨      : 같은 InGame BGM의 피치(=재생 속도)를 hurryPitch까지 올림
///  - 시간 0 이하 / 목숨 0 이하    : Result
/// </summary>
public class GameBGMController : MonoBehaviour
{
    [Header("참조 (비워두면 GameStateManager.Instance 자동 사용)")]
    [SerializeField] private GameStateManager gameStateManager;

    [Header("긴박 연출 조건")]
    [Tooltip("남은 시간이 이 값 이하가 되면 긴박 연출 (TimeLimit 게임)")]
    [SerializeField] private float hurryTime = 10f;
    [Tooltip("LifeLimit 게임에서 목숨이 1개 남으면 긴박 연출")]
    [SerializeField] private bool hurryOnLastLife = true;

    [Header("긴박 연출 (속도 + 음높이)")]
    [Tooltip("1.1 = 10% 빠르고 약간 높게 / 1.15 = 15% 빠르고 조금 더 높게. 속도와 음높이는 함께 변합니다.")]
    [Range(1f, 1.5f)]
    [SerializeField] private float hurryPitch = 1.12f;
    [Tooltip("피치가 1.0에서 hurryPitch까지 올라가는 시간")]
    [SerializeField] private float pitchChangeDuration = 0.6f;

    [Header("BGM 전환 페이드 시간")]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isHurryPlayed = false;
    private bool isResultPlayed = false;

    private void Start()
    {
        if (gameStateManager == null) gameStateManager = GameStateManager.Instance;
        Play(BGMType.InGame);

        // 같은 InGame BGM이 이어서 재생되는 경우(PlayBGM이 무시됨)에도 피치를 원래대로
        if (SoundManager.Instance != null) SoundManager.Instance.SetBGMPitch(1f, 0f);
    }

    private void Update()
    {
        if (gameStateManager == null || isResultPlayed) return;

        bool isTimeLimit = gameStateManager.gameType == GameType.TimeLimit;
        bool isLifeLimit = gameStateManager.gameType == GameType.LifeLimit;

        // 1. 시간 0 이하 또는 목숨 0 이하 → 게임 종료 BGM
        bool isTimeOver = isTimeLimit && gameStateManager.currentTime <= 0f;
        bool isLifeOver = isLifeLimit && gameStateManager.currentLife <= 0;

        if (gameStateManager.isGameOver || (gameStateManager.isGameStarted && (isTimeOver || isLifeOver)))
        {
            isResultPlayed = true;
            Play(BGMType.Result); // 새 BGM은 SoundManager에서 피치 1.0으로 재생됨
            return;
        }

        // 2. 위기 상황 → 같은 BGM의 속도와 음높이를 올림
        if (isHurryPlayed || !gameStateManager.isGameStarted) return;

        bool isHurry = false;
        if (isTimeLimit) isHurry = gameStateManager.currentTime <= hurryTime;
        else if (isLifeLimit && hurryOnLastLife) isHurry = gameStateManager.currentLife == 1;

        if (isHurry)
        {
            isHurryPlayed = true;
            if (SoundManager.Instance != null)
                SoundManager.Instance.SetBGMPitch(hurryPitch, pitchChangeDuration);
        }
    }

    private void Play(BGMType type)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBGM(type, fadeDuration);
    }
}
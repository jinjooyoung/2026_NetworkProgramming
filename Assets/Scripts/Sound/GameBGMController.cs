using UnityEngine;

/// <summary>
/// 게임 씬 전용 BGM 컨트롤러
/// GameStateManager의 상태를 보고 BGM을 자동으로 바꿔줍니다. (GameStateManager 코드는 건드리지 않음)
///  - 씬 시작        : InGame
///  - 10초 남았을 때  : InGameHurry (TimeLimit 게임)
///  - 마지막 목숨일 때 : InGameHurry (LifeLimit 게임, 옵션)
///  - 스코어 화면     : Result
/// </summary>
public class GameBGMController : MonoBehaviour
{
    [Header("참조 (비워두면 GameStateManager.Instance 자동 사용)")]
    [SerializeField] private GameStateManager gameStateManager;

    [Header("설정")]
    [Tooltip("남은 시간이 이 값 이하가 되면 긴박한 BGM으로 전환 (TimeLimit 게임)")]
    [SerializeField] private float hurryTime = 10f;
    [Tooltip("LifeLimit 게임에서 목숨이 1개 남으면 긴박한 BGM으로 전환")]
    [SerializeField] private bool hurryOnLastLife = true;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isHurryPlayed = false;
    private bool isResultPlayed = false;

    private void Start()
    {
        if (gameStateManager == null) gameStateManager = GameStateManager.Instance;
        Play(BGMType.InGame);
    }

    private void Update()
    {
        if (gameStateManager == null || isResultPlayed) return;

        // 1. 스코어 화면이 켜지면 결과 BGM
        bool isScoreShown = gameStateManager.scorePanel != null
            ? gameStateManager.scorePanel.activeInHierarchy
            : gameStateManager.isGameOver;

        if (gameStateManager.isGameOver && isScoreShown)
        {
            isResultPlayed = true;
            Play(BGMType.Result);
            return;
        }

        // 2. 위기 상황이면 긴박한 BGM
        if (isHurryPlayed || !gameStateManager.isGameStarted || gameStateManager.isGameOver) return;

        bool isHurry = false;
        if (gameStateManager.gameType == GameType.TimeLimit)
            isHurry = gameStateManager.currentTime <= hurryTime;
        else if (gameStateManager.gameType == GameType.LifeLimit && hurryOnLastLife)
            isHurry = gameStateManager.currentLife == 1;

        if (isHurry)
        {
            isHurryPlayed = true;
            Play(BGMType.InGameHurry);
        }
    }

    private void Play(BGMType type)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBGM(type, fadeDuration);
    }
}

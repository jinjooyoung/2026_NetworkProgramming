using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("게임 상태 매니저 참조")]
    public GameStateManager gameStateManager;

    [Header("UI 패널 오브젝트")]
    public GameObject timePanel;             // 시간 UI 패널
    public GameObject lifePanel;             // 생명 UI 패널

    [Header("UI 요소 변수")]
    public TextMeshProUGUI timeText;         // 시간 표시용 TMP (없을 수 있음)
    public Image[] lifeImages;               // 생명 표시용 이미지 배열 (없을 수 있음)
    public TextMeshProUGUI scoreText;        // 점수 표시용 TMP

    // 생명 색상 정의 (#C6C5C5: 회색 / #F44336: 빨간색)
    private readonly Color colorGray = new Color32(0xC6, 0xC5, 0xC5, 0xFF);
    private readonly Color colorRed = new Color32(0xF4, 0x43, 0x36, 0xFF);

    private void Awake()
    {
        // 씬 전환 시 파괴되는 단일 씬 전용 싱글톤
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // GameType에 따라 해당 패널만 활성화
        if (gameStateManager != null)
        {
            if (gameStateManager.gameType == GameType.TimeLimit)
            {
                if (lifePanel != null) lifePanel.SetActive(false);
                if (timePanel != null) timePanel.SetActive(true);
            }
            else if (gameStateManager.gameType == GameType.LifeLimit)
            {
                if (timePanel != null) timePanel.SetActive(false);
                if (lifePanel != null) lifePanel.SetActive(true);
            }
        }

        // 씬 시작 시 초기 1회 갱신
        UpdateTimeUI();
        UpdateScoreUI();
        UpdateLifeUI();
    }

    private void Update()
    {
        if (gameStateManager == null) return;

        // 게임 시작됨 & 게임 오버 안됨 상태일 때만 UI 지속 업데이트
        if (gameStateManager.isGameStarted && !gameStateManager.isGameOver)
        {
            UpdateTimeUI();
        }
    }

    // [시간 UI] 올림 처리 후 문자열 변환
    private void UpdateTimeUI()
    {
        if (gameStateManager == null || gameStateManager.gameType != GameType.TimeLimit) return;
        if (timeText == null) return;

        int displayTime = Mathf.CeilToInt(gameStateManager.currentTime);
        timeText.text = displayTime.ToString();
    }

    // [점수 UI] 3자리마다 컴마(,) 표기 처리
    public void UpdateScoreUI()
    {
        if (scoreText == null || gameStateManager == null) return;

        scoreText.text = gameStateManager.currentScore.ToString("#,##0");
    }

    // [생명 UI] 체력 감소 시 외부(GameStateManager)에서 직접 호출
    public void UpdateLifeUI()
    {
        if (gameStateManager == null || gameStateManager.gameType != GameType.LifeLimit) return;
        if (lifeImages == null || lifeImages.Length == 0) return;

        // 1. 모든 생명 이미지를 회색(#C6C5C5)으로 초기화
        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null)
            {
                lifeImages[i].color = colorGray;
            }
        }

        // 2. 현재 남은 생명 수만큼 빨간색(#F44336)으로 변경
        int currentLife = Mathf.Clamp(gameStateManager.currentLife, 0, lifeImages.Length);
        for (int i = 0; i < currentLife; i++)
        {
            if (lifeImages[i] != null)
            {
                lifeImages[i].color = colorRed;
            }
        }
    }
}
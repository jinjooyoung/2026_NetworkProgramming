using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameType { TimeLimit, LifeLimit }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    [Header("게임 기본 설정")]
    public int gameIndex;
    public GameType gameType;
    public float timeLimit = 60f;
    public int maxLife = 3;

    [Header("실시간 상태 변수")]
    public bool isGameStarted = false;
    public bool isGameOver = false;
    public float currentTime;
    public int currentLife;
    public int currentScore = 0;

    [Header("카운트다운 & 게임오버 UI")]
    public GameObject countdownPanel;
    public TextMeshProUGUI countdownText;
    public GameObject gameOverPanel;

    [Header("스코어 패널 루트")]
    public GameObject scorePanel;
    public CanvasGroup scorePanelCanvasGroup;

    [Header("일반점수화면 (일반 & 개인최고 공용)")]
    public GameObject panelNormal;
    public TextMeshProUGUI normalBestScoreText;
    public TextMeshProUGUI normalCurrentScoreText;
    public Button normalRestartButton;
    public Button normalHomeButton;

    [Header("랭킹점수화면 (명예의 전당)")]
    public GameObject panelHallOfFame;
    public TextMeshProUGUI hallOfFameCurrentScoreText;
    public TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[3];
    private int[] slotIndices = { 0, 0, 0 };
    public Button[] slotUpButtons = new Button[3];
    public Button[] slotDownButtons = new Button[3];
    public Button submitButton;
    public Button hallOfFameRestartButton;
    public Button hallOfFameHomeButton;

    // 폰트 색상 정의 (남색: #2D324D / 노란색: #FEC90D)
    private readonly Color colorNormal = new Color32(0x2D, 0x32, 0x4D, 0xFF);
    private readonly Color colorBest = new Color32(0xFE, 0xC9, 0x0D, 0xFF);

    private void Awake() { Instance = this; }

    private void Start()
    {
        currentTime = timeLimit;
        currentLife = maxLife;
        isGameStarted = false;
        isGameOver = false;

        if (countdownPanel != null) countdownPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scorePanel != null) scorePanel.SetActive(false);

        SetPanelState(scorePanelCanvasGroup, false);

        StartCoroutine(StartCountdownRoutine());
    }

    private IEnumerator StartCountdownRoutine()
    {
        int count = 3;
        while (count > 0)
        {
            if (countdownText != null) countdownText.text = count.ToString();
            yield return new WaitForSeconds(1f);
            count--;
        }
        if (countdownPanel != null) countdownPanel.SetActive(false);
        isGameStarted = true;
    }

    private void Update()
    {
        if (!isGameStarted || isGameOver) return;

        if (gameType == GameType.TimeLimit)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f) { currentTime = 0f; TriggerGameOver(); }
        }
        else if (gameType == GameType.LifeLimit)
        {
            if (currentLife <= 0) { currentLife = 0; TriggerGameOver(); }
        }
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;
        currentScore = Mathf.Max(0, currentScore + amount);
        UIManager.Instance.UpdateScoreUI();
    }

    public void ReduceLife(int amount = 1)
    {
        if (isGameOver) return;
        currentLife -= amount;
        UIManager.Instance.UpdateLifeUI();
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        isGameStarted = false;
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        yield return new WaitForSeconds(1f);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        ShowScorePanel();
    }

    private void ShowScorePanel()
    {
        if (scorePanel != null) scorePanel.SetActive(true);
        SetPanelState(scorePanelCanvasGroup, true);

        int localBest = PlayerPrefs.GetInt($"BestScore{gameIndex}", 0);
        int rank3Cutoff = NetworkManager.Instance != null ? NetworkManager.Instance.GetCutoffScore(gameIndex.ToString()) : 0;

        if (panelNormal != null) panelNormal.SetActive(false);
        if (panelHallOfFame != null) panelHallOfFame.SetActive(false);

        // TOP 3 랭킹 달성 여부 체크
        if (currentScore > rank3Cutoff)
        {
            // 1. 랭킹점수화면 (명예의 전당 패널)
            if (panelHallOfFame != null) panelHallOfFame.SetActive(true);

            if (hallOfFameCurrentScoreText != null)
                hallOfFameCurrentScoreText.text = $"{currentScore}";

            // 초기 버튼 활성화/비활성화 처리
            SetHallOfFameInteractable(true);

            // 로컬 최고점수 갱신
            if (currentScore > localBest)
            {
                PlayerPrefs.SetInt($"BestScore{gameIndex}", currentScore);
                PlayerPrefs.Save();
            }

            UpdateSlotUI();
        }
        else
        {
            // 2. 일반점수화면 (일반 또는 개인최고 패널)
            if (panelNormal != null) panelNormal.SetActive(true);

            bool isNewBest = currentScore > localBest;

            if (isNewBest)
            {
                PlayerPrefs.SetInt($"BestScore{gameIndex}", currentScore);
                PlayerPrefs.Save();
                localBest = currentScore;
            }

            // 색상 결정 (개인 최고 달성시 노란색, 기본은 남색)
            Color targetColor = isNewBest ? colorBest : colorNormal;

            if (normalBestScoreText != null)
            {
                normalBestScoreText.text = $"Best : {localBest}";
                normalBestScoreText.color = targetColor;
            }

            if (normalCurrentScoreText != null)
            {
                normalCurrentScoreText.text = $"{currentScore}";
                normalCurrentScoreText.color = targetColor;
            }
        }
    }

    // 패널 활성화/비활성화 및 CanvasGroup 상태 제어 헬퍼 함수
    private void SetPanelState(CanvasGroup canvasGroup, bool isActive)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isActive ? 1f : 0f;
            canvasGroup.blocksRaycasts = isActive;
            canvasGroup.interactable = isActive;
        }
    }

    public void OnClickSlotArrow(int slotIdx, int dir)
    {
        slotIndices[slotIdx] = (slotIndices[slotIdx] + dir + 26) % 26;
        UpdateSlotUI();
    }

    // --- 버튼 OnClick 전용 래퍼 함수 (매개변수 0개) ---
    public void OnClickSlot0Up() { OnClickSlotArrow(0, 1); }
    public void OnClickSlot0Down() { OnClickSlotArrow(0, -1); }

    public void OnClickSlot1Up() { OnClickSlotArrow(1, 1); }
    public void OnClickSlot1Down() { OnClickSlotArrow(1, -1); }

    public void OnClickSlot2Up() { OnClickSlotArrow(2, 1); }
    public void OnClickSlot2Down() { OnClickSlotArrow(2, -1); }

    private void UpdateSlotUI()
    {
        for (int i = 0; i < 3; i++)
        {
            if (slotTexts[i] != null)
                slotTexts[i].text = ((char)('A' + slotIndices[i])).ToString();
        }
    }

    public void OnClickSubmit()
    {
        // 1. 제출 UI 비활성화
        SetHallOfFameInteractable(false);

        // 2. 이니셜 문자열 생성
        string initial = "AAA";
        if (slotTexts != null && slotTexts.Length >= 3 &&
            slotTexts[0] != null && slotTexts[1] != null && slotTexts[2] != null)
        {
            initial = $"{slotTexts[0].text}{slotTexts[1].text}{slotTexts[2].text}";
        }

        // 3. 서버 점수 전송
        if (NetworkManager.Instance != null)
        {
            StartCoroutine(NetworkManager.Instance.PostScore(gameIndex.ToString(), initial, currentScore, (success) => {
                if (success)
                {
                    Debug.Log("서버 최고기록 송신 완료");
                }
                else
                {
                    Debug.LogWarning("서버 전송 실패 또는 타임아웃");
                }
            }));
        }
        else
        {
            Debug.LogWarning("[GameStateManager] NetworkManager가 씬에 존재하지 않습니다.");
        }
    }

    private void SetHallOfFameInteractable(bool isEnteringInitial)
    {
        // 이니셜 입력 관련 버튼 상태 변경
        if (submitButton != null) submitButton.interactable = isEnteringInitial;

        for (int i = 0; i < 3; i++)
        {
            if (slotUpButtons[i] != null) slotUpButtons[i].interactable = isEnteringInitial;
            if (slotDownButtons[i] != null) slotDownButtons[i].interactable = isEnteringInitial;
        }

        // 네비게이션 버튼 상태 변경 (제출 완료 전엔 비활성화, 제출 완료 후 활성화)
        if (hallOfFameRestartButton != null) hallOfFameRestartButton.interactable = !isEnteringInitial;
        if (hallOfFameHomeButton != null) hallOfFameHomeButton.interactable = !isEnteringInitial;
    }

    public void OnClickRestart() { SceneManager.LoadScene(gameIndex.ToString()); }
    public void OnClickHome() { SceneManager.LoadScene("GameSelectScene"); }
}
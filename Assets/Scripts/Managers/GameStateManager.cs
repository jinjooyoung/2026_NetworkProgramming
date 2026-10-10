using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public Text countdownText;
    public GameObject gameOverPanel;

    [Header("스코어 패널 (3종 화면)")]
    public GameObject scorePanel;
    public GameObject screenNormal;
    public GameObject screenPersonalBest;
    public GameObject screenHallOfFame;

    [Header("스코어 텍스트 UI")]
    public Text normalCurrentScoreText;
    public Text normalBestScoreText;
    public Text personalBestScoreText;
    public Text hallOfFameScoreText;

    [Header("명예의 전당 룰렛 UI")]
    public Text[] slotTexts = new Text[3];
    private int[] slotIndices = { 0, 0, 0 };
    public Button submitButton;

    [Header("갱신 리더보드 & 네비 버튼")]
    public GameObject leaderboardGroup;
    public Text rank1Text;
    public Text rank2Text;
    public Text rank3Text;
    public GameObject navButtonsGroup;

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
    }

    public void ReduceLife(int amount = 1)
    {
        if (isGameOver) return;
        currentLife -= amount;
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

        int localBest = PlayerPrefs.GetInt($"BestScore{gameIndex}", 0);
        int rank3Cutoff = NetworkManager.Instance != null ? NetworkManager.Instance.GetCutoffScore(gameIndex.ToString()) : 0;

        screenNormal.SetActive(false);
        screenPersonalBest.SetActive(false);
        screenHallOfFame.SetActive(false);
        if (leaderboardGroup != null) leaderboardGroup.SetActive(false);
        if (navButtonsGroup != null) navButtonsGroup.SetActive(false);

        if (currentScore > localBest)
        {
            PlayerPrefs.SetInt($"BestScore{gameIndex}", currentScore);
            PlayerPrefs.Save();

            if (currentScore > rank3Cutoff)
            {
                screenHallOfFame.SetActive(true);
                if (hallOfFameScoreText != null) hallOfFameScoreText.text = $"TOP 3 진입!\n{currentScore}점!";
                UpdateSlotUI();
            }
            else
            {
                screenPersonalBest.SetActive(true);
                if (personalBestScoreText != null) personalBestScoreText.text = $"새로운 최고 기록!\n{currentScore}점!";
                if (navButtonsGroup != null) navButtonsGroup.SetActive(true);
            }
        }
        else
        {
            screenNormal.SetActive(true);
            if (normalCurrentScoreText != null) normalCurrentScoreText.text = $"이번 점수: {currentScore}점";
            if (normalBestScoreText != null) normalBestScoreText.text = $"내 최고 기록: {localBest}점";
            if (navButtonsGroup != null) navButtonsGroup.SetActive(true);
        }
    }

    public void OnClickSlotArrow(int slotIdx, int dir)
    {
        slotIndices[slotIdx] = (slotIndices[slotIdx] + dir + 26) % 26;
        UpdateSlotUI();
    }

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
        if (submitButton != null) submitButton.interactable = false;
        string initial = $"{slotTexts[0].text}{slotTexts[1].text}{slotTexts[2].text}";

        StartCoroutine(NetworkManager.Instance.PostScore(gameIndex.ToString(), initial, currentScore, (success) => {
            screenHallOfFame.SetActive(false);
            ShowUpdatedLeaderboard();
        }));
    }

    private void ShowUpdatedLeaderboard()
    {
        if (leaderboardGroup != null) leaderboardGroup.SetActive(true);
        if (navButtonsGroup != null) navButtonsGroup.SetActive(true);

        var lb = NetworkManager.Instance.cachedLeaderboard;
        ScoreEntry[] list = lb.GetListByGameId(gameIndex.ToString());

        if (rank1Text != null) rank1Text.text = (list != null && list.Length > 0) ? $"1st {list[0].name} : {list[0].score}" : "-";
        if (rank2Text != null) rank2Text.text = (list != null && list.Length > 1) ? $"2nd {list[1].name} : {list[1].score}" : "-";
        if (rank3Text != null) rank3Text.text = (list != null && list.Length > 2) ? $"3rd {list[2].name} : {list[2].score}" : "-";
    }

    public void OnClickRestart() { SceneManager.LoadScene(gameIndex.ToString()); }
    public void OnClickHome() { SceneManager.LoadScene("GameSelectScene"); }
}
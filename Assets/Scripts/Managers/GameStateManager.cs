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

    // ===================== [신규] 피드백 연출 =====================
    [Header("[신규] 피드백 연출")]
    [Tooltip("화면 전체를 덮는 빨간 타격 이미지 (RaycastTarget 꺼짐, 알파 0)")]
    public Image hitFlashImage;
    [Range(0f, 1f)] public float hitFlashAlpha = 0.35f;
    public float cameraShakeDuration = 0.25f;
    public float cameraShakeStrength = 0.15f;

    private float lastNegativeFeedbackTime = -1f;

    /// <summary>
    /// [신규] 게임 입력을 막아야 하는 상태인지 (게임 시작 전 / 게임오버 / 설정 팝업·튜토리얼로 일시정지)
    /// 각 미니게임 매니저의 입력 체크에서 공통으로 사용
    /// </summary>
    public static bool IsInputBlocked =>
        Instance == null || !Instance.isGameStarted || Instance.isGameOver || Time.timeScale <= 0f;
    // ==============================================================

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
        PlaySFX(SFXType.Whistle); // 시작 호루라기
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

        // [신규] 점수 증감 피드백
        if (amount > 0) PlaySFX(SFXType.ScoreUp);
        else if (amount < 0) PlayNegativeFeedback();
    }

    public void ReduceLife(int amount = 1)
    {
        if (isGameOver) return;
        currentLife -= amount;
        UIManager.Instance.UpdateLifeUI();

        // [신규] 체력 감소 피드백
        PlayNegativeFeedback();
    }

    /// <summary>
    /// [신규] 부정 피드백 : 카메라 흔들림 + 빨간 화면 번쩍 + 삐- 사운드
    /// (폭탄처럼 점수 감소와 체력 감소가 같은 순간에 일어나면 1번만 재생)
    /// </summary>
    private void PlayNegativeFeedback()
    {
        if (Time.unscaledTime - lastNegativeFeedbackTime < 0.05f) return;
        lastNegativeFeedbackTime = Time.unscaledTime;

        DOTweenManager.ShakeCamera(null, cameraShakeDuration, cameraShakeStrength);
        DOTweenManager.HitFlash(hitFlashImage, hitFlashAlpha);
        PlaySFX(SFXType.ScoreDown);
    }

    // [신규]
    private void PlaySFX(SFXType type)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(type);
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
        PlaySFX(SFXType.Whistle); // 종료 호루라기
        yield return new WaitForSeconds(1f);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        ShowScorePanel();
    }

    private void ShowScorePanel()
    {
        if (scorePanel != null) scorePanel.SetActive(true);
        SetPanelState(scorePanelCanvasGroup, true);

        // [변경] NetworkManager(Render) → ScoreSaver(Firebase)
        ScoreSaver saver = ScoreSaver.Instance;
        int serverBest = saver != null ? saver.GetMyServerBest(gameIndex) : 0;                      // 내 서버 기록
        int localBest = Mathf.Max(PlayerPrefs.GetInt($"BestScore{gameIndex}", 0), serverBest);    // 다른 기기 기록 포함
        int rank3Cutoff = saver != null ? saver.GetCutoffScore(gameIndex.ToString()) : 0;
        bool canSave = saver != null && AuthManager.IsLoggedIn;   // 로그인해야 서버 저장 가능
        bool isServerNewBest = currentScore > serverBest;         // 서버의 내 칸을 갱신하는 점수인가

        if (panelNormal != null) panelNormal.SetActive(false);
        if (panelHallOfFame != null) panelHallOfFame.SetActive(false);

        // TOP 3 랭킹 달성 여부 체크
        // [변경] 사용자당 한 칸이므로, 내 서버 기록보다도 높아야 랭킹이 바뀜 + 로그인 상태여야 저장 가능
        if (canSave && isServerNewBest && currentScore > rank3Cutoff)
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

            LoadSavedInitials(); // [신규] 지난번에 입력한 이니셜을 기본값으로
            UpdateSlotUI();
        }
        else
        {
            // [신규] TOP3는 아니지만 서버의 내 기록보다 높으면 조용히 저장 (이니셜은 지난번 값)
            if (canSave && isServerNewBest && currentScore > 0)
                saver.Save(gameIndex, PlayerPrefs.GetString(INITIALS_KEY, "???"), currentScore);

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

    // [신규] 마지막으로 입력한 이니셜 저장/불러오기
    private const string INITIALS_KEY = "PlayerInitials";

    private void LoadSavedInitials()
    {
        string saved = PlayerPrefs.GetString(INITIALS_KEY, "");
        if (saved.Length != 3) return;
        for (int i = 0; i < 3; i++)
        {
            int idx = saved[i] - 'A';
            if (idx >= 0 && idx < 26) slotIndices[i] = idx;
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

        PlayerPrefs.SetString(INITIALS_KEY, initial); // [신규] 다음 입력의 기본값
        PlayerPrefs.Save();

        // 3. [변경] Firebase 에 점수 저장 (수업 실습 7 : 읽어서 비교 → 더 높을 때만 PUT)
        if (ScoreSaver.Instance != null)
        {
            ScoreSaver.Instance.Save(gameIndex, initial, currentScore, success =>
            {
                if (success) Debug.Log("서버 최고기록 저장 완료");
                else Debug.LogWarning("서버 저장 실패 (로그인 상태·DB 주소·규칙 확인)");
            });
        }
        else
        {
            Debug.LogWarning("[GameStateManager] ScoreSaver가 없습니다. (로비부터 실행해야 생성됨)");
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

    public void OnClickRestart() { SceneManager.LoadScene(SceneNames.Game(gameIndex)); }  // [변경] 씬 이름 → SceneNames
    public void OnClickHome() { SceneManager.LoadScene(SceneNames.GameSelect); }          // [변경] 씬 이름 → SceneNames
}
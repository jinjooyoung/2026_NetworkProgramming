using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainLobbyManager : MonoBehaviour
{
    [Header("통합 최고점수 UI (서버 전체 1위)")]
    public TextMeshProUGUI grandBestScoreText;
    public TextMeshProUGUI grandBestInitialText;

    [Header("각 게임 1~3위 롤링 UI")]
    public TextMeshProUGUI currentGameTitleText;
    public TextMeshProUGUI rank1Text;
    public TextMeshProUGUI rank2Text;
    public TextMeshProUGUI rank3Text;

    [Header("[신규] 로그인 사용자 표시 (수업 MenuUI 역할)")]
    public GameObject userInfoRoot;          // 이름 + 로그아웃 버튼 묶음
    public TextMeshProUGUI userNameText;     // "손님 Kq8wZ2"

    private string[] gameIds = { "0", "1", "2" };
    private string[] gameTitles = { "카드 짝맞추기", "스피드 분리수거", "후르츠 슬라이스" };
    private int currentIndex = 0;

    private void Start()
    {
        // [신규] 로그인 사용자 표시 (수업 MenuUI.Start)
        if (userInfoRoot != null) userInfoRoot.SetActive(AuthManager.IsLoggedIn);
        if (userNameText != null) userNameText.text = AuthManager.Name;

        // [변경] NetworkManager(Render 서버) → ScoreSaver(Firebase)
        ScoreSaver saver = ScoreSaver.Instance;
        if (saver == null)
        {
            Debug.LogError("[MainLobbyManager] ScoreSaver.Instance가 null입니다!");
            return;
        }

        // 1. 캐시가 있으면 즉시 표시 (게임에서 돌아왔을 때 빈 화면 방지)
        bool hadCache = saver.cachedLeaderboard != null;
        if (hadCache)
        {
            UpdateGrandBestUI();
            StartCoroutine(RotateRankRoutine());
        }

        // 2. 항상 서버에서 최신 랭킹을 다시 받아 갱신
        saver.FetchLeaderboard(success =>
        {
            if (this == null) return; // 응답 전에 씬이 바뀐 경우
            if (success)
            {
                UpdateGrandBestUI();
                StopAllCoroutines();
                StartCoroutine(RotateRankRoutine());
            }
            else if (!hadCache) // 실패했고 보여줄 캐시도 없을 때만 기본값
            {
                Debug.LogWarning("[MainLobbyManager] 랭킹 요청 실패.");
                if (grandBestScoreText != null) grandBestScoreText.text = "0";
                if (grandBestInitialText != null) grandBestInitialText.text = "---";
            }
        });

        // 3. 로그인했다면 내 서버 기록도 받아 둠 (다른 기기 기록 동기화 + 명예의 전당 판정용)
        if (AuthManager.IsLoggedIn) saver.FetchMyBests();
    }

    /// <summary>
    /// [신규] 로그아웃 버튼 OnClick (수업 MenuUI.Logout)
    /// </summary>
    public void OnClickLogout()
    {
        AuthManager.Logout();
    }

    // [서버 전체 최고 기록] 서버의 모든 게임 1~3위 기록 중 최고점수와 이니셜 표시
    private void UpdateGrandBestUI()
    {
        var lb = ScoreSaver.Instance != null ? ScoreSaver.Instance.cachedLeaderboard : null; // [변경]
        if (lb == null)
        {
            Debug.LogWarning("[UpdateGrandBestUI] cachedLeaderboard가 null이므로 UI 업데이트를 취소합니다.");
            return;
        }

        int maxScore = -1;
        string bestName = "---";

        // 서버의 모든 게임 리더보드 데이터 순회 (0번, 1번, 2번 게임)
        for (int i = 0; i < lb.Count; i++) // [변경] game1~3 고정 필드 → 게임 개수만큼
        {
            var list = lb.GetList(i);
            if (list != null && list.Length > 0)
            {
                Debug.Log($"[UpdateGrandBestUI] Game {i} 1위: Score={list[0].score}, Name={list[0].name}");
                if (list[0].score > maxScore)
                {
                    maxScore = list[0].score;
                    bestName = list[0].name;
                }
            }
            else
            {
                Debug.Log($"[UpdateGrandBestUI] Game {i} 데이터가 비어있습니다.");
            }
        }

        Debug.Log($"[UpdateGrandBestUI 최종 선택 결과] 최고점수: {maxScore}, 이니셜: {bestName}");

        // 전체 1위 점수 및 해당 작성자 이니셜 UI 반영
        if (grandBestScoreText != null)
            grandBestScoreText.text = maxScore > -1 ? maxScore.ToString("#,##0") : "0";

        if (grandBestInitialText != null)
            grandBestInitialText.text = maxScore > -1 ? bestName : "---";
    }

    private IEnumerator RotateRankRoutine()
    {
        while (true)
        {
            DisplayRank(gameIds[currentIndex], gameTitles[currentIndex]);
            yield return new WaitForSeconds(3f);
            currentIndex = (currentIndex + 1) % gameIds.Length;
        }
    }

    private void DisplayRank(string id, string title)
    {
        if (currentGameTitleText != null) currentGameTitleText.text = title;
        var lb = ScoreSaver.Instance != null ? ScoreSaver.Instance.cachedLeaderboard : null; // [변경]
        if (lb == null) return;

        ScoreEntry[] list = lb.GetListByGameId(id);
        Debug.Log($"[DisplayRank] ID: {id}({title}) 항목 개수: {(list != null ? list.Length : 0)}");

        if (rank1Text != null)
            rank1Text.text = (list != null && list.Length > 0) ? $"{list[0].score:#,##0}\n{list[0].name}" : "0\n---";

        if (rank2Text != null)
            rank2Text.text = (list != null && list.Length > 1) ? $"{list[1].score:#,##0}\n{list[1].name}" : "0\n---";

        if (rank3Text != null)
            rank3Text.text = (list != null && list.Length > 2) ? $"{list[2].score:#,##0}\n{list[2].name}" : "0\n---";
    }

    public void OnClickStart()
    {
        SceneManager.LoadScene(SceneNames.GameSelect); // [변경] 씬 이름 → SceneNames
    }
}
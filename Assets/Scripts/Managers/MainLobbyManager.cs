using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainLobbyManager : MonoBehaviour
{
    [Header("통합 최고점수 UI")]
    public TextMeshProUGUI grandBestScoreText;
    public TextMeshProUGUI grandBestInitialText;

    [Header("각 게임 1~3위 롤링 UI")]
    public TextMeshProUGUI currentGameTitleText;
    public TextMeshProUGUI rank1Text;
    public TextMeshProUGUI rank2Text;
    public TextMeshProUGUI rank3Text;

    private string[] gameIds = { "0", "1", "2" };
    private string[] gameTitles = { "카드 짝맞추기", "스피드 분리수거", "후르츠 슬라이스" };
    private int currentIndex = 0;

    private void Start()
    {
        // 1. 서버 통신 전, 내 기기에 저장된 개인 최고 기록부터 먼저 UI에 즉시 표시
        UpdateLocalGrandBestUI();

        // 2. NetworkManager 존재 체크 후 서버 리더보드 요청
        if (NetworkManager.Instance != null)
        {
            StartCoroutine(NetworkManager.Instance.GetLeaderboard((success) => {
                if (success)
                {
                    // 서버 통신 성공 시 서버 전체 1위(또는 최신 데이터)로 UI 갱신 및 랭킹 롤링 시작
                    UpdateGrandBestUI();
                    StartCoroutine(RotateRankRoutine());
                }
                else
                {
                    Debug.LogWarning("[MainLobbyManager] 리더보드 데이터를 가져오는데 실패했습니다.");
                }
            }));
        }
    }

    // [내 기기 개인 최고 기록] 로컬 PlayerPrefs에서 가장 높은 점수 가져오기
    private void UpdateLocalGrandBestUI()
    {
        int myBest = 0;
        for (int i = 0; i < 3; i++)
        {
            int score = PlayerPrefs.GetInt($"BestScore{i}", 0);
            if (score > myBest) myBest = score;
        }

        if (grandBestScoreText != null) grandBestScoreText.text = myBest.ToString("#,##0");
        if (grandBestInitialText != null) grandBestInitialText.text = "MY BEST";
    }

    // [서버 전체 최고 기록] 서버에 등록된 0~2번 게임 1등 중 최고점수 가져오기
    private void UpdateGrandBestUI()
    {
        var lb = NetworkManager.Instance != null ? NetworkManager.Instance.cachedLeaderboard : null;
        if (lb == null) return;

        int maxScore = -1;
        string bestName = "---";

        ScoreEntry[][] all = { lb.game1, lb.game2, lb.game3 };
        foreach (var list in all)
        {
            if (list != null && list.Length > 0 && list[0].score > maxScore)
            {
                maxScore = list[0].score;
                bestName = list[0].name;
            }
        }

        if (maxScore > -1)
        {
            if (grandBestScoreText != null) grandBestScoreText.text = maxScore.ToString("#,##0");
            if (grandBestInitialText != null) grandBestInitialText.text = bestName;
        }
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
        var lb = NetworkManager.Instance != null ? NetworkManager.Instance.cachedLeaderboard : null;
        if (lb == null) return;

        ScoreEntry[] list = lb.GetListByGameId(id);
        if (rank1Text != null)
            rank1Text.text = (list != null && list.Length > 0) ? $"{list[0].score:#,##0}\n{list[0].name}" : "0\n---";

        if (rank2Text != null)
            rank2Text.text = (list != null && list.Length > 1) ? $"{list[1].score:#,##0}\n{list[1].name}" : "0\n---";

        if (rank3Text != null)
            rank3Text.text = (list != null && list.Length > 2) ? $"{list[2].score:#,##0}\n{list[2].name}" : "0\n---";
    }

    public void OnClickStart()
    {
        SceneManager.LoadScene("GameSelectScene");
    }
}
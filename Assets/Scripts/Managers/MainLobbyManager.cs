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
        StartCoroutine(NetworkManager.Instance.GetLeaderboard((success) => {
            UpdateGrandBestUI();
            StartCoroutine(RotateRankRoutine());
        }));
    }

    private void UpdateGrandBestUI()
    {
        var lb = NetworkManager.Instance.cachedLeaderboard;
        if (lb == null) return;

        int maxScore = -1;
        string bestName = "AAA";

        ScoreEntry[][] all = { lb.game1, lb.game2, lb.game3 };
        foreach (var list in all)
        {
            if (list != null && list.Length > 0 && list[0].score > maxScore)
            {
                maxScore = list[0].score;
                bestName = list[0].name;
            }
        }

        if (grandBestScoreText != null) grandBestScoreText.text = maxScore > -1 ? maxScore.ToString() : "0";
        if (grandBestInitialText != null) grandBestInitialText.text = bestName;
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
        var lb = NetworkManager.Instance.cachedLeaderboard;
        if (lb == null) return;

        ScoreEntry[] list = lb.GetListByGameId(id);
        if (rank1Text != null)
            rank1Text.text = (list != null && list.Length > 0) ? $"{list[0].score}\n{list[0].name}" : "0\n---";

        if (rank2Text != null)
            rank2Text.text = (list != null && list.Length > 1) ? $"{list[1].score}\n{list[1].name}" : "0\n---";

        if (rank3Text != null)
            rank3Text.text = (list != null && list.Length > 2) ? $"{list[2].score}\n{list[2].name}" : "0\n---";
    }

    public void OnClickStart()
    {
        SceneManager.LoadScene("GameSelectScene");
    }
}
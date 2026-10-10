using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainLobbyManager : MonoBehaviour
{
    [Header("통합 최고점수 UI")]
    public Text grandBestScoreText;
    public Text grandBestInitialText;

    [Header("각 게임 1~3위 롤링 UI")]
    public Text currentGameTitleText;
    public Text rank1Text;
    public Text rank2Text;
    public Text rank3Text;

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
        if (rank1Text != null) rank1Text.text = (list != null && list.Length > 0) ? $"1st  {list[0].name}  {list[0].score}" : "1st  ---  0";
        if (rank2Text != null) rank2Text.text = (list != null && list.Length > 1) ? $"2nd  {list[1].name}  {list[1].score}" : "2nd  ---  0";
        if (rank3Text != null) rank3Text.text = (list != null && list.Length > 2) ? $"3rd  {list[2].name}  {list[2].score}" : "3rd  ---  0";
    }

    public void OnClickStart()
    {
        SceneManager.LoadScene("GameSelectScene");
    }
}
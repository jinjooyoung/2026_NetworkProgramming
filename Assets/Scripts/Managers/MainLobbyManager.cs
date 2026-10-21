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

    private string[] gameIds = { "0", "1", "2" };
    private string[] gameTitles = { "카드 짝맞추기", "스피드 분리수거", "후르츠 슬라이스" };
    private int currentIndex = 0;

    private void Start()
    {
        Debug.Log("[MainLobbyManager] Start 실행됨.");

        // 1. 이미 NetworkManager에서 가져온 캐시 데이터가 있다면 즉시 UI 반영
        if (NetworkManager.Instance != null && NetworkManager.Instance.cachedLeaderboard != null)
        {
            Debug.Log("[MainLobbyManager] 기존 캐시 데이터가 존재하여 즉시 UI를 업데이트합니다.");
            UpdateGrandBestUI();
            StartCoroutine(RotateRankRoutine());
        }
        else if (NetworkManager.Instance != null)
        {
            Debug.Log("[MainLobbyManager] 캐시 데이터가 없어 서버에서 GetLeaderboard를 새로 요청합니다.");
            StartCoroutine(NetworkManager.Instance.GetLeaderboard((success) => {
                if (success)
                {
                    Debug.Log("[MainLobbyManager] GetLeaderboard 성공 콜백 수신. UI 업데이트 진행.");
                    UpdateGrandBestUI();
                    StopAllCoroutines();
                    StartCoroutine(RotateRankRoutine());
                }
                else
                {
                    Debug.LogWarning("[MainLobbyManager] GetLeaderboard 요청 실패.");
                    if (grandBestScoreText != null) grandBestScoreText.text = "0";
                    if (grandBestInitialText != null) grandBestInitialText.text = "---";
                }
            }));
        }
        else
        {
            Debug.LogError("[MainLobbyManager] NetworkManager.Instance가 null입니다!");
        }
    }

    // [서버 전체 최고 기록] 서버의 모든 게임 1~3위 기록 중 최고점수와 이니셜 표시
    private void UpdateGrandBestUI()
    {
        var lb = NetworkManager.Instance != null ? NetworkManager.Instance.cachedLeaderboard : null;
        if (lb == null)
        {
            Debug.LogWarning("[UpdateGrandBestUI] cachedLeaderboard가 null이므로 UI 업데이트를 취소합니다.");
            return;
        }

        int maxScore = -1;
        string bestName = "---";

        // 서버의 모든 게임 리더보드 데이터 순회 (0번, 1번, 2번 게임)
        ScoreEntry[][] all = { lb.game1, lb.game2, lb.game3 };
        for (int i = 0; i < all.Length; i++)
        {
            var list = all[i];
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
        var lb = NetworkManager.Instance != null ? NetworkManager.Instance.cachedLeaderboard : null;
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
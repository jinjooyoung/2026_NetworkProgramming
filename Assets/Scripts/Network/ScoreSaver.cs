using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Firebase Realtime Database 점수 저장 / 랭킹 불러오기 - 10강 실습 7의 ScoreSaver 로직 기반
/// (기존 NetworkManager + Render(Node.js) 서버를 대체)
///
/// 데이터 구조 : scores / {게임번호} / {uid} = { "name": "ABC", "score": 300 }
///   - 수업은 scores/{uid} 한 칸이지만, 이 게임은 미니게임이 여러 개라 게임별로 한 칸씩
///
/// [수업 코드와 다른 점]
/// - 로비에 한 번 배치하고 DontDestroyOnLoad (랭킹 캐시를 게임 씬에서도 쓰기 위해)
/// - FetchLeaderboard : 게임별 TOP3 를 서버 쿼리(orderBy + limitToLast)로 읽음
/// - FetchMyBests     : 내 서버 기록을 읽어 와서 다른 기기에서도 개인 최고점 유지
/// - 401 이면 idToken 을 재발급해서 한 번 더 시도 (2.14 토큰 만료)
/// </summary>
public class ScoreSaver : MonoBehaviour
{
    public static ScoreSaver Instance; // 다른 스크립트에서 ScoreSaver.Instance 로 접근

    [Header("Realtime Database 주소 (끝에 / 없이)")]
    public string dbUrl;

    [Header("설정")]
    [Tooltip("미니게임 개수 (게임을 추가하면 이 값만 늘리면 됨)")]
    public int gameCount = 3;
    public int timeoutSeconds = 15;

    [NonSerialized] public LeaderboardData cachedLeaderboard; // 게임별 TOP3 (내림차순). 인스펙터에 저장하지 않음
    private int[] myServerBest;               // 게임별 내 서버 기록
    private bool busy;                        // 저장 요청이 진행 중인가

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            myServerBest = new int[gameCount];
            cachedLeaderboard = null;
        }
        else
        {
            Destroy(gameObject); // 로비에 다시 돌아왔을 때 생긴 중복 제거
        }
    }

    private string Trimmed => string.IsNullOrEmpty(dbUrl) ? "" : dbUrl.TrimEnd('/'); // 끝 '/' 실수 방지 (4.1)

    #region 1. 랭킹 불러오기 (로그인 없이도 읽기 가능 : 규칙 .read = true)

    /// <summary>
    /// 모든 게임의 TOP3 를 동시에 요청하고, 전부 끝나면 onComplete(성공 여부)
    /// </summary>
    public void FetchLeaderboard(Action<bool> onComplete)
    {
        StartCoroutine(FetchAllRoutine(onComplete));
    }

    private IEnumerator FetchAllRoutine(Action<bool> onComplete)
    {
        if (cachedLeaderboard == null) cachedLeaderboard = new LeaderboardData(gameCount);

        int done = 0;
        bool allOk = true;
        for (int i = 0; i < gameCount; i++)
            StartCoroutine(FetchGameRoutine(i, ok => { allOk &= ok; done++; }));

        yield return new WaitUntil(() => done >= gameCount);
        onComplete?.Invoke(allOk);
    }

    private IEnumerator FetchGameRoutine(int gameIndex, Action<bool> onComplete)
    {
        // 점수 기준 정렬 후 마지막 3개(= 상위 3개). 규칙에 ".indexOn": ["score"] 가 있어야 동작
        string url = Trimmed + "/scores/" + gameIndex + ".json?orderBy=%22score%22&limitToLast=3";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = timeoutSeconds;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ScoreSaver] 랭킹 {gameIndex} 읽기 실패 : {req.error}\n{req.downloadHandler.text}");
                onComplete?.Invoke(false);
                yield break;
            }

            cachedLeaderboard.Set(gameIndex, ParseEntries(req.downloadHandler.text));
            onComplete?.Invoke(true);
        }
    }

    /// <summary>
    /// 3위 점수 (이 점수보다 높아야 명예의 전당). 3명 미만이면 0
    /// </summary>
    public int GetCutoffScore(string gameId)
    {
        if (cachedLeaderboard == null) return 0;
        ScoreEntry[] list = cachedLeaderboard.GetListByGameId(gameId);
        if (list == null || list.Length < 3) return 0;
        return list[2].score;
    }

    #endregion

    #region 2. 내 기록 불러오기

    /// <summary>
    /// 로그인한 사람의 게임별 서버 기록을 읽어 캐시. 로컬 최고점보다 높으면 PlayerPrefs 도 갱신
    /// </summary>
    public void FetchMyBests()
    {
        if (!AuthManager.IsLoggedIn) return;
        for (int i = 0; i < gameCount; i++) StartCoroutine(FetchMyBestRoutine(i));
    }

    private IEnumerator FetchMyBestRoutine(int gameIndex)
    {
        string url = Trimmed + "/scores/" + gameIndex + "/" + AuthManager.Uid + ".json";
        UnityWebRequest result = null;
        yield return SendWithAuth(token => UnityWebRequest.Get(url + "?auth=" + token), r => result = r);

        if (result != null && result.result == UnityWebRequest.Result.Success)
            ApplyMyBest(gameIndex, ReadScore(result.downloadHandler.text));
        result?.Dispose();
    }

    /// <summary>
    /// 로그아웃 시 이전 사용자의 기록 캐시를 비움
    /// </summary>
    public void ClearMyBests()
    {
        if (myServerBest != null) Array.Clear(myServerBest, 0, myServerBest.Length);
    }

    public int GetMyServerBest(int gameIndex)
    {
        if (myServerBest == null || gameIndex < 0 || gameIndex >= myServerBest.Length) return 0;
        return myServerBest[gameIndex];
    }

    private void ApplyMyBest(int gameIndex, int score)
    {
        if (gameIndex < 0 || gameIndex >= myServerBest.Length) return;
        myServerBest[gameIndex] = Mathf.Max(myServerBest[gameIndex], score);

        // 다른 기기/브라우저에서 세운 기록이면 게임 선택 화면의 Best 에도 보이도록
        string key = $"BestScore{gameIndex}";
        if (score > PlayerPrefs.GetInt(key, 0))
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
        }
    }

    #endregion

    #region 3. 점수 저장 (수업 실습 7 : 읽어서 비교 → 더 높을 때만 PUT)

    /// <summary>
    /// 최고 기록 저장. 로그인 전이거나 저장 중이면 보내지 않음
    /// </summary>
    public void Save(int gameIndex, string name, int score, Action<bool> onComplete = null)
    {
        if (!AuthManager.IsLoggedIn || busy || string.IsNullOrEmpty(dbUrl))
        {
            onComplete?.Invoke(false);
            return;
        }
        busy = true;
        StartCoroutine(PutRoutine(gameIndex, name, score, onComplete));
    }

    private IEnumerator PutRoutine(int gameIndex, string name, int score, Action<bool> onComplete)
    {
        string url = Trimmed + "/scores/" + gameIndex + "/" + AuthManager.Uid + ".json";

        // 1. 서버에 있는 내 기록을 먼저 읽음. 없으면 null
        UnityWebRequest get = null;
        yield return SendWithAuth(token => UnityWebRequest.Get(url + "?auth=" + token), r => get = r);
        int best = (get != null && get.result == UnityWebRequest.Result.Success) ? ReadScore(get.downloadHandler.text) : 0;
        get?.Dispose();

        if (score <= best) // 서버 기록이 더 높으면 덮어쓰지 않음
        {
            ApplyMyBest(gameIndex, best);
            busy = false;
            onComplete?.Invoke(true);
            yield break;
        }

        // 2. PUT : 내 칸을 통째로 교체
        string body = JsonUtility.ToJson(new ScoreEntry { name = name, score = score });
        UnityWebRequest put = null;
        yield return SendWithAuth(token =>
        {
            UnityWebRequest req = UnityWebRequest.Put(url + "?auth=" + token, body);
            req.SetRequestHeader("Content-Type", "application/json");
            return req;
        }, r => put = r);

        bool ok = put != null && put.result == UnityWebRequest.Result.Success;
        Debug.Log(ok ? $"[ScoreSaver] saved : game {gameIndex}, {name}, {score}"
                     : $"[ScoreSaver] save failed {put?.error}\n{put?.downloadHandler.text}");
        put?.Dispose();

        if (ok)
        {
            ApplyMyBest(gameIndex, score);
            if (cachedLeaderboard == null) cachedLeaderboard = new LeaderboardData(gameCount);
            yield return FetchGameRoutine(gameIndex, null); // 랭킹 캐시 갱신 → 로비에 바로 반영
        }

        busy = false;
        onComplete?.Invoke(ok);
    }

    #endregion

    #region 공통

    /// <summary>
    /// ?auth=토큰 을 붙인 요청을 보내고, 401(토큰 만료)이면 토큰을 재발급해서 한 번 더 보냄
    /// 호출한 쪽에서 결과 UnityWebRequest 를 Dispose 해야 함
    /// </summary>
    private IEnumerator SendWithAuth(Func<string, UnityWebRequest> makeRequest, Action<UnityWebRequest> onDone)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            UnityWebRequest req = makeRequest(AuthManager.IdToken);
            req.timeout = timeoutSeconds;
            yield return req.SendWebRequest();

            if (req.responseCode == 401 && attempt == 0)
            {
                req.Dispose();
                yield return AuthManager.RefreshIdToken();
                continue;
            }

            onDone?.Invoke(req);
            yield break;
        }
    }

    /// <summary>
    /// {"name":"ABC","score":300} 또는 null → 점수
    /// </summary>
    private static int ReadScore(string json)
    {
        if (string.IsNullOrEmpty(json) || !json.TrimStart().StartsWith("{")) return 0;
        ScoreEntry e = JsonUtility.FromJson<ScoreEntry>(json);
        return e != null ? e.score : 0;
    }

    /// <summary>
    /// { "uid1": {"name":..,"score":..}, "uid2": {...} } → 점수 내림차순 목록
    /// JsonUtility 는 키가 바뀌는 딕셔너리를 못 읽으므로, 안쪽 { } 하나하나를 따로 변환
    /// </summary>
    private static ScoreEntry[] ParseEntries(string json)
    {
        List<ScoreEntry> list = new List<ScoreEntry>();
        if (!string.IsNullOrEmpty(json) && json != "null")
        {
            foreach (Match m in Regex.Matches(json, @"\{[^{}]*\}"))
            {
                ScoreEntry e = JsonUtility.FromJson<ScoreEntry>(m.Value);
                if (e != null && !string.IsNullOrEmpty(e.name)) list.Add(e);
            }
        }
        list.Sort((a, b) => b.score.CompareTo(a.score));
        return list.ToArray();
    }

    #endregion
}

[Serializable]
public class ScoreEntry
{
    public string name;
    public int score;
}

/// <summary>
/// 게임별 TOP3 목록 (인덱스 = 게임 번호)
/// </summary>
public class LeaderboardData
{
    private ScoreEntry[][] games;

    public LeaderboardData(int gameCount)
    {
        games = new ScoreEntry[gameCount][];
        for (int i = 0; i < gameCount; i++) games[i] = new ScoreEntry[0];
    }

    public int Count => games.Length;

    public void Set(int gameIndex, ScoreEntry[] list)
    {
        if (gameIndex >= 0 && gameIndex < games.Length) games[gameIndex] = list ?? new ScoreEntry[0];
    }

    public ScoreEntry[] GetList(int gameIndex)
    {
        return (gameIndex >= 0 && gameIndex < games.Length) ? games[gameIndex] : new ScoreEntry[0];
    }

    public ScoreEntry[] GetListByGameId(string gameId)
    {
        return int.TryParse(gameId, out int i) ? GetList(i) : new ScoreEntry[0];
    }
}

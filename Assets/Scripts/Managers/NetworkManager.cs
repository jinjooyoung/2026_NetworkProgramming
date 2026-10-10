using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// CS0246 에러를 해결하기 위한 데이터 구조 클래스들
[System.Serializable]
public class ScoreEntry
{
    public string name;
    public int score;
}

[System.Serializable]
public class LeaderboardData
{
    public ScoreEntry[] game1; // 0번 게임 (카드 짝맞추기)
    public ScoreEntry[] game2; // 1번 게임 (스피드 분리수거)
    public ScoreEntry[] game3; // 2번 게임 (후르츠 슬라이스)

    public ScoreEntry[] GetListByGameId(string gameId)
    {
        if (gameId == "0" || gameId == "game1") return game1;
        if (gameId == "1" || gameId == "game2") return game2;
        if (gameId == "2" || gameId == "game3") return game3;
        return new ScoreEntry[0];
    }
}

[System.Serializable]
public class PostData
{
    public string gameId;
    public string name;
    public int score;
}

[System.Serializable]
public class EncryptedPayload
{
    public string data;
}

// CS0103 에러를 해결하기 위한 NetworkManager 매니저 클래스
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;
    public string serverUrl = "https://2026-networkprogramming-omnibus-server.onrender.com";
    private const string SECRET_KEY = "OmnibusKey";

    public LeaderboardData cachedLeaderboard;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private string Encrypt(string text)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
            sb.Append((char)(text[i] ^ SECRET_KEY[i % SECRET_KEY.Length]));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private string Decrypt(string base64)
    {
        try
        {
            string text = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
                sb.Append((char)(text[i] ^ SECRET_KEY[i % SECRET_KEY.Length]));
            return sb.ToString();
        }
        catch
        {
            return "";
        }
    }

    public IEnumerator GetLeaderboard(Action<bool> onComplete)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(serverUrl + "/leaderboard"))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                EncryptedPayload payload = JsonUtility.FromJson<EncryptedPayload>(www.downloadHandler.text);
                string json = Decrypt(payload.data);
                cachedLeaderboard = JsonUtility.FromJson<LeaderboardData>(json);
                onComplete?.Invoke(true);
            }
            else
            {
                onComplete?.Invoke(false);
            }
        }
    }

    public IEnumerator PostScore(string gameId, string name, int score, Action<bool> onComplete)
    {
        PostData data = new PostData { gameId = gameId, name = name, score = score };
        string json = JsonUtility.ToJson(data);
        EncryptedPayload payload = new EncryptedPayload { data = Encrypt(json) };

        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/leaderboard", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                EncryptedPayload resPayload = JsonUtility.FromJson<EncryptedPayload>(www.downloadHandler.text);
                string resJson = Decrypt(resPayload.data);
                cachedLeaderboard = JsonUtility.FromJson<LeaderboardData>(resJson);
                onComplete?.Invoke(true);
            }
            else
            {
                onComplete?.Invoke(false);
            }
        }
    }

    public int GetCutoffScore(string gameId)
    {
        if (cachedLeaderboard == null) return 0;
        ScoreEntry[] list = cachedLeaderboard.GetListByGameId(gameId);
        if (list == null || list.Length < 3) return 0;
        return list[2].score;
    }
}
using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ScoreEntry
{
    public string name;
    public int score;
}

[System.Serializable]
public class LeaderboardData
{
    public ScoreEntry[] game1;
    public ScoreEntry[] game2;
    public ScoreEntry[] game3;

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

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;
    public string serverUrl = "https://two026-networkprogramming-omnibus-server.onrender.com";
    private const string SECRET_KEY = "OmnibusKey";

    public LeaderboardData cachedLeaderboard;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            StartCoroutine(GetLeaderboard(null));
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
            www.timeout = 60;

            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    EncryptedPayload payload = JsonUtility.FromJson<EncryptedPayload>(www.downloadHandler.text);
                    string json = Decrypt(payload.data);

                    // JSON 키값 매핑 처리 ("0" -> "game1")
                    json = json.Replace("\"0\":", "\"game1\":")
                               .Replace("\"1\":", "\"game2\":")
                               .Replace("\"2\":", "\"game3\":");

                    cachedLeaderboard = JsonUtility.FromJson<LeaderboardData>(json);

                    int len1 = cachedLeaderboard?.game1 != null ? cachedLeaderboard.game1.Length : 0;
                    Debug.Log($"[GetLeaderboard 성공] Game1: {len1}개 항목 정상 파싱됨!");

                    onComplete?.Invoke(true);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GetLeaderboard 예외 발생] {ex.Message}");
                    onComplete?.Invoke(false);
                }
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

            www.timeout = 60;

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    EncryptedPayload resPayload = JsonUtility.FromJson<EncryptedPayload>(www.downloadHandler.text);
                    string resJson = Decrypt(resPayload.data);

                    if (string.IsNullOrEmpty(resJson))
                    {
                        onComplete?.Invoke(false);
                        yield break;
                    }

                    // JSON 키값 매핑 처리 ("0" -> "game1")
                    resJson = resJson.Replace("\"0\":", "\"game1\":")
                                     .Replace("\"1\":", "\"game2\":")
                                     .Replace("\"2\":", "\"game3\":");

                    cachedLeaderboard = JsonUtility.FromJson<LeaderboardData>(resJson);
                    Debug.Log("[PostScore 성공] LeaderboardData 캐시 정상 갱신 완료!");

                    onComplete?.Invoke(true);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PostScore 실패] JSON 파싱 예외 발생: {ex.Message}");
                    onComplete?.Invoke(false);
                }
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
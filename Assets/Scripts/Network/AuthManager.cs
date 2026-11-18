using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// Firebase 익명(게스트) 로그인 - 10강 실습 5의 AuthManager 로직 기반
/// - 처음 로그인 : accounts:signUp 으로 새 손님 가입
/// - 다음 로그인 : 저장해 둔 refreshToken 으로 같은 손님의 새 idToken 발급
/// - Uid / IdToken / Name 은 static 이라 씬이 바뀌어도 남음
///
/// [수업 코드와 다른 점]
/// - Login 씬 대신 MainLobby 씬 위의 로그인 캔버스(loginCanvas)를 켜고 끔
/// - 로그인 / 로그아웃 후 이동하는 씬이 Menu·Login → MainLobby
/// - RefreshIdToken() : idToken 이 1시간 뒤 만료되면 ScoreSaver 가 자동으로 재발급 (2.14)
/// </summary>
public class AuthManager : MonoBehaviour
{
    private const string REFRESH_TOKEN_KEY = "refreshToken";

    [Header("Firebase 웹 API 키 (AIza로 시작하는 39자)")]
    public string apiKey;

    [Header("UI")]
    public GameObject loginCanvas;  // 로그인 전에만 켜지는 캔버스
    public TMP_Text statusText;     // 로그인 상태 글자

    public static string Uid = "", IdToken = "", Name = ""; // static: 씬이 바뀌어도 남음
    public static bool IsLoggedIn => !string.IsNullOrEmpty(Uid);

    private static string savedApiKey = ""; // 다른 씬에서 토큰 재발급할 때 사용
    private bool isRequesting = false;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(apiKey)) savedApiKey = apiKey;
    }

    private void Start()
    {
        // 이미 로그인한 상태로 로비에 돌아왔으면 로그인 캔버스를 띄우지 않음
        if (loginCanvas != null) loginCanvas.SetActive(!IsLoggedIn);
        if (statusText != null) statusText.text = "";
    }

    /// <summary>
    /// [게스트 로그인] 버튼 OnClick
    /// </summary>
    public void GuestLogin()
    {
        if (isRequesting) return; // 버튼 연타 방지
        if (statusText != null) statusText.text = "로그인 중...";
        StartCoroutine(SignIn());
    }

    /// <summary>
    /// 로그아웃 : static 값만 비우고 로비를 다시 불러옴.
    /// 저장된 refreshToken 은 남아 있어 다시 로그인하면 같은 손님.
    /// </summary>
    public static void Logout()
    {
        Uid = ""; IdToken = ""; Name = "";
        if (ScoreSaver.Instance != null) ScoreSaver.Instance.ClearMyBests();
        SceneManager.LoadScene(SceneNames.MainLobby);
    }

    private IEnumerator SignIn()
    {
        isRequesting = true;

        string saved = PlayerPrefs.GetString(REFRESH_TOKEN_KEY, ""); // 저장해 둔 증명서. 처음이면 빈 문자열
        string signUp = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + apiKey;
        string refresh = "https://securetoken.googleapis.com/v1/token?key=" + apiKey;

        using (UnityWebRequest req = saved == "" // 처음: 새 손님 가입 / 아니면: 같은 손님의 새 토큰
            ? UnityWebRequest.Post(signUp, "{\"returnSecureToken\":true}", "application/json")
            : UnityWebRequest.Post(refresh, "grant_type=refresh_token&refresh_token=" + saved, "application/x-www-form-urlencoded"))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                // 저장값이 무효(콘솔에서 사용자 삭제 등)일 수 있으므로 지움 → 한 번 더 누르면 새 손님으로 가입
                PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
                if (statusText != null) statusText.text = "로그인 실패 : " + req.error + "\n다시 눌러주세요.";
                Debug.LogWarning("[AuthManager] " + req.error + "\n" + req.downloadHandler.text);
                isRequesting = false;
                yield break;
            }

            AuthResponse r = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
            Uid = saved == "" ? r.localId : r.user_id;
            IdToken = saved == "" ? r.idToken : r.id_token;
            PlayerPrefs.SetString(REFRESH_TOKEN_KEY, saved == "" ? r.refreshToken : r.refresh_token);
            PlayerPrefs.Save();
            Name = "손님 " + Uid.Substring(0, 6);
        }

        isRequesting = false;
        SceneManager.LoadScene(SceneNames.MainLobby); // 로그인 끝 → 로비를 다시 불러와 랭킹·이름 표시
    }

    /// <summary>
    /// idToken 재발급 (발급 1시간 뒤 만료되면 저장 요청이 401). ScoreSaver 가 StartCoroutine 으로 호출.
    /// </summary>
    public static IEnumerator RefreshIdToken()
    {
        string saved = PlayerPrefs.GetString(REFRESH_TOKEN_KEY, "");
        if (saved == "" || savedApiKey == "") yield break;

        string refresh = "https://securetoken.googleapis.com/v1/token?key=" + savedApiKey;
        using (UnityWebRequest req = UnityWebRequest.Post(refresh, "grant_type=refresh_token&refresh_token=" + saved, "application/x-www-form-urlencoded"))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[AuthManager] 토큰 재발급 실패 : " + req.error);
                yield break;
            }

            AuthResponse r = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
            IdToken = r.id_token;
            if (!string.IsNullOrEmpty(r.refresh_token))
            {
                PlayerPrefs.SetString(REFRESH_TOKEN_KEY, r.refresh_token);
                PlayerPrefs.Save();
            }
            Debug.Log("[AuthManager] idToken 재발급 완료");
        }
    }
}

// 응답 JSON 의 키 이름과 똑같은 필드 (signUp 응답: camelCase / token 응답: snake_case)
[System.Serializable]
public class AuthResponse { public string idToken, localId, refreshToken, id_token, user_id, refresh_token; }

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임별 최초 플레이 튜토리얼 패널 (TutorialCanvas 프리팹 루트에 부착)
/// - PlayerPrefs "firstPlay_{gameIndex}" 가 1이 아니면(0 또는 없음) 패널을 띄우고 Time.timeScale = 0
/// - 확인 버튼 → 키를 1로 저장 → Time.timeScale = 1 → 캔버스 비활성화
/// - GameStateManager의 카운트다운은 WaitForSeconds(스케일 시간)이므로 튜토리얼 동안 자동으로 멈춰 있음
/// </summary>
public class TutorialPanel : MonoBehaviour
{
    private const string KEY_FORMAT = "firstPlay_{0}";

    [Header("게임 번호 (-1이면 씬의 GameStateManager.gameIndex 자동 사용)")]
    [SerializeField] private int gameIndex = -1;

    [Header("설명 문구 (비워두면 Text_Desc에 적힌 글 그대로 사용)")]
    [TextArea(2, 4)]
    [SerializeField] private string description = "";

    [Header("UI 참조")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform box;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button confirmButton;

    [Header("연출")]
    [SerializeField] private float fadeDuration = 0.2f;

    private bool isClosing = false;

    public static string GetKey(int index) => string.Format(KEY_FORMAT, index);
    public static bool HasPlayed(int index) => PlayerPrefs.GetInt(GetKey(index), 0) == 1;

    private void Start()
    {
        int index = ResolveGameIndex();

        if (HasPlayed(index))
        {
            // 이미 플레이한 적 있음 → 튜토리얼 없이 바로 진행
            gameObject.SetActive(false);
            return;
        }

        Open();
    }

    private int ResolveGameIndex()
    {
        if (gameIndex >= 0) return gameIndex;
        if (GameStateManager.Instance != null) return GameStateManager.Instance.gameIndex;
        return 0;
    }

    private void Open()
    {
        isClosing = false;
        if (descriptionText != null && !string.IsNullOrEmpty(description))
            descriptionText.text = description;
        if (confirmButton != null) confirmButton.interactable = true;

        Time.timeScale = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            DOTweenManager.Fade(canvasGroup, 1f, fadeDuration);
        }
        if (box != null) DOTweenManager.PopIn(box, Vector3.one);
    }

    /// <summary>
    /// [확인] 버튼 OnClick 연결용
    /// </summary>
    public void OnClickConfirm()
    {
        if (isClosing) return;
        isClosing = true;

        // 1. 최초 플레이 기록 저장
        PlayerPrefs.SetInt(GetKey(ResolveGameIndex()), 1);
        PlayerPrefs.Save();

        if (confirmButton != null) confirmButton.interactable = false;

        // 2. 페이드 아웃 후 타임스케일 복구 + 캔버스 비활성화
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            DOTweenManager.Fade(canvasGroup, 0f, fadeDuration).OnComplete(Close);
        }
        else
        {
            Close();
        }
    }

    private void Close()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    // 발표 시연용 : 컴포넌트 우클릭(⋮) 메뉴에서 실행하면 모든 게임의 튜토리얼이 다시 뜸
    [ContextMenu("모든 게임 튜토리얼 기록 초기화")]
    private void ResetAllTutorialRecords()
    {
        for (int i = 0; i < 20; i++) PlayerPrefs.DeleteKey(GetKey(i));
        PlayerPrefs.Save();
        Debug.Log("[TutorialPanel] firstPlay_0 ~ 19 기록 초기화 완료");
    }
}

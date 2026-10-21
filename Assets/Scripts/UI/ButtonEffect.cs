using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼 오브젝트에 붙이는 공통 클릭 피드백
///  - 누를 때 : pressScale배로 줄어듦
///  - 뗄 때   : overshootScale배까지 튀었다가 1.0으로 돌아옴 (뽀잉!)
///  - 클릭 시 : 버튼 클릭 사운드
/// Time.timeScale = 0 (설정 팝업, 튜토리얼) 상태에서도 동작합니다.
/// </summary>
[DisallowMultipleComponent]
public class ButtonEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("스케일 연출")]
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float overshootScale = 1.05f;
    [SerializeField] private float releaseDuration = 0.15f;

    [Header("사운드")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SFXType clickSound = SFXType.ButtonClick;

    private Button button;
    private Vector3 baseScale;
    private bool isPressed = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        baseScale = transform.localScale;
        if (button != null) button.onClick.AddListener(OnClicked);
    }

    private void OnDisable()
    {
        // 팝업이 닫히는 등 비활성화될 때 크기가 줄어든 상태로 남지 않도록 복구
        isPressed = false;
        DOTweenManager.KillScale(transform);
        transform.localScale = baseScale;
    }

    private bool IsInteractable()
    {
        return button == null || button.IsInteractable();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        isPressed = true;
        DOTweenManager.PressDown(transform, baseScale, pressScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;
        isPressed = false;
        DOTweenManager.Release(transform, baseScale, overshootScale, releaseDuration);
    }

    private void OnClicked()
    {
        if (playClickSound && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(clickSound);
    }
}

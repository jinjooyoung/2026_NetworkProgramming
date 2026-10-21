using System.Collections;
using UnityEngine;

public class TrashObject : MonoBehaviour
{
    public TrashDataSO trashData;
    public SpriteRenderer spriteRenderer;
    private Vector3 originPos;
    private bool isDragging = false;
    private Vector3 baseScale = Vector3.one; // [신규] 집기 연출 기준 크기

    // [신규]
    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public void Setup(TrashDataSO so)
    {
        trashData = so;
        if (spriteRenderer != null && so != null)
            spriteRenderer.sprite = so.trashSprite;
    }

    private void OnMouseDown()
    {
        // [변경] isGameStarted 체크 → IsInputBlocked (일시정지 중 드래그 시작 차단)
        if (GameStateManager.IsInputBlocked) return;

        // 드래그 시작 시 원위치 기억
        originPos = transform.position;
        isDragging = true;

        PlaySFX(SFXType.TrashPick); // [신규] 꾸깃
        DOTweenManager.PunchBounce(transform, baseScale, 1f, 1.15f, 0.2f, false); // [신규] 집는 느낌
    }

    private void OnMouseDrag()
    {
        if (!isDragging || Time.timeScale <= 0f) return; // [변경] 일시정지 중에는 끌려가지 않음
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = originPos.z;
        transform.position = mouse;
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // 1. 자기 자신의 콜라이더를 잠시 끔 (Self-Hit 방지)
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null) myCollider.enabled = false;

        // 2. 3D 화면 지점 마우스 레이 발사
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.Log("3D 레이 발사");

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            SpaceZone zone = hit.collider.GetComponent<SpaceZone>();

            // 스페이스존 컴포넌트가 존재할 경우
            if (zone != null)
            {
                DOTweenManager.PunchBounce(zone.transform, Vector3.one, 1f, 1.15f, 0.2f, false);

                bool isCorrect = (zone.spaceType == trashData.trashType);

                // 점수 가감
                PlaySFX(SFXType.TrashDrop); // [신규] 탱그랑
                GameStateManager.Instance.AddScore(isCorrect ? 50 : -30);

                // 리셋 진행
                var manager = FindFirstObjectByType<Game1_RecycleManager>();
                if (manager != null)
                {
                    manager.ResetTrashObject(this, originPos);
                }

                // 콜라이더 복구 후 리턴
                if (myCollider != null) myCollider.enabled = true;
                return;
            }
        }

        // 3. 콜라이더 다시 복구
        if (myCollider != null) myCollider.enabled = true;

        // 스페이스존이 아니거나 감지된 것이 없으면 원위치 복귀
        transform.position = originPos;
    }

    // [신규]
    private void PlaySFX(SFXType type)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(type);
    }
}
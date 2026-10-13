using System.Collections;
using UnityEngine;

public class TrashObject : MonoBehaviour
{
    public TrashDataSO trashData;
    public SpriteRenderer spriteRenderer;
    private Vector3 originPos;
    private bool isDragging = false;

    public void Setup(TrashDataSO so)
    {
        trashData = so;
        if (spriteRenderer != null && so != null)
            spriteRenderer.sprite = so.trashSprite;
    }

    private void OnMouseDown()
    {
        if (!GameStateManager.Instance.isGameStarted) return;

        // 드래그 시작 시 원위치 기억
        originPos = transform.position;
        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;
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
                bool isCorrect = (zone.spaceType == trashData.trashType);

                // 점수 가감
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
}
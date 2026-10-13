using System.Collections;
using UnityEngine;

public class TrashObject : MonoBehaviour
{
    public TrashDataSO trashData;
    public SpriteRenderer spriteRenderer;
    public Collider2D col2D;
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

        // 놓았을 때 해당 위치로 Ray2D 발사
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero);

        if (hit.collider != null)
        {
            SpaceZone zone = hit.collider.GetComponent<SpaceZone>();

            // 스페이스존 컴포넌트가 존재하는 경우
            if (zone != null)
            {
                bool isCorrect = (zone.spaceType == trashData.trashType);

                // 점수 가감 (일치: +50, 불일치: -30)
                GameStateManager.Instance.AddScore(isCorrect ? 50 : -30);

                // 매니저를 통해 비활성화 및 리셋 코루틴 실행
                var manager = FindObjectOfType<Game1_RecycleManager>();
                if (manager != null)
                {
                    manager.ResetTrashObject(this, originPos);
                }
                return;
            }
        }

        // 스페이스존이 아니거나 히트된 것이 없으면 원위치로 되돌리기만 함
        transform.position = originPos;
    }
}
using System.Collections;
using UnityEngine;

// 1번 게임: 쓰레기 오브젝트 개별 드래그 스크립트
public class TrashObject : MonoBehaviour
{
    public TrashDataSO trashData;
    public SpriteRenderer spriteRenderer;
    private Vector3 originPos;
    private bool isDragging = false;

    public void Setup(TrashDataSO so)
    {
        trashData = so;
        if (spriteRenderer != null && so != null) spriteRenderer.sprite = so.trashSprite;
        originPos = transform.position;
    }

    private void OnMouseDown()
    {
        if (!GameStateManager.Instance.isGameStarted) return;
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

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero);
        if (hit.collider != null && hit.collider.CompareTag("space"))
        {
            SpaceZone zone = hit.collider.GetComponent<SpaceZone>();
            if (zone != null)
            {
                bool isCorrect = (zone.spaceType == trashData.trashType);
                GameStateManager.Instance.AddScore(isCorrect ? 50 : -30);
                StartCoroutine(ResetRoutine());
                return;
            }
        }
        transform.position = originPos;
    }

    private IEnumerator ResetRoutine()
    {
        transform.position = originPos;
        gameObject.SetActive(false);
        var manager = FindObjectOfType<Game1_RecycleManager>();
        if (manager != null) manager.AssignRandomSO(this);
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(true);
    }
}
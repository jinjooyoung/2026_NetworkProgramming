using UnityEngine;
using DG.Tweening;

public class SliceEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void PlayEffect(Vector3 position, float angle)
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        transform.localScale = new Vector3(0.1f, 1.5f, 1f);

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;

            // 크기가 가로로 촥 늘어나면서 알파값이 0으로 페이드아웃
            transform.DOScaleX(0.8f, 0.15f).SetEase(Ease.OutQuad);
            spriteRenderer.DOFade(0f, 0.15f).OnComplete(() => {
                gameObject.SetActive(false);
            });
        }
    }
}
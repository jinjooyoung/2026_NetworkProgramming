using UnityEngine;

// 2번 게임: 스와이프 슬라이스 자르기 감지용 과일 스크립트
public class FruitObject : MonoBehaviour
{
    public FruitDataSO fruitData;
    public SpriteRenderer spriteRenderer;

    public void Setup(FruitDataSO so)
    {
        fruitData = so;
        if (spriteRenderer != null && so != null) spriteRenderer.sprite = so.fruitSprite;
    }

    public void OnSliced()
    {
        if (!gameObject.activeSelf) return;
        GameStateManager.Instance.AddScore(fruitData != null ? fruitData.score : 50);
        gameObject.SetActive(false);
    }
}
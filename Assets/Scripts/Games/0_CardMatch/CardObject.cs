using UnityEngine;

// 0번 게임: 카드 오브젝트 개별 스크립트
public class CardObject : MonoBehaviour
{
    public CardDataSO cardData;
    public SpriteRenderer spriteRenderer;
    public bool isFront = false;
    public bool isMatched = false;

    public void Setup(CardDataSO so)
    {
        cardData = so;
        isMatched = false;
        isFront = false;
        if (spriteRenderer != null && so != null) spriteRenderer.sprite = so.cardSprite;
    }
}
using UnityEngine;

// 0번 게임: 카드 SO
[CreateAssetMenu(fileName = "CardData", menuName = "Omnibus/CardData")]
public class CardDataSO : ScriptableObject
{
    public int cardIndex;
    public Sprite cardSprite;
}
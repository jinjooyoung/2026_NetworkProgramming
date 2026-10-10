using UnityEngine;

// 2번 게임: 과일 SO
[CreateAssetMenu(fileName = "FruitData", menuName = "Omnibus/FruitData")]
public class FruitDataSO : ScriptableObject
{
    public int score = 50;
    public Sprite fruitSprite;
}
using UnityEngine;

// 2¹ø °ÔÀÓ: °úÀÏ ¹× ÆøÅº SO
[CreateAssetMenu(fileName = "FruitData", menuName = "Omnibus/FruitData")]
public class FruitDataSO : ScriptableObject
{
    public bool isBomb = false; // true¸é ÆøÅº, false¸é ÀÏ¹İ °úÀÏ
    public int score = 50;
    public Sprite fruitSprite;
}
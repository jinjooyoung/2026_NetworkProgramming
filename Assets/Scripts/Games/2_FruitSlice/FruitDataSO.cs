using UnityEngine;

// 2번 게임: 과일 및 폭탄 SO
[CreateAssetMenu(fileName = "FruitData", menuName = "Omnibus/FruitData")]
public class FruitDataSO : ScriptableObject
{
    public bool isBomb = false; // true면 폭탄, false면 일반 과일
    public int score = 50;
    public Sprite fruitSprite;
}
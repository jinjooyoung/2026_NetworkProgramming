using UnityEngine;

public enum TrashType { Plastic, Can, Paper }

// 1번 게임: 쓰레기 SO
[CreateAssetMenu(fileName = "TrashData", menuName = "Omnibus/TrashData")]
public class TrashDataSO : ScriptableObject
{
    public int trashIndex;
    public TrashType trashType;
    public Sprite trashSprite;
}
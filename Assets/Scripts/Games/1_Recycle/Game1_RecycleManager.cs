using System.Collections.Generic;
using UnityEngine;

// 1번 게임: 스피드 분리수거 관리자
public class Game1_RecycleManager : MonoBehaviour
{
    public List<TrashDataSO> allTrashSOs;
    public List<TrashObject> trashObjects; // 5개 지정

    private void Start()
    {
        foreach (var trash in trashObjects)
        {
            AssignRandomSO(trash);
        }
    }

    public void AssignRandomSO(TrashObject trash)
    {
        if (allTrashSOs.Count == 0) return;
        TrashDataSO rndSO = allTrashSOs[Random.Range(0, allTrashSOs.Count)];
        trash.Setup(rndSO);
    }
}
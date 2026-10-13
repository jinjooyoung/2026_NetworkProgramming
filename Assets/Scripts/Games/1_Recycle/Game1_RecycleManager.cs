using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game1_RecycleManager : MonoBehaviour
{
    public List<TrashDataSO> allTrashSOs;
    public List<TrashObject> trashObjects; // 5개 지정
    public float respawnDelay = 0.5f;

    private void Start()
    {
        foreach (var trash in trashObjects)
        {
            AssignRandomSO(trash);
        }
    }

    public void AssignRandomSO(TrashObject trash)
    {
        if (allTrashSOs == null || allTrashSOs.Count == 0) return;
        TrashDataSO rndSO = allTrashSOs[Random.Range(0, allTrashSOs.Count)];
        trash.Setup(rndSO);
    }

    public void ResetTrashObject(TrashObject trash, Vector3 originPos)
    {
        StartCoroutine(ResetTrashRoutine(trash, originPos));
    }

    private IEnumerator ResetTrashRoutine(TrashObject trash, Vector3 originPos)
    {
        // 1. 비활성화
        trash.gameObject.SetActive(false);

        // 2. 원위치로 이동
        trash.transform.position = originPos;

        // 3. SO 다시 할당 및 리소스 셋업
        AssignRandomSO(trash);

        // 4. 딜레이
        yield return new WaitForSeconds(respawnDelay);

        // 5. 오브젝트 재활성화
        trash.gameObject.SetActive(true);
    }
}
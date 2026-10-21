using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 2번 게임: 후르츠 슬라이스 관리자 및 스와이프 감지
public class Game2_FruitSliceManager : MonoBehaviour
{
    [Header("SO 설정")]
    public List<FruitDataSO> allFruitSOs; // 과일 데이터 SO들만 등록
    public FruitDataSO bombSO;           // 폭탄 전용 SO 단일 등록

    [Header("과일 오브젝트 풀")]
    public List<FruitObject> fruitObjects; // 8개

    [Header("스폰 위치 기준")]
    public Transform leftX;
    public Transform rightX;
    public Transform topY;
    public Transform bottomY;

    private Vector2 swipeStart;
    private bool isSwiping = false; // [신규] 일시정지 전에 시작된 스와이프가 재개 후 판정되지 않도록
    private int fruitSpawnStack = 0; // 과일 생성 스택 카운터

    private void Start()
    {
        foreach (var f in fruitObjects)
        {
            f.gameObject.SetActive(false);
        }
        StartCoroutine(SpawnLoopRoutine());
    }

    private IEnumerator SpawnLoopRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (!GameStateManager.Instance.isGameStarted) continue;

            FruitObject inactive = fruitObjects.Find(f => !f.gameObject.activeSelf);
            if (inactive != null)
            {
                SpawnFruit(inactive);
            }
        }
    }

    private void SpawnFruit(FruitObject fruit)
    {
        // 1. 스택 증가
        fruitSpawnStack++;

        FruitDataSO selectedSO = null;

        // 2. 스택이 5가 되면 폭탄 생성 후 스택 리셋
        if (fruitSpawnStack >= 5 && bombSO != null)
        {
            selectedSO = bombSO;
            fruitSpawnStack = 0;
        }
        else
        {
            // 과일 데이터 SO 목록에서 랜덤 추출
            if (allFruitSOs != null && allFruitSOs.Count > 0)
            {
                selectedSO = allFruitSOs[Random.Range(0, allFruitSOs.Count)];
            }
        }

        if (selectedSO == null) return;

        // 3. SO 데이터 세팅
        fruit.Setup(selectedSO);

        // 4. 스폰 위치 및 물리 힘 적용
        bool isLeft = Random.value > 0.5f;
        float spawnX = isLeft ? leftX.position.x : rightX.position.x;
        float spawnY = Random.Range(bottomY.position.y, topY.position.y);

        fruit.transform.position = new Vector3(spawnX, spawnY, 0f);
        fruit.gameObject.SetActive(true);

        Rigidbody2D rb = fruit.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            Vector2 forceDir = isLeft ? new Vector2(Random.Range(2f, 4f), Random.Range(4.5f, 6.5f))
                                      : new Vector2(Random.Range(-4f, -2f), Random.Range(4.5f, 6.5f));
            rb.AddForce(forceDir, ForceMode2D.Impulse);
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SFXType.FruitWhoosh); // [신규] 휙
    }

    private void Update()
    {
        // [변경] isGameStarted 체크 → IsInputBlocked (설정 팝업/튜토리얼로 일시정지 중 입력 차단)
        if (GameStateManager.IsInputBlocked) { isSwiping = false; return; }

        if (Input.GetMouseButtonDown(0)) { swipeStart = Input.mousePosition; isSwiping = true; } // [변경] isSwiping 기록
        if (Input.GetMouseButtonUp(0) && isSwiping)                                              // [변경] && isSwiping
        {
            isSwiping = false;
            Vector2 swipeEnd = Input.mousePosition;
            if (Vector2.Distance(swipeStart, swipeEnd) > 30f)
            {
                CheckSliceHits(swipeStart, swipeEnd);
            }
        }
    }

    private void CheckSliceHits(Vector2 start, Vector2 end)
    {
        Vector3 wStart = Camera.main.ScreenToWorldPoint(start);
        Vector3 wEnd = Camera.main.ScreenToWorldPoint(end);

        RaycastHit2D[] hits = Physics2D.LinecastAll(wStart, wEnd);
        foreach (var hit in hits)
        {
            FruitObject f = hit.collider.GetComponent<FruitObject>();
            if (f != null) f.OnSliced();
        }
    }
}
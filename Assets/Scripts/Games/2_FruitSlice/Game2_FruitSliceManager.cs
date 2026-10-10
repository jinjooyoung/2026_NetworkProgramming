using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 2번 게임: 후르츠 슬라이스 관리자 및 스와이프 감지
public class Game2_FruitSliceManager : MonoBehaviour
{
    public List<FruitDataSO> allFruitSOs;
    public List<FruitObject> fruitObjects; // 8개

    public Transform leftX;
    public Transform rightX;
    public Transform topY;
    public Transform bottomY;

    private Vector2 swipeStart;

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
        FruitDataSO rndSO = allFruitSOs[Random.Range(0, allFruitSOs.Count)];
        fruit.Setup(rndSO);

        bool isLeft = Random.value > 0.5f;
        float spawnX = isLeft ? leftX.position.x : rightX.position.x;
        float spawnY = Random.Range(bottomY.position.y, topY.position.y);

        fruit.transform.position = new Vector3(spawnX, spawnY, 0f);
        fruit.gameObject.SetActive(true);

        Rigidbody2D rb = fruit.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            Vector2 forceDir = isLeft ? new Vector2(Random.Range(3f, 6f), Random.Range(8f, 12f))
                                      : new Vector2(Random.Range(-6f, -3f), Random.Range(8f, 12f));
            rb.AddForce(forceDir, ForceMode2D.Impulse);
        }
    }

    private void Update()
    {
        if (!GameStateManager.Instance.isGameStarted) return;

        if (Input.GetMouseButtonDown(0)) swipeStart = Input.mousePosition;
        if (Input.GetMouseButtonUp(0))
        {
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
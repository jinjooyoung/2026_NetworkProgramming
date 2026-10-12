using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 0번 게임: 카드 짝맞추기 게임 전체 관리자
public class Game0_CardMatchManager : MonoBehaviour
{
    public List<CardDataSO> allCardSOs;
    public List<CardObject> cardObjects; // 8개 지정
    public Transform playerTarget;

    private CardObject firstSelected = null;
    private CardObject secondSelected = null;
    private bool isChecking = false;

    private void Start()
    {
        StartCoroutine(WaitAndInitRoutine());
    }

    private IEnumerator WaitAndInitRoutine()
    {
        yield return new WaitUntil(() => GameStateManager.Instance.isGameStarted);
        InitTurn();
    }

    public void InitTurn()
    {
        isChecking = false;
        firstSelected = null;
        secondSelected = null;

        // 4종 랜덤 추출 및 8개 세팅
        List<CardDataSO> selected4 = new List<CardDataSO>();
        List<CardDataSO> pool = new List<CardDataSO>(allCardSOs);
        for (int i = 0; i < 4; i++)
        {
            int rnd = Random.Range(0, pool.Count);
            selected4.Add(pool[rnd]);
            pool.RemoveAt(rnd);
        }

        List<CardDataSO> cardList8 = new List<CardDataSO>();
        for (int i = 0; i < 4; i++) { cardList8.Add(selected4[i]); cardList8.Add(selected4[i]); }

        // 카드 섞기
        for (int i = 0; i < cardList8.Count; i++)
        {
            int rnd = Random.Range(0, cardList8.Count);
            var temp = cardList8[i]; cardList8[i] = cardList8[rnd]; cardList8[rnd] = temp;
        }

        for (int i = 0; i < cardObjects.Count; i++)
        {
            cardObjects[i].Setup(cardList8[i]);
            cardObjects[i].transform.rotation = Quaternion.identity; // 앞면
        }

        StartCoroutine(HideAllCardsAfterDelay(1f));
    }

    private IEnumerator HideAllCardsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var card in cardObjects)
        {
            card.transform.DORotate(new Vector3(0, 180, 0), 0.1f).SetEase(Ease.InOutQuad);
            card.isFront = false;
        }
    }

    private void Update()
    {
        if (!GameStateManager.Instance.isGameStarted || isChecking) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CardObject card = hit.collider.GetComponent<CardObject>();
                if (card != null && !card.isFront && !card.isMatched && card != firstSelected)
                {
                    FlipCardFront(card);
                }
            }
        }
    }

    private void FlipCardFront(CardObject card)
    {
        card.isFront = true;
        card.transform.DORotate(new Vector3(0, 0, 0), 0.1f).SetEase(Ease.InOutQuad);

        if (firstSelected == null)
        {
            firstSelected = card;
        }
        else if (secondSelected == null)
        {
            secondSelected = card;
            StartCoroutine(CheckMatchRoutine());
        }
    }

    private IEnumerator CheckMatchRoutine()
    {
        // 입력 블록 (두 번째 카드가 완전히 뒤집힌 후 비교 시작)
        isChecking = true;
        yield return new WaitForSeconds(0.15f);

        if (firstSelected.cardData.cardIndex == secondSelected.cardData.cardIndex)
        {
            // 일치했을 때
            GameStateManager.Instance.AddScore(50);
            firstSelected.isMatched = true;
            secondSelected.isMatched = true;

            firstSelected = null;
            secondSelected = null;
            isChecking = false; // 일치하면 즉시 다음 클릭 가능!
        }
        else
        {
            // 불일치했을 때: 플레이어가 두 카드를 확인할 수 있게 0.25초만 보여준 뒤 다시 뒤집기
            yield return new WaitForSeconds(0.25f);

            GameStateManager.Instance.AddScore(-30);

            firstSelected.transform.DORotate(new Vector3(0, 180, 0), 0.1f);
            secondSelected.transform.DORotate(new Vector3(0, 180, 0), 0.1f);

            firstSelected.isFront = false;
            secondSelected.isFront = false;

            firstSelected = null;
            secondSelected = null;
            isChecking = false;
        }

        // 전체 완성 여부 체크
        bool allClear = true;
        foreach (var c in cardObjects)
        {
            if (!c.isMatched)
            {
                allClear = false;
                break;
            }
        }

        if (allClear)
        {
            yield return new WaitForSeconds(0.3f);
            InitTurn();
        }
    }
}
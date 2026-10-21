using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   // [신규]
using TMPro;            // [신규]
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

    // ===================== [신규] 신호등 UI & 턴 연출 =====================
    [Header("[신규] 신호등 UI")]
    public Image signalLight;                       // 원형 신호등 이미지
    public TextMeshProUGUI signalText;              // 신호등 아래 안내 문구 (없어도 됨)
    public Color waitColor = new Color32(0xF4, 0x43, 0x36, 0xFF);   // 빨강 : 배정 & 외우는 중
    public Color goColor = new Color32(0x4C, 0xAF, 0x50, 0xFF);     // 초록 : 맞추기 시작
    public string waitMessage = "기억하세요!";
    public string goMessage = "맞추세요!";

    [Header("[신규] 새 턴 연출")]
    public float flipDuration = 0.15f;   // 카드 한 번 뒤집는 시간
    public float memorizeTime = 1f;      // 앞면을 보여주고 외우게 하는 시간

    private Vector3 signalBaseScale = Vector3.one;
    // ===================================================================

    private void Start()
    {
        if (signalLight != null) signalBaseScale = signalLight.transform.localScale; // [신규]
        SetSignal(false, false);                                                    // [신규] 시작은 빨간불
        StartCoroutine(WaitAndInitRoutine());
    }

    private IEnumerator WaitAndInitRoutine()
    {
        yield return new WaitUntil(() => GameStateManager.Instance.isGameStarted);
        InitTurn();
    }

    /// <summary>
    /// [변경] 새 턴 시작 : 바로 배정하던 방식 → NewTurnRoutine 연출 코루틴으로 진행
    /// </summary>
    public void InitTurn()
    {
        StartCoroutine(NewTurnRoutine());
    }

    /// <summary>
    /// [신규] 새 턴 연출
    /// 1. 빨간불 + 입력 차단
    /// 2. 모든 카드를 동시에 뒷면으로 (리셋되는 느낌)
    /// 3. 뒷면 상태에서 새 카드 배정
    /// 4. 모든 카드를 동시에 앞면으로 → memorizeTime 동안 외우기
    /// 5. 다시 모두 뒷면으로 → 초록불 + 입력 허용
    /// </summary>
    private IEnumerator NewTurnRoutine()
    {
        isChecking = true;
        firstSelected = null;
        secondSelected = null;
        SetSignal(false);

        // 2. 전체 뒷면
        FlipAllCards(180f);
        yield return new WaitForSeconds(flipDuration + 0.1f);

        // 3. 새 카드 배정
        AssignNewCards();

        // 4. 전체 앞면 → 외우기
        FlipAllCards(0f);
        yield return new WaitForSeconds(flipDuration + memorizeTime);

        // 5. 전체 뒷면 → 시작
        FlipAllCards(180f);
        foreach (var card in cardObjects) card.isFront = false;
        yield return new WaitForSeconds(flipDuration);

        SetSignal(true);
        isChecking = false;
    }

    /// <summary>
    /// [신규] 모든 카드를 같은 각도로 동시에 회전 (0 = 앞면, 180 = 뒷면)
    /// </summary>
    private void FlipAllCards(float yAngle)
    {
        foreach (var card in cardObjects)
        {
            card.transform.DOKill();
            card.transform.DORotate(new Vector3(0, yAngle, 0), flipDuration).SetEase(Ease.InOutQuad);
        }
        PlaySFX(SFXType.CardFlip);
    }

    /// <summary>
    /// [신규] 신호등 색상/문구 변경 (false = 빨강 대기, true = 초록 시작)
    /// </summary>
    private void SetSignal(bool canPlay, bool animate = true)
    {
        Color target = canPlay ? goColor : waitColor;

        if (signalLight != null)
        {
            if (animate)
            {
                DOTweenManager.ColorTo(signalLight, target, 0.15f);
                DOTweenManager.PunchBounce(signalLight.transform, signalBaseScale, 0.8f, 1.2f, 0.3f);
            }
            else signalLight.color = target;
        }

        if (signalText != null)
        {
            signalText.text = canPlay ? goMessage : waitMessage;
            signalText.color = target;
        }
    }

    // [신규]
    private void PlaySFX(SFXType type)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(type);
    }

    /// <summary>
    /// [변경] 기존 InitTurn의 카드 배정 부분을 분리 (로직 동일, 즉시 앞면 회전 + 1초 뒤 숨김 호출만 제거)
    /// </summary>
    private void AssignNewCards()
    {
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
            // [변경] 즉시 앞면 회전(rotation = identity) 제거 → NewTurnRoutine의 FlipAllCards(0)가 담당
        }

        // [변경] StartCoroutine(HideAllCardsAfterDelay(1f)) 제거 → NewTurnRoutine이 담당
    }

    // [미사용] NewTurnRoutine으로 대체됨. 기존 코드 보존용으로 남겨둠
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
        // [변경] isGameStarted 체크 → IsInputBlocked (설정 팝업/튜토리얼로 일시정지 중에도 입력 차단)
        if (GameStateManager.IsInputBlocked || isChecking) return;

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
        PlaySFX(SFXType.CardFlip); // [신규]

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
            PlaySFX(SFXType.CardFlip); // [신규]

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
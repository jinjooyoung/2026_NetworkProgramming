using UnityEngine;

// 2번 게임: 스와이프 슬라이스 자르기 감지용 과일 스크립트
public class FruitObject : MonoBehaviour
{
    public FruitDataSO fruitData;
    public SpriteRenderer spriteRenderer;
    public CircleCollider2D circleCollider;

    [Header("이펙트 설정")]
    public SliceEffect sliceEffectPrefab;   // 베는 선 이펙트 프리팹
    public GameObject juiceSplashPrefab;    // 촤악 뿌려지는/원형 파티클 프리팹

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();
    }

    public void Setup(FruitDataSO so)
    {
        fruitData = so;
        if (spriteRenderer != null && so != null && so.fruitSprite != null)
        {
            spriteRenderer.sprite = so.fruitSprite;

            // PPU에 따른 CircleCollider2D 반지름(Radius) 자동 계산
            AdjustColliderRadius(so.fruitSprite.pixelsPerUnit);
        }
    }

    private void AdjustColliderRadius(float ppu)
    {
        if (circleCollider == null) return;

        // [선형 보정 공식] PPU 40 -> 0.8 / PPU 140 -> 0.3
        // Radius = 1.0 - (PPU * 0.005)
        float targetRadius = 1.0f - (ppu * 0.005f);

        // 만약 PPU 범위가 벗어나더라도 안전하도록 최소/최대 범위 제한 (Clamp)
        circleCollider.radius = Mathf.Clamp(targetRadius, 0.2f, 1.0f);
    }

    public void OnSliced()
    {
        if (!gameObject.activeSelf || fruitData == null) return;

        Vector3 currentPos = transform.position;

        // 1. 썰린 위치에 슬라이스 선 이펙트 생성
        if (sliceEffectPrefab != null)
        {
            float randomAngle = Random.Range(-45f, 45f); // 썰리는 대각선 각도
            var fx = Instantiate(sliceEffectPrefab, currentPos, Quaternion.identity);
            fx.PlayEffect(currentPos, randomAngle);
        }

        // 2. 촤악 뿌려지는 파티클 이펙트 생성 (X축 90도 회전 적용)
        if (juiceSplashPrefab != null)
        {
            //Quaternion splashRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject splashFx = Instantiate(juiceSplashPrefab, currentPos, Quaternion.identity);
            //Destroy(splashFx, 2.0f); // 2초 뒤 자동 삭제
        }

        // 3. 점수 및 폭탄 처리
        if (fruitData.isBomb)
        {
            // 폭탄을 썰었을 때: -30점 및 목숨 1 차감
            GameStateManager.Instance.AddScore(-30);
            GameStateManager.Instance.ReduceLife(1);
        }
        else
        {
            // 과일을 썰었을 때: +SO에 설정된 점수
            GameStateManager.Instance.AddScore(fruitData.score);
        }

        gameObject.SetActive(false);
    }
}
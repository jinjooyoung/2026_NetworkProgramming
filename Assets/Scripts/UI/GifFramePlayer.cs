using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity는 GIF 파일을 직접 재생하지 못하므로,
/// ScreenToGif에서 프레임별 PNG로 내보낸 스프라이트들을 순서대로 넘겨서 GIF처럼 보여줍니다.
/// Time.timeScale = 0 인 튜토리얼 중에도 재생되도록 unscaledDeltaTime을 사용합니다.
/// </summary>
[RequireComponent(typeof(Image))]
public class GifFramePlayer : MonoBehaviour
{
    [Header("프레임 스프라이트 (순서대로)")]
    [SerializeField] private List<Sprite> frames = new List<Sprite>();

    [Header("재생 설정")]
    [SerializeField] private float framesPerSecond = 15f;
    [Tooltip("마지막 프레임에서 처음으로 돌아가기 전 대기 시간")]
    [SerializeField] private float loopDelay = 0.5f;
    [SerializeField] private bool loop = true;

    private Image image;
    private int frameIndex = 0;
    private float timer = 0f;

    private void Awake()
    {
        image = GetComponent<Image>();
        image.preserveAspect = true;
    }

    private void OnEnable()
    {
        frameIndex = 0;
        timer = 0f;
        ApplyFrame();
    }

    private void Update()
    {
        if (frames == null || frames.Count <= 1 || framesPerSecond <= 0f) return;

        timer += Time.unscaledDeltaTime;
        float frameTime = 1f / framesPerSecond;
        if (frameIndex == frames.Count - 1) frameTime += loopDelay;

        if (timer < frameTime) return;
        timer -= frameTime;

        if (frameIndex < frames.Count - 1) frameIndex++;
        else if (loop) frameIndex = 0;

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (image == null || frames == null || frames.Count == 0) return;
        if (frames[frameIndex] == null) return;
        image.sprite = frames[frameIndex];
        image.color = Color.white; // 프레임이 들어오면 플레이스홀더 회색 → 흰색
    }

    /// <summary>
    /// 코드로 프레임을 교체할 때 사용
    /// </summary>
    public void SetFrames(List<Sprite> newFrames)
    {
        frames = newFrames;
        frameIndex = 0;
        timer = 0f;
        ApplyFrame();
    }
}

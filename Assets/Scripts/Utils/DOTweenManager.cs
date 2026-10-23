using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 범용 정적 트윈 매니저
/// 버튼, UI, 카메라, 사운드 등 모든 오브젝트에 공통으로 쓰는 표준 피드백 연출 모음
/// 어디서든 DOTweenManager.함수명(...) 으로 바로 호출합니다.
///
/// [공통 규칙]
/// - 같은 대상 + 같은 채널(ID 접미사)의 트윈은 새로 호출하면 이전 트윈을 Kill 하고 다시 시작합니다.
/// - ignoreTimeScale 기본값 true : 튜토리얼/설정 팝업으로 Time.timeScale = 0 이어도 연출이 재생됩니다.
/// - SetLink : 대상 오브젝트가 파괴(씬 전환)되면 트윈도 자동으로 정리됩니다.
/// </summary>
public static class DOTweenManager
{
    // Tween ID 접미사 상수 (트윈 채널 충돌 방지)
    private const string ID_TRANSFORM = "_tr";
    private const string ID_COLOR = "_col";
    private const string ID_SHAKE = "_shake";
    private const string ID_AUDIO = "_audio";
    private const string ID_ROLL = "_roll";
    private const string ID_GROUP = "_group";
    private const string ID_PITCH = "_pitch";

    private static string MakeId(Object target, string suffix) => target.GetInstanceID() + suffix;

    #region 1. 스케일 증감 피드백 (Transform Scale)

    /// <summary>
    /// 대상의 Scale을 목표 크기(절대값)로 변경합니다.
    /// </summary>
    public static Tween Scale(Transform target, Vector3 targetScale, float duration = 0.15f, Ease ease = Ease.OutQuad, bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        string tweenId = MakeId(target, ID_TRANSFORM);
        DOTween.Kill(tweenId);

        return target.DOScale(targetScale, duration)
            .SetEase(ease)
            .SetId(tweenId)
            .SetUpdate(ignoreTimeScale)
            .SetLink(target.gameObject);
    }

    /// <summary>
    /// 대상의 현재 Scale에 배율(Multiplier)을 곱하여 스케일을 변경합니다.
    /// </summary>
    public static Tween ScaleMultiplier(Transform target, float multiplier, float duration = 0.15f, Ease ease = Ease.OutQuad, bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        return Scale(target, target.localScale * multiplier, duration, ease, ignoreTimeScale);
    }

    /// <summary>
    /// [눌림] 기준 크기의 pressScale 배로 빠르게 줄어듭니다. (버튼 PointerDown 용)
    /// </summary>
    public static Tween PressDown(Transform target, Vector3 baseScale, float pressScale = 0.8f, float duration = 0.08f)
    {
        return Scale(target, baseScale * pressScale, duration, Ease.OutQuad);
    }

    /// <summary>
    /// [놓음] 현재 크기 → overshoot 배 → 기준 크기로 "뽀잉" 하고 돌아옵니다. (버튼 PointerUp 용)
    /// </summary>
    public static Tween Release(Transform target, Vector3 baseScale, float overshootScale = 1.2f, float duration = 0.25f, bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        string tweenId = MakeId(target, ID_TRANSFORM);
        DOTween.Kill(tweenId);

        Sequence seq = DOTween.Sequence();
        seq.Append(target.DOScale(baseScale * overshootScale, duration * 0.45f).SetEase(Ease.OutQuad));
        seq.Append(target.DOScale(baseScale, duration * 0.55f).SetEase(Ease.OutBack));
        return seq.SetId(tweenId).SetUpdate(ignoreTimeScale).SetLink(target.gameObject);
    }

    /// <summary>
    /// [한 번에] 기준 크기 → pressScale → overshootScale → 기준 크기 "뽀잉" 연출을 한 번에 재생합니다.
    /// (점수 텍스트, 신호등, 아이콘 등 코드에서 바로 튕겨주고 싶을 때)
    /// </summary>
    public static Tween PunchBounce(Transform target, Vector3 baseScale, float pressScale = 0.8f, float overshootScale = 1.2f, float duration = 0.3f, bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        string tweenId = MakeId(target, ID_TRANSFORM);
        DOTween.Kill(tweenId);

        target.localScale = baseScale;
        Sequence seq = DOTween.Sequence();
        seq.Append(target.DOScale(baseScale * pressScale, duration * 0.25f).SetEase(Ease.OutQuad));
        seq.Append(target.DOScale(baseScale * overshootScale, duration * 0.35f).SetEase(Ease.OutQuad));
        seq.Append(target.DOScale(baseScale, duration * 0.4f).SetEase(Ease.OutBack));
        return seq.SetId(tweenId).SetUpdate(ignoreTimeScale).SetLink(target.gameObject);
    }

    /// <summary>
    /// [등장] 작은 크기에서 기준 크기로 튀어나오며 등장합니다. (팝업 박스 등장 용)
    /// </summary>
    public static Tween PopIn(Transform target, Vector3 baseScale, float fromMultiplier = 0.7f, float duration = 0.3f, bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        target.localScale = baseScale * fromMultiplier;
        return Scale(target, baseScale, duration, Ease.OutBack, ignoreTimeScale);
    }

    /// <summary>
    /// 대상의 스케일 트윈을 즉시 정지합니다.
    /// </summary>
    public static void KillScale(Transform target)
    {
        if (target == null) return;
        DOTween.Kill(MakeId(target, ID_TRANSFORM));
    }

    #endregion

    #region 2. 카메라 흔들림 (Camera Shake)

    /// <summary>
    /// 카메라를 흔듭니다. 연속 호출 시 이전 흔들림을 원위치로 복구한 뒤 다시 흔듭니다.
    /// </summary>
    /// <param name="cam">대상 카메라 (null이면 Camera.main)</param>
    /// <param name="duration">흔들리는 시간</param>
    /// <param name="strength">흔들림 세기 (월드 유닛)</param>
    /// <param name="vibrato">진동 횟수</param>
    public static Tween ShakeCamera(Camera cam = null, float duration = 0.25f, float strength = 0.15f, int vibrato = 25, bool ignoreTimeScale = true)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return null;

        Transform t = cam.transform;
        string tweenId = MakeId(t, ID_SHAKE);
        DOTween.Kill(tweenId, true); // complete = true → 이전 흔들림을 원래 위치로 되돌림

        return t.DOShakePosition(duration, new Vector3(strength, strength, 0f), vibrato, 90f, false, true)
            .SetId(tweenId)
            .SetUpdate(ignoreTimeScale)
            .SetLink(t.gameObject);
    }

    #endregion

    #region 3. 이미지 페이드 인/아웃 (Graphic / CanvasGroup Fade)

    /// <summary>
    /// Graphic(Image, TMP 텍스트 등)의 알파값을 목표값으로 변경합니다.
    /// </summary>
    public static Tween Fade(Graphic target, float toAlpha, float duration = 0.2f, Ease ease = Ease.OutQuad, bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        string tweenId = MakeId(target, ID_COLOR);
        DOTween.Kill(tweenId);

        return target.DOFade(toAlpha, duration)
            .SetEase(ease)
            .SetId(tweenId)
            .SetUpdate(ignoreTimeScale)
            .SetLink(target.gameObject);
    }

    /// <summary>
    /// 알파 0에서 targetAlpha까지 나타납니다.
    /// </summary>
    public static Tween FadeIn(Graphic target, float duration = 0.2f, float targetAlpha = 1f)
    {
        if (target == null) return null;
        Color c = target.color; c.a = 0f; target.color = c;
        return Fade(target, targetAlpha, duration);
    }

    /// <summary>
    /// 현재 알파에서 0까지 사라집니다.
    /// </summary>
    public static Tween FadeOut(Graphic target, float duration = 0.2f)
    {
        return Fade(target, 0f, duration);
    }

    /// <summary>
    /// 0 → peakAlpha → (유지) → 0 으로 번쩍였다 사라집니다.
    /// </summary>
    public static Tween FadeInOut(Graphic target, float peakAlpha = 1f, float inDuration = 0.05f, float holdDuration = 0f, float outDuration = 0.3f, bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        string tweenId = MakeId(target, ID_COLOR);
        DOTween.Kill(tweenId);

        Color c = target.color; c.a = 0f; target.color = c;

        Sequence seq = DOTween.Sequence();
        seq.Append(target.DOFade(peakAlpha, inDuration).SetEase(Ease.OutQuad));
        if (holdDuration > 0f) seq.AppendInterval(holdDuration);
        seq.Append(target.DOFade(0f, outDuration).SetEase(Ease.InQuad));
        return seq.SetId(tweenId).SetUpdate(ignoreTimeScale).SetLink(target.gameObject);
    }

    /// <summary>
    /// CanvasGroup 전체(패널)의 알파값을 변경합니다.
    /// </summary>
    public static Tween Fade(CanvasGroup target, float toAlpha, float duration = 0.2f, Ease ease = Ease.OutQuad, bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        string tweenId = MakeId(target, ID_GROUP);
        DOTween.Kill(tweenId);

        return target.DOFade(toAlpha, duration)
            .SetEase(ease)
            .SetId(tweenId)
            .SetUpdate(ignoreTimeScale)
            .SetLink(target.gameObject);
    }

    /// <summary>
    /// Graphic의 색상을 부드럽게 변경합니다. (신호등 빨강 ↔ 초록 등)
    /// </summary>
    public static Tween ColorTo(Graphic target, Color toColor, float duration = 0.15f, bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        string tweenId = MakeId(target, ID_COLOR);
        DOTween.Kill(tweenId);

        return target.DOColor(toColor, duration)
            .SetId(tweenId)
            .SetUpdate(ignoreTimeScale)
            .SetLink(target.gameObject);
    }

    #endregion

    #region 4. 사운드 페이드 인/아웃 (AudioSource Fade)

    /// <summary>
    /// AudioSource의 볼륨을 목표값으로 변경합니다.
    /// </summary>
    /// <param name="stopAtEnd">true면 페이드가 끝난 뒤 Stop()</param>
    public static Tween FadeAudio(AudioSource source, float toVolume, float duration = 0.5f, bool stopAtEnd = false, bool ignoreTimeScale = true)
    {
        if (source == null) return null;
        string tweenId = MakeId(source, ID_AUDIO);
        DOTween.Kill(tweenId);

        Tween t = source.DOFade(toVolume, duration)
            .SetId(tweenId)
            .SetUpdate(ignoreTimeScale)
            .SetLink(source.gameObject);

        if (stopAtEnd) t.OnComplete(source.Stop);
        return t;
    }

    /// <summary>
    /// 볼륨 0에서 시작해 targetVolume까지 커지며 재생합니다.
    /// </summary>
    public static Tween AudioFadeIn(AudioSource source, float targetVolume, float duration = 0.5f)
    {
        if (source == null) return null;
        source.volume = 0f;
        if (!source.isPlaying) source.Play();
        return FadeAudio(source, targetVolume, duration);
    }

    /// <summary>
    /// 현재 볼륨에서 0까지 줄인 뒤 정지합니다.
    /// </summary>
    public static Tween AudioFadeOut(AudioSource source, float duration = 0.5f)
    {
        return FadeAudio(source, 0f, duration, true);
    }

    /// <summary>
    /// AudioSource의 피치(재생 속도 + 음높이)를 목표값으로 변경합니다. duration 0이면 즉시 적용.
    /// </summary>
    public static Tween PitchAudio(AudioSource source, float toPitch, float duration = 0.5f, bool ignoreTimeScale = true)
    {
        if (source == null) return null;
        string tweenId = MakeId(source, ID_PITCH);
        DOTween.Kill(tweenId);

        if (duration <= 0f) { source.pitch = toPitch; return null; }

        return source.DOPitch(toPitch, duration)
            .SetId(tweenId)
            .SetUpdate(ignoreTimeScale)
            .SetLink(source.gameObject);
    }

    #endregion

    #region 5. 텍스트 숫자 롤링 (Number Rolling)

    /// <summary>
    /// 텍스트의 숫자가 from → to 로 촤라락 굴러가며 바뀝니다.
    /// </summary>
    /// <param name="format">숫자 포맷 (기본: 3자리 콤마)</param>
    public static Tween RollNumber(TMP_Text target, int from, int to, float duration = 0.35f, string format = "#,##0", bool ignoreTimeScale = true)
    {
        if (target == null) return null;
        string tweenId = MakeId(target, ID_ROLL);
        DOTween.Kill(tweenId);

        float value = from;
        target.text = from.ToString(format);

        return DOTween.To(() => value, x =>
            {
                value = x;
                target.text = Mathf.RoundToInt(x).ToString(format);
            }, to, duration)
            .SetEase(Ease.OutQuad)
            .SetId(tweenId)
            .SetUpdate(ignoreTimeScale)
            .SetLink(target.gameObject)
            .OnComplete(() => target.text = to.ToString(format));
    }

    #endregion

    #region 6. 화면 타격 이펙트 (Hit Flash)

    /// <summary>
    /// 화면 전체를 덮는 이미지를 빨갛게 번쩍입니다. (내부적으로 3번 FadeInOut 호출)
    /// </summary>
    /// <param name="overlay">화면 전체를 덮는 Image (RaycastTarget 꺼둘 것)</param>
    /// <param name="peakAlpha">가장 진할 때의 알파값</param>
    public static Tween HitFlash(Graphic overlay, float peakAlpha = 0.35f, float inDuration = 0.05f, float outDuration = 0.3f)
    {
        return HitFlash(overlay, new Color(1f, 0f, 0f, 0f), peakAlpha, inDuration, outDuration);
    }

    /// <summary>
    /// 지정한 색으로 화면을 번쩍입니다.
    /// </summary>
    public static Tween HitFlash(Graphic overlay, Color color, float peakAlpha = 0.35f, float inDuration = 0.05f, float outDuration = 0.3f)
    {
        if (overlay == null) return null;
        color.a = 0f;
        overlay.color = color;
        return FadeInOut(overlay, peakAlpha, inDuration, 0f, outDuration);
    }

    #endregion
}

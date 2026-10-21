using UnityEngine;

/// <summary>
/// 씬에 배치하여 인스펙터 드롭다운으로 선택한 BGM을 자동으로 재생하는 컴포넌트
/// (메인 화면 / 게임 선택 화면용)
/// </summary>
public class BGMPlayer : MonoBehaviour
{
    [Header("재생할 BGM 타입")]
    [SerializeField] private BGMType bgmType = BGMType.MainLobby;

    [Header("페이드 인/아웃 시간 (초)")]
    [Range(0f, 3f)]
    [SerializeField] private float fadeDuration = 0.8f;

    private void Start()
    {
        PlayBGM();
    }

    /// <summary>
    /// 인스펙터에서 지정된 BGM을 SoundManager를 통해 재생합니다.
    /// </summary>
    public void PlayBGM()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[BGMPlayer] SoundManager.Instance를 찾을 수 없습니다.");
            return;
        }
        SoundManager.Instance.PlayBGM(bgmType, fadeDuration);
    }

    /// <summary>
    /// 런타임에 코드로 BGM 타입을 교체하고 싶을 때 호출
    /// </summary>
    public void ChangeBGM(BGMType newBGM, float customFadeDuration = -1f)
    {
        bgmType = newBGM;
        float duration = customFadeDuration >= 0f ? customFadeDuration : fadeDuration;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBGM(bgmType, duration);
    }
}

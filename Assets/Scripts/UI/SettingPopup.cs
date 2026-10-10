using UnityEngine;
using UnityEngine.UI;

public class SettingPopup : MonoBehaviour
{
    public GameObject popupPanel;
    public Slider volumeSlider;
    public Toggle muteToggle;

    private void Start()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(val => AudioListener.volume = val);
        }
        if (muteToggle != null)
        {
            muteToggle.onValueChanged.AddListener(isMuted => AudioListener.pause = isMuted);
        }
    }

    public void TogglePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(!popupPanel.activeSelf);
    }
}
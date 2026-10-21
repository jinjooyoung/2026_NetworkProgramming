using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingPopup : MonoBehaviour
{
    public GameObject popupPanel;
    public Slider volumeSlider;
    public Toggle muteToggle;
    public CanvasGroup canvasGroup;

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
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0;
        }
    }

    public void TogglePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(!popupPanel.activeSelf);

        if (popupPanel.activeSelf) Time.timeScale = 0;
        if (!popupPanel.activeSelf) Time.timeScale = 1;

        if (popupPanel.activeSelf)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
                canvasGroup.alpha = 1;
            }
        }
        else
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                canvasGroup.alpha = 0;
            }
        }
    }

    public void OnClickMain()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneNames.MainLobby); // [변경] 씬 이름 → SceneNames
    }
}
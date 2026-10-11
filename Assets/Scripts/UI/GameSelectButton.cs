using UnityEngine;
using TMPro;

public class GameSelectButton : MonoBehaviour
{
    public int gameIndex;
    public TextMeshProUGUI bestScoreText;

    public void InitButton()
    {
        int best = PlayerPrefs.GetInt($"BestScore{gameIndex}", 0);
        if (bestScoreText != null) bestScoreText.text = $"{best}";
    }
}
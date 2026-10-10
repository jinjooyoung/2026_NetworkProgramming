using UnityEngine;
using UnityEngine.UI;

public class GameSelectButton : MonoBehaviour
{
    public int gameIndex;
    public Text bestScoreText;

    public void InitButton()
    {
        int best = PlayerPrefs.GetInt($"BestScore{gameIndex}", 0);
        if (bestScoreText != null) bestScoreText.text = $"BEST : {best}";
    }
}
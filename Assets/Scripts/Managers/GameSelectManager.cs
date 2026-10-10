using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSelectManager : MonoBehaviour
{
    public List<GameSelectButton> gameButtons;

    private void Start()
    {
        foreach (var btn in gameButtons)
        {
            if (btn != null) btn.InitButton();
        }
    }

    public void OnClickGameButton(int gameIndex)
    {
        SceneManager.LoadScene(gameIndex.ToString());
    }
}
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;
    
    public Text scoreText;
    public Text coinsText;
    public Text finalScoreText;

    public void ShowState(GameState state)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(state == GameState.MENU);
        if (hudPanel != null) hudPanel.SetActive(state == GameState.PLAYING);
        if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GAME_OVER);

        if (state == GameState.GAME_OVER && finalScoreText != null && scoreText != null)
        {
            finalScoreText.text = "Distance: " + scoreText.text;
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = score + "m";
    }

    public void UpdateCoins(int coins)
    {
        if (coinsText != null) coinsText.text = coins.ToString();
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MENU,
    PLAYING,
    GAME_OVER
}

public class GameManager : MonoBehaviour
{
    public PlayerController player;
    public DouyinSDKManager douyinManager;
    public UIManager uiManager;

    private GameState currentState = GameState.MENU;
    private int score = 0;
    private int coins = 0;
    private float startZ = 0f;

    void Start()
    {
        ChangeState(GameState.MENU);
    }

    void Update()
    {
        if (currentState == GameState.PLAYING && player != null)
        {
            float currentZ = player.transform.position.z;
            score = Mathf.FloorToInt(Mathf.Abs(currentZ - startZ));
            
            if (uiManager != null)
            {
                uiManager.UpdateScore(score);
                uiManager.UpdateCoins(coins);
            }
        }
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        
        if (uiManager != null)
        {
            uiManager.ShowState(newState);
        }

        switch (newState)
        {
            case GameState.MENU:
                if (player != null) player.gameObject.SetActive(false);
                break;
            case GameState.PLAYING:
                if (player != null)
                {
                    player.gameObject.SetActive(true);
                    player.ResetVelocity();
                }
                break;
            case GameState.GAME_OVER:
                if (player != null) player.gameObject.SetActive(false);
                if (douyinManager != null) douyinManager.StopRecording();
                break;
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;
    }

    public void GameOver()
    {
        if (currentState == GameState.PLAYING)
        {
            ChangeState(GameState.GAME_OVER);
        }
    }

    public void StartGame()
    {
        score = 0;
        coins = 0;
        if (player != null)
        {
            startZ = player.transform.position.z;
        }
        
        if (douyinManager != null)
        {
            douyinManager.StartRecording();
        }

        ChangeState(GameState.PLAYING);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RevivePlayer()
    {
        if (douyinManager != null)
        {
            douyinManager.ShowRewardedAd(
                () => {
                    ChangeState(GameState.PLAYING);
                    douyinManager.StartRecording();
                    player.currentShield = player.maxShield; // Give full health
                },
                () => {
                    Debug.Log("Ad failed, cannot revive.");
                }
            );
        }
    }

    public void ShareVideo()
    {
        if (douyinManager != null) douyinManager.ShareVideo();
    }
}

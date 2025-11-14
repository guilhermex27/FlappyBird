using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public Player player;
    public GameEventManager gameEventManager;
    public Spawner spawner;
    public Text scoreText;
    public GameObject PlayButtom;
    public GameObject gameOver;
    private int score;
    private void Awake()
    {
        Application.targetFrameRate = 60;

        Pause();
    }

    public void Play()
    {
        score = 0;
        if (scoreText != null) {
            scoreText.text = score.ToString();
        }

        if (PlayButtom != null) PlayButtom.SetActive(false);
        if (gameOver != null) gameOver.SetActive(false);

        player.enabled = true;
        player.ResetPlayer();

        if (gameEventManager != null) {
            gameEventManager.currentEventTimer = 0f;
            gameEventManager.eventInterval = Random.Range(45f, 70f);
            gameEventManager.eventTimer = gameEventManager.eventInterval;
        }
        
        PipesGreen[] pipesGreen = FindObjectsOfType<PipesGreen>();

        for (int i = 0; i < pipesGreen.Length; i++) {
            Destroy(pipesGreen[i].gameObject);
        }

        PipesRed[] pipes = FindObjectsOfType<PipesRed>();

        for (int i = 0; i < pipes.Length; i++) {
            Destroy(pipes[i].gameObject);
        }

        PipesEvent[] pipesEvents = FindObjectsOfType<PipesEvent>();
        for (int i = 0; i < pipesEvents.Length; i++) {
            Destroy(pipesEvents[i].gameObject);
        }

        if (spawner != null) {
            spawner.cooldownTimeRed = 10f * 1.5f;
        }
    }

    public void Pause()
    {
        player.enabled = false;
    }

    public void GameOver()
    {
        gameOver.SetActive(true);
        PlayButtom.SetActive(true);

        Pause();
    }
    public void IncreaseScore()
    {
        score++;
        scoreText.text = score.ToString();
    }
}

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
    private void Awake() {
        Application.targetFrameRate = 60;

        Pause();
    }

    public void Play()
    {
        score = 0;
        scoreText.text = score.ToString();

        PlayButtom.SetActive(false);
        gameOver.SetActive(false);

        Time.timeScale = 1f;
        player.enabled = true;

        gameEventManager.currentEventTimer = 0f;
        gameEventManager.eventInterval = Random.Range(45f,70f);
        gameEventManager.eventTimer = gameEventManager.eventInterval;

        PipesGreen[] pipesGreen = FindObjectsOfType<PipesGreen>();
        PipesRed[] pipes = FindObjectsOfType<PipesRed>();
        PipesEvent[] pipesEvents = FindObjectsOfType<PipesEvent>();

        for (int i = 0; i < pipesGreen.Length; i++)
        {
            Destroy(pipesGreen[i].gameObject);
        }

        for (int i = 0; i < pipes.Length; i++)
        {
            Destroy(pipes[i].gameObject);
        }

        for (int i = 0; i < pipesEvents.Length; i++)
        {
            Destroy(pipesEvents[i].gameObject);
        }

        spawner.cooldownTimeRed = 10f * 1.5f;
    }

    public void Pause()
    {
        Time.timeScale = 0f;
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

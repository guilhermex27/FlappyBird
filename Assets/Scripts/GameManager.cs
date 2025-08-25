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

        PipesGreen[] pipesGreen = FindObjectsOfType<PipesGreen>();
        Pipes[] pipes = FindObjectsOfType<Pipes>();

        for (int i = 0; i < pipesGreen.Length; i++)
        {
            Destroy(pipesGreen[i].gameObject);
        }

        for (int i = 0; i < pipes.Length; i++)
        {
            Destroy(pipes[i].gameObject);
        }

        gameEventManager.eventInterval = Random.Range(20f, 30f);
        gameEventManager.eventTimer = gameEventManager.eventInterval;

        spawner.cooldownTimeRed = Random.Range(15f, 30f);
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

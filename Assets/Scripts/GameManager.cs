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

    // public void Play()
    // {
    //     score = 0;
    //     scoreText.text = score.ToString();

    //     PlayButtom.SetActive(false);
    //     gameOver.SetActive(false);

    //     Time.timeScale = 1f;
    //     player.enabled = true;

    //     player.ResetPlayer();

    //     gameEventManager.currentEventTimer = 0f;
    //     gameEventManager.eventInterval = Random.Range(45f,70f);
    //     gameEventManager.eventTimer = gameEventManager.eventInterval;

    //     PipesGreen[] pipesGreen = FindObjectsOfType<PipesGreen>();
    //     PipesRed[] pipes = FindObjectsOfType<PipesRed>();
    //     PipesEvent[] pipesEvents = FindObjectsOfType<PipesEvent>();

    //     for (int i = 0; i < pipesGreen.Length; i++)
    //     {
    //         Destroy(pipesGreen[i].gameObject);
    //     }

    //     for (int i = 0; i < pipes.Length; i++)
    //     {
    //         Destroy(pipes[i].gameObject);
    //     }

    //     for (int i = 0; i < pipesEvents.Length; i++)
    //     {
    //         Destroy(pipesEvents[i].gameObject);
    //     }

    //     spawner.cooldownTimeRed = 10f * 1.5f;
    // }
    // Dentro de GameManager.cs

    public void Play()
    {
        // Debug.Log("--- INICIANDO NOVO EPISÓDIO ---");
        // Debug.Log("Passo A: Começando a função Play().");

        score = 0;
        if (scoreText != null) {
            scoreText.text = score.ToString();
        } else {
            Debug.LogWarning("AVISO: A referência para scoreText está nula!");
        }
        // Debug.Log("Passo B: Pontuação resetada.");

        if (PlayButtom != null) PlayButtom.SetActive(false);
        if (gameOver != null) gameOver.SetActive(false);
        // Debug.Log("Passo C: Botões da UI desativados.");

        // Time.timeScale = 1f;
        player.enabled = true;
        player.ResetPlayer();
        // Debug.Log("Passo D: Jogador resetado e ativado.");

        if (gameEventManager != null) {
            gameEventManager.currentEventTimer = 0f;
            gameEventManager.eventInterval = Random.Range(45f, 70f);
            gameEventManager.eventTimer = gameEventManager.eventInterval;
        } else {
            Debug.LogWarning("AVISO: A referência para gameEventManager está nula!");
        }
        // Debug.Log("Passo E: Gerenciador de eventos resetado.");

        // Debug.Log("Passo F: Procurando canos para destruir...");
        PipesGreen[] pipesGreen = FindObjectsOfType<PipesGreen>();
        Debug.Log("Encontrados " + pipesGreen.Length + " canos verdes.");
        for (int i = 0; i < pipesGreen.Length; i++) {
            Destroy(pipesGreen[i].gameObject);
        }
        // Debug.Log("Passo G: Canos verdes destruídos.");

        PipesRed[] pipes = FindObjectsOfType<PipesRed>();
        Debug.Log("Encontrados " + pipes.Length + " canos vermelhos.");
        for (int i = 0; i < pipes.Length; i++) {
            Destroy(pipes[i].gameObject);
        }
        // Debug.Log("Passo H: Canos vermelhos destruídos.");

        PipesEvent[] pipesEvents = FindObjectsOfType<PipesEvent>();
        Debug.Log("Encontrados " + pipesEvents.Length + " canos de evento.");
        for (int i = 0; i < pipesEvents.Length; i++) {
            Destroy(pipesEvents[i].gameObject);
        }
        // Debug.Log("Passo I: Canos de evento destruídos.");

        if (spawner != null) {
            spawner.cooldownTimeRed = 10f * 1.5f;
        } else {
            Debug.LogWarning("AVISO: A referência para spawner está nula!");
        }
        // Debug.Log("Passo J: Cooldown do spawner resetado.");
        // Debug.Log("--- FUNÇÃO PLAY() CONCLUÍDA COM SUCESSO! ---");
    }

    public void Pause()
    {
        // Time.timeScale = 0f;
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

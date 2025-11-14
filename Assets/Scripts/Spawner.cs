using System.Diagnostics;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab_pipe_green;
    public GameObject prefab_pipe_red;
    public float spawnRate = 1f;
    public float minHeight = -1f;
    public float maxHeight = 1.5f;
    public float cooldownTimeRed;
    private bool isActive = true;

    private float timer = 0f;

    private void Start()
    {
        // Resetamos o cooldown no início
        cooldownTimeRed = 10f * 1.5f;
    }

    // Usamos FixedUpdate em vez de OnEnable/InvokeRepeating
    void FixedUpdate()
    {
        // Se o spawner estiver desligado, não faz nada
        if (!isActive) return;

        // Adiciona o tempo do passo de física ao nosso timer
        timer += Time.fixedDeltaTime;

        // Se o timer atingiu a taxa de spawn
        if (timer >= spawnRate)
        {
            Spawn();    // Chama a função de spawn
            timer = 0f; // Reseta o timer
        }
    }
    // private void OnEnable()
    // {
    //     InvokeRepeating(nameof(Spawn), spawnRate, spawnRate);
    //     cooldownTimeRed = 10f * 1.5f;
    // }

    // private void OnDisable()
    // {
    //     CancelInvoke(nameof(Spawn));
    // }
    public void SetActiveSpawner(bool value)
    {
        isActive = value;
    }

    private void Spawn()
    {
        if (!isActive) return;

        if (cooldownTimeRed > 0f)
        {
            cooldownTimeRed -= spawnRate;
        }
        // if (cooldownTimeRed <= 0f)
        // {
        //     // GameObject pipes_red = Instantiate(prefab_pipe_red, transform.position, Quaternion.identity);
        //     // pipes_red.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);
        //     // cooldownTimeRed = 10f * 1.5f;
            
        // }
        // else
        // {
        GameObject pipes = Instantiate(prefab_pipe_green, transform.position, Quaternion.identity);
        pipes.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);
        // }

    }
}

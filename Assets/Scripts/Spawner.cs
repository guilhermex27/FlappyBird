using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab_pipe_green;
    public GameObject prefab_pipe_red;
    public float spawnRate = 1f;
    public float minHeight = -1f;
    public float maxHeight = 1.5f;
    public float cooldownTimeRed;
    private void OnEnable()
    {
        InvokeRepeating(nameof(Spawn), spawnRate, spawnRate);
        cooldownTimeRed = Random.Range(15.0f,30.0f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Spawn));
    }

    private void Spawn()
    {
        if (cooldownTimeRed > 0f)
        {
            cooldownTimeRed -= spawnRate;
        }
        if (cooldownTimeRed <= 0f)
        {
            GameObject pipes_red = Instantiate(prefab_pipe_red, transform.position, Quaternion.identity);
            pipes_red.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);
            cooldownTimeRed = Random.Range(15.0f,30.0f);
        }
        else
        {
            GameObject pipes = Instantiate(prefab_pipe_green, transform.position, Quaternion.identity);
            pipes.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);
        }
    
    }
}

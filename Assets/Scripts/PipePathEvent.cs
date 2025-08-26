using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "GameEvents/Caminho Apertado")]
public class TightPathEvent : GameEventSO
{
    public GameObject tightPipePrefab;
    public float eventDuration = 8f;
    public float spawnInterval = 1.2f;
    public float minHeight = -0.5f;
    public float maxHeight = 0.5f;
    private bool isRunning;
    private Coroutine runningCoroutine;
    public override void StartEvent(GameEventManager eventManager)
    {
        if (isRunning) return;
        runningCoroutine = eventManager.StartCoroutine(RunEvent(eventManager));
    }
    private IEnumerator RunEvent(GameEventManager eventManager)
    {
        isRunning = true;

        Spawner spawner = GameObject.FindObjectOfType<Spawner>();
        if (spawner != null) spawner.SetActiveSpawner(false);

        yield return new WaitForSeconds(0.65f);

        float timer = 0f;

        while (timer < eventDuration)
        {
            if (spawner != null)
            {
                Vector3 pos = spawner.transform.position + Vector3.up * Random.Range(minHeight, maxHeight);
                GameObject.Instantiate(tightPipePrefab, pos, Quaternion.identity);
            }

            yield return new WaitForSeconds(spawnInterval);
            timer += spawnInterval;
        }

        yield return new WaitForSeconds(0.65f);

        if (spawner != null) spawner.SetActiveSpawner(true);

        isRunning = false;
        runningCoroutine = null;
    }
    public override void EndEvent(GameEventManager eventManager)
    {
        if (runningCoroutine != null)
        {
            eventManager.StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }

        Spawner spawner = GameObject.FindObjectOfType<Spawner>();
        if (spawner != null) spawner.SetActiveSpawner(true);

        isRunning = false;
    }
}
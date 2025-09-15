using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "GameEvents/Ventania")]
public class WindEvent : GameEventSO
{
    public GameObject windPipePrefab;
    public float backgroundMultiplier = 2f;
    public float groundMultiplier = 2f;
    private float backgroundOriginalVelocity;
    private float groundOriginalVelocity;
    public float spawnInterval = 0.4f;
    public float minHeight = -1.0f;
    public float maxHeight = 1.5f;
    private bool isRunning;
    private Coroutine runningCoroutine;

    [Header("Efeito Visual")]
    public GameObject windPrefab;
    private GameObject windInstance;
    public override void StartEvent(GameEventManager eventManager)
    {
        if (isRunning) return;
        runningCoroutine = eventManager.StartCoroutine(RunEvent(eventManager));
    }
    private IEnumerator RunEvent(GameEventManager eventManager)
    {
        isRunning = true;

        Background background = GameObject.FindObjectOfType<Background>();
        Ground ground = GameObject.FindObjectOfType<Ground>();

        Spawner spawner = GameObject.FindObjectOfType<Spawner>();
        if (spawner != null) spawner.SetActiveSpawner(false);

        if (windPrefab != null && windInstance == null)
        {
            windInstance = GameObject.Instantiate(windPrefab);
        }

        yield return new WaitForSeconds(5f);

        if (background != null && ground != null)
        {
            backgroundOriginalVelocity = background.animationSpeed;
            groundOriginalVelocity = ground.animationSpeed;

            background.animationSpeed *= backgroundMultiplier;
            ground.animationSpeed *= groundMultiplier;
        }

        float timer = 0f;

        while (timer < (duration - 10))
        {
            if (spawner != null)
            {
                Vector3 pos = spawner.transform.position + Vector3.up * Random.Range(minHeight, maxHeight);
                GameObject.Instantiate(windPipePrefab, pos, Quaternion.identity);
            }

            yield return new WaitForSeconds(spawnInterval);
            timer += spawnInterval;
        }

        // if (spawner != null) spawner.SetActiveSpawner(true);

        // isRunning = false;
    }
    public override void EndEvent(GameEventManager eventManager)
    {
        if (runningCoroutine != null)
        {
            eventManager.StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }

        if (windInstance != null)
        {
            GameObject.Destroy(windInstance);
        }

        Background background = GameObject.FindObjectOfType<Background>();
        Ground ground = GameObject.FindObjectOfType<Ground>();

        if (background != null && ground != null)
        {
            background.animationSpeed = backgroundOriginalVelocity;
            ground.animationSpeed = groundOriginalVelocity;
        }

        Spawner spawner = GameObject.FindObjectOfType<Spawner>();
        if (spawner != null) spawner.SetActiveSpawner(true);

        isRunning = false;
    }
}
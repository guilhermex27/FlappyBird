using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Security.AccessControl;

public class FlappyAgent : Agent
{
    private Player player;
    private GameManager gameManager;
    private GameEventManager gameEventManager;
    private PipeController cachedClosestPipe;
    public override void Initialize()
    {
        player = GetComponent<Player>();
        gameManager = FindObjectOfType<GameManager>();
        gameEventManager = FindObjectOfType<GameEventManager>();
    }
    public override void OnEpisodeBegin()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (gameManager != null)
            gameManager.Play();
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;

        if (Input.GetMouseButtonDown(0))
        {
            discreteActions[0] = 1; 
        }
    }
    public void Collided()
    {
        AddReward(-1.0f);
        EndEpisode();
    }
    public void PassedPipe()
    {
        AddReward(1.0f);
        return;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        const float maxVerticalRange = 7.5f;
        const float maxHorizontalDist = 11.5f;
        const float maxGapY = 4.5f;

        float normalizedY, distToPipe, pipeGapY, normalizedVelocity;
        float rawBirdY, rawVerticalVelocity;
        float rawDistToPipe = 0f;
        float rawPipeGapY = 0f;

        var closestPipe = cachedClosestPipe;

        rawBirdY = transform.position.y;
        normalizedY = Mathf.Clamp(rawBirdY / maxVerticalRange, -1f, 1f);

        if(closestPipe != null && closestPipe.transform.position.x > transform.position.x)
        {
            rawDistToPipe = closestPipe.transform.position.x - transform.position.x;
            distToPipe = Mathf.Clamp(rawDistToPipe / maxHorizontalDist, -1f, 1f);

            rawPipeGapY = closestPipe.scoringTrigger.transform.position.y;
            pipeGapY = Mathf.Clamp(rawPipeGapY / maxGapY, -1f, 1f);
        }
        else
        {
            distToPipe = 0f;
            pipeGapY = 0f;
        }
        
        rawVerticalVelocity = player.GetVerticalVelocity();
        normalizedVelocity = Mathf.Clamp(rawVerticalVelocity / 9.8f, -1f, 1f);

        sensor.AddObservation(normalizedY);
        sensor.AddObservation(distToPipe);
        sensor.AddObservation(pipeGapY);
        sensor.AddObservation(normalizedVelocity);

        // --- 5. CÓDIGO DE DEBUG (USANDO AS VARIÁVEIS LOCAIS) ---
        if (StepCount % 1 == 0)
        {
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"--- OBS (Frame {StepCount}) ---");
            logBuilder.AppendLine($"1. Bird Y (Global): {normalizedY:F3} (Raw: {rawBirdY:F2})");
            
            if (closestPipe != null)
            {
                logBuilder.AppendLine($"3. Pipe Dist:       {distToPipe:F3} (Raw: {rawDistToPipe:F2})");
                logBuilder.AppendLine($"4. Pipe Gap Y:      {pipeGapY:F3} (Raw: {rawPipeGapY:F2})");
            }
            else
            {
                logBuilder.AppendLine($"3. Pipe Dist:       {distToPipe:F3} (No Pipe)");
                logBuilder.AppendLine($"4. Pipe Gap Y:      {pipeGapY:F3} (No Pipe)");
            }

            logBuilder.AppendLine($"7. Bird V-Vel:      {normalizedVelocity:F3} (Raw: {rawVerticalVelocity:F2})");
            logBuilder.AppendLine($"-----------------------");

            Debug.Log(logBuilder.ToString());
        }
    }
    private PipeController GetClosestPipe()
    {
        PipeController closest = null;
        float closestDistance = float.MaxValue;

        foreach (var pipe in PipeController.activePipes)
        {
            if (pipe == null || pipe.gameObject == null) continue;
            float distance = pipe.transform.position.x - transform.position.x;
            if (distance > 0 && distance < closestDistance)
            {
                closestDistance = distance;
                closest = pipe;
            }
        }
        return closest;
    }

    void FixedUpdate()
    {
        cachedClosestPipe = GetClosestPipe();

        if (cachedClosestPipe == null || cachedClosestPipe.scoringTrigger == null)
            return;

        AddReward(0.0005f);

        float topY = cachedClosestPipe.upperPipe.transform.position.y - 6.666667f;
        float botY = cachedClosestPipe.lowerPipe.transform.position.y + 6.666667f;

        if (transform.position.y > topY || transform.position.y < botY)
        {
            AddReward(-0.005f);
        }

        float distanceToPipeX = cachedClosestPipe.transform.position.x - transform.position.x;

        if (distanceToPipeX < 2.0f)
        {
            float idealPositionY = cachedClosestPipe.scoringTrigger.transform.position.y;
            float distanceToIdeal = Mathf.Abs(transform.position.y - idealPositionY);

            float rewardForPositioning = (1.0f - Mathf.Clamp(distanceToIdeal, 0, 1)) * 0.005f;
            AddReward(rewardForPositioning);
        }
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];

        switch (action)
        {
            case 0:
                break;
            case 1:
                player.Jump();
                break;
        }
    }
}
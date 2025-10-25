using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class FlappyAgent : Agent
{
    private Player player;
    private GameManager gameManager;
    private GameEventManager gameEventManager;
    public override void Initialize()
    {
        player = GetComponent<Player>();
        gameManager = FindObjectOfType<GameManager>();
        gameEventManager = FindObjectOfType<GameEventManager>();
    }
    public override void OnEpisodeBegin()
    {
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
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            discreteActions[0] = 2;
        }
    }
    public void Collided()
    {
        AddReward(-1.0f);
        EndEpisode();
    }
    public void PassedPipe()
    {
        // AddReward(1.0f);
        return;
    }
public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Altura do pássaro
        sensor.AddObservation(transform.position.y);
        // 2. Força do pulo
        sensor.AddObservation(player.GetStr());

        PipeController closestPipe = GetClosestPipe();

        if (closestPipe == null)
        {
            // Se não há canos, adicionamos valores padrão
            sensor.AddObservation(0f);   // 3. Distância horizontal do cano
            sensor.AddObservation(0f);   // 4. Altura do vão do cano
            // sensor.AddObservation(-1f);  // 5. Tipo do cano (-1 para "nenhum")
            // sensor.AddObservation(0f);   // 6. Velocidade vertical do cano
            sensor.AddObservation(0f);   // 7. Velocidade horizontal do cano (NOVO!)
            // sensor.AddObservation(0f);   // 8. Amplitude de movimento do cano (NOVO!)
            // sensor.AddObservation(0f);  // 9. Frequência de movimento do cano (NOVO!)
        }
        else
        {
            // 3. Distância horizontal até o próximo cano
            sensor.AddObservation(closestPipe.transform.position.x - transform.position.x);
            // 4. Altura do vão do próximo cano
            sensor.AddObservation(closestPipe.scoringTrigger.transform.position.y);
            // 5. Tipo do próximo cano
            // sensor.AddObservation((float)closestPipe.pipeType);
            // 6. Velocidade vertical do próximo cano
            // sensor.AddObservation(closestPipe.GetVerticalVelocity());
            // 7. Velocidade horizontal do próximo cano (NOVO!)
            sensor.AddObservation(closestPipe.speed);

            // sensor.AddObservation(closestPipe.amplitude);                                    // 8. Amplitude (NOVO!)
            // sensor.AddObservation(closestPipe.frequency);
        }

        // 8. Estado do evento ativo
        // sensor.AddObservation((float)gameEventManager.GetCurrentEvent());
    }
    private PipeController GetClosestPipe()
    {
        GameObject[] pipes = GameObject.FindGameObjectsWithTag("Pipe");
        PipeController closest = null;
        float closestDistance = float.MaxValue;

        if (pipes.Length == 0) return null;

        foreach (var pipe in pipes)
        {
            if (pipe == null) continue;

            float distance = pipe.transform.position.x - transform.position.x;
            if (distance > 0 && distance < closestDistance)
            {
                closestDistance = distance;
                closest = pipe.GetComponent<PipeController>();
            }
        }
        return closest;
    }
    void FixedUpdate()
    {
        AddReward(0.001f);

        PipeController closestPipe = GetClosestPipe();

        if (closestPipe == null || closestPipe.scoringTrigger == null)
        {
            return; 
        }
            
        float distanceToPipeX = closestPipe.transform.position.x - transform.position.x;

        if (distanceToPipeX < 3.0f)
        {
            float idealPositionY = closestPipe.scoringTrigger.transform.position.y;
            float distanceToIdeal = Mathf.Abs(transform.position.y - idealPositionY);
            float rewardForPositioning = (1.0f - Mathf.Clamp(distanceToIdeal, 0, 1)) * 0.05f;
            
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
                if (!player.IsSuspended())
                {
                    player.Jump();
                }
                break;
            case 2:
                player.OpenParachute();
                break;
        }
    }
}
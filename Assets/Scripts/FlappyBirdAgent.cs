using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class FlappyAgent : Agent
{
    private Player player;
    private GameManager gameManager;
    private Spawner spawner;

    private float timerAlive = 0f;

    public override void Initialize()
    {
        player = GetComponent<Player>();
        spawner = FindObjectOfType<Spawner>();
        gameManager = FindObjectOfType<GameManager>();
        player.agent = this; // conectar
    }

    public override void OnEpisodeBegin()
    {
        gameManager.Play(); // Reinicia o ambiente
        timerAlive = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Bird position
        sensor.AddObservation(player.transform.position.x);
        sensor.AddObservation(player.transform.position.y);

        // Próximo cano (pega o primeiro objeto Pipe mais à frente)
        GameObject nextPipe = FindClosestPipe();

        if (nextPipe != null)
        {
            sensor.AddObservation(nextPipe.transform.position.x);
            sensor.AddObservation(nextPipe.transform.position.y);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // Gravidade e força de pulo
        sensor.AddObservation(player.gravity);
        sensor.AddObservation(player.strength);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int act = actions.DiscreteActions[0];

        switch (act)
        {
            case 1:
                player.Jump();
                break;
            case 2:
                player.OpenParachute();
                break;
        }

        timerAlive += Time.deltaTime;
        if (timerAlive > 1f)
        {
            AddReward(+0.5f);
            timerAlive = 0f;
        }
    }

    public GameObject FindClosestPipe()
    {
        GameObject[] pipes = GameObject.FindGameObjectsWithTag("Obstacle");
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var p in pipes)
        {
            float dist = p.transform.position.x - player.transform.position.x;
            if (dist > 0 && dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }
        return closest;
    }

    public void RegisterReward(string eventName)
    {
        switch (eventName)
        {
            case "pass_pipe":
                AddReward(+1.0f);
                break;
            case "collision":
                AddReward(-1.0f);
                EndEpisode();
                break;
            case "parachute_before_red":
                AddReward(+2.0f);
                break;
            case "missed_parachute_red":
                AddReward(-2.0f);
                break;
        }
    }
}
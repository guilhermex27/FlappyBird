using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class FlappyAgent : Agent
{
    public float gravity = -9.8f;
    public float jumpStrength = 2.5f;
    public float glideDuration = 0.6f;
    public float glideCooldown = 10f;

    private Vector3 direction;
    private bool isGliding = false;
    private float glideTimer = 0f;
    private float cooldownTimer = 0f;

    private Transform closestPipeTop;
    private Transform closestPipeBottom;

    public override void OnEpisodeBegin()
    {
        // Reinicia o player e o ambiente
        transform.position = new Vector3(-1, 0, 0);
        direction = Vector3.zero;
        isGliding = false;
        glideTimer = 0f;
        cooldownTimer = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        FindClosestPipe();

        // Observações: posição Y do player e posição dos canos
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(closestPipeTop != null ? closestPipeTop.position.y : 0f);
        sensor.AddObservation(closestPipeBottom != null ? closestPipeBottom.position.y : 0f);
        sensor.AddObservation(closestPipeTop != null ? closestPipeTop.position.x - transform.position.x : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];

        if (isGliding)
        {
            glideTimer -= Time.deltaTime;
            if (glideTimer <= 0f)
                isGliding = false;
        }
        else
        {
            if (action == 1) // Pular
                direction = Vector3.up * jumpStrength;

            else if (action == 2 && cooldownTimer <= 0f) // Planar
            {
                isGliding = true;
                glideTimer = glideDuration;
                cooldownTimer = glideCooldown;
                direction.y = 0f;
            }

            direction.y += gravity * Time.deltaTime;
        }

        transform.position += direction * Time.deltaTime;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Recompensas
        AddReward(0.001f); // Recompensa por sobreviver

        if (transform.position.y > 5f || transform.position.y < -5f)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;
        if (Input.GetMouseButtonDown(0)) discreteActions[0] = 1;
        if (Input.GetKeyDown(KeyCode.Space)) discreteActions[0] = 2;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
        {
            AddReward(-1f);
            EndEpisode();
        }
        else if (other.CompareTag("Scoring"))
        {
            AddReward(0.5f);
        }
    }

    private void FindClosestPipe()
    {
        GameObject[] pipes = GameObject.FindGameObjectsWithTag("Pipe");
        float minDist = float.MaxValue;

        foreach (var pipe in pipes)
        {
            float dist = pipe.transform.position.x - transform.position.x;
            if (dist > 0 && dist < minDist)
            {
                minDist = dist;
                Transform top = pipe.transform.Find("Top");
                Transform bottom = pipe.transform.Find("Bottom");
                closestPipeTop = top;
                closestPipeBottom = bottom;
            }
        }
    }
}

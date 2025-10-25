// PipeController.cs
using UnityEngine;

public class PipeController : MonoBehaviour
{
    public enum PipeType
    {
        Green = 0,
        Red = 1,
        Event = 2
    }
    [Header("Parâmetros de Movimento")]
    public float amplitude;
    public float frequency;
    public GameObject scoringTrigger;
    public PipeType pipeType;
    public float speed;
    private Vector3 lastPosition;
    private float verticalVelocity;
    private float horizontalVelocity;
    private PipesGreen pipesGreen;

    void Start()
    {
        lastPosition = transform.position;
        pipesGreen = GetComponent<PipesGreen>();
    }

    void FixedUpdate()
    {
        // Calcula a velocidade vertical (para canos que sobem/descem)
        verticalVelocity = (transform.position.y - lastPosition.y) / Time.fixedDeltaTime;

        // Calcula a velocidade horizontal (para o WindEvent)
        horizontalVelocity = (transform.position.x - lastPosition.x) / Time.fixedDeltaTime;

        // Atualiza a última posição para o próximo cálculo
        lastPosition = transform.position;
        
        speed = pipesGreen.speed;
    }

    public float GetVerticalVelocity()
    {
        return verticalVelocity;
    }

    // <<< Função NOVA para o agente perguntar a velocidade horizontal
    public float GetHorizontalVelocity()
    {
        return horizontalVelocity;
    }
}
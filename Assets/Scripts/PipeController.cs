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
    private Vector3 lastPosition;
    private float verticalVelocity;
    private float horizontalVelocity; // <<< Variável NOVA

    void Start()
    {
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        // Calcula a velocidade vertical (para canos que sobem/descem)
        verticalVelocity = (transform.position.y - lastPosition.y) / Time.fixedDeltaTime;

        // Calcula a velocidade horizontal (para o WindEvent)
        horizontalVelocity = (transform.position.x - lastPosition.x) / Time.fixedDeltaTime; // <<< Linha NOVA

        // Atualiza a última posição para o próximo cálculo
        lastPosition = transform.position;
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
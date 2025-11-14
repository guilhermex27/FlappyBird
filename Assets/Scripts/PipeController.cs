// PipeController.cs
using UnityEngine;
using System.Collections.Generic;
public class PipeController : MonoBehaviour
{
    public static List<PipeController> activePipes = new List<PipeController>();

    void OnEnable()
    {
        if (!activePipes.Contains(this)) activePipes.Add(this);
    }
    void OnDisable()
    {
        activePipes.Remove(this);
    }
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
    public GameObject upperPipe;
    public GameObject lowerPipe;
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
        verticalVelocity = (transform.position.y - lastPosition.y) / Time.fixedDeltaTime;

        horizontalVelocity = (transform.position.x - lastPosition.x) / Time.fixedDeltaTime;

        lastPosition = transform.position;
        
        speed = pipesGreen.speed;
    }

    public float GetVerticalVelocity()
    {
        return verticalVelocity;
    }

    public float GetHorizontalVelocity()
    {
        return horizontalVelocity;
    }
}
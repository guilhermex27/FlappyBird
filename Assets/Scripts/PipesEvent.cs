using UnityEngine;

public class PipesEvent : MonoBehaviour
{
    public float speed = 5f;
    private float leftEdge;

    [Header("Movimento Vertical")]
    private float amplitude = 0.7f;
    private float frequency = 0.7f;

    private Vector3 startPos;

    private PipeController pipeController;

    private void Start()
    {
        pipeController = GetComponent<PipeController>();

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
        startPos = transform.position;

        if (pipeController != null)
        {
            pipeController.amplitude = this.amplitude;
            pipeController.frequency = this.frequency;
        }
    }

    private void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        if (transform.position.x < leftEdge)
        {
            Destroy(gameObject);
        }
    }
}

using UnityEngine;

public class PipesEvent : MonoBehaviour
{
    public float speed = 5f;
    private float leftEdge;

    [Header("Movimento Vertical")]
    private float amplitude = 0.5f;
    private float frequency = 0.5f;

    private Vector3 startPos;

    private void Start()
    {
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
        startPos = transform.position;
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

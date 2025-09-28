using UnityEngine;

public class PipesGreen : MonoBehaviour
{
    public float speed = 5f;
    private float leftEdge;

    [Header("Movimento Vertical")]
    public float amplitude = 0.5f; 
    public float frequency = 2f;

    private Vector3 startPos;

    private void Start()
    {
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
        startPos = transform.position;

        int nA = Random.Range(1, 3);
        int nF = Random.Range(1, 4);

        amplitude = nA * 0.5f;

        frequency = nF * 0.5f;
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
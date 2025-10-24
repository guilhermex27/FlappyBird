using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // Adicione esta linha no topo das variáveis do Player.cs
    private FlappyAgent agent;
    private SpriteRenderer spriteRenderer;
    public Sprite[] sprites;
    public Sprite[] sprite_Paraquedas;
    public Image cooldownImage;
    private int spriteIndex;
    private Vector3 direction;

    public float gravity = -9.8f;

    public float strength = 2.5f; //5
    private bool isSuspended = false;
    private float suspendTimer = 0f;
    private float cooldownTimer = 0f;
    private float cooldownDuration = 10f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        agent = GetComponent<FlappyAgent>();
    }
    private void Start()
    {
        InvokeRepeating(nameof(AnimateSprite), 0.15f, 0.15f);
    }

    private void Update()
    {
        if (isSuspended)
        {
            suspendTimer -= Time.deltaTime;

            if (suspendTimer <= 0f)
            {
                isSuspended = false;
            }
        }
        else
        {
            // if (Input.GetMouseButtonDown(0))
            // {
            //     direction = Vector3.up * strength;
            // }

            // if (Input.GetKeyDown(KeyCode.Space) && cooldownTimer <= 0f)
            // {
            //     isSuspended = true;
            //     suspendTimer = 0.6f;
            //     cooldownTimer = cooldownDuration;

            //     direction.y = 0f;
            // }

            direction.y += gravity * Time.deltaTime;
        }

        transform.position += direction * Time.deltaTime;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = 1f - (cooldownTimer / cooldownDuration);
        }
    }

    private void AnimateSprite()
    {
        if (isSuspended)
        {
            spriteRenderer.sprite = sprite_Paraquedas[0];
        }
        else
        {
            spriteIndex++;
            if (spriteIndex >= sprites.Length)
            {
                spriteIndex = 0;
            }

            spriteRenderer.sprite = sprites[spriteIndex];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Obstacle")
        {
            // FindObjectOfType<GameManager>().GameOver();
            agent.Collided();
        }
        else if (other.gameObject.tag == "Scoring")
        {
            FindObjectOfType<GameManager>().IncreaseScore();
            agent.PassedPipe();
        }
    }

    // private void OnEnable()
    // {
    //     isSuspended = false;
    //     suspendTimer = 0f;
    //     cooldownTimer = 0f;
    //     cooldownDuration = 10f;
    //     Vector3 position = transform.position;
    //     position.y = 0f;
    //     transform.position = position;
    //     direction = Vector3.zero;
    // }

    // Dentro de Player.cs

    // A função OnEnable agora só chama o Reset

    // Adicione esta função dentro de Player.cs

    public void Jump()
    {
        direction = Vector3.up * strength;
    }
    public void OpenParachute()
    {
        if (cooldownTimer <= 0f)
        {
            isSuspended = true;
            suspendTimer = 0.6f;
            cooldownTimer = cooldownDuration;
            direction.y = 0f;
        }
    }
    public float GetVerticalVelocity()
    {
        return direction.y;
    }
    private void OnEnable()
    {
        ResetPlayer();
    }

    // Esta é a nova função que faz o trabalho sujo
    public void ResetPlayer()
    {
        isSuspended = false;
        suspendTimer = 0f;
        cooldownTimer = 0f;
        cooldownDuration = 10f; // Você pode querer manter isso ou resetar
        
        // A parte mais importante: resetar a posição e velocidade
        Vector3 position = transform.position;
        position.y = 0.5f;
        transform.position = position;
        direction = Vector3.zero;
    }

}
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "GameEvents/Chuva")]
public class RainEvent : GameEventSO
{
    private Player player;
    private float originalStrength;
    private float originalGravity;
    public float rainStrengthMultiplier = 0.5f;
    public float rainGravityMultiplier = 1.0f;

    [Header("Efeito Visual")]
    public GameObject rainPrefab;
    private GameObject rainInstance;

    public GameObject rainOverlayPrefab;
    private GameObject rainOverlayInstance;

    public override void StartEvent(GameEventManager manager)
    {
        Debug.Log("Chuva começou");
        player = FindObjectOfType<Player>();

        if (player != null)
        {
            originalStrength = player.strength;
            originalGravity = player.gravity;

            player.strength *= rainStrengthMultiplier;
            player.gravity *= rainGravityMultiplier;
        }

        if (rainPrefab != null && rainInstance == null)
        {
            rainInstance = GameObject.Instantiate(rainPrefab);
        }

        if (rainOverlayPrefab != null && rainOverlayInstance == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            rainOverlayInstance = GameObject.Instantiate(rainOverlayPrefab, canvas.transform);
            rainOverlayInstance.transform.SetAsFirstSibling();

            var fader = rainOverlayInstance.GetComponent<UIFader>();
            if (fader != null) fader.FadeIn(1f);
        }

    }

    public override void EndEvent(GameEventManager manager)
    {
        Debug.Log("Chuva terminou");
        if (player != null)
        {
            player.strength = originalStrength;
            player.gravity = originalGravity;
        }

        if (rainInstance != null)
        {
            GameObject.Destroy(rainInstance);
        }

        if (rainOverlayInstance != null)
        {
            var fader = rainOverlayInstance.GetComponent<UIFader>();
            if (fader != null)
            {
                fader.FadeOut(1f);
                GameObject.Destroy(rainOverlayInstance, 1.1f);
            }
            else
            {
                GameObject.Destroy(rainOverlayInstance);
            }
        }

    }
}
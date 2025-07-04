using UnityEngine;

[RequireComponent(typeof(EnemyAnimatorScript))]
public class NpcController: MonoBehaviour
{
    public NpcKIClient kiClient;
    private EnemyAnimatorScript animatorScript;
    private Renderer enemyRenderer;

    public float movementSpeed = 1f;

    private Vector3 moveDirection = Vector3.zero;
    private string currentAction = "idle";

    void Start()
    {
        animatorScript = GetComponent<EnemyAnimatorScript>();
        enemyRenderer = GetComponentInChildren<Renderer>();
        
        if (kiClient == null)
        {
            kiClient = FindObjectOfType<NpcKIClient>();
        }

        
        if (kiClient != null)
        {
            kiClient.OnKIResponse += HandleKIResponse;
            kiClient.RequestKIResponse("Starte KI-Verhalten.");
        }
        else
        {
            Debug.LogError("KI-Client nicht gesetzt!");
        }

        InvokeRepeating(nameof(SendPeriodicRequest), 10f, 10f);
    }

    void Update()
    {
        if (currentAction == "walk")
        {
            transform.position += moveDirection * (movementSpeed * Time.deltaTime);
        }
    }

    void SendPeriodicRequest()
    {
        kiClient.RequestKIResponse("Was soll der Gegner als nächstes tun?");
    }

    void HandleKIResponse(string action, string direction, string emotion)
    {
        Debug.Log($"[KI] Action: {action}, Direction: {direction}, Emotion: {emotion}");

        // Animation
        switch (action)
        {
            case "walk":
                animatorScript.walk();
                currentAction = "walk";
                break;
            case "coruch":
                animatorScript.crouch();
                currentAction = "crouch";
                break;
            case "wink":
                animatorScript.wink();
                currentAction = "wink";
                break;
            default:
                animatorScript.idle();
                currentAction = "idle";
                break;
        }

        // Bewegung: Richtung setzen
        moveDirection = DirectionToVector(direction);
        if (moveDirection != Vector3.zero)
        {
            // In Bewegungsrichtung drehen
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = targetRotation;
        }

        // Farbe nach Emotion ändern
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = EmotionToColor(emotion);
        }
    }

    Vector3 DirectionToVector(string dir)
    {
        switch (dir.ToLower())
        {
            case "north": return Vector3.forward;
            case "south": return Vector3.back;
            case "east": return Vector3.right;
            case "west": return Vector3.left;
            default: return Vector3.zero;
        }
    }

    Color EmotionToColor(string emotion)
    {
        switch (emotion.ToLower())
        {
            case "scared": return Color.blue;
            case "angry": return Color.red;
            case "happy": return Color.yellow;
            case "calm": return Color.green;
            default: return Color.gray;
        }
    }
}

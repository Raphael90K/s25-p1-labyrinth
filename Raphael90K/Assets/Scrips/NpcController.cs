using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(EnemyAnimator))]
public class NpcController : MonoBehaviour
{
    public NpcKIClient kiClient;
    private EnemyAnimator _animator;
    private Renderer enemyRenderer;

    public float baseSpeed = 1f;
    private float movementSpeed = 1f;

    private Vector3 moveDirection = Vector3.zero;
    private string currentAction = "idle";

    private Transform player; // Spieler-Referenz im Inspector setzen

    private string lastEmotion = "calm";
    private float lastDistanceToPlayer = 0f;
    private float currentDistanceToPlayer = 0f;
    private bool playerInSight = false;
    private float angleToPlayer = 0f;

    private Rigidbody rb;
    
    private FaceTextureSwitcher faceTextureSwitcher;

    void Start()
    {
        faceTextureSwitcher = GetComponent<FaceTextureSwitcher>();

        _animator = GetComponent<EnemyAnimator>();
        enemyRenderer = GetComponentInChildren<Renderer>();

        if (kiClient == null)
            kiClient = FindObjectOfType<NpcKIClient>();


        StartCoroutine(WaitForPlayer());

        lastDistanceToPlayer = Vector3.Distance(transform.position, player.position);
        currentDistanceToPlayer = lastDistanceToPlayer;
        playerInSight = CheckLineOfSight();
        angleToPlayer = GetAngleToPlayer();


        rb = GetComponent<Rigidbody>();
        if (kiClient != null)
        {
            kiClient.OnKIResponse += HandleKIResponse;
        }
        else
        {
            Debug.LogError("KI-Client nicht gesetzt!");
        }

        InvokeRepeating(nameof(SendPeriodicRequest), 0f, 5f);
    }

    IEnumerator WaitForPlayer()
    {
        while (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("Spieler gefunden: " + player.name);
            }

            yield return new WaitForSeconds(0.5f);
        }

        // Erstes Distance-Update, sobald Spieler da ist
        currentDistanceToPlayer = Vector3.Distance(transform.position, player.position);
    }


    void Update()
    {
        currentDistanceToPlayer = Vector3.Distance(transform.position, player.position);
        playerInSight = CheckLineOfSight();
        angleToPlayer = GetAngleToPlayer();

        if (currentAction == "walk" || currentAction == "run")
        {
            Vector3 newPosition = rb.position + moveDirection * (movementSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);
        }
    }

    bool CheckLineOfSight()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        if (Physics.Raycast(transform.position + Vector3.up, dir, out RaycastHit hit, 50f))
        {
            return hit.transform == player;
        }

        return false;
    }

    float GetAngleToPlayer()
    {
        Vector3 toPlayer = (player.position - transform.position).normalized;

        // 0° ist Norden (Z+), im Uhrzeigersinn
        float angle = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;

        if (angle < 0)
            angle += 360f;

        return angle;
    }


    void SendPeriodicRequest()
    {
        string promptJson = BuildNpcStatusJson();
        kiClient.RequestKIResponse(promptJson);
        lastDistanceToPlayer = currentDistanceToPlayer;
    }

    string BuildNpcStatusJson()
    {
        string json = $@"{{
              ""npcLastEmotion"": ""{lastEmotion}"",
              ""npcLastAction"": ""{currentAction}"",
              ""lastDistanceToPlayer"": {lastDistanceToPlayer:F2},
              ""actualDistanceToPlayer"": {currentDistanceToPlayer:F2},
              ""playerDirectionAngle"": {angleToPlayer:F1},
              ""playerInSight"": {playerInSight.ToString().ToLower()}
            }}";

        return json;
    }


    void HandleKIResponse(string action, string direction, string emotion)
    {
        Debug.Log($"[KI] Action: {action}, Direction: {direction}, Emotion: {emotion}");
        float directionAngle = 0f;
        if (!float.TryParse(direction, out directionAngle))
        {
            Debug.LogWarning($"Ungültiger Richtungswert: '{direction}', setze auf 0° (Norden).");
            directionAngle = 0f;
        }


        // Animation
        switch (action)
        {
            case "walk":
                _animator.walk();
                this.movementSpeed = this.baseSpeed * 1f;
                currentAction = "walk";
                break;
            case "crouch":
                _animator.crouch();
                currentAction = "crouch";
                break;
            case "wave":
                _animator.wave();
                currentAction = "wave";
                break;
            case "run":
                _animator.walk();
                this.movementSpeed = this.baseSpeed * 3f;
                currentAction = "run";
                break;
            default:
                _animator.idle();
                currentAction = "idle";
                break;
        }

        lastEmotion = emotion; // merken für nächste Anfrage

        // Bewegung: Richtung setzen
        moveDirection = moveDirection = Quaternion.Euler(0, directionAngle, 0) * Vector3.forward;

        if (moveDirection != Vector3.zero)
        {
            // In Bewegungsrichtung drehen
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = targetRotation;
        }
        // Emotionale Textur setzen
        if (faceTextureSwitcher != null)
        {
            faceTextureSwitcher.SetEmotion(emotion);
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
            case "scared": return Color.magenta;
            case "angry": return Color.red;
            case "happy": return Color.blue;
            case "calm": return Color.green;
            default: return Color.gray;
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Spieler hat NPC erreicht. Starte neu...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

}
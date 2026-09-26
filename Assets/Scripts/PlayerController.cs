using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Network Sync Variables")]
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;

    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score = 0;

    private void Start()
    {
        if (isLocalPlayer)
        {
            GetComponent<Renderer>().material.color = Color.green;
            float randomX = Random.Range(-3f, 3f);
            float randomZ = Random.Range(-3f, 3f);
            transform.position = new Vector3(randomX, 0.5f, randomZ);
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.red;
        }

        UpdateHealthUI(health, health);
        UpdateScoreUI(score, score);
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthUI(oldHealth, newHealth);
    }

    private void OnScoreChanged(int oldScore, int newScore)
    {
        UpdateScoreUI(oldScore, newScore);
    }

    private void UpdateHealthUI(int oldVal, int newVal)
    {
        if (healthSlider != null)
        {
            healthSlider.value = newVal;
        }
    }

    private void UpdateScoreUI(int oldVal, int newVal)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + newVal;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleMovement();

        // Simulasi input keyboard K & L
        if (Input.GetKeyDown(KeyCode.K))
        {
            CmdTakeDamage(10);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            CmdAddScore(5);
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    [Command]
    private void CmdTakeDamage(int damageAmount)
    {
        health = Mathf.Max(0, health - damageAmount);
    }

    [Command]
    private void CmdAddScore(int scoreAmount)
    {
        score += scoreAmount;
    }
}
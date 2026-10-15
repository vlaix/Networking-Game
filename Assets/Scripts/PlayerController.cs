using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Platformer Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float groundCheckDistance = 1.1f;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI scoreText;
    private TextMeshProUGUI winLoseText; // Dicari otomatis, tidak perlu drag manual

    [Header("Network Sync Variables")]
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;

    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score = 0;

    private static readonly Vector3 SpawnPoint = new Vector3(-9f, 3f, 0f);
    private const float FallLimitY = -10f;

    private Rigidbody rb;
    private float moveInput;
    private bool jumpRequested;
    private bool deathRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            GetComponent<Renderer>().material.color = Color.green;
            ResetToSpawn();

            GameObject winTextObj = GameObject.Find("WinLoseText");
            if (winTextObj != null)
            {
                winLoseText = winTextObj.GetComponent<TextMeshProUGUI>();
            }
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.red;
        }

        UpdateHealthUI(health, health);
        UpdateScoreUI(score, score);

        if (winLoseText != null) winLoseText.text = "";

        if (!SharedCamera.activePlayers.Contains(transform))
            SharedCamera.activePlayers.Add(transform);
    }

    private void OnDestroy()
    {
        SharedCamera.activePlayers.Remove(transform);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            jumpRequested = true;
        }

        HandleFacing();


        if (!deathRequested && transform.position.y < FallLimitY)
        {
            deathRequested = true;
            CmdTakeDamage(100);
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        // Gerak lewat velocity (menghormati collision), Y dibiarkan dikelola gravitasi, Z selalu 0
        Vector3 v = rb.velocity;
        v.x = moveInput * moveSpeed;
        v.z = 0f;
        rb.velocity = v;

        if (jumpRequested)
        {
            jumpRequested = false;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Pengaman: paksa Z tetap 0 jika ada yang menggesernya (mis. spawn/NetworkTransform)
        if (Mathf.Abs(rb.position.z) > 0.0001f)
        {
            Vector3 p = rb.position;
            p.z = 0f;
            rb.position = p;
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }

    // Karakter hanya menghadap kanan (+X) atau kiri (-X)
    private void HandleFacing()
    {
        if (Mathf.Abs(moveInput) < 0.01f) return;

        float targetYaw = moveInput > 0f ? 90f : -90f;
        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ResetToSpawn()
    {
        rb.velocity = Vector3.zero;
        rb.position = SpawnPoint;
        transform.position = SpawnPoint;
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthUI(oldHealth, newHealth);

        if (!isLocalPlayer) return;

        if (newHealth <= 0)
        {
            StartCoroutine(TampilkanPesan("YOU LOSE!", Color.red));
            ResetToSpawn();
            CmdResetHealth();
        }
        else
        {
            // Health sudah dipulihkan server -> boleh mati lagi
            deathRequested = false;
        }
    }

    public void PicuKondisiMenang()
    {
        StartCoroutine(TampilkanPesan("YOU WIN!", Color.green));
        ResetToSpawn();
        CmdResetHealth();
    }

    private IEnumerator TampilkanPesan(string pesan, Color warna)
    {
        if (winLoseText != null)
        {
            winLoseText.text = pesan;
            winLoseText.color = warna;
            yield return new WaitForSeconds(3f);
            winLoseText.text = "";
        }
    }

    [Command]
    public void CmdResetHealth() { health = 100; }

    private void OnScoreChanged(int oldScore, int newScore) { UpdateScoreUI(oldScore, newScore); }
    private void UpdateHealthUI(int oldVal, int newVal) { if (healthSlider != null) healthSlider.value = newVal; }
    private void UpdateScoreUI(int oldVal, int newVal) { if (scoreText != null) scoreText.text = "Score: " + newVal; }

    [Command]
    public void CmdTakeDamage(int damageAmount) { health = Mathf.Max(0, health - damageAmount); }

    [Command]
    public void CmdAddScore(int scoreAmount) { score += scoreAmount; }
}
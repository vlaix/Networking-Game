using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    [Header("Platformer Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI scoreText;
    private TextMeshProUGUI winLoseText; // Dicari otomatis, tidak perlu drag manual

    [Header("Network Sync Variables")]
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;

    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score = 0;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (isLocalPlayer)
        {
            GetComponent<Renderer>().material.color = Color.green;
            // Spawn di X = -9 sesuai permintaan Anda
            transform.position = new Vector3(-9f, 3f, 0f); 

            // OTOMATIS MENCARI TEXT UI BERDASARKAN NAMA DI SCENE
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

        SharedCamera.activePlayers.Add(transform);
    }

    private void OnDestroy()
    {
        SharedCamera.activePlayers.Remove(transform);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleMovement();
        HandleJump();
        
        // LOSE CONDITION: Ketika player jatuh ke bawah (Y < -10)
        if (transform.position.y < -10f)
        {
            CmdTakeDamage(100); 
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

    private void HandleJump()
    {
        bool isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthUI(oldHealth, newHealth);

        if (isLocalPlayer && newHealth <= 0)
        {
            StartCoroutine(TampilkanPesan("YOU LOSE!", Color.red));
            
            transform.position = new Vector3(-9f, 3f, 0f);
            rb.velocity = Vector3.zero; 
            CmdResetHealth();
        }
    }

    public void PicuKondisiMenang()
    {
        StartCoroutine(TampilkanPesan("YOU WIN!", Color.green));
        
        transform.position = new Vector3(-9f, 3f, 0f);
        rb.velocity = Vector3.zero;
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
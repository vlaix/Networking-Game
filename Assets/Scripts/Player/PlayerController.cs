using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : NetworkBehaviour
{
    [Header("Platformer Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI scoreText; // Digunakan sebagai Teks Timer di layar

    [Header("Network Sync Variables")]
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;

    // Menyimpan skor/waktu dalam satuan milidetik (integer) agar mudah dikirim ke Backend
    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score = 0;

    [Header("Timer Settings")]
    private float elapsedTime = 0f;
    private bool isTimerRunning = false;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (isLocalPlayer)
        {
            GetComponent<Renderer>().material.color = Color.green;
            transform.position = new Vector3(-9f, 3f, 0f);

            // Mulai timer saat pemain lokal spawn
            ResetAndStartTimer();
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.red;
        }

        UpdateHealthUI(health, health);
        UpdateScoreUI(score, score);

        SharedCamera.activePlayers.Add(transform);
    }

    private void OnDestroy()
    {
        SharedCamera.activePlayers.Remove(transform);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Jalankan Timer dan izinkan bergerak HANYA jika game sedang aktif (belum finish)
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay(elapsedTime);

            HandleMovement();
            HandleJump();
        }

        // Kematian karena jatuh
        if (transform.position.y < -10f)
        {
            CmdTakeDamage(100);
        }
    }

    private void ResetAndStartTimer()
    {
        elapsedTime = 0f;
        isTimerRunning = true;
    }

    private void UpdateTimerDisplay(float timeInSeconds)
    {
        if (scoreText != null)
        {
            scoreText.text = "Time: " + FormatTime(timeInSeconds);
        }
    }

    // Helper static untuk memformat detik ke string "00:00.00"
    public static string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    // Dipanggil saat player menyentuh objek FinishLine
    public void PicuKondisiMenang()
    {
        if (!isTimerRunning) return; // Mencegah pemicuan ganda

        isTimerRunning = false; // Hentikan timer & hentikan pergerakan

        // Convert waktu detik ke milidetik (misal: 12.34 detik -> 12340 ms)
        int finalTimeMs = Mathf.RoundToInt(elapsedTime * 1000f);
        CmdSetScore(finalTimeMs);

        // Panggil Command untuk memberitahu GameManager di Server bahwa player ini menang
        CmdReportWin();

        // Kunci kecepatan fisik agar karakter kaku/diam di tempat garis finis
        rb.velocity = Vector3.zero;
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
            // Jika mati, reset timer dan balikkan ke posisi awal
            transform.position = new Vector3(-9f, 3f, 0f);
            rb.velocity = Vector3.zero;
            CmdResetHealth();
            ResetAndStartTimer();
        }
    }

    [Command]
    private void CmdReportWin()
    {
        if (GameManager.Instance != null)
        {
            // Kirim netId milik GameObject ini ke GameManager di Server
            GameManager.Instance.TriggerGameOver(netId);
        }
    }

    [Command]
    public void CmdSetScore(int newScore) { score = newScore; }

    [Command]
    public void CmdResetHealth() { health = 100; }

    private void OnScoreChanged(int oldScore, int newScore) { UpdateScoreUI(oldScore, newScore); }
    private void UpdateHealthUI(int oldVal, int newVal) { if (healthSlider != null) healthSlider.value = newVal; }

    private void UpdateScoreUI(int oldVal, int newVal)
    {
        if (scoreText != null)
        {
            float seconds = newVal / 1000f;
            scoreText.text = "Time: " + FormatTime(seconds);
        }
    }

    [Command]
    public void CmdTakeDamage(int damageAmount) { health = Mathf.Max(0, health - damageAmount); }
}
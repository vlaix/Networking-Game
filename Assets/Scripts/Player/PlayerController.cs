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
    private bool hasReachedFinish = false;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (isLocalPlayer)
        {
            GetComponent<Renderer>().material.color = Color.green;
            transform.position = new Vector3(-9f, 3f, 0f);

            // Seluruh pemain dalam satu sesi mencatat rekor sebagai satu tim (User ID 1 = Tim Kelompok 8)
            PlayerPrefs.SetInt("ACTIVE_USER_ID", 1);

            // Mulai timer saat pemain lokal spawn
            ResetAndStartTimer();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += HandleGameOver;
            }
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

        if (isLocalPlayer && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Jalankan Timer dan izinkan bergerak HANYA jika pemain belum mencapai finis
        if (isTimerRunning && !hasReachedFinish)
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
        hasReachedFinish = false;
    }

    private void UpdateTimerDisplay(float timeInSeconds)
    {
        if (scoreText != null && !hasReachedFinish)
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
        if (hasReachedFinish) return; // Mencegah pemicuan ganda

        hasReachedFinish = true;
        isTimerRunning = false;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }

        int myTimeMs = Mathf.RoundToInt(elapsedTime * 1000f);
        CmdReportReachedFinish(netId, myTimeMs);
    }

    // Menampilkan status jika sedang menunggu rekan satu tim
    public void SetWaitingForTeammate()
    {
        if (scoreText != null)
        {
            scoreText.text = "Finish! Menunggu teman...";
        }
    }

    private void HandleGameOver(bool isTeamWin, int teamFinalTimeMs)
    {
        if (isLocalPlayer)
        {
            StopTimerAndMovement(teamFinalTimeMs);
        }
    }

    public void StopTimerAndMovement(int finalTeamTimeMs = 0)
    {
        isTimerRunning = false;
        hasReachedFinish = true;

        if (finalTeamTimeMs > 0)
        {
            score = finalTeamTimeMs;
            CmdSetScore(finalTeamTimeMs);
        }
        else if (score <= 0)
        {
            score = Mathf.RoundToInt(elapsedTime * 1000f);
            CmdSetScore(score);
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
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
            // Jika mati, reset timer dan balikkan ke posisi awal
            transform.position = new Vector3(-9f, 3f, 0f);
            rb.velocity = Vector3.zero;
            CmdResetHealth();
            ResetAndStartTimer();
        }
    }

    [Command]
    private void CmdReportReachedFinish(uint playerNetId, int myTimeMs)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerReachedFinish(playerNetId, myTimeMs);
        }
    }

    [Command]
    public void CmdRequestRestart()
    {
        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
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

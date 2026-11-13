using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI matchScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Button References")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button viewLeaderboardButton;

    private void Start()
    {
        // Pastikan kedua panel nonaktif saat permainan dimulai
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOver;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        if (viewLeaderboardButton != null)
        {
            viewLeaderboardButton.onClick.AddListener(OnViewLeaderboardClicked);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
    }

    // Menerima status kemenangan tim dan waktu akhir bersama (ms)
    public void HandleGameOver(bool isTeamWin, int teamFinalTimeMs)
    {
        // 1. Tampilkan GameOverPanel di layer belakang
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        // 2. Tampilkan LeaderboardPanel menutupi layar agar pemain langsung melihat papan peringkat
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            leaderboardPanel.transform.SetAsLastSibling();

            LeaderboardManager lm = leaderboardPanel.GetComponent<LeaderboardManager>();
            if (lm != null)
            {
                lm.FetchLeaderboard();
            }
        }

        // 3. Pastikan kursor mouse terbuka dan bisa digerakkan
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 4. Perbarui status teks kemenangan tim
        if (winnerText != null)
        {
            if (isTeamWin)
            {
                winnerText.text = "STAGE CLEAR!";
                winnerText.color = Color.green;
            }
            else
            {
                winnerText.text = "GAME OVER!";
                winnerText.color = Color.red;
            }
        }

        // 5. Tampilkan dan simpan waktu akhir tim ke Backend SQLite
        float finalTimeSeconds = teamFinalTimeMs / 1000f;
        if (matchScoreText != null)
        {
            matchScoreText.text = "Match Time: " + PlayerController.FormatTime(finalTimeSeconds);
        }

        if (NetworkClient.localPlayer != null)
        {
            PlayerController localPlayer = NetworkClient.localPlayer.GetComponent<PlayerController>();
            if (localPlayer != null)
            {
                localPlayer.StopTimerAndMovement(teamFinalTimeMs);
            }
        }

        if (ScoreManager.Instance != null)
        {
            if (isTeamWin && teamFinalTimeMs > 0)
            {
                // Simpan catatan waktu tim ke backend
                ScoreManager.Instance.SaveCurrentScore(teamFinalTimeMs, () =>
                {
                    ScoreManager.Instance.GetPersonalHighScore((bestTimeMs) =>
                    {
                        if (highScoreText != null)
                        {
                            highScoreText.text = "Best Time: " + PlayerController.FormatTime(bestTimeMs / 1000f);
                        }

                        // Refresh leaderboard setelah skor baru tersimpan
                        if (leaderboardPanel != null)
                        {
                            leaderboardPanel.GetComponent<LeaderboardManager>()?.FetchLeaderboard();
                        }
                    });
                });
            }
            else
            {
                ScoreManager.Instance.GetPersonalHighScore((bestTimeMs) =>
                {
                    if (highScoreText != null)
                    {
                        highScoreText.text = "Best Time: " + PlayerController.FormatTime(bestTimeMs / 1000f);
                    }
                });
            }
        }
    }

    private void OnRestartButtonClicked()
    {
        Debug.Log("[GameOverUI] Tombol Restart diklik. Memulai ulang permainan...");

        if (NetworkServer.active)
        {
            // Server mereload scene untuk semua pemain di room secara sinkron
            NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
        }
        else if (NetworkClient.localPlayer != null)
        {
            // Client meminta server untuk merestart scene
            PlayerController pc = NetworkClient.localPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.CmdRequestRestart();
            }
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnQuitButtonClicked()
    {
        Debug.Log("[GameOverUI] Tombol Quit diklik. Kembali ke menu utama...");

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnViewLeaderboardClicked()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            leaderboardPanel.transform.SetAsLastSibling();
            leaderboardPanel.GetComponent<LeaderboardManager>()?.FetchLeaderboard();
        }
    }
}

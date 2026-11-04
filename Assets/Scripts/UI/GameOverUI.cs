using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI matchScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOver;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
    }

    // Menerima uint winnerNetId
    public void HandleGameOver(uint winnerNetId)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // 1. Bandingkan netId milik Player Lokal dengan winnerNetId
        if (winnerText != null)
        {
            if (NetworkClient.localPlayer != null && NetworkClient.localPlayer.netId == winnerNetId)
            {
                winnerText.text = "YOU WIN!";
                winnerText.color = Color.green;
            }
            else
            {
                winnerText.text = "YOU LOSE!";
                winnerText.color = Color.red;
            }
        }

        // 2. Ambil data Player Lokal di Mirror & Kirim/Ambil Waktu ke Backend
        if (NetworkClient.localPlayer != null)
        {
            PlayerController localPlayer = NetworkClient.localPlayer.GetComponent<PlayerController>();
            if (localPlayer != null)
            {
                int matchTimeMs = localPlayer.score;
                float matchTimeSeconds = matchTimeMs / 1000f;

                if (matchScoreText != null)
                {
                    matchScoreText.text = "Match Time: " + PlayerController.FormatTime(matchTimeSeconds);
                }

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.SaveCurrentScore(matchTimeMs, () =>
                    {
                        ScoreManager.Instance.GetPersonalHighScore((bestTimeMs) =>
                        {
                            if (highScoreText != null)
                            {
                                float bestTimeSeconds = bestTimeMs / 1000f;
                                highScoreText.text = "Best Time: " + PlayerController.FormatTime(bestTimeSeconds);
                            }
                        });
                    });
                }
            }
        }
    }

    private void OnRestartButtonClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
    }
}
using UnityEngine;
using TMPro;

public class LeaderboardItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void Setup(int rank, string username, int scoreMs)
    {
        if (rankText != null)
        {
            rankText.text = $"#{rank}";
        }

        if (usernameText != null)
        {
            usernameText.text = username;
        }

        if (scoreText != null)
        {
            scoreText.text = PlayerController.FormatTime(scoreMs / 1000f);
        }
    }

    public void Setup(int rank, LeaderboardEntry entry)
    {
        if (entry != null)
        {
            Setup(rank, entry.username, entry.highScore);
        }
    }
}

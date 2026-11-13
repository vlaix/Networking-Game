using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string leaderboardUrl = "http://localhost:5000/api/leaderboard";

    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject leaderboardItemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(FetchLeaderboard);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseLeaderboard);
        }
    }

    private void OnDestroy()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(FetchLeaderboard);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseLeaderboard);
        }
    }

    private void OnEnable()
    {
        FetchLeaderboard();
    }

    public void CloseLeaderboard()
    {
        gameObject.SetActive(false);
    }

    public void FetchLeaderboard()
    {
        StartCoroutine(GetLeaderboardRoutine());
    }

    private IEnumerator GetLeaderboardRoutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(leaderboardUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"[LeaderboardManager] Data leaderboard berhasil dimuat: {json}");

                LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(json);

                if (response != null && response.data != null)
                {
                    PopulateScrollView(response.data);
                }
            }
            else
            {
                Debug.LogError($"[LeaderboardManager] Gagal mengambil leaderboard: {request.error}");
            }
        }
    }

    private void PopulateScrollView(List<LeaderboardEntry> leaderboardData)
    {
        // Bersihkan item lama di contentParent
        if (contentParent != null)
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }

        if (leaderboardItemPrefab == null || contentParent == null) return;

        for (int i = 0; i < leaderboardData.Count; i++)
        {
            LeaderboardEntry entry = leaderboardData[i];
            GameObject itemObj = Instantiate(leaderboardItemPrefab, contentParent);
            LeaderboardItemUI itemUI = itemObj.GetComponent<LeaderboardItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(i + 1, entry);
            }
        }
    }
}

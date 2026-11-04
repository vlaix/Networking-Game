using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private string baseURL = "http://localhost:5000/api";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SaveCurrentScore(int finalScore, Action onComplete = null)
    {
        int activeUserId = PlayerPrefs.GetInt("ACTIVE_USER_ID", 1);
        StartCoroutine(SendScoreRequest(activeUserId, finalScore, onComplete));
    }

    private IEnumerator SendScoreRequest(int userId, int score, Action onComplete)
    {
        SaveScoreRequest reqData = new SaveScoreRequest { userId = userId, score = score };
        string jsonPayload = JsonUtility.ToJson(reqData);

        using (UnityWebRequest request = new UnityWebRequest(baseURL + "/score/save", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[ScoreManager] Skor Disimpan ke Backend!");
            }
            else
            {
                Debug.LogError($"[ScoreManager] Gagal Simpan Skor: {request.error}");
            }
            onComplete?.Invoke();
        }
    }

    public void GetPersonalHighScore(Action<int> onCallback)
    {
        int activeUserId = PlayerPrefs.GetInt("ACTIVE_USER_ID", 1);
        StartCoroutine(FetchHighScoreRequest(activeUserId, onCallback));
    }

    private IEnumerator FetchHighScoreRequest(int userId, Action<int> onCallback)
    {
        string url = $"{baseURL}/score/highscore/{userId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                HighScoreResponse resp = JsonUtility.FromJson<HighScoreResponse>(request.downloadHandler.text);
                Debug.Log($"[ScoreManager] High Score Diterima: {resp.highScore}");
                onCallback?.Invoke(resp.highScore);
            }
            else
            {
                Debug.LogError($"[ScoreManager] Gagal Ambil High Score: {request.error}");
                onCallback?.Invoke(0);
            }
        }
    }
}
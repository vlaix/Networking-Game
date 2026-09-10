using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkManagerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        // Menambahkan listener event pada tombol UI
        hostButton.onClick.AddListener(OnHostButtonClicked);
        clientButton.onClick.AddListener(OnClientButtonClicked);
    }

    private void OnHostButtonClicked()
    {
        // Menjalankan fungsi StartHost dari NetworkManager Mirror
        NetworkManager.singleton.StartHost();
        UpdateUIStatus("Status: Connected as HOST");
    }

    private void OnClientButtonClicked()
    {
        // Menjalankan fungsi StartClient dari NetworkManager Mirror
        // Pastikan networkAddress di NetworkManager diatur ke "localhost" atau "127.0.0.1"
        NetworkManager.singleton.StartClient();
        UpdateUIStatus("Status: Connecting as CLIENT...");
    }

    private void UpdateUIStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        // Menyembunyikan tombol pilihan setelah role dipilih
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
    }
}
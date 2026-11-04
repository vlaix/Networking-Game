using System;
using Mirror;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    // Event mengirim uint winnerNetId (Identity ID milik Player pemenang)
    public event Action<uint> OnGameOver;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Dipanggil di Server saat ada pemain yang menyentuh Finish Line
    public void TriggerGameOver(uint winnerNetId)
    {
        if (!isServer) return;
        RpcNotifyGameOver(winnerNetId);
    }

    // RPC mengirim uint (tipe data primitif yang didukung penuh oleh Mirror Weaver)
    [ClientRpc]
    private void RpcNotifyGameOver(uint winnerNetId)
    {
        OnGameOver?.Invoke(winnerNetId);
    }
}
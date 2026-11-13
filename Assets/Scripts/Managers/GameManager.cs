using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    // Event mengirim bool isTeamWin dan int finalTimeMs
    public event Action<bool, int> OnGameOver;

    // Catatan netId pemain yang sudah berada di finish line
    private readonly HashSet<uint> playersAtFinish = new HashSet<uint>();

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

    // Dipanggil di Server saat pemain menyentuh garis finis
    public void PlayerReachedFinish(uint playerNetId, int playerTimeMs)
    {
        if (!isServer) return;

        playersAtFinish.Add(playerNetId);

        // Hitung total pemain yang terhubung
        int totalPlayers = NetworkServer.connections.Count;
        if (totalPlayers <= 0) totalPlayers = 1;

        Debug.Log($"[GameManager] Pemain {playerNetId} mencapai finish ({playersAtFinish.Count}/{totalPlayers})");

        // Jika seluruh anggota tim sudah masuk finis
        if (playersAtFinish.Count >= totalPlayers)
        {
            TriggerGameOver(true, playerTimeMs);
        }
        else
        {
            RpcNotifyPlayerWaiting(playerNetId);
        }
    }

    public void TriggerGameOver(bool isTeamWin, int finalTimeMs = 0)
    {
        if (!isServer) return;
        RpcNotifyGameOver(isTeamWin, finalTimeMs);
    }

    [ClientRpc]
    private void RpcNotifyPlayerWaiting(uint playerNetId)
    {
        if (NetworkClient.localPlayer != null && NetworkClient.localPlayer.netId == playerNetId)
        {
            PlayerController pc = NetworkClient.localPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.SetWaitingForTeammate();
            }
        }
    }

    [ClientRpc]
    private void RpcNotifyGameOver(bool isTeamWin, int finalTimeMs)
    {
        OnGameOver?.Invoke(isTeamWin, finalTimeMs);
    }
}

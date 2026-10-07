using UnityEngine;
using System.Collections.Generic;

public class SharedCamera : MonoBehaviour
{
    [Header("Pengaturan Jarak Kamera")]
    public Vector3 offset = new Vector3(0f, 5f, -15f);
    public float smoothSpeed = 5f;

    // List statis ini akan menyimpan semua player yang muncul di arena
    public static List<Transform> activePlayers = new List<Transform>();

    private void LateUpdate()
    {
        // Jika tidak ada pemain, kamera diam
        if (activePlayers.Count == 0) return;

        // Menghitung titik tengah dari list pemain yang aktif
        Vector3 centerPoint = GetCenterPoint();
        Vector3 targetPosition = centerPoint + offset;

        // Menggeser kamera secara halus
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }

    private Vector3 GetCenterPoint()
    {
        if (activePlayers.Count == 1)
        {
            return activePlayers[0].position;
        }

        Bounds bounds = new Bounds(activePlayers[0].position, Vector3.zero);
        for (int i = 1; i < activePlayers.Count; i++)
        {
            // Cegah error jika ada pemain yang tiba-tiba disconnect
            if (activePlayers[i] != null) 
            {
                bounds.Encapsulate(activePlayers[i].position);
            }
        }
        return bounds.center;
    }
}
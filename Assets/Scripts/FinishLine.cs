using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Cek jika objek yang menyentuh adalah Player
        if (other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            if (player.isLocalPlayer)
            {
                player.PicuKondisiMenang();
            }
        }
    }
}
using UnityEngine;

public class Ranjau : MonoBehaviour
{
    // Mengurangi HP 100 agar langsung mati (kalah)
    [SerializeField] private int trapDamage = 100; 

    private void OnTriggerEnter(Collider other)
    {
        // Mengecek apakah yang menyentuh ranjau adalah Player
        if (other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            // Pastikan hanya player milik kita sendiri yang mengirim perintah damage
            // agar tidak bentrok antara layar Host dan Client
            if (player.isLocalPlayer)
            {
                player.CmdTakeDamage(trapDamage);
            }
        }
    }
}
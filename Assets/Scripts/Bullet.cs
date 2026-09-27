using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float destroyTime = 3f;

    // Dipanggil saat peluru di-spawn di jaringan (khusus Server)
    public override void OnStartServer()
    {
        base.OnStartServer();
        // Hancurkan peluru otomatis setelah beberapa detik jika tidak mengenai apapun
        Invoke(nameof(DestroyBullet), destroyTime);
    }

    private void Update()
    {
        // Gerakkan peluru maju setiap frame
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    // [ServerCallback] memastikan fungsi ini HANYA dieksekusi di Server
    [ServerCallback] 
    private void OnTriggerEnter(Collider other)
    {
        // Cek apakah peluru mengenai Player lain
        if (other.TryGetComponent<PlayerController>(out PlayerController targetPlayer))
        {
            // Panggil method pengurangan HP pada target
            targetPlayer.TakeDamage(damageAmount);
            DestroyBullet();
        }
    }

    [Server]
    private void DestroyBullet()
    {
        // Despawn dari jaringan dan hancurkan objek
        NetworkServer.Destroy(gameObject);
    }
}
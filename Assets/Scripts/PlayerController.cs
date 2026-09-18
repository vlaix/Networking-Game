using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour 
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;

    private void Start()
    {
        // Menginisialisasi warna dan posisi spawn acak saat objek terbentuk
        if (isLocalPlayer) // Pengganti IsOwner
        {
            GetComponent<Renderer>().material.color = Color.green; // Player Lokal = Hijau

            float randomX = Random.Range(-3f, 3f);
            float randomZ = Random.Range(-3f, 3f);
            transform.position = new Vector3(randomX, 0.5f, randomZ);
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.red;   // Player Lain = Merah
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
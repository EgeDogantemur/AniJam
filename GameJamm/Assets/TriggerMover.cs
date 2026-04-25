using UnityEngine;

public class TriggerMover : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [Tooltip("Hareket etmesini istediğin asıl obje")]
    public Transform objectToMove; 
    
    [Tooltip("Objenin başlangıç konumu")]
    public Transform pointA;       
    
    [Tooltip("Objenin gideceği hedef konum")]
    public Transform pointB;       
    
    [Tooltip("Hareket hızı")]
    public float speed = 5f;       

    [Header("Ses Ayarları")]
    public AudioSource audioSource;
    public AudioClip triggerSound;

    private bool isMoving = false;

    void Start()
    {
        // Oyun başladığında objeyi emin olmak için direkt A noktasına yerleştirir
        if (objectToMove != null && pointA != null)
        {
            objectToMove.position = pointA.position;
        }
    }

    void Update()
    {
        // Tetiklenme gerçekleştikten sonra obje B noktasına ulaşana kadar hareket eder
        if (isMoving && objectToMove != null && pointB != null)
        {
            objectToMove.position = Vector3.MoveTowards(objectToMove.position, pointB.position, speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Sadece "Player" etiketli obje çarparsa ve daha önce tetiklenmediyse çalışsın
        if (other.CompareTag("Player") && !isMoving)
        {
            isMoving = true;

            // Sesi çal
            if (audioSource != null && triggerSound != null)
            {
                audioSource.PlayOneShot(triggerSound);
            }
        }
    }
}
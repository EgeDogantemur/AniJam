using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f;

    [Header("Input")]
    [Tooltip("Inspector'dan Action type'ı Button olan bir Input Action tanımlayın (örn. E tuşu)")]
    public InputAction interactAction;

    private Camera playerCamera;
    private CollectableObject currentTarget;

    void Awake()
    {
        // Hiyerarşideki "Cam" objesinden kamerayı bul
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("PlayerInteract: Camera bulunamadı! Lütfen 'Cam' objesinin doğru ayarlandığından emin olun.");
        }
    }

    void OnEnable()
    {
        interactAction.Enable();
    }

    void OnDisable()
    {
        interactAction.Disable();
    }

    void Update()
    {
        CheckForInteractable();
        
        // Etkileşim tuşuna basıldıysa ve bakılan bir hedef varsa onu topla
        if (interactAction.WasPressedThisFrame() && currentTarget != null)
        {
            currentTarget.PickUp(this.gameObject);
            currentTarget = null; // Toplandıktan sonra hedefi temizle
        }
    }

    private void CheckForInteractable()
    {
        if (playerCamera == null) return;

        // Kameranın baktığı yönde bir ışın (ray) oluştur
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // Raycast'i fırlat
        if (Physics.Raycast(ray, out hit, interactRange))
        {
            CollectableObject collectable = hit.collider.GetComponent<CollectableObject>();

            // Eğer yeni bir toplanabilir objeye bakıyorsak
            if (collectable != null && collectable != currentTarget)
            {
                // Eski hedefin highlight'ını kapat
                if (currentTarget != null) currentTarget.SetHighlight(false);
                
                // Yeni hedefi ata ve highlight'ı aç
                currentTarget = collectable;
                currentTarget.SetHighlight(true);
            }
            // Eğer toplanabilir olmayan bir objeye bakıyorsak
            else if (collectable == null && currentTarget != null)
            {
                currentTarget.SetHighlight(false);
                currentTarget = null;
            }
        }
        else
        {
            // Hiçbir şeye bakmıyorsak hedefi temizle
            if (currentTarget != null)
            {
                currentTarget.SetHighlight(false);
                currentTarget = null;
            }
        }
    }
}

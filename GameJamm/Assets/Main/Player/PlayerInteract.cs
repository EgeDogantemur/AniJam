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
    private IInteractable currentTarget;

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
        
        // Etkileşim tuşuna basıldıysa ve bakılan bir hedef varsa etkileşime gir
        if (interactAction.WasPressedThisFrame() && currentTarget != null)
        {
            currentTarget.Interact(this.gameObject);
            currentTarget = null; // Etkileşimden sonra hedefi temizle
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
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            // Eğer yeni bir etkileşimli objeye bakıyorsak
            if (interactable != null && interactable != currentTarget)
            {
                // Eski hedefin highlight'ını kapat
                if (currentTarget != null) currentTarget.SetHighlight(false);
                
                // Yeni hedefi ata ve highlight'ı aç
                currentTarget = interactable;
                currentTarget.SetHighlight(true);
            }
            // Eğer etkileşilemeyen bir objeye bakıyorsak
            else if (interactable == null && currentTarget != null)
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

using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Ayarları")]
    [SerializeField] private float openAngle = 120f;
    [Header("Lock Ayarları")]
    [Tooltip("Collider'ı olan ve etkileşime girilecek kilit objesi")]
    public GameObject lockObject;
    [Tooltip("Kilidi açmak için gereken objenin Tag'i")]
    public string keyTag = "Key";

    [Header("Outline Ayarları")]
    public Color outlineColor = Color.yellow;
    [Range(0f, 0.1f)]
    public float outlineThickness = 0.02f;
    private GameObject outlineObject;
    private bool isHighlighted = false;
    
    private bool isUnlocked = false;

    [Header("Ses Ayarları")]
    public AudioSource audioSource;
    public AudioClip triggerSound;

    private void Start()
    {
        outlineObject = SetupLock(lockObject);
    }

    private GameObject SetupLock(GameObject currentLock)
    {
        if (currentLock == null) return null;
        
        // Kilit objesinin üzerine IInteractable arayüzünü ekleyerek PlayerInteract'a görünür yapıyoruz
        DoorInteractable interactable = currentLock.AddComponent<DoorInteractable>();
        interactable.parentDoor = this;

        Collider lockCol = currentLock.GetComponent<Collider>();
        if (lockCol == null) return null;

        GameObject newOutline = new GameObject("OutlineEffect");
        newOutline.transform.SetParent(currentLock.transform, false);
        newOutline.transform.localPosition = Vector3.zero;
        newOutline.transform.localRotation = Quaternion.identity;
        newOutline.transform.localScale = Vector3.one;

        // Çizgi (Edge) outline için LineRenderer kullanıyoruz
        LineRenderer lr = newOutline.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = outlineThickness;
        lr.endWidth = outlineThickness;
        lr.positionCount = 16;
        
        Shader unlit = Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color");
        if (unlit != null)
        {
            Material outlineMat = new Material(unlit);
            if (outlineMat.HasProperty("_BaseColor")) outlineMat.SetColor("_BaseColor", outlineColor);
            if (outlineMat.HasProperty("_Color")) outlineMat.SetColor("_Color", outlineColor);
            lr.material = outlineMat;
        }

        Vector3 center = Vector3.zero;
        Vector3 size = Vector3.one;

        if (lockCol is BoxCollider box)
        {
            center = box.center;
            size = box.size;
        }
        else if (lockCol is SphereCollider sphere)
        {
            center = sphere.center;
            size = Vector3.one * (sphere.radius * 2f);
        }
        else if (lockCol is CapsuleCollider capsule)
        {
            center = capsule.center;
            float d = capsule.radius * 2f;
            float h = capsule.height;
            if (capsule.direction == 0) size = new Vector3(h, d, d);
            else if (capsule.direction == 1) size = new Vector3(d, h, d);
            else if (capsule.direction == 2) size = new Vector3(d, d, h);
        }
        else if (lockCol is MeshCollider meshCol && meshCol.sharedMesh != null)
        {
            center = meshCol.sharedMesh.bounds.center;
            size = meshCol.sharedMesh.bounds.size;
        }

        Vector3 extents = size / 2f;
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
        corners[1] = center + new Vector3(-extents.x, -extents.y,  extents.z);
        corners[2] = center + new Vector3( extents.x, -extents.y,  extents.z);
        corners[3] = center + new Vector3( extents.x, -extents.y, -extents.z);
        corners[4] = center + new Vector3(-extents.x,  extents.y, -extents.z);
        corners[5] = center + new Vector3(-extents.x,  extents.y,  extents.z);
        corners[6] = center + new Vector3( extents.x,  extents.y,  extents.z);
        corners[7] = center + new Vector3( extents.x,  extents.y, -extents.z);

        // Küpün 12 ayrı kenarını tek bir çizgide çizmek için gereken düğüm sırası
        int[] path = { 0, 1, 2, 3, 0, 4, 5, 1, 5, 6, 2, 6, 7, 3, 7, 4 };
        for (int i = 0; i < 16; i++)
        {
            lr.SetPosition(i, corners[path[i]]);
        }

        newOutline.SetActive(false);
        return newOutline;
    }

    public void SetHighlightForLock(bool state)
    {
        if (isHighlighted == state) return;
        isHighlighted = state;
        if (outlineObject != null) outlineObject.SetActive(state);
    }

    public void InteractWithLock(GameObject interactor)
    {
        if (isUnlocked) return; // Zaten açıksa tekrar etkileşime girmesin

        if (interactor.GetComponent<BasicInventorySystem>() == null) return;

        BasicInventorySystem inv = interactor.GetComponent<BasicInventorySystem>();
        
        if (inv.inventorySlots[inv.selectedSlotIndex].CompareTag(keyTag))
        {
            isUnlocked = true;
            transform.Rotate(0, openAngle, 0);
            inv.inventorySlots[inv.selectedSlotIndex] = null;

            if (audioSource != null && triggerSound != null)
            {
                audioSource.PlayOneShot(triggerSound);
            }
        }
    }
}

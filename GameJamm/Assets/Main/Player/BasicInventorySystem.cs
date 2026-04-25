using UnityEngine;
using UnityEngine.InputSystem;

public class BasicInventorySystem : MonoBehaviour
{
    [Header("Settings")]
    [Range(1, 10)]
    public int maxInventorySize = 5;

    [Header("Input System")]
    [Tooltip("Örn: Fare tekerleği (Scroll) veya özel butonlar için InputAction")]
    public InputAction changeSlotAction;
    [Tooltip("Seçili eşyayı yere atmak için InputAction (Örn: G tuşu)")]
    public InputAction dropItemAction;

    [Header("Inventory State")]
    public GameObject[] inventorySlots;
    public int selectedSlotIndex = 0;
    public string heldItem = "";

    public Camera playerCamera; // Referans atanmazsa otomatik bulunur
    private GameObject currentlyShownItem = null;

    void Awake()
    {
        inventorySlots = new GameObject[maxInventorySize];
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void OnEnable()
    {
        changeSlotAction.Enable();
        dropItemAction.Enable();
    }

    void OnDisable()
    {
        changeSlotAction.Disable();
        dropItemAction.Disable();
    }

    void Update()
    {
        HandleInput();
        UpdateHeldItem();
    }

    private void HandleInput()
    {
        if (dropItemAction.WasPressedThisFrame())
        {
            DropSelectedItem();
        }

        if (changeSlotAction.triggered)
        {
            var value = changeSlotAction.ReadValueAsObject();
            if (value is Vector2 vec2 && vec2.y != 0) 
            {
                int newIndex = selectedSlotIndex + (vec2.y > 0 ? 1 : -1);
                SetSelectedSlot(newIndex);
            }
            else if (value is float fVal && fVal != 0) 
            {
                int newIndex = selectedSlotIndex + (fVal > 0 ? 1 : -1);
                SetSelectedSlot(newIndex);
            }
            else 
            {
                SetSelectedSlot(selectedSlotIndex + 1);
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SetSelectedSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SetSelectedSlot(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SetSelectedSlot(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SetSelectedSlot(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) SetSelectedSlot(4);
            if (Keyboard.current.digit6Key.wasPressedThisFrame) SetSelectedSlot(5);
            if (Keyboard.current.digit7Key.wasPressedThisFrame) SetSelectedSlot(6);
            if (Keyboard.current.digit8Key.wasPressedThisFrame) SetSelectedSlot(7);
            if (Keyboard.current.digit9Key.wasPressedThisFrame) SetSelectedSlot(8);
            if (Keyboard.current.digit0Key.wasPressedThisFrame) SetSelectedSlot(9);
        }
    }

    public bool AddItem(GameObject item)
    {
        for (int i = 0; i < maxInventorySize; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = item;
                
                item.SetActive(false); 
                item.transform.SetParent(transform); 
                
                UpdateHeldItem();
                return true; 
            }
        }
        
        Debug.Log("Envanter dolu!");
        return false; 
    }

    public void DropSelectedItem()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < maxInventorySize)
        {
            if (inventorySlots[selectedSlotIndex] != null)
            {
                GameObject droppedItem = inventorySlots[selectedSlotIndex];
                inventorySlots[selectedSlotIndex] = null;

                if (droppedItem == currentlyShownItem) currentlyShownItem = null;

                droppedItem.SetActive(true);
                droppedItem.transform.SetParent(null);
                
                // Yere atıldığında fizik ve collider'ları geri aç
                Collider[] cols = droppedItem.GetComponentsInChildren<Collider>();
                foreach (var c in cols) c.enabled = true;
                
                Rigidbody[] rbs = droppedItem.GetComponentsInChildren<Rigidbody>();
                foreach (var r in rbs) r.isKinematic = false;
                
                if (playerCamera != null) {
                    droppedItem.transform.position = playerCamera.transform.position + playerCamera.transform.forward * 0.2f;
                } else {
                    droppedItem.transform.position = transform.position + transform.forward * 0.2f + Vector3.up * 1f;
                }

                UpdateHeldItem();
            }
        }
    }

    public void SetSelectedSlot(int index)
    {
        if (index < 0) index = maxInventorySize - 1;
        if (index >= maxInventorySize) index = 0;

        selectedSlotIndex = index;
        UpdateHeldItem();
    }

    private void UpdateHeldItem()
    {
        GameObject newHeldItem = null;

        if (selectedSlotIndex >= 0 && selectedSlotIndex < maxInventorySize && inventorySlots[selectedSlotIndex] != null)
        {
            newHeldItem = inventorySlots[selectedSlotIndex];
            heldItem = newHeldItem.name;
        }
        else
        {
            heldItem = ""; 
        }

        if (currentlyShownItem != newHeldItem)
        {
            // Eski eşyayı gizle (envantere geri dönmüş gibi davranır)
            if (currentlyShownItem != null)
            {
                currentlyShownItem.SetActive(false);
            }

            currentlyShownItem = newHeldItem;

            // Yeni eşyayı göster ve kameranın çocuğu yap
            if (currentlyShownItem != null && playerCamera != null)
            {
                currentlyShownItem.SetActive(true);
                
                // Eldeyken fizik hareketini ve çarpışmaları engelle
                Collider[] cols = currentlyShownItem.GetComponentsInChildren<Collider>();
                foreach (var c in cols) c.enabled = false;
                
                Rigidbody[] rbs = currentlyShownItem.GetComponentsInChildren<Rigidbody>();
                foreach (var r in rbs) r.isKinematic = true;

                // Kameranın çocuğu yap ve belirtilen pozisyona taşı
                currentlyShownItem.transform.SetParent(playerCamera.transform);
                currentlyShownItem.transform.localPosition = new Vector3(0.25f, -0.2f, 0.5f); // Biraz sağda ve önde
                currentlyShownItem.transform.localRotation = Quaternion.identity;
            }
        }
    }
}


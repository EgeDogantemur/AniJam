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

    void Awake()
    {
        // Envanteri belirlenen maksimum boyuta göre oluştur
        inventorySlots = new GameObject[maxInventorySize];
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
        // Eşya atma işlemi kontrolü
        if (dropItemAction.WasPressedThisFrame())
        {
            DropSelectedItem();
        }

        // 1. InputAction üzerinden etkileşim kontrolü (Örn: Fare tekerleği veya özel buton)
        if (changeSlotAction.triggered)
        {
            var value = changeSlotAction.ReadValueAsObject();
            if (value is Vector2 vec2 && vec2.y != 0) // Scroll wheel
            {
                int newIndex = selectedSlotIndex + (vec2.y > 0 ? 1 : -1);
                SetSelectedSlot(newIndex);
            }
            else if (value is float fVal && fVal != 0) // 1D Axis
            {
                int newIndex = selectedSlotIndex + (fVal > 0 ? 1 : -1);
                SetSelectedSlot(newIndex);
            }
            else // Sadece bir Butonsa (Değer okumuyorsa)
            {
                SetSelectedSlot(selectedSlotIndex + 1);
            }
        }

        // 2. Klavyenin üstündeki sayı tuşları ile kontrol (1 -> 0, 6 -> 5 vs.)
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

    // Boş ilk envanter slotuna eşya alabilme
    public bool AddItem(GameObject item)
    {
        for (int i = 0; i < maxInventorySize; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = item;
                
                // Eşya envantere alındığında dünyada gizle ve parent olarak ayarla
                item.SetActive(false); 
                item.transform.SetParent(transform); 
                
                UpdateHeldItem();
                return true; // Eşya başarıyla eklendi
            }
        }
        
        Debug.Log("Envanter dolu!");
        return false; // Boş yer yok
    }

    // Seçili envanter slotundan eşya atabilme
    public void DropSelectedItem()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < maxInventorySize)
        {
            if (inventorySlots[selectedSlotIndex] != null)
            {
                GameObject droppedItem = inventorySlots[selectedSlotIndex];
                inventorySlots[selectedSlotIndex] = null;

                // Eşyayı dünyada tekrar görünür yap ve parent'ını kaldır
                droppedItem.SetActive(true);
                droppedItem.transform.SetParent(null);
                
                // Yere atılan eşyayı karakterin biraz önüne yerleştir
                droppedItem.transform.position = transform.position + transform.forward * 0.1f + Vector3.up * 1f;

                UpdateHeldItem();
            }
        }
    }

    // Seçili slotu değiştirmek için kullanılabilir
    public void SetSelectedSlot(int index)
    {
        // Sınırları aştığında başa veya sona sarması için (Wrap around)
        if (index < 0) index = maxInventorySize - 1;
        if (index >= maxInventorySize) index = 0;

        selectedSlotIndex = index;
        UpdateHeldItem();
    }

    // Seçili envanter slotunda eşya varsa "heldItem" değişkenini onun adıyla değiştir
    private void UpdateHeldItem()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < maxInventorySize && inventorySlots[selectedSlotIndex] != null)
        {
            heldItem = inventorySlots[selectedSlotIndex].name;
        }
        else
        {
            heldItem = ""; // Eşya yoksa boş bırak
        }
    }
}


using UnityEngine;
using System.Collections;

[System.Serializable]
public class MirrorItem
{
    [Tooltip("Envanterdeki objenin Unity Tag'i")]
    public string targetTag; 
    public GameObject targetObject;
}

public class Mirror : MonoBehaviour
{
    [Header("Mirror Ayarları")]
    public MirrorItem[] mirrorItems;
    public GameObject wallObject;
    [Tooltip("Collider'ı olan ve etkileşime girilecek obje")]
    public GameObject joint;
    
    [Header("Outline Ayarları")]
    public Color outlineColor = Color.yellow;
    [Range(0f, 0.1f)]
    public float outlineThickness = 0.02f;
     private GameObject outlineObject;
    private bool isHighlighted = false;
    private bool isSequenceRunning = false;
    
    // Joint üzerinde duran objeyi takip etmek için
    private GameObject mountedObject = null;
    private bool isGateOpen = false;
    
    void Start()
    {
        SetupJoint();
    }
    
    private void SetupJoint()
    {
        if (joint == null) return;
        
        // Joint objesinin üzerine IInteractable arayüzünü ekleyerek PlayerInteract'a görünür yapıyoruz
        MirrorInteractable interactable = joint.AddComponent<MirrorInteractable>();
        interactable.parentMirror = this;

        Collider col = joint.GetComponent<Collider>();
        if (col == null) return;

        outlineObject = new GameObject("OutlineEffect");
        outlineObject.transform.SetParent(joint.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

        // Çizgi (Edge) outline için LineRenderer kullanıyoruz
        LineRenderer lr = outlineObject.AddComponent<LineRenderer>();
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

        if (col is BoxCollider box)
        {
            center = box.center;
            size = box.size;
        }
        else if (col is SphereCollider sphere)
        {
            center = sphere.center;
            size = Vector3.one * (sphere.radius * 2f);
        }
        else if (col is CapsuleCollider capsule)
        {
            center = capsule.center;
            float d = capsule.radius * 2f;
            float h = capsule.height;
            if (capsule.direction == 0) size = new Vector3(h, d, d);
            else if (capsule.direction == 1) size = new Vector3(d, h, d);
            else if (capsule.direction == 2) size = new Vector3(d, d, h);
        }
        else if (col is MeshCollider meshCol && meshCol.sharedMesh != null)
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

        outlineObject.SetActive(false);
    }

    public void SetHighlight(bool state)
    {
        if (isHighlighted == state) return;
        isHighlighted = state;
        if (outlineObject != null)
        {
            outlineObject.SetActive(state);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (isSequenceRunning) return;

        BasicInventorySystem inventory = interactor.GetComponent<BasicInventorySystem>();
        if (inventory != null)
        {
            // 1. DURUM: EĞER JOINT ÜZERİNDE ZATEN BİR EŞYA VARSA, ONU GERİ AL
            if (mountedObject != null)
            {
                // Envantere girebilmesi ve sonrasında yere atılabilmesi için collider'ı geri aç
                Collider[] cols = mountedObject.GetComponentsInChildren<Collider>();
                foreach (var c in cols) c.enabled = true;

                if (inventory.AddItem(mountedObject))
                {
                    mountedObject = null;
                    // Duvarı geri kapatmak için boş parametreyle sekansı çağır
                    StartCoroutine(MirrorSequence(""));
                }
                else
                {
                    // Envanter doluysa ve eklenemediyse collider'ı tekrar kapat ki orada kalmaya devam etsin
                    foreach (var c in cols) c.enabled = false;
                }
                return;
            }

            // 2. DURUM: JOINT BOŞSA, ENVANTERDEN EŞYA KOYMAYA ÇALIŞ
            int selIndex = inventory.selectedSlotIndex;
            if (selIndex >= 0 && selIndex < inventory.inventorySlots.Length)
            {
                // Elimizde tuttuğumuz objeyi bul
                GameObject heldObj = inventory.inventorySlots[selIndex];
                if (heldObj != null)
                {
                    // BasicInventorySystem'de objenin ismini kullanıyoruz
                    string tagToCheck = heldObj.name; 

                    bool tagMatches = false;
                    foreach (var item in mirrorItems)
                    {
                        if (item.targetTag == tagToCheck)
                        {
                            tagMatches = true;
                            break;
                        }
                    }

                    if (tagMatches)
                    {
                    // 1. Envanterden eşyayı çıkar ve görseli güncelle
                    inventory.inventorySlots[selIndex] = null;
                    inventory.SetSelectedSlot(selIndex); 
                    
                    // 2. Eşyayı joint noktasına monte et
                    heldObj.SetActive(true);
                    heldObj.transform.SetParent(joint.transform);
                    heldObj.transform.position = joint.transform.position;
                    heldObj.transform.rotation = joint.transform.rotation;
                    
                    // 3. Rigidbody'lerini kapat ki hareket etmesin
                    Rigidbody[] rbs = heldObj.GetComponentsInChildren<Rigidbody>();
                    foreach (var rb in rbs) rb.isKinematic = true;

                    // 4. Collider'ını kapat ki tekrar tıkladığımızda alttaki Joint'i (Mirror) algılasın
                    Collider[] cols = heldObj.GetComponentsInChildren<Collider>();
                    foreach (var c in cols) c.enabled = false;

                    mountedObject = heldObj;

                    // 5. Eğer koyduğumuz eşya doğruysa kapı sekansını başlat
                    if (tagMatches)
                    {
                        StartCoroutine(MirrorSequence(tagToCheck));
                    }
                }
            }
        }
    }
    }

    private IEnumerator MirrorSequence(string matchingTag)
    {
        isSequenceRunning = true;
        SetHighlight(false); // Etkileşim başlayınca outline'ı kapat

        bool isClosing = string.IsNullOrEmpty(matchingTag);

        if (!isClosing)
        {
            // AÇILMA SEKANSI (Eşya koyuldu)

            // 1. wallObject belirmeli (Aşağıdan yukarı kayarak)
            if (wallObject != null)
            {
                wallObject.SetActive(true);
                yield return StartCoroutine(SlideObject(wallObject, true, 0.5f));
            }

            // 2. diğer tüm arraydeki objeler gizlenmeli
            foreach (var item in mirrorItems)
            {
                if (item.targetObject != null)
                {
                    item.targetObject.SetActive(false);
                }
            }

            // 3. tagi heldObject ile eşleşen obje ortaya çıkmalı
            yield return new WaitForSeconds(0.25f); 
            
            foreach (var item in mirrorItems)
            {
                if (item.targetTag == matchingTag && item.targetObject != null)
                {
                    item.targetObject.SetActive(true);
                }
            }

            // 4. ardından wallObject yavaşça gizlenmeli
            yield return new WaitForSeconds(0.5f); 
            
            if (wallObject != null)
            {
                // Yavaşça aşağı kayarak kaybolma efekti
                yield return StartCoroutine(SlideObject(wallObject, false, 1f));
                wallObject.SetActive(false);
            }

            isGateOpen = true;
        }
        else
        {
            // KAPANMA SEKANSI (Eşya geri alındı)
            if (!isGateOpen) { isSequenceRunning = false; yield break; } // Zaten kapalıysa çık

            // 1. wallObject yukarı kayarak kapıyı kapatır
            if (wallObject != null)
            {
                wallObject.SetActive(true);
                yield return StartCoroutine(SlideObject(wallObject, true, 0.5f));
            }

            // 2. Açılmış olan targetObject'i tekrar gizle
            foreach (var item in mirrorItems)
            {
                if (item.targetObject != null)
                {
                    item.targetObject.SetActive(false);
                }
            }
            
            // Kapı kapalı kalmaya devam eder, sekans biter
            isGateOpen = false;
        }

        isSequenceRunning = false;
    }

    private IEnumerator SlideObject(GameObject obj, bool isAppearing, float duration)
    {
        // Objenin boyunu bulup o kadar aşağı kaydıracağız. Collider yoksa varsayılan 5 birim.
        Collider col = obj.GetComponent<Collider>();
        float dropDistance = col != null ? col.bounds.size.y + 0.5f : 5f;
        
        Vector3 originalPos = obj.transform.position;
        Vector3 hiddenPos = originalPos + Vector3.down * dropDistance;

        Vector3 startPos = isAppearing ? hiddenPos : originalPos;
        Vector3 endPos = isAppearing ? originalPos : hiddenPos;

        obj.transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); 
            
            obj.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        obj.transform.position = endPos;

        if (!isAppearing)
        {
            obj.transform.position = originalPos;
        }
    }
}

using UnityEngine;
using System.Collections;

[System.Serializable]
public class MirrorItem
{
    [Tooltip("Envanterdeki objenin Unity Tag'i")]
    public string targetTag; 
    public GameObject targetObjectA;
    public GameObject targetObjectB;
}

public class Mirror : MonoBehaviour
{
    [Header("Mirror Ayarları")]
    public MirrorItem[] mirrorItems;
    public GameObject wallObject;
    [Tooltip("Collider'ı olan ve etkileşime girilecek obje A")]
    public GameObject jointA;
    [Tooltip("Collider'ı olan ve etkileşime girilecek obje B")]
    public GameObject jointB;
    
    [Header("Outline Ayarları")]
    public Color outlineColor = Color.yellow;
    [Range(0f, 0.1f)]
    public float outlineThickness = 0.02f;
    private GameObject outlineObjectA;
    private GameObject outlineObjectB;
    private bool isHighlightedA = false;
    private bool isHighlightedB = false;
    
    // Joint üzerinde duran objeyi takip etmek için
    private GameObject mountedObject = null;

    [Header("Ses Ayarları")]
    public AudioSource audioSource;
    public AudioClip triggerSound;
    
    void Start()
    {
        outlineObjectA = SetupJoint(jointA);
        outlineObjectB = SetupJoint(jointB);
    }
    
    private GameObject SetupJoint(GameObject currentJoint)
    {
        if (currentJoint == null) return null;
        
        // Joint objesinin üzerine IInteractable arayüzünü ekleyerek PlayerInteract'a görünür yapıyoruz
        MirrorInteractable interactable = currentJoint.AddComponent<MirrorInteractable>();
        interactable.parentMirror = this;

        Collider col = currentJoint.GetComponent<Collider>();
        if (col == null) return null;

        GameObject newOutline = new GameObject("OutlineEffect");
        newOutline.transform.SetParent(currentJoint.transform, false);
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

        newOutline.SetActive(false);
        return newOutline;
    }

    public void SetHighlightForJoint(bool state, GameObject interactedJoint)
    {
        if (interactedJoint == jointA)
        {
            if (isHighlightedA == state) return;
            isHighlightedA = state;
            if (outlineObjectA != null) outlineObjectA.SetActive(state);
        }
        else if (interactedJoint == jointB)
        {
            if (isHighlightedB == state) return;
            isHighlightedB = state;
            if (outlineObjectB != null) outlineObjectB.SetActive(state);
        }
    }

    public void InteractWithJoint(GameObject interactor, GameObject interactedJoint)
    {
        BasicInventorySystem inventory = interactor.GetComponent<BasicInventorySystem>();
        if (inventory != null)
        {
            int selIndex = inventory.selectedSlotIndex;
            GameObject heldObj = null;
            if (selIndex >= 0 && selIndex < inventory.inventorySlots.Length)
            {
                heldObj = inventory.inventorySlots[selIndex];
            }

            if (mountedObject != null)
            {
                if (wallObject != null) wallObject.SetActive(true);

                if (heldObj == null)
                {
                    Collider[] cols = mountedObject.GetComponentsInChildren<Collider>();
                    foreach (var c in cols) c.enabled = true;

                    if (inventory.AddItem(mountedObject))
                    {
                        mountedObject = null;
                        
                        foreach (var mItem in mirrorItems)
                        {
                            if (interactedJoint == jointA && mItem.targetObjectA != null) mItem.targetObjectA.SetActive(false);
                            if (interactedJoint == jointB && mItem.targetObjectB != null) mItem.targetObjectB.SetActive(false);
                            
                            if (audioSource != null && triggerSound != null)
                            {
                                audioSource.PlayOneShot(triggerSound);
                            }
                        }
                    }
                    else
                    {
                        foreach (var c in cols) c.enabled = false;
                    }
                }
                else
                {
                    string tagToCheck = heldObj.tag;
                    int matchIndex = GetMirrorItemIndex(tagToCheck);

                    if (matchIndex != -1)
                    {
                        foreach (var mItem in mirrorItems)
                        {
                            if (interactedJoint == jointA && mItem.targetObjectA != null) mItem.targetObjectA.SetActive(false);
                            if (interactedJoint == jointB && mItem.targetObjectB != null) mItem.targetObjectB.SetActive(false);
                            
                            if (audioSource != null && triggerSound != null)
                            {
                                audioSource.PlayOneShot(triggerSound);
                            }
                        }
                        
                        if (interactedJoint == jointA && mirrorItems[matchIndex].targetObjectA != null)
                        {
                            mirrorItems[matchIndex].targetObjectA.SetActive(true);
                        }
                        else if (interactedJoint == jointB && mirrorItems[matchIndex].targetObjectB != null)
                        {
                            mirrorItems[matchIndex].targetObjectB.SetActive(true);
                        }

                        GameObject item = heldObj;
                        
                        inventory.inventorySlots[selIndex] = null;
                        inventory.SetSelectedSlot(selIndex);

                        Collider[] cols = mountedObject.GetComponentsInChildren<Collider>();
                        foreach (var c in cols) c.enabled = true;

                        GameObject oldMounted = mountedObject;
                        if (inventory.AddItem(oldMounted))
                        {
                            mountedObject = null;
                        }
                        
                        MountObjectToJoint(item, interactedJoint);

                        if (wallObject != null) wallObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (heldObj != null)
                {
                    string tagToCheck = heldObj.tag;
                    int matchIndex = GetMirrorItemIndex(tagToCheck);

                    if (matchIndex != -1)
                    {
                        foreach (var mItem in mirrorItems)
                        {
                            if (interactedJoint == jointA && mItem.targetObjectA != null) mItem.targetObjectA.SetActive(false);
                            if (interactedJoint == jointB && mItem.targetObjectB != null) mItem.targetObjectB.SetActive(false);

                            if (audioSource != null && triggerSound != null)
                            {
                                audioSource.PlayOneShot(triggerSound);
                            }
                        }
                        
                        if (interactedJoint == jointA && mirrorItems[matchIndex].targetObjectA != null)
                        {
                            mirrorItems[matchIndex].targetObjectA.SetActive(true);
                        }
                        else if (interactedJoint == jointB && mirrorItems[matchIndex].targetObjectB != null)
                        {
                            mirrorItems[matchIndex].targetObjectB.SetActive(true);
                        }

                        GameObject item = heldObj;
                        
                        inventory.inventorySlots[selIndex] = null;
                        inventory.SetSelectedSlot(selIndex);

                        MountObjectToJoint(item, interactedJoint);

                        if (wallObject != null) wallObject.SetActive(false);
                    }
                }
            }
        }
    }

    private int GetMirrorItemIndex(string tag)
    {
        for (int i = 0; i < mirrorItems.Length; i++)
        {
            if (mirrorItems[i].targetTag == tag)
            {
                return i;
            }
        }
        return -1;
    }

    private void MountObjectToJoint(GameObject item, GameObject interactedJoint)
    {
        mountedObject = item;
        mountedObject.SetActive(true);
        mountedObject.transform.SetParent(interactedJoint.transform);
        mountedObject.transform.position = interactedJoint.transform.position;
        mountedObject.transform.rotation = interactedJoint.transform.rotation;
        
        Rigidbody[] rbs = mountedObject.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs) rb.isKinematic = true;

        Collider[] cols = mountedObject.GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;
    }
}

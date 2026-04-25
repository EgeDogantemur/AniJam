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
    
    // Joint üzerinde duran objeyi takip etmek için
    private GameObject mountedObject = null;
    
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
                            if (mItem.targetObject != null)
                                mItem.targetObject.SetActive(false);
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
                            if (mItem.targetObject != null)
                                mItem.targetObject.SetActive(false);
                        }
                        
                        if (mirrorItems[matchIndex].targetObject != null)
                        {
                            mirrorItems[matchIndex].targetObject.SetActive(true);
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
                        
                        MountObjectToJoint(item);

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
                            if (mItem.targetObject != null)
                                mItem.targetObject.SetActive(false);
                        }
                        
                        if (mirrorItems[matchIndex].targetObject != null)
                        {
                            mirrorItems[matchIndex].targetObject.SetActive(true);
                        }

                        GameObject item = heldObj;
                        
                        inventory.inventorySlots[selIndex] = null;
                        inventory.SetSelectedSlot(selIndex);

                        MountObjectToJoint(item);

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

    private void MountObjectToJoint(GameObject item)
    {
        mountedObject = item;
        mountedObject.SetActive(true);
        mountedObject.transform.SetParent(joint.transform);
        mountedObject.transform.position = joint.transform.position;
        mountedObject.transform.rotation = joint.transform.rotation;
        
        Rigidbody[] rbs = mountedObject.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs) rb.isKinematic = true;

        Collider[] cols = mountedObject.GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;
    }
}

using UnityEngine;

public class CollectableObject : MonoBehaviour, IInteractable
{
    [Header("Outline Ayarları")]
    public Color outlineColor = Color.yellow;
    [Range(0f, 0.1f)]
    public float outlineThickness = 0.02f; // Çizgi kalınlığı
    
    private GameObject outlineObject;
    private bool isHighlighted = false;

    void Start()
    {
        CreateOutlineObject();
    }

    private void CreateOutlineObject()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        outlineObject = new GameObject("OutlineEffect");
        outlineObject.transform.SetParent(this.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

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
            // MeshCollider bounds is in world space, we convert it to local
            // but for simplicity, we can just use the local bounds of the sharedMesh
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

        int[] path = { 0, 1, 2, 3, 0, 4, 5, 1, 5, 6, 2, 6, 7, 3, 7, 4 };
        for (int i = 0; i < 16; i++)
        {
            lr.SetPosition(i, corners[path[i]]);
        }

        outlineObject.SetActive(false);
    }

    public void Interact(GameObject interactor)
    {
        BasicInventorySystem inventory = interactor.GetComponent<BasicInventorySystem>();
        if (inventory != null)
        {
            SetHighlight(false); // Envantere girerken outline'ı sil
            inventory.AddItem(this.gameObject);
        }
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
}


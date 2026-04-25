using UnityEngine;

public class CollectableObject : MonoBehaviour
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
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        // Eğer objenin mesh'i yoksa outline oluşturamayız
        if (mf == null || mr == null || mf.sharedMesh == null) return;

        // Outline efekti için objenin görünmez bir kopyasını yaratıyoruz
        outlineObject = new GameObject("OutlineEffect");
        outlineObject.transform.SetParent(this.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

        MeshFilter outMf = outlineObject.AddComponent<MeshFilter>();
        
        // Mesh'i ters çevirip köşelerini şişiriyoruz (Böylece arkada görünür ve dışarı taşar)
        outMf.mesh = CreateInvertedMesh(mf.sharedMesh, outlineThickness);

        MeshRenderer outMr = outlineObject.AddComponent<MeshRenderer>();
        
        // Dış çizgi için HDRP veya Built-in uyumlu Unlit (gölgesiz) materyal bul
        Shader unlit = Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color");
        if (unlit != null)
        {
            Material outlineMat = new Material(unlit);
            if (outlineMat.HasProperty("_BaseColor")) outlineMat.SetColor("_BaseColor", outlineColor); // HDRP
            if (outlineMat.HasProperty("_Color")) outlineMat.SetColor("_Color", outlineColor); // Built-in
            outMr.material = outlineMat;
        }

        outlineObject.SetActive(false); // Başlangıçta kapalı
    }

    private Mesh CreateInvertedMesh(Mesh originalMesh, float thickness)
    {
        Mesh invertedMesh = Instantiate(originalMesh);
        
        // 1. Üçgenleri ters çevir (Dış yüzeyler içe döner, obje sadece arkadan görülür)
        int[] triangles = invertedMesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int temp = triangles[i];
            triangles[i] = triangles[i + 1];
            triangles[i + 1] = temp;
        }
        invertedMesh.triangles = triangles;
        
        // 2. Köşeleri normalleri yönünde dışarı iterek objeyi kalınlaştır
        Vector3[] vertices = invertedMesh.vertices;
        Vector3[] normals = invertedMesh.normals;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += normals[i] * thickness;
        }
        invertedMesh.vertices = vertices;
        
        return invertedMesh;
    }

    public void PickUp(GameObject interactor)
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


using UnityEngine;

public class TavanVantilatoru : MonoBehaviour
{
    [Tooltip("Vantilatörün dönüş hızı. Ters yöne dönmesini isterseniz eksi bir değer yazın (örn: -150).")]
    public float donusHizi = 150f;

    void Update()
    {
        // Space.Self kullanımı, objenin dünya eksenine göre değil, 
        // KENDİ yerel Y ekseni etrafında dönmesini sağlar.
        // Böylece vantilatörü eğik bir tavana koysanız bile yamuk dönmez, doğru şekilde döner.
        transform.Rotate(0f, donusHizi * Time.deltaTime, 0f, Space.Self);
    }
}
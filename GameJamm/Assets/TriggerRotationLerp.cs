using UnityEngine;
using System.Collections;

public class TriggerRotationLerp : MonoBehaviour
{
    [Header("Dönüş Ayarları")]
    [Tooltip("Dönüşü pürüzsüzce gerçekleşecek obje")]
    public Transform targetTransform;
    
    [Tooltip("Başlangıç X rotasyonu (derece)")]
    public float startRotationX = -90f;
    
    [Tooltip("Hedef X rotasyonu (derece)")]
    public float endRotationX = -8f;
    
    [Tooltip("Dönüş hızı (saniye başına derece)")]
    public float speed = 20f;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Sadece "Player" tag'ine sahip objeler trigger'a girerse çalışsın
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            // Dönüş işlemini başlatmak için coroutine'i çağır
            StartCoroutine(RotateObjectSmoothly());
        }
    }

    private IEnumerator RotateObjectSmoothly()
    {
        if (targetTransform == null)
        {
            Debug.LogError("Dönecek obje seçilmedi! Lütfen Inspector'da ata.");
            yield break;
        }

        // Objeyi başlangıç açısına ayarla
        Vector3 initialEuler = targetTransform.localEulerAngles;
        initialEuler.x = startRotationX;
        targetTransform.localEulerAngles = initialEuler;

        // Dönüşü pürüzsüzce gerçekleştir
        while (Mathf.Abs(targetTransform.localEulerAngles.x - endRotationX) > 0.01f)
        {
            // Vector3.MoveTowards veya Mathf.Lerp ile pürüzsüz geçiş sağla
            float nextX = Mathf.MoveTowardsAngle(targetTransform.localEulerAngles.x, endRotationX, speed * Time.deltaTime);
            Vector3 newEuler = targetTransform.localEulerAngles;
            newEuler.x = nextX;
            targetTransform.localEulerAngles = newEuler;
            
            // Bir sonraki frame'i bekle
            yield return null;
        }

        // Tam olarak hedef açıya sabitle
        Vector3 finalEuler = targetTransform.localEulerAngles;
        finalEuler.x = endRotationX;
        targetTransform.localEulerAngles = finalEuler;
    }
}
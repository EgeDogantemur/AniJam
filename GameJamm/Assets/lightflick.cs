using UnityEngine;

public class LightToggle : MonoBehaviour
{
    [Tooltip("Açıp kapatmak istediğiniz ışık objesini buraya sürükleyin.")]
    public Light hedefIsik; 

    [Tooltip("Işığın kaç saniyede bir yanıp söneceğini belirler.")]
    public float yanipSonmeHizi = 1.0f; // Varsayılan olarak 1 saniyede bir

    void Start()
    {
        // Eğer Inspector'dan ışık atanmadıysa, scriptin eklendiği objedeki ışığı otomatik bulmaya çalışır
        if (hedefIsik == null)
        {
            hedefIsik = GetComponent<Light>();
        }

        // Işık başarıyla bulunduysa veya atandıysa yanıp sönme işlemini başlat
        if (hedefIsik != null)
        {
            // "IsigiAcKapat" isimli fonksiyonu hemen (0 saniye sonra) başlat ve "yanipSonmeHizi" süresinde bir tekrarla
            InvokeRepeating("IsigiAcKapat", 0f, yanipSonmeHizi);
        }
        else
        {
            Debug.LogWarning("Hedef ışık atanmadı ve bu objede bir ışık bileşeni bulunamadı!");
        }
    }

    // Bu fonksiyon InvokeRepeating tarafından sürekli olarak çağrılır
    void IsigiAcKapat()
    {
        // Işığın mevcut durumunun tersini alır
        hedefIsik.enabled = !hedefIsik.enabled;
    }
}
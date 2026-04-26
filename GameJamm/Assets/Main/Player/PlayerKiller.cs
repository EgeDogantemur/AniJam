using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerKiller : MonoBehaviour
{
    public string enemyTag = "Enemy";

    private void Update()
    {
        if (transform.position.y < -100)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.tag);
        if(other.CompareTag(enemyTag))
        {
            Debug.Log("Player died");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}

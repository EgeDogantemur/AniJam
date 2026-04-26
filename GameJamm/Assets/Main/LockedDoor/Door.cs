using UnityEngine;

public class Door : MonoBehaviour
{
    private Collider col;

    private void Start()
    {
        col = GetComponent<Collider>();
    }

    public void OpenDoor()
    {
        col.enabled = false;
        transform.Rotate(0, 90, 0);
    }

    public void CloseDoor()
    {
        col.enabled = true;
        transform.Rotate(0, -90, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OpenDoor();
        }
        if (other.GetComponentInParent<PlayerController>())
        {
            OpenDoor();
        }
        Debug.Log(other.tag);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CloseDoor();
        }
    }
}

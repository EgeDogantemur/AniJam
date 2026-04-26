using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    public Door parentDoor;

    public void Interact(GameObject interactor)
    {
        if (parentDoor != null)
        {
            parentDoor.InteractWithLock(interactor);
        }
    }

    public void SetHighlight(bool state)
    {
        if (parentDoor != null)
        {
            parentDoor.SetHighlightForLock(state);
        }
    }
}

using UnityEngine;

public interface IInteractable
{
    void Interact(GameObject interactor);
    void SetHighlight(bool state);
}

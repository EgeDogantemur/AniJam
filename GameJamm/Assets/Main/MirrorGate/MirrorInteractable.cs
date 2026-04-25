using UnityEngine;

public class MirrorInteractable : MonoBehaviour, IInteractable
{
    public Mirror parentMirror;

    public void Interact(GameObject interactor)
    {
        if (parentMirror != null)
        {
            parentMirror.Interact(interactor);
        }
    }

    public void SetHighlight(bool state)
    {
        if (parentMirror != null)
        {
            parentMirror.SetHighlight(state);
        }
    }
}

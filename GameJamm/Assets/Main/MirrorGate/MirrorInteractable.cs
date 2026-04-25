using UnityEngine;

public class MirrorInteractable : MonoBehaviour, IInteractable
{
    public Mirror parentMirror;

    public void Interact(GameObject interactor)
    {
        if (parentMirror != null)
        {
            parentMirror.InteractWithJoint(interactor, this.gameObject);
        }
    }

    public void SetHighlight(bool state)
    {
        if (parentMirror != null)
        {
            parentMirror.SetHighlightForJoint(state, this.gameObject);
        }
    }
}

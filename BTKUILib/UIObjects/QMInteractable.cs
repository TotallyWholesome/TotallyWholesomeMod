namespace BTKUILib.UIObjects
{

    /// <summary>
    /// This object is the base class for button and toggles
    /// </summary>
    public class QMInteractable : QMUIElement
    {
        internal QMInteractable()
        {
            UserInterface.Interactables.Add(UUID, this);
        }

        internal virtual void OnInteraction(bool? toggle = null)
        {

        }

        internal override void DeleteInternal(bool tabChange = false)
        {
            base.DeleteInternal(tabChange);

            UserInterface.Interactables.Remove(UUID);
        }
    }
}
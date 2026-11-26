// P1-3 can implement this on a MonoBehaviour and assign it to the selection UI.
// Return false to reject the choice without consuming a pending level-up.
public interface IRunCardSelectionHandler
{
    bool TrySelectCard(RunCardData card);
}

// The handler commits the effect and consumes exactly one pending level-up.
// Return false without changing stats or rewards when the choice is rejected.
public interface IRunCardSelectionHandler
{
    bool TrySelectCard(RunCardData card);
}

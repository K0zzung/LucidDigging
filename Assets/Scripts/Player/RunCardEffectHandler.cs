using System;
using UnityEngine;

public class RunCardEffectHandler : MonoBehaviour, IRunCardSelectionHandler
{
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private RunProgression progression;
    [SerializeField] private GameFlowController gameFlow;
    private bool committing;

    private void Start() {
        if (progression == null) progression = RunProgression.Instance;
        if (gameFlow == null) gameFlow = GameFlowController.Instance;
        if (progression != null) progression.OnRunReset += ResetRunBonuses;
    }

    public bool TrySelectCard(RunCardData card) {
        if (committing || !isActiveAndEnabled || card == null || !card.HasValidEffect ||
            playerStatus == null || playerStatus.IsDead || progression == null ||
            progression.PendingLevelUps <= 0 || gameFlow == null || !gameFlow.CanConfirmRunCard)
            return false;

        committing = true;
        bool effectApplied = false;
        try {
            try {
                if (!progression.TryConsumePendingLevelUp(() => {
                    // No callbacks occur between mutation and reward consumption.
                    playerStatus.ApplyRunCard(card);
                    effectApplied = true;
                })) return false;
            }
            catch (Exception exception) {
                Debug.LogException(exception, this);
                // A failing observer must not make a committed card retryable.
                if (!effectApplied) return false;
            }

            try {
                playerStatus.NotifyRunStatsChanged();
            }
            catch (Exception exception) {
                Debug.LogException(exception, this);
            }
            return true;
        }
        finally {
            committing = false;
        }
    }

    private void ResetRunBonuses() {
        if (playerStatus != null) playerStatus.ResetRunBonuses();
    }

    private void OnDestroy() {
        if (progression != null) progression.OnRunReset -= ResetRunBonuses;
    }
}

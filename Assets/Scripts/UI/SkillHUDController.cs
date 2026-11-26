using UnityEngine;

public class SkillHUDController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField]
    private RunSkillManager runSkillManager;

    [Header("Slots")]
    [SerializeField]
    private SkillSlotUI dashSlot;

    [SerializeField]
    private SkillSlotUI shockwaveSlot;

    [SerializeField]
    private SkillSlotUI shieldSlot;


    private void Start() {
        if (runSkillManager == null) {
            runSkillManager =
                RunSkillManager.Instance;
        }

        if (runSkillManager == null) {
            Debug.LogError(
                "[SkillHUD] RunSkillManager ¾øÀ½");

            return;
        }

        runSkillManager.OnSkillChargeChanged +=
            HandleSkillChargeChanged;

        RefreshAll();
    }


    private void OnDestroy() {
        if (runSkillManager != null) {
            runSkillManager.OnSkillChargeChanged -=
                HandleSkillChargeChanged;
        }
    }


    private void HandleSkillChargeChanged(
        SkillType type,
        int charges) {
        bool available =
            charges > 0;

        switch (type) {
            case SkillType.Dash:
                dashSlot.SetUnlocked(
                    available);
                break;

            case SkillType.Shockwave:
                shockwaveSlot.SetUnlocked(
                    available);
                break;

            case SkillType.Shield:
                shieldSlot.SetUnlocked(
                    available);
                break;
        }
    }


    private void RefreshAll() {
        dashSlot.SetUnlocked(
            runSkillManager.HasSkill(
                SkillType.Dash));

        shockwaveSlot.SetUnlocked(
            runSkillManager.HasSkill(
                SkillType.Shockwave));

        shieldSlot.SetUnlocked(
            runSkillManager.HasSkill(
                SkillType.Shield));
    }
}
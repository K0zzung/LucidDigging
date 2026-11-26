using UnityEngine;
using TMPro;

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


    [SerializeField] private TMP_FontAsset runSkillFont;

    private void Start() {
        if (runSkillManager == null) {
            runSkillManager =
                RunSkillManager.Instance;
        }

        if (runSkillManager == null) {
            Debug.LogError(
                "[SkillHUD] RunSkillManager 없음");

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
        SkillSlotUI slot = type == SkillType.Dash ? dashSlot : type == SkillType.Shield ? shieldSlot : shockwaveSlot;
        if (slot != null) slot.SetRunState(charges, runSkillManager.GetEvolutionLevel(type), runSkillFont);
    }


    private void RefreshAll() {
        HandleSkillChargeChanged(SkillType.Dash, runSkillManager.GetCharges(SkillType.Dash));
        HandleSkillChargeChanged(SkillType.Shield, runSkillManager.GetCharges(SkillType.Shield));
        HandleSkillChargeChanged(SkillType.Shockwave, runSkillManager.GetCharges(SkillType.Shockwave));
    }
}

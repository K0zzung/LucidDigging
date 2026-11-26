using UnityEngine;

public class SkillItemPickup : GridPickup
{
    [SerializeField]
    private SkillType skillType;


    protected override bool TryApplyPickupEffect() {
        if (RunSkillManager.Instance == null) {
            Debug.LogError(
                "[SkillItemPickup] RunSkillManager가 없습니다.");

            return false;
        }


        bool acquired = RunSkillManager.Instance.TryAddSkillItem(skillType);


        if (!acquired) {
            Debug.Log(
                $"[SkillItem] {skillType} 이미 보유 중");

            return false;
        }


        // =============================
        // Pickup Sound
        // =============================

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlayItemPickup();
        }


        Debug.Log(
            $"[SkillItem] {skillType} 획득");


        return true;
    }
}
using System;
using UnityEngine;

public class RunSkillManager : MonoBehaviour
{
    public static RunSkillManager Instance {
        get;
        private set;
    }

    [Header("Skill Item")]
    [SerializeField]
    private int maxChargesPerSkill = 1;

    public int DashCharges { get; private set; }
    public int ShockwaveCharges { get; private set; }
    public int ShieldCharges { get; private set; }

    // Skill 종류 / 현재 남은 개수
    public event Action<SkillType, int> OnSkillChargeChanged;


    private void Awake() {
        if (Instance != null &&
            Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResetRunSkills();
    }


    public bool HasSkill(SkillType type) {
        return GetCharges(type) > 0;
    }


    public int GetCharges(SkillType type) {
        switch (type) {
            case SkillType.Dash:
                return DashCharges;

            case SkillType.Shockwave:
                return ShockwaveCharges;

            case SkillType.Shield:
                return ShieldCharges;
        }

        return 0;
    }


    /// <summary>
    /// Item 획득.
    /// 최대 개수라면 false.
    /// </summary>
    public bool TryAddSkillItem(SkillType type) {
        int current =
            GetCharges(type);

        if (current >= maxChargesPerSkill) {
            Debug.Log(
                $"[Skill] {type} Item 이미 보유 중");

            return false;
        }

        switch (type) {
            case SkillType.Dash:
                DashCharges++;
                break;

            case SkillType.Shockwave:
                ShockwaveCharges++;
                break;

            case SkillType.Shield:
                ShieldCharges++;
                break;
        }

        int newCharges =
            GetCharges(type);

        Debug.Log(
            $"[Skill] {type} Item 획득 / Charge : {newCharges}");

        OnSkillChargeChanged?.Invoke(
            type,
            newCharges);

        return true;
    }


    /// <summary>
    /// Skill Item 1개 소비.
    /// </summary>
    public bool TryConsumeSkill(SkillType type) {
        if (!HasSkill(type))
            return false;

        switch (type) {
            case SkillType.Dash:
                DashCharges--;
                break;

            case SkillType.Shockwave:
                ShockwaveCharges--;
                break;

            case SkillType.Shield:
                ShieldCharges--;
                break;
        }

        int newCharges =
            GetCharges(type);

        Debug.Log(
            $"[Skill] {type} 사용 / 남은 Charge : {newCharges}");

        OnSkillChargeChanged?.Invoke(
            type,
            newCharges);

        return true;
    }


    public void ResetRunSkills() {
        DashCharges = 0;
        ShockwaveCharges = 0;
        ShieldCharges = 0;
    }
}
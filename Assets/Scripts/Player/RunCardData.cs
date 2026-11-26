using UnityEngine;

public enum RunCardEffectType
{
    Attack,
    Defense,
    MaxHealth,
    Heal,
    AttackAndDefense
}

[CreateAssetMenu(fileName = "RunCard", menuName = "Game/Run Card")]
public class RunCardData : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private RunCardRarity rarity;
    [SerializeField] private string baseCardId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private RunCardEffectType effectType;
    [SerializeField, Min(0)] private int attackBonus;
    [SerializeField, Min(0)] private int defenseBonus;
    [SerializeField, Min(0)] private int maxHealthBonus;
    [SerializeField, Min(0)] private int healAmount;
    [Header("Nightmare - Run Penalties")]
    [SerializeField, Min(0)] private int attackPenalty;
    [SerializeField, Min(0)] private int defensePenalty;
    [SerializeField, Min(0)] private int maxHealthPenalty;

    public string Id => id;
    public RunCardRarity Rarity => rarity;
    public string BaseCardId => baseCardId;
    public string DisplayName => displayName;
    public string Description => string.IsNullOrWhiteSpace(description)
        ? EffectDescription : EffectDescription + "\n" + description;
    public string PositiveEffectDescription => EffectSummary;
    public string EffectDescription => HasPenalty ? EffectSummary + "\n" + PenaltyDescription : EffectSummary;
    public RunCardEffectType EffectType => effectType;
    public int AttackBonus => Mathf.Max(0, attackBonus);
    public int DefenseBonus => Mathf.Max(0, defenseBonus);
    public int MaxHealthBonus => Mathf.Max(0, maxHealthBonus);
    public int HealAmount => Mathf.Max(0, healAmount);
    public int AttackPenalty => Mathf.Max(0, attackPenalty);
    public int DefensePenalty => Mathf.Max(0, defensePenalty);
    public int MaxHealthPenalty => Mathf.Max(0, maxHealthPenalty);
    public bool HasPenalty => AttackPenalty > 0 || DefensePenalty > 0 || MaxHealthPenalty > 0;

    public string PenaltyDescription {
        get {
            var parts = new System.Collections.Generic.List<string>();
            if (AttackPenalty > 0) parts.Add($"공격력 -{AttackPenalty}");
            if (DefensePenalty > 0) parts.Add($"방어력 -{DefensePenalty}");
            if (MaxHealthPenalty > 0) parts.Add($"최대 체력 -{MaxHealthPenalty}");
            return string.Join(" / ", parts);
        }
    }

    public bool HasValidEffect {
        get {
            if (!System.Enum.IsDefined(typeof(RunCardRarity), rarity) ||
                attackBonus < 0 || defenseBonus < 0 || maxHealthBonus < 0 || healAmount < 0 ||
                attackPenalty < 0 || defensePenalty < 0 || maxHealthPenalty < 0 ||
                (rarity == RunCardRarity.Nightmare ? !HasPenalty : HasPenalty))
                return false;
            switch (effectType) {
                case RunCardEffectType.Attack: return AttackBonus > 0;
                case RunCardEffectType.Defense: return DefenseBonus > 0;
                case RunCardEffectType.MaxHealth: return MaxHealthBonus > 0;
                case RunCardEffectType.Heal: return HealAmount > 0;
                case RunCardEffectType.AttackAndDefense: return AttackBonus > 0 || DefenseBonus > 0;
                default: return false;
            }
        }
    }

    private string EffectSummary {
        get {
            switch (effectType) {
                case RunCardEffectType.Attack: return $"이번 탐험 공격력 +{AttackBonus}";
                case RunCardEffectType.Defense: return $"이번 탐험 방어력 +{DefenseBonus}";
                case RunCardEffectType.MaxHealth: return $"최대 체력 +{MaxHealthBonus} / 현재 체력 +{MaxHealthBonus}";
                case RunCardEffectType.Heal: return $"체력 {HealAmount} 회복 (최대 체력 초과 불가)";
                case RunCardEffectType.AttackAndDefense: return $"공격력 +{AttackBonus} / 방어력 +{DefenseBonus}";
                default: return "유효하지 않은 카드 효과";
            }
        }
    }
}

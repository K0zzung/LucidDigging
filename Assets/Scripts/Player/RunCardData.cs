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
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private RunCardEffectType effectType;
    [SerializeField, Min(0)] private int attackBonus;
    [SerializeField, Min(0)] private int defenseBonus;
    [SerializeField, Min(0)] private int maxHealthBonus;
    [SerializeField, Min(0)] private int healAmount;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => EffectSummary + "\n" + description;
    public RunCardEffectType EffectType => effectType;
    public int AttackBonus => Mathf.Max(0, attackBonus);
    public int DefenseBonus => Mathf.Max(0, defenseBonus);
    public int MaxHealthBonus => Mathf.Max(0, maxHealthBonus);
    public int HealAmount => Mathf.Max(0, healAmount);

    public bool HasValidEffect {
        get {
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
                case RunCardEffectType.Attack: return $"Run attack +{AttackBonus}";
                case RunCardEffectType.Defense: return $"Run defense +{DefenseBonus}";
                case RunCardEffectType.MaxHealth: return $"Max HP +{MaxHealthBonus}, current HP +{MaxHealthBonus}";
                case RunCardEffectType.Heal: return $"Restore {HealAmount} HP (up to max HP)";
                case RunCardEffectType.AttackAndDefense: return $"Run attack +{AttackBonus}, defense +{DefenseBonus}";
                default: return "Invalid card effect";
            }
        }
    }
}

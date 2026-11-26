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

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public RunCardEffectType EffectType => effectType;
}

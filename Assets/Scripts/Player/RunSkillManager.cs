using System;
using UnityEngine;

public readonly struct RunSkillState
{
    public int Charges { get; }
    public int TotalPickups { get; }
    public int EvolutionLevel => TotalPickups == 0 ? 0 : TotalPickups == 1 ? 1 : TotalPickups < 4 ? 2 : 3;
    public RunSkillState(int charges, int pickups) { Charges = charges; TotalPickups = pickups; }
}

public class RunSkillManager : MonoBehaviour
{
    public static RunSkillManager Instance { get; private set; }
    public const int MaxChargesPerSkill = 3;
    private readonly RunSkillState[] states = new RunSkillState[3];
    private PlayerStatus player;
    public int DashCharges => GetCharges(SkillType.Dash);
    public int ShockwaveCharges => GetCharges(SkillType.Shockwave);
    public int ShieldCharges => GetCharges(SkillType.Shield);
    // Remains compatible with the existing pickup and HUD contract.
    public event Action<SkillType, int> OnSkillChargeChanged;

    private void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ResetRunSkills();
    }
    private void Start() {
        player = FindObjectOfType<PlayerStatus>();
        if (player != null) player.OnPlayerDied += ResetRunSkills;
    }
    private void OnDestroy() {
        if (player != null) player.OnPlayerDied -= ResetRunSkills;
        if (Instance == this) Instance = null;
    }
    private static bool Valid(SkillType type) => type == SkillType.Dash || type == SkillType.Shield || type == SkillType.Shockwave;
    public RunSkillState GetState(SkillType type) => Valid(type) ? states[(int)type] : default;
    public int GetCharges(SkillType type) => GetState(type).Charges;
    public int GetTotalPickups(SkillType type) => GetState(type).TotalPickups;
    public int GetEvolutionLevel(SkillType type) => GetState(type).EvolutionLevel;
    public bool HasSkill(SkillType type) => GetCharges(type) > 0;

    public bool TryAddSkillItem(SkillType type) {
        if (!Valid(type) || (player != null && player.IsDead)) return false;
        RunSkillState state = GetState(type);
        if (state.Charges >= MaxChargesPerSkill) return false;
        states[(int)type] = new RunSkillState(state.Charges + 1,
            state.TotalPickups == int.MaxValue ? int.MaxValue : state.TotalPickups + 1);
        Notify(type);
        return true;
    }
    public bool TryConsumeSkill(SkillType type) {
        if (!Valid(type) || (player != null && player.IsDead)) return false;
        RunSkillState state = GetState(type);
        if (state.Charges <= 0) return false;
        states[(int)type] = new RunSkillState(state.Charges - 1, state.TotalPickups);
        Notify(type);
        return true;
    }
    // Shockwave is one charged attack, not repeated one-charge casts.
    public bool TryConsumeShockwave(out int chargesAtCast) {
        chargesAtCast = 0;
        if (player != null && player.IsDead) return false;
        RunSkillState state = GetState(SkillType.Shockwave);
        if (state.Charges <= 0) return false;
        chargesAtCast = state.Charges;
        states[(int)SkillType.Shockwave] = new RunSkillState(0, state.TotalPickups);
        Notify(SkillType.Shockwave);
        return true;
    }

    public void ResetRunSkills() {
        // Clear all three before notifying, so observers see a consistent new run.
        Array.Clear(states, 0, states.Length);
        Notify(SkillType.Dash); Notify(SkillType.Shield); Notify(SkillType.Shockwave);
    }
    private void Notify(SkillType type) {
        if (OnSkillChargeChanged == null) return;
        // A HUD observer failure must not make an already committed pickup retryable.
        foreach (Action<SkillType, int> handler in OnSkillChargeChanged.GetInvocationList()) {
            try { handler(type, GetCharges(type)); }
            catch (Exception error) { Debug.LogException(error, this); }
        }
    }
}

using System;
using UnityEngine;

public class RunProgression : MonoBehaviour
{
    public static RunProgression Instance { get; private set; }

    [Header("Experience Required")]
    [SerializeField, Min(1)] private int firstLevelExperience = 20;
    [SerializeField, Min(0)] private int experienceIncreasePerLevel = 10;

    public int Level { get; private set; } = 1;
    public int CurrentExperience { get; private set; }
    public int PendingLevelUps { get; private set; }
    public int RequiredExperience => (int)Math.Min(int.MaxValue,
        Math.Max(1, firstLevelExperience) +
        (long)(Level - 1) * Math.Max(0, experienceIncreasePerLevel));

    public event Action OnProgressionChanged;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this);
            return;
        }

        Instance = this;
        ResetRun();
    }

    public void AddExperience(int amount) {
        if (amount <= 0)
            return;

        long remaining = (long)CurrentExperience + amount;
        while (remaining >= RequiredExperience) {
            remaining -= RequiredExperience;
            Level++;
            PendingLevelUps++;
        }

        CurrentExperience = (int)remaining;
        Debug.Log($"[RunProgression] XP +{amount} / Level {Level} / " +
            $"XP {CurrentExperience}/{RequiredExperience} / Pending {PendingLevelUps}");
        OnProgressionChanged?.Invoke();
    }

    public bool TryConsumePendingLevelUp() {
        if (PendingLevelUps <= 0)
            return false;

        PendingLevelUps--;
        OnProgressionChanged?.Invoke();
        return true;
    }

    public void ResetRun() {
        Level = 1;
        CurrentExperience = 0;
        PendingLevelUps = 0;
        OnProgressionChanged?.Invoke();
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }
}

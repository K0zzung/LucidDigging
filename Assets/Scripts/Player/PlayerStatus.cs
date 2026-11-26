using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    private int permanentMaxHP;
    public int MaxHP => ClampFinalStat((long)permanentMaxHP + RunMaxHPBonus - RunMaxHPPenalty, 1);
    public int RunAttackBonus { get; private set; }
    public int RunDefenseBonus { get; private set; }
    public int RunMaxHPBonus { get; private set; }
    public int RunAttackPenalty { get; private set; }
    public int RunDefensePenalty { get; private set; }
    public int RunMaxHPPenalty { get; private set; }

    private static int ClampFinalStat(long value, int minimum) {
        return (int)Math.Max(minimum, Math.Min(int.MaxValue, value));
    }
    public int CurrentHP { get; private set; }
    public int BaseAttack {
        get;
        private set;
    }


    public int Attack {
        get {
            int weaponAttack =
                0;


            if (WeaponManager.Instance != null) {
                weaponAttack =
                    WeaponManager.Instance
                        .GetAdditionalAttack();
            }


            return ClampFinalStat((long)BaseAttack + weaponAttack + RunAttackBonus - RunAttackPenalty, 1);
        }
    }

    public event Action OnPlayerDied;
    public event Action OnStatsChanged;
    public event Action<int, int> OnHPChanged;
    public bool IsDead { get; private set; }

    public int BaseDefense {
        get;
        private set;
    }

    private int temporaryDefenseBonus;

    public int Defense =>
        ClampFinalStat((long)BaseDefense + RunDefenseBonus + temporaryDefenseBonus - RunDefensePenalty, 0);
    public bool IsInvincible { get; private set; }



    private void Awake() {
        LoadStats();
        CurrentHP = MaxHP;
    }

    private void LoadStats() {
        permanentMaxHP =
            PlayerPrefs.GetInt(
                "Player_MaxHP",
                100);


        BaseAttack =
            PlayerPrefs.GetInt(
                "Player_Attack",
                10);


        BaseDefense =
            PlayerPrefs.GetInt(
                "Player_Defense",
                5);
    }

    private void Die() {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log("[Player] Dead");
        ResetRunBonuses();

        OnPlayerDied?.Invoke();
    }


    public void SaveStats() {
        PlayerPrefs.SetInt(
            "Player_MaxHP",
            permanentMaxHP);


        PlayerPrefs.SetInt(
            "Player_Attack",
            BaseAttack);


        PlayerPrefs.SetInt(
            "Player_Defense",
            BaseDefense);


        PlayerPrefs.Save();
    }

    public void TakeDamage(
    int damage) {
        if (IsDead)
            return;


        if (IsInvincible) {
            Debug.Log(
                "Invincible - Damage Ignored");

            return;
        }


        if (damage <= 0)
            return;


        int beforeHP =
            CurrentHP;


        CurrentHP =
            Mathf.Max(
                0,
                CurrentHP - damage);


        // =============================
        // Floating Damage
        // =============================

        if (FloatingDamageManager.Instance != null) {
            FloatingDamageManager.Instance
                .ShowReceivedDamage(
                    transform.position
                    + Vector3.up * 0.45f,
                    damage);
        }


        // =============================
        // HP UI
        // =============================

        OnHPChanged?.Invoke(
            CurrentHP,
            MaxHP);


        Debug.Log(
            $"[Player Damage] " +
            $"Damage : {damage} / " +
            $"HP : {beforeHP} -> {CurrentHP}");


        if (CurrentHP <= 0) {
            Die();
        }
    }
    public void IncreasePermanentMaxHP(
    int amount) {
        if (amount <= 0)
            return;


        permanentMaxHP +=
            amount;


        // 최대 HP가 오른 만큼 현재 HP도 같이 증가
        CurrentHP =
            Mathf.Min(
                MaxHP,
                CurrentHP + amount);


        SaveStats();


        OnHPChanged?.Invoke(
            CurrentHP,
            MaxHP);


        Debug.Log(
            $"[Player Upgrade] " +
            $"MaxHP +{amount} / " +
            $"MaxHP : {MaxHP}");
    }


    public void IncreasePermanentAttack(
        int amount) {
        if (amount <= 0)
            return;


        BaseAttack +=
            amount;


        SaveStats();


        Debug.Log(
            $"[Player Upgrade] " +
            $"Attack +{amount} / " +
            $"BaseAttack : {BaseAttack}");
    }


    public void IncreasePermanentDefense(
        int amount) {
        if (amount <= 0)
            return;


        BaseDefense +=
            amount;


        SaveStats();


        Debug.Log(
            $"[Player Upgrade] " +
            $"Defense +{amount} / " +
            $"BaseDefense : {BaseDefense}");
    }
    // Called only by the validated card transaction; publish HUD events after commit.
    internal void ApplyRunCard(RunCardData card) {
        if (card == null || !card.HasValidEffect || IsDead || CurrentHP <= 0)
            throw new InvalidOperationException("Cannot apply this card to the current run.");

        // Prepare all values before mutation. Overflow/validation failure leaves stats
        // and the pending reward untouched because this runs inside the reward callback.
        int attack = RunAttackBonus;
        int defense = RunDefenseBonus;
        int health = RunMaxHPBonus;
        int addedHealth = 0;
        int heal = 0;
        checked {
            switch (card.EffectType) {
                case RunCardEffectType.Attack: attack += card.AttackBonus; break;
                case RunCardEffectType.Defense: defense += card.DefenseBonus; break;
                case RunCardEffectType.MaxHealth:
                    addedHealth = card.MaxHealthBonus;
                    health += addedHealth;
                    break;
                case RunCardEffectType.Heal: heal = card.HealAmount; break;
                case RunCardEffectType.AttackAndDefense:
                    attack += card.AttackBonus;
                    defense += card.DefenseBonus;
                    break;
            }
            int attackLoss = RunAttackPenalty + card.AttackPenalty;
            int defenseLoss = RunDefensePenalty + card.DefensePenalty;
            int healthLoss = RunMaxHPPenalty + card.MaxHealthPenalty;
            int nextMaxHP = ClampFinalStat((long)permanentMaxHP + health - healthLoss, 1);
            long nextHP = Math.Min(nextMaxHP, (long)CurrentHP + addedHealth);
            nextHP = Math.Min(nextMaxHP, nextHP + heal);

            RunAttackBonus = attack;
            RunDefenseBonus = defense;
            RunMaxHPBonus = health;
            RunAttackPenalty = attackLoss;
            RunDefensePenalty = defenseLoss;
            RunMaxHPPenalty = healthLoss;
            CurrentHP = (int)Math.Max(1L, nextHP);
        }
    }

    internal void NotifyRunStatsChanged() {
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        OnStatsChanged?.Invoke();
    }

    public void ResetRunBonuses() {
        RunAttackBonus = 0;
        RunDefenseBonus = 0;
        RunMaxHPBonus = 0;
        RunAttackPenalty = 0;
        RunDefensePenalty = 0;
        RunMaxHPPenalty = 0;
        CurrentHP = Mathf.Min(CurrentHP, MaxHP);
        NotifyRunStatsChanged();
    }

    public void SetInvincible(bool invincible) {
        IsInvincible = invincible;
    }
    public void AddTemporaryDefense(int amount) {
        temporaryDefenseBonus +=
            Mathf.Max(0, amount);
    }


    public void RemoveTemporaryDefense(int amount) {
        temporaryDefenseBonus =
            Mathf.Max(
                0,
                temporaryDefenseBonus - amount);
    }

}


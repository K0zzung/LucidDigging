using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    private int permanentMaxHP;
    public int MaxHP => permanentMaxHP + RunMaxHPBonus;
    public int RunAttackBonus { get; private set; }
    public int RunDefenseBonus { get; private set; }
    public int RunMaxHPBonus { get; private set; }
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


            return
                BaseAttack
                + weaponAttack + RunAttackBonus;
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
        BaseDefense + RunDefenseBonus + temporaryDefenseBonus;
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
        switch (card.EffectType) {
            case RunCardEffectType.Attack:
                RunAttackBonus += card.AttackBonus;
                break;
            case RunCardEffectType.Defense:
                RunDefenseBonus += card.DefenseBonus;
                break;
            case RunCardEffectType.MaxHealth:
                RunMaxHPBonus += card.MaxHealthBonus;
                CurrentHP = Mathf.Min(MaxHP, CurrentHP + card.MaxHealthBonus);
                break;
            case RunCardEffectType.Heal:
                CurrentHP = Mathf.Min(MaxHP, CurrentHP + card.HealAmount);
                break;
            case RunCardEffectType.AttackAndDefense:
                RunAttackBonus += card.AttackBonus;
                RunDefenseBonus += card.DefenseBonus;
                break;
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


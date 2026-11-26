using System;
using UnityEngine;

public class PermanentUpgradeManager : MonoBehaviour
{
    public static PermanentUpgradeManager Instance {
        get;
        private set;
    }


    // =========================================================
    // Reference
    // =========================================================

    [Header("Reference")]

    [SerializeField]
    private PlayerStatus playerStatus;
    [SerializeField] private PlayerSkillController playerSkillController;

    // =========================================================
    // Stat Increase
    // =========================================================

    [Header("Stat Increase Per Level")]

    [SerializeField]
    private int hpIncreasePerLevel = 100;

    [SerializeField]
    private int attackIncreasePerLevel = 100;

    [SerializeField]
    private int defenseIncreasePerLevel = 1;


    // =========================================================
    // Cost
    // =========================================================

    [Header("Upgrade Cost")]

    [Tooltip("Lv.0 -> Lv.1 비용")]
    [SerializeField]
    private int baseUpgradeCost = 1;

    [Tooltip("레벨 1 증가마다 늘어나는 비용")]
    [SerializeField]
    private int costIncreasePerLevel = 1;


    // =========================================================
    // Event
    // =========================================================

    public event Action<PermanentUpgradeType, int>
        OnUpgradeLevelChanged;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake() {
        if (Instance != null &&
            Instance != this) {
            Destroy(
                gameObject);

            return;
        }


        Instance =
            this;
    }


    // =========================================================
    // Level
    // =========================================================

    public int GetLevel(
        PermanentUpgradeType type) {
        return PlayerPrefs.GetInt(
            GetLevelKey(type),
            0);
    }


    // =========================================================
    // Cost
    // =========================================================

    public int GetUpgradeCost(
        PermanentUpgradeType type) {
        int level =
            GetLevel(
                type);


        return Mathf.Max(
            1,
            baseUpgradeCost
            + level
            * costIncreasePerLevel);
    }


    // =========================================================
    // Can Upgrade
    // =========================================================

    public bool CanUpgrade(
        PermanentUpgradeType type,
        CurrencyType currencyType) {
        if (CurrencyManager.Instance == null)
            return false;


        // 스탯 강화인데 PlayerStatus가 없으면
        // 재화를 사용하지 못하게 막음
        if (IsStatUpgrade(type) &&
            playerStatus == null) {
            return false;
        }


        int cost =
            GetUpgradeCost(
                type);


        int currentCurrency =
            CurrencyManager.Instance
                .GetCurrency(
                    currencyType);


        return
            currentCurrency >= cost;
    }


    // =========================================================
    // Upgrade
    // =========================================================

    public bool TryUpgrade(
        PermanentUpgradeType type,
        CurrencyType currencyType) {
        if (!CanUpgrade(
            type,
            currencyType)) {
            return false;
        }


        int cost =
            GetUpgradeCost(
                type);


        // =============================
        // 재화 차감
        // =============================

        bool spent =
            CurrencyManager.Instance
                .SpendCurrency(
                    currencyType,
                    cost);


        if (!spent)
            return false;


        // =============================
        // 실제 스탯 상승
        // =============================

        ApplyUpgradeEffect(
            type);


        // =============================
        // 강화 Lv 저장
        // =============================

        int nextLevel =
            GetLevel(type)
            + 1;


        PlayerPrefs.SetInt(
            GetLevelKey(type),
            nextLevel);


        PlayerPrefs.Save();


        OnUpgradeLevelChanged?.Invoke(
            type,
            nextLevel);


        Debug.Log(
            $"[Permanent Upgrade] " +
            $"{type} Lv.{nextLevel} / " +
            $"Cost : {cost} {currencyType}");


        return true;
    }


    // =========================================================
    // Effect
    // =========================================================

    private void ApplyUpgradeEffect(
        PermanentUpgradeType type) {
        if (playerStatus == null)
            return;


        switch (type) {
            case PermanentUpgradeType.HP:

                playerStatus
                    .IncreasePermanentMaxHP(
                        hpIncreasePerLevel);

                break;


            case PermanentUpgradeType.Attack:

                playerStatus
                    .IncreasePermanentAttack(
                        attackIncreasePerLevel);

                break;


            case PermanentUpgradeType.Defense:

                playerStatus
                    .IncreasePermanentDefense(
                        defenseIncreasePerLevel);

                break;


            // Skill은 지금은 Lv만 저장.
            // 실제 효과 연결은 Item 강화 만들 때 붙이면 됨.
            case PermanentUpgradeType.Dash:
            case PermanentUpgradeType.Shield:
            case PermanentUpgradeType.Shockwave:

                break;
        }
    }


    // =========================================================
    // Utility
    // =========================================================

    private bool IsStatUpgrade(
        PermanentUpgradeType type) {
        return
            type == PermanentUpgradeType.HP ||
            type == PermanentUpgradeType.Attack ||
            type == PermanentUpgradeType.Defense;
    }


    private string GetLevelKey(
        PermanentUpgradeType type) {
        return
            $"PermanentUpgrade_{type}_Level";
    }

    public string GetCurrentValueText(
    PermanentUpgradeType type) {
        switch (type) {
            // =============================
            // Status
            // =============================

            case PermanentUpgradeType.HP:

                if (playerStatus == null)
                    return "HP: -";


                return
                    $"HP: {playerStatus.MaxHP}";


            case PermanentUpgradeType.Attack:

                if (playerStatus == null)
                    return "ATK: -";


                return
                    $"ATK: {playerStatus.BaseAttack}";


            case PermanentUpgradeType.Defense:

                if (playerStatus == null)
                    return "DEF: -";


                return
                    $"DEF: {playerStatus.Defense}";


            // =============================
            // Skill
            // =============================

            case PermanentUpgradeType.Dash:

                if (playerSkillController == null)
                    return "DASH: -";


                return
                    $"DASH: " +
                    $"{playerSkillController.GetCurrentDashAttackPercent():0}%";


            case PermanentUpgradeType.Shockwave:

                if (playerSkillController == null)
                    return "SHOCKWAVE: -";


                return
                    $"SHOCKWAVE: " +
                    $"{playerSkillController.GetCurrentShockwaveAttackPercent():0}%";


            case PermanentUpgradeType.Shield:

                if (playerSkillController == null)
                    return "SHIELD: -";


                return
                    $"SHIELD: DEF +" +
                    $"{playerSkillController.GetCurrentShieldDefenseBonus()}";
        }


        return "-";
    }
}
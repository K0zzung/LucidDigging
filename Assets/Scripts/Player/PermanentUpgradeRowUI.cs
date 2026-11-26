using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PermanentUpgradeRowUI : MonoBehaviour
{
    // =========================================================
    // Upgrade Setting
    // =========================================================

    [Header("Upgrade Setting")]

    [SerializeField]
    private PermanentUpgradeType upgradeType;

    [SerializeField]
    private CurrencyType currencyType;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private TMP_Text levelText;

    [SerializeField]
    private TMP_Text costText;

    [SerializeField]
    private Button upgradeButton;

    [SerializeField]
    private TMP_Text currentValueText;

    [Tooltip("재화 부족 시 반투명해질 영역")]
    [SerializeField]
    private CanvasGroup upgradeButtonCanvasGroup;


    // =========================================================
    // Setting
    // =========================================================

    [Header("Visual")]

    [Range(0f, 1f)]
    [SerializeField]
    private float disabledAlpha = 0.35f;

    [Header("Upgrade Animation")]

    [SerializeField]
    private float textPunchScale = 0.18f;

    [SerializeField]
    private float textPunchDuration = 0.25f;


    // =========================================================
    // Unity
    // =========================================================

    private void Start() {
        SubscribeEvents();

        Refresh();
    }


    private void OnEnable() {
        Refresh();
    }


    private void OnDestroy() {
        UnsubscribeEvents();
    }


    // =========================================================
    // Event
    // =========================================================

    private void SubscribeEvents() {
        if (CurrencyManager.Instance != null) {
            CurrencyManager.Instance.OnCurrencyChanged +=
                Refresh;
        }


        if (PermanentUpgradeManager.Instance != null) {
            PermanentUpgradeManager.Instance.OnUpgradeLevelChanged +=
                HandleUpgradeLevelChanged;
        }
    }


    private void UnsubscribeEvents() {
        if (CurrencyManager.Instance != null) {
            CurrencyManager.Instance.OnCurrencyChanged -=
                Refresh;
        }


        if (PermanentUpgradeManager.Instance != null) {
            PermanentUpgradeManager.Instance.OnUpgradeLevelChanged -=
                HandleUpgradeLevelChanged;
        }
    }


    private void HandleUpgradeLevelChanged(
        PermanentUpgradeType changedType,
        int level) {
        if (changedType !=
            upgradeType) {
            return;
        }


        Refresh();
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void Refresh() {
        if (PermanentUpgradeManager.Instance == null)
            return;


        int level =
            PermanentUpgradeManager.Instance
                .GetLevel(
                    upgradeType);


        int cost =
            PermanentUpgradeManager.Instance
                .GetUpgradeCost(
                    upgradeType);


        // =============================
        // Level
        // =============================

        if (levelText != null) {
            levelText.text =
                $"Lv {level}";
        }


        // =============================
        // Cost
        // =============================

        if (costText != null) {
            costText.text =
                $"X{cost}";
        }


        // =============================
        // Current Value
        // =============================

        if (currentValueText != null) {
            currentValueText.text =
                PermanentUpgradeManager.Instance
                    .GetCurrentValueText(
                        upgradeType);
        }


        // =============================
        // Upgrade Button
        // =============================

        bool canUpgrade =
            PermanentUpgradeManager.Instance
                .CanUpgrade(
                    upgradeType,
                    currencyType);


        if (upgradeButton != null) {
            upgradeButton.interactable =
                canUpgrade;
        }


        if (upgradeButtonCanvasGroup != null) {
            upgradeButtonCanvasGroup.alpha =
                canUpgrade
                ? 1f
                : disabledAlpha;
        }
    }


    // =========================================================
    // Button
    // =========================================================

    public void OnClickUpgrade() {
        if (PermanentUpgradeManager.Instance == null)
            return;


        bool upgraded =
            PermanentUpgradeManager.Instance
                .TryUpgrade(
                    upgradeType,
                    currencyType);


        if (!upgraded)
            return;


        // 숫자 먼저 갱신
        Refresh();


        // =============================
        // Upgrade Text Animation
        // =============================

        PlayTextPunch(
            levelText);


        PlayTextPunch(
            currentValueText);


        PlayTextPunch(
            costText);
    }

    private void PlayTextPunch(
    TMP_Text targetText) {
        if (targetText == null)
            return;


        Transform target =
            targetText.transform;


        // 빠르게 연속 강화해도 Tween 겹침 방지
        target.DOKill(
            true);


        target.DOPunchScale(
            Vector3.one * textPunchScale,
            textPunchDuration,
            6,
            0.5f);
    }
}
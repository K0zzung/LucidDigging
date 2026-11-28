using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Only binds scene objects; creation is performed once by the explicit Editor command.
public sealed class ShopSkillPanel : MonoBehaviour
{
    [Serializable] public sealed class Row {
        public PermanentUpgradeType Type;
        public TMP_Text Level, Effect, Cost, BuyText;
        public Button Buy;
    }
    [SerializeField] private ShopPanelController shop;
    [SerializeField] private PlayerSkillController skills;
    [SerializeField] private Row[] rows;
    private PermanentUpgradeManager upgrades;
    private CurrencyManager currency;
    private bool wired, bound;
    private void OnEnable() { Bind(); Refresh(); }
    private void Start() { Bind(); Refresh(); }
    private void OnDisable() {
        if (!bound) return;
        if (upgrades != null) upgrades.OnUpgradeLevelChanged -= Changed;
        if (currency != null) currency.OnCurrencyChanged -= Refresh;
        bound = false;
    }
    private void Bind() {
        if (!wired && rows != null) {
            foreach (var row in rows) { var target = row; row.Buy.onClick.AddListener(() => Buy(target)); }
            wired = true;
        }
        if (bound || rows == null) return;
        upgrades = PermanentUpgradeManager.Instance; currency = CurrencyManager.Instance;
        if (upgrades == null || currency == null) return;
        upgrades.OnUpgradeLevelChanged += Changed;
        currency.OnCurrencyChanged += Refresh;
        bound = true;
    }
    private void Changed(PermanentUpgradeType type, int level) { Refresh(); }
    private void Buy(Row row) {
        if (!bound || shop == null || !shop.IsSkillTabOpen) return;
        upgrades.TryUpgrade(row.Type, upgrades.GetCurrencyType(row.Type));
        Refresh();
    }
    public void Refresh() {
        if (!bound) return;
        foreach (var row in rows) {
            int level = upgrades.GetLevel(row.Type), cost = upgrades.GetUpgradeCost(row.Type);
            bool max = level == int.MaxValue;
            row.Level.text = max ? $"Lv.{level}  MAX" : $"Lv.{level} → <color=#FFF270>Lv.{(long)level + 1}</color>";
            row.Effect.text = Preview(skills, row.Type);
            row.Cost.text = max ? "MAX" : $"x{cost}";
            row.Buy.interactable = shop != null && shop.IsSkillTabOpen && upgrades.CanUpgrade(row.Type, upgrades.GetCurrencyType(row.Type));
            row.BuyText.text = max ? "MAX" : currency.GetCurrency(upgrades.GetCurrencyType(row.Type)) < cost ? "재화 부족" : "강화";
        }
    }
    public static string Preview(PlayerSkillController source, PermanentUpgradeType type) {
        if (source == null) return "";
        float step = source.GetPermanentUpgradeStep(type);
        if (type == PermanentUpgradeType.Shield) {
            int current = source.GetCurrentShieldDefenseBonus();
            return $"일정 시간 방어력 증가\n방어력 +{current} → +{current + step:0}";
        }
        float value = type == PermanentUpgradeType.Dash ? source.GetCurrentDashAttackPercent() : source.GetCurrentShockwaveAttackPercent();
        string description = type == PermanentUpgradeType.Dash ? "진행 방향으로 돌진 공격" : "주변 범위에 충격파 공격";
        return $"{description}\n공격력 {value:0}% → {value + step:0}%";
    }
#if UNITY_EDITOR
    public void ConfigureEditor(ShopPanelController owner, PlayerSkillController source, Row[] offers) {
        shop = owner; skills = source; rows = offers;
    }
#endif
}

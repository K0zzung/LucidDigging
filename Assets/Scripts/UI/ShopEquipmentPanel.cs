using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Binds scene-authored UI only. All frames, slots and text exist in the Hierarchy.
public sealed class ShopEquipmentPanel : MonoBehaviour
{
    [Serializable] public sealed class UpgradeRow {
        public Image Icon;
        public TMP_Text Title, Level, Effect, Cost, BuyText;
        public Button Buy;
    }
    [Serializable] public sealed class EquipmentSlot {
        public WeaponData Item;
        public Button Button;
        public Image Icon, Selected;
        public TMP_Text State;
    }
    [SerializeField] private ShopPanelController shop;
    [SerializeField] private UpgradeRow weaponRow, pickaxeRow;
    [SerializeField] private EquipmentSlot[] slots;
    private WeaponData selectedWeapon, selectedPickaxe;
    private WeaponManager weapons;
    private PermanentUpgradeManager upgrades;
    private CurrencyManager currency;
    private bool wired, bound;
    private int selectionFrame = -1;

#if UNITY_EDITOR
    public void ConfigureEditor(ShopPanelController owner, UpgradeRow weapon, UpgradeRow pickaxe, EquipmentSlot[] equipment) {
        shop = owner; weaponRow = weapon; pickaxeRow = pickaxe; slots = equipment;
    }
#endif

    private void OnEnable() { Bind(); Refresh(); }
    private void Start() { Bind(); Refresh(); }
    private void OnDisable() { Unbind(); }

    private void Bind() {
        if (!wired) {
            if (slots == null || weaponRow == null || pickaxeRow == null) return;
            foreach (var slot in slots) {
                var captured = slot;
                if (slot != null && slot.Button != null) slot.Button.onClick.AddListener(() => Select(captured));
            }
            if (weaponRow.Buy != null) weaponRow.Buy.onClick.AddListener(() => Buy(false));
            if (pickaxeRow.Buy != null) pickaxeRow.Buy.onClick.AddListener(() => Buy(true));
            wired = true;
        }
        if (bound) return;
        weapons = WeaponManager.Instance;
        upgrades = PermanentUpgradeManager.Instance;
        currency = CurrencyManager.Instance;
        if (weapons == null || upgrades == null || currency == null) return;
        if (selectedWeapon == null) selectedWeapon = weapons.CurrentWeapon;
        if (selectedPickaxe == null && weapons.IsPickaxeActive) selectedPickaxe = weapons.ActiveEquipment;
        foreach (var slot in slots) {
            if (slot == null || slot.Item == null || !weapons.IsOwned(slot.Item)) continue;
            if (slot.Item.IsPickaxe && selectedPickaxe == null) selectedPickaxe = slot.Item;
            if (!slot.Item.IsPickaxe && selectedWeapon == null) selectedWeapon = slot.Item;
        }
        currency.OnCurrencyChanged += Refresh;
        upgrades.OnEquipmentUpgradeChanged += EquipmentChanged;
        weapons.OnWeaponUnlocked += EquipmentChanged;
        weapons.Changed += Refresh;
        bound = true;
    }
    private void Unbind() {
        if (!bound) return;
        if (currency != null) currency.OnCurrencyChanged -= Refresh;
        if (upgrades != null) upgrades.OnEquipmentUpgradeChanged -= EquipmentChanged;
        if (weapons != null) { weapons.OnWeaponUnlocked -= EquipmentChanged; weapons.Changed -= Refresh; }
        bound = false;
    }
    private void EquipmentChanged(WeaponData item) { Refresh(); }
    private void Select(EquipmentSlot slot) {
        if (!bound || shop == null || !shop.IsEquipmentTabOpen || slot.Item == null || !weapons.IsOwned(slot.Item)) return;
        if (!weapons.EquipFromShop(slot.Item, shop)) return;
        if (slot.Item.IsPickaxe) selectedPickaxe = slot.Item; else selectedWeapon = slot.Item;
        selectionFrame = Time.frameCount;
        // EquipFromShop reuses the real equipment events and saved selection.
        Refresh();
    }
    private void Buy(bool pickaxe) {
        if (!bound || shop == null || !shop.IsEquipmentTabOpen || selectionFrame == Time.frameCount) return;
        upgrades.TryUpgradeEquipment(pickaxe ? selectedPickaxe : selectedWeapon);
        Refresh();
    }
    public void Refresh() {
        if (!bound) return;
        RefreshRow(weaponRow, selectedWeapon, false);
        RefreshRow(pickaxeRow, selectedPickaxe, true);
        foreach (var slot in slots) {
            if (slot == null || slot.Item == null) continue;
            bool owned = weapons.IsOwned(slot.Item);
            bool selected = slot.Item == (slot.Item.IsPickaxe ? selectedPickaxe : selectedWeapon);
            slot.Icon.sprite = slot.Item.InventoryIcon;
            slot.Icon.color = owned ? Color.white : new Color(.3f, .3f, .3f, .65f);
            slot.Button.interactable = owned;
            slot.Selected.enabled = owned && selected;
            slot.State.text = !owned ? "잠김" : selected ? "선택" : "";
        }
    }
    private void RefreshRow(UpgradeRow row, WeaponData item, bool pickaxe) {
        if (row == null) return;
        int level = upgrades.GetEquipmentLevel(item);
        bool max = level == int.MaxValue;
        int cost = upgrades.GetEquipmentUpgradeCost(item);
        row.Icon.sprite = item != null ? item.InventoryIcon : null;
        row.Title.text = item != null ? item.WeaponName : pickaxe ? "곡괭이 공격력" : "무기 공격력";
        row.Level.text = max ? $"Lv.{level}  MAX" : $"Lv.{level} → <color=#FFF270>Lv.{(long)level + 1}</color>";
        int baseAttack = upgrades.GetEquipmentBaseAttack(item);
        int bonus = upgrades.GetEquipmentAttackBonus(item);
        long total = Math.Min(int.MaxValue, (long)baseAttack + bonus);
        row.Effect.text = (pickaxe ? "터렛 특화" : "몬스터·보스 특화") +
            $"\nATK : +{total} ({baseAttack} + <color=#FFF270>{bonus}</color>)";
        row.Cost.text = max ? "MAX" : $"x{cost}";
        row.Buy.interactable = item != null && upgrades.CanUpgradeEquipment(item);
        row.BuyText.text = max ? "MAX" : item == null ? "선택 필요" :
            currency.GetCurrency(CurrencyType.Dash) < cost ? "재화 부족" : "강화";
    }
}

using System;
using UnityEngine;

// Per-item progress is separate from the existing shared upgrades and save keys.
public partial class PermanentUpgradeManager
{
    [Header("Individual equipment upgrades (pink currency)")]
    [SerializeField, Min(0)] private int weaponEquipmentAttackPerLevel = 10;
    [SerializeField, Min(0)] private int pickaxeEquipmentAttackPerLevel = 10;
    public event Action<WeaponData> OnEquipmentUpgradeChanged;

    private static string EquipmentLevelKey(WeaponData item) =>
        $"EquipmentUpgrade_{(item.IsPickaxe ? "Pickaxe" : "Weapon")}_{item.WeaponId}_Level";

    public int GetEquipmentLevel(WeaponData item) => item == null ? 0 :
        Math.Max(0, PlayerPrefs.GetInt(EquipmentLevelKey(item), 0));

    public int GetEquipmentIncreasePerLevel(WeaponData item) => item == null ? 0 :
        Math.Max(0, item.IsPickaxe ? pickaxeEquipmentAttackPerLevel : weaponEquipmentAttackPerLevel);

    public int GetEquipmentAttackBonus(WeaponData item) =>
        (int)Math.Min(int.MaxValue, (long)GetEquipmentLevel(item) * GetEquipmentIncreasePerLevel(item));

    public int GetEquipmentBaseAttack(WeaponData item) {
        if(item==null)return 0;
        long value=item.IsPickaxe?(long)item.PickaxeAttack+PickaxeAttackBonus:
            (long)(playerStatus!=null && Application.isPlaying?playerStatus.BaseAttack:PlayerPrefs.GetInt("Player_Attack",10))+item.AdditionalAttack;
        return (int)Math.Max(0L,Math.Min(int.MaxValue,value));
    }

    public int GetEquipmentUpgradeCost(WeaponData item) =>
        (int)Math.Max(1L, Math.Min(int.MaxValue,
            (long)baseUpgradeCost + (long)GetEquipmentLevel(item) * costIncreasePerLevel));

    private static bool IsRegisteredEquipment(WeaponData item) {
        var weapons = WeaponManager.Instance;
        if (item == null || weapons == null || !weapons.IsOwned(item)) return false;
        if (item.IsPickaxe) {
            foreach (var candidate in weapons.AllPickaxes) if (candidate == item) return true;
        } else {
            foreach (var candidate in weapons.AllWeapons) if (candidate == item) return true;
        }
        return false;
    }

    public bool CanUpgradeEquipment(WeaponData item) => IsShopOpen &&
        IsRegisteredEquipment(item) && GetEquipmentLevel(item) < int.MaxValue &&
        CurrencyManager.Instance != null &&
        CurrencyManager.Instance.GetCurrency(CurrencyType.Dash) >= GetEquipmentUpgradeCost(item);

    public bool TryUpgradeEquipment(WeaponData item) {
        if (purchaseInProgress || lastPurchaseFrame == Time.frameCount || !CanUpgradeEquipment(item)) return false;
        purchaseInProgress = true;
        lastPurchaseFrame = Time.frameCount;
        int level = GetEquipmentLevel(item) + 1;
        try {
            // Both swords and pickaxes use the existing pink/Dash wallet.
            if (!CurrencyManager.Instance.SpendCurrency(CurrencyType.Dash, GetEquipmentUpgradeCost(item))) return false;
            PlayerPrefs.SetInt(EquipmentLevelKey(item), level);
            PlayerPrefs.Save();
        }
        finally { purchaseInProgress = false; }
        OnEquipmentUpgradeChanged?.Invoke(item);
        return true;
    }
}

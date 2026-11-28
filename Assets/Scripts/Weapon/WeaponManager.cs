using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour, IInventoryEquipmentProvider
{
    public static WeaponManager Instance {
        get;
        private set;
    }


    // =========================================================
    // Weapon Data
    // =========================================================

    [Header("Weapon Data")]

    [SerializeField]
    private WeaponData defaultWeapon;

    [SerializeField]
    private List<WeaponData> allWeapons =
        new List<WeaponData>();


    // =========================================================
    // Runtime
    // =========================================================

    [Header("Pickaxes (independent ownership)")]
    [SerializeField] private WeaponData defaultPickaxe;
    [SerializeField] private List<WeaponData> allPickaxes = new List<WeaponData>();
    [SerializeField] private PlayerGridController equipmentPlayer;
    [Header("Ordinary attack efficiency; not applied to Terrain or skills")]
    [SerializeField] private float weaponTurretMultiplier = .25f;
    [SerializeField] private float pickaxeMonsterMultiplier = .25f;
    [SerializeField] private float pickaxeBossMultiplier = .25f;
    [SerializeField] private float pickaxeTurretMultiplier = 2f;
    private WeaponData activePickaxe;
    private WeaponData lastPickaxe;
    private const string LastPickaxeKey="LastSelectedPickaxeId";
    public bool EquipLastWeapon() => EquipWeapon(CurrentWeapon!=null && IsOwned(CurrentWeapon)?CurrentWeapon:defaultWeapon);
    public bool EquipLastPickaxe() => EquipPickaxe(lastPickaxe!=null && IsOwned(lastPickaxe)?lastPickaxe:defaultPickaxe);
    public WeaponData ActiveEquipment => activePickaxe != null ? activePickaxe : CurrentWeapon;
    public bool IsPickaxeActive => activePickaxe != null;
    public event Action<WeaponData> OnActiveEquipmentChanged;
    public event Action Changed;
    private readonly List<InventoryEquipmentItem> pickaxeItems = new List<InventoryEquipmentItem>();
    public IReadOnlyList<InventoryEquipmentItem> Items {
        get {
            pickaxeItems.Clear();
            foreach(var p in allPickaxes) if(p!=null) pickaxeItems.Add(new InventoryEquipmentItem(
                p.WeaponId,p.WeaponName,p.InventoryIcon,IsOwned(p),ActiveEquipment==p,ActiveEquipment==p));
            return pickaxeItems;
        }
    }
    public bool TryEquip(string id) {
        foreach(var p in allPickaxes) if(p!=null && p.WeaponId==id)return EquipPickaxe(p);
        return false;
    }
    private bool shopEquipRequest;
    public bool EquipFromShop(WeaponData item, ShopPanelController shop) {
        if(shop==null || !shop.IsEquipmentTabOpen || GameFlowController.Instance==null ||
            GameFlowController.Instance.CurrentState!=GameState.Inventory)return false;
        if(equipmentPlayer==null)equipmentPlayer=FindObjectOfType<PlayerGridController>();
        if(equipmentPlayer!=null && equipmentPlayer.IsActionInProgress)return false;
        shopEquipRequest=true;
        try { return item!=null && (item.IsPickaxe?EquipPickaxe(item):EquipWeapon(item)); }
        finally { shopEquipRequest=false; }
    }
    private bool CanSwitchEquipment() {
        if(shopEquipRequest)return true;
        if(equipmentPlayer==null)equipmentPlayer=FindObjectOfType<PlayerGridController>();
        return equipmentPlayer!=null && !equipmentPlayer.IsActionInProgress && Time.timeScale>0f &&
            GameFlowController.Instance!=null && GameFlowController.Instance.CurrentState==GameState.Playing;
    }
    public bool EquipPickaxe(WeaponData item) {
        if(item==null || !item.IsPickaxe || !allPickaxes.Contains(item) || !IsOwned(item) || !CanSwitchEquipment())return false;
        activePickaxe=item;lastPickaxe=item;
        PlayerPrefs.SetString(LastPickaxeKey,item.WeaponId);
        SaveActiveEquipment();NotifyEquipment();return true;
    }
    private void NotifyEquipment() { OnActiveEquipmentChanged?.Invoke(ActiveEquipment);Changed?.Invoke(); }
    private void SaveActiveEquipment() {
        PlayerPrefs.SetString("ActiveEquipmentId",ActiveEquipment.WeaponId);PlayerPrefs.Save();
    }
    public int GetPickaxeBaseAttack() {
        int bonus=PermanentUpgradeManager.Instance!=null?PermanentUpgradeManager.Instance.PickaxeAttackBonus:0;
        return (int)System.Math.Min(int.MaxValue,(long)(activePickaxe!=null?activePickaxe.PickaxeAttack:0)+bonus+
            (PermanentUpgradeManager.Instance!=null?PermanentUpgradeManager.Instance.GetEquipmentAttackBonus(activePickaxe):0));
    }
    public int GetTurretAttack(PlayerStatus status) {
        return IsPickaxeActive
            ? EquipmentCombat.Attack(GetPickaxeBaseAttack(),status.RunAttackBonus,status.RunAttackPenalty,pickaxeTurretMultiplier)
            : Mathf.Max(1,Mathf.RoundToInt(status.Attack*weaponTurretMultiplier));
    }
    public int GetNormalAttack(PlayerStatus status,EnemyBase target) {
        bool boss=target is BossEnemy || target is BossExplosionEnemy;
        bool turret=!boss && target.CombatCategory==EnemyCombatCategory.Turret;
        if(turret)return GetTurretAttack(status);
        if(!IsPickaxeActive)return status.Attack;
        return EquipmentCombat.Attack(GetPickaxeBaseAttack(),status.RunAttackBonus,status.RunAttackPenalty,
            boss?pickaxeBossMultiplier:pickaxeMonsterMultiplier);
    }
    public IReadOnlyList<WeaponData> AllWeapons => allWeapons;
    public IReadOnlyList<WeaponData> AllPickaxes => allPickaxes;
    public event Action<WeaponData> OnWeaponUnlocked;

    public WeaponData CurrentWeapon {
        get;
        private set;
    }


    public event Action<WeaponData>
        OnWeaponChanged;


    // =========================================================
    // Save Keys
    // =========================================================

    private const string EquippedWeaponKey =
        "EquippedWeaponId";


    // =========================================================
    // Unity
    // =========================================================

    private void Awake() {
        Instance =
            this;


        InitializeWeapons();
        if(defaultPickaxe!=null && defaultPickaxe.IsPickaxe && allPickaxes.Contains(defaultPickaxe))SetOwned(defaultPickaxe);
        string activeId=PlayerPrefs.GetString("ActiveEquipmentId", "");
        string lastPickaxeId=PlayerPrefs.GetString(LastPickaxeKey,activeId);
        foreach(var p in allPickaxes) {
            if(p==null || !IsOwned(p))continue;
            if(p.WeaponId==activeId)activePickaxe=p;
            if(p.WeaponId==lastPickaxeId)lastPickaxe=p;
        }

    }



    [ContextMenu("DEBUG - Reset Weapon Data")]
    public void DebugResetWeaponData() {
        // =============================
        // 모든 Weapon 소유 기록 삭제
        // =============================

        if (allWeapons != null) {
            foreach (WeaponData weapon
                     in allWeapons) {
                if (weapon == null)
                    continue;


                PlayerPrefs.DeleteKey(
                    GetOwnedKey(
                        weapon));
            }
        }


        // Default가 혹시 allWeapons에 없더라도
        // 확실하게 제거
        if (defaultWeapon != null) {
            PlayerPrefs.DeleteKey(
                GetOwnedKey(
                    defaultWeapon));
        }


        // 장착 정보 제거
        PlayerPrefs.DeleteKey(
            EquippedWeaponKey);


        // =============================
        // 기본 무기는 다시 지급
        // =============================

        if (defaultWeapon != null) {
            SetOwned(
                defaultWeapon);


            CurrentWeapon =
                defaultWeapon;


            SaveEquippedWeapon();


            OnWeaponChanged?.Invoke(
                CurrentWeapon);
        }


        PlayerPrefs.Save();


        Debug.Log(
            "[Weapon] DEBUG Weapon Data Reset");
    }
    // =========================================================
    // Initialize
    // =========================================================

    private void InitializeWeapons() {
        if (defaultWeapon == null) {
            Debug.LogError(
                "[WeaponManager] Default Weapon이 없습니다.");

            return;
        }


        // 기본 무기는 항상 소유 상태
        SetOwned(
            defaultWeapon);


        // 저장된 장착 무기 불러오기
        string equippedWeaponId =
            PlayerPrefs.GetString(
                EquippedWeaponKey,
                defaultWeapon.WeaponId);


        WeaponData savedWeapon =
            FindWeaponById(
                equippedWeaponId);


        // 저장 데이터가 없거나
        // 현재 목록에서 제거된 무기라면 Default
        if (savedWeapon == null ||
            !IsOwned(savedWeapon)) {
            savedWeapon =
                defaultWeapon;
        }


        CurrentWeapon =
            savedWeapon;


        SaveEquippedWeapon();
    }


    // =========================================================
    // Ownership
    // =========================================================

    public bool IsOwned(
        WeaponData weapon) {
        if (weapon == null)
            return false;


        string key =
            GetOwnedKey(
                weapon);


        return PlayerPrefs.GetInt(
            key,
            0) == 1;
    }


    public bool UnlockWeapon(
        WeaponData weapon) {
        if (weapon == null)
            return false;


        // 이미 가지고 있음
        if (IsOwned(weapon)) {
            return false;
        }


        SetOwned(
            weapon);


        Debug.Log(
            $"[Weapon] Unlocked : {weapon.WeaponName}");


        OnWeaponUnlocked?.Invoke(weapon);
        Changed?.Invoke();
        return true;
    }

    public int GetAdditionalAttack() {
        if (CurrentWeapon == null)
            return 0;


        return (int)System.Math.Min(int.MaxValue,(long)CurrentWeapon.AdditionalAttack+
            (PermanentUpgradeManager.Instance!=null?PermanentUpgradeManager.Instance.GetEquipmentAttackBonus(CurrentWeapon):0));
    }

    private void SetOwned(
        WeaponData weapon) {
        if (weapon == null)
            return;


        string key =
            GetOwnedKey(
                weapon);


        PlayerPrefs.SetInt(
            key,
            1);


        PlayerPrefs.Save();
    }


    private string GetOwnedKey(
        WeaponData weapon) {
        return
            $"{(weapon.IsPickaxe ? "PickaxeOwned" : "WeaponOwned")}_{weapon.WeaponId}";
    }


    // =========================================================
    // Equip
    // =========================================================

    public bool EquipWeapon(
        WeaponData weapon) {
        if (weapon == null)
            return false;


        if (!IsOwned(weapon)) {
            Debug.LogWarning(
                $"[Weapon] 소유하지 않은 무기입니다 : {weapon.WeaponName}");

            return false;
        }


        // 이미 장착 중
        if(weapon.IsPickaxe || !allWeapons.Contains(weapon) || !CanSwitchEquipment())return false;
        if(CurrentWeapon==weapon && activePickaxe==null)return true;
        activePickaxe=null;


        CurrentWeapon =
            weapon;


        SaveEquippedWeapon();
        SaveActiveEquipment();
        NotifyEquipment();

        OnWeaponChanged?.Invoke(
            CurrentWeapon);


        Debug.Log(
            $"[Weapon] Equipped : {CurrentWeapon.WeaponName}");


        return true;
    }


    private void SaveEquippedWeapon() {
        if (CurrentWeapon == null)
            return;


        PlayerPrefs.SetString(
            EquippedWeaponKey,
            CurrentWeapon.WeaponId);


        PlayerPrefs.Save();
    }


    // =========================================================
    // Search
    // =========================================================

    public WeaponData FindWeaponById(
        string weaponId) {
        if (string.IsNullOrEmpty(
            weaponId)) {
            return null;
        }


        foreach (WeaponData weapon
                 in allWeapons) {
            if (weapon == null)
                continue;


            if (weapon.WeaponId ==
                weaponId) {
                return weapon;
            }
        }


        return null;
    }
}
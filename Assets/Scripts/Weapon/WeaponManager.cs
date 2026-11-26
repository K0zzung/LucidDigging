using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
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


        return true;
    }

    public int GetAdditionalAttack() {
        if (CurrentWeapon == null)
            return 0;


        return CurrentWeapon.AdditionalAttack;
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
            $"WeaponOwned_{weapon.WeaponId}";
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
        if (CurrentWeapon == weapon)
            return true;


        CurrentWeapon =
            weapon;


        SaveEquippedWeapon();


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
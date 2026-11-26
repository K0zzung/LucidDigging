using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    // =========================================================
    // Data
    // =========================================================

    [Header("Weapon")]

    [SerializeField]
    private WeaponData weapon;


    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField]
    private WeaponInventoryUI inventoryUI;

    [SerializeField]
    private Image weaponImage;

    [SerializeField]
    private Button equipButton;

    [SerializeField]
    private TMP_Text equipText;

    [SerializeField]
    private GameObject lockRoot;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake() {
        if (equipButton != null) {
            equipButton.onClick.AddListener(
                HandleEquipClicked);
        }


        ApplyWeaponData();
    }


    // =========================================================
    // Static Data
    // =========================================================

    private void ApplyWeaponData() {
        if (weapon == null)
            return;


        if (weaponImage != null) {
            weaponImage.sprite =
                weapon.InventoryIcon;
        }
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void Refresh() {
        if (weapon == null)
            return;


        if (WeaponManager.Instance == null)
            return;


        bool owned =
            WeaponManager.Instance
                .IsOwned(
                    weapon);


        bool equipped =
            WeaponManager.Instance
                .CurrentWeapon
            == weapon;


        // =============================
        // Owned
        // =============================

        if (equipButton != null) {
            equipButton.interactable =
                owned && !equipped;
        }


        // =============================
        // Lock
        // =============================

        if (lockRoot != null) {
            lockRoot.SetActive(
                !owned);
        }


        // =============================
        // Button Text
        // =============================

        if (equipText != null) {
            if (!owned) {
                equipText.text =
                    "Locked";
            }
            else if (equipped) {
                equipText.text =
                    "Equipped";
            }
            else {
                equipText.text =
                    "Equip";
            }
        }
    }


    // =========================================================
    // Equip
    // =========================================================

    private void HandleEquipClicked() {
        if (weapon == null)
            return;


        if (inventoryUI == null)
            return;


        inventoryUI.EquipWeapon(
            weapon);
    }
}
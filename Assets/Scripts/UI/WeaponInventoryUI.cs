using UnityEngine;

public class WeaponInventoryUI : MonoBehaviour
{
    // =========================================================
    // Root
    // =========================================================

    [Header("Root")]

    [SerializeField]
    private GameObject weaponContent;

    [SerializeField]
    private GameObject playButton;


    // =========================================================
    // Weapon Slots
    // =========================================================

    [Header("Weapon Slots")]

    [SerializeField]
    private WeaponSlotUI[] weaponSlots;

    [SerializeField]
    private GameObject closeButton;


    // =========================================================
    // Unity
    // =========================================================

    private void Start() {
        if (weaponContent != null) {
            weaponContent.SetActive(
                false);
        }


        if (closeButton != null) {
            closeButton.SetActive(
                false);
        }
    }


    // =========================================================
    // Open
    // =========================================================

    public void OpenWeaponContent() {
        if (weaponContent != null) {
            weaponContent.SetActive(
                true);
        }


        if (playButton != null) {
            playButton.SetActive(
                false);
        }


        if (closeButton != null) {
            closeButton.SetActive(
                true);
        }


        Refresh();
    }


    // =========================================================
    // Close
    // =========================================================

    public void CloseWeaponContent() {
        if (weaponContent != null) {
            weaponContent.SetActive(
                false);
        }


        if (playButton != null) {
            playButton.SetActive(
                true);
        }


        if (closeButton != null) {
            closeButton.SetActive(
                false);
        }
    }


    // =========================================================
    // Equip
    // =========================================================

    public void EquipWeapon(
        WeaponData weapon) {
        if (weapon == null)
            return;


        if (WeaponManager.Instance == null)
            return;


        bool equipped =
            WeaponManager.Instance
                .EquipWeapon(
                    weapon);


        if (!equipped)
            return;


        Refresh();


        Debug.Log(
            $"[Inventory] Weapon Equip : {weapon.WeaponName}");
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void Refresh() {
        if (WeaponManager.Instance == null)
            return;


        RefreshSlots();
    }


    private void RefreshSlots() {
        if (weaponSlots == null)
            return;


        foreach (WeaponSlotUI slot
                 in weaponSlots) {
            if (slot == null)
                continue;


            slot.Refresh();
        }
    }
}
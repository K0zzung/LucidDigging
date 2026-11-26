using UnityEngine;

public class InventoryUpgradePanelController : MonoBehaviour
{
    // =========================================================
    // Reference
    // =========================================================

    [Header("Reference")]

    [SerializeField]
    private WeaponInventoryUI weaponInventoryUI;


    // =========================================================
    // Panels
    // =========================================================

    [Header("Panels")]

    [SerializeField]
    private GameObject statusPanel;

    [SerializeField]
    private GameObject itemPanel;


    // =========================================================
    // Common UI
    // =========================================================

    [Header("Common UI")]

    [SerializeField]
    private GameObject playButton;

    [SerializeField]
    private GameObject closeButton;


    // =========================================================
    // Unity
    // =========================================================

    private void Start() {
        if (statusPanel != null) {
            statusPanel.SetActive(
                false);
        }


        if (itemPanel != null) {
            itemPanel.SetActive(
                false);
        }
    }


    // =========================================================
    // Status
    // =========================================================

    public void OpenStatus() {
        // 기존 Weapon 패널 정리
        if (weaponInventoryUI != null) {
            weaponInventoryUI
                .CloseWeaponContent();
        }


        if (itemPanel != null) {
            itemPanel.SetActive(
                false);
        }


        if (statusPanel != null) {
            statusPanel.SetActive(
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
    }


    // =========================================================
    // Weapon
    // =========================================================

    public void OpenWeapon() {
        if (statusPanel != null) {
            statusPanel.SetActive(
                false);
        }


        if (itemPanel != null) {
            itemPanel.SetActive(
                false);
        }


        // Weapon은 기존 시스템으로 열기
        if (weaponInventoryUI != null) {
            weaponInventoryUI
                .OpenWeaponContent();
        }
    }


    // =========================================================
    // Item
    // =========================================================

    public void OpenItem() {
        // 기존 Weapon 패널 정리
        if (weaponInventoryUI != null) {
            weaponInventoryUI
                .CloseWeaponContent();
        }


        if (statusPanel != null) {
            statusPanel.SetActive(
                false);
        }


        if (itemPanel != null) {
            itemPanel.SetActive(
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
    }


    // =========================================================
    // Close
    // =========================================================

    public void CloseAllContents() {
        if (statusPanel != null) {
            statusPanel.SetActive(
                false);
        }


        if (itemPanel != null) {
            itemPanel.SetActive(
                false);
        }


        if (weaponInventoryUI != null) {
            weaponInventoryUI
                .CloseWeaponContent();
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
}
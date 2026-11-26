using UnityEngine;

public class InventoryTabController : MonoBehaviour
{
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private GameObject weaponPanel;
    [SerializeField] private GameObject skillPanel;

    public void OpenStatus() {
        statusPanel.SetActive(true);
        weaponPanel.SetActive(false);
        skillPanel.SetActive(false);
    }

    public void OpenWeapon() {
        statusPanel.SetActive(false);
        weaponPanel.SetActive(true);
        skillPanel.SetActive(false);
    }

    public void OpenSkill() {
        statusPanel.SetActive(false);
        weaponPanel.SetActive(false);
        skillPanel.SetActive(true);
    }
}
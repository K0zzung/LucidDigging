using UnityEngine;

public class PlayerWeaponVisual : MonoBehaviour
{
    [Header("Weapon Renderer")]
    [SerializeField]
    private SpriteRenderer weaponRenderer;


    private bool isBound;


    private void OnEnable() {
        BindWeaponManager();
    }


    private void Start() {
        // Awake 순서 때문에 OnEnable에서
        // WeaponManager가 아직 없었을 경우 대비
        BindWeaponManager();

        RefreshCurrentWeapon();
    }


    private void OnDisable() {
        UnbindWeaponManager();
    }


    private void BindWeaponManager() {
        if (isBound)
            return;


        if (WeaponManager.Instance == null)
            return;


        WeaponManager.Instance.OnWeaponChanged +=
            HandleWeaponChanged;


        isBound =
            true;
    }


    private void UnbindWeaponManager() {
        if (!isBound)
            return;


        if (WeaponManager.Instance != null) {
            WeaponManager.Instance.OnWeaponChanged -=
                HandleWeaponChanged;
        }


        isBound =
            false;
    }


    private void RefreshCurrentWeapon() {
        if (WeaponManager.Instance == null)
            return;


        HandleWeaponChanged(
            WeaponManager.Instance.CurrentWeapon);
    }


    private void HandleWeaponChanged(
        WeaponData weapon) {
        if (weaponRenderer == null)
            return;


        if (weapon == null) {
            weaponRenderer.sprite =
                null;

            return;
        }


        weaponRenderer.sprite =
            weapon.WeaponSprite;
    }
}
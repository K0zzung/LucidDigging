using UnityEngine;

public class PlayerWeaponVisual : MonoBehaviour
{
    [Header("Weapon Renderer")]
    [SerializeField]
    private SpriteRenderer weaponRenderer;


    private bool isBound;
    private Vector3 defaultPosition,defaultScale;
    private Quaternion defaultRotation;
    private void Awake() {
        if(weaponRenderer==null)return;
        defaultPosition=weaponRenderer.transform.localPosition;defaultScale=weaponRenderer.transform.localScale;
        defaultRotation=weaponRenderer.transform.localRotation;
    }


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


        WeaponManager.Instance.OnActiveEquipmentChanged +=
            HandleWeaponChanged;


        isBound =
            true;
    }


    private void UnbindWeaponManager() {
        if (!isBound)
            return;


        if (WeaponManager.Instance != null) {
            WeaponManager.Instance.OnActiveEquipmentChanged -=
                HandleWeaponChanged;
        }


        isBound =
            false;
    }


    private void RefreshCurrentWeapon() {
        if (WeaponManager.Instance == null)
            return;


        HandleWeaponChanged(
            WeaponManager.Instance.ActiveEquipment);
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


        weaponRenderer.sprite = weapon.WeaponSprite;
        weaponRenderer.transform.localScale=defaultScale*(weapon.IsPickaxe?weapon.DisplayScale:1f);
        weaponRenderer.transform.localPosition=defaultPosition+(weapon.IsPickaxe?(Vector3)weapon.GripOffset:Vector3.zero);
        weaponRenderer.transform.localRotation=defaultRotation*Quaternion.Euler(0,0,weapon.IsPickaxe?weapon.GripAngle:0);
    }
}
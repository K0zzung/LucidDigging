using UnityEngine;

public class PlayerWeaponAttackVFX : MonoBehaviour
{
    // =========================================================
    // Reference
    // =========================================================

    [Header("Reference")]

    [SerializeField]
    private Transform effectRoot;


    // =========================================================
    // Position
    // =========================================================

    [Header("Position")]

    [SerializeField]
    private float effectOffset = 0.45f;


    // =========================================================
    // Play
    // =========================================================

    public void Play(
        Vector2Int direction) {
        if (WeaponManager.Instance == null)
            return;


        WeaponData weapon =
            WeaponManager.Instance
                .CurrentWeapon;


        if (weapon == null)
            return;


        GameObject prefab =
            weapon.AttackEffectPrefab;


        if (prefab == null)
            return;


        Vector3 centerPosition =
            effectRoot != null
            ? effectRoot.position
            : transform.position;


        Vector2 normalizedDirection =
            ((Vector2)direction)
            .normalized;


        Vector3 spawnPosition =
            centerPosition
            + new Vector3(
                normalizedDirection.x,
                normalizedDirection.y,
                0f)
            * effectOffset;


        GameObject effectObject =
            Instantiate(
                prefab,
                spawnPosition,
                Quaternion.identity);


        WeaponAttackEffect effect =
            effectObject.GetComponent<
                WeaponAttackEffect>();


        if (effect != null) {
            effect.Play(
                direction);
        }
    }
}
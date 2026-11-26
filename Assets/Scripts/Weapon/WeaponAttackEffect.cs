using UnityEngine;

public class WeaponAttackEffect : MonoBehaviour
{
    [Header("Lifetime")]

    [SerializeField]
    private float lifetime = 0.5f;


    // =========================================================
    // Play
    // =========================================================

    public void Play(
        Vector2Int direction) {
        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                GetDirectionAngle(
                    direction));


        Destroy(
            gameObject,
            lifetime);
    }


    // =========================================================
    // Direction
    // =========================================================

    private float GetDirectionAngle(
        Vector2Int direction) {
        if (direction == Vector2Int.right) {
            return 0f;
        }


        if (direction == Vector2Int.left) {
            return 180f;
        }


        if (direction == Vector2Int.down) {
            return -90f;
        }


        if (direction ==
            new Vector2Int(
                1,
                -1)) {
            return -45f;
        }


        if (direction ==
            new Vector2Int(
                -1,
                -1)) {
            return -135f;
        }


        return 0f;
    }
}
using UnityEngine;

public class FloatingDamageManager : MonoBehaviour
{
    public static FloatingDamageManager Instance {
        get;
        private set;
    }


    // =========================================================
    // Reference
    // =========================================================

    [Header("Reference")]

    [SerializeField]
    private FloatingDamageText damagePrefab;

    [SerializeField]
    private Transform floatingRoot;


    // =========================================================
    // Color
    // =========================================================

    [Header("Color")]

    [SerializeField]
    private Color dealtDamageColor =
        Color.white;

    [SerializeField]
    private Color receivedDamageColor =
        Color.red;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake() {
        Instance =
            this;
    }


    // =========================================================
    // Player ¡æ Enemy / Terrain
    // =========================================================

    public void ShowDealtDamage(
        Vector3 worldPosition,
        int damage) {
        Spawn(
            worldPosition,
            damage,
            false,
            dealtDamageColor);
    }


    // =========================================================
    // Enemy / Terrain ¡æ Player
    // =========================================================

    public void ShowReceivedDamage(
        Vector3 worldPosition,
        int damage) {
        Spawn(
            worldPosition,
            damage,
            true,
            receivedDamageColor);
    }


    // =========================================================
    // Spawn
    // =========================================================

    private void Spawn(
        Vector3 worldPosition,
        int damage,
        bool receivedDamage,
        Color color) {
        if (damagePrefab == null)
            return;


        if (damage <= 0)
            return;


        FloatingDamageText instance;


        if (floatingRoot != null) {
            instance =
                Instantiate(
                    damagePrefab,
                    worldPosition,
                    Quaternion.identity,
                    floatingRoot);
        }
        else {
            instance =
                Instantiate(
                    damagePrefab,
                    worldPosition,
                    Quaternion.identity);
        }


        instance.Play(
            damage,
            receivedDamage,
            color);
    }
}
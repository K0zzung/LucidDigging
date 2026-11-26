using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance {
        get;
        private set;
    }


    [Header("Difficulty")]

    [SerializeField]
    private int depthPerTier = 100;

    [SerializeField]
    private float hpIncreasePerTier = 0.25f;

    [SerializeField]
    private float attackIncreasePerTier = 0.05f;


    private void Awake() {
        if (Instance != null &&
            Instance != this) {
            Destroy(gameObject);
            return;
        }


        Instance =
            this;
    }


    public int GetTier(
        int depth) {
        if (depthPerTier <= 0)
            return 0;


        return
            depth / depthPerTier;
    }


    public float GetEnemyHpMultiplier(
        int depth) {
        int tier =
            GetTier(
                depth);


        return
            1f
            + tier
            * hpIncreasePerTier;
    }


    public float GetEnemyAttackMultiplier(
        int depth) {
        int tier =
            GetTier(
                depth);


        return
            1f
            + tier
            * attackIncreasePerTier;
    }
}
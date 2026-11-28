using DG.Tweening;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Data")]
    [SerializeField]
    protected EnemyData data;


    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string hitTrigger = "Hit";

    [SerializeField]
    private string dieTrigger = "Die";


    [Header("Death")]
    [SerializeField]
    private float fadeDuration = 0.3f;


    // =========================================================
    // Health Bar
    // =========================================================

    [Header("Health Bar")]

    [SerializeField]
    private EnemyHealthBar healthBarPrefab;

    [SerializeField]
    private Transform healthBarAnchor;


    private EnemyHealthBar healthBarInstance;

    protected virtual bool UseNormalKillSound => true;
    protected virtual bool UsesDepthHpScaling => true;
    protected virtual bool UseWorldHealthBar => true;
    protected virtual bool TryPreventDeath() => false;
    // =========================================================
    // Runtime
    // =========================================================

    protected EnemyManager enemyManager;
    private bool experienceGranted;


    public Vector3Int Cell {
        get;
        private set;
    }


    private Vector2Int placementFootprint;
    public Vector2Int FootprintSize => placementFootprint.x>0 && placementFootprint.y>0
        ? placementFootprint : data.footprintSize;
    // Per-instance placement only; never mutate the shared EnemyData asset.
    public bool TrySetPlacementFootprint(Vector2Int size) {
        if(enemyManager!=null || size.x<1 || size.y<1)return false;
        placementFootprint=size;
        return true;
    }


    public int MaxHP {
        get;
        protected set;
    }


    public int CurrentHP {
        get;
        protected set;
    }


    public bool IsDead {
        get;
        protected set;
    }


    // =========================================================
    // Initialize
    // =========================================================

    public virtual void Initialize(
        EnemyManager manager,
        Vector3Int anchorCell,
        int depth) {
        enemyManager =
            manager;

        Cell =
            anchorCell;


        float multiplier =
            UsesDepthHpScaling
                ? DifficultyManager.Instance.GetEnemyHpMultiplier(depth)
                : 1f;


        // 이번 Enemy의 실제 최대 HP
        MaxHP =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    data.maxHP
                    * multiplier));


        CurrentHP =
            MaxHP;


        IsDead =
            false;
        experienceGranted = false;


        Debug.Log(
            $"{data.enemyName} Spawn / " +
            $"Depth {depth} / " +
            $"HP {CurrentHP}");
    }


    // =========================================================
    // Damage
    // =========================================================
    private int enemyDamageDepth;
    // Preserve virtual damage/death behavior while withholding player kill XP for this hit.
    public bool TakeDamageFromEnemy(int attack) {
        enemyDamageDepth++;
        try {return TakeDamage(attack);}
        finally {enemyDamageDepth--;}
    }

    public virtual bool TakeDamage(
    int attack) {
        return TakeDamage(
            attack,
            out _);
    }


    public virtual bool TakeDamage(
        int attack,
        out int damage) {
        damage =
            0;


        if (IsDead || experienceGranted)
            return false;


        damage =
            DamageCalculator.CalculateDamage(
                attack,
                data.defense);


        CurrentHP =
            Mathf.Max(
                0,
                CurrentHP - damage);


        Debug.Log(
            $"{data.enemyName} " +
            $"Damage {damage} / " +
            $"HP {CurrentHP}");


        // =============================
        // Death
        // =============================

        if (CurrentHP <= 0 && TryPreventDeath()) return false;
        if (CurrentHP <= 0) {
            HideHealthBar();


            // Claim before virtual death handling to guard repeated/reentrant damage.
            experienceGranted = true;
            Die();

            if (enemyDamageDepth == 0 && RunProgression.Instance != null) {
                RunProgression.Instance.AddExperience(data.experienceReward);
            }


            return true;
        }


        // =============================
        // Health Bar
        // =============================

        if (healthBarInstance != null) {
            RefreshHealthBar();
        }


        PlayHitAnimation();


        return false;
    }


    // =========================================================
    // Stats
    // =========================================================

    public int GetAttack() {
        return data.attack;
    }


    // =========================================================
    // Hit
    // =========================================================

    protected virtual void PlayHitAnimation() {
        if (animator == null)
            return;


        animator.SetTrigger(
            hitTrigger);
    }


    // =========================================================
    // Death
    // =========================================================

    protected virtual void Die() {
        if (IsDead)
            return;


        IsDead =
            true;


        HideHealthBar();


        // Grid 판정에서는 즉시 제거
        if (enemyManager != null) {
            enemyManager.RemoveEnemy(
                this);
        }


        if (animator != null) {
            animator.ResetTrigger(
                hitTrigger);

            animator.SetTrigger(
                dieTrigger);
        }
        else {
            // Animator가 없으면 바로 종료 처리
            OnDeathAnimationFinished();
        }
    }


    /// <summary>
    /// Die Animation 마지막 프레임의
    /// Animation Event에서 호출
    /// </summary>
    public virtual void OnDeathAnimationFinished() {
        FadeOutAndDestroy();
    }


    protected virtual void FadeOutAndDestroy() {
        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>();


        if (renderers.Length == 0) {
            Destroy(
                gameObject);

            return;
        }


        Sequence sequence =
            DOTween.Sequence();


        sequence.Join(
            transform.DOMoveY(
                    transform.position.y - 0.25f,
                    fadeDuration)
                .SetEase(
                    Ease.OutQuad));


        foreach (SpriteRenderer renderer
                 in renderers) {
            sequence.Join(
                renderer.DOFade(
                    0f,
                    fadeDuration));
        }


        sequence.OnComplete(() => {
            Destroy(
                gameObject);
        });
    }


    // =========================================================
    // Health Bar
    // =========================================================

    public void ShowHealthBar() {
        if (!UseWorldHealthBar) return;
        if (IsDead)
            return;


        if (healthBarPrefab == null)
            return;


        if (healthBarAnchor == null)
            return;


        // 처음 맞아서 살아남았을 때만 생성
        if (healthBarInstance == null) {
            healthBarInstance =
                Instantiate(
                    healthBarPrefab,
                    healthBarAnchor);


            healthBarInstance
                .transform
                .localPosition =
                Vector3.zero;


            healthBarInstance
                .transform
                .localRotation =
                Quaternion.identity;
        }


        RefreshHealthBar();
    }


    private void RefreshHealthBar() {
        if (healthBarInstance == null)
            return;


        float ratio =
            MaxHP > 0
            ? CurrentHP / (float)MaxHP
            : 0f;


        healthBarInstance.SetRatio(
            ratio);
    }


    private void HideHealthBar() {
        if (healthBarInstance == null)
            return;


        healthBarInstance.gameObject.SetActive(
            false);
    }


    // =========================================================
    // Cell
    // =========================================================

    public void SetCell(
        Vector3Int cell) {
        Cell =
            cell;
    }
}
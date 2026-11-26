using System.Collections;
using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerGridController gridController;

    [SerializeField]
    private PlayerStatus playerStatus;


    // =============================
    // Dash
    // =============================

    [Header("Dash")]
    [SerializeField]
    private float dashDuration = 2f;

    [SerializeField]
    private float dashAttackPercent = 200f;

    [Tooltip("영구 강화 Lv.1당 증가하는 Dash 공격력 %")]
    [SerializeField]
    private float dashAttackPercentPerLevel = 10f;

    [SerializeField]
    private float dashStepInterval = 0.08f;

    [SerializeField]
    private float dashMoveDuration = 0.045f;


    // =============================
    // Shockwave
    // =============================

    [Header("Shockwave")]
    [Tooltip("3 = 3x3, 5 = 5x5")]
    [SerializeField]
    private int shockwaveSize = 3;

    [SerializeField]
    private float shockwaveAttackPercent = 150f;

    [SerializeField]
    private float shockwaveSkillLockTime = 0.3f;

    [Tooltip("영구 강화 Lv.1당 증가하는 Shockwave 공격력 %")]
    [SerializeField]
    private float shockwaveAttackPercentPerLevel = 10f;

    [SerializeField]
    private ShockwaveEffect shockwaveEffectPrefab;


    // =============================
    // Shield
    // =============================

    [Header("Shield")]
    [SerializeField]
    private int shieldDefenseBonus = 50;

    [SerializeField]
    private float shieldDuration = 3f;

    [Tooltip("쉴드 발동 순간 다른 스킬 입력을 막는 짧은 시간")]
    [SerializeField]
    private float shieldCastLockTime = 0.15f;

    [Tooltip("영구 강화 Lv.1당 증가하는 Shield 방어력")]
    [SerializeField]
    private int shieldDefenseBonusPerLevel = 10;

    [SerializeField]
    private ShieldEffect shieldEffectPrefab;


    // =============================
    // State
    // =============================

    public bool IsSkillLocked {
        get;
        private set;
    }
    public bool IsShieldActive {
        get;
        private set;
    }

    // Gameplay and presentation share this scaled-time deadline.
    private float shieldEndsAt;
    public float ShieldDuration => shieldDuration;
    public float ShieldRemainingTime => IsShieldActive ? Mathf.Max(0f, shieldEndsAt - Time.time) : 0f;
    public ShieldEffect ShieldVisualSource => shieldEffectPrefab;

    private void OnEnable() {
        if (playerStatus != null) playerStatus.OnPlayerDied += ClearShield;
    }

    private int currentShieldBonus;

    private ShockwaveEffect shockwaveVisual;
    private Coroutine shieldCoroutine;
    private Coroutine skillLockCoroutine;

    private void Awake() {
        if (gridController == null) {
            gridController =
                GetComponent<PlayerGridController>();
        }

        if (playerStatus == null) {
            playerStatus =
                GetComponent<PlayerStatus>();
        }
    }


    // =============================
    // 공통 진입점
    // =============================

    public bool TryUseSkill(
        SkillType type,
        Vector2Int direction) {
        if (IsSkillLocked)
            return false;


        if (RunSkillManager.Instance == null) {
            Debug.LogError(
                "[Skill] RunSkillManager 없음");

            return false;
        }


        if (!RunSkillManager.Instance.HasSkill(type)) {
            Debug.Log(
                $"[Skill] {type} Item 없음");

            return false;
        }


        switch (type) {
            case SkillType.Dash:

                return TryDash(
                    direction);


            case SkillType.Shockwave:

                return TryShockwave();


            case SkillType.Shield:

                return TryShield();
        }


        return false;
    }


    // =============================
    // DASH
    // =============================

    private bool TryDash(
    Vector2Int direction) {
        if (direction == Vector2Int.zero)
            return false;


        float finalAttackPercent =
            GetCurrentDashAttackPercent();


        IsSkillLocked =
            true;


        bool started =
            gridController.StartDash(
                direction,
                dashDuration,
                finalAttackPercent,
                dashStepInterval,
                dashMoveDuration,
                OnDashFinished);


        if (!started) {
            IsSkillLocked =
                false;

            return false;
        }


        RunSkillManager.Instance
            .TryConsumeSkill(
                SkillType.Dash);


        return true;
    }


    private void OnDashFinished() {
        IsSkillLocked = false;
    }


    // =============================
    // SHOCKWAVE
    // =============================

    private bool TryShockwave() {
        float finalAttackPercent =
            GetCurrentShockwaveAttackPercent();


        IsSkillLocked =
            true;


        if (shockwaveEffectPrefab != null && shockwaveVisual == null)
            shockwaveVisual = Instantiate(shockwaveEffectPrefab, transform.position, Quaternion.identity);
        if (shockwaveVisual != null) shockwaveVisual.Prepare(playerStatus);

        gridController.PerformShockwave(shockwaveSize, finalAttackPercent, shockwaveVisual);
        if (shockwaveVisual != null) shockwaveVisual.Play(transform.position, shockwaveSize);


        RunSkillManager.Instance
            .TryConsumeSkill(
                SkillType.Shockwave);


        StartSkillLock(
            shockwaveSkillLockTime);


        return true;
    }


    // =============================
    // SHIELD
    // =============================

    private bool TryShield() {
        int finalDefenseBonus =
            GetCurrentShieldDefenseBonus();


        IsSkillLocked =
            true;


        if (!IsShieldActive) {
            IsShieldActive =
                true;


            currentShieldBonus =
                finalDefenseBonus;


            playerStatus.AddTemporaryDefense(
                currentShieldBonus);


            Debug.Log(
                $"[Shield] Start / " +
                $"Defense +{currentShieldBonus} / " +
                $"Current : {playerStatus.Defense}");
        }
        else {
            Debug.Log(
                "[Shield] Refresh Duration");
        }


        if (shieldCoroutine != null) {
            StopCoroutine(
                shieldCoroutine);
        }


        shieldEndsAt = Time.time + shieldDuration;
        shieldCoroutine = StartCoroutine(ShieldRoutine());


        RunSkillManager.Instance
            .TryConsumeSkill(
                SkillType.Shield);


        StartSkillLock(
            shieldCastLockTime);


        return true;
    }


    private IEnumerator ShieldRoutine() {
        while (Time.time < shieldEndsAt)
            yield return null;

        shieldCoroutine = null;
        ClearShield();
    }

    private void ClearShield() {
        if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
        shieldCoroutine = null;
        shieldEndsAt = 0f;
        IsShieldActive = false;
        if (currentShieldBonus != 0 && playerStatus != null)
            playerStatus.RemoveTemporaryDefense(currentShieldBonus);
        currentShieldBonus = 0;
    }

    private void StartSkillLock(
    float duration) {
        if (skillLockCoroutine != null) {
            StopCoroutine(
                skillLockCoroutine);
        }

        skillLockCoroutine =
            StartCoroutine(
                SkillLockRoutine(
                    duration));
    }


    private IEnumerator SkillLockRoutine(
        float duration) {
        IsSkillLocked = true;

        yield return new WaitForSeconds(
            duration);

        IsSkillLocked = false;

        skillLockCoroutine = null;
    }

    private int GetPermanentSkillLevel(
    PermanentUpgradeType upgradeType) {
        if (PermanentUpgradeManager.Instance == null)
            return 0;


        return PermanentUpgradeManager.Instance.GetLevel(upgradeType);
    }



    private void OnDisable() {
        if (shockwaveVisual != null) shockwaveVisual.gameObject.SetActive(false);
        if (playerStatus != null) playerStatus.OnPlayerDied -= ClearShield;
        ClearShield();
        if (skillLockCoroutine != null) StopCoroutine(skillLockCoroutine);
        skillLockCoroutine = null;
        IsSkillLocked = false;
    }


    private void OnDestroy() {
        if (shockwaveVisual != null) Destroy(shockwaveVisual.gameObject);
    }

    public float GetCurrentDashAttackPercent() {
        int level =
            GetPermanentSkillLevel(
                PermanentUpgradeType.Dash);


        return
            dashAttackPercent
            + level
            * dashAttackPercentPerLevel;
    }


    public float GetCurrentShockwaveAttackPercent() {
        int level =
            GetPermanentSkillLevel(
                PermanentUpgradeType.Shockwave);


        return
            shockwaveAttackPercent
            + level
            * shockwaveAttackPercentPerLevel;
    }


    public int GetCurrentShieldDefenseBonus() {
        int level =
            GetPermanentSkillLevel(
                PermanentUpgradeType.Shield);


        return
            shieldDefenseBonus
            + level
            * shieldDefenseBonusPerLevel;
    }
}
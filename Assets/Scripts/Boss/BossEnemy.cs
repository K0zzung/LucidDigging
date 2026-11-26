using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BossEnemy : EnemyBase
{
    protected override bool UsesDepthHpScaling => false;

    // =========================================================
    // Visual
    // =========================================================

    [Header("Visual")]

    [Tooltip("실제 보스 Sprite가 들어있는 자식 Transform")]
    [SerializeField]
    private Transform visualRoot;

    [SerializeField] private SpriteRenderer[] bodyRenderers;


    // =========================================================
    // Idle
    // =========================================================

    [Header("Idle")]

    [SerializeField]
    private float idleMoveX = 0.06f;

    [SerializeField]
    private float idleMoveY = 0.05f;

    [SerializeField]
    private float idleRotateZ = 1.5f;

    [SerializeField]
    private float idleDuration = 0.7f;


    // =========================================================
    // Hit
    // =========================================================

    [Header("Hit")]

    [Range(0f, 1f)]
    [SerializeField]
    private float hitAlpha = 0.35f;

    [SerializeField]
    private float hitFadeTime = 0.07f;


    // =========================================================
    // Spike Pattern
    // =========================================================

    [Header("Spike Pattern")]

    [Tooltip("공격 사이 최소 대기시간")]
    [SerializeField]
    private float attackIntervalMin = 2.5f;

    [Tooltip("공격 사이 최대 대기시간")]
    [SerializeField]
    private float attackIntervalMax = 4f;

    [Tooltip("경고가 표시되는 시간")]
    [SerializeField]
    private float warningDuration = 0.8f;

    [Tooltip("보스 위쪽 몇 Cell까지 공격할지")]
    [SerializeField]
    private int spikeRangeCells = 4;


    [Tooltip("가시 공격력")]
    [SerializeField]
    private int spikeAttack = 25;
    [Header("Spike VFX")]
    [SerializeField]
    private float spikeVfxYOffset = -0.7f;

    [SerializeField]
    private BossSpikeWarning spikeWarningPrefab;

    [SerializeField]
    private BossSpikeStrike spikeStrikePrefab;


    // =========================================================
    // Death
    // =========================================================

    [Header("Death")]

    [SerializeField]
    private float deathShakeDuration = 1f;

    [SerializeField]
    private float deathShakeStrength = 0.18f;

    [SerializeField]
    private float deathFadeDuration = 0.35f;

    // =========================================================
    // Weapon Drop
    // =========================================================

    [Header("Weapon Drop")]

    [SerializeField]
    private WeaponData weaponDrop;

    [SerializeField]
    private WeaponPickup weaponPickupPrefab;


    // =========================================================
    // Audio
    // =========================================================

    [Header("Audio")]

    [SerializeField]
    private AudioClip spikeAttackSFX;


    // =========================================================
    // Runtime References
    // =========================================================

    private PlayerGridController player;
    private PlayerStatus playerStatus;

    private TerrainManager terrainManager;


    // Arena
    private int arenaLeftX;
    private int arenaWidth;


    // =========================================================
    // Runtime State
    // =========================================================

    private bool bossActivated;

    private Coroutine attackRoutine;

    private Tween idleMoveTween;
    private Tween idleRotateTween;

    private Sequence hitSequence;
    private Sequence deathSequence;


    private Vector3 visualOriginalLocalPosition;
    private Quaternion visualOriginalLocalRotation;


    // =========================================================
    // Initialize
    // =========================================================

    public override void Initialize(
        EnemyManager manager,
        Vector3Int anchorCell,
        int spawnDepth) {
        base.Initialize(
            manager,
            anchorCell,
            spawnDepth);


        // =============================
        // Body SpriteRenderer 자동 수집
        // =============================

        if (bodyRenderers == null ||
            bodyRenderers.Length == 0) {
            if (visualRoot != null) {
                bodyRenderers =
                    visualRoot.GetComponentsInChildren<SpriteRenderer>(
                        true);
            }
            else {
                bodyRenderers =
                    GetComponentsInChildren<SpriteRenderer>(
                        true);
            }
        }


        // 모든 파츠 Alpha 정상화
        SetAllBodyAlpha(1f);


        if (visualRoot != null) {
            visualOriginalLocalPosition =
                visualRoot.localPosition;

            visualOriginalLocalRotation =
                visualRoot.localRotation;

            PlayIdleMotion();
        }
    }

    private void SetAllBodyAlpha(float alpha) {
        if (bodyRenderers == null)
            return;


        foreach (SpriteRenderer renderer
                 in bodyRenderers) {
            if (renderer == null)
                continue;


            Color color =
                renderer.color;

            color.a =
                alpha;

            renderer.color =
                color;
        }
    }

    // =========================================================
    // Boss 활성화
    //
    // DungeonGenerator가 실제 Depth에 도달했을 때 호출
    // =========================================================

    public void ActivateBoss(
        PlayerGridController player,
        TerrainManager terrainManager,
        int arenaLeftX,
        int arenaWidth) {
        if (bossActivated)
            return;


        this.player =
            player;

        this.terrainManager =
            terrainManager;

        this.arenaLeftX =
            arenaLeftX;

        this.arenaWidth =
            arenaWidth;


        if (player != null) {
            playerStatus =
                player.GetComponent<PlayerStatus>();
        }


        bossActivated =
            true;


        attackRoutine =
            StartCoroutine(
                AttackLoop());


        Debug.Log(
            $"[Boss] Activated : {name}");
    }

    

    // =========================================================
    // Idle
    // =========================================================

    private void PlayIdleMotion() {
        if (visualRoot == null)
            return;


        idleMoveTween?.Kill();
        idleRotateTween?.Kill();


        visualRoot.localPosition =
            visualOriginalLocalPosition;

        visualRoot.localRotation =
            visualOriginalLocalRotation;


        // 위/아래 + 살짝 옆으로
        idleMoveTween =
            visualRoot
                .DOLocalMove(
                    visualOriginalLocalPosition
                    + new Vector3(
                        idleMoveX,
                        idleMoveY,
                        0f),
                    idleDuration)
                .SetEase(
                    Ease.InOutSine)
                .SetLoops(
                    -1,
                    LoopType.Yoyo);


        // 아주 약한 좌우 고개 흔들림
        idleRotateTween =
            visualRoot
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        0f,
                        idleRotateZ),
                    idleDuration)
                .SetEase(
                    Ease.InOutSine)
                .SetLoops(
                    -1,
                    LoopType.Yoyo);
    }


    // =========================================================
    // Hit
    // =========================================================

    protected override void PlayHitAnimation() {
        if (bodyRenderers == null ||
            bodyRenderers.Length == 0) {
            return;
        }


        // 이전 피격 연출 중이라면 종료
        hitSequence?.Kill();

        KillAllBodyTweens();


        // 이전 Hit 도중 다시 맞아도
        // 항상 정상 Alpha에서 시작
        SetAllBodyAlpha(1f);


        hitSequence =
            DOTween.Sequence();


        // =============================
        // 전체 몸 투명하게
        // =============================

        AppendFadeAll(
            hitSequence,
            hitAlpha,
            hitFadeTime);


        // =============================
        // 전체 몸 원래대로
        // =============================

        AppendFadeAll(
            hitSequence,
            1f,
            hitFadeTime);
    }


    // =========================================================
    // Attack Loop
    // =========================================================

    private IEnumerator AttackLoop() {
        while (!IsDead &&
               bossActivated) {
            float waitTime =
                Random.Range(
                    attackIntervalMin,
                    attackIntervalMax);


            yield return new WaitForSeconds(
                waitTime);


            if (IsDead)
                yield break;


            yield return SpikeAttackRoutine();
        }
    }


    // =========================================================
    // Spike Attack
    // =========================================================

    private IEnumerator SpikeAttackRoutine() {
        if (player == null)
            yield break;


        int arenaRightX =
            arenaLeftX
            + arenaWidth
            - 1;


        int targetX =
            Mathf.Clamp(
                player.CurrentCell.x,
                arenaLeftX,
                arenaRightX);


        int startY =
            Cell.y
            + FootprintSize.y;


        // =====================================================
        // 1. Warning
        // 기존 코드 그대로
        // =====================================================

        for (int i = 0;
             i < spikeRangeCells;
             i++) {
            Vector3Int cell =
                new Vector3Int(
                    targetX,
                    startY + i,
                    0);


            if (spikeWarningPrefab == null)
                continue;


            Vector3 position =
                enemyManager.GetCellCenterWorld(
                    cell);


            BossSpikeWarning warning =
                Instantiate(
                    spikeWarningPrefab,
                    position,
                    Quaternion.identity);


            warning.Play(
                warningDuration);
        }


        Debug.Log(
            $"[Boss] Spike Warning / X : {targetX}");


        yield return new WaitForSeconds(
            warningDuration);


        if (IsDead)
            yield break;


        // 큰 Enemy 중복 타격 방지
        HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();

        // =====================================================
        // 실제 공격 Sound
        // =====================================================

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX(spikeAttackSFX);
        }
        // =====================================================
        // 2. Strike
        // VFX는 딱 한 번
        // =====================================================

        Vector3Int firstCell = new Vector3Int(targetX, startY,0);


        Vector3Int lastCell =
            new Vector3Int(
                targetX,
                startY + spikeRangeCells - 1,
                0);


        Vector3 firstPosition =
            enemyManager.GetCellCenterWorld(
                firstCell);


        Vector3 lastPosition =
            enemyManager.GetCellCenterWorld(
                lastCell);


        Vector3 spikePosition = firstPosition;
        spikePosition.y += spikeVfxYOffset;

        if (spikeStrikePrefab != null) {
            BossSpikeStrike spike =
                Instantiate(
                    spikeStrikePrefab,
                    spikePosition,
                    Quaternion.identity);


            spike.Play(() =>
            {
                if (IsDead)
                    return;


                for (int i = 0;
                     i < spikeRangeCells;
                     i++) {
                    Vector3Int attackCell =
                        new Vector3Int(
                            targetX,
                            startY + i,
                            0);


                    ApplySpikeDamage(
                        attackCell,
                        hitEnemies);
                }
            });
        }
    }



    // =========================================================
    // Spike Damage
    // =========================================================

    private void ApplySpikeDamage(
        Vector3Int cell,
        HashSet<EnemyBase> hitEnemies) {
        // =====================================================
        // Player
        // =====================================================

        if (player != null &&
            playerStatus != null &&
            player.CurrentCell == cell) {
            int damage =
                DamageCalculator.CalculateDamage(
                    spikeAttack,
                    playerStatus.Defense);


            playerStatus.TakeDamage(
                damage);
        }


        // =====================================================
        // Other Enemy
        // =====================================================

        if (enemyManager != null &&
            enemyManager.TryGetEnemy(
                cell,
                out EnemyBase enemy)) {
            if (enemy != null &&
                enemy != this &&
                !enemy.IsDead &&
                hitEnemies.Add(enemy)) {
                enemy.TakeDamage(
                    spikeAttack);
            }
        }


        // =====================================================
        // Terrain
        // =====================================================

        if (terrainManager != null &&terrainManager.HasTerrain(cell)) {
            terrainManager.DamageTerrain(cell, spikeAttack);
        }
    }


    // =========================================================
    // Death
    // =========================================================

    protected override void Die() {
        if (IsDead)
            return;


        IsDead = true;
        if (AudioManager.Instance != null) {
            AudioManager.Instance
                .PlayBossKill();
        }

        bossActivated = false;


        // =============================
        // 공격 중지
        // =============================

        if (attackRoutine != null) {
            StopCoroutine(
                attackRoutine);


            attackRoutine =
                null;
        }


        // =============================
        // Grid에서는 즉시 제거
        // =============================

        if (enemyManager != null) {
            enemyManager.RemoveEnemy(
                this);
        }


        // =============================
        // 전용 Weapon Drop
        //
        // Boss가 Grid에서 제거된 뒤 계산해야
        // Player의 착지 위치를 정상 탐색 가능
        // =============================

        TrySpawnWeaponDrop();


        // =============================
        // 기존 Tween 종료
        // =============================

        idleMoveTween?.Kill();

        idleRotateTween?.Kill();


        hitSequence?.Kill();

        deathSequence?.Kill();


        KillAllBodyTweens();


        // 피격 Fade 도중 죽어도
        // 정상 Alpha에서 Death 시작
        SetAllBodyAlpha(
            1f);


        if (visualRoot == null) {
            Destroy(
                gameObject);

            return;
        }


        visualRoot.DOKill();


        visualRoot.localPosition =
            visualOriginalLocalPosition;


        visualRoot.localRotation =
            visualOriginalLocalRotation;


        // =============================
        // 죽음 Sequence
        // =============================

        deathSequence =
            DOTween.Sequence();


        // =============================
        // 1. 좌우 흔들림
        // =============================

        deathSequence.Append(
            visualRoot.DOShakePosition(
                deathShakeDuration,
                new Vector3(
                    deathShakeStrength,
                    0f,
                    0f),
                25,
                90f,
                false,
                true));


        // =============================
        // 2. Fade
        // =============================

        AppendFadeAll(
            deathSequence,
            0f,
            deathFadeDuration);


        // =============================
        // 3. 제거
        // =============================

        deathSequence.OnComplete(
            () => {
                Destroy(
                    gameObject);
            });


        Debug.Log(
            $"[Boss] Die : {name}");
    }

    private void TrySpawnWeaponDrop() {
        if (weaponDrop == null)
            return;


        if (weaponPickupPrefab == null)
            return;


        if (WeaponManager.Instance == null)
            return;


        // =============================
        // 이미 보유 중이면 드랍 X
        // =============================

        if (WeaponManager.Instance
            .IsOwned(
                weaponDrop)) {
            Debug.Log(
                $"[Weapon Drop] Already Owned : {weaponDrop.WeaponName}");

            return;
        }


        if (player == null)
            return;


        if (enemyManager == null)
            return;


        // =============================
        // Player의 보스전 종료 후
        // 실제 착지 위치
        // =============================

        if (!player.TryGetBossLandingCell(
            out Vector3Int dropCell)) {
            Debug.LogWarning(
                "[Weapon Drop] 착지 Cell을 찾지 못했습니다.");

            return;
        }


        // =============================
        // 보스 중앙에서 무기가 튀어나옴
        // =============================

        Vector3Int bossCenterCell =
            Cell
            + new Vector3Int(
                FootprintSize.x / 2,
                FootprintSize.y / 2,
                0);


        Vector3 startPosition =
            enemyManager.GetCellCenterWorld(
                bossCenterCell);


        Vector3 targetPosition =
            enemyManager.GetCellCenterWorld(
                dropCell);


        WeaponPickup pickup =
            Instantiate(
                weaponPickupPrefab,
                targetPosition,
                Quaternion.identity);


        pickup.Initialize(
            weaponDrop,
            dropCell,
            startPosition,
            player);


        Debug.Log(
            $"[Weapon Drop] {weaponDrop.WeaponName} / Cell : {dropCell}");
    }

    private void KillAllBodyTweens() {
        if (bodyRenderers == null)
            return;


        foreach (SpriteRenderer renderer
                 in bodyRenderers) {
            if (renderer == null)
                continue;


            renderer.DOKill();
        }
    }

    private void AppendFadeAll(
    Sequence sequence,
    float targetAlpha,
    float duration) {
        if (bodyRenderers == null)
            return;


        bool firstTween =
            true;


        foreach (SpriteRenderer renderer
                 in bodyRenderers) {
            if (renderer == null)
                continue;


            Tween tween =
                renderer.DOFade(
                    targetAlpha,
                    duration);


            if (firstTween) {
                sequence.Append(
                    tween);

                firstTween =
                    false;
            }
            else {
                sequence.Join(
                    tween);
            }
        }
    }
    // =========================================================
    // Cleanup
    // =========================================================

    private void OnDisable() {
        if (attackRoutine != null) {
            StopCoroutine(
                attackRoutine);

            attackRoutine = null;
        }


        idleMoveTween?.Kill();
        idleRotateTween?.Kill();

        hitSequence?.Kill();
        deathSequence?.Kill();


        if (visualRoot != null) {
            visualRoot.DOKill();
        }


        KillAllBodyTweens();
    }
}
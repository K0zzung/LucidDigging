using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BossExplosionEnemy : EnemyBase
{
    protected override bool UsesDepthHpScaling => false;

    // =========================================================
    // Visual
    // =========================================================

    [Header("Visual")]

    [Tooltip("실제 보스 Sprite가 들어있는 자식 Transform")]
    [SerializeField]
    private Transform visualRoot;

    [SerializeField]
    private SpriteRenderer[] bodyRenderers;


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
    // Explosion Pattern
    // =========================================================

    [Header("Explosion Pattern")]

    [Tooltip("공격 사이 최소 대기시간")]
    [SerializeField]
    private float attackIntervalMin = 2.5f;

    [Tooltip("공격 사이 최대 대기시간")]
    [SerializeField]
    private float attackIntervalMax = 4f;

    [Tooltip("경고가 표시되는 시간")]
    [SerializeField]
    private float warningDuration = 0.8f;

    [Tooltip("폭발 공격력")]
    [SerializeField]
    private int explosionAttack = 25;

    [Tooltip("3x3 경고에 사용할 기존 Warning Prefab")]
    [SerializeField]
    private BossSpikeWarning warningPrefab;

    [Tooltip("가운데 한 번 생성할 폭발 VFX")]
    [SerializeField]
    private GameObject explosionPrefab;

    [Tooltip("폭발 VFX 자동 제거 시간")]
    [SerializeField]
    private float explosionVfxLifetime = 1f;


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

    [Header("Audio")]

    [SerializeField]
    private AudioClip explosionAttackSFX;

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
        // Body Renderer 자동 수집
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


        SetAllBodyAlpha(1f);


        if (visualRoot != null) {
            visualOriginalLocalPosition =
                visualRoot.localPosition;

            visualOriginalLocalRotation =
                visualRoot.localRotation;

            PlayIdleMotion();
        }
    }


    private void SetAllBodyAlpha(
        float alpha) {
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
            $"[Explosion Boss] Activated : {name}");
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


        hitSequence?.Kill();

        KillAllBodyTweens();

        SetAllBodyAlpha(1f);


        hitSequence =
            DOTween.Sequence();


        AppendFadeAll(
            hitSequence,
            hitAlpha,
            hitFadeTime);


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


            yield return ExplosionAttackRoutine();
        }
    }


    // =========================================================
    // Explosion Attack
    // =========================================================

    private IEnumerator ExplosionAttackRoutine() {
        if (player == null)
            yield break;


        // =====================================================
        // 경고 시작 순간의 Player 위치 기억
        // =====================================================

        Vector3Int targetCell =
            player.CurrentCell;


        // =====================================================
        // X 범위 보정
        //
        // 3x3 공격이므로 가운데 Cell 기준
        // 좌우 한 칸씩 필요함
        // =====================================================
        
        if (arenaWidth >= 3) {
            int minCenterX =
                arenaLeftX + 1;

            int maxCenterX =
                arenaLeftX
                + arenaWidth
                - 2;


            targetCell.x =
                Mathf.Clamp(
                    targetCell.x,
                    minCenterX,
                    maxCenterX);
        }


        Debug.Log(
            $"[Explosion Boss] Warning / Target : {targetCell}");


        // =====================================================
        // 1. 3x3 Warning
        // =====================================================

        for (int x = -1;
             x <= 1;
             x++) {
            for (int y = 0;
                 y <= 2;
                 y++) {
                Vector3Int warningCell =
                    targetCell
                    + new Vector3Int(
                        x,
                        y,
                        0);


                if (warningPrefab == null)
                    continue;


                Vector3 position =
                    enemyManager.GetCellCenterWorld(
                        warningCell);


                BossSpikeWarning warning =
                    Instantiate(
                        warningPrefab,
                        position,
                        Quaternion.identity);


                warning.Play(
                    warningDuration);
            }
        }


        // =====================================================
        // Warning 시간 동안 회피
        // =====================================================

        yield return new WaitForSeconds(
            warningDuration);


        if (IsDead)
            yield break;


        // =====================================================
        // 2. Explosion VFX
        //
        // 가운데에 딱 1개만 생성
        // =====================================================

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX(explosionAttackSFX);
        }

        Vector3Int explosionCenterCell = targetCell+ Vector3Int.up;


        Vector3 explosionPosition =
            enemyManager.GetCellCenterWorld(
                explosionCenterCell);


        if (explosionPrefab != null) {
            GameObject explosion =
                Instantiate(
                    explosionPrefab,
                    explosionPosition,
                    Quaternion.identity);


            BossExplosionVFX explosionVFX =
                explosion.GetComponent<BossExplosionVFX>();


            if (explosionVFX != null) {
                explosionVFX.Play();
            }
            else {
                Debug.LogWarning(
                    "[Explosion Boss] " +
                    "Explosion Prefab에 BossExplosionVFX가 없습니다.");

                Destroy(
                    explosion,
                    explosionVfxLifetime);
            }
        }


        // =====================================================
        // 3. 3x3 Damage
        // =====================================================

        HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();


        for (int x = -1;
             x <= 1;
             x++) {
            for (int y = 0;
                 y <= 2;
                 y++) {
                Vector3Int attackCell =
                    targetCell
                    + new Vector3Int(
                        x,
                        y,
                        0);


                ApplyExplosionDamage(
                    attackCell,
                    hitEnemies);
            }
        }


        Debug.Log(
            $"[Explosion Boss] Explosion / Target : {targetCell}");
    }

    private void TrySpawnWeaponDrop() {
        if (weaponDrop == null)
            return;


        if (weaponPickupPrefab == null)
            return;


        if (WeaponManager.Instance == null)
            return;


        // 이미 가지고 있다면
        // 이번 Run에서는 아예 생성하지 않음
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


        if (!player.TryGetBossLandingCell(
            out Vector3Int dropCell)) {
            Debug.LogWarning(
                "[Weapon Drop] 착지 Cell을 찾지 못했습니다.");

            return;
        }


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

    // =========================================================
    // Explosion Damage
    // =========================================================

    private void ApplyExplosionDamage(
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
                    explosionAttack,
                    playerStatus.Defense);


            playerStatus.TakeDamage(
                damage);


            Debug.Log(
                $"[Explosion Boss] Player Hit / " +
                $"Damage : {damage}");
        }


        // =====================================================
        // Other Enemy
        //
        // 큰 Enemy가 여러 Cell을 차지하더라도
        // 한 공격에 한 번만 맞도록 처리
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
                    explosionAttack);
            }
        }


        // =====================================================
        // Terrain
        // =====================================================

        if (terrainManager != null &&
            terrainManager.HasTerrain(
                cell)) {
            terrainManager.DamageTerrain(
                cell,
                explosionAttack);
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
            AudioManager.Instance.PlayBossKill();
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
        // Grid에서 즉시 제거
        // =============================

        if (enemyManager != null) {
            enemyManager.RemoveEnemy(
                this);
        }


        // =============================
        // Weapon Drop
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
        // Death Sequence
        // =============================

        deathSequence =
            DOTween.Sequence();


        // 1. 좌우 흔들림
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


        // 2. Fade
        AppendFadeAll(
            deathSequence,
            0f,
            deathFadeDuration);


        // 3. 제거
        deathSequence.OnComplete(
            () => {
                Destroy(
                    gameObject);
            });


        Debug.Log(
            $"[Explosion Boss] Die : {name}");
    }


    // =========================================================
    // Tween Utility
    // =========================================================

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
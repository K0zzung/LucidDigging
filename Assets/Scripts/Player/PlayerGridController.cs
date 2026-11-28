using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
public class PlayerGridController : MonoBehaviour
{
    

    [Header("References")]
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private PlayerWeaponView weaponView;
    [SerializeField] private TerrainManager terrainManager;
    [SerializeField] private PlayerStatus playerStatus;
    public PlayerStatus Status => playerStatus;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private DepthManager depthManager;
    [SerializeField] private PlayerSkillController skillController;
    [SerializeField] private PlayerDashVFX dashVFX;
    [SerializeField] private DashEffect dashEffectPrefab;
    [SerializeField] private float dashEffectOffset = 0.6f;
    [SerializeField] private PlayerWeaponAttackVFX weaponAttackVFX;
    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.04f;

    [SerializeField] private GridPickupManager pickupManager;


    [Header("Hold Input")]
    [SerializeField]
    private float firstRepeatDelay = 0.12f;

    [SerializeField]
    private float repeatInterval = 0.07f;



    [SerializeField, Min(0f)]
    private float directionCombinationBuffer = 0.04f;

    private bool directionPending;
    private bool pendingFromDiagonal;
    private Vector2Int pendingDirection;
    private float pendingDirectionDeadline;

    private Vector2Int activeHeldDirection =Vector2Int.zero;

    private float nextHeldActionTime;


    private Vector3Int currentCell;
    public Vector3Int CurrentCell => currentCell;


    private bool bossPresentationLocked;
    private bool bossPresentationRequested;
    public void RequestBossPresentationLock() {
        bossPresentationRequested = true;
        directionPending = false; pendingFromDiagonal = false;
        activeHeldDirection = Vector2Int.zero; skillQueued = false;
    }
    public bool IsWaitingForBossPresentation { get; private set; }
    private Vector3 bossPresentationPosition;
    public void BeginBossPresentationLock() {
        bossPresentationRequested = false;
        bossPresentationLocked = true;
        moveTween?.Kill();
        bossPresentationPosition = transform.position;
        directionPending = false; pendingFromDiagonal = false;
        activeHeldDirection = Vector2Int.zero; skillQueued = false;
    }
    public void EndBossPresentationLock() { bossPresentationRequested = false; bossPresentationLocked = false; }
    private void LateUpdate() {
        if (bossPresentationLocked) transform.position = bossPresentationPosition;
    }
    private bool isBusy;
    public bool IsActionInProgress => bossPresentationRequested || bossPresentationLocked || HasActiveAction;
    public bool IsReadyForBossPresentation => !HasActiveAction;
    private bool HasActiveAction => isBusy || skillQueued ||
        (moveTween != null && moveTween.IsActive() && !moveTween.IsComplete()) ||
        (skillController != null && skillController.IsSkillLocked);


    private sealed class AttackCompletion {
        public bool AnimationComplete;
        public bool MovementComplete = true;
        public bool Released;
    }

    private void TryCompleteAttack(AttackCompletion completion) {
        if (completion.Released || !completion.AnimationComplete || !completion.MovementComplete)
            return;

        completion.Released = true;
        isBusy = false;
    }

    private Action TrackAttackMovement(AttackCompletion completion) {
        completion.MovementComplete = false;
        return () => {
            completion.MovementComplete = true;
            TryCompleteAttack(completion);
        };
    }
    private bool skillQueued;

    private SkillType queuedSkill;

    private Vector2Int queuedSkillDirection = Vector2Int.down;
    private Tween moveTween;

    private Vector2Int lastDirection = Vector2Int.down;

    private bool inputEnabled;

    private int startCellY;
    public int StartCellY => startCellY;


    private void Start() {
        playerStatus.OnPlayerDied += AbortDash;
        // 현재 Player 위치를 Grid Cell 좌표로 변환
        currentCell = terrainTilemap.WorldToCell(transform.position);

        // 정확하게 Cell 중앙으로 위치 보정
        transform.position = terrainTilemap.GetCellCenterWorld(currentCell);

        //게임 시작 위치를 Depth 0 기준으로 저장.
        startCellY = currentCell.y;
        depthManager.UpdateDepth(currentCell,startCellY);
    }


    private void Update() {
        if (bossPresentationRequested || bossPresentationLocked || !inputEnabled || playerStatus == null || playerStatus.IsDead)
            return;


        Vector2Int heldDirection = GetHeldDirection();
        if (dashRunning) UpdateDashSteering(heldDirection);
        else UpdatePendingDirection(heldDirection);


        Vector2Int skillDirection =
            heldDirection != Vector2Int.zero
            ? heldDirection
            : lastDirection;

        // Skill 입력 먼저 수집

        CaptureSkillInput(skillDirection);


        // 일반 공격 / 이동 / Dash 중
        if (isBusy)
            return;

        // 예약된 Skill 우선 실행

        if (skillQueued) {
            SkillType skill = queuedSkill;
            Vector2Int direction = queuedSkillDirection;
            // 실행 전에 Queue 제거
            skillQueued =false;
            if (skillController != null) {
                bool executed = skillController.TryUseSkill(skill,direction);
                // Skill 사용 성공

                if (executed) {
                    // Camera
                    PlaySkillCameraEffect(skill);

                    // Audio
                    PlaySkillAudio(skill);
                    return;
                }
            }
        }
        // 일반 Hold 이동 / 공격

        // Reserve the idle boundary before another held action can start.
        if (GameFlowController.Instance != null &&
            GameFlowController.Instance.TryBeginLevelUpSelection())
            return;

        HandleHeldInput(heldDirection);
    }


    public void SetInputEnabled(bool enabled) {
        inputEnabled = enabled;

        if (!enabled) {
            directionPending = false;
            activeHeldDirection = Vector2Int.zero;

        }
    }



    private void SetCurrentCell(Vector3Int cell) {
        currentCell = cell;
        if (depthManager != null) {
            depthManager.UpdateDepth(currentCell,startCellY);
        }
    }


    private void TryAction(Vector2Int direction) {
        lastDirection = direction;

        Vector3Int directionCell = new Vector3Int(direction.x,direction.y,0);

        Vector3Int targetCell = currentCell + directionCell;

        // 1. Wall

        if (wallTilemap != null &&wallTilemap.HasTile(targetCell)) {
            return;
        }


        // 2. Enemy

        if (enemyManager.TryGetEnemy(targetCell,out EnemyBase enemy)) {
            AttackEnemy(direction,targetCell,enemy);

            return;
        }


        // 3. Terrain

        if (terrainTilemap.HasTile(targetCell)) {
            AttackTerrain(direction,targetCell);

            return;
        }

        // 4. Empty

        Move(direction,targetCell);
    }

    private void Move(Vector2Int direction,Vector3Int targetCell) {
        isBusy = true;

        SetCurrentCell(targetCell);

        Vector3 targetPosition = terrainTilemap.GetCellCenterWorld(targetCell);

        moveTween?.Kill();

        moveTween = transform.DOMove(targetPosition, moveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                if (pickupManager != null) {
                    pickupManager.TryCollect(
                        currentCell,
                        transform);
                }

                isBusy = false;
            });
    }

    private void AttackTerrain(Vector2Int direction,Vector3Int targetCell) {
        isBusy =true;
        var completion = new AttackCompletion();
        weaponView.PlayAttack(
            direction,

            onHit: () => {
                // =============================
                // Weapon Slash VFX
                // =============================

                if (weaponAttackVFX != null) {
                    weaponAttackVFX.Play(
                        direction);
                }


                // =============================
                // Terrain Attack
                // =============================

                int terrainAttack =terrainManager.GetTerrainAttack(
                        targetCell);


                bool hardTerrain = terrainManager.GetTerrainDefense(targetCell) >= 30;
                int attack = terrainManager.IsTurret(targetCell) && WeaponManager.Instance!=null
                    ? WeaponManager.Instance.GetTurretAttack(playerStatus) : playerStatus.Attack;
                bool destroyed = terrainManager.DamageTerrain(targetCell,attack,out int dealtDamage);


                // 실제 준 데미지 표시
                ShowDealtDamage(
                    targetCell,
                    dealtDamage);


                // =============================
                // Camera
                // =============================

                if (dealtDamage > 0)
                    PlayNormalImpact(targetCell, direction, destroyed ? AttackImpactKind.Break :
                        hardTerrain ? AttackImpactKind.HardTerrain : AttackImpactKind.Terrain);


                // =============================
                // Destroy
                // =============================

                if (destroyed) {
                    MoveIntoDestroyedCell(
                        targetCell, TrackAttackMovement(completion));
                }

                // =============================
                // Survive
                // =============================

                else {
                    terrainManager.ShowHealthBar(
                        targetCell);


                    int damage =
                        DamageCalculator.CalculateDamage(
                            terrainAttack,
                            playerStatus.Defense);


                    playerStatus.TakeDamage(
                        damage);
                }
            },

            onComplete: () => {
                completion.AnimationComplete = true;
                TryCompleteAttack(completion);
            });
    }
    // Visual feedback only; called at the existing onHit callback after damage.
    private void PlayNormalImpact(Vector3Int cell, Vector2Int direction, AttackImpactKind kind) {
        if (weaponAttackVFX != null)
            weaponAttackVFX.PlayImpact(terrainTilemap.GetCellCenterWorld(cell), direction, kind);
        if (CameraShakeController.Instance != null)
            CameraShakeController.Instance.ShakeAttackImpact(kind);
    }

    private bool IsBoss(EnemyBase enemy) {
        return
            enemy is BossEnemy ||
            enemy is BossExplosionEnemy;
    }

    private void ShowDealtDamage(Vector3Int cell,int damage) {
        if (FloatingDamageManager.Instance == null)
            return;


        Vector3 position = terrainTilemap.GetCellCenterWorld(cell) + Vector3.up * 0.35f;


        FloatingDamageManager.Instance.ShowDealtDamage(position,damage);
    }
    private void AttackEnemy(Vector2Int direction,Vector3Int targetCell,EnemyBase enemy) {
        isBusy = true;
        var completion = new AttackCompletion();


        weaponView.PlayAttack(
            direction,

            onHit: () => {
                // =============================
                // Weapon Slash VFX
                // =============================

                if (weaponAttackVFX != null) {
                    weaponAttackVFX.Play(
                        direction);
                }


                int enemyAttack =
                    enemy.GetAttack();


                // =============================
                // Player → Enemy
                // =============================

                bool killed =
                    enemy.TakeDamage(
                        WeaponManager.Instance != null ? WeaponManager.Instance.GetNormalAttack(playerStatus,enemy) : playerStatus.Attack,
                        out int dealtDamage);


                ShowDealtDamage(
                    targetCell,
                    dealtDamage);


                if (dealtDamage > 0)
                    PlayNormalImpact(targetCell, direction, IsBoss(enemy) ? AttackImpactKind.Boss :
                        killed ? AttackImpactKind.Break : AttackImpactKind.Enemy);

                // =============================
                // Boss Kill
                // =============================

                if (killed &&IsBoss(enemy)) {

                    // Boss 처치 Camera
                    if (CameraShakeController.Instance != null) {
                        CameraShakeController.Instance.ShakeBossKill();
                    }


                    CompleteBossDefeat(TrackAttackMovement(completion),enemy);


                    return;
                }


                // =============================
                // Normal Hit Camera
                // =============================




                // =============================
                // 일반 Enemy Kill
                // =============================

                if (killed) {
                    // 일반 Enemy 처치 Sound
                    if (AudioManager.Instance != null) {
                        AudioManager.Instance.PlayEnemyKill();
                    }


                    MoveIntoDestroyedCell(targetCell, TrackAttackMovement(completion));


                    return;
                }


                // =============================
                // Enemy 생존
                // =============================

                enemy.ShowHealthBar();

                // Boss damage is handled by its Spike/Explosion pattern.
                if (IsBoss(enemy))
                    return;

                int damage =
                    DamageCalculator.CalculateDamage(enemyAttack,playerStatus.Defense);

                playerStatus.TakeDamage(damage);
            },

            onComplete: () => {
                completion.AnimationComplete = true;
                TryCompleteAttack(completion);
            });
    }

    private void MoveIntoDestroyedCell(Vector3Int targetCell, Action onComplete) {
        // 논리적인 현재 위치 변경
        currentCell = targetCell;
        SetCurrentCell(targetCell);
        Vector3 targetPosition =
            terrainTilemap.GetCellCenterWorld(targetCell);

        moveTween?.Kill();

        moveTween = transform.DOMove(targetPosition, moveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                if (pickupManager != null) {
                    pickupManager.TryCollect(currentCell, transform);
                }
                onComplete?.Invoke();
            });
    }

    private Vector2Int GetHeldDirection() {
        bool left = Input.GetKey(KeyCode.LeftArrow);

        bool right = Input.GetKey(KeyCode.RightArrow);

        bool down =Input.GetKey(KeyCode.DownArrow);

        // Down 포함

        if (down) {
            if (left && !right) {
                return new Vector2Int(-1,-1);
            }

            if (right && !left) {
                return new Vector2Int(1,-1);
            }

            // 좌우를 동시에 누른 경우에도
            // 수평 입력은 상쇄하고 아래로 처리
            return Vector2Int.down;
        }


        // 수평

        if (left && !right) {
            return Vector2Int.left;
        }

        if (right && !left) {
            return Vector2Int.right;
        }


        // Left + Right 동시 입력
        return Vector2Int.zero;
    }

    private void UpdatePendingDirection(Vector2Int direction) {
        if (!directionPending)
            return;

        // A released diagonal must not leave a delayed single-direction action.
        // Check even after the deadline: an attack or skill may still be busy.
        if (pendingFromDiagonal && direction == Vector2Int.zero &&
            !Input.GetKey(KeyCode.LeftArrow) &&
            !Input.GetKey(KeyCode.RightArrow) &&
            !Input.GetKey(KeyCode.DownArrow)) {
            directionPending = false;
            activeHeldDirection = Vector2Int.zero;
            return;
        }

        if (Time.time >= pendingDirectionDeadline)
            return;

        // Freeze a completed diagonal; releasing a key must not lose the tap.
        if (pendingDirection.x != 0 && pendingDirection.y == -1)
            return;

        // Preserve the existing neutral result for opposing horizontal keys.
        if (direction == Vector2Int.zero &&
            Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.RightArrow)) {
            directionPending = false;
            return;
        }

        if (direction != Vector2Int.zero) {
            pendingDirection = direction;
            if (direction.x != 0 && direction.y == -1)
                pendingFromDiagonal = false;
        }
    }

    private void HandleHeldInput(Vector2Int direction) {
        if (!directionPending) {
            if (direction == Vector2Int.zero) {
                activeHeldDirection = Vector2Int.zero;
                return;
            }

            if (direction != activeHeldDirection) {
                directionPending = true;
                pendingFromDiagonal = activeHeldDirection.x != 0 &&
                    activeHeldDirection.y == -1 &&
                    (direction.x == 0 || direction.y == 0);
                pendingDirection = direction;
                pendingDirectionDeadline = Time.time + Mathf.Max(0f, directionCombinationBuffer);
            }
        }

        if (directionPending) {
            bool diagonalReady = pendingDirection.x != 0 && pendingDirection.y == -1;
            if (!diagonalReady && Time.time < pendingDirectionDeadline)
                return;

            Vector2Int actionDirection = pendingDirection;
            directionPending = false;
            // Track the current keys so a completed tap is not replayed.
            activeHeldDirection = direction;
            nextHeldActionTime = Time.time + firstRepeatDelay;
            TryAction(actionDirection);
            return;
        }

        if (Time.time < nextHeldActionTime)
            return;

        TryAction(activeHeldDirection);
        nextHeldActionTime = Time.time + repeatInterval;
    }

    private void OnDisable() {
        IsWaitingForBossPresentation = false;
        EndBossPresentationLock();
        AbortDash();
        if (dashVFX != null) dashVFX.Cancel();
        directionPending = false;
        moveTween?.Kill();
    }

    private bool dashRunning;
    private float dashEndsAt;
    private int lastDashRefreshFrame = -1;
    private Coroutine dashCoroutine;
    private Action dashCompletion;

    public bool TryRefreshDash(float duration) {
        if (!dashRunning || !inputEnabled || playerStatus.IsDead || Time.timeScale <= 0f ||
            Time.time >= dashEndsAt || lastDashRefreshFrame == Time.frameCount || duration <= 0f ||
            RunSkillManager.Instance == null) return false;
        if (!RunSkillManager.Instance.TryConsumeSkill(SkillType.Dash)) return false;
        lastDashRefreshFrame = Time.frameCount;
        dashEndsAt = Time.time + duration;
        return true;
    }
    private void CompleteDashSkill() {
        Action callback = dashCompletion;
        dashCompletion = null;
        callback?.Invoke();
    }
    private void AbortDash() {
        if (!dashRunning) return;
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        dashCoroutine = null;
        moveTween?.Kill();
        if (terrainTilemap != null) transform.position = terrainTilemap.GetCellCenterWorld(currentCell);
        dashRunning = false;
        ClearDashSteering();
        playerStatus.SetInvincible(false);
        isBusy = false;
        if (dashVFX != null) dashVFX.Cancel();
        CompleteDashSkill();
    }
    private void OnDestroy() {
        if (playerStatus != null) playerStatus.OnPlayerDied -= AbortDash;
        AbortDash();
    }
    private Vector2Int dashDirection, dashCandidate;
    private float dashDirectionDeadline;
    private bool dashDirectionPending;

    private void UpdateDashSteering(Vector2Int input) {
        if (input == Vector2Int.zero || input == dashDirection) {
            dashDirectionPending = false;
            return;
        }
        if (!dashDirectionPending || input != dashCandidate) {
            dashCandidate = input;
            dashDirectionDeadline = Time.time + Mathf.Max(0f, directionCombinationBuffer);
            dashDirectionPending = true;
        }
        if ((input.x != 0 && input.y == -1) || Time.time >= dashDirectionDeadline) {
            dashDirection = input;
            dashDirectionPending = false;
        }
    }
    private void ClearDashSteering() {
        dashDirectionPending = false;
        directionPending = false;
        pendingFromDiagonal = false;
        activeHeldDirection = Vector2Int.zero;
    }

    private IEnumerator DashRoutine(Vector2Int direction, float duration, float attackPercent,float stepInterval, float dashMoveDuration,Action onComplete) {
        isBusy = true;
        dashRunning = true;
        dashDirection = direction;
        ClearDashSteering();
        if (dashVFX != null) dashVFX.Begin(direction);
        SpawnDashEffect(direction);


        lastDirection = direction;


        // Dash 중 무적
        playerStatus.SetInvincible(true);


        dashEndsAt = Time.time + duration;
        lastDashRefreshFrame = -1;


        int dashAttack =Mathf.Max(1, Mathf.RoundToInt(playerStatus.Attack * attackPercent/ 100f));


        // ===================================
        // Dash 진행
        // ===================================

        bool firstDashTick = true;
        while (Time.time < dashEndsAt && !playerStatus.IsDead) {
            if (!firstDashTick) UpdateDashSteering(GetHeldDirection());
            firstDashTick = false;
            direction = dashDirection;
            lastDirection = direction;
            if (weaponView != null) weaponView.SetDirection(direction);
            Vector3Int directionCell = new Vector3Int(direction.x, direction.y,  0);


            Vector3Int targetCell = currentCell + directionCell;


            // ===================================
            // Wall
            // ===================================

            if (wallTilemap != null &&
                wallTilemap.HasTile(targetCell)) {
                if (dashVFX != null) dashVFX.Contact(terrainTilemap.GetCellCenterWorld(targetCell), direction, true, true);
                break;
            }


            bool canMove = false;




            // ===================================
            // Enemy
            // ===================================

            if (enemyManager.TryGetEnemy(targetCell,out EnemyBase enemy)) {


                bool killed = enemy.TakeDamage(dashAttack,out int dealtDamage);
                if (dealtDamage > 0 && dashVFX != null) dashVFX.Contact(terrainTilemap.GetCellCenterWorld(targetCell), direction, false, killed);


                // Floating Damage
                ShowDealtDamage(
                    targetCell,
                    dealtDamage);


                // ===================================
                // Enemy 사망
                // ===================================

                if (killed) {
                    // =============================
                    // Boss Kill
                    // =============================

                    if (IsBoss(enemy)) {
                        // Boss 처치 Camera
                        if (CameraShakeController.Instance != null) {
                            CameraShakeController.Instance.ShakeBossKill();
                        }


                        if (dashVFX != null) dashVFX.End();
                        playerStatus.SetInvincible(false);


                        ClearDashSteering();
                        dashRunning = false;
                        CompleteDashSkill();


                        CompleteBossDefeat(null,enemy);


                        yield break;
                    }


                    // =============================
                    // 일반 Enemy Kill
                    // =============================

                    if (AudioManager.Instance != null) {
                        AudioManager.Instance.PlayEnemyKill();
                    }


                    canMove =
                        true;
                }

                // ===================================
                // Enemy 생존
                // ===================================

                else {
                    enemy.ShowHealthBar();
                }
            }


            // ===================================
            // Terrain
            // ===================================

            else if (terrainTilemap.HasTile(
                targetCell)) {


                bool destroyed =terrainManager.DamageTerrain(targetCell,dashAttack,out int dealtDamage);
                if (dealtDamage > 0 && dashVFX != null) dashVFX.Contact(terrainTilemap.GetCellCenterWorld(targetCell), direction, false, destroyed);


                ShowDealtDamage(targetCell, dealtDamage);


                if (destroyed) {
                    canMove = true;
                }

                else {
                    terrainManager.ShowHealthBar(targetCell);
                }
            }


            // ===================================
            // Empty
            // ===================================

            else {
                canMove =true;
            }


            // ===================================
            // Dash 충돌 Camera
            // ===================================




            // ===================================
            // Dash VFX
            // ===================================

            SpawnDashEffect(direction);


            // ===================================
            // 이동
            // ===================================

            if (canMove) {
                yield return DashMoveToCell(targetCell,dashMoveDuration);
            }


            yield return new WaitForSeconds(stepInterval);
        }


        // ===================================
        // Dash 종료
        // ===================================

        ClearDashSteering();
        dashRunning = false;
        if (dashVFX != null) dashVFX.End();
        playerStatus.SetInvincible(false);


        isBusy =  false;


        CompleteDashSkill();
    }

    private IEnumerator DashMoveToCell(Vector3Int targetCell,float duration) {
        SetCurrentCell(targetCell);


        Vector3 targetPosition =terrainTilemap.GetCellCenterWorld(targetCell);


        moveTween?.Kill();


        moveTween = transform.DOMove(targetPosition, duration)
            .SetEase(Ease.Linear);


        yield return moveTween.WaitForCompletion();


        // 지나가는 Cell의 Item / Currency도 먹음
        if (pickupManager != null) {
            pickupManager.TryCollect(currentCell, transform);
        }
    }

    private void CompleteBossDefeat(Action onComplete = null, EnemyBase defeatedBoss = null) {
        if (defeatedBoss is BossEnemy hades && hades.IsDeathPresentationPlaying) {
            IsWaitingForBossPresentation = true;
            StartCoroutine(WaitForHadesPresentation(hades,onComplete));
            return;
        }
        // Boss defeat no longer moves the player or changes their Grid Cell.
        if (onComplete != null) onComplete();
        else isBusy=false;
    }

    private IEnumerator WaitForHadesPresentation(BossEnemy hades,Action onComplete) {
        while (hades != null && hades.IsDeathPresentationPlaying && !playerStatus.IsDead) yield return null;
        IsWaitingForBossPresentation = false;
        EndBossPresentationLock();
        if (playerStatus.IsDead || !isActiveAndEnabled) { onComplete?.Invoke(); yield break; }
        // Release the original attack lock without an automatic descent.
        CompleteBossDefeat(onComplete);
    }

    public bool TryGetBossLandingCell(out Vector3Int landingCell) {
        Vector3Int checkCell =currentCell + Vector3Int.down;

        const int maxSearchDistance =30;


        for (int i = 0;i < maxSearchDistance;i++) {
            // =============================
            // Terrain
            // =============================

            if (terrainTilemap.HasTile(
                checkCell)) {
                landingCell =checkCell+ Vector3Int.up;

                return true;
            }


            // =============================
            // 다른 Enemy
            // =============================

            if (enemyManager != null &&
                enemyManager.HasEnemy(checkCell)) {
                landingCell =checkCell+ Vector3Int.up;

                return true;
            }


            checkCell +=Vector3Int.down;
        }


        landingCell =currentCell;


        return false;
    }
    private void SpawnDashEffect(Vector2Int direction) {
        if (dashEffectPrefab == null)
            return;

        Vector3 spawnPosition =
            transform.position +
            new Vector3(direction.x, direction.y, 0f) * dashEffectOffset;

        if (dashVFX != null) {
            dashVFX.PlayLegacy(dashEffectPrefab, spawnPosition, direction);
        }
        else {
            DashEffect effect = Instantiate(dashEffectPrefab, spawnPosition, Quaternion.identity);
            effect.Play(spawnPosition, direction);
        }
    }

    public bool StartDash(Vector2Int direction,float duration,float attackPercent,float stepInterval,float moveDuration, Action onComplete = null) {
        if (isBusy)
            return false;

        if (direction == Vector2Int.zero)
            return false;

        Debug.Log("Dash 시작");
        dashCompletion = onComplete;
        dashCoroutine = StartCoroutine(DashRoutine(direction,duration,attackPercent,stepInterval,moveDuration,onComplete));
        return true;
    }

    private readonly EnemyBase[] shockwaveCells = new EnemyBase[49];
    private readonly Dictionary<EnemyBase, int> shockwaveTargetAttack = new Dictionary<EnemyBase, int>(49);
    private readonly HashSet<EnemyBase> damagedShockwaveEnemies = new HashSet<EnemyBase>();
    public static int GetShockwaveCellAttack(int attack, float percent, int dx, int dy, bool attenuateOuter) {
        float weight = attenuateOuter && Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) == 3 ? 0.5f : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(attack * percent * weight / 100f));
    }

    public void PerformShockwave(int size,float attackPercent, ShockwaveEffect visual = null, bool attenuateOuter = false) {
        // =============================
        // 크기 보정
        // =============================

        size = Mathf.Clamp(size,1,7);


        if (size % 2 == 0) {
            size++;
        }


        Vector3Int origin = currentCell;
        int half = size / 2;
        if (visual != null) visual.SetGrid(transform.position, size,
            terrainTilemap.GetCellCenterWorld(currentCell + Vector3Int.right) - terrainTilemap.GetCellCenterWorld(currentCell),
            terrainTilemap.GetCellCenterWorld(currentCell + Vector3Int.up) - terrainTilemap.GetCellCenterWorld(currentCell));


        int attackAtCast = playerStatus.Attack;


        // 큰 Enemy 중복 타격 방지
        damagedShockwaveEnemies.Clear();
        shockwaveTargetAttack.Clear();
        // Resolve each multi-cell target's strongest attack before applying damage.
        int index = 0;
        for (int x = -half; x <= half; x++) for (int y = -half; y <= half; y++) {
            Vector3Int cell = origin + new Vector3Int(x, y, 0);
            shockwaveCells[index] = null;
            if ((wallTilemap == null || !wallTilemap.HasTile(cell)) && enemyManager.TryGetEnemy(cell, out EnemyBase target)) {
                int attack = GetShockwaveCellAttack(attackAtCast, attackPercent, x, y, attenuateOuter);
                shockwaveCells[index] = target;
                if (!shockwaveTargetAttack.TryGetValue(target, out int previous) || attack > previous)
                    shockwaveTargetAttack[target] = attack;
            }
            index++;
        }
        index = 0;




        for (int x = -half; x <= half; x++) {
            for (int y = -half;y <= half;y++) {
                Vector3Int cell = origin + new Vector3Int(x, y,0);
                EnemyBase target = shockwaveCells[index++];


                // =============================
                // Wall
                // =============================

                if (wallTilemap != null &&
                    wallTilemap.HasTile(cell)) {
                    continue;
                }


                // =============================
                // Enemy
                // =============================

                if (target != null) {
                    EnemyBase enemy = target;
                    // 큰 Enemy가 여러 Cell이어도
                    // 딱 한 번만 공격
                    if (damagedShockwaveEnemies.Add(enemy)) {
                        bool killed = enemy.TakeDamage(shockwaveTargetAttack[enemy], out int dealtDamage);
                        if (dealtDamage > 0 && visual != null) visual.AddImpact(terrainTilemap.GetCellCenterWorld(cell), dealtDamage, killed || IsBoss(enemy));


                        ShowDealtDamage(cell,dealtDamage);


                        // =============================
                        // Enemy 생존
                        // =============================

                        if (!killed) {
                            enemy.ShowHealthBar();
                        }


                        // =============================
                        // Boss Kill
                        // =============================

                        else if (IsBoss(enemy)) {
                            if (CameraShakeController.Instance != null) {
                                CameraShakeController.Instance.ShakeBossKill();
                            }


                            CompleteBossDefeat(null,enemy);
                        }


                        // =============================
                        // 일반 Enemy Kill
                        // =============================

                        else {
                            if (AudioManager.Instance != null) {
                                AudioManager.Instance.PlayEnemyKill();
                            }
                        }
                    }


                    continue;
                }


                // =============================
                // Terrain
                // =============================

                if (terrainTilemap.HasTile(cell)) {
                    bool destroyed = terrainManager.DamageTerrain(cell, GetShockwaveCellAttack(attackAtCast, attackPercent, x, y, attenuateOuter),out int dealtDamage);
                    if (dealtDamage > 0 && visual != null) visual.AddImpact(terrainTilemap.GetCellCenterWorld(cell), dealtDamage, destroyed);

                    ShowDealtDamage(cell, dealtDamage);

                    // 살아남은 Terrain만 HP Bar 표시
                    if (!destroyed) {
                        terrainManager.ShowHealthBar(cell);
                    }
                }
            }
        }
        System.Array.Clear(shockwaveCells, 0, shockwaveCells.Length);
        damagedShockwaveEnemies.Clear();
        shockwaveTargetAttack.Clear();
    }
    private void CaptureSkillInput(Vector2Int direction) {
        // Q during Dash is a refresh event, never a queued second coroutine.
        // Process it even when another skill is already queued.
        if (dashRunning && Input.GetKeyDown(KeyCode.Q)) {
            if (skillController != null) skillController.TryContinueDash();
            return;
        }
        // 이미 먼저 눌린 Skill이 예약되어 있음
        if (skillQueued)
            return;


        // 현재 다른 Skill 사용 중
        if (skillController != null &&
            skillController.IsSkillLocked) {
            return;
        }


        // 같은 Frame이라면 Q > W > E
        if (Input.GetKeyDown(KeyCode.Q)) {
            QueueSkill(SkillType.Dash,direction);

            return;
        }


        if (Input.GetKeyDown(KeyCode.W)) {
            QueueSkill(SkillType.Shield, direction);
            return;
        }


        if (Input.GetKeyDown(KeyCode.E)) {
            QueueSkill(
                SkillType.Shockwave,
                direction);
        }
    }

    private void PlaySkillAudio(SkillType skill) {
        if (AudioManager.Instance == null)
            return;


        switch (skill) {
            case SkillType.Dash:

                AudioManager.Instance.PlayDash();
                break;


            case SkillType.Shield:

                AudioManager.Instance.PlayShield();
                break;


            case SkillType.Shockwave:

                AudioManager.Instance.PlayShockwave();
                break;
        }
    }
    private void PlaySkillCameraEffect(SkillType skill) {
        if (CameraShakeController.Instance == null)
            return;


        switch (skill) {
            case SkillType.Dash:

                CameraShakeController.Instance
                    .ShakeDashStart();

                break;


            case SkillType.Shield:

                CameraShakeController.Instance
                    .ShakeShield();

                break;


            case SkillType.Shockwave:

                CameraShakeController.Instance
                    .ShakeShockwave();

                break;
        }
    }

    private void QueueSkill(SkillType skill,Vector2Int direction) {
        skillQueued = true;

        queuedSkill = skill;

        queuedSkillDirection = direction;

        Debug.Log( $"[Skill Input] Queued : {skill}");
    }
}
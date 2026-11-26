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



    private Vector2Int activeHeldDirection =Vector2Int.zero;

    private float nextHeldActionTime;

    private bool waitingFirstRepeat;

    private Vector3Int currentCell;
    public Vector3Int CurrentCell => currentCell;


    private bool isBusy;
    private bool skillQueued;

    private SkillType queuedSkill;

    private Vector2Int queuedSkillDirection = Vector2Int.down;
    private Tween moveTween;

    private Vector2Int lastDirection = Vector2Int.down;

    private bool inputEnabled;

    private int startCellY;
    public int StartCellY => startCellY;


    private void Start() {
        // 현재 Player 위치를 Grid Cell 좌표로 변환
        currentCell = terrainTilemap.WorldToCell(transform.position);

        // 정확하게 Cell 중앙으로 위치 보정
        transform.position = terrainTilemap.GetCellCenterWorld(currentCell);

        //게임 시작 위치를 Depth 0 기준으로 저장.
        startCellY = currentCell.y;
        depthManager.UpdateDepth(currentCell,startCellY);
    }


    private void Update() {
        if (!inputEnabled)
            return;


        Vector2Int heldDirection = GetHeldDirection();


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

        HandleHeldInput(heldDirection);
    }


    public void SetInputEnabled(bool enabled) {
        inputEnabled = enabled;

        if (!enabled) {
            activeHeldDirection = Vector2Int.zero;

            waitingFirstRepeat = false;
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


                bool destroyed = terrainManager.DamageTerrain(targetCell,playerStatus.Attack,out int dealtDamage);


                // 실제 준 데미지 표시
                ShowDealtDamage(
                    targetCell,
                    dealtDamage);


                // =============================
                // Camera
                // =============================

                if (CameraShakeController.Instance != null) {
                    CameraShakeController.Instance
                        .ShakeNormal();
                }


                // =============================
                // Destroy
                // =============================

                if (destroyed) {
                    MoveIntoDestroyedCell(
                        targetCell);
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
                isBusy =
                    false;
            });
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
                        playerStatus.Attack,
                        out int dealtDamage);


                ShowDealtDamage(
                    targetCell,
                    dealtDamage);


                // =============================
                // Boss Kill
                // =============================

                if (killed &&IsBoss(enemy)) {

                    // Boss 처치 Camera
                    if (CameraShakeController.Instance != null) {
                        CameraShakeController.Instance.ShakeBossKill();
                    }


                    MoveDownAfterBossDeath();


                    return;
                }


                // =============================
                // Normal Hit Camera
                // =============================

                if (CameraShakeController.Instance != null) {
                    CameraShakeController.Instance.ShakeNormal();
                }


                // =============================
                // 일반 Enemy Kill
                // =============================

                if (killed) {
                    // 일반 Enemy 처치 Sound
                    if (AudioManager.Instance != null) {
                        AudioManager.Instance.PlayEnemyKill();
                    }


                    MoveIntoDestroyedCell(targetCell);


                    return;
                }


                // =============================
                // Enemy 생존
                // =============================

                enemy.ShowHealthBar();


                int damage =
                    DamageCalculator.CalculateDamage(enemyAttack,playerStatus.Defense);

                playerStatus.TakeDamage(damage);
            },

            onComplete: () => {
                isBusy =
                    false;
            });
    }

    private void MoveIntoDestroyedCell(Vector3Int targetCell) {
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

    private void HandleHeldInput(Vector2Int direction) {
        // 아무 키도 안 누름
        if (direction == Vector2Int.zero) {
            activeHeldDirection = Vector2Int.zero;

            waitingFirstRepeat = false;

            return;
        }

        // 새 방향이 입력됨

        if (direction != activeHeldDirection) {
            activeHeldDirection = direction;

            // 다음 행동은 즉시 가능
            nextHeldActionTime = Time.time;

            waitingFirstRepeat = true;
        }


        // 아직 반복 입력 시간 아님
        if (Time.time <nextHeldActionTime) {
            return;
        }


        TryAction(activeHeldDirection);


        // 첫 입력 후에는 약간의 Delay
        if (waitingFirstRepeat) {
            nextHeldActionTime = Time.time+ firstRepeatDelay;

            waitingFirstRepeat = false;
        }
        else {
            nextHeldActionTime = Time.time + repeatInterval;
        }
    }

    private void OnDisable() {
        moveTween?.Kill();
    }

    private IEnumerator DashRoutine(Vector2Int direction, float duration, float attackPercent,float stepInterval, float dashMoveDuration,Action onComplete) {
        isBusy = true;


        lastDirection = direction;


        // Dash 중 무적
        playerStatus.SetInvincible(true);


        float startTime = Time.time;


        int dashAttack =Mathf.Max(1, Mathf.RoundToInt(playerStatus.Attack * attackPercent/ 100f));


        // ===================================
        // Dash 진행
        // ===================================

        while (Time.time - startTime <duration) {
            Vector3Int directionCell = new Vector3Int(direction.x, direction.y,  0);


            Vector3Int targetCell = currentCell + directionCell;


            // ===================================
            // Wall
            // ===================================

            if (wallTilemap != null &&
                wallTilemap.HasTile(targetCell)) {
                break;
            }


            bool canMove = false;


            bool hitSomething = false;


            // ===================================
            // Enemy
            // ===================================

            if (enemyManager.TryGetEnemy(targetCell,out EnemyBase enemy)) {
                hitSomething = true;


                bool killed = enemy.TakeDamage(dashAttack,out int dealtDamage);


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


                        playerStatus.SetInvincible(false);


                        onComplete?.Invoke();


                        MoveDownAfterBossDeath();


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
                hitSomething =true;


                bool destroyed =terrainManager.DamageTerrain(targetCell,dashAttack,out int dealtDamage);


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

            if (hitSomething &&
                CameraShakeController.Instance != null) {
                CameraShakeController.Instance.ShakeDashImpact();
            }


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

        playerStatus.SetInvincible(false);


        isBusy =  false;


        onComplete?.Invoke();
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

    private void MoveDownAfterBossDeath() {
        if (TryGetBossLandingCell(out Vector3Int landingCell)) {
            MoveToLandingCell(landingCell);
            return;
        }

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
    private void MoveToLandingCell(Vector3Int landingCell) {
        isBusy = true;


        SetCurrentCell(landingCell);


        Vector3 targetPosition =
            terrainTilemap.GetCellCenterWorld(landingCell);


        moveTween?.Kill();


        moveTween =
            transform.DOMove(
                targetPosition,
                0.15f)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                // 착지 위치에 Pickup이 있으면 획득
                if (pickupManager != null) {
                    pickupManager.TryCollect(currentCell,transform);
                }


                isBusy = false;
            });
    }
    private void SpawnDashEffect(Vector2Int direction) {
        if (dashEffectPrefab == null)
            return;

        Vector3 spawnPosition =
            transform.position +
            new Vector3(direction.x, direction.y, 0f) * dashEffectOffset;

        DashEffect effect =
            Instantiate(dashEffectPrefab, spawnPosition, Quaternion.identity);

        effect.Play(spawnPosition, direction);
    }

    public bool StartDash(Vector2Int direction,float duration,float attackPercent,float stepInterval,float moveDuration, Action onComplete = null) {
        if (isBusy)
            return false;

        if (direction == Vector2Int.zero)
            return false;

        Debug.Log("Dash 시작");
        StartCoroutine(DashRoutine(direction,duration,attackPercent,stepInterval,moveDuration,onComplete));
        return true;
    }

    public void PerformShockwave(int size,float attackPercent) {
        // =============================
        // 크기 보정
        // =============================

        size = Mathf.Max(1,size);


        if (size % 2 == 0) {
            size++;
        }


        int half = size / 2;


        int shockwaveAttack = Mathf.Max(1,Mathf.RoundToInt(playerStatus.Attack* attackPercent/ 100f));


        // 큰 Enemy 중복 타격 방지
        HashSet<EnemyBase> damagedEnemies = new HashSet<EnemyBase>();




        for (int x = -half; x <= half; x++) {
            for (int y = -half;y <= half;y++) {
                Vector3Int cell =currentCell+ new Vector3Int(x, y,0);


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

                if (enemyManager.TryGetEnemy(cell,out EnemyBase enemy)) {
                    // 큰 Enemy가 여러 Cell이어도
                    // 딱 한 번만 공격
                    if (damagedEnemies.Add(enemy)) {
                        bool killed = enemy.TakeDamage(shockwaveAttack, out int dealtDamage);


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


                            MoveDownAfterBossDeath();
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
                    bool destroyed = terrainManager.DamageTerrain(cell, shockwaveAttack,out int dealtDamage);

                    ShowDealtDamage(cell, dealtDamage);

                    // 살아남은 Terrain만 HP Bar 표시
                    if (!destroyed) {
                        terrainManager.ShowHealthBar(cell);
                    }
                }
            }
        }
    }
    private void CaptureSkillInput(Vector2Int direction) {
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
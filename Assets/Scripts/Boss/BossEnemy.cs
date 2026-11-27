using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BossEnemy : EnemyBase
{
    protected override bool UsesDepthHpScaling => false;
    protected override bool UseWorldHealthBar => false;
    [Header("Phase 2 and petrification")]
    [SerializeField] private Sprite phaseTwoHead;
    [SerializeField] private Sprite phaseTwoJaw;
    [SerializeField] private Sprite bossHealthFrame;
    [SerializeField] private TMPro.TMP_FontAsset bossNameFont;
    [Header("Boss HUD position - X right / Y up (canvas units)")]
    [SerializeField] private Vector2 bossHUDOffset = Vector2.zero;
    public Vector2 HUDOffset => bossHUDOffset;
    [SerializeField] private Sprite[] stoneFrames;
    [SerializeField, Min(0)] private int phaseTwoMaxHP;
    [SerializeField, Min(1f)] private float phaseTwoPower = 1.5f;
    [SerializeField, Min(1f)] private float phaseTwoPatternSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float stoneFrameDuration = 1f;
    public int Phase { get; private set; } = 1;
    public bool IsDeathPresentationPlaying { get; private set; }
    public event System.Action HealthChanged;
    private HadesBossHUD bossHUD;
    private float PatternSpeed => Phase == 2 ? phaseTwoPatternSpeed : 1f;
    private int PatternAttack(int value) => Mathf.Max(1,Mathf.RoundToInt(value*(Phase == 2 ? phaseTwoPower : 1f)));
    protected override bool TryPreventDeath() {
        if (Phase != 1 || !awakened) return false;
        Phase = 2;
        MaxHP = phaseTwoMaxHP > 0 ? phaseTwoMaxHP : MaxHP;
        CurrentHP = MaxHP;
        // A phase refill is not death: preserve warnings, active attacks and projectiles.
        if (headRenderer != null && phaseTwoHead != null) headRenderer.sprite = phaseTwoHead;
        if (jawRenderer != null && phaseTwoJaw != null) jawRenderer.sprite = phaseTwoJaw;
        if(awakeningWarning!=null)awakeningWarning.ShowBossMessage("\ub0b4 \uc7a0\uc744 \ubc29\ud574\ud558\ub2e4\ub2c8, \ud6c4\ud68c\ud558\uac8c \ud574\uc8fc\ub9c8..",awakeningMessageFont);
        return true;
    }

    // =========================================================
    // Visual
    // =========================================================

    [Header("Visual")]

    [Tooltip("실제 보스 Sprite가 들어있는 자식 Transform")]
    [SerializeField]
    private Transform visualRoot;

    [SerializeField] private SpriteRenderer[] bodyRenderers;
    [Header("Hades presentation - grid registration unchanged")]
    [SerializeField] private Transform headPart;
    [SerializeField] private Transform jawPart;
    [SerializeField] private Transform presentationHealthBarAnchor;
    [SerializeField, Range(0.5f,1f)] private float arenaWidthFill = 0.94f;
    [SerializeField] private float visualLiftCells = 1f;
    [SerializeField] private float breathScaleY = 0.012f;
    [SerializeField] private float jawTravel = 0.1f;
    [Header("Awakening presentation")]
    [SerializeField, Min(0.1f)] private float awakeningCameraDuration = 0.65f;
    [SerializeField] private TMPro.TMP_FontAsset awakeningMessageFont;
    private CameraFollowTarget awakeningCamera;
    private DepthWarningUI awakeningWarning;
    private bool ownsAwakeningPresentation;
    private Tween awakeningJawTween;
    private DepthBackgroundController hadesBackground;
    [Header("Awakening background")]
    [SerializeField] private Color awakeningLightColor = new Color(.92f,.94f,.98f,1f);
    [SerializeField] private Color hadesBattleBackgroundColor = new Color(.18f,.20f,.26f,1f);
    [SerializeField, Range(1,4)] private int awakeningBackgroundFlashes = 2;
    [SerializeField, Min(.1f)] private float awakeningBackgroundStep = .18f;
    [Header("Awakening")]
    [SerializeField] private Sprite[] wakeFrames;
    [SerializeField, Min(0.05f)] private float wakeFrameDuration = 0.45f;
    [Header("Opening horizontal sweep - bottom to top")]
    [SerializeField, Range(1,12)] private int openingSweepRows = 8;
    [SerializeField, Min(0.05f)] private float openingSweepRowInterval = 0.2f;
    private readonly Coroutine[] openingRowRoutines = new Coroutine[12];
    private readonly bool[] openingRowsFinished = new bool[12];
    [Header("Aimed wave - direction locked at launch")]
    [SerializeField] private GameObject mouthOrbPrefab;
    [SerializeField] private GameObject missilePrefab;
    [SerializeField, Min(1)] private int missilePoolSize = 5;
    [SerializeField, Min(0.1f)] private float missileSpeedCells = 7f;
    [SerializeField, Min(1f)] private float missileRangeCells = 12f;
    [SerializeField, Min(1)] private int missileAttack = 100;
    private bool awakened;
    private SpriteRenderer wakeRenderer;
    private SpriteRenderer headRenderer, jawRenderer;
    private bool headWasEnabled, jawWasEnabled;
    private SpriteRenderer mouthOrb;
    private Animator mouthOrbAnimator;
    [SerializeField, Min(0.1f)] private float missileVolleyDuration = 4f;
    [SerializeField, Range(0.2f,5f)] private float missileFireInterval = 0.45f;
    [SerializeField, Min(0.01f)] private float mouthOrbFadeTime = 0.25f;
    [SerializeField, Min(1f)] private float missileJawTravelMultiplier = 6f;
    [SerializeField, Range(0f,0.4f)] private float mouthOrbDownRatio = 0.13f;
    private HadesWaveProjectile[] missiles;
    public bool IsAwakened => awakened;
    private bool presentationReady;
    private Vector3 visualOriginalScale, headRest, jawRest;
    [SerializeField, Min(0.1f)] private float tellDurationMultiplier = 2f;
    [SerializeField, Min(0f)] private float tellHoldTime = 0.12f;
    [SerializeField] private Color hitTint = new Color(1f,0.45f,0.55f,1f);
    [SerializeField, Range(0f,1f)] private float hitTintAmount = 0.85f;
    private Transform headBreathingRoot;
    private Vector3 headBreathingPosition, headBreathingScale;
    private Color[] bodyRestColors;
    private Sequence tellSequence;


    // =========================================================
    // Idle
    // =========================================================

    [Header("Idle")]

    [SerializeField]
    private float idleMoveY = 0.05f;

    [SerializeField]
    private float idleDuration = 0.7f;


    // =========================================================
    // Hit
    // =========================================================

    [Header("Hit")]

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
    private BossSpikeWarning spikeWarningPrefab;

    [SerializeField]
    private BossSpikeStrike spikeStrikePrefab;


    // =========================================================
    // Death
    // =========================================================

    [Header("Death")]







    // =========================================================
    // Weapon Drop
    // =========================================================

    [Header("Weapon Drop")]

    [SerializeField]
    private WeaponData weaponDrop;
    [SerializeField] private bool forceWeaponDropForTesting;
    [SerializeField, Min(0f)] private float weaponDropRiseCells = 2f;

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

    [Header("Hades pattern selection")]
    [SerializeField] private float[] patternWeights = {1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f};
    [SerializeField] private float[] warningMultipliers = {1f, 1.15f, 1.1f, 1f, 1.25f};
    [SerializeField, Min(0.01f)] private float cellStrikeInterval = 0.055f;
    [SerializeField, Min(0f)] private float delayedRetargetGap = 0.25f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private bool forceDebugPattern;
    [SerializeField] private HadesSpikePattern debugPattern;
#endif
    [Header("Accelerating single-cell pursuit")]
    [SerializeField] private float[] pursuitCadence = {1f, 0.8f, 0.5f};
    [SerializeField, Range(3,8)] private int pursuitMinShots = 4;
    [SerializeField, Range(3,8)] private int pursuitMaxShots = 6;
    private Tilemap arenaWalls;
    private readonly List<Vector3Int> forkFollowupCells = new List<Vector3Int>(8);
    private readonly List<Vector3Int> spikeCells = new List<Vector3Int>(32);
    private readonly HashSet<Vector3Int> damagedSpikeCells = new HashSet<Vector3Int>();
    private readonly List<Vector3Int> dangerCells = new List<Vector3Int>(32);
    private readonly HashSet<EnemyBase> patternHitEnemies = new HashSet<EnemyBase>();
    private readonly List<BossSpikeWarning> markers = new List<BossSpikeWarning>(32);
    private GameObject patternRoot;
    private int lastPattern = -1;
    private bool playerHitThisWave;
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
        bodyRestColors = new Color[bodyRenderers.Length];
        for(int i=0;i<bodyRenderers.Length;i++) {
            Color color=bodyRenderers[i]!=null?bodyRenderers[i].color:Color.white;
            color.a=1f;bodyRestColors[i]=color;
        }
        SetAllBodyAlpha(1f);


        if (visualRoot != null) {
            SetupPresentation(manager,anchorCell);
            visualOriginalLocalPosition =
                visualRoot.localPosition;

            visualOriginalLocalRotation =
                visualRoot.localRotation;

            SetupSleepingHead();
        }
    }

    public override bool TakeDamage(int attack, out int damage) {
        damage = 0;
        if (!awakened) return false;
        bool killed = base.TakeDamage(attack,out damage);
        HealthChanged?.Invoke();
        return killed;
    }

    private void SetupSleepingHead() {
        awakened = false;
        EnsureMouthOrb();
        ConfigureBossSorting();
        HideMouthOrb();
        if (headPart == null || wakeFrames == null || wakeFrames.Length == 0) {
            Debug.LogError("[Hades] Wake frame references are missing.", this);
            return;
        }
        headRenderer = headPart.GetComponent<SpriteRenderer>();
        jawRenderer = jawPart != null ? jawPart.GetComponent<SpriteRenderer>() : null;
        if (headRenderer == null || headRenderer.sprite == null) return;
        headWasEnabled = headRenderer.enabled;
        jawWasEnabled = jawRenderer != null;
        var obj = new GameObject("HadesSleepingHead");
        obj.transform.SetParent(headPart, false);
        wakeRenderer = obj.AddComponent<SpriteRenderer>();
        wakeRenderer.sharedMaterial = headRenderer.sharedMaterial;
        wakeRenderer.sortingLayerID = headRenderer.sortingLayerID;
        wakeRenderer.sortingOrder = headRenderer.sortingOrder;
        wakeRenderer.sprite = wakeFrames[0];
        Bounds original = headRenderer.sprite.bounds;
        Bounds frame = wakeFrames[0].bounds;
        float fit = original.size.x / Mathf.Max(0.001f, frame.size.x);
        obj.transform.localScale = Vector3.one * fit;
        // Same sheet width and top anchor; never refit the Grid or boss footprint.
        obj.transform.localPosition = new Vector3(original.center.x-frame.center.x*fit,
            original.max.y-frame.max.y*fit, 0f);
        headRenderer.enabled = false;
        if (jawRenderer != null) jawRenderer.enabled = jawWasEnabled;
    }

    private IEnumerator WakeAndAttack() {
        while (CanAttack && (player.CurrentCell.x < arenaLeftX || player.CurrentCell.x >= arenaLeftX+arenaWidth ||
            Mathf.Abs(player.CurrentCell.y-(Cell.y+FootprintSize.y)) > spikeRangeCells+2)) yield return null;
        if (!CanAttack) yield break;
        ownsAwakeningPresentation=true;
        player.RequestBossPresentationLock();
        while(CanAttack && !player.IsReadyForBossPresentation)yield return null;
        if(!CanAttack){ReleaseAwakeningPresentation();yield break;}
        player.BeginBossPresentationLock();
        awakeningCamera=FindObjectOfType<CameraFollowTarget>();
        awakeningWarning=FindObjectOfType<DepthWarningUI>();
        hadesBackground=FindObjectOfType<DepthBackgroundController>();
        if(hadesBackground!=null)hadesBackground.BeginHadesBackground(awakeningLightColor,
            hadesBattleBackgroundColor,awakeningBackgroundFlashes,awakeningBackgroundStep);
        if(awakeningCamera!=null) {
            awakeningCamera.FocusBoss(ArtBounds().center,awakeningCameraDuration);
            yield return new WaitForSeconds(awakeningCameraDuration);
        }
        if(!CanAttack){ReleaseAwakeningPresentation();yield break;}
        if (wakeRenderer != null) {
            for (int i=0; i<wakeFrames.Length && CanAttack; i++) {
                wakeRenderer.sprite = wakeFrames[i];
                if(jawPart!=null) {
                    awakeningJawTween?.Kill();
                    float remaining=1f-(i+1f)/wakeFrames.Length;
                    Vector3 offset=new Vector3((i%2==0?-1f:1f)*jawTravel*.2f*remaining,-jawTravel*.4f*remaining,0f);
                    awakeningJawTween=jawPart.DOLocalMove(jawRest+offset,Mathf.Max(.05f,wakeFrameDuration)).SetEase(Ease.OutBack);
                }
                yield return new WaitForSeconds(Mathf.Max(0.05f,wakeFrameDuration));
            }
            awakeningJawTween?.Kill();
            if(jawPart!=null)jawPart.localPosition=jawRest;
            wakeRenderer.enabled = false;
            headRenderer.enabled = headWasEnabled;
            if (jawRenderer != null) jawRenderer.enabled = jawWasEnabled;
        }
        if (!CanAttack) yield break;
        awakened = true;
        if (bossHUD == null && bossHealthFrame != null) {
            bossHUD = new GameObject("HadesBossHUD").AddComponent<HadesBossHUD>();
            bossHUD.Bind(this,bossHealthFrame,bossNameFont);
        }
        PlayIdleMotion();
        if(awakeningWarning!=null) {
            float messageTime=awakeningWarning.ShowBossMessage("\uC800\uC2B9\uC758 \uC2E0 \uD558\uB370\uC2A4\uAC00 \uAE68\uC5B4\uB0AC\uC2B5\uB2C8\uB2E4",awakeningMessageFont);
            if(messageTime>0f)yield return new WaitForSeconds(messageTime);
        }
        if(!CanAttack){ReleaseAwakeningPresentation();yield break;}
        if(awakeningCamera!=null) {
            awakeningCamera.RestorePlayerFocus(awakeningCameraDuration);
            yield return new WaitForSeconds(awakeningCameraDuration);
        }
        ReleaseAwakeningPresentation();
        if(!CanAttack)yield break;
        yield return OpeningHorizontalSweep();
        if (CanAttack) yield return AttackLoop();
    }

    private void ReleaseAwakeningPresentation() {
        if(!ownsAwakeningPresentation)return;
        ownsAwakeningPresentation=false;
        if(awakeningWarning!=null)awakeningWarning.CancelBossMessage();
        if(awakeningCamera!=null)awakeningCamera.CancelBossFocus();
        if(player!=null && !player.IsWaitingForBossPresentation)player.EndBossPresentationLock();
    }
    private void ConfigureBossSorting() {
        foreach (var body in bodyRenderers) if (body != null) {
            body.sortingOrder = body.transform == headPart ? 1 : body.transform == jawPart ? 0 : -3;
        }
        if (mouthOrb != null) {
            mouthOrb.sortingLayerID = bodyRenderers[0].sortingLayerID;
            mouthOrb.sortingOrder = 2; // Deterministic order: torso < jaw (0) < head (1) < orb (2).
        }
    }
    private void HideMouthOrb() {
        if (mouthOrb == null) return;
        Color color = mouthOrb.color; color.a = 0f; mouthOrb.color = color;
        mouthOrb.enabled = false;
        if (mouthOrbAnimator != null) mouthOrbAnimator.enabled = false;
    }
    private void EnsureMouthOrb() {
        if (mouthOrb != null || mouthOrbPrefab == null) return;
        var obj = Instantiate(mouthOrbPrefab,headBreathingRoot,false);
        mouthOrb = obj.GetComponent<SpriteRenderer>();
        mouthOrbAnimator = obj.GetComponent<Animator>();
        if (mouthOrb == null) { Destroy(obj); Debug.LogError("[Hades] Mouth orb prefab requires a SpriteRenderer.",this); }
    }
    private void DrawMouthOrb(float age, float alpha) {
        if (mouthOrb == null) return;
        if (!mouthOrb.enabled && mouthOrbAnimator != null) {
            mouthOrbAnimator.enabled = true;
            mouthOrbAnimator.Rebind(); mouthOrbAnimator.Update(0f);
        }
        mouthOrb.enabled = true;
        // Fit the animated prefab inside the opening, behind both sets of teeth.
        if (headRenderer != null && jawRenderer != null && mouthOrb.sprite != null) {
            // User-authored size is fixed at local X/Y = 1, never recomputed per frame.
            mouthOrb.transform.localScale = Vector3.one;
            float gapCenter = (headRenderer.bounds.min.y+jawRenderer.bounds.max.y)*0.5f;
            mouthOrb.transform.position = new Vector3(headRenderer.bounds.center.x,
                gapCenter-headRenderer.bounds.size.y*mouthOrbDownRatio,headPart.position.z);
        }
        Color color = Color.white; color.a = Mathf.Clamp01(alpha); mouthOrb.color = color;
    }
    private IEnumerator FireAimedWave() {
        StopTell();
        EnsureMissilePool();
        HideMouthOrb();
        float orbStart = Time.time;
        if (jawPart != null) {
            tellSequence = DOTween.Sequence();
            tellSequence.Append(jawPart.DOLocalMove(jawRest+Vector3.down*jawTravel*missileJawTravelMultiplier,0.55f).SetEase(Ease.OutSine));
            if (headPart != null) tellSequence.Join(headPart.DOLocalMove(headRest+Vector3.up*jawTravel*0.4f,0.55f));
            // The orb appears while the mouth opens, without an additional object/tween.
            float openingStart = Time.time;
            while (CanAttack && tellSequence != null && tellSequence.IsActive() && !tellSequence.IsComplete()) {
                float age = Time.time-openingStart;
                DrawMouthOrb(age,age/Mathf.Max(0.01f,mouthOrbFadeTime));
                yield return null;
            }
            tellSequence = null;
        }
        float start = Time.time;
        float nextShot = start;
        while (CanAttack && Time.time-start < Mathf.Max(0.1f,missileVolleyDuration)) {
            DrawMouthOrb(Time.time-orbStart,1f);
            if (Time.time >= nextShot) {
                LaunchMouthMissile();
                // Do not burst multiple catch-up shots after a long frame or pause.
                nextShot = Time.time+Mathf.Clamp(missileFireInterval,0.2f,5f);
            }
            yield return null;
        }
        float fadeStart = Time.time;
        while (CanAttack && Time.time-fadeStart < mouthOrbFadeTime) {
            DrawMouthOrb(Time.time-orbStart,1f-(Time.time-fadeStart)/Mathf.Max(0.01f,mouthOrbFadeTime));
            yield return null;
        }
        HideMouthOrb();
        if (jawPart != null && CanAttack) {
            tellSequence = DOTween.Sequence();
            tellSequence.Append(jawPart.DOLocalMove(jawRest,0.4f).SetEase(Ease.InOutSine));
            if (headPart != null) tellSequence.Join(headPart.DOLocalMove(headRest,0.4f));
            yield return tellSequence.WaitForCompletion();
            tellSequence = null;
        }
        RestoreTell();
    }
    private void LaunchMouthMissile() {
        if (missiles == null || mouthOrb == null) return;
        foreach (var missile in missiles) if (!missile.gameObject.activeSelf) {
            Vector3 center = enemyManager.GetCellCenterWorld(player.CurrentCell);
            float cellSize = (enemyManager.GetCellCenterWorld(player.CurrentCell+Vector3Int.right)-center).magnitude;
            missile.Launch(mouthOrb.bounds.center, player.transform.position-mouthOrb.bounds.center, cellSize,
                missileSpeedCells, missileRangeCells,PatternAttack(missileAttack),player,playerStatus,arenaWalls,enemyManager,this);
            if (AudioManager.Instance != null && spikeAttackSFX != null) AudioManager.Instance.PlaySFX(spikeAttackSFX);
            break;
        }
    }

    private void EnsureMissilePool() {
        if (missiles != null) return;
        if (missilePrefab == null || mouthOrb == null) {
            Debug.LogError("[Hades] Assign boss_ball_0 and hadesAttackBall prefabs.",this); return;
        }
        int capacity = Mathf.Max(missilePoolSize,Mathf.CeilToInt(
            (missileRangeCells/Mathf.Max(0.1f,missileSpeedCells)+0.18f)/Mathf.Clamp(missileFireInterval,0.2f,5f))+1);
        missiles = new HadesWaveProjectile[Mathf.Clamp(capacity,1,16)];
        for (int i=0; i<missiles.Length; i++) {
            var obj = Instantiate(missilePrefab);
            // Keep the authored WORLD size (2.5), rather than multiplying it by the boss scale.
            obj.transform.SetParent(transform,true);
            var sprite = obj.GetComponent<SpriteRenderer>();
            sprite.sortingLayerID = bodyRenderers[0].sortingLayerID;
            sprite.sortingOrder = 2;
            missiles[i] = obj.GetComponent<HadesWaveProjectile>();
            if (missiles[i] == null) missiles[i] = obj.AddComponent<HadesWaveProjectile>();
            missiles[i].Prepare();
            obj.SetActive(false);
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
        int arenaWidth, Tilemap arenaWalls = null) {
        if (bossActivated)
            return;
        if (player == null || player.Status == null || player.Status.IsDead) {
            Debug.LogError("[Hades] Active scene player status is required.",this);
            return;
        }


        this.arenaWalls = arenaWalls;
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
                player.Status;
            if (playerStatus != null) playerStatus.OnPlayerDied += StopAttacks;
        }


        bossActivated =
            true;


        attackRoutine =
            StartCoroutine(
                WakeAndAttack());


        Debug.Log(
            $"[Boss] Activated : {name}");
    }

    

    // =========================================================
    // Idle
    // =========================================================

    private void SetupPresentation(EnemyManager manager, Vector3Int anchor) {
        if(presentationReady)return;
        presentationReady=true;
        if(headPart!=null)headRest=headPart.localPosition;
        if(jawPart!=null)jawRest=jawPart.localPosition;
        // Keep every art part together, but keep the Grid root and health bar independent.
        Transform group=new GameObject("HadesPresentation").transform;
        group.SetParent(transform,false);
        Transform oldRoot=visualRoot;
        oldRoot.SetParent(group,true);
        headBreathingRoot=oldRoot;
        headBreathingPosition=oldRoot.localPosition;
        headBreathingScale=oldRoot.localScale;
        foreach(var body in bodyRenderers) {
            if(body!=null && !body.transform.IsChildOf(group))body.transform.SetParent(group,true);
        }
        visualRoot=group;
        Bounds bounds=ArtBounds();
        Vector3 center=manager.GetCellCenterWorld(anchor);
        float cellWidth=(manager.GetCellCenterWorld(anchor+Vector3Int.right)-center).magnitude;
        float cellHeight=(manager.GetCellCenterWorld(anchor+Vector3Int.up)-center).magnitude;
        float targetWidth=cellWidth * FootprintSize.x * arenaWidthFill;
        if(bounds.size.x>0.001f)group.localScale *= targetWidth/bounds.size.x;
        bounds=ArtBounds();
        Vector3 shift=new Vector3(transform.position.x-bounds.center.x,transform.position.y-(FootprintSize.y-1)*cellHeight*0.5f+cellHeight*visualLiftCells-bounds.min.y,0f);
        group.localPosition+=transform.InverseTransformVector(shift);
        visualOriginalScale=group.localScale;
        if(presentationHealthBarAnchor!=null) {
            bounds=ArtBounds();
            presentationHealthBarAnchor.position=new Vector3(bounds.center.x,bounds.max.y+cellHeight*0.15f,presentationHealthBarAnchor.position.z);
        }
    }
    private Bounds ArtBounds() {
        Bounds bounds=new Bounds(transform.position,Vector3.zero);bool first=true;
        foreach(var body in bodyRenderers) if(body!=null) {
            if(first){bounds=body.bounds;first=false;}else bounds.Encapsulate(body.bounds);
        }
        return bounds;
    }
    private void PlayIdleMotion() {
        if(visualRoot==null)return;
        idleMoveTween?.Kill();idleRotateTween?.Kill();
        visualRoot.localPosition=visualOriginalLocalPosition;
        visualRoot.localRotation=visualOriginalLocalRotation;
        visualRoot.localScale=visualOriginalScale;
        if(headBreathingRoot==null)return;
        headBreathingRoot.localPosition=headBreathingPosition;
        headBreathingRoot.localScale=headBreathingScale;
        idleMoveTween=headBreathingRoot.DOLocalMoveY(headBreathingPosition.y+idleMoveY,idleDuration)
            .SetEase(Ease.InOutSine).SetLoops(-1,LoopType.Yoyo);
        Vector3 inhale=headBreathingScale;
        inhale.y*=1f+breathScaleY;
        idleRotateTween=headBreathingRoot.DOScale(inhale,idleDuration).SetEase(Ease.InOutSine).SetLoops(-1,LoopType.Yoyo);
    }
    private void RestoreTell() {
        if(headPart!=null)headPart.localPosition=headRest;
        if(jawPart!=null)jawPart.localPosition=jawRest;
    }
    private void StopTell() {tellSequence?.Kill();tellSequence=null;RestoreTell();}
    private IEnumerator PlayBossTell(HadesSpikePattern kind) {
        StopTell();
        if(jawPart==null)yield break;
        int pulses=kind==HadesSpikePattern.AcceleratingHunt?3:
            kind==HadesSpikePattern.Fork||kind==HadesSpikePattern.DelayedHunt?2:1;
        float beat=kind==HadesSpikePattern.AcceleratingHunt?0.055f:kind==HadesSpikePattern.SafeLane?0.16f:0.1f;
        beat*=Mathf.Max(0.1f,tellDurationMultiplier);
        float depth=jawTravel*(kind==HadesSpikePattern.SafeLane?1.6f:kind==HadesSpikePattern.Line?0.75f:1f);
        tellSequence=DOTween.Sequence();
        for(int i=0;i<pulses;i++) {
            tellSequence.Append(jawPart.DOLocalMove(jawRest+Vector3.down*depth,beat).SetEase(Ease.OutSine));
            if(headPart!=null)tellSequence.Join(headPart.DOLocalMove(headRest+Vector3.up*depth*0.25f,beat).SetEase(Ease.OutSine));
            float hold=kind==HadesSpikePattern.SafeLane?0.24f:kind==HadesSpikePattern.DelayedHunt?0.16f:kind==HadesSpikePattern.AcceleratingHunt?0.06f:tellHoldTime;
            tellSequence.AppendInterval(hold);
            tellSequence.Append(jawPart.DOLocalMove(jawRest,beat).SetEase(Ease.InOutSine));
            if(headPart!=null)tellSequence.Join(headPart.DOLocalMove(headRest,beat).SetEase(Ease.InOutSine));
            if(kind==HadesSpikePattern.Sweep && headPart!=null) {
                tellSequence.Append(headPart.DOLocalMove(headRest+Vector3.right*jawTravel*0.6f,0.1f*tellDurationMultiplier));
                tellSequence.Append(headPart.DOLocalMove(headRest,0.1f*tellDurationMultiplier));
            }
        }
        yield return tellSequence.WaitForCompletion();
        tellSequence=null;RestoreTell();
    }

    private void RestoreBodyColors() {
        if(bodyRestColors==null)return;
        for(int i=0;i<bodyRenderers.Length;i++)
            if(bodyRenderers[i]!=null)bodyRenderers[i].color=bodyRestColors[i];
    }
    private void AppendHitTint(Sequence sequence,float amount,float duration) {
        bool first=true;
        for(int i=0;i<bodyRenderers.Length;i++) {
            var renderer=bodyRenderers[i];if(renderer==null)continue;
            Color color=Color.Lerp(bodyRestColors[i],hitTint,amount);color.a=1f;
            Tween tween=renderer.DOColor(color,duration).SetEase(Ease.OutQuad);
            if(first){sequence.Append(tween);first=false;}else sequence.Join(tween);
        }
    }
    protected override void PlayHitAnimation() {
        if(bodyRenderers==null || bodyRestColors==null)return;
        hitSequence?.Kill();KillAllBodyTweens();RestoreBodyColors();
        hitSequence=DOTween.Sequence();
        AppendHitTint(hitSequence,hitTintAmount,0.035f);
        AppendHitTint(hitSequence,0f,hitFadeTime);
    }

    private IEnumerator AttackLoop() {
        while (!IsDead &&
               bossActivated) {
            float waitTime =
                Random.Range(
                    attackIntervalMin,
                    attackIntervalMax);


            yield return new WaitForSeconds(
                waitTime/PatternSpeed);


            if (IsDead || !bossActivated || playerStatus == null || playerStatus.IsDead)
                yield break;


            yield return SpikeAttackRoutine();
        }
    }


    // =========================================================
    // Spike Attack
    // =========================================================

    private bool CanAttack => !IsDead && bossActivated && player != null && playerStatus != null && !playerStatus.IsDead;

    private HadesSpikePattern ChoosePattern() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (forceDebugPattern) return debugPattern;
#endif
        int count = 8;
        float hp = CurrentHP / (float)Mathf.Max(1,MaxHP);
        float total = 0f;
        for (int i = 0; i < count; i++) if (i != lastPattern && HadesSpikePatterns.IsAvailable((HadesSpikePattern)i,hp)) total += Weight(i);
        if (total <= 0f) return HadesSpikePattern.Line;
        float value = Random.value * total;
        for (int i = 0; i < count; i++) {
            if (i == lastPattern || !HadesSpikePatterns.IsAvailable((HadesSpikePattern)i,hp)) continue;
            value -= Weight(i);
            if (value < 0f) return (HadesSpikePattern)i;
        }
        return HadesSpikePattern.Line;
    }
    private float Weight(int index) => patternWeights != null && index < patternWeights.Length ? Mathf.Max(0f,patternWeights[index]) : 1f;
    private bool FreeEscapeCell(Vector3Int cell) {
        return cell.x >= arenaLeftX && cell.x < arenaLeftX + arenaWidth &&
            (arenaWalls == null || !arenaWalls.HasTile(cell)) &&
            (terrainManager == null || !terrainManager.HasTerrain(cell)) && !enemyManager.HasEnemy(cell);
    }
    private IEnumerator WaitForDodgeWindow() {
        float until = Time.time + 0.35f;
        // Do not let held attacks starve the boss forever; the subsequent full
        // warning exceeds ordinary attack/movement locks. Dash is invincible.
        while (CanAttack && (Time.timeScale <= 0f || (player.IsActionInProgress && Time.time < until)))
            yield return null;
    }
    private IEnumerator SpikeAttackRoutine() {
        yield return WaitForDodgeWindow();
        if (!CanAttack || spikeWarningPrefab == null || spikeStrikePrefab == null) yield break;
        HadesSpikePattern kind = ChoosePattern();
        lastPattern = (int)kind;
        if (kind == HadesSpikePattern.AimedWave) { yield return FireAimedWave(); yield break; }
        yield return PlayBossTell(kind);
        if(!CanAttack)yield break;
        if (kind == HadesSpikePattern.Horizontal) { yield return HorizontalSpikeWave(); yield break; }
        if (kind == HadesSpikePattern.AcceleratingHunt) {
            yield return AcceleratingPursuit();
            yield break;
        }
        yield return SpikeWave(kind);
        if (kind == HadesSpikePattern.Fork && CanAttack && forkFollowupCells.Count > 0)
            yield return SpikeWave(kind,true);
        if (kind == HadesSpikePattern.DelayedHunt && CanAttack) {
            yield return new WaitForSeconds(delayedRetargetGap);
            yield return WaitForDodgeWindow();
            if (CanAttack) yield return SpikeWave(kind); // New aim, complete new warning.
        }
    }
    private void ConfigureWarning(BossSpikeWarning marker, Vector3Int cell) {
        Vector3 c = enemyManager.GetCellCenterWorld(cell);
        marker.ConfigureCell(enemyManager.GetCellCenterWorld(cell+Vector3Int.right)-c,
            enemyManager.GetCellCenterWorld(cell+Vector3Int.up)-c);
    }
    private IEnumerator AcceleratingPursuit() {
        CleanupPattern();
        patternRoot = new GameObject("HadesAcceleratingPursuit");
        int minShots = Mathf.Clamp(pursuitMinShots,3,8);
        int shots = Random.Range(minShots,Mathf.Clamp(pursuitMaxShots,minShots,8)+1);
        for (int i=0;i<shots && CanAttack;i++) {
            Vector3Int cell = player.CurrentCell;
            if (cell.x<arenaLeftX || cell.x>=arenaLeftX+arenaWidth ||
                (arenaWalls!=null && arenaWalls.HasTile(cell)) ||
                (enemyManager.TryGetEnemy(cell,out EnemyBase occupant) && occupant==this)) break;
            dangerCells.Clear();
            for(int h=0;h<3;h++)dangerCells.Add(cell+Vector3Int.up*h);
            if (!HadesSpikePatterns.HasEscape(cell,dangerCells,FreeEscapeCell)) break;
            float cadence = pursuitCadence != null && pursuitCadence.Length>0 ? pursuitCadence[Mathf.Min(i,pursuitCadence.Length-1)] : 0.5f;
            float warning = Mathf.Max(0.35f,cadence-spikeStrikePrefab.HitDelay/PatternSpeed);
            var marker = Instantiate(spikeWarningPrefab,enemyManager.GetCellCenterWorld(cell),Quaternion.identity,patternRoot.transform);
            ConfigureWarning(marker,cell);marker.Play(warning,false);
            yield return new WaitForSeconds(warning);
            if (!CanAttack) break;
            if (!HadesSpikePatterns.HasEscape(player.CurrentCell,dangerCells,FreeEscapeCell)) break;
            bool hit=false;
            patternHitEnemies.Clear();damagedSpikeCells.Clear();playerHitThisWave=false;
            float cellHeight=(enemyManager.GetCellCenterWorld(cell+Vector3Int.up)-enemyManager.GetCellCenterWorld(cell)).magnitude;
            var spike=Instantiate(spikeStrikePrefab,enemyManager.GetCellCenterWorld(cell)+Vector3.down*(cellHeight*0.5f),Quaternion.identity,patternRoot.transform);
            if(AudioManager.Instance!=null)AudioManager.Instance.PlaySFX(spikeAttackSFX);
            spike.SetPhaseSpeed(PatternSpeed);
            spike.PlayCell(cellHeight,()=>{
                hit=true;
                if(!CanAttack)return;
                if(marker!=null){marker.gameObject.SetActive(false);Destroy(marker.gameObject);}
                ApplySpikeDamage(cell,patternHitEnemies);
            });
            while(CanAttack && !hit)yield return null;
            // Read the player's NEW position for the next warning, never move an existing warning.
        }
        if(CanAttack)yield return new WaitForSeconds(spikeStrikePrefab.Lifetime/PatternSpeed);
        CleanupPattern();
    }
    private void StopOpeningSweep() {
        for(int i=0;i<openingRowRoutines.Length;i++) {
            if(openingRowRoutines[i]!=null)StopCoroutine(openingRowRoutines[i]);
            openingRowRoutines[i]=null;
            openingRowsFinished[i]=true;
        }
    }
    private IEnumerator OpeningHorizontalSweep() {
        if(!CanAttack || spikeWarningPrefab==null || spikeStrikePrefab==null)yield break;
        StopOpeningSweep();CleanupPattern();
        patternRoot=new GameObject("HadesOpeningSweep");
        patternHitEnemies.Clear();damagedSpikeCells.Clear();playerHitThisWave=false;
        int rows=Mathf.Clamp(openingSweepRows,1,openingRowRoutines.Length);
        float interval=Mathf.Max(.05f,openingSweepRowInterval);
        int firstRow=Cell.y+FootprintSize.y;
        for(int i=0;i<rows;i++) {
            openingRowsFinished[i]=false;
            openingRowRoutines[i]=StartCoroutine(RunOpeningRow(firstRow+i,i*interval,i));
        }
        // Join actual row completion; frame rounding must not truncate the last retraction.
        while(CanAttack) {
            bool pending=false;
            for(int i=0;i<rows;i++)if(!openingRowsFinished[i]){pending=true;break;}
            if(!pending)break;
            yield return null;
        }
        StopOpeningSweep();CleanupPattern();
    }
    private IEnumerator RunOpeningRow(int row,float delay,int index) {
        try {yield return OpeningHorizontalRow(row,delay);}
        finally {openingRowsFinished[index]=true;}
    }
    private IEnumerator OpeningHorizontalRow(int row,float delay) {
        if(delay>0f)yield return new WaitForSeconds(delay);
        if(!CanAttack || patternRoot==null)yield break;
        // Opening only: independent row geometry prevents staggered callbacks from
        // overwriting the regular pattern's shared lists. At most 12 rows exist.
        var cells=new List<Vector3Int>(2);
        var danger=new List<Vector3Int>(6);
        HadesSpikePatterns.BuildHorizontal(arenaLeftX,arenaWidth,row,cells,danger);
        cells.RemoveAll(cell=>!HorizontalBaseValid(cell));
        danger.Clear();
        foreach(var cell in cells)for(int i=0;i<3;i++)
            danger.Add(cell+new Vector3Int((cell.x==arenaLeftX?1:-1)*i,0,0));
        if(!HadesSpikePatterns.ReserveEscape(player.CurrentCell,danger,FreeEscapeCell))yield break;
        cells.RemoveAll(cell=>!danger.Contains(cell) ||
            !danger.Contains(cell+new Vector3Int(cell.x==arenaLeftX?1:-1,0,0)) ||
            !danger.Contains(cell+new Vector3Int(cell.x==arenaLeftX?2:-2,0,0)));
        if(cells.Count==0)yield break;
        danger.Clear();
        foreach(var cell in cells)for(int i=0;i<3;i++)
            danger.Add(cell+new Vector3Int((cell.x==arenaLeftX?1:-1)*i,0,0));
        var rowRoot=new GameObject("OpeningRow"+row);
        rowRoot.transform.SetParent(patternRoot.transform,false);
        var rowMarkers=new BossSpikeWarning[cells.Count];
        float warning=Mathf.Max(.8f,warningDuration);
        for(int n=0;n<cells.Count;n++) {
            Vector3Int cell=cells[n];Vector3 center=enemyManager.GetCellCenterWorld(cell);
            var marker=Instantiate(spikeWarningPrefab,center,Quaternion.identity,rowRoot.transform);
            marker.ConfigureHorizontal(enemyManager.GetCellCenterWorld(cell+Vector3Int.right)-center,
                enemyManager.GetCellCenterWorld(cell+Vector3Int.up)-center,cell.x==arenaLeftX?1:-1);
            marker.Play(warning,false);rowMarkers[n]=marker;
        }
        yield return new WaitForSeconds(warning);
        if(!CanAttack || rowRoot==null)yield break;
        if(!HadesSpikePatterns.HasEscape(player.CurrentCell,danger,FreeEscapeCell)) {Destroy(rowRoot);yield break;}
        if(AudioManager.Instance!=null)AudioManager.Instance.PlaySFX(spikeAttackSFX);
        float speed=PatternSpeed;
        for(int n=0;n<cells.Count;n++) {
            Vector3Int cell=cells[n];int direction=cell.x==arenaLeftX?1:-1;
            Vector3 center=enemyManager.GetCellCenterWorld(cell);
            float width=(enemyManager.GetCellCenterWorld(cell+Vector3Int.right)-center).magnitude;
            float height=(enemyManager.GetCellCenterWorld(cell+Vector3Int.up)-center).magnitude;
            var marker=rowMarkers[n];
            var spike=Instantiate(spikeStrikePrefab,center+Vector3.left*(width*.5f*direction),Quaternion.identity,rowRoot.transform);
            spike.SetPhaseSpeed(speed);
            spike.PlayHorizontal(width,height,direction,()=>{
                if(!CanAttack)return;
                if(marker!=null){marker.gameObject.SetActive(false);Destroy(marker.gameObject);}
                for(int i=0;i<3 && CanAttack;i++) {
                    Vector3Int target=cell+new Vector3Int(direction*i,0,0);
                    if(damagedSpikeCells.Add(target))ApplySpikeCellDamage(target,patternHitEnemies);
                }
            });
        }
        yield return new WaitForSeconds(spikeStrikePrefab.Lifetime/speed);
        if(rowRoot!=null)Destroy(rowRoot);
    }
    private bool HorizontalBaseValid(Vector3Int cell) {
        int direction=cell.x==arenaLeftX?1:-1;
        for(int i=0;i<3;i++) {
            Vector3Int target=cell+new Vector3Int(direction*i,0,0);
            if((arenaWalls!=null && arenaWalls.HasTile(target)) ||
                (enemyManager.TryGetEnemy(target,out EnemyBase enemy) && enemy==this))return false;
        }
        return true;
    }
    private IEnumerator HorizontalSpikeWave() {
        int row=Mathf.Clamp(player.CurrentCell.y,Cell.y+FootprintSize.y,
            Cell.y+FootprintSize.y+Mathf.Clamp(spikeRangeCells,1,8)-1);
        HadesSpikePatterns.BuildHorizontal(arenaLeftX,arenaWidth,row,spikeCells,dangerCells);
        spikeCells.RemoveAll(cell=>!HorizontalBaseValid(cell));
        dangerCells.Clear();
        foreach(var cell in spikeCells) for(int i=0;i<3;i++)
            dangerCells.Add(cell+new Vector3Int((cell.x==arenaLeftX?1:-1)*i,0,0));
        if(!HadesSpikePatterns.ReserveEscape(player.CurrentCell,dangerCells,FreeEscapeCell))yield break;
        spikeCells.RemoveAll(cell=>!dangerCells.Contains(cell) ||
            !dangerCells.Contains(cell+new Vector3Int((cell.x==arenaLeftX?1:-1),0,0)) ||
            !dangerCells.Contains(cell+new Vector3Int((cell.x==arenaLeftX?2:-2),0,0)));
        dangerCells.Clear();
        foreach(var cell in spikeCells)for(int i=0;i<3;i++)
            dangerCells.Add(cell+new Vector3Int((cell.x==arenaLeftX?1:-1)*i,0,0));
        if(spikeCells.Count==0)yield break;
        CleanupPattern();patternRoot=new GameObject("HadesHorizontalSpikes");
        float warning=Mathf.Max(.8f,warningDuration);
        foreach(var cell in spikeCells) {
            Vector3 center=enemyManager.GetCellCenterWorld(cell);
            var marker=Instantiate(spikeWarningPrefab,center,Quaternion.identity,patternRoot.transform);
            marker.ConfigureHorizontal(enemyManager.GetCellCenterWorld(cell+Vector3Int.right)-center,
                enemyManager.GetCellCenterWorld(cell+Vector3Int.up)-center,cell.x==arenaLeftX?1:-1);
            marker.Play(warning,false);markers.Add(marker);
        }
        yield return new WaitForSeconds(warning);
        if(!CanAttack || !HadesSpikePatterns.HasEscape(player.CurrentCell,dangerCells,FreeEscapeCell)) {CleanupPattern();yield break;}
        patternHitEnemies.Clear();damagedSpikeCells.Clear();playerHitThisWave=false;
        if(AudioManager.Instance!=null)AudioManager.Instance.PlaySFX(spikeAttackSFX);
        for(int n=0;n<spikeCells.Count;n++) {
            Vector3Int cell=spikeCells[n];int direction=cell.x==arenaLeftX?1:-1;
            Vector3 center=enemyManager.GetCellCenterWorld(cell);
            float width=(enemyManager.GetCellCenterWorld(cell+Vector3Int.right)-center).magnitude;
            float height=(enemyManager.GetCellCenterWorld(cell+Vector3Int.up)-center).magnitude;
            var marker=markers[n];
            var spike=Instantiate(spikeStrikePrefab,center+Vector3.left*(width*.5f*direction),Quaternion.identity,patternRoot.transform);
            spike.SetPhaseSpeed(PatternSpeed);
            spike.PlayHorizontal(width,height,direction,()=>{
                if(!CanAttack)return;
                if(marker!=null){marker.gameObject.SetActive(false);Destroy(marker.gameObject);}
                for(int i=0;i<3 && CanAttack;i++) {
                    Vector3Int target=cell+new Vector3Int(direction*i,0,0);
                    if(damagedSpikeCells.Add(target))ApplySpikeCellDamage(target,patternHitEnemies);
                }
            });
        }
        yield return new WaitForSeconds(spikeStrikePrefab.Lifetime/PatternSpeed);
        CleanupPattern();
    }
    private IEnumerator SpikeWave(HadesSpikePattern kind, bool followup = false) {
        if (followup) {
            spikeCells.Clear(); spikeCells.AddRange(forkFollowupCells);
        }
        else {
            if (kind == HadesSpikePattern.Fork) forkFollowupCells.Clear();
            HadesSpikePatterns.Build(kind, arenaLeftX, arenaWidth, Cell.y + FootprintSize.y,
            Mathf.Clamp(spikeRangeCells,1,8), player.CurrentCell, Random.value < 0.5f, spikeCells);
        }
        spikeCells.RemoveAll(cell => (arenaWalls != null && arenaWalls.HasTile(cell)) ||
            (enemyManager.TryGetEnemy(cell,out EnemyBase occupant) && occupant == this));
        dangerCells.Clear();
        foreach(var cell in spikeCells)for(int h=0;h<3;h++)dangerCells.Add(cell+Vector3Int.up*h);
        if (!HadesSpikePatterns.ReserveEscape(player.CurrentCell,dangerCells,FreeEscapeCell)) yield break;
        // Remove an entire spike if its footprint would cover the reserved escape.
        spikeCells.RemoveAll(cell=>!dangerCells.Contains(cell)||!dangerCells.Contains(cell+Vector3Int.up)||!dangerCells.Contains(cell+Vector3Int.up*2));
        if(spikeCells.Count==0)yield break;
        if (kind == HadesSpikePattern.Fork && !followup)
            HadesSpikePatterns.BuildComplement(arenaLeftX,arenaWidth,spikeCells[0].y,spikeCells,forkFollowupCells);
        dangerCells.Clear();
        foreach(var cell in spikeCells)for(int h=0;h<3;h++)dangerCells.Add(cell+Vector3Int.up*h);
        CleanupPattern();
        patternRoot = new GameObject("HadesSpikeWave");
        float multiplier = warningMultipliers != null && (int)kind < warningMultipliers.Length ? warningMultipliers[(int)kind] : 1f;
        float warning = Mathf.Max(0.8f,warningDuration * multiplier);
        float interval = kind == HadesSpikePattern.Sweep ? Mathf.Max(0.01f,cellStrikeInterval) : 0f;
        for (int i = 0; i < spikeCells.Count; i++) {
            var marker = Instantiate(spikeWarningPrefab, enemyManager.GetCellCenterWorld(spikeCells[i]), Quaternion.identity, patternRoot.transform);
            ConfigureWarning(marker,spikeCells[i]);
            marker.Play(warning + i * interval + spikeStrikePrefab.HitDelay/PatternSpeed, false);
            markers.Add(marker);
        }
        yield return new WaitForSeconds(warning);
        if (!CanAttack || !HadesSpikePatterns.HasEscape(player.CurrentCell,dangerCells,FreeEscapeCell)) {
            CleanupPattern(); yield break;
        }
        patternHitEnemies.Clear(); damagedSpikeCells.Clear(); playerHitThisWave = false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(spikeAttackSFX);
        float cellHeight = (enemyManager.GetCellCenterWorld(dangerCells[0]+Vector3Int.up) - enemyManager.GetCellCenterWorld(dangerCells[0])).magnitude;
        bool cameraPlayed = false;
        for (int i = 0; i < spikeCells.Count && CanAttack; i++) {
            Vector3Int cell = spikeCells[i];
            BossSpikeWarning marker = markers[i];
            Vector3 position = enemyManager.GetCellCenterWorld(cell) + Vector3.down*(cellHeight*0.5f);
            var spike = Instantiate(spikeStrikePrefab,position,Quaternion.identity,patternRoot.transform);
            spike.SetPhaseSpeed(PatternSpeed);
            spike.PlayCell(cellHeight, () => {
                if (!CanAttack) return;
                if (marker != null) { marker.gameObject.SetActive(false); Destroy(marker.gameObject); }
                if (!cameraPlayed && CameraShakeController.Instance != null) {
                    cameraPlayed = true;
                    CameraShakeController.Instance.ShakeAttackImpact(kind == HadesSpikePattern.SafeLane ? AttackImpactKind.Break : AttackImpactKind.Enemy);
                }
                ApplySpikeDamage(cell,patternHitEnemies);
            });
            if (interval > 0f) yield return new WaitForSeconds(interval);
        }
        yield return new WaitForSeconds(spikeStrikePrefab.Lifetime/PatternSpeed);
        CleanupPattern();
    }
    private void CleanupPattern() {
        markers.Clear();
        if (patternRoot == null) return;
        patternRoot.SetActive(false);
        foreach (var sprite in patternRoot.GetComponentsInChildren<SpriteRenderer>(true)) sprite.DOKill();
        Destroy(patternRoot); patternRoot = null;
    }
    private void StopAttacks() {
        if(awakeningWarning!=null)awakeningWarning.CancelBossMessage();
        awakeningJawTween?.Kill();
        if(hadesBackground!=null)hadesBackground.EndHadesBackground();
        ReleaseAwakeningPresentation();
        StopOpeningSweep();
        if (bossHUD != null) bossHUD.gameObject.SetActive(false);
        HideMouthOrb();
        if (missiles != null) foreach(var missile in missiles) if(missile != null) missile.gameObject.SetActive(false);
        StopTell();
        bossActivated = false;
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null;
        CleanupPattern();
    }

    private void ApplySpikeDamage(Vector3Int cell, HashSet<EnemyBase> hitEnemies) {
        for(int h=0;h<3 && CanAttack;h++) {
            Vector3Int target=cell+Vector3Int.up*h;
            if((arenaWalls==null||!arenaWalls.HasTile(target)) && damagedSpikeCells.Add(target))
                ApplySpikeCellDamage(target,hitEnemies);
        }
    }
    private void ApplySpikeCellDamage(
        Vector3Int cell,
        HashSet<EnemyBase> hitEnemies) {
        // =====================================================
        // Player
        // =====================================================

        if (player != null &&
            playerStatus != null &&
            !playerHitThisWave && player.CurrentCell == cell) {
            playerHitThisWave = true;
            int damage =
                DamageCalculator.CalculateDamage(
                    PatternAttack(spikeAttack),
                    playerStatus.Defense);


            playerStatus.TakeDamage(
                damage);
            if (!CanAttack) return;
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
                    PatternAttack(spikeAttack));
            }
        }


        // =====================================================
        // Terrain
        // =====================================================

        if (terrainManager != null &&terrainManager.HasTerrain(cell)) {
            terrainManager.DamageTerrain(cell, PatternAttack(spikeAttack));
        }
    }


    // =========================================================
    // Death
    // =========================================================

    protected override void Die() {
        if (IsDead)
            return;


        IsDead = true;
        if(awakeningWarning!=null)awakeningWarning.CancelBossMessage();
        ReleaseAwakeningPresentation();
        StopOpeningSweep();
        HideMouthOrb();
        if (missiles != null) foreach (var missile in missiles) if (missile != null) missile.gameObject.SetActive(false);
        RestoreBodyColors();
        Vector3 deathHeadPose=headPart!=null?headPart.localPosition:Vector3.zero;
        Vector3 deathJawPose=jawPart!=null?jawPart.localPosition:Vector3.zero;
        StopTell();
        if(headPart!=null)headPart.localPosition=deathHeadPose;
        if(jawPart!=null)jawPart.localPosition=deathJawPose;
        CleanupPattern();
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


        IsDeathPresentationPlaying = true;
        if (player != null) player.BeginBossPresentationLock();
        HealthChanged?.Invoke();
        StartCoroutine(PetrifyAndFinish());
    }

    private IEnumerator PetrifyAndFinish() {
        // Replace the existing parts in their existing hierarchy. The first stone
        // stage is fitted once to each live part; later crops share its sheet anchor.
        var parts = new SpriteRenderer[3];
        parts[0]=headRenderer; parts[1]=jawRenderer;
        foreach (var body in bodyRenderers) if(body!=headRenderer && body!=jawRenderer) {parts[2]=body;break;}
        var fittedPosition=new Vector3[3];
        var fittedScale=new Vector3[3];
        var referenceSprite=new Sprite[3];
        int[] columns={0,1,3}; // The live prefab has no separate black neck/connector part.
        int stages=stoneFrames!=null?stoneFrames.Length/4:0;
        if (wakeRenderer!=null)wakeRenderer.enabled=false;
        if(stages==0)Debug.LogError("[Hades] Missing registered stone stages.",this);
        for(int i=0;i<3 && stages>0;i++) {
            var part=parts[i];var first=stoneFrames[columns[i]];
            if(part==null || part.sprite==null || first==null)continue;
            Transform t=part.transform;
            Bounds alive=part.sprite.bounds;
            Bounds stone=first.bounds;
            Vector3 oldScale=t.localScale;
            fittedScale[i]=new Vector3(oldScale.x*alive.size.x/Mathf.Max(.001f,stone.size.x),
                oldScale.y*alive.size.y/Mathf.Max(.001f,stone.size.y),oldScale.z);
            fittedPosition[i]=t.localPosition+t.localRotation*Vector3.Scale(alive.center,oldScale)
                -t.localRotation*Vector3.Scale(stone.center,fittedScale[i]);
            referenceSprite[i]=first;
        }
        for(int stage=0;stage<stages;stage++) {
            for(int i=0;i<3;i++) {
                var part=parts[i];Sprite first=referenceSprite[i];
                Sprite sprite=stoneFrames[stage*4+columns[i]];
                if(part==null || first==null || sprite==null)continue;
                Vector2 firstAnchor=first.rect.position+first.pivot;
                Vector2 anchor=sprite.rect.position+sprite.pivot;
                Vector3 delta=new Vector3((anchor.x-firstAnchor.x)/first.pixelsPerUnit,
                    (anchor.y-firstAnchor.y)/first.pixelsPerUnit,0f);
                part.sprite=sprite;part.enabled=true;
                part.transform.localScale=fittedScale[i];
                part.transform.localPosition=fittedPosition[i]+part.transform.localRotation*Vector3.Scale(delta,fittedScale[i]);
                part.sortingOrder=i==0?1:i==1?0:-3;
            }
            yield return new WaitForSeconds(stoneFrameDuration);
        }
        IsDeathPresentationPlaying=false;
        Destroy(gameObject);
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

        if (!forceWeaponDropForTesting && WeaponManager.Instance
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
            player,
            weaponDropRiseCells*(enemyManager.GetCellCenterWorld(dropCell+Vector3Int.up)-targetPosition).magnitude);


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
        IsDeathPresentationPlaying = false;
        if (player != null && !player.IsWaitingForBossPresentation) player.EndBossPresentationLock();
        if (bossHUD != null) Destroy(bossHUD.gameObject);
        if (playerStatus != null) playerStatus.OnPlayerDied -= StopAttacks;
        StopAttacks();
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
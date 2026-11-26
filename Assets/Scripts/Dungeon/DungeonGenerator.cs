using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    [System.Serializable]
    public class TerrainSpawnEntry
    {
        public TerrainData data;

        [Min(0f)]
        public float weight = 1f;
    }


    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField]
    private PlayerGridController player;

    [SerializeField]
    private DepthManager depthManager;

    [SerializeField]
    private TerrainManager terrainManager;

    [SerializeField]
    private EnemyManager enemyManager;

    [SerializeField]
    private GridPickupManager pickupManager;


    // =========================================================
    // Wall
    // =========================================================

    [Header("Wall")]

    [SerializeField]
    private Tilemap wallTilemap;

    [SerializeField]
    private TileBase leftWallTile;

    [SerializeField]
    private TileBase rightWallTile;


    // =========================================================
    // Chunk
    // =========================================================

    [Header("Chunk")]

    [SerializeField]
    private int chunkWidth = 7;

    [SerializeField]
    private int chunkHeight = 10;

    [Tooltip("플레이어 시작 위치에서 몇 Depth 아래부터 생성할지")]
    [SerializeField]
    private int generationStartDepth = 1;


    // =========================================================
    // Generation
    // =========================================================

    [Header("Generation")]

    [SerializeField]
    private int initialChunkCount = 3;

    [Tooltip("플레이어보다 몇 Depth 아래까지 미리 생성할지")]
    [SerializeField]
    private int generateAheadDepth = 20;


    // =========================================================
    // Terrain
    // =========================================================

    [Header("Terrain")]

    [SerializeField]
    private List<TerrainSpawnEntry> terrainTable =
        new List<TerrainSpawnEntry>();


    // =========================================================
    // Enemy
    // =========================================================

    [Header("Enemy")]

    [SerializeField]
    private EnemyBase enemy1Prefab;

    [SerializeField]
    private EnemyBase enemy2Prefab;

    [SerializeField]
    private EnemyBase enemy3Prefab;

    [Range(0f, 1f)]
    [SerializeField]
    private float enemySpawnChance = 0.03f;


    // =========================================================
    // Currency
    // =========================================================

    [Header("Currency")]

    [SerializeField]
    private CurrencyPickup[] currencyPrefabs;

    [Range(0f, 1f)]
    [SerializeField]
    private float currencySpawnChance = 0.04f;


    // =========================================================
    // Item
    // =========================================================

    [Header("Item")]

    [SerializeField]
    private SkillItemPickup[] itemPrefabs;

    [Range(0f, 1f)]
    [SerializeField]
    private float itemSpawnChance = 0.02f;


    // =========================================================
    // Boss
    // =========================================================

    [Header("Boss")]

    [SerializeField]
    private int firstBossDepth = 222;

    [SerializeField]
    private BossEnemy firstBossPrefab;

    [Tooltip("Boss Depth보다 몇 칸 전에 공격 패턴을 활성화할지")]
    [SerializeField]
    private int bossActivationLeadDepth = 3;

    // =========================================================
    // Second Boss
    // =========================================================

    [Header("Second Boss")]

    [SerializeField]
    private int secondBossDepth = 100;

    [SerializeField]
    private BossExplosionEnemy secondBossPrefab;

    [Tooltip("두 번째 보스 위 몇 칸 전에 공격 시작")]
    [SerializeField]
    private int secondBossActivationLeadDepth = 11;

    // =========================================================
    // Runtime
    // =========================================================

    private int startCellX;
    private int startCellY;

    private int lastGeneratedChunk = -1;


    // Boss Runtime
    private bool firstBossChunkPrepared;
    private bool firstBossSpawned;
    private bool firstBossActivated;

    private Vector3Int firstBossAnchorCell;

    private BossEnemy firstBossInstance;

    private int firstBossArenaLeftX;
    private int firstBossArenaWidth;

    private int firstBossTopDepth;

    // Second Boss Runtime
    private bool secondBossChunkPrepared;
    private bool secondBossSpawned;
    private bool secondBossActivated;

    private Vector3Int secondBossAnchorCell;

    private BossExplosionEnemy secondBossInstance;

    private int secondBossArenaLeftX;
    private int secondBossArenaWidth;

    private int secondBossTopDepth;

    // =========================================================
    // Unity
    // =========================================================

    private IEnumerator Start() {
        // PlayerGridController.Start가 먼저 끝난 뒤
        // CurrentCell / StartCellY 사용
        yield return null;

        InitializeGenerator();
    }


    private void OnDestroy() {
        if (depthManager != null) {
            depthManager.OnDepthChanged -=
                HandleDepthChanged;
        }
    }


    // =========================================================
    // Initialize
    // =========================================================

    private void InitializeGenerator() {
        if (player == null ||
            depthManager == null ||
            terrainManager == null ||
            enemyManager == null) {
            Debug.LogError(
                "[DungeonGenerator] Reference가 연결되지 않았습니다.");

            return;
        }


        startCellX =
            player.CurrentCell.x;

        startCellY =
            player.StartCellY;


        // =============================
        // 초기 Chunk 생성
        // =============================

        for (int i = 0;
             i < initialChunkCount;
             i++) {
            GenerateNextChunk();
        }


        depthManager.OnDepthChanged +=
            HandleDepthChanged;


        HandleDepthChanged(
            depthManager.CurrentDepth);
    }


    // =========================================================
    // Depth
    // =========================================================

    private void HandleDepthChanged(
        int currentDepth) {
        // =============================
        // 아래쪽 Chunk 미리 생성
        // =============================

        int targetDepth =
            currentDepth
            + generateAheadDepth;


        int targetChunk =
            Mathf.Max(
                0,
                (targetDepth - generationStartDepth)
                / chunkHeight);


        while (lastGeneratedChunk <
               targetChunk) {
            GenerateNextChunk();
        }


        // =============================
        // Boss 활성화
        // =============================

        if (firstBossSpawned &&
            !firstBossActivated &&
            firstBossInstance != null) {
            int activationDepth =
                Mathf.Max(
                    0,
                    firstBossDepth
                    - bossActivationLeadDepth);


            if (currentDepth >= activationDepth) {
                Debug.Log("액티베이트보스");
                Debug.Log(currentDepth);
                Debug.Log(activationDepth); 
                ActivateFirstBoss();
            }
        }

        // =============================
        // Second Boss 활성화
        // =============================

        if (secondBossSpawned &&
            !secondBossActivated &&
            secondBossInstance != null) {
            int activationDepth =
                Mathf.Max(
                    0,
                    secondBossTopDepth
                    - secondBossActivationLeadDepth);


            if (currentDepth >= activationDepth) {
                ActivateSecondBoss();
            }
        }
    }


    // =========================================================
    // Chunk
    // =========================================================

    private void GenerateNextChunk() {
        int chunkIndex =
            lastGeneratedChunk + 1;


        int topDepth =
            generationStartDepth
            + chunkIndex * chunkHeight;


        int topY =
            startCellY - topDepth;


        int leftX =
            startCellX
            - chunkWidth / 2;


        HashSet<Vector3Int> reservedCells =
            new HashSet<Vector3Int>();

        bool isFirstBossChunk = ContainsFirstBossDepth(topDepth);

        bool isSecondBossChunk =
            ContainsSecondBossDepth(
                topDepth);

        bool isBossChunk =
            isFirstBossChunk ||
            isSecondBossChunk;

        // =====================================================
        // Boss Chunk
        // =====================================================

        if (isFirstBossChunk) {
            if (!firstBossChunkPrepared) {
                PrepareFirstBossChunk(
                    leftX,
                    topY,
                    reservedCells);
            }
        }
        else if (isSecondBossChunk) {
            if (!secondBossChunkPrepared) {
                PrepareSecondBossChunk(
                    leftX,
                    topY,
                    reservedCells);
            }
        }

        // =====================================================
        // Normal Chunk
        // =====================================================

        else {
            GenerateEnemies(
                leftX,
                topY,
                topDepth,
                reservedCells);


            GenerateCurrencies(
                leftX,
                topY,
                reservedCells);


            GenerateItems(
                leftX,
                topY,
                reservedCells);
        }



        // =====================================================
        // Terrain
        // =====================================================

        for (int row = 0;
             row < chunkHeight;
             row++) {
            int y =
                topY - row;


            for (int column = 0;
                 column < chunkWidth;
                 column++) {
                int x =
                    leftX + column;


                Vector3Int cell =
                    new Vector3Int(
                        x,
                        y,
                        0);


                // Enemy / Pickup / BossArena 등
                if (reservedCells.Contains(cell))
                    continue;


                TerrainData terrain =
                    PickRandomTerrain();


                if (terrain != null) {
                    terrainManager.PlaceTerrain(
                        cell,
                        terrain);
                }
            }


            GenerateWalls(
                leftX,
                y);
        }


        lastGeneratedChunk =
            chunkIndex;

        // =============================
        // Boss Chunk가 방금 만들어졌다면
        // Boss도 지금 같이 Spawn
        // =============================

        if (isFirstBossChunk &&
            firstBossChunkPrepared &&
            !firstBossSpawned) {
            SpawnFirstBoss();
        }


        if (isSecondBossChunk &&
            secondBossChunkPrepared &&
            !secondBossSpawned) {
            SpawnSecondBoss();
        }


        Debug.Log(
            $"[DungeonGenerator] " +
            $"Chunk {chunkIndex} 생성 / " +
            $"Depth {topDepth} ~ " +
            $"{topDepth + chunkHeight - 1}" +
            $"{(isBossChunk ? " / BOSS CHUNK" : "")}");
    }


    // =========================================================
    // Boss Chunk
    // =========================================================

    private bool ContainsFirstBossDepth(
        int topDepth) {
        int bottomDepth =
            topDepth
            + chunkHeight
            - 1;


        return
            firstBossDepth >= topDepth &&
            firstBossDepth <= bottomDepth;
    }

    private bool ContainsSecondBossDepth(
    int topDepth) {
        int bottomDepth =
            topDepth
            + chunkHeight
            - 1;

        return
            secondBossDepth >= topDepth &&
            secondBossDepth <= bottomDepth;
    }
    private void ActivateFirstBoss() {
        if (firstBossActivated)
            return;

        if (firstBossInstance == null)
            return;


        firstBossInstance.ActivateBoss(
            player,
            terrainManager,
            firstBossArenaLeftX,
            firstBossArenaWidth);


        firstBossActivated =
            true;


        Debug.Log(
            $"[Boss] Activated / " +
            $"Boss Depth : {firstBossDepth}");
    }
    private void PrepareFirstBossChunk(
     int leftX,
     int topY,
     HashSet<Vector3Int> reservedCells) {
        if (firstBossPrefab == null) {
            Debug.LogError(
                "[Boss] First Boss Prefab이 없습니다.");

            return;
        }


        Vector2Int bossSize =
            firstBossPrefab.FootprintSize;


        // =============================
        // Chunk 중앙 X
        // =============================

        int centerX =
            leftX
            + chunkWidth / 2;


        // =============================
        // Chunk 가장 아래 Cell
        // =============================

        int chunkBottomY =
            topY
            - (chunkHeight - 1);


        // =============================
        // 보스 아래 Terrain 여유 공간
        // =============================

        int bottomPadding = 1;


        // =============================
        // Boss Anchor
        //
        // Footprint 기준 좌하단
        // =============================

        firstBossAnchorCell =
            new Vector3Int(
                centerX - bossSize.x / 2,
                chunkBottomY + bottomPadding,
                0);

        int bossBottomDepth = firstBossDepth- bottomPadding;
        firstBossTopDepth = bossBottomDepth - (bossSize.y - 1);


        Debug.Log(
            $"[Boss] Top Depth : {firstBossTopDepth}");
        // =============================
        // Boss가 실제로 차지하는
        // 7x7 Cell만 Terrain 생성 제외
        // =============================

        ReserveFootprint(
            firstBossAnchorCell,
            bossSize,
            reservedCells);


        firstBossArenaLeftX =
            leftX;

        firstBossArenaWidth =
            chunkWidth;


        firstBossChunkPrepared =
            true;


        Debug.Log(
            $"[Boss] Arena Prepared / " +
            $"Anchor : {firstBossAnchorCell} / " +
            $"Size : {bossSize} / " +
            $"Bottom Padding : {bottomPadding}");
    }


    private void PrepareSecondBossChunk(
    int leftX,
    int topY,
    HashSet<Vector3Int> reservedCells) {
        if (secondBossPrefab == null) {
            Debug.LogError(
                "[Second Boss] Prefab이 없습니다.");

            return;
        }


        Vector2Int bossSize =
            secondBossPrefab.FootprintSize;


        // Chunk 중앙
        int centerX =
            leftX
            + chunkWidth / 2;


        // Chunk 제일 아래
        int chunkBottomY =
            topY
            - (chunkHeight - 1);


        // 보스 아래 1칸 여유
        int bottomPadding = 1;


        // 좌하단 Anchor
        secondBossAnchorCell =
            new Vector3Int(
                centerX - bossSize.x / 2,
                chunkBottomY + bottomPadding,
                0);


        // 실제 Boss 맨 위 Depth
        int bossBottomDepth =
            secondBossDepth
            - bottomPadding;

        secondBossTopDepth =
            bossBottomDepth
            - (bossSize.y - 1);


        // 보스 영역 Terrain 제외
        ReserveFootprint(
            secondBossAnchorCell,
            bossSize,
            reservedCells);


        secondBossArenaLeftX =
            leftX;

        secondBossArenaWidth =
            chunkWidth;


        secondBossChunkPrepared =
            true;


        Debug.Log(
            $"[Second Boss] Arena Prepared / " +
            $"Anchor : {secondBossAnchorCell} / " +
            $"Size : {bossSize} / " +
            $"Top Depth : {secondBossTopDepth}");
    }

    private void SpawnSecondBoss() {
        if (secondBossSpawned)
            return;


        if (!secondBossChunkPrepared)
            return;


        if (secondBossPrefab == null) {
            Debug.LogError(
                "[Second Boss] Prefab이 없습니다.");

            return;
        }


        EnemyBase spawnedEnemy =
            enemyManager.SpawnEnemy(
                secondBossPrefab,
                secondBossAnchorCell,
                secondBossDepth);


        if (spawnedEnemy == null) {
            Debug.LogError(
                $"[Second Boss] Spawn 실패 / " +
                $"Anchor : {secondBossAnchorCell}");

            return;
        }


        secondBossInstance =
            spawnedEnemy as BossExplosionEnemy;


        if (secondBossInstance == null) {
            Debug.LogError(
                "[Second Boss] BossExplosionEnemy가 아닙니다.");

            Destroy(
                spawnedEnemy.gameObject);

            return;
        }


        secondBossSpawned =
            true;


        Debug.Log(
            $"[Second Boss] Spawn / " +
            $"Depth : {secondBossDepth}");
    }

    private void ActivateSecondBoss() {
        if (secondBossActivated)
            return;


        if (secondBossInstance == null) {
            Debug.LogError(
                "[Second Boss] Instance가 없습니다.");

            return;
        }


        secondBossInstance.ActivateBoss(
            player,
            terrainManager,
            secondBossArenaLeftX,
            secondBossArenaWidth);


        secondBossActivated =
            true;


        Debug.Log(
            $"[Second Boss] Activated / " +
            $"Depth : {secondBossDepth}");
    }

    // =========================================================
    // Boss Spawn
    // =========================================================

    private void SpawnFirstBoss() {
        if (firstBossSpawned)
            return;


        if (!firstBossChunkPrepared)
            return;


        if (firstBossPrefab == null) {
            Debug.LogError(
                "[Boss] First Boss Prefab이 없습니다.");

            return;
        }


        EnemyBase spawnedEnemy =
            enemyManager.SpawnEnemy(
                firstBossPrefab,
                firstBossAnchorCell,
                firstBossDepth);


        if (spawnedEnemy == null) {
            Debug.LogError(
                $"[Boss] Spawn 실패 / " +
                $"Anchor : {firstBossAnchorCell}");

            return;
        }


        // =============================
        // 반드시 Instance 저장
        // =============================

        firstBossInstance =
            spawnedEnemy as BossEnemy;


        if (firstBossInstance == null) {
            Debug.LogError(
                "[Boss] Spawn된 Enemy가 BossEnemy가 아닙니다.");

            Destroy(
                spawnedEnemy.gameObject);

            return;
        }


        firstBossSpawned =
            true;


        Debug.Log(
            $"[Boss] First Boss Spawn / " +
            $"Depth : {firstBossDepth}");
    }


    // =========================================================
    // Wall
    // =========================================================

    private void GenerateWalls(
        int leftX,
        int y) {
        if (wallTilemap == null)
            return;


        Vector3Int leftWallCell =
            new Vector3Int(
                leftX - 1,
                y,
                0);


        Vector3Int rightWallCell =
            new Vector3Int(
                leftX + chunkWidth,
                y,
                0);


        if (leftWallTile != null) {
            wallTilemap.SetTile(
                leftWallCell,
                leftWallTile);
        }


        if (rightWallTile != null) {
            wallTilemap.SetTile(
                rightWallCell,
                rightWallTile);
        }
    }


    // =========================================================
    // Enemy
    // =========================================================

    private void GenerateEnemies(
        int leftX,
        int topY,
        int topDepth,
        HashSet<Vector3Int> reservedCells) {
        if (enemyManager == null)
            return;


        for (int row = 0;
             row < chunkHeight;
             row++) {
            int y =
                topY - row;


            int depth =
                topDepth + row;


            EnemyBase enemyPrefab =
                GetEnemyPrefabByDepth(
                    depth);


            if (enemyPrefab == null)
                continue;


            Vector2Int size =
                enemyPrefab.FootprintSize;


            for (int column = 0;
                 column < chunkWidth;
                 column++) {
                if (Random.value >
                    enemySpawnChance) {
                    continue;
                }


                int x =
                    leftX + column;


                Vector3Int anchorCell =
                    new Vector3Int(
                        x,
                        y,
                        0);


                // =============================
                // Footprint가 Chunk 안에 있는지
                // =============================

                if (!CanFitFootprintInChunk(
                    anchorCell,
                    size,
                    leftX,
                    topY)) {
                    continue;
                }


                // =============================
                // 다른 예약 Cell과 겹치는지
                // =============================

                if (IsFootprintReserved(
                    anchorCell,
                    size,
                    reservedCells)) {
                    continue;
                }


                // =============================
                // 실제 Spawn
                // =============================

                EnemyBase spawned =
                    enemyManager.SpawnEnemy(
                        enemyPrefab,
                        anchorCell,
                        depth);


                if (spawned == null)
                    continue;


                ReserveFootprint(
                    anchorCell,
                    size,
                    reservedCells);
            }
        }
    }

   
    private bool CanFitFootprintInChunk(
        Vector3Int anchorCell,
        Vector2Int size,
        int leftX,
        int topY) {
        int rightX =
            leftX + chunkWidth - 1;


        int bottomY =
            topY - (chunkHeight - 1);


        int footprintRight =
            anchorCell.x + size.x - 1;


        int footprintTop =
            anchorCell.y + size.y - 1;


        return
            anchorCell.x >= leftX &&
            footprintRight <= rightX &&
            anchorCell.y >= bottomY &&
            footprintTop <= topY;
    }


    private bool IsFootprintReserved(
        Vector3Int anchorCell,
        Vector2Int size,
        HashSet<Vector3Int> reservedCells) {
        for (int x = 0;
             x < size.x;
             x++) {
            for (int y = 0;
                 y < size.y;
                 y++) {
                Vector3Int cell =
                    anchorCell
                    + new Vector3Int(
                        x,
                        y,
                        0);


                if (reservedCells.Contains(cell))
                    return true;
            }
        }


        return false;
    }


    private void ReserveFootprint(
        Vector3Int anchorCell,
        Vector2Int size,
        HashSet<Vector3Int> reservedCells) {
        for (int x = 0;
             x < size.x;
             x++) {
            for (int y = 0;
                 y < size.y;
                 y++) {
                reservedCells.Add(
                    anchorCell
                    + new Vector3Int(
                        x,
                        y,
                        0));
            }
        }
    }


    private EnemyBase GetEnemyPrefabByDepth(
        int depth) {
        // 0 ~ 221
        if (depth < 222) {
            return enemy1Prefab;
        }


        // 222 ~ 443
        if (depth < 444) {
            return enemy2Prefab;
        }


        // 444 이상
        return enemy3Prefab;
    }


    // =========================================================
    // Currency
    // =========================================================

    private void GenerateCurrencies(
        int leftX,
        int topY,
        HashSet<Vector3Int> reservedCells) {
        if (pickupManager == null)
            return;


        if (currencyPrefabs == null ||
            currencyPrefabs.Length == 0) {
            return;
        }


        for (int row = 0;
             row < chunkHeight;
             row++) {
            int y =
                topY - row;


            for (int column = 0;
                 column < chunkWidth;
                 column++) {
                if (Random.value >
                    currencySpawnChance) {
                    continue;
                }


                int x =
                    leftX + column;


                Vector3Int cell =
                    new Vector3Int(
                        x,
                        y,
                        0);


                if (reservedCells.Contains(cell))
                    continue;


                CurrencyPickup prefab =
                    currencyPrefabs[
                        Random.Range(
                            0,
                            currencyPrefabs.Length)
                    ];


                if (prefab == null)
                    continue;


                bool spawned =
                    pickupManager.SpawnPickup(
                        prefab,
                        cell);


                if (spawned) {
                    reservedCells.Add(
                        cell);
                }
            }
        }
    }


    // =========================================================
    // Item
    // =========================================================

    private void GenerateItems(
        int leftX,
        int topY,
        HashSet<Vector3Int> reservedCells) {
        if (pickupManager == null)
            return;


        if (itemPrefabs == null ||
            itemPrefabs.Length == 0) {
            return;
        }


        for (int row = 0;
             row < chunkHeight;
             row++) {
            int y =
                topY - row;


            for (int column = 0;
                 column < chunkWidth;
                 column++) {
                if (Random.value >
                    itemSpawnChance) {
                    continue;
                }


                int x =
                    leftX + column;


                Vector3Int cell =
                    new Vector3Int(
                        x,
                        y,
                        0);


                if (reservedCells.Contains(cell))
                    continue;


                SkillItemPickup prefab =
                    itemPrefabs[
                        Random.Range(
                            0,
                            itemPrefabs.Length)
                    ];


                if (prefab == null)
                    continue;


                bool spawned =
                    pickupManager.SpawnPickup(
                        prefab,
                        cell);


                if (spawned) {
                    reservedCells.Add(
                        cell);
                }
            }
        }
    }


    // =========================================================
    // Terrain
    // =========================================================

    private TerrainData PickRandomTerrain() {
        if (terrainTable == null ||
            terrainTable.Count == 0) {
            return null;
        }


        float totalWeight = 0f;


        foreach (TerrainSpawnEntry entry
                 in terrainTable) {
            if (entry.data == null)
                continue;


            totalWeight +=
                Mathf.Max(
                    0f,
                    entry.weight);
        }


        if (totalWeight <= 0f)
            return null;


        float random =
            Random.Range(
                0f,
                totalWeight);


        foreach (TerrainSpawnEntry entry
                 in terrainTable) {
            if (entry.data == null)
                continue;


            float weight =
                Mathf.Max(
                    0f,
                    entry.weight);


            if (random < weight) {
                return entry.data;
            }


            random -= weight;
        }


        return terrainTable[
            terrainTable.Count - 1
        ].data;
    }
}
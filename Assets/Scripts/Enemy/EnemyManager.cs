using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Transform enemyRoot;

    // 점유 Cell -> Enemy
    private readonly Dictionary<Vector3Int, EnemyBase> enemies = new Dictionary<Vector3Int, EnemyBase>();


    private void Awake() {
        enemies.Clear();
    }

    private void Start() {
        RegisterSceneEnemies();
    }


    // =====================================
    // Scene에 직접 배치해둔 Enemy 등록
    // =====================================

    private void RegisterSceneEnemies() {
        if (enemyRoot == null)
            return;

        EnemyBase[] sceneEnemies = enemyRoot.GetComponentsInChildren<EnemyBase>(true);

        foreach (EnemyBase enemy in sceneEnemies) {
            Vector3Int anchorCell = GetAnchorCellFromWorldPosition(enemy);
            RegisterEnemy(enemy,anchorCell,0);
        }
    }


    // =====================================
    // Enemy 검색
    // =====================================

    public bool TryGetEnemy(Vector3Int cell,out EnemyBase enemy) {
        if (enemies.TryGetValue(cell, out enemy)) {
            if (enemy != null)
                return true;
            enemies.Remove(cell);
        }
        enemy = null;
        return false;
    }


    public bool HasEnemy(Vector3Int cell) {
        return TryGetEnemy(cell, out _);
    }


    // =====================================
    // Enemy Spawn
    // =====================================

    public EnemyBase SpawnEnemy(EnemyBase enemyPrefab,Vector3Int anchorCell, int depth) {
        if (enemyPrefab == null)
            return null;

        if (!CanOccupy(enemyPrefab,anchorCell)) {
            Debug.LogWarning(
                $"Enemy 생성 공간 부족 : {anchorCell}");
            return null;
        }

        EnemyBase enemy = Instantiate(enemyPrefab,Vector3.zero,Quaternion.identity,enemyRoot);

        RegisterEnemy(enemy,anchorCell,depth);

        return enemy;
    }


    // =====================================
    // Enemy 등록
    // =====================================

    public bool RegisterEnemy(EnemyBase enemy,Vector3Int anchorCell, int depth) {
        if (enemy == null)
            return false;

        if (!CanOccupy(enemy,anchorCell)) {
            Debug.LogWarning($"Enemy 등록 실패 : {enemy.name}");
            return false;
        }

        enemy.Initialize(this,anchorCell,depth);

        // 점유하는 모든 Cell에 같은 Enemy 등록
        foreach (Vector3Int cell in GetOccupiedCells(enemy, anchorCell)) {
            enemies[cell] = enemy;
        }

        // Sprite는 모든 Cell의 정중앙에 배치
        enemy.transform.position = GetFootprintCenterWorld(anchorCell,enemy.FootprintSize);

        Debug.Log($"Enemy 등록 : {enemy.name} / Anchor : {anchorCell}");
        return true;
    }


    // =====================================
    // Enemy 제거
    // =====================================

    public void RemoveEnemy(EnemyBase enemy) {
        if (enemy == null)
            return;

        foreach (Vector3Int cell in
                 GetOccupiedCells(enemy, enemy.Cell)) {
            if (enemies.TryGetValue(
                    cell,
                    out EnemyBase registered)) {
                if (registered == enemy)
                    enemies.Remove(cell);
            }
        }
    }


    // =====================================
    // 점유 Cell 계산
    // =====================================

    private IEnumerable<Vector3Int> GetOccupiedCells(
        EnemyBase enemy,
        Vector3Int anchorCell) {
        Vector2Int size =
            enemy.FootprintSize;

        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                yield return
                    anchorCell
                    + new Vector3Int(
                        x,
                        y,
                        0);
            }
        }
    }


    // =====================================
    // 해당 위치에 Enemy 배치 가능한가?
    // =====================================

    private bool CanOccupy(
        EnemyBase enemy,
        Vector3Int anchorCell) {
        foreach (Vector3Int cell in
                 GetOccupiedCells(enemy, anchorCell)) {
            if (HasEnemy(cell))
                return false;

            if (terrainTilemap.HasTile(cell))
                return false;
        }

        return true;
    }


    // =====================================
    // 2x2 / 3x3 영역의 정확한 중앙 좌표
    // =====================================

    private Vector3 GetFootprintCenterWorld(
        Vector3Int anchorCell,
        Vector2Int size) {
        Vector3Int oppositeCell =
            anchorCell
            + new Vector3Int(
                size.x - 1,
                size.y - 1,
                0);

        Vector3 first =
            terrainTilemap.GetCellCenterWorld(
                anchorCell);

        Vector3 last =
            terrainTilemap.GetCellCenterWorld(
                oppositeCell);

        return (first + last) * 0.5f;
    }


    // =====================================
    // Scene에 직접 배치한 Enemy 위치 →
    // Footprint 기준 Cell 추정
    // =====================================

    private Vector3Int GetAnchorCellFromWorldPosition(
        EnemyBase enemy) {
        Vector2Int size =
            enemy.FootprintSize;

        Vector3 cellSize =
            terrainTilemap.layoutGrid.cellSize;

        Vector3 offset =
            new Vector3(
                (size.x - 1) * cellSize.x * 0.5f,
                (size.y - 1) * cellSize.y * 0.5f,
                0f);

        Vector3 anchorPosition =
            enemy.transform.position - offset;

        return terrainTilemap.WorldToCell(
            anchorPosition);
    }

    // =====================================
    // Cell 중심 World 좌표 반환
    // =====================================
    public Vector3 GetCellCenterWorld(Vector3Int cell) {
        return terrainTilemap.GetCellCenterWorld(cell);
    }
}
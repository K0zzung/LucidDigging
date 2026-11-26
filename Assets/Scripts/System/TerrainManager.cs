using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap terrainTilemap;

    [Header("Terrain Data")]
    [SerializeField] private List<TerrainData> terrainDatas;

    [Header("Health Bar")]

    [SerializeField]
    private EnemyHealthBar healthBarPrefab;

    [SerializeField]
    private Vector3 healthBarOffset =new Vector3(0f, 0.35f,0f);

    private readonly Dictionary<Vector3Int, EnemyHealthBar> terrainHealthBars = new Dictionary<Vector3Int, EnemyHealthBar>();

    [SerializeField]
    private TerrainHitEffect terrainHitEffectPrefab;

    [SerializeField]
    private TerrainBreakEffect terrainBreakEffectPrefab;

    // TileBase → TerrainData 검색용
    private Dictionary<TileBase, TerrainData> terrainDataLookup;

    // 피해를 받은 타일만 현재 HP 저장
    private Dictionary<Vector3Int, int> damagedTiles = new Dictionary<Vector3Int, int>();
        

    private void Awake() {
        //terrainDataLookup.Clear();
        terrainDataLookup = new Dictionary<TileBase, TerrainData>();

        foreach (TerrainData data in terrainDatas) {
            if (data == null)
                continue;

            if (data.tile == null)
                continue;

            terrainDataLookup[data.tile] =data;
        }
    }

    public void PlaceTerrain(
    Vector3Int cell,
    TerrainData data) {
        if (data == null)
            return;


        if (data.tile == null)
            return;


        // 혹시 이전 Terrain의 HP Bar가 남아있다면 제거
        RemoveHealthBar(
            cell);


        // 이전 HP 기록 초기화
        damagedTiles.Remove(
            cell);


        terrainTilemap.SetTile(
            cell,
            data.tile);
    }

    public bool HasTerrain(Vector3Int cell) {
        if (terrainTilemap == null)
            return false;

        return terrainTilemap.HasTile(cell);
    }

    private void PlayHitEffect(Sprite sprite,Vector3 position) {
        if (terrainHitEffectPrefab == null)
            return;

        if (sprite == null)
            return;


        TerrainHitEffect effect =Instantiate(terrainHitEffectPrefab,position,Quaternion.identity);
        effect.Play(sprite,position);
    }


    private void PlayBreakEffect(Sprite sprite,Vector3 position) {
        if (terrainBreakEffectPrefab == null)
            return;

        if (sprite == null)
            return;
        TerrainBreakEffect effect =Instantiate(terrainBreakEffectPrefab,position,Quaternion.identity);
        effect.Play(sprite,position);
    }

    public void ShowHealthBar(
    Vector3Int cell) {
        if (healthBarPrefab == null)
            return;


        if (terrainTilemap == null)
            return;


        // 이미 파괴된 Terrain이면 생성하지 않음
        if (!HasTerrain(cell))
            return;


        // =============================
        // 최초 생성
        // =============================

        if (!terrainHealthBars.TryGetValue(
            cell,
            out EnemyHealthBar healthBar)
            || healthBar == null) {
            Vector3 position =
                terrainTilemap.GetCellCenterWorld(
                    cell)
                + healthBarOffset;


            healthBar =
                Instantiate(
                    healthBarPrefab,
                    position,
                    Quaternion.identity);


            terrainHealthBars[cell] =
                healthBar;
        }


        // 현재 HP 반영
        RefreshHealthBar(
            cell);
    }

    private void RefreshHealthBar(
    Vector3Int cell) {
        if (!terrainHealthBars.TryGetValue(
            cell,
            out EnemyHealthBar healthBar)) {
            return;
        }


        if (healthBar == null) {
            terrainHealthBars.Remove(
                cell);

            return;
        }


        if (!TryGetTerrainHP(
            cell,
            out int currentHP,
            out int maxHP)) {
            return;
        }


        float ratio =
            maxHP > 0
            ? currentHP / (float)maxHP
            : 0f;


        healthBar.SetRatio(
            ratio);
    }

    private void RemoveHealthBar(
    Vector3Int cell) {
        if (!terrainHealthBars.TryGetValue(
            cell,
            out EnemyHealthBar healthBar)) {
            return;
        }


        if (healthBar != null) {
            Destroy(
                healthBar.gameObject);
        }


        terrainHealthBars.Remove(
            cell);
    }
    public bool DamageTerrain(
    Vector3Int cell,
    int attack) {
        return DamageTerrain(
            cell,
            attack,
            out _);
    }
    /// <summary>
    /// 해당 위치의 지형을 공격한다.
    /// true  = 이번 공격으로 파괴됨
    /// false = 아직 살아있음
    /// </summary>
    public bool DamageTerrain(
    Vector3Int cell,
    int attack,
    out int damage) {
        damage =
            0;


        TerrainData data =
            GetTerrainData(
                cell);


        if (data == null) {
            Debug.LogWarning(
                $"[TerrainManager] " +
                $"TerrainData를 찾지 못했습니다. " +
                $"Cell : {cell}");

            return false;
        }


        Sprite sprite =
            terrainTilemap.GetSprite(
                cell);


        Vector3 position =
            terrainTilemap.GetCellCenterWorld(
                cell);


        int currentHP;


        if (!damagedTiles.TryGetValue(
            cell,
            out currentHP)) {
            currentHP =
                data.maxHP;
        }


        // =============================
        // Damage
        // =============================

        damage =
            DamageCalculator.CalculateDamage(
                attack,
                data.defense);


        currentHP =
            Mathf.Max(
                0,
                currentHP - damage);


        Debug.Log(
            $"[Terrain] {data.terrainName} " +
            $"Damage : {damage} / " +
            $"HP : {currentHP} / {data.maxHP}");


        // =============================
        // Destroy
        // =============================

        if (currentHP <= 0) {
            damagedTiles.Remove(cell);


            RemoveHealthBar(cell);


            terrainTilemap.SetTile(cell, null);


            PlayBreakEffect(sprite, position);
            if (AudioManager.Instance != null) {
                AudioManager.Instance.PlayTerrainBreak();
            }

            return true;
        }


        // =============================
        // Survive
        // =============================

        damagedTiles[cell] =
            currentHP;


        if (terrainHealthBars.ContainsKey(
            cell)) {
            RefreshHealthBar(
                cell);
        }


        PlayHitEffect(
            sprite,
            position);


        return false;
    }


    /// <summary>
    /// 해당 타일의 공격력 반환
    /// </summary>
    public int GetTerrainDefense(Vector3Int cell) {
        TerrainData terrain = GetTerrainData(cell);
        return terrain != null ? terrain.defense : 0;
    }

    public int GetTerrainAttack(Vector3Int cell) {
        TerrainData data = GetTerrainData(cell);

        if (data == null)
            return 0;

        return data.attack;
    }
    private bool TryGetTerrainHP(
    Vector3Int cell,
    out int currentHP,
    out int maxHP) {
        currentHP = 0;
        maxHP = 0;


        TerrainData data =
            GetTerrainData(cell);


        if (data == null)
            return false;


        maxHP =
            data.maxHP;


        if (!damagedTiles.TryGetValue(
            cell,
            out currentHP)) {
            currentHP =
                maxHP;
        }


        return true;
    }

    /// <summary>
    /// 해당 위치에 있는 Tile에 대응되는
    /// TerrainData를 찾는다.
    /// </summary>
    private TerrainData GetTerrainData(Vector3Int cell) {
        TileBase tile = terrainTilemap.GetTile(cell);

        if (tile == null)
            return null;

        if (terrainDataLookup.TryGetValue(tile,out TerrainData data)) {
            return data;
        }

        Debug.LogWarning(
            $"TerrainData를 찾을 수 없습니다. Cell : {cell}");

        return null;
    }
}
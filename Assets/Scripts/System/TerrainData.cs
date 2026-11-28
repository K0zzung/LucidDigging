using UnityEngine;
using UnityEngine.Tilemaps;

public enum TerrainCombatCategory { Terrain, Turret }

[CreateAssetMenu(
    fileName = "TerrainData",
    menuName = "Game/Terrain Data")]
public class TerrainData : ScriptableObject
{
    public string terrainName;
    public TerrainCombatCategory combatCategory = TerrainCombatCategory.Terrain;

    public TileBase tile;

    public Sprite sprite;


    public int maxHP;
    public int attack;
    public int defense;
}
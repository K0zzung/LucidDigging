using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    fileName = "TerrainData",
    menuName = "Game/Terrain Data")]
public class TerrainData : ScriptableObject
{
    public string terrainName;

    public TileBase tile;

    public Sprite sprite;


    public int maxHP;
    public int attack;
    public int defense;
}
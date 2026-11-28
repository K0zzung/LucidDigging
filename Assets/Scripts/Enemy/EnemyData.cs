using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public EnemyCombatCategory combatCategory = EnemyCombatCategory.Monster;

    [Header("Status")]
    public int maxHP;
    public int attack;
    public int defense;

    [Header("Run Experience")]
    [Min(0)] public int experienceReward = 10;

    [Header("Grid")]
    public Vector2Int footprintSize = new Vector2Int(2, 2);
}
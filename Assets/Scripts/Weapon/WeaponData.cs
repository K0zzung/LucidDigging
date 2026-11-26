using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponData",
    menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    // =========================================================
    // Identity
    // =========================================================

    [Header("Identity")]

    [SerializeField]
    private string weaponId;

    [SerializeField]
    private string weaponName;


    // =========================================================
    // Stats
    // =========================================================

    [Header("Stats")]

    [Min(0)]
    [SerializeField]
    private int additionalAttack;


    // =========================================================
    // Inventory
    // =========================================================

    [Header("Inventory")]

    [SerializeField]
    private Sprite inventoryIcon;




    // =========================================================
    // Player Visual
    // =========================================================

    [Header("Player Visual")]

    [SerializeField]
    private Sprite weaponSprite;


    // =========================================================
    // Attack Effect
    // =========================================================

    [Header("Attack Effect")]

    [SerializeField]
    private GameObject attackEffectPrefab;

    [Header("Audio")]

    [SerializeField]
    private AudioClip attackSound;

    public AudioClip AttackSound => attackSound;


    // =========================================================
    // Properties
    // =========================================================

    public string WeaponId =>
        weaponId;

    public string WeaponName =>
        weaponName;

    public int AdditionalAttack =>
        additionalAttack;

    public Sprite InventoryIcon =>
        inventoryIcon;

    public Sprite WeaponSprite =>
        weaponSprite;

    public GameObject AttackEffectPrefab =>
        attackEffectPrefab;
}
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponData",
    menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public bool IsPickaxe => isPickaxe;
    public int PickaxeAttack => pickaxeAttack;
    public float DisplayScale => displayScale;
    public Vector2 GripOffset => gripOffset;
    public float GripAngle => gripAngle;
    [Header("Pickaxe / visible-size calibration")]
    [SerializeField] private bool isPickaxe;
    [SerializeField,Min(1)] private int pickaxeAttack = 10;
    [SerializeField,Min(0.001f)] private float displayScale = 1f;
    [SerializeField] private Vector2 gripOffset;
    [SerializeField] private float gripAngle;

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
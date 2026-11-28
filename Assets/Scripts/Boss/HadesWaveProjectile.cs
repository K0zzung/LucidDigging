using UnityEngine;
using UnityEngine.Tilemaps;

// Fixed pool member: no physics callbacks, textures, materials or per-shot allocations.
public sealed class HadesWaveProjectile : MonoBehaviour
{
    private SpriteRenderer view;
    private Animator animationPlayer;
    private PlayerGridController player;
    private PlayerStatus status;
    private Tilemap walls;
    private EnemyManager enemies;
    private EnemyBase source;
    private Vector3 direction;
    private float speed, range, travelled, age, cellSize, fading;
    private int attack;
    private const float FadeTime = 0.18f;

    public void Prepare() {
        view = GetComponent<SpriteRenderer>();
        animationPlayer = GetComponent<Animator>();
    }

    public void Launch(Vector3 origin, Vector3 aim, float gridSize, float cellsPerSecond,
        float maxCells, int attackPower, PlayerGridController target, PlayerStatus targetStatus, Tilemap wallMap, EnemyManager enemyMap = null, EnemyBase owner = null) {
        enemies = enemyMap; source = owner;
        player = target; status = targetStatus; walls = wallMap;
        direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector3.down;
        cellSize = Mathf.Max(0.01f,gridSize);
        speed = cellsPerSecond*cellSize; range = maxCells*cellSize; attack = attackPower;
        travelled = age = fading = 0f;
        transform.position = origin;
        transform.rotation = Quaternion.Euler(0f,0f,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        // Keep prefab-authored size; Animator owns sprite-frame playback.
        view.color = Color.white;
        gameObject.SetActive(true);
        if (animationPlayer != null) { animationPlayer.Rebind(); animationPlayer.Update(0f); }
    }

    private void Update() {
        if (status == null || status.IsDead) { gameObject.SetActive(false); return; }
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        age += dt;

        if (fading > 0f) {
            fading += dt;
            Color color = view.color; color.a = 1f-Mathf.Clamp01(fading/FadeTime); view.color=color;
            if (fading >= FadeTime) gameObject.SetActive(false);
            return;
        }
        float distance = Mathf.Min(speed*dt,Mathf.Max(0f,range-travelled));
        Vector3 from = transform.position;
        // Sub-cell sweep prevents a low frame rate from skipping a wall/player Cell.
        int steps = Mathf.Max(1,Mathf.CeilToInt(distance/(cellSize*0.2f)));
        for(int i=1;i<=steps;i++) {
            Vector3 point = from+direction*(distance*i/steps);
            transform.position = point;
            if (walls != null && walls.HasTile(walls.WorldToCell(point))) { BeginFade(); return; }
            if (player != null && walls != null && walls.WorldToCell(point) == player.CurrentCell) {
                BeginFade(); // Claim the hit before damage can invoke death/cleanup callbacks.
                status.TakeDamage(DamageCalculator.CalculateDamage(attack,status.Defense));
                return;
            }
            if (enemies != null && walls != null && enemies.TryGetEnemy(walls.WorldToCell(point),out EnemyBase enemy) &&
                enemy != null && enemy != source && !enemy.IsDead) {
                BeginFade(); // One impact even for a multi-cell enemy or reentrant death callbacks.
                enemy.TakeDamageFromEnemy(attack);
                return;
            }
        }
        travelled += distance;
        if (travelled >= range) BeginFade();
    }

    private void BeginFade() { fading = 0.0001f; }
    private void OnDisable() { player = null; status = null; walls = null; enemies = null; source = null; }
}

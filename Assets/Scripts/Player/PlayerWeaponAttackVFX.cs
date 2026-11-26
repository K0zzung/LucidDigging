using System.Collections.Generic;
using UnityEngine;

public enum AttackImpactKind { Enemy, Boss, Terrain, HardTerrain, Break }

public class PlayerWeaponAttackVFX : MonoBehaviour
{
    [SerializeField] private Transform effectRoot;
    [SerializeField] private float effectOffset = 0.45f;
    [Header("Contact sparks - visual only")]
    [SerializeField] private float impactLifetime = 0.12f;
    [SerializeField] private float impactRadius = 0.16f;
    [SerializeField] private float impactWidth = 0.025f;
    [SerializeField] private Color stoneColor = new Color(1f, 0.85f, 0.55f);
    [SerializeField] private Color arondightColor = new Color(0.55f, 0.85f, 1f);
    [SerializeField] private Color dragonColor = new Color(1f, 0.45f, 0.22f);

    private readonly Dictionary<GameObject, WeaponAttackEffect[]> slashPools = new Dictionary<GameObject, WeaponAttackEffect[]>();
    private GameObject impact;
    private LineRenderer[] rays;
    private Material sparkMaterial;
    private float impactAge;
    private float currentRadius;
    private Color currentColor;

    public void Play(Vector2Int direction) {
        var weapon = WeaponManager.Instance != null ? WeaponManager.Instance.CurrentWeapon : null;
        if (weapon == null || weapon.AttackEffectPrefab == null) return;
        GameObject prefab = weapon.AttackEffectPrefab;
        if (!slashPools.TryGetValue(prefab, out WeaponAttackEffect[] pool)) {
            pool = new WeaponAttackEffect[2];
            slashPools.Add(prefab, pool);
        }
        WeaponAttackEffect effect = null;
        for (int i = 0; i < pool.Length; i++) {
            if (pool[i] == null) {
                var instance = Instantiate(prefab);
                pool[i] = instance.GetComponent<WeaponAttackEffect>();
                if (pool[i] == null) { Destroy(instance); return; }
                effect = pool[i];
                break;
            }
            if (!pool[i].gameObject.activeSelf) { effect = pool[i]; break; }
        }
        // Bound the pool even if Inspector lifetime exceeds the action interval.
        if (effect == null) effect = pool[0];
        Vector3 center = effectRoot != null ? effectRoot.position : transform.position;
        Vector2 normal = ((Vector2)direction).normalized;
        effect.transform.position = center + new Vector3(normal.x, normal.y, 0f) * effectOffset;
        effect.PlayPooled(direction);
    }

    public void PlayImpact(Vector3 contact, Vector2Int direction, AttackImpactKind kind) {
        if (impact == null && !BuildImpact()) return;
        var weapon = WeaponManager.Instance != null ? WeaponManager.Instance.CurrentWeapon : null;
        string id = weapon != null ? weapon.WeaponId : "0";
        currentColor = id == "2" ? dragonColor : id == "1" ? arondightColor : stoneColor;
        float weight = id == "2" ? 1.2f : id == "1" ? 1.08f : 1f;
        float targetWeight = kind == AttackImpactKind.Boss ? 0.85f : kind == AttackImpactKind.Break ? 1.25f : 1f;
        currentRadius = impactRadius * weight * targetWeight;
        // Use the struck cell's near face, including multi-cell bosses.
        Vector2 normal = ((Vector2)direction).normalized;
        impact.transform.position = contact - new Vector3(normal.x, normal.y, 0f) * 0.22f;
        impact.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg + 45f);
        impactAge = 0f;
        impact.SetActive(true);
        DrawImpact();
    }

    private bool BuildImpact() {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return false;
        sparkMaterial = new Material(shader);
        impact = new GameObject("NormalAttackContact (reused)");
        rays = new LineRenderer[4];
        for (int i = 0; i < rays.Length; i++) {
            var piece = new GameObject("Spark" + i);
            piece.transform.SetParent(impact.transform, false);
            var ray = piece.AddComponent<LineRenderer>();
            ray.sharedMaterial = sparkMaterial;
            ray.useWorldSpace = false;
            ray.positionCount = 2;
            ray.sortingOrder = 8;
            ray.numCapVertices = 0;
            rays[i] = ray;
        }
        impact.SetActive(false);
        return true;
    }

    private void Update() {
        if (impact == null || !impact.activeSelf) return;
        impactAge += Time.deltaTime;
        if (impactAge >= impactLifetime) { impact.SetActive(false); return; }
        DrawImpact();
    }

    private void DrawImpact() {
        float t = Mathf.Clamp01(impactAge / Mathf.Max(0.001f, impactLifetime));
        float radius = currentRadius * Mathf.Lerp(0.45f, 1.6f, 1f - (1f - t) * (1f - t));
        Color color = Color.Lerp(Color.white, currentColor, Mathf.Clamp01(t * 3f));
        color.a = 1f - t;
        for (int i = 0; i < rays.Length; i++) {
            float angle = i * Mathf.PI * 0.5f;
            var normal = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            rays[i].SetPosition(0, normal * radius * t);
            rays[i].SetPosition(1, normal * radius);
            rays[i].startWidth = impactWidth * (1f - t);
            rays[i].endWidth = impactWidth * 0.25f * (1f - t);
            rays[i].startColor = rays[i].endColor = color;
        }
    }

    private void OnDisable() {
        foreach (var pool in slashPools.Values)
            foreach (var effect in pool) if (effect != null) effect.gameObject.SetActive(false);
        if (impact != null) impact.SetActive(false);
    }

    private void OnDestroy() {
        foreach (var pool in slashPools.Values)
            foreach (var effect in pool) if (effect != null) Destroy(effect.gameObject);
        if (impact != null) Destroy(impact);
        if (sparkMaterial != null) Destroy(sparkMaterial);
    }
}

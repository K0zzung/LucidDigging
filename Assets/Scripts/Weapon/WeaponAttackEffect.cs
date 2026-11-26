using UnityEngine;

public class WeaponAttackEffect : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 0.5f;
    private Vector3 originalScale;
    private Animator animator;
    private SpriteRenderer[] sprites;
    private Color[] colors;
    private float remaining;
    private bool pooled;

    private void Awake() {
        originalScale = transform.localScale;
        animator = GetComponent<Animator>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        colors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
    }

    public void Play(Vector2Int direction) { Begin(direction, false); }
    public void PlayPooled(Vector2Int direction) { Begin(direction, true); }

    private void Begin(Vector2Int direction, bool reuse) {
        pooled = reuse;
        remaining = lifetime;
        gameObject.SetActive(true);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        // Rotation plus Y reflection matches the mirrored left-hand sword sweep.
        transform.localScale = new Vector3(originalScale.x,
            originalScale.y * (direction.x < 0 ? -1f : 1f), originalScale.z);
        for (int i = 0; i < sprites.Length; i++) sprites[i].color = colors[i];
        if (animator != null) {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void Update() {
        remaining -= Time.deltaTime;
        if (remaining <= 0f) {
            if (pooled) gameObject.SetActive(false);
            else Destroy(gameObject);
            return;
        }
        float alpha = Mathf.Clamp01(remaining / Mathf.Max(0.001f, lifetime * 0.3f));
        for (int i = 0; i < sprites.Length; i++) {
            Color color = colors[i];
            color.a *= alpha;
            sprites[i].color = color;
        }
    }
}

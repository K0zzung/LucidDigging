using DG.Tweening;
using UnityEngine;

public class DashEffect : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.06f;
    [SerializeField] private float visibleDuration = 0.04f;
    [SerializeField] private float fadeOutDuration = 0.12f;
    [SerializeField] private float maxAlpha = 0.8f;

    [Header("Scale")]
    [SerializeField] private Vector3 startScale = new Vector3(0.8f, 0.8f, 1f);
    [SerializeField] private Vector3 endScale = new Vector3(1.1f, 1.1f, 1f);

    private Sequence sequence;
    private bool pooled;

    private void Reset() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void PlayPooled(Vector3 worldPosition, Vector2Int direction) {
        pooled = true;
        gameObject.SetActive(true);
        Play(worldPosition, direction);
    }

    public void Play(Vector3 worldPosition, Vector2Int direction) {
        transform.position = worldPosition;

        ApplyRotation(direction);

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null) {
            Debug.LogError("DashEffect: SpriteRenderer가 없습니다.");
            return;
        }

        sequence?.Kill();
        transform.DOKill();

        // 초기값
        transform.localScale = startScale;

        Color color = spriteRenderer.color;
        color.a = 0f;
        spriteRenderer.color = color;

        sequence = DOTween.Sequence();

        sequence.Append(
            spriteRenderer.DOFade(maxAlpha, fadeInDuration)
        );

        sequence.Join(
            transform.DOScale(endScale, fadeInDuration + visibleDuration + fadeOutDuration)
        );

        sequence.AppendInterval(visibleDuration);

        sequence.Append(
            spriteRenderer.DOFade(0f, fadeOutDuration)
        );

        sequence.OnComplete(() => {
            if (pooled) gameObject.SetActive(false);
            else Destroy(gameObject);
        });
    }

    private void ApplyRotation(Vector2Int direction) {
        if (direction == Vector2Int.zero)
            return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 270f;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDisable() {
        sequence?.Kill();
        transform.DOKill();
    }

    private void OnDestroy() {
        sequence?.Kill();
        transform.DOKill();
    }
}
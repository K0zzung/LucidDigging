using DG.Tweening;
using UnityEngine;

public class BossExplosionVFX : MonoBehaviour
{
    [Header("Scale")]

    [Tooltip("처음 나타날 때 크기")]
    [SerializeField]
    private float startScale = 0.35f;

    [Tooltip("폭발 최대 크기")]
    [SerializeField]
    private float maxScale = 1.1f;

    [Tooltip("커지는 시간")]
    [SerializeField]
    private float scaleUpDuration = 0.12f;


    [Header("Fade")]

    [Tooltip("나타나는 시간")]
    [SerializeField]
    private float fadeInDuration = 0.05f;

    [Tooltip("최대 크기에서 잠깐 유지")]
    [SerializeField]
    private float holdDuration = 0.08f;

    [Tooltip("사라지는 시간")]
    [SerializeField]
    private float fadeOutDuration = 0.22f;


    private SpriteRenderer[] renderers;

    private Sequence sequence;

    private Vector3 originalScale;


    private void Awake() {
        renderers =
            GetComponentsInChildren<SpriteRenderer>(
                true);

        originalScale =
            transform.localScale;
    }


    public void Play() {
        sequence?.Kill();


        // =============================
        // 처음은 작게
        // =============================

        transform.localScale =
            originalScale
            * startScale;


        // =============================
        // 처음 Alpha 0
        // =============================

        SetAlpha(0f);


        sequence =
            DOTween.Sequence();


        // =============================
        // 1. 빠르게 커지기
        // =============================

        sequence.Append(
            transform.DOScale(
                originalScale * maxScale,
                scaleUpDuration)
            .SetEase(
                Ease.OutBack));


        // =============================
        // Fade In은 동시에
        // =============================

        foreach (SpriteRenderer renderer
                 in renderers) {
            if (renderer == null)
                continue;

            sequence.Join(
                renderer.DOFade(
                    1f,
                    fadeInDuration));
        }


        // =============================
        // 2. 잠깐 유지
        // =============================

        sequence.AppendInterval(
            holdDuration);


        // =============================
        // 3. 살짝 더 커지면서 Fade Out
        // =============================

        sequence.Append(
            transform.DOScale(
                originalScale * 1.3f,
                fadeOutDuration)
            .SetEase(
                Ease.OutQuad));


        foreach (SpriteRenderer renderer
                 in renderers) {
            if (renderer == null)
                continue;

            sequence.Join(
                renderer.DOFade(
                    0f,
                    fadeOutDuration));
        }


        // =============================
        // 종료
        // =============================

        sequence.OnComplete(() => {
            Destroy(
                gameObject);
        });
    }


    private void SetAlpha(
        float alpha) {
        if (renderers == null)
            return;


        foreach (SpriteRenderer renderer
                 in renderers) {
            if (renderer == null)
                continue;


            Color color =
                renderer.color;

            color.a =
                alpha;

            renderer.color =
                color;
        }
    }


    private void OnDestroy() {
        sequence?.Kill();
    }
}
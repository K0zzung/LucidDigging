using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShockwaveEffect : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;


    [Header("Animation")]
    [SerializeField]
    private float startScale = 0.2f;

    [SerializeField]
    private float scalePerCell = 0.85f;

    [SerializeField]
    private float expandDuration = 0.25f;

    [SerializeField]
    private float fadeDuration = 0.2f;

    [SerializeField]
    private float maxAlpha = 0.8f;


    private void Awake() {
        if (spriteRenderer == null) {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }
    }


    public void Play(
        Vector3 position,
        int size) {
        transform.position =
            position;


        transform.localScale =
            Vector3.one
            * startScale;


        Color color =
            spriteRenderer.color;

        color.a = maxAlpha;

        spriteRenderer.color =
            color;


        float targetScale = size* scalePerCell;


        Sequence sequence =
            DOTween.Sequence();


        // 빠르게 확장
        sequence.Append(
            transform.DOScale(
                targetScale,
                expandDuration)
            .SetEase(Ease.OutQuad));


        // 확장 후 사라짐
        sequence.Join(
            spriteRenderer.DOFade(
                0f,
                expandDuration
                + fadeDuration));


        sequence.OnComplete(() => {
            Destroy(gameObject);
        });
    }
}
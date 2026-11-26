using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShieldEffect : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("Fade")]
    [SerializeField]
    private float fadeInTime = 0.08f;

    [SerializeField]
    private float visibleTime = 0.12f;

    [SerializeField]
    private float fadeOutTime = 0.25f;

    [SerializeField]
    private float maxAlpha = 0.9f;


    [Header("Scale")]
    [SerializeField]
    private float startScale = 0.75f;

    [SerializeField]
    private float endScale = 1.1f;


    private void Awake() {
        if (spriteRenderer == null) {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }
    }


    public void Play(
        Vector3 position) {
        transform.position =
            position;


        transform.localScale =
            Vector3.one * startScale;


        Color color =
            spriteRenderer.color;

        color.a = 0f;

        spriteRenderer.color =
            color;


        Sequence sequence =
            DOTween.Sequence();


        sequence.Append(
            spriteRenderer.DOFade(
                maxAlpha,
                fadeInTime));


        sequence.Join(
            transform.DOScale(
                endScale,
                fadeInTime + visibleTime));


        sequence.AppendInterval(
            visibleTime);


        sequence.Append(
            spriteRenderer.DOFade(
                0f,
                fadeOutTime));


        sequence.OnComplete(() => {
            Destroy(gameObject);
        });
    }
}
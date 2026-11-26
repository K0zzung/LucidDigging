using DG.Tweening;
using UnityEngine;

public class BossSpikeWarning : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;


    [Range(0f, 1f)]
    [SerializeField]
    private float minAlpha = 0.15f;


    [Range(0f, 1f)]
    [SerializeField]
    private float maxAlpha = 0.65f;


    [SerializeField]
    private float blinkTime = 0.1f;


    private Tween blinkTween;


    private void Awake() {
        if (spriteRenderer == null) {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }
    }


    public void Play(
        float duration) {
        if (spriteRenderer == null) {
            Destroy(gameObject);
            return;
        }


        Color color =
            spriteRenderer.color;

        color.a =
            minAlpha;

        spriteRenderer.color =
            color;


        blinkTween =
            spriteRenderer
                .DOFade(
                    maxAlpha,
                    blinkTime)
                .SetLoops(
                    -1,
                    LoopType.Yoyo);


        Destroy(
            gameObject,
            duration);
    }


    private void OnDestroy() {
        blinkTween?.Kill();
    }
}
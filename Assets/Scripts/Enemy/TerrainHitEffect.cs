using DG.Tweening;
using UnityEngine;

public class TerrainHitEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public void Play(Sprite sprite,Vector3 position,System.Action onComplete = null) {
        spriteRenderer.sprite = sprite;
        transform.position = position;
        transform.localScale = Vector3.one;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            transform.DOScale(
                1.15f,
                0.05f)
            .SetEase(Ease.OutQuad)
        );

        sequence.Append(
            transform.DOScale(
                0.9f,
                0.05f)
        );

        sequence.Append(
            transform.DOScale(
                1f,
                0.05f)
        );

        sequence.OnComplete(() => {
            onComplete?.Invoke();

            Destroy(gameObject);
        });
    }
}
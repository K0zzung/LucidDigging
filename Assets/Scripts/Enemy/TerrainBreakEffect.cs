using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TerrainBreakEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Header("Scale")]
    [SerializeField] private float scaleMultiplier = 1f;

    [Header("Movement")]
    [SerializeField] private float jumpHeight = 0.55f;
    [SerializeField] private float horizontalDistance = 0.65f;
    [SerializeField] private float fallDistance = 5f;

    [Header("Time")]
    [SerializeField] private float jumpTime = 0.15f;
    [SerializeField] private float fallTime = 0.5f;

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Play(Sprite sprite, Vector3 position) {
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;

        transform.position = position;
        transform.rotation = Quaternion.identity;

        // 기획 Test.
        transform.localScale = Vector3.one * scaleMultiplier;

        // -1 = 왼쪽
        // +1 = 오른쪽

        float direction =
            Random.value < 0.5f
                ? -1f
                : 1f;


        // 위로 튀는 지점
        Vector3 peakPosition = position +new Vector3(horizontalDistance * 0.35f * direction,jumpHeight,0f);


        // 최종 낙하 지점
        Vector3 endPosition =position +new Vector3(horizontalDistance * direction,-fallDistance,0f);


        Sequence sequence =
            DOTween.Sequence();


        // ============================
        // Pop!
        // ============================

        sequence.Append(
            transform.DOMove(
                    peakPosition,
                    jumpTime)
                .SetEase(Ease.OutQuad)
        );


        // ============================
        // Fall
        // ============================

        sequence.Append(
            transform.DOMove(
                    endPosition,
                    fallTime)
                .SetEase(Ease.InQuad)
        );


        // 전체 시간 동안 회전
        float totalTime =
            jumpTime + fallTime;

        sequence.Insert(
            0f,
            transform.DORotate(
                new Vector3(
                    0f,
                    0f,
                    270f * direction),
                totalTime,
                RotateMode.FastBeyond360)
        );


        // 마지막에 살짝 투명하게
        sequence.Insert(
            jumpTime + fallTime * 0.65f,
            spriteRenderer.DOFade(
                0f,
                fallTime * 0.35f)
        );


        sequence.OnComplete(() => {
            Destroy(gameObject);
        });
    }
}
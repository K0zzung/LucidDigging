using DG.Tweening;
using TMPro;
using UnityEngine;

public class FloatingDamageText : MonoBehaviour
{
    // =========================================================
    // Reference
    // =========================================================

    [Header("Reference")]

    [SerializeField]
    private TMP_Text damageText;


    // =========================================================
    // Animation
    // =========================================================

    [Header("Animation")]

    [SerializeField]
    private float jumpHeight = 0.45f;

    [SerializeField]
    private float riseDuration = 0.18f;

    [SerializeField]
    private float fallDistance = 0.35f;

    [SerializeField]
    private float fallDuration = 0.35f;

    [SerializeField]
    private float horizontalRandom = 0.15f;


    [Header("Scale")]

    [SerializeField]
    private float startScale = 0.8f;

    [SerializeField]
    private float peakScale = 1.1f;


    private Sequence sequence;


    // =========================================================
    // Play
    // =========================================================

    public void Play(
        int damage,
        bool receivedDamage,
        Color color) {
        if (damageText == null)
            return;


        sequence?.Kill();


        // =============================
        // Text
        // =============================

        if (receivedDamage) {
            damageText.text =
                $"-{damage}";
        }
        else {
            damageText.text =
                damage.ToString();
        }


        damageText.color =
            color;


        damageText.alpha =
            1f;


        // =============================
        // 시작 위치
        // =============================

        transform.position +=
            new Vector3(
                Random.Range(
                    -horizontalRandom,
                    horizontalRandom),
                0f,
                0f);


        Vector3 startPosition =
            transform.position;


        transform.localScale =
            Vector3.one
            * startScale;


        // =============================
        // Animation
        // =============================

        sequence =
            DOTween.Sequence();


        // 위로 튀기
        sequence.Append(
            transform.DOMoveY(
                startPosition.y
                + jumpHeight,
                riseDuration)
            .SetEase(
                Ease.OutQuad));


        sequence.Join(
            transform.DOScale(
                peakScale,
                riseDuration)
            .SetEase(
                Ease.OutBack));


        // 아래로 떨어지기
        sequence.Append(
            transform.DOMoveY(
                startPosition.y
                - fallDistance,
                fallDuration)
            .SetEase(
                Ease.InQuad));


        // 떨어지면서 Fade
        sequence.Join(
            DOTween.To(
                () =>
                    damageText.alpha,

                value =>
                    damageText.alpha =
                        value,

                0f,
                fallDuration));


        sequence.Join(
            transform.DOScale(
                startScale,
                fallDuration));


        sequence.OnComplete(
            () => {
                Destroy(
                    gameObject);
            });
    }


    private void OnDestroy() {
        sequence?.Kill();
    }
}
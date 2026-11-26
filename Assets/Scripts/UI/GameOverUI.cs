using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [Header("Root")]

    [SerializeField]
    private CanvasGroup canvasGroup;


    [Header("Result")]

    [SerializeField]
    private TMP_Text depthText;

    [SerializeField]
    private TMP_Text bestDepthText;


    [Header("Run Currency")]

    [SerializeField]
    private TMP_Text dashCurrencyText;

    [SerializeField]
    private TMP_Text shockwaveCurrencyText;

    [SerializeField]
    private TMP_Text shieldCurrencyText;


    [Header("Animation")]

    [SerializeField]
    private RectTransform currencyResultPanel;

    [SerializeField]
    private RectTransform gameOverTextRoot;

    [SerializeField]
    private RectTransform bestDepthRoot;

    [SerializeField]
    private float fadeDuration = 0.3f;

    [SerializeField]
    private float currencyMoveDistance = 70f;

    [SerializeField]
    private float currencyMoveDuration = 0.4f;

    [SerializeField]
    private float popupDuration = 0.35f;

    [SerializeField]
    private float countDuration = 0.5f;

    [SerializeField]
    private float sectionDelay = 0.15f;


    private Sequence showSequence;

    private Vector2 currencyOriginalPosition;

    [Header("Retry")]
    [SerializeField]
    private TMP_Text retryPromptText;

    [SerializeField]
    private float retryPromptDelay = 1.5f;

    [SerializeField]
    private float retryPromptFadeDuration = 0.35f;

    [SerializeField]
    private float retryPromptPulseDuration = 0.8f;

    public bool IsReadyForRetry { get; private set; }

    private Tween retryPromptTween;
    

    private void Awake() {
        if (canvasGroup == null) {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }


        if (currencyResultPanel != null) {
            currencyOriginalPosition =
                currencyResultPanel.anchoredPosition;
        }


        HideImmediate();
    }


    public void Show(
    int depth) {
        gameObject.SetActive(
            true);


        showSequence?.Kill();

        retryPromptTween?.Kill();


        IsReadyForRetry =
            false;


        // =============================
        // Best Depth
        // =============================

        int bestDepth =
            PlayerPrefs.GetInt(
                "BestDepth",
                0);


        if (depth > bestDepth) {
            bestDepth =
                depth;


            PlayerPrefs.SetInt(
                "BestDepth",
                bestDepth);


            PlayerPrefs.Save();
        }


        // =============================
        // Run Currency
        // =============================

        CurrencyManager currency =
            CurrencyManager.Instance;


        int dashCurrency = 0;

        int shockwaveCurrency = 0;

        int shieldCurrency = 0;


        if (currency != null) {
            dashCurrency =
                currency.RunDashCurrency;

            shockwaveCurrency =
                currency.RunShockwaveCurrency;

            shieldCurrency =
                currency.RunShieldCurrency;
        }


        // =============================
        // Text 초기값
        // =============================

        if (depthText != null) {
            depthText.text =
                "0m";
        }


        if (bestDepthText != null) {
            bestDepthText.text =
                "0m";
        }


        if (dashCurrencyText != null) {
            dashCurrencyText.text =
                "X 0";
        }


        if (shockwaveCurrencyText != null) {
            shockwaveCurrencyText.text =
                "X 0";
        }


        if (shieldCurrencyText != null) {
            shieldCurrencyText.text =
                "X 0";
        }


        // =============================
        // Canvas 초기화
        // =============================

        if (canvasGroup != null) {
            canvasGroup.alpha =
                0f;

            canvasGroup.interactable =
                false;

            canvasGroup.blocksRaycasts =
                false;
        }


        if (currencyResultPanel != null) {
            currencyResultPanel.anchoredPosition =
                currencyOriginalPosition
                + Vector2.up
                * currencyMoveDistance;
        }


        if (gameOverTextRoot != null) {
            gameOverTextRoot.localScale =
                Vector3.one * 0.7f;
        }


        if (bestDepthRoot != null) {
            bestDepthRoot.localScale =
                Vector3.one * 0.7f;
        }


        // =============================
        // Retry Prompt 초기화
        // =============================

        if (retryPromptText != null) {
            retryPromptText.alpha =
                0f;


            retryPromptText.transform.localScale =
                Vector3.one * 0.9f;
        }


        // =============================
        // GameOver Sequence
        // =============================

        showSequence =
            DOTween.Sequence();


        // 전체 등장
        if (canvasGroup != null) {
            showSequence.Append(
                canvasGroup.DOFade(
                    1f,
                    fadeDuration));
        }


        // Currency Panel 내려오기
        if (currencyResultPanel != null) {
            showSequence.Join(
                currencyResultPanel
                    .DOAnchorPos(
                        currencyOriginalPosition,
                        currencyMoveDuration)
                    .SetEase(
                        Ease.OutCubic));
        }


        // =============================
        // Currency Count
        // =============================

        showSequence.AppendCallback(
            () => {
                PlayCount(
                    dashCurrencyText,
                    dashCurrency,
                    "X ");

                PlayCount(
                    shockwaveCurrencyText,
                    shockwaveCurrency,
                    "X ");

                PlayCount(
                    shieldCurrencyText,
                    shieldCurrency,
                    "X ");
            });


        // 카운트가 실제로 끝날 때까지 기다림
        showSequence.AppendInterval(
            countDuration
            + sectionDelay);


        // =============================
        // Game Over Text
        // =============================

        if (gameOverTextRoot != null) {
            showSequence.Append(
                gameOverTextRoot
                    .DOScale(
                        1f,
                        popupDuration)
                    .SetEase(
                        Ease.OutBack));
        }


        showSequence.AppendCallback(
            () => {
                PlayDepthCount(
                    depthText,
                    depth);
            });


        showSequence.AppendInterval(
            countDuration
            + sectionDelay);


        // =============================
        // Best Depth
        // =============================

        if (bestDepthRoot != null) {
            showSequence.Append(
                bestDepthRoot
                    .DOScale(
                        1f,
                        popupDuration)
                    .SetEase(
                        Ease.OutBack));
        }


        showSequence.AppendCallback(
            () => {
                PlayDepthCount(
                    bestDepthText,
                    bestDepth);
            });


        // 중요:
        // Best Depth 숫자 카운트까지
        // 완전히 끝날 때까지 기다린다.
        showSequence.AppendInterval(
            countDuration);


        // =============================
        // Retry Prompt Delay
        // =============================

        showSequence.AppendInterval(
            retryPromptDelay);


        // =============================
        // Retry Prompt 등장
        // =============================

        if (retryPromptText != null) {
            showSequence.Append(
                retryPromptText
                    .DOFade(
                        1f,
                        retryPromptFadeDuration));


            showSequence.Join(
                retryPromptText
                    .transform
                    .DOScale(
                        1f,
                        retryPromptFadeDuration)
                    .SetEase(
                        Ease.OutBack));
        }


        // =============================
        // 이제부터 Retry 허용
        // =============================

        showSequence.AppendCallback(
            () => {
                IsReadyForRetry =
                    true;


                // 문구가 살짝 깜빡이도록
                if (retryPromptText != null) {
                    retryPromptTween =
                        retryPromptText
                            .DOFade(
                                0.45f,
                                retryPromptPulseDuration)
                            .SetEase(
                                Ease.InOutSine)
                            .SetLoops(
                                -1,
                                LoopType.Yoyo);
                }
            });


        showSequence.OnComplete(
            () => {
                if (canvasGroup != null) {
                    canvasGroup.interactable =
                        true;

                    canvasGroup.blocksRaycasts =
                        true;
                }
            });
    }


    private void PlayCount(
        TMP_Text target,
        int amount,
        string prefix) {
        if (target == null)
            return;


        int safeAmount =
            Mathf.Max(
                0,
                amount);


        DOVirtual.Int(
            0,
            safeAmount,
            countDuration,
            value => {
                if (target != null) {
                    target.text =
                        $"{prefix}{value}";
                }
            })
            .SetEase(
                Ease.OutCubic);
    }


    private void PlayDepthCount(
        TMP_Text target,
        int depth) {
        if (target == null)
            return;


        int safeDepth =
            Mathf.Max(
                0,
                depth);


        DOVirtual.Int(
            0,
            safeDepth,
            countDuration,
            value => {
                if (target != null) {
                    target.text =
                        $"{value}m";
                }
            })
            .SetEase(
                Ease.OutCubic);
    }


    public void HideImmediate() {
        IsReadyForRetry =
            false;


        showSequence?.Kill();

        retryPromptTween?.Kill();


        if (retryPromptText != null) {
            retryPromptText.alpha =
                0f;
        }


        if (canvasGroup != null) {
            canvasGroup.alpha =
                0f;

            canvasGroup.interactable =
                false;

            canvasGroup.blocksRaycasts =
                false;
        }


        gameObject.SetActive(
            false);
    }


    private void OnDisable() {
        IsReadyForRetry = false;


        showSequence?.Kill();

        retryPromptTween?.Kill();
    }
}
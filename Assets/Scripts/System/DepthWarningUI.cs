using DG.Tweening;
using TMPro;
using UnityEngine;

public class DepthWarningUI : MonoBehaviour
{
    // =========================================================
    // Reference
    // =========================================================

    [Header("Reference")]

    [SerializeField]
    private DepthManager depthManager;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private TMP_Text warningText;


    // =========================================================
    // Text
    // =========================================================

    [Header("Text")]

    [SerializeField]
    private string message =
        "Dream deepens.. Enemies grow stronger";


    // =========================================================
    // Animation
    // =========================================================

    [Header("Animation")]

    [SerializeField]
    private float fadeInDuration = 0.35f;

    [SerializeField]
    private float holdDuration = 1.2f;

    [SerializeField]
    private float fadeOutDuration = 0.45f;


    // =========================================================
    // Runtime
    // =========================================================

    private Sequence sequence;

    private bool hiddenByGameOver;
    private bool bossMessageActive;
    private TMP_FontAsset regularFont;
    public float ShowBossMessage(string text,TMP_FontAsset font) {
        if(hiddenByGameOver || canvasGroup==null || warningText==null)return 0f;
        if(!bossMessageActive)regularFont=warningText.font;
        bossMessageActive=true;
        if(font!=null)warningText.font=font;
        warningText.text=text;
        PlayWarning();
        sequence.OnComplete(()=>{bossMessageActive=false;warningText.font=regularFont;warningText.text=message;});
        return fadeInDuration+holdDuration+fadeOutDuration;
    }
    public void CancelBossMessage() {
        if(!bossMessageActive)return;
        bossMessageActive=false;sequence?.Kill();sequence=null;
        if(canvasGroup!=null)canvasGroup.alpha=0f;
        if(warningText!=null){warningText.font=regularFont;warningText.text=message;}
    }


    // =========================================================
    // Unity
    // =========================================================

    private void Start() {
        hiddenByGameOver =
            false;


        if (canvasGroup != null) {
            canvasGroup.alpha =
                0f;
        }


        if (warningText != null) {
            warningText.text =
                message;
        }


        if (depthManager != null) {
            depthManager.OnDifficultyTierChanged +=
                HandleDifficultyTierChanged;
        }
    }


    private void OnDestroy() {
        if (depthManager != null) {
            depthManager.OnDifficultyTierChanged -=
                HandleDifficultyTierChanged;
        }


        sequence?.Kill();
    }


    // =========================================================
    // Difficulty
    // =========================================================

    private void HandleDifficultyTierChanged(
        int tier) {
        if (tier <= 0)
            return;


        if (hiddenByGameOver || bossMessageActive)
            return;


        if(warningText!=null)warningText.text=message;
        PlayWarning();
    }


    // =========================================================
    // Warning
    // =========================================================

    private void PlayWarning() {
        if (canvasGroup == null)
            return;


        sequence?.Kill();


        canvasGroup.alpha =
            0f;


        sequence =
            DOTween.Sequence();


        sequence.Append(
            canvasGroup.DOFade(
                1f,
                fadeInDuration));


        sequence.AppendInterval(
            holdDuration);


        sequence.Append(
            canvasGroup.DOFade(
                0f,
                fadeOutDuration));
    }


    // =========================================================
    // Game Over
    // =========================================================

    public void HideImmediately() {
        CancelBossMessage();
        hiddenByGameOver =
            true;


        sequence?.Kill();


        sequence =
            null;


        if (canvasGroup != null) {
            canvasGroup.alpha =
                0f;
        }
    }
}
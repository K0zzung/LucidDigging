using DG.Tweening;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private RectTransform skillBar;

    [SerializeField]
    private GameObject hpSlider;


    [Header("Skill Bar Position")]

    [SerializeField]
    private Vector2 hiddenPosition =
        new Vector2(
            -630f,
            781f);

    [SerializeField]
    private Vector2 shownPosition =
        new Vector2(
            1f,
            781f);


    [Header("Animation")]

    [SerializeField]
    private float openDuration = 0.4f;

    [SerializeField]
    private float closeDuration = 0.3f;


    public RectTransform HealthBarRect => hpSlider != null ? hpSlider.GetComponent<RectTransform>() : null;

    private Tween skillBarTween;


    private void Awake() {
        // 시작할 때 잠깐 원래 위치가 보이는 것 방지
        if (skillBar != null) {
            skillBar.anchoredPosition =
                hiddenPosition;
        }
    }


    // =========================================================
    // 게임 시작
    // =========================================================

    public void ShowGameplayHUD() {
        if(skillBar!=null)skillBar.GetComponent<RunInventoryPanel>()?.ShowWithGameplayHUD();
        if (hpSlider != null) {
            hpSlider.SetActive(
                true);
        }


        if (skillBar == null)
            return;


        skillBarTween?.Kill();


        skillBar.anchoredPosition =
            hiddenPosition;


        skillBarTween =
            skillBar.DOAnchorPos(
                    shownPosition,
                    openDuration)
                .SetEase(
                    Ease.OutBack);
    }


    // =========================================================
    // Player Death
    // =========================================================

    public void HideGameplayHUD() {
        if(skillBar!=null)skillBar.GetComponent<RunInventoryPanel>()?.HideWithGameplayHUD();
        // HP는 죽는 순간 바로 제거
        if (hpSlider != null) {
            hpSlider.SetActive(
                false);
        }


        if (skillBar == null)
            return;


        skillBarTween?.Kill();


        skillBarTween =
            skillBar.DOAnchorPos(
                    hiddenPosition,
                    closeDuration)
                .SetEase(
                    Ease.InBack);
    }


    // =========================================================
    // 강제 초기화
    // =========================================================

    public void ResetHUD() {
        if(skillBar!=null)skillBar.GetComponent<RunInventoryPanel>()?.HideWithGameplayHUD();
        skillBarTween?.Kill();


        if (skillBar != null) {
            skillBar.anchoredPosition =
                hiddenPosition;
        }


        if (hpSlider != null) {
            hpSlider.SetActive(
                false);
        }
    }


    private void OnDisable() {
        skillBarTween?.Kill();
    }
}
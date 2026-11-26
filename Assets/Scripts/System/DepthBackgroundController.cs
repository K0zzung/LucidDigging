using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthBackgroundController : MonoBehaviour
{
    [Serializable]
    public struct DepthColorPoint
    {
        [Min(0f)]
        public float depth;

        public Color color;
    }


    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private Transform cameraFollowTarget;


    // =========================================================
    // Depth Color
    // =========================================================

    [Header("Depth Color Settings")]

    [Tooltip("깊이별 배경색")]
    [SerializeField]
    private List<DepthColorPoint> colorPoints =
        new List<DepthColorPoint>();


    [Header("Transition")]

    [SerializeField]
    private AnimationCurve transitionCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f);


    // =========================================================
    // Death Flash
    // =========================================================

    [Header("Death Flash")]

    [Tooltip("사망 순간 빨간색")]
    [SerializeField]
    private Color deathRedColor =
        new Color(
            0.75f,
            0.08f,
            0.08f,
            1f);

    [Tooltip("사망 순간 흰색")]
    [SerializeField]
    private Color deathWhiteColor =
        new Color(
            0.95f,
            0.95f,
            0.95f,
            1f);

    [Tooltip("빨강/흰색이 바뀌는 속도")]
    [SerializeField]
    private float deathFlashStepDuration = 0.07f;

    [Tooltip("Blink가 끝난 뒤 원래 색으로 돌아가는 시간")]
    [SerializeField]
    private float deathFlashReturnDuration = 0.12f;


    // =========================================================
    // Runtime
    // =========================================================

    private float startY;

    private bool backgroundOverride;

    private Coroutine backgroundEffectRoutine;


    // =========================================================
    // Unity
    // =========================================================

    private void Start() {
        if (mainCamera == null) {
            mainCamera =
                Camera.main;
        }


        if (cameraFollowTarget == null) {
            Debug.LogError(
                "[DepthBackgroundController] " +
                "CameraFollowTarget이 없습니다.");

            enabled = false;

            return;
        }


        startY =
            cameraFollowTarget.position.y;


        UpdateBackgroundColor();
    }


    private void LateUpdate() {
        // 사망 연출 등이 직접 Background를
        // 제어 중이라면 Depth 색상 갱신 금지
        if (backgroundOverride)
            return;


        UpdateBackgroundColor();
    }


    // =========================================================
    // Normal Depth Background
    // =========================================================

    private void UpdateBackgroundColor() {
        if (mainCamera == null)
            return;


        if (colorPoints == null ||
            colorPoints.Count == 0) {
            return;
        }


        float currentDepth =
            startY
            - cameraFollowTarget.position.y;


        currentDepth =
            Mathf.Max(
                0f,
                currentDepth);


        Color backgroundColor =
            EvaluateColor(
                currentDepth);


        mainCamera.backgroundColor =
            backgroundColor;
    }


    private Color EvaluateColor(
        float depth) {
        if (depth <= colorPoints[0].depth) {
            return colorPoints[0].color;
        }


        for (int i = 0;
             i < colorPoints.Count - 1;
             i++) {
            DepthColorPoint current =
                colorPoints[i];

            DepthColorPoint next =
                colorPoints[i + 1];


            if (depth <= next.depth) {
                float t =
                    Mathf.InverseLerp(
                        current.depth,
                        next.depth,
                        depth);


                t =
                    transitionCurve.Evaluate(
                        t);


                return Color.Lerp(
                    current.color,
                    next.color,
                    t);
            }
        }


        return colorPoints[
            colorPoints.Count - 1
        ].color;
    }


    // =========================================================
    // Death Flash
    // =========================================================

    public void PlayDeathFlash() {
        if (mainCamera == null)
            return;


        if (backgroundEffectRoutine != null) {
            StopCoroutine(
                backgroundEffectRoutine);
        }


        backgroundEffectRoutine =
            StartCoroutine(
                DeathFlashRoutine());
    }


    private IEnumerator DeathFlashRoutine() {
        backgroundOverride =
            true;


        // 죽기 직전의 Depth 색상 기억
        Color originalColor =
            mainCamera.backgroundColor;


        // =============================
        // 1. RED
        // =============================

        yield return LerpBackgroundColor(
            originalColor,
            deathRedColor,
            deathFlashStepDuration);


        // =============================
        // 2. WHITE
        // =============================

        yield return LerpBackgroundColor(
            deathRedColor,
            deathWhiteColor,
            deathFlashStepDuration);


        // =============================
        // 3. RED
        // =============================

        yield return LerpBackgroundColor(
            deathWhiteColor,
            deathRedColor,
            deathFlashStepDuration);


        // =============================
        // 4. WHITE
        // =============================

        yield return LerpBackgroundColor(
            deathRedColor,
            deathWhiteColor,
            deathFlashStepDuration);


        // =============================
        // 원래 Depth 색으로 복귀
        // =============================

        yield return LerpBackgroundColor(
            deathWhiteColor,
            originalColor,
            deathFlashReturnDuration);


        backgroundOverride =
            false;

        backgroundEffectRoutine =
            null;


        UpdateBackgroundColor();
    }


    // =========================================================
    // Game Over Camera Return
    // =========================================================

    public void ReturnToStartColor(
        float duration) {
        if (mainCamera == null)
            return;


        if (backgroundEffectRoutine != null) {
            StopCoroutine(
                backgroundEffectRoutine);
        }


        backgroundEffectRoutine =
            StartCoroutine(
                ReturnToStartColorRoutine(
                    duration));
    }


    private IEnumerator ReturnToStartColorRoutine(
        float duration) {
        backgroundOverride =
            true;


        Color currentColor =
            mainCamera.backgroundColor;


        // Depth 0 = 게임 시작 시 배경색
        Color startColor =
            EvaluateColor(
                0f);


        yield return LerpBackgroundColor(
            currentColor,
            startColor,
            duration);


        mainCamera.backgroundColor =
            startColor;


        backgroundEffectRoutine =
            null;


        // 중요:
        // Game Over 화면에서는 시작 색으로
        // 고정시키기 위해 Override를 풀지 않는다.
    }


    // =========================================================
    // Color Tween
    // =========================================================

    private IEnumerator LerpBackgroundColor(
        Color from,
        Color to,
        float duration) {
        if (duration <= 0f) {
            mainCamera.backgroundColor =
                to;

            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration) {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / duration);


            mainCamera.backgroundColor =
                Color.Lerp(
                    from,
                    to,
                    t);


            yield return null;
        }


        mainCamera.backgroundColor =
            to;
    }


    // =========================================================
    // 다음 Run을 Scene Reload 없이 시작할 경우 사용
    // =========================================================

    public void ResumeDepthTracking() {
        if (backgroundEffectRoutine != null) {
            StopCoroutine(
                backgroundEffectRoutine);

            backgroundEffectRoutine =
                null;
        }


        backgroundOverride =
            false;


        UpdateBackgroundColor();
    }


#if UNITY_EDITOR

    private void OnValidate() {
        if (colorPoints == null)
            return;


        colorPoints.Sort(
            (a, b) =>
                a.depth.CompareTo(
                    b.depth));
    }

#endif
}
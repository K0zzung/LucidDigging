using System;
using UnityEngine;

public class DepthManager : MonoBehaviour
{
    public int CurrentDepth {
        get;
        private set;
    }


    public event Action<int> OnDepthChanged;

    public event Action<int> OnDifficultyTierChanged;


    private int previousTier;


    private void Start() {
        // 현재 Difficulty 기준 Tier로 초기화
        // 시작하자마자 잘못된 경고가 뜨는 것 방지
        if (DifficultyManager.Instance != null) {
            previousTier =
                DifficultyManager.Instance
                    .GetTier(
                        CurrentDepth);
        }
    }


    public void UpdateDepth(
        Vector3Int playerCell,
        int startY) {
        int depth =
            startY
            - playerCell.y;


        depth =
            Mathf.Max(
                0,
                depth);


        Debug.Log(
            $"[Depth] " +
            $"StartY : {startY} / " +
            $"PlayerY : {playerCell.y} / " +
            $"Calculated : {depth}");


        if (depth ==
            CurrentDepth) {
            return;
        }


        CurrentDepth =
            depth;


        Debug.Log(
            $"[Depth] Changed -> {CurrentDepth}");


        // =============================
        // 일반 Depth 변경
        // =============================

        OnDepthChanged?.Invoke(
            CurrentDepth);


        // =============================
        // 실제 Difficulty Tier 확인
        // =============================

        if (DifficultyManager.Instance == null)
            return;


        int tier =
            DifficultyManager.Instance
                .GetTier(
                    CurrentDepth);


        // 같은 Tier에서는 아무것도 하지 않음
        if (tier <=
            previousTier) {
            return;
        }


        previousTier =
            tier;


        Debug.Log(
            $"[Difficulty] Tier Changed -> {tier}");


        OnDifficultyTierChanged?.Invoke(
            tier);
    }
}
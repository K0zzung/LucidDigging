using System;
using DG.Tweening;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private RectTransform inventoryRoot;

    [Header("Position")]
    [SerializeField] private Vector2 hiddenPosition;
    [SerializeField] private Vector2 openedPosition;

    [Header("Animation")]
    [SerializeField] private float openDuration = 1f;

    private Tween currentTween;

    private void Start() {
        inventoryRoot.anchoredPosition = hiddenPosition;
    }

    public void Open(Action onComplete = null) {
        currentTween?.Kill();

        currentTween = inventoryRoot
            .DOAnchorPos(openedPosition, openDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                onComplete?.Invoke();
            });
    }

    public void Close(Action onComplete = null) {
        currentTween?.Kill();

        currentTween = inventoryRoot
            .DOAnchorPos(hiddenPosition, openDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() => {
                onComplete?.Invoke();
            });
    }

    private void OnDisable() {
        currentTween?.Kill();
    }
}
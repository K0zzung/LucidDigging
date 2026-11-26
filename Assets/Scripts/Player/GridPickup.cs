using DG.Tweening;
using UnityEngine;

public abstract class GridPickup : MonoBehaviour
{
    protected GridPickupManager pickupManager;

    public Vector3Int Cell { get; private set; }

    [Header("Collect Effect")]
    [SerializeField]
    private float collectDuration = 0.18f;

    private bool isCollecting;


    public virtual void Initialize(
        GridPickupManager manager,
        Vector3Int cell) {
        pickupManager = manager;
        Cell = cell;

        isCollecting = false;
    }


    /// <summary>
    /// true = 실제 획득 성공
    /// false = 획득하지 못함
    /// </summary>
    public bool TryCollect(Transform collector) {
        if (isCollecting)
            return false;


        // ==========================
        // 실제 효과 적용 먼저 시도
        // ==========================

        bool success =
            TryApplyPickupEffect();


        // 최대 보유 등으로 획득 실패
        if (!success)
            return false;


        isCollecting = true;


        // ==========================
        // 획득 연출
        // ==========================

        PlayCollectAnimation(
            collector);

        return true;
    }


    /// <summary>
    /// 실제 아이템/재화 적용.
    /// 성공 여부 반환.
    /// </summary>
    protected abstract bool TryApplyPickupEffect();


    private void PlayCollectAnimation(
        Transform collector) {
        if (collector == null) {
            Destroy(gameObject);
            return;
        }


        transform.DOKill();


        Sequence sequence =
            DOTween.Sequence();


        sequence.Join(
            transform.DOMove(
                collector.position,
                collectDuration)
            .SetEase(Ease.InQuad)
        );


        sequence.Join(
            transform.DOScale(
                Vector3.zero,
                collectDuration)
            .SetEase(Ease.InQuad)
        );


        sequence.OnComplete(() => {
            Destroy(gameObject);
        });
    }


    private void OnDestroy() {
        transform.DOKill();
    }
}
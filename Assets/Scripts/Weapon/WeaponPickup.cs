using DG.Tweening;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    // =========================================================
    // Visual
    // =========================================================

    [Header("Visual")]

    [SerializeField]
    private Transform visualRoot;

    [SerializeField]
    private SpriteRenderer weaponRenderer;


    // =========================================================
    // Drop Animation
    // =========================================================

    [Header("Drop Animation")]

    [SerializeField]
    private float dropDuration = 0.8f;

    [SerializeField]
    private float jumpPower = 1.5f;

    [SerializeField]
    private float rotateAmount = 720f;

    [SerializeField]
    private float landingPunchScale = 0.15f;

    [SerializeField]
    private float landingPunchDuration = 0.15f;


    // =========================================================
    // Collect Animation
    // =========================================================

    [Header("Collect")]

    [SerializeField]
    private float collectDuration = 0.2f;


    // =========================================================
    // Runtime
    // =========================================================

    private WeaponData weaponData;

    private PlayerGridController player;

    private Vector3Int cell;

    private bool canCollect;

    private bool collected;

    private Sequence dropSequence;


    // =========================================================
    // Initialize
    // =========================================================

    public void Initialize(
        WeaponData weapon,
        Vector3Int targetCell,
        Vector3 startWorldPosition,
        PlayerGridController targetPlayer) {
        weaponData =
            weapon;


        cell =
            targetCell;


        player =
            targetPlayer;


        canCollect =
            false;


        collected =
            false;


        if (weaponRenderer != null &&
            weaponData != null) {
            weaponRenderer.sprite =
                weaponData.WeaponSprite;
        }


        PlayDropAnimation(
            startWorldPosition);
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update() {
        if (!canCollect)
            return;


        if (collected)
            return;


        if (player == null)
            return;


        if (player.CurrentCell !=
            cell) {
            return;
        }


        Collect();
    }


    // =========================================================
    // Drop
    // =========================================================

    private void PlayDropAnimation(
        Vector3 startWorldPosition) {
        if (visualRoot == null) {
            canCollect =
                true;

            return;
        }


        dropSequence?.Kill();


        Vector3 targetPosition =
            transform.position;


        visualRoot.position =
            startWorldPosition;


        visualRoot.localScale =
            Vector3.one;


        visualRoot.rotation =
            Quaternion.identity;


        dropSequence =
            DOTween.Sequence();


        // =============================
        // 튀어나오면서 목표 Cell까지 하강
        // =============================

        dropSequence.Append(
            visualRoot
                .DOJump(
                    targetPosition,
                    jumpPower,
                    1,
                    dropDuration)
                .SetEase(
                    Ease.OutQuad));


        // =============================
        // 공중 회전
        // =============================

        dropSequence.Join(
            visualRoot
                .DORotate(
                    new Vector3(
                        0f,
                        0f,
                        rotateAmount),
                    dropDuration,
                    RotateMode.FastBeyond360)
                .SetEase(
                    Ease.OutCubic));


        // =============================
        // 착지
        // =============================

        dropSequence.AppendCallback(
            () => {
                visualRoot.position =
                    targetPosition;


                visualRoot.rotation =
                    Quaternion.identity;
            });


        dropSequence.Append(
            visualRoot
                .DOPunchScale(
                    Vector3.one
                    * landingPunchScale,
                    landingPunchDuration,
                    5,
                    0.5f));


        dropSequence.OnComplete(
            () => {
                canCollect =
                    true;
            });
    }


    // =========================================================
    // Collect
    // =========================================================

    private void Collect() {
        if (collected)
            return;


        if (weaponData == null)
            return;


        if (WeaponManager.Instance == null)
            return;


        collected =
            true;


        // 영구 소유 처리
        WeaponManager.Instance.UnlockWeapon(weaponData);
        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlayItemPickup();
        }

        Debug.Log($"[Weapon Pickup] Acquired : {weaponData.WeaponName}");


        // =============================
        // 획득 연출
        // =============================

        if (visualRoot == null ||
            player == null) {
            Destroy(
                gameObject);

            return;
        }


        visualRoot.DOKill();


        Sequence collectSequence =
            DOTween.Sequence();


        collectSequence.Append(
            visualRoot.DOMove(
                player.transform.position,
                collectDuration)
                .SetEase(
                    Ease.InQuad));


        collectSequence.Join(
            visualRoot.DOScale(
                Vector3.zero,
                collectDuration));


        collectSequence.OnComplete(
            () => {
                Destroy(
                    gameObject);
            });
    }


    // =========================================================
    // Cleanup
    // =========================================================

    private void OnDestroy() {
        dropSequence?.Kill();


        if (visualRoot != null) {
            visualRoot.DOKill();
        }
    }
}
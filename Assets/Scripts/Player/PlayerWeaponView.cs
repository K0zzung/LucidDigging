using System;
using DG.Tweening;
using UnityEngine;

public class PlayerWeaponView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform armFacingRoot;
    [SerializeField] private Transform armPivot;

    [Header("Direction")]
    [SerializeField] private float visualAngleOffset = -270f; 

    [Header("Attack Animation")]
    [SerializeField] private float windupAngle = 35f;
    [SerializeField] private float followThroughAngle = 75f;

    [SerializeField] private float windupTime = 0.05f;
    [SerializeField] private float swingTime = 0.08f;
    [SerializeField] private float returnTime = 0.1f;

    [Header("Visual travel (does not change attack timing)")]
    [SerializeField] private float pullbackDistance = 0.045f;
    [SerializeField] private float strikeDistance = 0.075f;
    [SerializeField] private float arondightMotionWeight = 1.08f;
    [SerializeField] private float dragonMotionWeight = 1.22f;

    private Vector3 defaultArmPosition;
    private Vector3 defaultArmScale;
    private Sequence attackSequence;

    private Vector3 facingRootOriginalScale;
    private Quaternion defaultArmRotation;

    private void Awake() {
        // Scene에서 설정해둔 기본 Scale 저장
        facingRootOriginalScale =
            armFacingRoot.localScale;

        // Scene에서 직접 맞춘 기본 검 각도 저장
        defaultArmRotation =
            armPivot.localRotation;
        defaultArmPosition = armPivot.localPosition;
        defaultArmScale = armPivot.localScale;
    }

    public void SetDirection(Vector2Int direction) {
        ApplyDirection(direction);
    }

    public void PlayAttack(
        Vector2Int direction,
        Action onHit = null,
        Action onComplete = null) {
        attackSequence?.Kill();
        RestorePose();

        float baseAngle = ApplyDirection(direction);

        Vector3 travel = new Vector3(Mathf.Abs(direction.x), direction.y, 0f).normalized;
        string weaponId = WeaponManager.Instance != null && WeaponManager.Instance.CurrentWeapon != null
            ? WeaponManager.Instance.CurrentWeapon.WeaponId : "0";
        float weight = weaponId == "2" ? dragonMotionWeight : weaponId == "1" ? arondightMotionWeight : 1f;
        attackSequence = DOTween.Sequence();

        if (WeaponManager.Instance != null &&AudioManager.Instance != null) {
            WeaponData weapon = WeaponManager.Instance.CurrentWeapon;

            if (weapon != null) {
                AudioManager.Instance.PlaySFX(weapon.AttackSound);
            }
        }

        // 1. 공격 전 살짝 뒤로 젖힘
        attackSequence.Append(
            armPivot.DOLocalRotate(
                new Vector3(
                    0f,
                    0f,
                    baseAngle + windupAngle * weight),
                windupTime
            ).SetEase(Ease.OutSine)
        );
        attackSequence.Join(armPivot.DOLocalMove(defaultArmPosition - travel * pullbackDistance * weight, windupTime).SetEase(Ease.OutSine));

        // 2. 빠르게 휘두름
        attackSequence.Append(
            armPivot.DOLocalRotate(
                new Vector3(
                    0f,
                    0f,
                    baseAngle - followThroughAngle * weight),
                swingTime
            ).SetEase(Ease.InExpo)
        );
        attackSequence.Join(armPivot.DOLocalMove(defaultArmPosition + travel * strikeDistance * weight, swingTime).SetEase(Ease.InExpo));

        // 실제 공격 판정
        attackSequence.AppendCallback(() => {
            onHit?.Invoke();
        });

        // 3. 기본 방향 복귀
        attackSequence.Append(
            armPivot.DOLocalRotateQuaternion(
                defaultArmRotation,
                returnTime
                ).SetEase(Ease.OutCubic)
        );
        attackSequence.Join(armPivot.DOLocalMove(defaultArmPosition, returnTime).SetEase(Ease.OutCubic));

        attackSequence.OnComplete(() => {
            attackSequence = null;
            RestorePose();
            onComplete?.Invoke();
        });
    }

    private float ApplyDirection(Vector2Int direction) {
        bool isLeft = direction.x < 0;

        // ArmPivot까지 포함해서 통째로 Mirror
        Vector3 scale = facingRootOriginalScale;

        scale.x =
            Mathf.Abs(facingRootOriginalScale.x)
            * (isLeft ? -1f : 1f);

        armFacingRoot.localScale = scale;

        // 왼쪽 방향은 오른쪽 기준 방향으로 변환
        Vector2 canonicalDirection =
            new Vector2(
                Mathf.Abs(direction.x),
                direction.y
            );

        float directionAngle =
            Mathf.Atan2(
                canonicalDirection.y,
                canonicalDirection.x)
            * Mathf.Rad2Deg;

        float finalAngle =
            directionAngle + visualAngleOffset;

        armPivot.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                finalAngle
            );

        return finalAngle;
    }

    private void RestorePose() {
        armPivot.localPosition = defaultArmPosition;
        armPivot.localScale = defaultArmScale;
        armPivot.localRotation = defaultArmRotation;
    }

    private void OnDisable() {
        attackSequence?.Kill();
        attackSequence = null;
        if (armPivot != null) RestorePose();
    }
}
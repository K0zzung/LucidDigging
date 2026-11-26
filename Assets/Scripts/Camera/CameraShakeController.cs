using Cinemachine;
using DG.Tweening;
using UnityEngine;

public class CameraShakeController : MonoBehaviour
{
    public static CameraShakeController Instance {
        get;
        private set;
    }


    // =========================================================
    // Camera
    // =========================================================

    [Header("Camera")]

    [SerializeField]
    private CinemachineVirtualCamera gameplayCamera;


    // =========================================================
    // Normal Attack
    // =========================================================

    [Header("Normal Hit")]

    [SerializeField]
    private float normalStrength = 0.2f;

    [SerializeField]
    private float normalDuration = 0.1f;

    [SerializeField]
    private float normalFrequency = 2f;


    // =========================================================
    // Dash
    // =========================================================

    [Header("Dash Start")]

    [SerializeField]
    private float dashStartStrength = 0.18f;

    [SerializeField]
    private float dashStartDuration = 0.08f;

    [SerializeField]
    private float dashStartFrequency = 4f;


    [Header("Dash Impact")]

    [SerializeField]
    private float dashImpactStrength = 0.32f;

    [SerializeField]
    private float dashImpactDuration = 0.09f;

    [SerializeField]
    private float dashImpactFrequency = 4.5f;


    // =========================================================
    // Shield
    // =========================================================

    [Header("Shield")]

    [SerializeField]
    private float shieldStrength = 0.14f;

    [SerializeField]
    private float shieldDuration = 0.13f;

    [SerializeField]
    private float shieldFrequency = 1.5f;


    // =========================================================
    // Shockwave
    // =========================================================

    [Header("Shockwave")]

    [SerializeField]
    private float shockwaveStrength = 0.5f;

    [SerializeField]
    private float shockwaveDuration = 0.22f;

    [SerializeField]
    private float shockwaveFrequency = 2.5f;


    // =========================================================
    // Boss Kill
    // =========================================================

    [Header("Boss Kill")]

    [SerializeField]
    private float bossKillStrength = 0.75f;

    [SerializeField]
    private float bossKillDuration = 0.32f;

    [SerializeField]
    private float bossKillFrequency = 2.2f;





    // =========================================================
    // Runtime
    // =========================================================

    private CinemachineBasicMultiChannelPerlin noise;

    [Header("Normal attack contact (strength / duration / frequency)")]
    [SerializeField] private Vector3 enemyContact = new Vector3(0.075f, 0.055f, 2.4f);
    [SerializeField] private Vector3 terrainContact = new Vector3(0.09f, 0.06f, 2.8f);
    [SerializeField] private Vector3 hardTerrainContact = new Vector3(0.13f, 0.075f, 3.2f);
    [SerializeField] private Vector3 breakContact = new Vector3(0.17f, 0.085f, 3.4f);
    [SerializeField] private Vector3 bossContact = new Vector3(0.09f, 0.055f, 2.6f);
    private float bossKillProtectedUntil;
    private float protectedShakeUntil;
    private Tween shakeTween;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake() {
        Instance =
            this;


        if (gameplayCamera != null) {
            noise =
                gameplayCamera
                    .GetCinemachineComponent<
                        CinemachineBasicMultiChannelPerlin>();
        }


        if (noise == null) {
            Debug.LogWarning(
                "[CameraShake] GamePlayCam에 " +
                "Basic Multi Channel Perlin이 없습니다.");
        }
    }


    private void OnDestroy() {
        shakeTween?.Kill();


        if (Instance == this) {
            Instance =
                null;
        }
    }




    // =========================================================
    // Presets
    // =========================================================

    public void ShakeAttackImpact(AttackImpactKind kind) {
        // Ordinary hits must never cancel an active skill/boss-kill preset.
        if (Time.time < protectedShakeUntil) return;
        Vector3 preset = kind == AttackImpactKind.Break ? breakContact :
            kind == AttackImpactKind.HardTerrain ? hardTerrainContact :
            kind == AttackImpactKind.Terrain ? terrainContact :
            kind == AttackImpactKind.Boss ? bossContact : enemyContact;
        ApplyShake(preset.x, preset.y, preset.z);
    }

    public void ShakeNormal() {
        Shake(
            normalStrength,
            normalDuration,
            normalFrequency);
    }


    public void ShakeDashStart() {
        if (Time.time < protectedShakeUntil) return;
        Shake(
            dashStartStrength * 0.45f,
            dashStartDuration,
            dashStartFrequency);
    }


    public void ShakeDashImpact() { ShakeDashContact(true); }

    [Header("Dash contact (visual only)")]
    [SerializeField] private float dashContactCooldown = 0.12f;
    private float nextDashContact;
    public void ShakeDashContact(bool strong) {
        if (Time.time < protectedShakeUntil || Time.time < nextDashContact) return;
        nextDashContact = Time.time + Mathf.Max(0.01f, dashContactCooldown);
        ApplyShake(dashImpactStrength * (strong ? 0.7f : 0.32f), dashImpactDuration, dashImpactFrequency);
    }

    public void ShakeShield() {
        Shake(
            shieldStrength,
            shieldDuration,
            shieldFrequency);
    }


    public void ShakeShockwave() {
        Shake(
            shockwaveStrength,
            shockwaveDuration,
            shockwaveFrequency);
    }


    public void ShakeBossKill() {
        bossKillProtectedUntil = Time.time + bossKillDuration;
        Shake(
            bossKillStrength,
            bossKillDuration,
            bossKillFrequency);
    }

   

    // 기존 코드 호환용
    public void ShakeSkill() {
        ShakeShockwave();
    }


    // =========================================================
    // Shake
    // =========================================================

    public void Shake(
        float strength,
        float duration,
        float frequency) {
        if (Time.time < bossKillProtectedUntil && strength < bossKillStrength) return;
        protectedShakeUntil = Time.time + Mathf.Max(0f, duration);
        ApplyShake(strength, duration, frequency);
    }

    private void ApplyShake(float strength, float duration, float frequency) {
        if (noise == null) return;
        shakeTween?.Kill();


        noise.m_AmplitudeGain =
            strength;


        noise.m_FrequencyGain =
            frequency;


        shakeTween =
            DOTween.To(
                () =>
                    noise.m_AmplitudeGain,

                value =>
                    noise.m_AmplitudeGain =
                        value,

                0f,
                duration)
            .SetEase(
                Ease.OutQuad)
            .OnComplete(
                () => {
                    noise.m_AmplitudeGain =
                        0f;
                });
    }
}
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance {
        get;
        private set;
    }


    // =========================================================
    // Sources
    // =========================================================

    [Header("Audio Sources")]

    [SerializeField]
    private AudioSource bgmSource;

    [SerializeField]
    private AudioSource sfxSource;

    [SerializeField]
    private AudioSource uiSource;


    // =========================================================
    // BGM
    // =========================================================

    [Header("BGM")]

    [SerializeField]
    private AudioClip gameplayBGM;


    // =========================================================
    // Common SFX
    // =========================================================

    [Header("Common SFX")]

    [SerializeField]
    private AudioClip enemyKillSFX;

    [SerializeField]
    private AudioClip terrainBreakSFX;

    [SerializeField]
    private AudioClip bossKillSFX;


    // =========================================================
    // Skill SFX
    // =========================================================

    [Header("Skill SFX")]

    [SerializeField]
    private AudioClip dashSFX;

    [SerializeField]
    private AudioClip shieldSFX;

    [SerializeField]
    private AudioClip shockwaveSFX;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private AudioClip uiClickSFX;


    [Header("Mixer Snapshot")]

    [SerializeField]
    private AudioMixerSnapshot gameplaySnapshot;

    [SerializeField]
    private AudioMixerSnapshot gameOverSnapshot;

    [SerializeField]
    private float snapshotTransitionTime = 0.15f;

    [Header("Pickup SFX")]

    [SerializeField]
    private AudioClip itemPickupSFX;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake() {
        Instance =
            this;
    }


    private void Start() {
        EnterGameplayAudio();
        PlayBGM(gameplayBGM);
    }


    // =========================================================
    // Generic
    // =========================================================

    public void PlaySFX(
        AudioClip clip,
        float volume = 1f) {
        if (clip == null)
            return;

        if (sfxSource == null)
            return;


        sfxSource.PlayOneShot(
            clip,
            volume);
    }


    public void PlayUI(
        AudioClip clip,
        float volume = 1f) {
        if (clip == null)
            return;

        if (uiSource == null)
            return;


        uiSource.PlayOneShot(
            clip,
            volume);
    }


    public void PlayBGM(
        AudioClip clip) {
        if (clip == null)
            return;

        if (bgmSource == null)
            return;


        if (bgmSource.clip == clip &&
            bgmSource.isPlaying) {
            return;
        }


        bgmSource.clip =
            clip;

        bgmSource.loop =
            true;

        bgmSource.Play();
    }


    // =========================================================
    // Presets
    // =========================================================

    public void PlayEnemyKill() {
        PlaySFX(
            enemyKillSFX);
    }


    public void PlayTerrainBreak() {
        PlaySFX(
            terrainBreakSFX);
    }


    public void PlayBossKill() {
        PlaySFX(bossKillSFX);
    }


    public void PlayDash() {
        PlaySFX(
            dashSFX);
    }


    public void PlayShield() {
        PlaySFX(
            shieldSFX);
    }


    public void PlayShockwave() {
        PlaySFX(
            shockwaveSFX);
    }


    public void PlayUIClick() {
        PlayUI(
            uiClickSFX);
    }

    public void EnterGameplayAudio() {
        if (gameplaySnapshot == null)
            return;


        gameplaySnapshot.TransitionTo(
            snapshotTransitionTime);
    }


    public void EnterGameOverAudio() {
        if (gameOverSnapshot == null)
            return;


        gameOverSnapshot.TransitionTo(
            snapshotTransitionTime);
    }

    public void PlayItemPickup() {
        if (itemPickupSFX == null) {
            Debug.LogError(
                "[Audio] Item Pickup SFX가 연결되지 않았습니다.");

            return;
        }


        if (sfxSource == null) {
            Debug.LogError(
                "[Audio] SFX Source가 연결되지 않았습니다.");

            return;
        }


        Debug.Log(
            "[Audio] Item Pickup Sound Play");


        PlaySFX(
            itemPickupSFX);
    }
}
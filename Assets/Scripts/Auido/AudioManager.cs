using DG.Tweening;
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
    [SerializeField] private AudioClip hadesBattleBGM;
    [SerializeField, Min(0f)] private float bgmFadeOutTime = .35f;
    [SerializeField, Min(.01f)] private float bgmFadeInTime = 1.2f;
    private Sequence bgmTransition;
    private AudioClip pendingBGM;
    private Object hadesMusicOwner;
    private float bgmVolume = 1f;



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
        if(bgmSource!=null)bgmVolume=bgmSource.volume;
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
        bgmTransition?.Kill(); bgmTransition=null; pendingBGM=null;
        if(bgmSource!=null)bgmSource.volume=bgmVolume;
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

    public void BeginHadesBattle(Object owner) {
        if(owner==null || hadesBattleBGM==null)return;
        hadesMusicOwner=owner;
        FadeToBGM(hadesBattleBGM);
    }
    public void EndHadesBattle(Object owner) {
        if(hadesMusicOwner!=owner || hadesMusicOwner==null)return;
        hadesMusicOwner=null;
        FadeToBGM(gameplayBGM);
    }
    private void FadeToBGM(AudioClip clip) {
        if(bgmSource==null || clip==null || pendingBGM==clip)return;
        if(bgmTransition==null && bgmSource.clip==clip && bgmSource.isPlaying)return;
        bgmTransition?.Kill();pendingBGM=clip;
        bgmTransition=DOTween.Sequence().SetUpdate(true);
        if(bgmSource.clip!=clip || !bgmSource.isPlaying) {
            bgmTransition.Append(bgmSource.DOFade(0f,bgmFadeOutTime));
            bgmTransition.AppendCallback(()=>{
                bgmSource.clip=clip;bgmSource.loop=true;bgmSource.volume=0f;bgmSource.Play();
            });
        }
        bgmTransition.Append(bgmSource.DOFade(bgmVolume,bgmFadeInTime).SetEase(Ease.OutSine));
        bgmTransition.OnComplete(()=>{bgmTransition=null;pendingBGM=null;});
    }
    private void OnDisable() {
        bgmTransition?.Kill();bgmTransition=null;pendingBGM=null;hadesMusicOwner=null;
        if(bgmSource!=null)bgmSource.volume=bgmVolume;
    }

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
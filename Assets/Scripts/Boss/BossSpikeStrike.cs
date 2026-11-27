using System;
using System.Collections;
using UnityEngine;

public class BossSpikeStrike : MonoBehaviour
{
    [SerializeField] private float hitDelay = 0.15f;
    [Header("Original frame animation - shared ground anchor")]
    [SerializeField] private Sprite[] riseFrames;
    [SerializeField] private Vector2[] riseOffsets;
    [SerializeField] private float[] riseKeys;
    [SerializeField] private Sprite[] downFrames;
    [SerializeField] private Vector2[] downOffsets;
    [SerializeField] private float[] downKeys;
    [SerializeField, Min(0.1f)] private float playbackSpeed = 1.3f;
    [SerializeField, Min(0f)] private float peakHold = 0.06f;
    private float phaseSpeed = 1f;
    public void SetPhaseSpeed(float multiplier) { phaseSpeed = Mathf.Max(0.1f,multiplier); }
    private const float ClipLength = 0.6f;
    private Vector3 groundPosition;
    private SpriteRenderer rendererObject;
    private Color originalColor;
    private bool hit;
    public float HitDelay => hitDelay/phaseSpeed;
    public float Lifetime => ClipLength * 2f / Mathf.Max(0.1f,playbackSpeed*phaseSpeed) + peakHold/phaseSpeed;
    private void Awake() {
        rendererObject = GetComponent<SpriteRenderer>();
        if (rendererObject != null) originalColor = rendererObject.color;
    }
    public void PlayCell(float cellHeight, Action onHit) {
        if (riseFrames != null && riseFrames.Length > 0 && riseFrames[riseFrames.Length-1] != null) {
            // Full sheet frames have 140 visible pixels above their common ground.
            float scale = 3f*cellHeight*riseFrames[riseFrames.Length-1].pixelsPerUnit/140f;
            transform.localScale = Vector3.one*scale;
        }
        Play(onHit);
    }
    public void PlayHorizontal(float cellWidth,float cellHeight,int direction,Action onHit) {
        if (riseFrames != null && riseFrames.Length > 0 && riseFrames[riseFrames.Length-1] != null) {
            float ppu=riseFrames[riseFrames.Length-1].pixelsPerUnit;
            transform.localScale=new Vector3(cellHeight*ppu/63f,3f*cellWidth*ppu/140f,1f);
        }
        transform.localRotation=Quaternion.Euler(0f,0f,direction>0?-90f:90f);
        Play(onHit);
    }
    public void Play(Action onHit) {
        StopAllCoroutines(); hit = false;
        groundPosition = transform.localPosition;
        var animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        if (rendererObject == null || riseFrames == null || riseFrames.Length == 0 || downFrames == null || downFrames.Length == 0 ||
            Array.Exists(riseFrames, frame => frame == null) || Array.Exists(downFrames, frame => frame == null)) {
            Debug.LogError("[Hades Spike] Missing rise/down frame data.",this);
            Destroy(gameObject); return;
        }
        StartCoroutine(StrikeRoutine(onHit));
    }
    public static int FrameIndex(float clipTime, float[] keys) {
        int index = 0;
        if (keys != null) for (int i=1;i<keys.Length;i++) { if(clipTime < keys[i])break; index=i; }
        return index;
    }
    private void Draw(Sprite[] frames, Vector2[] offsets, float[] keys, float time, float alpha) {
        int index = Mathf.Min(FrameIndex(time,keys),frames.Length-1);
        rendererObject.sprite = frames[index];
        Vector2 offset = offsets != null && index < offsets.Length ? offsets[index] : Vector2.zero;
        // Every crop is registered against the same sheet-space ground line.
        transform.localPosition = groundPosition + transform.localRotation*new Vector3(offset.x*transform.localScale.x,offset.y*transform.localScale.y,0f);
        Color c = originalColor; c.a *= alpha; rendererObject.color=c;
    }
    private IEnumerator StrikeRoutine(Action onHit) {
        float start = Time.time;
        float speed = Mathf.Max(0.1f,playbackSpeed*phaseSpeed);
        float rise = ClipLength / speed;
        float downStart = rise + peakHold/phaseSpeed;
        float duration = downStart + ClipLength / speed;
        while (Time.time-start < duration) {
            float age=Time.time-start;
            if(age<downStart) Draw(riseFrames,riseOffsets,riseKeys,Mathf.Min(ClipLength,age*speed),1f);
            else {
                float remaining=duration-age;
                Draw(downFrames,downOffsets,downKeys,(age-downStart)*speed,Mathf.Clamp01(remaining/0.06f));
            }
            if(!hit && age>=HitDelay) {hit=true;onHit?.Invoke();}
            yield return null;
        }
        transform.localPosition=groundPosition;
        Color clear=originalColor;clear.a=0f;rendererObject.color=clear;
        Destroy(gameObject);
    }
    private void OnDisable() { StopAllCoroutines(); }
}

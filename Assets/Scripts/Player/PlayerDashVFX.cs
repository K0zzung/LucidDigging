using UnityEngine;

// Original blue Dash prefab only. No extra ghosts, procedural rays or fracture visuals.
public class PlayerDashVFX : MonoBehaviour
{
    [SerializeField] private float contactInterval = 0.12f;
    private readonly DashEffect[] legacy = new DashEffect[5];
    private int legacyCursor;
    private float lastLegacyTime = float.NegativeInfinity;
    private float nextContact;
    private PlayerStatus status;
    private void Awake() { status = GetComponent<PlayerStatus>(); }
    private void OnEnable() { if (status != null) status.OnPlayerDied += Cancel; }
    public void Begin(Vector2Int direction) { nextContact = 0f; }
    public void End() { } // Let the original blue sprite finish its own fade.
    public void PlayLegacy(DashEffect prefab, Vector3 position, Vector2Int direction) {
        if (prefab == null || Time.time - lastLegacyTime < 0.01f) return;
        lastLegacyTime = Time.time;
        int index = legacyCursor;
        legacyCursor = (legacyCursor + 1) % legacy.Length;
        if (legacy[index] == null) legacy[index] = Instantiate(prefab);
        legacy[index].PlayPooled(position, direction);
    }
    public void Contact(Vector3 position, Vector2Int direction, bool wall, bool strong) {
        if (!wall && Time.time < nextContact) return;
        nextContact = Time.time + Mathf.Max(0.01f, contactInterval);
        if (CameraShakeController.Instance != null)
            CameraShakeController.Instance.ShakeDashContact(wall || strong);
    }
    public void Cancel() {
        foreach (var effect in legacy) if (effect != null) effect.gameObject.SetActive(false);
    }
    private void OnDisable() {
        if (status != null) status.OnPlayerDied -= Cancel;
        Cancel();
    }
    private void OnDestroy() {
        foreach (var effect in legacy) if (effect != null) Destroy(effect.gameObject);
    }
}

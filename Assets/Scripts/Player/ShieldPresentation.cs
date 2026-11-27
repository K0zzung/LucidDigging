using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: never owns the buff lifetime, defense or skill consumption.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkillController))]
public sealed class ShieldPresentation : MonoBehaviour
{
    [Header("Existing art")]
    [SerializeField] private Sprite buffIcon;
    [SerializeField] private TMP_FontAsset font;
    [Header("World visual")]
    [SerializeField] private float diameter = 1.65f;
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 0.12f, 0f);
    [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.28f;
    [Header("HUD - canvas units at reference width 1080")]
    [SerializeField] private float topInset = 360f;
    [SerializeField] private float rightInset = 24f;

    private PlayerSkillController skills;
    private PlayerStatus status;
    private SpriteRenderer shell;
    private SpriteRenderer echo;
    private Vector3 shellScale;
    private GameObject visualRoot;
    private RectTransform safeRoot;
    private RectTransform container;
    private GameObject tile;
    private Image border;
    private TMP_Text seconds;
    private Image timeFill;
    private const float BadgeWidth = 304f;
    private const float BadgeHeight = 136f;
    private Canvas canvas;
    private Graphic[] existingHUD;
    private Vector3[] hudLayoutScales;
    private readonly Vector3[] corners = new Vector3[4];
    private bool wasActive;
    private bool built;
    private float activationAge = 1f;
    private float flash;
    private float visibility;
    private int lastHP;
    private int lastMaxHP;
    private int lastSeconds = -1;

    private void Awake() {
        skills = GetComponent<PlayerSkillController>();
        var grid = GetComponent<PlayerGridController>();
        status = grid != null && grid.Status != null ? grid.Status : GetComponent<PlayerStatus>();
    }

    private void OnEnable() {
        if (status == null) return;
        lastHP = status.CurrentHP;
        lastMaxHP = status.MaxHP;
        status.OnHPChanged += HandleHPChanged;
        status.OnPlayerDied += HideImmediately;
    }

    private void Start() {
        if (status == null || skills == null) return;
        BuildWorldVisual();
        BuildHUD();
        built = true;
    }

    private void BuildWorldVisual() {
        var source = skills.ShieldVisualSource != null
            ? skills.ShieldVisualSource.GetComponent<SpriteRenderer>() : null;
        if (source == null || source.sprite == null) {
            Debug.LogWarning("[Shield] Missing existing shield sprite; world visual unavailable.", this);
            return;
        }

        visualRoot = new GameObject("ShieldAura (reused)");
        visualRoot.transform.SetParent(transform, false);
        visualRoot.transform.localPosition = centerOffset;
        shell = CreateLayer("Ward", source, 0);
        echo = CreateLayer("ActivationEcho", source, 1);
        float extent = Mathf.Max(source.sprite.bounds.size.x, source.sprite.bounds.size.y);
        shellScale = Vector3.one * (diameter / Mathf.Max(0.001f, extent));
        visualRoot.SetActive(false);
    }

    private SpriteRenderer CreateLayer(string name, SpriteRenderer source, int order) {
        var layer = new GameObject(name);
        layer.transform.SetParent(visualRoot.transform, false);
        var renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + order;
        renderer.color = Color.clear;
        return renderer;
    }

    private void BuildHUD() {
        var hud = FindObjectOfType<HUDController>();
        canvas = hud != null ? hud.GetComponentInParent<Canvas>() : null;
        if (canvas == null || buffIcon == null || font == null) {
            Debug.LogWarning("[Shield] Missing HUD canvas/icon/font; buff HUD unavailable.", this);
            return;
        }
        existingHUD = hud.GetComponentsInChildren<Graphic>(true);
        hudLayoutScales = new Vector3[existingHUD.Length];
        for (int i = 0; i < existingHUD.Length; i++)
            hudLayoutScales[i] = existingHUD[i].rectTransform.localScale;
        safeRoot = Rect("BuffSafeArea", canvas.transform);
        container = Rect("ActiveBuffs", safeRoot);
        container.anchorMin = container.anchorMax = container.pivot = Vector2.one;
        container.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);
        var layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.spacing = 10f;

        var tileRect = Rect("ShieldBuff", container);
        tile = tileRect.gameObject;
        tileRect.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);
        border = tile.AddComponent<Image>();
        border.color = new Color(0.32f, 0.65f, 0.86f, 1f);
        border.raycastTarget = false;
        var background = Rect("OpaqueNavyPlate", tileRect);
        background.offsetMin = new Vector2(3f, 3f);
        background.offsetMax = new Vector2(-3f, -3f);
        var plate = background.gameObject.AddComponent<Image>();
        plate.color = new Color(0.035f, 0.055f, 0.11f, 1f);
        plate.raycastTarget = false;

        var iconRect = Area("ShieldIcon", background, new Vector2(0f, 0.2f), new Vector2(0.32f, 1f), 12f);
        var icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = buffIcon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Label("BuffName", background, "보호막", 28f,
            new Vector2(0.34f, 0.63f), new Vector2(0.95f, 0.94f),
            new Color(0.65f, 0.85f, 1f));
        seconds = Label("RemainingTime", background, "", 46f,
            new Vector2(0.34f, 0.24f), new Vector2(0.95f, 0.65f), Color.white);

        var trackRect = Area("DurationTrack", background, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.16f));
        var track = trackRect.gameObject.AddComponent<Image>();
        track.color = new Color(0.1f, 0.18f, 0.28f, 1f);
        track.raycastTarget = false;
        var fillRect = Rect("RemainingDuration", trackRect);
        fillRect.pivot = new Vector2(0f, 0.5f);
        timeFill = fillRect.gameObject.AddComponent<Image>();
        timeFill.color = new Color(0.35f, 0.8f, 1f, 1f);
        timeFill.raycastTarget = false;
        tile.SetActive(false);
        UpdateSafeArea();
    }

    private static RectTransform Area(string name, Transform parent, Vector2 min, Vector2 max, float padding = 0f) {
        var rect = Rect(name, parent);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
        return rect;
    }

    private TMP_Text Label(string name, Transform parent, string text, float size, Vector2 min, Vector2 max, Color color) {
        var label = Area(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent) {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void LateUpdate() {
        if (!built) return;
        bool active = skills.isActiveAndEnabled && skills.IsShieldActive && !status.IsDead;
        if (active && !wasActive) {
            activationAge = 0f;
            if (visualRoot != null) visualRoot.SetActive(true);
        }
        wasActive = active;
        activationAge += Time.deltaTime;
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 6f);
        visibility = Mathf.MoveTowards(visibility, active ? 1f : 0f, Time.deltaTime * (active ? 12f : 5f));
        if (shell != null) {
            float arrival = Mathf.Clamp01(activationAge / 0.18f);
            float breathe = Mathf.Sin(Time.time * 2.4f);
            shell.transform.localScale = shellScale * Mathf.Lerp(0.65f, 1f + breathe * 0.015f, 1f - Mathf.Pow(1f - arrival, 3f));
            shell.color = new Color(0.7f + flash * 0.3f, 0.85f + flash * 0.15f, 1f,
                visibility * (idleAlpha + breathe * 0.025f + flash * 0.4f));
            float burst = Mathf.Clamp01(activationAge / 0.32f);
            echo.transform.localScale = shellScale * Mathf.Lerp(0.6f, 1.22f, burst);
            echo.color = new Color(0.75f, 0.85f, 1f, active ? (1f - burst) * 0.45f : 0f);
            if (!active && visibility <= 0f) visualRoot.SetActive(false);
        }
        if (tile != null) {
            tile.SetActive(active);
            if (active) {
                int value = Mathf.CeilToInt(skills.ShieldRemainingTime);
                if (value != lastSeconds) {
                    lastSeconds = value;
                    seconds.SetText("{0}초", value);
                }
                float ratio = Mathf.Clamp01(skills.ShieldRemainingTime / Mathf.Max(0.001f, skills.ShieldDuration));
                timeFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
                Color accent = value <= 3 ? new Color(1f, 0.72f, 0.35f) : new Color(0.35f, 0.8f, 1f);
                timeFill.color = accent;
                seconds.color = value <= 3 ? accent : Color.white;
                border.color = Color.Lerp(accent, Color.white, flash);
                UpdateSafeArea();
            }
        }
    }

    private void UpdateSafeArea() {
        if (safeRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
        var area = Screen.safeArea;
        safeRoot.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        // Preserve the existing HUD's top band; scale the new badge for narrow screens.
        float scale = Mathf.Min(1f, safeRoot.rect.width / 1080f);
        container.localScale = Vector3.one * scale;
        float inset = topInset * scale;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        // The existing HP/skill bar uses centered anchors. Its screen position shifts
        // on tall phones, so reserve its actual visible bounds rather than a guess.
        for (int i = 0; i < existingHUD.Length; i++) {
            var graphic = existingHUD[i];
            if (graphic == null || !graphic.isActiveAndEnabled) continue;
            var rect = graphic.rectTransform;
            rect.GetLocalCorners(corners);
            // Item pickup punches the skill icon's scale. Measure its resting
            // layout instead so the buff timer does not follow that animation.
            Vector3 restingCorner = rect.localPosition + rect.localRotation *
                Vector3.Scale(corners[0], hudLayoutScales[i]);
            Vector3 worldBottom = rect.parent.TransformPoint(restingCorner);
            Vector2 screenBottom = RectTransformUtility.WorldToScreenPoint(uiCamera, worldBottom);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot, screenBottom, uiCamera, out Vector2 local))
                inset = Mathf.Max(inset, safeRoot.rect.yMax - local.y + 12f * scale);
        }
        container.anchoredPosition = new Vector2(-rightInset * scale, -inset);
        // Ensure the badge remains inside even unusually short safe areas.
        float height = safeRoot.rect.height;
        if (inset + BadgeHeight * scale > height)
            container.anchoredPosition = new Vector2(-rightInset * scale, -Mathf.Max(0f, height - BadgeHeight * scale));
    }

    private void HandleHPChanged(int hp, int maxHP) {
        // Max-HP card penalties can also lower current HP; those are not hits.
        if (hp < lastHP && maxHP == lastMaxHP && skills.IsShieldActive) flash = 1f;
        lastHP = hp;
        lastMaxHP = maxHP;
    }

    private void HideImmediately() {
        wasActive = false;
        visibility = flash = 0f;
        lastSeconds = -1;
        if (visualRoot != null) visualRoot.SetActive(false);
        if (tile != null) tile.SetActive(false);
    }

    private void OnDisable() {
        if (status != null) {
            status.OnHPChanged -= HandleHPChanged;
            status.OnPlayerDied -= HideImmediately;
        }
        HideImmediately();
    }

    private void OnDestroy() {
        if (safeRoot != null) Destroy(safeRoot.gameObject);
        if (visualRoot != null) Destroy(visualRoot);
    }
}

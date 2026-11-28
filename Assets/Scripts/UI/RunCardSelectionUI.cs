using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunCardSelectionUI : MonoBehaviour
{
    [SerializeField] private RunProgression progression;
    [SerializeField] private GameFlowController gameFlow;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private RunCardData[] cardPool;
    [SerializeField] private RunCardRaritySettings raritySettings;
    [SerializeField] private MonoBehaviour selectionHandler;

    [Header("Lucid Fragments - Presentation")]
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private Sprite commonFrame;
    [SerializeField] private Sprite attackIcon;
    [SerializeField] private Sprite defenseIcon;
    [SerializeField] private Sprite maxHealthIcon;
    [SerializeField] private Sprite healIcon;
    [SerializeField] private Sprite attackAndDefenseIcon;

    [Header("Nightmare - Dedicated Icons")]
    [SerializeField] private Sprite nightmareAttackIcon;
    [SerializeField] private Sprite nightmareDefenseIcon;
    [SerializeField] private Sprite nightmareMaxHealthIcon;
    [SerializeField] private Sprite nightmareHealIcon;
    [SerializeField] private Sprite nightmareAttackAndDefenseIcon;

    public event Action<RunCardData> OnCardSelected;
    public bool IsOpen => root != null && root.activeSelf;
    public bool IsReady => configured && !configurationFailed && isActiveAndEnabled &&
        parentCanvas != null && parentCanvas.isActiveAndEnabled && EventSystem.current != null &&
        selectionHandler != null && selectionHandler.isActiveAndEnabled;

    private GameObject root;
    private TMP_Text remainingChoices;
    private RectTransform safeArea;
    private readonly Button[] buttons = new Button[3];
    private readonly TMP_Text[] names = new TMP_Text[3];
    private readonly TMP_Text[] effects = new TMP_Text[3];
    private readonly Image[] icons = new Image[3];
    private readonly RectTransform[] iconContainers = new RectTransform[3];
    private const float IconDisplayScale = 1.3f;
    private const float NightmareIconSizeMultiplier = 1.3f;
    private readonly Image[] frames = new Image[3];
    private readonly List<RunCardData> candidates = new List<RunCardData>();
    private RunCardData[] offeredCards;
    private int offerVersion;
    private int offerFrame;
    private bool acceptingSelection;
    private int selectionAttemptFrame=-1;
    private bool configured;
    private bool configurationFailed;

    private void Start() {
        if (progression == null) progression = RunProgression.Instance;
        if (gameFlow == null) gameFlow = GameFlowController.Instance;
        if (progression == null || gameFlow == null || parentCanvas == null ||
            font == null || koreanFont == null || commonFrame == null ||
            attackIcon == null || defenseIcon == null || maxHealthIcon == null ||
            healIcon == null || attackAndDefenseIcon == null || EventSystem.current == null ||
            !(selectionHandler is IRunCardSelectionHandler)) {
            Debug.LogError("[RunCards] Missing UI references or invalid selection handler.");
            enabled = false;
            return;
        }

        if (cardPool != null) candidates.AddRange(cardPool);
        if (!RunCardSampler.CanDrawThree(candidates, raritySettings, out string error)) {
            ReportConfigurationError(error);
            return;
        }

        BuildUI();
        gameFlow.OnStateChanged += HandleStateChanged;
        Canvas.willRenderCanvases += AlignIconsToSlots;
        configured = true;
    }

    private void Update() {
        if(!configured || !IsOpen || !acceptingSelection || gameFlow==null ||
            gameFlow.CurrentState!=GameState.LevelUpSelection || Time.frameCount<=offerFrame)return;
        if(UiNumberShortcuts.TryRead(out int number))SelectCard(number-1,offerVersion);
    }
    private void LateUpdate() {
        if (!configured || configurationFailed) return;
        if (progression.PendingLevelUps > 0 &&
            !RunCardSampler.CanDrawThree(candidates, raritySettings, out string error)) {
            ReportConfigurationError(error);
            return;
        }
        if (IsOpen) UpdateSafeArea();
        if (gameFlow.CurrentState != GameState.LevelUpSelection) {
            if (IsOpen) Hide();
            if (progression.PendingLevelUps > 0)
                gameFlow.TryBeginLevelUpSelection();
        }
        if (gameFlow.CurrentState != GameState.LevelUpSelection) return;

        if (!gameFlow.CanConfirmRunCard) {
            Hide();
            gameFlow.EndLevelUpSelection();
            return;
        }

        if (progression.PendingLevelUps <= 0) {
            Hide();
            gameFlow.EndLevelUpSelection();
        }
        else if (!IsOpen) {
            root.SetActive(true);
            ShowNextOffer();
        }
    }

    private void ShowNextOffer() {
        offerVersion++;
        offerFrame = Time.frameCount;
        UiNumberShortcuts.BlockThisFrame();
        if (!RunCardSampler.TryDrawThree(candidates, raritySettings, out offeredCards, out string error)) {
            ReportConfigurationError(error);
            return;
        }
        UpdateSafeArea();
        remainingChoices.text = progression.PendingLevelUps > 1
            ? $"남은 선택 {progression.PendingLevelUps}회"
            : string.Empty;
        EventSystem.current.SetSelectedGameObject(null);
        for (int i = 0; i < buttons.Length; i++) {
            int index = i;
            int version = offerVersion;
            bool available=offeredCards!=null && i<offeredCards.Length && offeredCards[i]!=null;
            buttons[i].gameObject.SetActive(available);
            if(!available){buttons[i].interactable=false;continue;}
            RunCardData card = offeredCards[i];
            names[i].text = card.DisplayName;
            effects[i].text = card.HasPenalty
                ? card.PositiveEffectDescription + "\n<color=#FF8F9C>" + card.PenaltyDescription + "</color>"
                : card.EffectDescription;
            Sprite icon = GetIcon(card);
            if (icon == null) {
                ReportConfigurationError($"Missing icon for {card.Rarity}/{card.EffectType} ({card.Id}).");
                return;
            }
            icons[i].sprite = icon;
            // Nightmare originals have more transparent padding: enlarge their current display only.
            float iconScale = IconDisplayScale *
                (card.Rarity == RunCardRarity.Nightmare ? NightmareIconSizeMultiplier : 1f);
            icons[i].rectTransform.localScale = Vector3.one * iconScale;
            frames[i].sprite = raritySettings.GetFrame(card.Rarity);
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => SelectCard(index, version));
            buttons[i].interactable = true;
        }
        acceptingSelection = true;
    }

    private void ReportConfigurationError(string error) {
        if (configurationFailed) return;
        configurationFailed = true;
        Hide();
        if (gameFlow != null) gameFlow.EndLevelUpSelection();
        Debug.LogError($"[RunCards] {error} Fix the card/settings assets and restart the scene.", this);
    }

    private void SelectCard(int index, int version) {
        if (!acceptingSelection || !IsOpen || version != offerVersion ||
            Time.frameCount <= offerFrame || gameFlow.CurrentState != GameState.LevelUpSelection ||
            !gameFlow.CanConfirmRunCard || progression.PendingLevelUps <= 0 ||
            offeredCards == null || index < 0 || index >= offeredCards.Length ||
            offeredCards[index] == null || index>=buttons.Length || buttons[index]==null ||
            !buttons[index].isActiveAndEnabled || !buttons[index].gameObject.activeInHierarchy ||
            !buttons[index].IsInteractable() || selectionAttemptFrame==Time.frameCount)
            return;

        selectionAttemptFrame=Time.frameCount;
        UiNumberShortcuts.BlockThisFrame();
        acceptingSelection = false;
        foreach (Button button in buttons) button.interactable = false;
        RunCardData selected = offeredCards[index];
        var handler = selectionHandler as IRunCardSelectionHandler;
        try {
            if (handler == null || !handler.TrySelectCard(selected)) {
                acceptingSelection = true;
                foreach (Button button in buttons) button.interactable = true;
                return;
            }
        }
        catch (Exception exception) {
            Debug.LogException(exception, this);
            acceptingSelection = true;
            foreach (Button button in buttons) button.interactable = true;
            return;
        }

        // The handler has already committed the effect and consumed the reward.
        Debug.Log($"[RunCards] Applied {selected.Id} / Pending {progression.PendingLevelUps}");
        if (gameFlow.CanConfirmRunCard && progression.PendingLevelUps > 0) ShowNextOffer();
        else {
            Hide();
            gameFlow.EndLevelUpSelection();
        }
        OnCardSelected?.Invoke(selected);
    }

    private void HandleStateChanged(GameState state) {
        if (state != GameState.LevelUpSelection) Hide();
    }

    private void Hide() {
        if(IsOpen)UiNumberShortcuts.BlockThisFrame();
        acceptingSelection = false;
        offeredCards = null;
        offerVersion++;
        if (root != null) root.SetActive(false);
    }

    private void OnDisable() {
        Hide();
        if (configured && gameFlow != null) gameFlow.EndLevelUpSelection();
    }

    private void OnDestroy() {
        Canvas.willRenderCanvases -= AlignIconsToSlots;
        if (gameFlow != null) gameFlow.OnStateChanged -= HandleStateChanged;
        if (root != null) Destroy(root);
    }

    private Sprite GetIcon(RunCardData card) {
        if (card.Rarity != RunCardRarity.Nightmare) return GetIcon(card.EffectType);
        switch (card.EffectType) {
            case RunCardEffectType.Attack: return nightmareAttackIcon;
            case RunCardEffectType.Defense: return nightmareDefenseIcon;
            case RunCardEffectType.MaxHealth: return nightmareMaxHealthIcon;
            case RunCardEffectType.Heal: return nightmareHealIcon;
            case RunCardEffectType.AttackAndDefense: return nightmareAttackAndDefenseIcon;
            default: return null;
        }
    }

    private Sprite GetIcon(RunCardEffectType type) {
        switch (type) {
            case RunCardEffectType.Attack: return attackIcon;
            case RunCardEffectType.Defense: return defenseIcon;
            case RunCardEffectType.MaxHealth: return maxHealthIcon;
            case RunCardEffectType.Heal: return healIcon;
            case RunCardEffectType.AttackAndDefense: return attackAndDefenseIcon;
            default: return null;
        }
    }

    private void BuildUI() {
        RectTransform overlay = CreateRect("LucidFragments", parentCanvas.transform,
            Vector2.zero, Vector2.one);
        root = overlay.gameObject;
        var canvas = root.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<Image>().color = new Color(0.025f, 0.03f, 0.065f, 0.97f);

        safeArea = CreateRect("SafeArea", overlay, Vector2.zero, Vector2.one);
        RectTransform content = CreateRect("Content", safeArea,
            new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.965f));
        // Fit the entire composition, including all three cards, inside any Game View.
        var contentAspect = content.gameObject.AddComponent<AspectRatioFitter>();
        contentAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        contentAspect.aspectRatio = 1000f / 1250f;

        TMP_Text title = CreateLabel("Title", content, new Vector2(0.05f, 0.875f),
            new Vector2(0.95f, 0.955f), 48, font, TextAlignmentOptions.Center);
        title.text = "LUCID FRAGMENTS";
        title.color = new Color(0.76f, 0.68f, 1f, 1f);
        TMP_Text guide = CreateLabel("Guide", content, new Vector2(0.05f, 0.79f),
            new Vector2(0.95f, 0.87f), 42, koreanFont, TextAlignmentOptions.Center);
        guide.text = "꿈의 파편을 선택하세요";

        for (int i = 0; i < buttons.Length; i++) {
            float center = 0.665f - i * 0.20f;
            RectTransform card = CreateRect("Card" + (i + 1), content,
                new Vector2(0.03f, center), new Vector2(0.97f, center));
            card.pivot = new Vector2(0.5f, 0.5f);
            var aspect = card.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            aspect.aspectRatio = commonFrame.rect.width / commonFrame.rect.height;

            // A transparent raycast surface makes even the frame's holes clickable.
            card.gameObject.AddComponent<Image>().color = Color.clear;
            RectTransform fill = CreateRect("Interior", card, new Vector2(0.025f, 0.18f),
                new Vector2(0.975f, 0.82f));
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.055f, 0.065f, 0.13f, 1f);
            fillImage.raycastTarget = false;
            Image frame = CreateSprite("CommonFrame", card, Vector2.zero, Vector2.one, commonFrame);
            frames[i] = frame;
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.86f, 0.90f, 1f, 1f);
            colors.pressedColor = new Color(0.65f, 0.72f, 0.90f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            buttons[i] = button;

            // All slots share the original display size; only the measured center varies.
            iconContainers[i] = CreateRect("IconContainer", frame.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            iconContainers[i].pivot = new Vector2(0.5f, 0.5f);
            icons[i] = CreateSprite("EffectIcon", iconContainers[i],
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), null);
            icons[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icons[i].rectTransform.localScale = Vector3.one * IconDisplayScale;
            names[i] = CreateLabel("Name", card, new Vector2(0.255f, 0.53f),
                new Vector2(0.925f, 0.77f), 36, koreanFont, TextAlignmentOptions.MidlineLeft);
            effects[i] = CreateLabel("Effect", card, new Vector2(0.255f, 0.235f),
                new Vector2(0.925f, 0.49f), 28, koreanFont, TextAlignmentOptions.MidlineLeft);
            effects[i].color = new Color(0.77f, 0.81f, 0.91f, 1f);
        }

        remainingChoices = CreateLabel("RemainingChoices", content, new Vector2(0.06f, 0.135f),
            new Vector2(0.94f, 0.175f), 24, koreanFont, TextAlignmentOptions.Center);
        remainingChoices.color = new Color(0.76f, 0.68f, 1f, 1f);
        TMP_Text footer = CreateLabel("Footer", content, new Vector2(0.04f, 0.055f),
            new Vector2(0.96f, 0.12f), 26, koreanFont, TextAlignmentOptions.Center);
        footer.text = "선택한 힘은 이번 탐험 동안 유지됩니다.";
        footer.color = new Color(0.67f, 0.72f, 0.83f, 1f);
        UpdateSafeArea();
        root.SetActive(false);
    }

    // Transparent-slot bounding-box centers measured from CardFrames.png (2172 x 724).
    // Coordinates are texture pixels from bottom-left, not hand-tuned rarity offsets.
    // Subtracting sprite.rect.position also accounts for the current Sprite Editor crop.
    private static bool TryGetSlotCenter(Sprite sprite, out Vector2 center) {
        Vector2 textureCenter;
        switch (sprite.name) {
            case "Normal": textureCenter = new Vector2(160.5f, 600f); break;
            case "rare": textureCenter = new Vector2(1239f, 600f); break;
            case "epic": textureCenter = new Vector2(160.5f, 363f); break;
            case "legendary": textureCenter = new Vector2(1239.5f, 363.5f); break;
            case "Lucid": textureCenter = new Vector2(160.5f, 125.5f); break;
            default: center = Vector2.zero; return false;
        }
        center = textureCenter - sprite.rect.position;
        return true;
    }

    private static Vector2 SlotToLocal(Rect displayRect, Rect spriteRect,
        Vector2 pivot, Vector2 slotCenter) {
        float scale = Mathf.Min(displayRect.width / spriteRect.width,
            displayRect.height / spriteRect.height);
        Vector2 renderedSize = spriteRect.size * scale;
        Vector2 padding = Vector2.Scale(displayRect.size - renderedSize, pivot);
        return displayRect.position + padding + slotCenter * scale;
    }

    // Runs after the normal canvas layout pass, including while timeScale is zero.
    // No pixel reads, asset changes, or forced/recursive canvas rebuilds are needed.
    private void AlignIconsToSlots() {
        if (!IsOpen) return;
        for (int i = 0; i < icons.Length; i++) {
            Image frame = frames[i];
            if (frame == null || frame.sprite == null || iconContainers[i] == null ||
                !TryGetSlotCenter(frame.sprite, out Vector2 slotCenter)) continue;
            Rect bounds = frame.GetPixelAdjustedRect();
            if (bounds.width <= 0f || bounds.height <= 0f) continue;

            RectTransform container = iconContainers[i];
            container.anchorMin = frame.rectTransform.pivot;
            container.anchorMax = frame.rectTransform.pivot;
            container.anchoredPosition = SlotToLocal(bounds, frame.sprite.rect,
                frame.rectTransform.pivot, slotCenter);
            // Preserve the previous base width/height for all three card instances.
            Vector2 baseSize = Vector2.Scale(frame.rectTransform.rect.size,
                new Vector2(0.063f, 0.30f));
            container.sizeDelta = baseSize;
            icons[i].rectTransform.sizeDelta = baseSize;
        }
    }

    private void UpdateSafeArea() {
        if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
    }

    private static Image CreateSprite(string name, Transform parent, Vector2 min, Vector2 max, Sprite sprite) {
        RectTransform rect = CreateRect(name, parent, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max) {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.gameObject.layer = parent.gameObject.layer;
        return rect;
    }

    private TMP_Text CreateLabel(string name, Transform parent, Vector2 min, Vector2 max,
        float size, TMP_FontAsset labelFont, TextAlignmentOptions alignment) {
        RectTransform rect = CreateRect(name, parent, min, max);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = labelFont;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12;
        text.fontSizeMax = size;
        text.alignment = alignment;
        text.overflowMode = TextOverflowModes.Truncate;
        text.enableWordWrapping = true;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}

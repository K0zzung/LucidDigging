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
    [SerializeField] private MonoBehaviour selectionHandler;

    [Header("Lucid Fragments - Presentation")]
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private Sprite commonFrame;
    [SerializeField] private Sprite attackIcon;
    [SerializeField] private Sprite defenseIcon;
    [SerializeField] private Sprite maxHealthIcon;
    [SerializeField] private Sprite healIcon;
    [SerializeField] private Sprite attackAndDefenseIcon;

    public event Action<RunCardData> OnCardSelected;
    public bool IsOpen => root != null && root.activeSelf;
    public bool IsReady => configured && isActiveAndEnabled &&
        parentCanvas != null && parentCanvas.isActiveAndEnabled && EventSystem.current != null &&
        selectionHandler != null && selectionHandler.isActiveAndEnabled;

    private GameObject root;
    private TMP_Text remainingChoices;
    private RectTransform safeArea;
    private readonly Button[] buttons = new Button[3];
    private readonly TMP_Text[] names = new TMP_Text[3];
    private readonly TMP_Text[] effects = new TMP_Text[3];
    private readonly Image[] icons = new Image[3];
    private readonly List<RunCardData> candidates = new List<RunCardData>();
    private RunCardData[] offeredCards;
    private int offerVersion;
    private int offerFrame;
    private bool acceptingSelection;
    private bool configured;

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

        var ids = new HashSet<string>();
        if (cardPool != null) {
            foreach (RunCardData card in cardPool) {
                if (card != null && card.HasValidEffect && !string.IsNullOrWhiteSpace(card.Id) && ids.Add(card.Id))
                    candidates.Add(card);
            }
        }
        if (candidates.Count < 3) {
            Debug.LogError("[RunCards] At least three cards with unique IDs are required.");
            enabled = false;
            return;
        }

        BuildUI();
        gameFlow.OnStateChanged += HandleStateChanged;
        configured = true;
    }

    private void LateUpdate() {
        if (!configured) return;
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
        offeredCards = DrawThree(candidates);
        UpdateSafeArea();
        remainingChoices.text = progression.PendingLevelUps > 1
            ? $"남은 선택 {progression.PendingLevelUps}회"
            : string.Empty;
        EventSystem.current.SetSelectedGameObject(null);
        for (int i = 0; i < buttons.Length; i++) {
            int index = i;
            int version = offerVersion;
            RunCardData card = offeredCards[i];
            names[i].text = card.DisplayName;
            effects[i].text = card.EffectDescription;
            icons[i].sprite = GetIcon(card.EffectType);
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => SelectCard(index, version));
            buttons[i].interactable = true;
        }
        acceptingSelection = true;
    }

    // Partial Fisher-Yates: each offer contains three distinct card IDs.
    public static RunCardData[] DrawThree(IReadOnlyList<RunCardData> pool) {
        if (pool == null || pool.Count < 3)
            throw new ArgumentException("At least three unique cards are required.", nameof(pool));
        var remaining = new List<RunCardData>(pool);
        var result = new RunCardData[3];
        for (int i = 0; i < result.Length; i++) {
            int index = UnityEngine.Random.Range(i, remaining.Count);
            RunCardData swap = remaining[i];
            remaining[i] = remaining[index];
            remaining[index] = swap;
            result[i] = remaining[i];
        }
        return result;
    }

    private void SelectCard(int index, int version) {
        if (!acceptingSelection || !IsOpen || version != offerVersion ||
            Time.frameCount <= offerFrame || gameFlow.CurrentState != GameState.LevelUpSelection ||
            !gameFlow.CanConfirmRunCard || progression.PendingLevelUps <= 0 ||
            offeredCards == null || index < 0 || index >= offeredCards.Length ||
            offeredCards[index] == null)
            return;

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
        if (gameFlow != null) gameFlow.OnStateChanged -= HandleStateChanged;
        if (root != null) Destroy(root);
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

            // Keep the square icon inside the circular opening of the supplied frame.
            icons[i] = CreateSprite("EffectIcon", card, new Vector2(0.103f, 0.35f),
                new Vector2(0.166f, 0.65f), null);
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

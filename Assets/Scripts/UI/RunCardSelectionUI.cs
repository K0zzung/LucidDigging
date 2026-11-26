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

    public event Action<RunCardData> OnCardSelected;
    public bool IsOpen => root != null && root.activeSelf;
    public bool IsReady => configured && isActiveAndEnabled &&
        parentCanvas != null && parentCanvas.isActiveAndEnabled && EventSystem.current != null;

    private GameObject root;
    private TMP_Text heading;
    private readonly Button[] buttons = new Button[3];
    private readonly TMP_Text[] labels = new TMP_Text[3];
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
            font == null || EventSystem.current == null ||
            (selectionHandler != null && !(selectionHandler is IRunCardSelectionHandler))) {
            Debug.LogError("[RunCards] Missing UI references or invalid selection handler.");
            enabled = false;
            return;
        }

        var ids = new HashSet<string>();
        if (cardPool != null) {
            foreach (RunCardData card in cardPool) {
                if (card != null && !string.IsNullOrWhiteSpace(card.Id) && ids.Add(card.Id))
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
        heading.text = $"LEVEL UP  /  {progression.PendingLevelUps} choice(s) remaining\n" +
            "UI TEST ONLY - No stat effects are applied";
        EventSystem.current.SetSelectedGameObject(null);
        for (int i = 0; i < buttons.Length; i++) {
            int index = i;
            int version = offerVersion;
            RunCardData card = offeredCards[i];
            labels[i].text = card.DisplayName + "\n\n" + card.Description;
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
            if (handler != null && !handler.TrySelectCard(selected)) {
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

        if (!gameFlow.CanConfirmRunCard || !progression.TryConsumePendingLevelUp()) return;
        Debug.Log($"[RunCards] Selected {selected.Id} (UI test; no stat effect).");
        if (progression.PendingLevelUps > 0) ShowNextOffer();
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

    private void BuildUI() {
        RectTransform overlay = CreateRect("RunCardSelection", parentCanvas.transform,
            Vector2.zero, Vector2.one);
        root = overlay.gameObject;
        var canvas = root.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<Image>().color = new Color(0.025f, 0.025f, 0.04f, 0.96f);
        heading = CreateLabel("Heading", overlay, new Vector2(0.06f, 0.79f),
            new Vector2(0.94f, 0.95f), 34);
        for (int i = 0; i < buttons.Length; i++) {
            float top = 0.76f - i * 0.22f;
            RectTransform card = CreateRect("Card" + (i + 1), overlay,
                new Vector2(0.1f, top - 0.19f), new Vector2(0.9f, top));
            Image image = card.gameObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.17f, 0.23f, 1f);
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            buttons[i] = button;
            labels[i] = CreateLabel("Text", card, new Vector2(0.04f, 0.06f),
                new Vector2(0.96f, 0.94f), 32);
        }
        TMP_Text footer = CreateLabel("Footer", overlay, new Vector2(0.06f, 0.03f),
            new Vector2(0.94f, 0.1f), 26);
        footer.text = "Choose one card to continue. Effects arrive in P1-3.";
        root.SetActive(false);
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

    private TMP_Text CreateLabel(string name, Transform parent, Vector2 min, Vector2 max, float size) {
        RectTransform rect = CreateRect(name, parent, min, max);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}

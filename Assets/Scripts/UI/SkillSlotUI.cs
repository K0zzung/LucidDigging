using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private RectTransform badgeRoot;
    private RectTransform chargeBadge;

    [Header("Locked")]
    [SerializeField]
    private Color lockedIconColor = new Color(0.25f, 0.25f, 0.25f, 0.4f);

    [Header("Unlocked")]
    [SerializeField]
    private Color unlockedIconColor = Color.white;

    private Vector3 originalIconScale;

    [Header("Unlock Effect")]
    [SerializeField]
    private float punchStrength = 0.15f;

    [SerializeField]
    private float punchDuration = 0.2f;


    private void Awake() {
        if (skillIcon != null) {
            originalIconScale = skillIcon.transform.localScale;
        }
    }


    public void SetUnlocked(bool unlocked) {
        if (skillIcon == null)
            return;

        skillIcon.color =
            unlocked
                ? unlockedIconColor
                : lockedIconColor;

        // »πµÊ«ﬂ¿ª ∂ß∏∏ Punch
        if (unlocked) {
            PlayUnlockEffect();
        }
    }

    private TMP_Text runStateText;
    private int shownCharges;
    public void SetRunState(int charges, int evolution, TMP_FontAsset font) {
        if (skillIcon == null) return;
        if (runStateText == null) {
            var label = new GameObject("RunSkillState", typeof(RectTransform));
            var rect = label.GetComponent<RectTransform>();
            rect.SetParent(badgeRoot != null ? badgeRoot : skillIcon.rectTransform.parent, false);
            chargeBadge = rect;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(80f, 48f);
            rect.anchoredPosition = new Vector2(-4f, 4f);
            var edge = label.AddComponent<Image>();
            edge.color = new Color(0.76f, 0.84f, 1f, 1f);
            edge.raycastTarget = false;
            var inset = new GameObject("Inset", typeof(RectTransform));
            var insetRect = inset.GetComponent<RectTransform>();
            insetRect.SetParent(rect, false);
            insetRect.anchorMin = Vector2.zero; insetRect.anchorMax = Vector2.one;
            insetRect.offsetMin = new Vector2(2f, 2f); insetRect.offsetMax = new Vector2(-2f, -2f);
            var fill = inset.AddComponent<Image>();
            fill.color = new Color(0.04f, 0.035f, 0.12f, 1f);
            fill.raycastTarget = false;
            var textObject = new GameObject("Text", typeof(RectTransform));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(2f, 0f); textRect.offsetMax = new Vector2(-2f, 0f);
            runStateText = textObject.AddComponent<TextMeshProUGUI>();
            runStateText.font = font;
            runStateText.fontSize = 38f;
            runStateText.enableAutoSizing = false;
            runStateText.fontSizeMin = 9f; runStateText.fontSizeMax = 13f;
            runStateText.alignment = TextAlignmentOptions.Center;
            runStateText.raycastTarget = false;
        }
        skillIcon.color = charges > 0 ? unlockedIconColor : lockedIconColor;
        runStateText.text = $"x{charges}";
        runStateText.transform.parent.gameObject.SetActive(charges > 0);
        runStateText.color = charges > 0 ? new Color(1f, 0.97f, 0.87f) : new Color(0.6f, 0.65f, 0.75f);
        if (charges > shownCharges) PlayUnlockEffect();
        shownCharges = charges;
    }

    private void PlayUnlockEffect() {
        if (skillIcon == null)
            return;

        Transform iconTransform = skillIcon.transform;

        iconTransform.DOKill();

        iconTransform.localScale = originalIconScale;

        Vector3 punchAmount = originalIconScale* punchStrength;

        iconTransform.DOPunchScale(punchAmount,punchDuration, 5, 0.5f);
    }
    private void OnDestroy() {
        if (chargeBadge != null) Destroy(chargeBadge.gameObject);
        transform.DOKill();
    }
}
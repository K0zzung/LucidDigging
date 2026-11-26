using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image skillIcon;

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
        transform.DOKill();
    }
}
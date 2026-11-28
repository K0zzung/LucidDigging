using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PermanentUpgradeRowUI : MonoBehaviour
{
    // =========================================================
    // Upgrade Setting
    // =========================================================

    [Header("Upgrade Setting")]

    [SerializeField]
    private PermanentUpgradeType upgradeType;

    [SerializeField]
    private CurrencyType currencyType;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private TMP_Text levelText;

    [SerializeField]
    private TMP_Text costText;

    [SerializeField]
    private Button upgradeButton;

    [SerializeField]
    private TMP_Text currentValueText;

    [Tooltip("재화 부족 시 반투명해질 영역")]
    [SerializeField]
    private CanvasGroup upgradeButtonCanvasGroup;


    // =========================================================
    // Setting
    // =========================================================

    [Header("Visual")]

    [Range(0f, 1f)]
    [SerializeField]
    private float disabledAlpha = 0.35f;

    [Header("Upgrade Animation")]

    [SerializeField]
    private float textPunchScale = 0.18f;

    [SerializeField]
    private float textPunchDuration = 0.25f;


    public const float CardHeight=204f,CardGap=14f;
    private TMP_Text buyCaption;
    private int blockedPurchaseFrame=-1;
    public PermanentUpgradeType UpgradeType => upgradeType;
    public Button UpgradeButton => upgradeButton;
    public void BlockPurchaseThisFrame() {blockedPurchaseFrame=Time.frameCount;}
    public void ConfigureAsPickaxe() {
        upgradeType=PermanentUpgradeType.PickaxeAttack;currencyType=CurrencyType.Dash;
        // Current-value labels in the old scene live outside the source row.
        currentValueText=null;
        upgradeButton.onClick=new Button.ButtonClickedEvent();
        upgradeButton.onClick.AddListener(OnClickUpgrade);
    }
    public void BuildCategoryLayout(RectTransform parent,int index,TMP_FontAsset pixelFont) {
        var row=(RectTransform)transform;row.SetParent(parent,false);row.localScale=Vector3.one;
        row.anchorMin=row.anchorMax=row.pivot=new Vector2(.5f,1f);
        row.anchoredPosition=new Vector2(0,-index*(CardHeight+CardGap));row.sizeDelta=new Vector2(900,CardHeight);
        for(int i=0;i<row.childCount;i++)row.GetChild(i).gameObject.SetActive(false);
        var background=gameObject.GetComponent<Image>();if(background==null)background=gameObject.AddComponent<Image>();
        background.color=new Color(.13f,.1f,.22f,1);background.raycastTarget=false;
        if(currentValueText==null)currentValueText=ShopUpgradeCategories.CreateText("Effect",row,pixelFont,24);
        if(levelText==null)levelText=ShopUpgradeCategories.CreateText("Level",row,pixelFont,22);
        if(costText==null)costText=ShopUpgradeCategories.CreateText("Cost",row,pixelFont,22);
        SetupText(currentValueText,row,pixelFont,new Vector2(20,-64),new Vector2(860,32),24);
        SetupText(levelText,row,pixelFont,new Vector2(545,-20),new Vector2(335,32),22);
        SetupText(costText,row,pixelFont,new Vector2(20,-145),new Vector2(640,42),22);
        var title=ShopUpgradeCategories.CreateText("UpgradeTitle",row,pixelFont,30);
        SetupText(title,row,pixelFont,new Vector2(20,-16),new Vector2(500,40),30);
        title.text=Title(upgradeType);
        var detail=ShopUpgradeCategories.CreateText("Purpose",row,pixelFont,19);
        SetupText(detail,row,pixelFont,new Vector2(20,-104),new Vector2(860,28),19);
        detail.color=new Color(.66f,.76f,.85f);
        detail.text=upgradeType==PermanentUpgradeType.Attack?"몬스터·보스에 특화된 검의 영구 공격력":
            upgradeType==PermanentUpgradeType.PickaxeAttack?"모든 곡괭이에 공통 적용 · 터렛 공격 특화":"영구 강화 · 사망 후에도 유지";
        var buttonRect=(RectTransform)upgradeButton.transform;buttonRect.SetParent(row,false);
        buttonRect.anchorMin=buttonRect.anchorMax=buttonRect.pivot=new Vector2(1,1);
        buttonRect.anchoredPosition=new Vector2(-20,-136);buttonRect.sizeDelta=new Vector2(210,54);buttonRect.localScale=Vector3.one;
        upgradeButton.gameObject.SetActive(true);
        if(upgradeButton.targetGraphic!=null)upgradeButton.targetGraphic.raycastTarget=true;
        foreach(var graphic in upgradeButton.GetComponentsInChildren<Graphic>(true))
            if(graphic!=upgradeButton.targetGraphic)graphic.raycastTarget=false;
        buyCaption=ShopUpgradeCategories.CreateText("BuyCaption",buttonRect,pixelFont,23);
        var caption=buyCaption;
        var captionRect=caption.rectTransform;captionRect.anchorMin=Vector2.zero;captionRect.anchorMax=Vector2.one;
        captionRect.offsetMin=captionRect.offsetMax=Vector2.zero;caption.alignment=TextAlignmentOptions.Center;caption.text="강화";
        BlockPurchaseThisFrame();Refresh();
    }
    private static void SetupText(TMP_Text text,Transform parent,TMP_FontAsset font,Vector2 pos,Vector2 size,float fontsize) {
        var rect=text.rectTransform;rect.SetParent(parent,false);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;
        text.margin=Vector4.zero;text.characterSpacing=0;text.wordSpacing=0;text.lineSpacing=0;text.paragraphSpacing=0;
        text.alignment=TextAlignmentOptions.MidlineLeft;
        text.font=font;text.fontSize=fontsize;text.enableAutoSizing=false;text.enableWordWrapping=false;
        text.overflowMode=TextOverflowModes.Ellipsis;text.raycastTarget=false;text.color=Color.white;text.gameObject.SetActive(true);
    }
    private static string Title(PermanentUpgradeType type) {
        switch(type) {
            case PermanentUpgradeType.HP:return "최대 체력";
            case PermanentUpgradeType.Defense:return "방어력";
            case PermanentUpgradeType.Attack:return "무기 공격력";
            case PermanentUpgradeType.PickaxeAttack:return "곡괭이 공격력";
            default:return type.ToString();
        }
    }
    // =========================================================
    // Unity
    // =========================================================

    private void Start() {
        SubscribeEvents();

        Refresh();
    }


    private void OnEnable() {
        Refresh();
    }


    private void OnDestroy() {
        UnsubscribeEvents();
    }


    // =========================================================
    // Event
    // =========================================================

    private void SubscribeEvents() {
        if (CurrencyManager.Instance != null) {
            CurrencyManager.Instance.OnCurrencyChanged +=
                Refresh;
        }


        if (PermanentUpgradeManager.Instance != null) {
            PermanentUpgradeManager.Instance.OnUpgradeLevelChanged +=
                HandleUpgradeLevelChanged;
        }
    }


    private void UnsubscribeEvents() {
        if (CurrencyManager.Instance != null) {
            CurrencyManager.Instance.OnCurrencyChanged -=
                Refresh;
        }


        if (PermanentUpgradeManager.Instance != null) {
            PermanentUpgradeManager.Instance.OnUpgradeLevelChanged -=
                HandleUpgradeLevelChanged;
        }
    }


    private void HandleUpgradeLevelChanged(
        PermanentUpgradeType changedType,
        int level) {
        if (changedType !=
            upgradeType) {
            return;
        }


        Refresh();
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void Refresh() {
        if (PermanentUpgradeManager.Instance == null)
            return;


        int level =
            PermanentUpgradeManager.Instance
                .GetLevel(
                    upgradeType);


        int cost =
            PermanentUpgradeManager.Instance
                .GetUpgradeCost(
                    upgradeType);


        // =============================
        // Level
        // =============================

        if (levelText != null) {
            levelText.text =
                level==int.MaxValue?$"Lv.{level} MAX":$"Lv.{level} → Lv.{(long)level+1}";
        }


        // =============================
        // Cost
        // =============================

        if (costText != null) {
            costText.text =
                $"{(currencyType==CurrencyType.Dash?"분홍":currencyType==CurrencyType.Shockwave?"파랑":"노랑")} {cost} / 보유 { (CurrencyManager.Instance!=null?CurrencyManager.Instance.GetCurrency(currencyType):0)}";
        }


        // =============================
        // Current Value
        // =============================

        if (currentValueText != null) {
            currentValueText.text =
                PermanentUpgradeManager.Instance
                    .GetCurrentValueText(
                        upgradeType);
            int increase=PermanentUpgradeManager.Instance.GetIncreasePerLevel(upgradeType);
            if(increase>0)currentValueText.text+=$" (+{increase}/Lv)";
        }


        // =============================
        // Upgrade Button
        // =============================

        bool canUpgrade =
            PermanentUpgradeManager.Instance
                .CanUpgrade(
                    upgradeType,
                    currencyType);


        int balance=CurrencyManager.Instance!=null?CurrencyManager.Instance.GetCurrency(currencyType):0;
        bool maximum=level==int.MaxValue;
        bool insufficient=balance<cost;
        string state=maximum?"MAX":!PermanentUpgradeManager.Instance.IsShopOpen?"상점 닫힘":
            insufficient?"재화 부족":canUpgrade?"강화":"사용 불가";
        if(buyCaption!=null)buyCaption.text=state;
        if(costText!=null) {
            if(maximum)costText.text="최대 강화";
            costText.color=insufficient && !maximum?new Color(1f,.65f,.65f):Color.white;
        }
        if(upgradeButton!=null) {
            upgradeButton.interactable=canUpgrade;
            var colors=upgradeButton.colors;
            colors.normalColor=new Color(.72f,.55f,1f);
            colors.disabledColor=new Color(disabledAlpha*.9f,disabledAlpha*.8f,disabledAlpha*1.1f,1f);
            upgradeButton.colors=colors;
        }
        // Use the Button's disabled color, not a second alpha fade that hides its caption.
        if(upgradeButtonCanvasGroup!=null)upgradeButtonCanvasGroup.alpha=1f;
    }


    // =========================================================
    // Button
    // =========================================================

    public void OnClickUpgrade() {
        if(!isActiveAndEnabled || !gameObject.activeInHierarchy || Time.frameCount<=blockedPurchaseFrame ||
            upgradeButton==null || !upgradeButton.IsInteractable())return;
        if (PermanentUpgradeManager.Instance == null)
            return;


        bool upgraded =
            PermanentUpgradeManager.Instance
                .TryUpgrade(
                    upgradeType,
                    currencyType);


        if (!upgraded)
            return;


        // 숫자 먼저 갱신
        Refresh();


        // =============================
        // Upgrade Text Animation
        // =============================

        PlayTextPunch(
            levelText);


        PlayTextPunch(
            currentValueText);


        PlayTextPunch(
            costText);
    }

    public void AuditPurchaseButton() {
        var manager=PermanentUpgradeManager.Instance;
        var button=upgradeButton;
        Debug.Log($"[Shop Audit] {upgradeType} / active={isActiveAndEnabled} / button={button!=null} / " +
            $"interactable={(button!=null && button.IsInteractable())} / " +
            $"persistentCallbacks={(button!=null?button.onClick.GetPersistentEventCount():0)} / " +
            $"shopOpen={(manager!=null && manager.IsShopOpen)} / " +
            $"level={(manager!=null?manager.GetLevel(upgradeType):0)} / " +
            $"cost={(manager!=null?manager.GetUpgradeCost(upgradeType):0)} / " +
            $"balance={(CurrencyManager.Instance!=null?CurrencyManager.Instance.GetCurrency(currencyType):0)}",this);
    }
    private void PlayTextPunch(
    TMP_Text targetText) {
        if (targetText == null)
            return;


        Transform target =
            targetText.transform;


        // 빠르게 연속 강화해도 Tween 겹침 방지
        target.DOKill(
            true);


        target.DOPunchScale(
            Vector3.one * textPunchScale,
            textPunchDuration,
            6,
            0.5f);
    }
}
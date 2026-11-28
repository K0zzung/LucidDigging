using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Connects the artist-authored panel and the existing equipped-player preview.
public sealed class ShopPanelController : MonoBehaviour {
    [Header("Existing scene panel")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Button closeButton,buyButton;
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Sprite selectedTabSprite,unselectedTabSprite;
    [SerializeField] private GameObject[] contentRoots;
    [SerializeField] private Button[] statButtons;
    [SerializeField] private Button[] menuButtons;
    [Header("Existing currency and information TMPs")]
    [SerializeField] private TMP_Text pinkText,blueText,yellowText,titleText,currentLevelText,nextLevelText;
    [SerializeField] private TMP_Text currentStatLabel,nextStatLabel,currentValueText,nextValueText,costText,buyText;
    [SerializeField] private TMP_Text hpLevelText,defLevelText;
    [SerializeField] private Image costIcon,selectedStatIcon;
    [SerializeField] private Sprite blueCurrency,yellowCurrency,hpIcon,defIcon;
    [Header("UI-only player portrait")]
    [SerializeField] private RawImage playerPreview;
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Camera previewCamera;
    [Header("Slide")]
    [SerializeField,Min(.05f)] private float slideDuration=.35f;
    private Vector2 openPosition;
    private Tween slide;
    private Color[] tabColors,statColors;
    private bool[] menuStates;
    private bool initialized,bound,showing,transitioning;
    private int selectedTab,selectionFrame=-1;
    private PermanentUpgradeType selectedStat=PermanentUpgradeType.HP;
    private readonly Vector3[] corners=new Vector3[4];
    private CurrencyManager currency;
    private PermanentUpgradeManager upgrades;
    public bool IsOpen => showing && !transitioning && panel!=null && panel.gameObject.activeInHierarchy && isActiveAndEnabled;
    public bool IsShowing => showing || transitioning;
    public bool IsEquipmentTabOpen => IsOpen && selectedTab==1;
    public bool IsSkillTabOpen => IsOpen && selectedTab==2;
    private void Awake(){Initialize();}
    private void Start(){Bind();Refresh();}
    private void OnEnable(){if(initialized)Bind();}
    private void Initialize() {
        if(initialized)return;
        if(panel==null || panelGroup==null || closeButton==null || buyButton==null || menuButtons==null ||
            tabButtons==null || tabButtons.Length!=3 || statButtons==null || statButtons.Length!=2 ||
            System.Array.Exists(tabButtons,b=>b==null || b.image==null) || System.Array.Exists(statButtons,b=>b==null || b.image==null) ||
            contentRoots==null || contentRoots.Length!=3 || pinkText==null || blueText==null || yellowText==null ||
            titleText==null || currentLevelText==null || nextLevelText==null || currentStatLabel==null || nextStatLabel==null ||
            currentValueText==null || nextValueText==null || costText==null || buyText==null || hpLevelText==null || defLevelText==null ||
            costIcon==null || selectedStatIcon==null) {
            Debug.LogError("[ShopPanel] Missing authored scene references.",this);enabled=false;return;
        }
        openPosition=panel.anchoredPosition;
        tabColors=Capture(tabButtons);statColors=Capture(statButtons);
        menuStates=new bool[menuButtons.Length];
        for(int i=0;i<3;i++){int index=i;tabButtons[i].onClick.AddListener(()=>SelectTab(index));}
        statButtons[0].onClick.AddListener(()=>SelectStat(PermanentUpgradeType.HP));
        statButtons[1].onClick.AddListener(()=>SelectStat(PermanentUpgradeType.Defense));
        closeButton.onClick.AddListener(Close);buyButton.onClick.AddListener(Buy);
        // Only controls and the panel's own modal surface participate in raycasting.
        foreach(var text in panel.GetComponentsInChildren<TMP_Text>(true))text.raycastTarget=false;
        if(costIcon!=null)costIcon.raycastTarget=false;
        if(selectedStatIcon!=null)selectedStatIcon.raycastTarget=false;
        ConfigurePlayerPreview();
        initialized=true;panelGroup.interactable=false;panelGroup.blocksRaycasts=false;
        panel.anchoredPosition=ClosedPosition();
        panel.gameObject.SetActive(false);
    }
    private void ConfigurePlayerPreview() {
        if(playerPreview==null)return;
        playerPreview.raycastTarget=false;
        // This is the same persistent RenderTexture as the existing menu preview.
        // Its PlayerWeaponVisual follows the active sword/pickaxe automatically.
        if(previewCamera==null || previewCamera.targetTexture==null)return;
        playerPreview.texture=previewCamera.targetTexture;
        var previewPlayer=GameObject.Find("Preview_Player");
        if(previewPlayer==null)return;
        foreach(var body in previewPlayer.GetComponentsInChildren<SpriteRenderer>(true)) {
            if(body.gameObject.name!="Body" || body.sprite==null)continue;
            var bounds=body.bounds;
            var bottom=previewCamera.WorldToViewportPoint(bounds.center-Vector3.up*bounds.extents.y);
            var top=previewCamera.WorldToViewportPoint(bounds.center+Vector3.up*bounds.extents.y);
            var center=previewCamera.WorldToViewportPoint(bounds.center);
            float height=top.y-bottom.y;
            var rect=playerPreview.rectTransform.rect;
            if(height>.001f && rect.height>.001f) {
                float width=height*rect.width/rect.height;
                playerPreview.uvRect=new Rect(center.x-width*.5f,center.y-height*.5f,width,height);
            }
            break;
        }
    }
    private Color[] Capture(Button[] buttons) {
        var colors=new Color[buttons.Length];
        for(int i=0;i<buttons.Length;i++) {
            colors[i]=buttons[i].image.color;
            // Tint uses cached artist colors, not Button ColorBlock multiplication.
            buttons[i].transition=Selectable.Transition.None;
        }
        return colors;
    }
    private void Bind() {
        if(bound)return;
        currency=CurrencyManager.Instance;upgrades=PermanentUpgradeManager.Instance;
        if(currency==null || upgrades==null)return;
        currency.OnCurrencyChanged+=Refresh;upgrades.OnUpgradeLevelChanged+=UpgradeChanged;bound=true;
    }
    private void Unbind() {
        if(!bound)return;
        if(currency!=null)currency.OnCurrencyChanged-=Refresh;
        if(upgrades!=null)upgrades.OnUpgradeLevelChanged-=UpgradeChanged;
        bound=false;
    }
    public void Open() {
        if(!initialized)Initialize();
        if(!initialized || showing || transitioning)return;
        var flow=GameFlowController.Instance;
        if(flow==null || (flow.CurrentState!=GameState.Ready && flow.CurrentState!=GameState.Inventory))return;
        Bind();if(!bound)return;
        ConfigurePlayerPreview();
        slide?.Kill();showing=true;transitioning=true;
        for(int i=0;i<menuButtons.Length;i++)if(menuButtons[i]!=null){menuStates[i]=menuButtons[i].interactable;menuButtons[i].interactable=false;}
        panel.gameObject.SetActive(true);panel.SetAsLastSibling();
        panelGroup.interactable=false;panelGroup.blocksRaycasts=true;
        panel.anchoredPosition=ClosedPosition();
        selectedTab=0;selectedStat=PermanentUpgradeType.HP;
        flow.SetState(GameState.Inventory);ApplySelection();Refresh();
        slide=panel.DOAnchorPos(openPosition,slideDuration).SetUpdate(true).SetEase(Ease.OutCubic).OnComplete(()=>{
            slide=null;transitioning=false;panelGroup.interactable=true;Refresh();
        });
    }
    private Vector2 ClosedPosition() {
        var canvas=panel.GetComponentInParent<Canvas>(true);
        if(canvas==null)return panel.anchoredPosition;
        var canvasRect=(RectTransform)canvas.rootCanvas.transform;
        canvasRect.GetWorldCorners(corners);
        float canvasTop=panel.parent.InverseTransformPoint(corners[1]).y;
        panel.GetWorldCorners(corners);
        float panelBottom=panel.parent.InverseTransformPoint(corners[0]).y;
        return panel.anchoredPosition+Vector2.up*Mathf.Max(0f,canvasTop-panelBottom+20f);
    }
    public void Close() {
        if(!initialized || (!showing && !transitioning))return;
        slide?.Kill();showing=false;transitioning=true;
        panelGroup.interactable=false;panelGroup.blocksRaycasts=true;Refresh();
        slide=panel.DOAnchorPos(ClosedPosition(),slideDuration).SetUpdate(true).SetEase(Ease.InCubic).OnComplete(()=>{
            slide=null;FinishClose(true);
        });
    }
    private void FinishClose(bool restoreMenu) {
        showing=transitioning=false;
        if(panel==null || panelGroup==null)return;
        panelGroup.interactable=false;panelGroup.blocksRaycasts=false;
        panel.anchoredPosition=ClosedPosition();panel.gameObject.SetActive(false);
        if(restoreMenu) {
            for(int i=0;i<menuButtons.Length;i++)if(menuButtons[i]!=null)menuButtons[i].interactable=menuStates[i];
            var flow=GameFlowController.Instance;
            if(flow!=null && flow.CurrentState==GameState.Inventory)flow.SetState(GameState.Ready);
        }
    }
    public void SelectTab(int index) {
        if(!IsOpen || index<0 || index>2)return;
        selectedTab=index;selectionFrame=Time.frameCount;ApplySelection();Refresh();
    }
    public void SelectStat(PermanentUpgradeType type) {
        if(!IsOpen || selectedTab!=0 || (type!=PermanentUpgradeType.HP && type!=PermanentUpgradeType.Defense))return;
        selectedStat=type;selectionFrame=Time.frameCount;ApplySelection();Refresh();
    }
    private void ApplySelection() {
        for(int i=0;i<3;i++) {
            var original=tabColors[i];tabButtons[i].image.color=i==selectedTab?original:new Color(.45f,.45f,.45f,original.a);
            if(selectedTabSprite!=null && unselectedTabSprite!=null)tabButtons[i].image.sprite=i==selectedTab?selectedTabSprite:unselectedTabSprite;
            if(contentRoots[i]!=null)contentRoots[i].SetActive(i==selectedTab);
        }
        for(int i=0;i<2;i++) {
            var original=statColors[i];bool selected=i==(selectedStat==PermanentUpgradeType.HP?0:1);
            statButtons[i].image.color=selected?original:new Color(.45f,.45f,.45f,original.a);
        }
    }
    private void UpgradeChanged(PermanentUpgradeType type,int level){Refresh();}
    public void Refresh() {
        if(!bound)return;
        pinkText.SetText("{0}",currency.GetCurrency(CurrencyType.Dash));
        blueText.SetText("{0}",currency.GetCurrency(CurrencyType.Shockwave));
        yellowText.SetText("{0}",currency.GetCurrency(CurrencyType.Shield));
        bool hp=selectedStat==PermanentUpgradeType.HP;
        int level=upgrades.GetLevel(selectedStat);int value=upgrades.GetPermanentValue(selectedStat);
        titleText.text=hp?"체력 강화":"방어력 강화";
        currentLevelText.SetText("Lv.{0}",level);nextLevelText.text=level==int.MaxValue?"MAX":$"Lv.{(long)level+1}";
        currentStatLabel.text=hp?"현재 체력":"현재 방어력";nextStatLabel.text="강화 후";
        currentValueText.SetText("{0}",value);nextValueText.SetText("{0}",upgrades.GetNextPermanentValue(selectedStat));
        costIcon.sprite=hp?blueCurrency:yellowCurrency;selectedStatIcon.sprite=hp?hpIcon:defIcon;
        int cost=upgrades.GetUpgradeCost(selectedStat);costText.text=level==int.MaxValue?"MAX":$"x{cost}";
        hpLevelText.SetText("Lv.{0}",upgrades.GetLevel(PermanentUpgradeType.HP));
        defLevelText.SetText("Lv.{0}",upgrades.GetLevel(PermanentUpgradeType.Defense));
        bool allowed=IsOpen && selectedTab==0 && upgrades.CanUpgrade(selectedStat,upgrades.GetCurrencyType(selectedStat));
        buyButton.interactable=allowed;
        buyText.text=level==int.MaxValue?"MAX":currency.GetCurrency(upgrades.GetCurrencyType(selectedStat))<cost?"재화 부족":"강화";
    }
    public void Buy() {
        if(!IsOpen || selectedTab!=0 || Time.frameCount<=selectionFrame || upgrades==null || !buyButton.IsInteractable())return;
        upgrades.TryUpgrade(selectedStat,upgrades.GetCurrencyType(selectedStat));Refresh();
    }
    private void OnDisable(){Unbind();slide?.Kill();slide=null;if(initialized)FinishClose(showing || transitioning);}
    private void OnDestroy(){Unbind();slide?.Kill();}
}

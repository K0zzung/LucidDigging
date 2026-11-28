using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Small in-run drawer; deliberately separate from the existing shop/upgrade inventory.
public sealed class RunInventoryPanel : MonoBehaviour
{
    [Header("Validated pixel art")]
    [SerializeField] private Sprite panelSprite,slotSprite,equippedSprite,handleSprite;
    [SerializeField] private TMP_FontAsset font;
    [Header("Existing HUD references")]
    [SerializeField] private RectTransform[] skillSlots;
    [SerializeField] private PlayerGridController player;
    [Header("Optional real pickaxe integration")]
    [SerializeField] private MonoBehaviour pickaxeProvider;
    [Header("Layout / animation")]
    [SerializeField,Min(0.05f)] private float slideDuration=.22f;
    [SerializeField] private float slotGapBelowHUD=14f;
    private const float Width=432f, Height=456f, HandleWidth=48f;
    private RectTransform overlay,panel,canvasRect;
    private TMP_Text handleText,statusText;
    [SerializeField,Min(0.01f)] private float handleFadeDuration=.25f;
    private CanvasGroup handleGroup;
    private Tween handleFade;
    private Tween slide;
    private bool hudVisible;
    private bool opened,built,bound;
    private float openedX,closedX;
    private Vector2 lastCanvasSize;
    private readonly Vector3[] corners=new Vector3[4];
    private readonly List<Slot> weaponSlots=new List<Slot>();
    private readonly List<Slot> toolSlots=new List<Slot>();
    private WeaponManager weapons;
    private GameFlowController flow;
    private IInventoryEquipmentProvider tools;
    private sealed class Slot {
        public Button Button;public Image Icon,Equipped;public TMP_Text Name,State;
        public WeaponData Weapon;public string Id;public bool Tool;
    }
    private bool Playing => flow!=null && flow.CurrentState==GameState.Playing;
    private void Start() {EnsureInitialized();}
    private bool EnsureInitialized() {
        if(built)return true;
        var canvas=GetComponentInParent<Canvas>();
        if(canvas==null || panelSprite==null || slotSprite==null || equippedSprite==null || handleSprite==null) {
            Debug.LogError("[RunInventory] Assign Canvas and the four inventory sprites.",this);enabled=false;return false;
        }
        canvasRect=(RectTransform)canvas.rootCanvas.transform;
        weapons=WeaponManager.Instance;flow=GameFlowController.Instance;
        tools=pickaxeProvider!=null ? pickaxeProvider as IInventoryEquipmentProvider : weapons;
        if(pickaxeProvider!=null && tools==null)Debug.LogError("[RunInventory] Pickaxe provider must implement IInventoryEquipmentProvider.",this);
        Build();built=true;Bind();overlay.gameObject.SetActive(false);return true;
    }
    private void OnEnable() {if(built)Bind();}
    // Called synchronously by HUDController, before the Q/W/E entrance tween starts.
    public void ShowWithGameplayHUD() {
        if(!EnsureInitialized())return;
        Bind();hudVisible=true;ResetDrawer();Refresh();overlay.gameObject.SetActive(true);
        handleFade?.Kill();handleGroup.alpha=0f;handleGroup.interactable=false;handleGroup.blocksRaycasts=false;
        handleFade=handleGroup.DOFade(1f,handleFadeDuration).SetEase(Ease.OutQuad).OnComplete(()=>{
            handleFade=null;handleGroup.alpha=1f;handleGroup.interactable=true;handleGroup.blocksRaycasts=true;
        });
    }
    public void HideWithGameplayHUD() {
        hudVisible=false;handleFade?.Kill();handleFade=null;
        if(!built)return;
        handleGroup.alpha=0f;handleGroup.interactable=false;handleGroup.blocksRaycasts=false;
        ResetDrawer();overlay.gameObject.SetActive(false);
    }
    private void ResetDrawer() {
        slide?.Kill();opened=false;Layout();handleText.text=">";
    }
    private void Bind() {
        if(bound)return;
        if(weapons!=null){weapons.OnWeaponChanged+=WeaponChanged;weapons.OnWeaponUnlocked+=WeaponChanged;}
        if(tools!=null)tools.Changed+=Refresh;
        if(flow!=null)flow.OnStateChanged+=HandleState;
        bound=true;
    }
    private void Unbind() {
        if(!bound)return;
        if(weapons!=null){weapons.OnWeaponChanged-=WeaponChanged;weapons.OnWeaponUnlocked-=WeaponChanged;}
        if(tools!=null)tools.Changed-=Refresh;
        if(flow!=null)flow.OnStateChanged-=HandleState;
        bound=false;
    }
    private void Update() {
        if(built && Playing && Time.timeScale>0f && Input.GetKeyDown(KeyCode.Tab))Toggle();
    }
    private void LateUpdate() {
        if(built && overlay.gameObject.activeSelf && canvasRect.rect.size!=lastCanvasSize)Layout();
    }
    public void Toggle() {
        if(!built || !Playing || Time.timeScale<=0f)return;
        slide?.Kill();Layout(true);Refresh();opened=!opened;
        handleText.text=opened?"<":">";
        slide=panel.DOAnchorPosX(opened?openedX:closedX,slideDuration).SetEase(Ease.OutCubic).SetUpdate(true);
    }
    private void HandleState(GameState state) {
        if(!built)return;
        if(state==GameState.GameOver){HideWithGameplayHUD();return;}
        // Ready during the camera blend must not postpone the handle entrance.
        if(state==GameState.Ready)return;
        ResetDrawer();overlay.gameObject.SetActive(hudVisible && state==GameState.Playing);
        if(state==GameState.Playing)Refresh();
    }
    private void Layout(bool preserveX=false) {
        float currentX=panel.anchoredPosition.x;
        slide?.Kill();lastCanvasSize=canvasRect.rect.size;
        float top=520f;
        bool found=false;
        if(skillSlots!=null)foreach(var slot in skillSlots) {
            if(slot==null)continue;
            slot.GetWorldCorners(corners);
            float value=canvasRect.rect.yMax-canvasRect.InverseTransformPoint(corners[0]).y+slotGapBelowHUD;
            top=found?Mathf.Max(top,value):value;found=true;
        }
        float safeScale=canvasRect.rect.width/Mathf.Max(1,Screen.width);
        float bottom=Screen.safeArea.y*safeScale;
        float available=Mathf.Max(1f,canvasRect.rect.height-top-bottom-12f);
        float scale=Mathf.Clamp(Mathf.Min(1f,canvasRect.rect.width*.48f/(Width+HandleWidth),available/Height),.05f,1f);
        panel.localScale=new Vector3(scale,scale,1f);
        openedX=Screen.safeArea.x*safeScale+12f;
        closedX=Screen.safeArea.x*safeScale-Width*scale;
        panel.anchoredPosition=new Vector2(preserveX?currentX:(opened?openedX:closedX),-top);
    }
    private void Build() {
        overlay=Rect("RunInventoryOverlay",canvasRect,Vector2.zero,Vector2.one);
        panel=Rect("InventoryDrawer",overlay,Vector2.up,Vector2.up);
        panel.pivot=Vector2.up;panel.sizeDelta=new Vector2(Width,Height);
        var background=panel.gameObject.AddComponent<Image>();background.sprite=panelSprite;background.raycastTarget=true;
        Text("Title",panel,"장비",new Vector2(24,-18),new Vector2(384,36),26).alignment=TextAlignmentOptions.Center;
        Text("WeaponsHeading",panel,"무기",new Vector2(24,-66),new Vector2(384,30),24);
        Text("PickaxesHeading",panel,"곡괭이",new Vector2(24,-240),new Vector2(384,30),24);
        RectTransform weaponContent=Section("Weapons",new Vector2(48,-102),weapons!=null?weapons.AllWeapons.Count:0);
        if(weapons!=null)for(int i=0;i<weapons.AllWeapons.Count;i++) {
            WeaponData data=weapons.AllWeapons[i];if(data==null)continue;
            Slot slot=CreateSlot(weaponContent,i,false);slot.Weapon=data;weaponSlots.Add(slot);
        }
        int toolCount=tools!=null?tools.Items.Count:3;
        RectTransform toolContent=Section("Pickaxes",new Vector2(48,-280),toolCount);
        for(int i=0;i<toolCount;i++)toolSlots.Add(CreateSlot(toolContent,i,true));
        statusText=Text("EquipmentStatus",panel,"",new Vector2(24,-416),new Vector2(384,28),17);
        statusText.alignment=TextAlignmentOptions.Center;
        RectTransform handle=Rect("InventoryHandle",panel,Vector2.up,Vector2.up);
        handle.pivot=Vector2.up;handle.anchoredPosition=new Vector2(Width,-168);handle.sizeDelta=new Vector2(HandleWidth,120);
        handleGroup=handle.gameObject.AddComponent<CanvasGroup>();
        handleGroup.alpha=0f;handleGroup.interactable=false;handleGroup.blocksRaycasts=false;
        var handleImage=handle.gameObject.AddComponent<Image>();handleImage.sprite=handleSprite;
        var button=handle.gameObject.AddComponent<Button>();button.targetGraphic=handleImage;button.onClick.AddListener(Toggle);
        handleText=Text("HandleArrow",handle,">",Vector2.zero,new Vector2(HandleWidth,120),28);handleText.alignment=TextAlignmentOptions.Center;
        Canvas.ForceUpdateCanvases();Layout();
    }
    private RectTransform Section(string name,Vector2 position,int count) {
        RectTransform viewport=Rect(name+"Viewport",panel,Vector2.up,Vector2.up);
        viewport.pivot=Vector2.up;viewport.anchoredPosition=position;viewport.sizeDelta=new Vector2(336,130);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;
        RectTransform content=Rect(name+"Content",viewport,Vector2.up,Vector2.up);
        content.pivot=Vector2.up;content.sizeDelta=new Vector2(Mathf.Max(336,count*120-24),130);
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.content=content;scroll.viewport=viewport;
        scroll.horizontal=true;scroll.vertical=false;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        return content;
    }
    private Slot CreateSlot(RectTransform parent,int index,bool tool) {
        RectTransform rect=Rect("Slot"+index,parent,Vector2.up,Vector2.up);
        rect.pivot=Vector2.up;rect.anchoredPosition=new Vector2(index*120,0);rect.sizeDelta=new Vector2(96,96);
        var image=rect.gameObject.AddComponent<Image>();image.sprite=slotSprite;image.preserveAspect=true;
        var slot=new Slot{Tool=tool,Button=rect.gameObject.AddComponent<Button>()};slot.Button.targetGraphic=image;
        RectTransform icon=Rect("EquipmentIcon",rect,Vector2.zero,Vector2.one);icon.offsetMin=new Vector2(12,12);icon.offsetMax=new Vector2(-12,-12);
        slot.Icon=icon.gameObject.AddComponent<Image>();slot.Icon.preserveAspect=true;slot.Icon.raycastTarget=false;
        slot.Equipped=Rect("EquippedOutline",rect,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();
        slot.Equipped.sprite=equippedSprite;slot.Equipped.preserveAspect=true;slot.Equipped.raycastTarget=false;
        slot.Name=Text("EquipmentName",rect,"",new Vector2(-10,-100),new Vector2(116,24),16);slot.Name.alignment=TextAlignmentOptions.Center;
        slot.State=Text("SlotState",rect,"",new Vector2(0,-66),new Vector2(96,28),14);slot.State.alignment=TextAlignmentOptions.Center;
        slot.Button.onClick.AddListener(()=>Select(slot));return slot;
    }
    private void Select(Slot slot) {
        if(!Playing || Time.timeScale<=0f || (player!=null && player.IsActionInProgress)) {
            statusText.text="행동 완료 후 장착할 수 있습니다";return;
        }
        bool success=slot.Tool?tools!=null&&tools.TryEquip(slot.Id):weapons!=null&&slot.Weapon!=null&&weapons.IsOwned(slot.Weapon)&&weapons.EquipWeapon(slot.Weapon);
        Refresh();if(!success)statusText.text="장착할 수 없는 장비입니다";
    }
    private void WeaponChanged(WeaponData data) {Refresh();}
    private void Refresh() {
        if(!built)return;
        bool pickaxeInHand=false;
        if(tools!=null)foreach(var item in tools.Items)if(item.InHand)pickaxeInHand=true;
        foreach(var slot in weaponSlots) {
            bool equipped=weapons.ActiveEquipment==slot.Weapon;
            Apply(slot,new InventoryEquipmentItem(slot.Weapon.WeaponId,slot.Weapon.WeaponName,slot.Weapon.InventoryIcon,
                weapons.IsOwned(slot.Weapon),equipped,equipped&&!pickaxeInHand));
        }
        for(int i=0;i<toolSlots.Count;i++) {
            var item=tools!=null&&i<tools.Items.Count?tools.Items[i]:new InventoryEquipmentItem("","미연결",null,false,false,false);
            Apply(toolSlots[i],item);
        }
        statusText.text=tools==null?"곡괭이 데이터 미연결":pickaxeInHand?"현재 사용: 곡괭이":"현재 사용: 무기";
    }
    private static void Apply(Slot slot,InventoryEquipmentItem item) {
        slot.Id=item.Id;slot.Name.text=item.Name;slot.Icon.sprite=item.Icon;slot.Icon.enabled=item.Icon!=null;
        slot.Icon.color=item.Unlocked?Color.white:new Color(.4f,.35f,.5f,.4f);
        slot.Equipped.enabled=item.Equipped;slot.Button.interactable=item.Unlocked&&!string.IsNullOrEmpty(item.Id);
        slot.State.text=item.InHand?"사용 중":item.Equipped?"장착":item.Unlocked?"":string.IsNullOrEmpty(item.Id)?"":"잠김";
        slot.State.color=new Color(.8f,.95f,.93f);
    }
    private TMP_Text Text(string name,Transform parent,string value,Vector2 position,Vector2 size,float fontSize) {
        RectTransform rect=Rect(name,parent,Vector2.up,Vector2.up);rect.pivot=Vector2.up;rect.anchoredPosition=position;rect.sizeDelta=size;
        var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=fontSize;
        text.color=new Color(.91f,.85f,.98f);text.raycastTarget=false;text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Ellipsis;
        return text;
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max) {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
    }
    private void OnDisable() {
        Unbind();HideWithGameplayHUD();
    }
    private void OnDestroy() {Unbind();handleFade?.Kill();slide?.Kill();if(overlay!=null)Destroy(overlay.gameObject);}
}

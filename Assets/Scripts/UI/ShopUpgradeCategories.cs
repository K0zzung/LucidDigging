using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Reuses the existing shop root, currency bar and six purchase rows. No second shop.
[DefaultExecutionOrder(100)]
public sealed class ShopUpgradeCategories : MonoBehaviour {
    [SerializeField] private PermanentUpgradeRowUI[] existingRows;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private GameObject oldTabs,oldContent,oldCloseButton;
    [SerializeField] private RectTransform playButtonRect;
    private RectTransform body;
    private ScrollRect scroll;
    private int selectedCategory;
    private readonly int[] rowCounts=new int[3];
    private readonly Vector3[] corners=new Vector3[4];
    private const float HeaderHeight=110f, BottomPadding=16f;
    private readonly RectTransform[] categories=new RectTransform[3];
    private readonly Image[] tabImages=new Image[3];
    private PermanentUpgradeRowUI[] rows;
    private GameFlowController flow;
    private bool built;
    private void Start() {
        if(existingRows==null || existingRows.Length!=6 || font==null){Debug.LogError("[Shop] Assign six existing rows and pixel font.",this);enabled=false;return;}
        var attack=System.Array.Find(existingRows,r=>r!=null && r.UpgradeType==PermanentUpgradeType.Attack);
        if(attack==null){enabled=false;return;}
        // Clone only the new seventh offer. Its button is explicitly rebound to the clone.
        var pickaxe=Instantiate(attack,transform);pickaxe.name="PickaxeAttackUpgrade";pickaxe.ConfigureAsPickaxe();
        rows=new PermanentUpgradeRowUI[7];System.Array.Copy(existingRows,rows,6);rows[6]=pickaxe;
        body=Rect("UpgradeCategories",transform);body.anchorMin=body.anchorMax=body.pivot=new Vector2(.5f,1);
        body.anchoredPosition=new Vector2(0,-350);body.sizeDelta=new Vector2(960,560);
        var edge=body.gameObject.AddComponent<Image>();edge.color=new Color(.36f,.25f,.53f,1);edge.raycastTarget=false;
        var plate=Rect("NavyInset",body);plate.offsetMin=new Vector2(4,4);plate.offsetMax=new Vector2(-4,-4);
        var plateImage=plate.gameObject.AddComponent<Image>();plateImage.color=new Color(.035f,.04f,.09f,1);plateImage.raycastTarget=false;
        var viewport=Rect("OffersViewport",body);viewport.offsetMin=new Vector2(20,BottomPadding);viewport.offsetMax=new Vector2(-20,-HeaderHeight);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scrollSurface=viewport.gameObject.AddComponent<Image>();scrollSurface.color=Color.clear;
        scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.inertia=false;
        scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30f;
        var rail=Rect("ContentScrollBar",body);rail.anchorMin=new Vector2(1,0);rail.anchorMax=Vector2.one;
        rail.offsetMin=new Vector2(-12,BottomPadding);rail.offsetMax=new Vector2(-4,-HeaderHeight);
        rail.gameObject.AddComponent<Image>().color=new Color(.12f,.1f,.2f);
        var thumb=Rect("Handle",rail);var thumbImage=thumb.gameObject.AddComponent<Image>();thumbImage.color=new Color(.56f,.39f,.77f);
        var scrollbar=rail.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=thumb;scrollbar.targetGraphic=thumbImage;
        scrollbar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=scrollbar;
        scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        string[] labels={"기본","장비","스킬"};
        for(int i=0;i<3;i++) {
            int index=i;
            var tab=Rect("Category"+i,body);tab.anchorMin=tab.anchorMax=tab.pivot=new Vector2(0,1);
            tab.anchoredPosition=new Vector2(24+310*i,-20);tab.sizeDelta=new Vector2(292,72);
            tabImages[i]=tab.gameObject.AddComponent<Image>();
            var button=tab.gameObject.AddComponent<Button>();button.targetGraphic=tabImages[i];button.onClick.AddListener(()=>Select(index));
            var label=CreateText("Label",tab,font,30);label.text=labels[i];label.alignment=TextAlignmentOptions.Center;
            categories[i]=Rect(labels[i]+"Upgrades",viewport);
            categories[i].anchorMin=new Vector2(0,1);categories[i].anchorMax=new Vector2(1,1);categories[i].pivot=new Vector2(.5f,1);
            categories[i].sizeDelta=Vector2.zero;
        }
        int[] positions=new int[3];
        foreach(var row in rows) {int category=CategoryOf(row.UpgradeType);row.BuildCategoryLayout(categories[category],positions[category]++,font);}
        for(int i=0;i<3;i++) {
            rowCounts[i]=positions[i];
            categories[i].sizeDelta=new Vector2(0,ContentHeight(rowCounts[i]));
        }
        if(oldTabs!=null)oldTabs.SetActive(false);
        if(oldContent!=null)oldContent.SetActive(false);
        if(oldCloseButton!=null)oldCloseButton.SetActive(false);
        built=true;flow=GameFlowController.Instance;if(flow!=null)flow.OnStateChanged+=StateChanged;
        Select(0);StateChanged(flow!=null?flow.CurrentState:GameState.Ready);
    }
    public static int CategoryOf(PermanentUpgradeType type) {
        if(type==PermanentUpgradeType.HP || type==PermanentUpgradeType.Defense)return 0;
        if(type==PermanentUpgradeType.Attack || type==PermanentUpgradeType.PickaxeAttack)return 1;
        return 2;
    }
    public void Select(int category) {
        if(!built || category<0 || category>=3)return;
        foreach(var row in rows)row.BlockPurchaseThisFrame();
        selectedCategory=category;scroll.StopMovement();scroll.content=categories[category];
        categories[category].anchoredPosition=Vector2.zero;
        FitBody();
        for(int i=0;i<3;i++) {
            categories[i].gameObject.SetActive(i==category);
            tabImages[i].color=i==category?new Color(.32f,.23f,.5f):new Color(.1f,.09f,.19f);
        }
        foreach(var row in rows)if(row.isActiveAndEnabled)row.Refresh();
    }
    private static float ContentHeight(int count) => count*PermanentUpgradeRowUI.CardHeight+Mathf.Max(0,count-1)*PermanentUpgradeRowUI.CardGap;
    private void LateUpdate(){if(built && body.gameObject.activeInHierarchy)FitBody();}
    private void FitBody() {
        float height=HeaderHeight+ContentHeight(rowCounts[selectedCategory])+BottomPadding;
        if(playButtonRect!=null) {
            playButtonRect.GetWorldCorners(corners);
            var root=(RectTransform)transform;
            float playTop=root.InverseTransformPoint(corners[1]).y;
            float bodyTop=root.rect.yMax+body.anchoredPosition.y;
            height=Mathf.Min(height,Mathf.Max(HeaderHeight+80f,bodyTop-playTop-16f));
        }
        if(Mathf.Abs(body.sizeDelta.y-height)>.1f)body.sizeDelta=new Vector2(960,height);
        scroll.vertical=ContentHeight(rowCounts[selectedCategory])>height-HeaderHeight-BottomPadding+1f;
    }
    [ContextMenu("Audit shop purchase buttons")]
    public void AuditButtons(){if(rows!=null)foreach(var row in rows)row.AuditPurchaseButton();}
    private void StateChanged(GameState state) {
        if(body==null)return;
        body.gameObject.SetActive(state==GameState.Inventory);
        if(state==GameState.Inventory)foreach(var row in rows){row.BlockPurchaseThisFrame();row.Refresh();}
    }
    public static TMP_Text CreateText(string name,Transform parent,TMP_FontAsset font,float size) {
        var text=Rect(name,parent).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;
        text.color=Color.white;text.raycastTarget=false;text.enableWordWrapping=false;return text;
    }
    private static RectTransform Rect(string name,Transform parent) {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;
    }
    private void OnDestroy(){if(flow!=null)flow.OnStateChanged-=StateChanged;}
}

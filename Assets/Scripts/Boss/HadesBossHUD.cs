using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Event-driven boss HUD. Owns presentation only, never health or phase state.
public sealed class HadesBossHUD : MonoBehaviour
{
    private BossEnemy boss;
    private Image purpleFill, redFill;
    private TMP_Text label;
    private RectTransform panel;
    private Vector2 basePanelPosition, appliedOffset;
    public void Bind(BossEnemy target, Sprite frameSprite, TMP_FontAsset nameFont = null) {
        boss = target;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080,1920);
        scaler.matchWidthOrHeight = 0f;
        panel = Rect("HadesHealth",transform,new Vector2(0.02f,0),new Vector2(0.98f,0));
        panel.pivot = new Vector2(0.5f,0);
        panel.sizeDelta = new Vector2(0,200);
        // Source alpha bounds X=34..607 in a 608px canvas: center the visible frame.
        float visibleCenterOffset=((34f+607f)*0.5f-608f*0.5f)/608f;
        panel.anchoredPosition = new Vector2(-1080f*.96f*visibleCenterOffset,Screen.safeArea.y*1080f/Mathf.Max(1,Screen.width));
        basePanelPosition = panel.anchoredPosition;
        ApplyPosition();
        Image background = Image("BarBackground",panel,new Vector2(.165f,.43f),new Vector2(.953f,.55f));
        background.color = new Color(.08f,.03f,.11f,.95f);
        redFill = Image("Phase2RedFill",panel,new Vector2(.165f,.43f),new Vector2(.953f,.55f));
        redFill.color = new Color(1f,.18f,.3f);
        purpleFill = Image("Phase1PurpleFill",panel,new Vector2(.165f,.43f),new Vector2(.953f,.55f));
        purpleFill.color = new Color(.65f,.22f,.9f);
        Image frame = Image("HadesFrame",panel,Vector2.zero,Vector2.one);
        frame.sprite = frameSprite;
        frame.preserveAspect = false;
        var textRect = Rect("BossName",panel,new Vector2(.18f,.64f),new Vector2(.95f,.97f));
        textRect.offsetMin = new Vector2(-29f,0f);
        textRect.offsetMax = new Vector2(-29f,0f);
        label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = nameFont != null ? nameFont : TMP_Settings.defaultFontAsset;
        label.fontSize = 50;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 9f;
        label.color = new Color(.98f,.9f,.77f,1f);
        label.outlineColor = new Color(.12f,.04f,.18f,1f);
        label.outlineWidth = .2f;
        label.alignment = TextAlignmentOptions.Center;
        label.text = "HADES";
        label.raycastTarget = false;
        boss.HealthChanged += Refresh;
        Refresh();
    }
    private void ApplyPosition() {
        appliedOffset = boss.HUDOffset;
        panel.anchoredPosition = basePanelPosition+appliedOffset;
    }
    private void LateUpdate() {
        // Inspector edits are reflected during Play without rebuilding or rewriting UI each frame.
        if (boss != null && panel != null && appliedOffset != boss.HUDOffset) ApplyPosition();
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 min,Vector2 max) {
        var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false);
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        return rect;
    }
    private static Image Image(string name, Transform parent,Vector2 min,Vector2 max) {
        var image = Rect(name,parent,min,max).gameObject.AddComponent<Image>();
        image.raycastTarget=false;
        return image;
    }
    private void Refresh() {
        if(boss==null)return;
        float ratio=Mathf.Clamp01(boss.CurrentHP/(float)Mathf.Max(1,boss.MaxHP));
        float purpleRatio=boss.Phase==1?ratio:0f;
        float redRatio=boss.Phase==1?1f:ratio;
        purpleFill.rectTransform.anchorMax=new Vector2(.165f+.788f*purpleRatio,.55f);
        redFill.rectTransform.anchorMax=new Vector2(.165f+.788f*redRatio,.55f);
        label.color=boss.Phase==2?new Color(1f,0f,0f,1f):new Color(.98f,.9f,.77f,1f);

    }
    private void OnDestroy() {if(boss!=null)boss.HealthChanged-=Refresh;}
}

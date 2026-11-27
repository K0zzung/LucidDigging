using DG.Tweening;
using UnityEngine;

public class BossSpikeWarning : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;


    [Range(0f, 1f)]
    [SerializeField]
    private float minAlpha = 0.15f;


    [Range(0f, 1f)]
    [SerializeField]
    private float maxAlpha = 0.65f;


    [SerializeField]
    private float blinkTime = 0.1f;


    private Tween blinkTween;
    private Mesh warningMesh;
    private MeshRenderer cellArea;
    private MaterialPropertyBlock tint;
    public void ConfigureCell(Vector3 xStep, Vector3 yStep) { ConfigureArea(xStep,yStep,false,1); }
    public void ConfigureHorizontal(Vector3 xStep, Vector3 yStep,int direction) { ConfigureArea(xStep,yStep,true,direction); }
    private void ConfigureArea(Vector3 xStep,Vector3 yStep,bool horizontal,int direction) {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        var filter = gameObject.AddComponent<MeshFilter>();
        cellArea = gameObject.AddComponent<MeshRenderer>();
        cellArea.sharedMaterial = spriteRenderer != null ? spriteRenderer.sharedMaterial : null;
        cellArea.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        cellArea.sortingOrder = 1;
        Vector3 across=horizontal?yStep:xStep;
        Vector3 along=horizontal?xStep*(direction<0?-1:1):yStep;
        Vector3 x=across*0.5f, bottom=along*-0.5f, top=along*2.5f;
        warningMesh = new Mesh {name=horizontal?"Spike 3x1 warning":"Spike 1x3 warning"};
        warningMesh.vertices = new[]{bottom-x,bottom+x,top+x,top-x};
        warningMesh.colors = new[]{Color.white,Color.white,Color.white,Color.white};
        warningMesh.triangles = new[]{0,1,2,0,2,3};
        filter.sharedMesh=warningMesh;
        tint = new MaterialPropertyBlock();
        tint.SetTexture("_MainTex",Texture2D.whiteTexture);
    }
    private void SetWarningAlpha(float alpha) {
        tint.SetColor("_Color",new Color(1f,0.03f,0.03f,alpha));
        cellArea.SetPropertyBlock(tint);
    }

    private void Awake() {
        if (spriteRenderer == null) {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }
    }


    public void Play(
        float duration, bool autoDestroy = true) {
        if (spriteRenderer == null) {
            Destroy(gameObject);
            return;
        }


        if (cellArea != null) {
            float alpha=0.2f;
            SetWarningAlpha(alpha);
            blinkTween=DOTween.To(()=>alpha,value=>{alpha=value;SetWarningAlpha(value);},0.38f,blinkTime).SetLoops(-1,LoopType.Yoyo);
            if(autoDestroy)Destroy(gameObject,duration);
            return;
        }

        Color color =
            spriteRenderer.color;

        color.a =
            minAlpha;

        spriteRenderer.color =
            color;


        blinkTween =
            spriteRenderer
                .DOFade(
                    maxAlpha,
                    blinkTime)
                .SetLoops(
                    -1,
                    LoopType.Yoyo);


        if (autoDestroy) Destroy(gameObject, duration);
    }


    private void OnDisable() { blinkTween?.Kill(); }

    private void OnDestroy() {
        blinkTween?.Kill();
        if (warningMesh != null) Destroy(warningMesh);
    }
}
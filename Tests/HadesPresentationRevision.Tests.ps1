# Extracted production petrification, layered HUD and drop decision. No Unity playback.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
function Method([string]$source,[string]$signature) {
 $start=$source.IndexOf($signature);if($start -lt 0){throw $signature}
 $open=$source.IndexOf('{',$start);$depth=0
 for($i=$open;$i -lt $source.Length;$i++) {
  if($source[$i] -eq '{'){$depth++};if($source[$i] -eq '}'){$depth--;if($depth -eq 0){return $source.Substring($start,$i-$start+1)}}
 }
 throw 'Unclosed production method'
}
$boss=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Boss/BossEnemy.cs'),[Text.Encoding]::GetEncoding(949))
$hud=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Boss/HadesBossHUD.cs'))
$petrify=Method $boss 'private IEnumerator PetrifyAndFinish()'
$refresh=Method $hud 'private void Refresh()'
$drop=Method $boss 'private void TrySpawnWeaponDrop()'
$stub=@'
using System;
using System.Collections;
using UnityEngine;
namespace UnityEngine {
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);public static Vector3 one=>new Vector3(1,1,1);public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
 public struct Quaternion {public static Quaternion identity=>new Quaternion();public static Vector3 operator*(Quaternion q,Vector3 v)=>v;}
 public struct Vector3Int {public int x,y,z;public Vector3Int(int a,int b,int c){x=a;y=b;z=c;}public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);}
 public struct Vector2Int {public int x,y;}
 public struct Bounds {public Vector3 center,size;}
 public struct Rect {public Vector2 position;}
 public class Sprite {public Bounds bounds;public Rect rect;public Vector2 pivot;public float pixelsPerUnit=16;}
 public class Transform {public Vector3 localPosition,localScale=Vector3.one;public Quaternion localRotation;}
 public class SpriteRenderer {public Sprite sprite;public Transform transform=new Transform();public bool enabled=true;public int sortingOrder;}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public class WaitForSeconds {public float seconds;public WaitForSeconds(float value){seconds=value;}}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));}
 public static class Debug {public static void Log(object v){}public static void LogWarning(object v){}public static void LogError(object v,object c){}}
}
public class StoneProbe {
 public SpriteRenderer headRenderer=new SpriteRenderer(),jawRenderer=new SpriteRenderer(),torso=new SpriteRenderer(),wakeRenderer=new SpriteRenderer();
 public SpriteRenderer[] bodyRenderers;
 public Sprite[] stoneFrames=new Sprite[16];public float stoneFrameDuration=1;public bool IsDeathPresentationPlaying=true,Destroyed;public object gameObject=new object();
 void Destroy(object o){Destroyed=true;}
 public IEnumerator Play()=>PetrifyAndFinish();
 PETRIFY
}
public class RectTransform {public Vector2 anchorMax;}
public class Image {public RectTransform rectTransform=new RectTransform();public Color color;}
public class Label {public string text;public Color color;}
public class BossHP {public int Phase=1,CurrentHP=50,MaxHP=100;}
public class HUDProbe {
 public BossHP boss=new BossHP();public Image purpleFill=new Image(),redFill=new Image(),phaseOne=new Image(),phaseTwo=new Image();public Label label=new Label();
 public void Update()=>Refresh();
 REFRESH
}
public class WeaponData {public string WeaponName="Arondight";}
public class WeaponManager {public static WeaponManager Instance=new WeaponManager();public bool Owned;public bool IsOwned(WeaponData d)=>Owned;}
public class PlayerGridController {public bool TryGetBossLandingCell(out Vector3Int c){c=new Vector3Int(2,-10,0);return true;}}
public class EnemyManager {public Vector3 GetCellCenterWorld(Vector3Int c)=>new Vector3(c.x,c.y,0);}
public class WeaponPickup {public Vector3Int Cell;public float Rise;public void Initialize(WeaponData d,Vector3Int c,Vector3 start,PlayerGridController p,float rise=0){Cell=c;Rise=rise;}}
public class DropProbe {
 public WeaponData weaponDrop=new WeaponData();public WeaponPickup weaponPickupPrefab=new WeaponPickup();public bool forceWeaponDropForTesting;public float weaponDropRiseCells=2;
 public PlayerGridController player=new PlayerGridController();public EnemyManager enemyManager=new EnemyManager();public Vector3Int Cell;public Vector2Int FootprintSize=new Vector2Int{x=7,y=7};
 public int Spawns;public WeaponPickup Last;
 WeaponPickup Instantiate(WeaponPickup prefab,Vector3 target,Quaternion rotation){Spawns++;return Last=new WeaponPickup();}
 public void Drop()=>TrySpawnWeaponDrop();
 DROP
}
public static class PresentationTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
 static Sprite Sprite(float w,float h,float cx,float cy,float anchor){return new Sprite{bounds=new Bounds{size=new Vector3(w,h,0),center=new Vector3(cx,cy,0)},rect=new Rect{position=new Vector2(anchor,0)},pivot=new Vector2(1,1)};}
 static Vector3 Center(SpriteRenderer r)=>r.transform.localPosition+Vector3.Scale(r.sprite.bounds.center,r.transform.localScale);
 public static string Run(){
  var p=new StoneProbe();p.bodyRenderers=new[]{p.headRenderer,p.jawRenderer,p.torso};
  var centers=new Vector3[3];var sizes=new Vector3[3];
  for(int i=0;i<3;i++){var r=p.bodyRenderers[i];r.sprite=Sprite(10-i,8-i,.3f,-.2f,0);r.transform.localScale=new Vector3(1.2f,.9f,1);r.transform.localPosition=new Vector3(i,i*2,0);centers[i]=Center(r);sizes[i]=Vector3.Scale(r.sprite.bounds.size,r.transform.localScale);}
  for(int i=0;i<16;i++)p.stoneFrames[i]=Sprite(6,5,.1f,.2f,(i/4)*2);
  var routine=p.Play();float duration=0;
  Check(routine.MoveNext(),"first stone stage");duration+=((WaitForSeconds)routine.Current).seconds;
  for(int i=0;i<3;i++){var r=p.bodyRenderers[i];Vector3 center=Center(r),size=Vector3.Scale(r.sprite.bounds.size,r.transform.localScale);Check(Near(center.x,centers[i].x)&&Near(center.y,centers[i].y)&&Near(size.x,sizes[i].x)&&Near(size.y,sizes[i].y),"first stone stage matches each living part position and size");}
  while(routine.MoveNext())duration+=((WaitForSeconds)routine.Current).seconds;
  Check(duration==4&&p.Destroyed&&!p.IsDeathPresentationPlaying,"four 1-second stages complete");
  Check(p.headRenderer.sortingOrder>p.jawRenderer.sortingOrder,"head always above jaw");
  var h=new HUDProbe();h.Update();Check(Near(h.redFill.rectTransform.anchorMax.x,.953f)&&Near(h.purpleFill.rectTransform.anchorMax.x,.559f),"phase one: full red behind half purple");
  h.boss.Phase=2;h.Update();Check(Near(h.purpleFill.rectTransform.anchorMax.x,.165f)&&Near(h.redFill.rectTransform.anchorMax.x,.559f),"phase two: purple gone, red shrinks");Check(h.label.color.r==1&&h.label.color.g==0&&h.label.color.b==0,"phase two name turns red");
  WeaponManager.Instance.Owned=true;var d=new DropProbe();d.Drop();Check(d.Spawns==0,"owned weapon suppressed by default");d.forceWeaponDropForTesting=true;d.Drop();Check(d.Spawns==1&&d.Last.Cell.y==-10&&d.Last.Rise==2,"checkbox forces owned weapon drop at original landing cell");
  WeaponManager.Instance.Owned=false;d=new DropProbe();d.Drop();Check(d.Spawns==1,"unowned weapon still drops");
  return "PASS: production first stone stage matches each live part bounds, four seconds, stable layering; layered purple/red HUD; owned/unowned/test-forced weapon drop. Managed stand-ins; Unity not executed.";
 }
}
'@
Add-Type -TypeDefinition $stub.Replace('PETRIFY',$petrify).Replace('REFRESH',$refresh).Replace('DROP',$drop)
[PresentationTests]::Run()

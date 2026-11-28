"""Tests the actual ShopPanelController with UI/tween/backend doubles. Not a Unity render/input test."""
from pathlib import Path
import tempfile,subprocess
root=Path(__file__).resolve().parents[1];temp=Path(tempfile.mkdtemp(prefix='AuthoredShop-'))
(temp/'ShopPanelController.cs').write_bytes((root/'Assets/Scripts/UI/ShopPanelController.cs').read_bytes())
(temp/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649</NoWarn></PropertyGroup></Project>')
(temp/'Doubles.cs').write_text(r'''
using System;using System.Collections.Generic;
namespace UnityEngine {
 public class Object{}public class MonoBehaviour{public bool enabled=true,isActiveAndEnabled=true;}
 public class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}public class SerializeField:Attribute{}public class MinAttribute:Attribute{public MinAttribute(float v){}}
 public static class Time{public static int frameCount=1;public static float timeScale=0;}
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new Vector2(0,0);public static Vector2 up=>new Vector2(0,1);public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator *(Vector2 a,float v)=>new Vector2(a.x*v,a.y*v);}
 public struct Vector3{public static Vector3 up=>new Vector3(0,1,0);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public float x,y,z;public Vector3(float a,float b,float c=0){x=a;y=b;z=c;}}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public class GameObject{public string name;public static GameObject Find(string name)=>null;public T[] GetComponentsInChildren<T>(bool inactive)=>Array.Empty<T>();public int layer;public GameObject(params object[] args){}public T GetComponent<T>() where T:new()=>new T();public bool activeInHierarchy=true;public void SetActive(bool b){activeInHierarchy=b;}}
 public class Transform{public Vector3 InverseTransformPoint(Vector3 p)=>p;}
 public class RectTransform:Transform{public Vector3 localScale;public Vector2 anchorMin,anchorMax,pivot,sizeDelta;public Rect rect=new Rect();public void SetParent(Transform p,bool keep){parent=p;}public Vector2 anchoredPosition;public Transform parent=new Transform();public GameObject gameObject=new GameObject();public Canvas canvas;public void SetAsLastSibling(){}public T GetComponentInParent<T>(bool includeInactive=false) where T:class=>canvas as T;public T[] GetComponentsInChildren<T>(bool all)=>Array.Empty<T>();public void GetWorldCorners(Vector3[] c){c[0]=new Vector3(anchoredPosition.x-50,anchoredPosition.y-100);c[1]=new Vector3(anchoredPosition.x-50,anchoredPosition.y+100);c[2]=new Vector3(anchoredPosition.x+50,anchoredPosition.y+100);c[3]=new Vector3(anchoredPosition.x+50,anchoredPosition.y-100);}}
 public class Canvas{public Transform transform;public Canvas rootCanvas=>this;}
 public class CanvasGroup{public bool interactable,blocksRaycasts;}
 public class Sprite{}public class Rect{public float width=96,height=48;public Rect(){}public Rect(float x,float y,float w,float h){width=w;height=h;}}public class Camera{public RenderTexture targetTexture;public Vector3 WorldToViewportPoint(Vector3 p)=>p;}public class Bounds{public Vector3 center,extents;}public class SpriteRenderer{public GameObject gameObject=new GameObject();public Sprite sprite;public Bounds bounds=new Bounds();}public class RenderTexture{public int width=256,height=256;}public class CanvasRenderer{}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class Debug{public static void LogError(string s,object o){throw new Exception(s);}}
}
namespace UnityEngine.UI{
 public class Graphic{public bool raycastTarget=true;}
 public class RawImage:Graphic{public UnityEngine.Rect uvRect;public UnityEngine.RectTransform rectTransform=new UnityEngine.RectTransform();public UnityEngine.RenderTexture texture;}
 public class Image:Graphic{public bool enabled=true;public UnityEngine.RectTransform rectTransform=new UnityEngine.RectTransform();public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();public UnityEngine.Color color=new UnityEngine.Color(.8f,.7f,.9f,.6f);public UnityEngine.Sprite sprite;public bool preserveAspect;}
 public class Selectable{public enum Transition{None,ColorTint}}
 public class Button:Selectable{public Image image=new Image();public bool interactable=true;public Transition transition;public Event onClick=new Event();public bool IsInteractable()=>interactable;public class Event{public List<Action> actions=new List<Action>();public void AddListener(Action a){actions.Add(a);}public void Invoke(){foreach(var a in actions)a();}}}
}
namespace TMPro{public class TMP_Text:UnityEngine.UI.Graphic{public string text;public void SetText(string t,params float[] n){text=t;for(int i=0;i<n.Length;i++)text=text.Replace("{"+i+"}",n[i].ToString());}}}
namespace DG.Tweening{
 public enum Ease{OutCubic,InCubic}
 public class Tween{public bool killed,unscaled;public Action done;public UnityEngine.RectTransform rect;public UnityEngine.Vector2 end;public float duration;public Tween SetUpdate(bool b){unscaled=b;return this;}public Tween SetEase(Ease e)=>this;public Tween OnComplete(Action a){done=a;return this;}public void Kill(){killed=true;}public void Complete(){if(!killed){rect.anchoredPosition=end;done?.Invoke();}}}
 public static class ShortcutExtensions{public static List<Tween> all=new List<Tween>();public static Tween DOAnchorPos(this UnityEngine.RectTransform r,UnityEngine.Vector2 end,float duration){var t=new Tween{rect=r,end=end,duration=duration};all.Add(t);return t;}}
}
public enum GameState{Ready,Inventory,Playing}public enum CurrencyType{Dash,Shockwave,Shield}public enum PermanentUpgradeType{HP,Attack,Defense,Dash,Shield,Shockwave,PickaxeAttack}
public class GameFlowController{public static GameFlowController Instance=new GameFlowController();public GameState CurrentState=GameState.Ready;public void SetState(GameState s){CurrentState=s;}}
public class CurrencyManager{public static CurrencyManager Instance=new CurrencyManager();public event Action OnCurrencyChanged;public int[] values={2,15,2};public int GetCurrency(CurrencyType t)=>values[(int)t];public void Changed()=>OnCurrencyChanged?.Invoke();}
public class PermanentUpgradeManager{public static PermanentUpgradeManager Instance=new PermanentUpgradeManager();public event Action<PermanentUpgradeType,int> OnUpgradeLevelChanged;public int[] levels=new int[7];public int buys;public PermanentUpgradeType last;public ShopPanelController ui;public int GetLevel(PermanentUpgradeType t)=>levels[(int)t];public int GetPermanentValue(PermanentUpgradeType t)=>t==PermanentUpgradeType.HP?100+levels[0]*50:5+levels[2]*5;public int GetNextPermanentValue(PermanentUpgradeType t)=>GetPermanentValue(t)+(t==PermanentUpgradeType.HP?50:5);public int GetUpgradeCost(PermanentUpgradeType t)=>1+GetLevel(t);public CurrencyType GetCurrencyType(PermanentUpgradeType t)=>t==PermanentUpgradeType.HP?CurrencyType.Shockwave:CurrencyType.Shield;public bool CanUpgrade(PermanentUpgradeType t,CurrencyType c)=>ui.IsOpen&&CurrencyManager.Instance.GetCurrency(c)>=GetUpgradeCost(t);public bool TryUpgrade(PermanentUpgradeType t,CurrencyType c){if(!CanUpgrade(t,c))return false;CurrencyManager.Instance.values[(int)c]-=GetUpgradeCost(t);levels[(int)t]++;last=t;buys++;CurrencyManager.Instance.Changed();OnUpgradeLevelChanged?.Invoke(t,GetLevel(t));return true;}}
''',encoding='utf-8')
(temp/'Program.cs').write_text(r'''
using System;using System.Reflection;using System.Linq;using UnityEngine;using UnityEngine.UI;using TMPro;using DG.Tweening;
class Program{
 static int count;static void Check(bool b,string name){if(!b)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Set(object o,string k,object v)=>o.GetType().GetField(k,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static T Get<T>(object o,string k)=>(T)o.GetType().GetField(k,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static void Call(object o,string k)=>o.GetType().GetMethod(k,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static Tween Latest()=>ShortcutExtensions.all.Last();
 static void Main(){
 var ui=new ShopPanelController();var panel=new RectTransform{anchoredPosition=new Vector2(7,-5)};var canvas=new Canvas{transform=new RectTransform{anchoredPosition=new Vector2(0,900)}};panel.canvas=canvas;
 var group=new CanvasGroup();Set(ui,"panel",panel);Set(ui,"panelGroup",group);
 foreach(var k in new[]{"pinkText","blueText","yellowText","titleText","currentLevelText","nextLevelText","currentStatLabel","nextStatLabel","currentValueText","nextValueText","costText","buyText","hpLevelText","defLevelText"})Set(ui,k,new TMP_Text());
 var close=new Button();var buy=new Button();var tabs=new[]{new Button(),new Button(),new Button()};var stats=new[]{new Button(),new Button()};var menu=new[]{new Button(),new Button()};
 Set(ui,"closeButton",close);Set(ui,"buyButton",buy);Set(ui,"tabButtons",tabs);Set(ui,"statButtons",stats);Set(ui,"menuButtons",menu);Set(ui,"contentRoots",new[]{new GameObject(),null,null});
 Set(ui,"costIcon",new Image());Set(ui,"selectedStatIcon",new Image());Set(ui,"playerPreview",new RawImage());
 var blue=new Sprite();var yellow=new Sprite();Set(ui,"blueCurrency",blue);Set(ui,"yellowCurrency",yellow);Set(ui,"hpIcon",new Sprite());Set(ui,"defIcon",new Sprite());
 var portrait=new RawImage();portrait.rectTransform.localScale=new Vector3(.85f,.65f,1f);Set(ui,"playerPreview",portrait);Set(ui,"previewCamera",new Camera{targetTexture=new RenderTexture()});
 PermanentUpgradeManager.Instance.ui=ui;Call(ui,"Awake");Call(ui,"Start");
 Check(portrait.texture!=null&&!portrait.raycastTarget,"persistent RawImage displays equipped-player texture directly");
 Check(portrait.rectTransform.localScale.x==.85f&&portrait.rectTransform.localScale.y==.65f,"shop player preview preserves artist-edited RectTransform scale");
 Check(!panel.gameObject.activeInHierarchy&&!group.blocksRaycasts,"initial panel inactive and no invisible raycast blocker");
 ui.Open();Check(ui.IsShowing&&!ui.IsOpen&&!group.interactable&&!menu[0].interactable&&!menu[1].interactable,"slide gates shop purchase and background menu input");
 Check(Latest().unscaled&&Math.Abs(Latest().duration-.35f)<.001f&&panel.anchoredPosition.y>1000,"slide uses unscaled .35 seconds and starts fully above canvas");
 int animations=ShortcutExtensions.all.Count;ui.Open();Check(ShortcutExtensions.all.Count==animations,"duplicate open does not stack tweens");
 Latest().Complete();Check(ui.IsOpen&&group.interactable&&group.blocksRaycasts&&panel.anchoredPosition.x==7&&panel.anchoredPosition.y==-5,"slide restores exact authored open position");
 Check(Get<TMP_Text>(ui,"pinkText").text=="2"&&Get<TMP_Text>(ui,"blueText").text=="15"&&Get<TMP_Text>(ui,"yellowText").text=="2","three existing currency TMPs use manager values");
 Check(Get<TMP_Text>(ui,"currentValueText").text=="100"&&Get<TMP_Text>(ui,"nextValueText").text=="150"&&Get<Image>(ui,"costIcon").sprite==blue,"HP preview and real blue currency selected");
 var original=tabs[0].image.color;tabs[1].onClick.Invoke();Check(!Get<GameObject[]>(ui,"contentRoots")[0].activeInHierarchy&&!buy.interactable,"equipment tab remains empty and disables buying");
 tabs[2].onClick.Invoke();tabs[0].onClick.Invoke();Check(tabs[0].image.color.r==original.r&&tabs[0].image.color.a==original.a,"tab color restores exact artist tint and alpha");
 Time.frameCount++;buy.onClick.Invoke();Check(PermanentUpgradeManager.Instance.buys==1&&CurrencyManager.Instance.values[1]==14&&Get<TMP_Text>(ui,"currentValueText").text=="150","existing buy button event routes HP purchase and refreshes currencies and stats");
 stats[1].onClick.Invoke();buy.onClick.Invoke();Check(PermanentUpgradeManager.Instance.buys==1,"same-frame stat switch cannot buy wrong offer");
 Check(Get<Image>(ui,"costIcon").sprite==yellow&&Get<TMP_Text>(ui,"currentValueText").text=="5"&&Get<TMP_Text>(ui,"nextValueText").text=="10","DEF preview uses permanent DEF and yellow cost icon");
 Time.frameCount++;buy.onClick.Invoke();Check(PermanentUpgradeManager.Instance.last==PermanentUpgradeType.Defense&&CurrencyManager.Instance.values[2]==1,"DEF button selection buys DEF only");
 Check(!buy.interactable&&Get<TMP_Text>(ui,"buyText").text=="재화 부족","insufficient currency disables button and explains state");
 close.onClick.Invoke();Check(!ui.IsOpen&&!group.interactable&&!menu[0].interactable,"closing blocks purchases until slide completes");Latest().Complete();
 Check(!panel.gameObject.activeInHierarchy&&!group.blocksRaycasts&&menu[0].interactable&&menu[1].interactable&&GameFlowController.Instance.CurrentState==GameState.Ready,"close deactivates panel and restores main menu");
 for(int i=0;i<3;i++){ui.Open();Latest().Complete();Check(panel.anchoredPosition.x==7&&panel.anchoredPosition.y==-5,"repeated open retains authored position "+i);ui.Close();Latest().Complete();}
 Check(buy.onClick.actions.Count==1&&close.onClick.actions.Count==1&&tabs.All(t=>t.onClick.actions.Count==1),"repeat opening does not add click listeners");
 CurrencyManager.Instance.values[1]=99;CurrencyManager.Instance.Changed();ui.Open();Latest().Complete();Check(Get<TMP_Text>(ui,"blueText").text=="99"&&Get<TMP_Text>(ui,"currentValueText").text=="150","reopening reads fresh wallet and saved upgrade state");
 ui.Close();var interrupted=Latest();Latest().Complete();ui.Open();ui.Close();Latest().Complete();interrupted.Complete();Check(!ui.IsOpen,"canceled tween cannot reopen panel");
 Console.WriteLine(count+" authored-shop controller tests passed. UI/tween/backend doubles; not Unity Play Mode.");
 }
}
''',encoding='utf-8')
r=subprocess.run(['dotnet','run','--project',str(temp/'Test.csproj'),'--verbosity','quiet'],cwd=root,text=True,encoding='utf-8',errors='replace',capture_output=True);print(r.stdout);print(r.stderr);raise SystemExit(r.returncode)

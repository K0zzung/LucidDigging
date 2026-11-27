# Production camera state and extracted lock/message methods; not Unity playback.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
function Method([string]$s,[string]$signature) {
 $start=$s.IndexOf($signature);if($start -lt 0){throw $signature};$open=$s.IndexOf('{',$start);$depth=0
 for($i=$open;$i -lt $s.Length;$i++){if($s[$i] -eq '{'){$depth++};if($s[$i] -eq '}'){$depth--;if($depth -eq 0){return $s.Substring($start,$i-$start+1)}}}
 throw 'Unclosed production method'
}
$enc=[Text.Encoding]::GetEncoding(949)
$grid=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Player/PlayerGridController.cs'),$enc)
$warning=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/System/DepthWarningUI.cs'),$enc)
$camera=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Camera/CameraFollowTarget.cs'),$enc) -replace '(?m)^using [^;]+;\r?\n',''
$lock=''
foreach($signature in @('public void RequestBossPresentationLock()','public void BeginBossPresentationLock()','public void EndBossPresentationLock()','private void LateUpdate()')){$lock+=Method $grid $signature}
$a=$grid.IndexOf('    public bool IsActionInProgress =>');$b=$grid.IndexOf('    private sealed class AttackCompletion',$a);$lock+=$grid.Substring($a,$b-$a)
$messages=''
foreach($signature in @('public float ShowBossMessage(','public void CancelBossMessage()','private void HandleDifficultyTierChanged(','public void HideImmediately()')){$messages+=Method $warning $signature}
$stub=@'
#pragma warning disable 0649
using System;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using TMPro;
namespace UnityEngine {
 public class MonoBehaviour {public Transform transform=new Transform();}
 public class Transform {public Vector3 position;}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
 public struct Vector2Int {public static Vector2Int zero=>new Vector2Int();}
 public class CanvasGroup {public float alpha;}
 public class SerializeField:Attribute {}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace TMPro {public class TMP_FontAsset {} public class TMP_Text {public string text;public TMP_FontAsset font;}}
namespace DG.Tweening {
 public enum Ease {InOutSine}
 public class Tween {public bool Active;public void Kill(){Active=false;}public bool IsActive()=>Active;public bool IsComplete()=>!Active;public Tween SetEase(Ease e)=>this;}
 public class Sequence:Tween {Action done;public Sequence OnComplete(Action a){done=a;return this;}public void Complete(){done?.Invoke();}}
 public static class DOTween {public static Tween To(Func<float> read,Action<float> write,float target,float duration){write(target);return new Tween();}}
}
public class Skill {public bool IsSkillLocked;}
public class LockProbe {
 public Transform transform=new Transform();public bool bossPresentationRequested,bossPresentationLocked,directionPending,pendingFromDiagonal,skillQueued,isBusy;
 public Vector2Int activeHeldDirection;public Vector3 bossPresentationPosition;public Tween moveTween=new Tween();public Skill skillController=new Skill();
 public void FreezeTick()=>LateUpdate();
 LOCK
}
public class MessageProbe {
 public bool hiddenByGameOver,bossMessageActive;public TMP_FontAsset regularFont;
 public TMP_Text warningText=new TMP_Text();public CanvasGroup canvasGroup=new CanvasGroup();public Sequence sequence;
 public float fadeInDuration=.35f,holdDuration=1.2f,fadeOutDuration=.45f;public string message="depth";public int Plays;
 void PlayWarning(){Plays++;sequence=new Sequence();canvasGroup.alpha=1;}
 public void Tier()=>HandleDifficultyTierChanged(3);
 MESSAGES
}
public static class AwakeningTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Call(object o,string m)=>o.GetType().GetMethod(m,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
 public static string Run(){
  var p=new LockProbe{isBusy=true,directionPending=true,skillQueued=true};p.moveTween.Active=true;
  p.RequestBossPresentationLock();Check(p.moveTween.Active&&!p.IsReadyForBossPresentation&&p.IsActionInProgress&&!p.skillQueued,"request waits without killing active movement and blocks new queued input");
  p.isBusy=false;p.moveTween.Active=false;Check(p.IsReadyForBossPresentation&&p.IsActionInProgress,"ready without losing presentation/input guard");
  p.transform.position=new Vector3(1,2,3);p.BeginBossPresentationLock();p.transform.position=new Vector3(8,9,3);p.FreezeTick();Check(p.transform.position.x==1&&p.transform.position.y==2,"stable player position");p.EndBossPresentationLock();Check(!p.IsActionInProgress,"unlock restores controls");
  var c=new CameraFollowTarget();var player=new Transform{position=new Vector3(4,-10,0)};c.transform.position=new Vector3(4,-8,0);
  typeof(CameraFollowTarget).GetField("player",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(c,player);Call(c,"Start");c.FocusBoss(new Vector3(99,-20,0),.65f);Call(c,"LateUpdate");Check(c.transform.position.y==-20&&c.transform.position.x==4,"boss focus keeps map X");
  c.RestorePlayerFocus(.65f);Call(c,"LateUpdate");Check(c.transform.position.y==-10,"focus never corrupts lowest player Y");player.position=new Vector3(4,-9,0);Call(c,"LateUpdate");Check(c.transform.position.y==-10,"downward-only tracking preserved");c.FocusBoss(new Vector3(0,-20,0),.65f);Call(c,"OnDisable");Check(c.transform.position.y==-10,"disable cancels focus offset");
  var ui=new MessageProbe();var original=new TMP_FontAsset();var korean=new TMP_FontAsset();ui.warningText.font=original;
  float duration=ui.ShowBossMessage("announcement",korean);Check(Math.Abs(duration-2)<.0001&&ui.warningText.text=="announcement"&&ui.warningText.font==korean,"existing warning duration and message font");ui.Tier();Check(ui.Plays==1,"depth warning cannot overwrite announcement");ui.sequence.Complete();Check(!ui.bossMessageActive&&ui.warningText.font==original&&ui.warningText.text=="depth","original font/message restored");
  ui.ShowBossMessage("announcement",korean);ui.HideImmediately();Check(ui.canvasGroup.alpha==0&&!ui.bossMessageActive&&ui.ShowBossMessage("late",korean)==0,"gameover hides and rejects late announcements");
  return "PASS: actual camera focus/restore state, extracted safe player lock and depth announcement ownership/font restoration/gameover guards. Managed stand-ins; tween visuals and Unity not executed.";
 }
}
'@
Add-Type -TypeDefinition ($stub.Replace('LOCK',$lock).Replace('MESSAGES',$messages)+$camera)
[AwakeningTests]::Run()

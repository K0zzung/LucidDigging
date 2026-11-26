# Production weapon animation against a managed sequence scheduler; not Unity rendering.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stub = @'
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DG.Tweening;
using Cinemachine;
namespace Cinemachine {
 public class CinemachineBasicMultiChannelPerlin {public float m_AmplitudeGain,m_FrequencyGain;}
 public class CinemachineVirtualCamera {public T GetCinemachineComponent<T>() where T:new()=>new T();}
}
public enum AttackImpactKind {Enemy,Boss,Terrain,HardTerrain,Break}
namespace UnityEngine {
 public class MonoBehaviour {}
 public class SerializeField:Attribute {}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string s){}}
 public struct Vector3 {
  public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public Vector3 normalized {get{float m=(float)Math.Sqrt(x*x+y*y+z*z);return m==0?this:this*(1/m);}}
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
 }
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public struct Vector2Int {public int x,y;public Vector2Int(int a,int b){x=a;y=b;}}
 public struct Quaternion {public float z;public static Quaternion Euler(float x,float y,float z)=>new Quaternion{z=z};}
 public class Transform {public Vector3 localPosition,localScale;public Quaternion localRotation;}
 public static class Time {public static float time;}
 public static class Debug {public static void LogWarning(object value){}}
 public static class Mathf {public const float Rad2Deg=57.2957795f;public static float Abs(float f)=>Math.Abs(f);public static float Max(float a,float b)=>Math.Max(a,b);public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);}
}
namespace DG.Tweening {
 public enum Ease {OutQuad,InQuad,OutSine,InExpo,OutCubic}
 public class Tween {public float Length;public Action Apply;public virtual void Kill(){}public Tween SetEase(Ease ease)=>this;public Tween OnComplete(Action callback)=>this;}
 public class Sequence:Tween {
  class Entry {public float At;public Action Apply;public bool Done;}
  List<Entry> entries=new List<Entry>();float lastStart;Action complete;bool killed,finished;
  public Sequence Append(Tween t){lastStart=Length;Length+=t.Length;entries.Add(new Entry{At=Length,Apply=t.Apply});return this;}
  public Sequence Join(Tween t){entries.Add(new Entry{At=lastStart+t.Length,Apply=t.Apply});Length=Math.Max(Length,lastStart+t.Length);return this;}
  public Sequence AppendCallback(Action a){entries.Add(new Entry{At=Length,Apply=a});return this;}
  public new Sequence OnComplete(Action a){complete=a;return this;}
  public override void Kill(){killed=true;}
  public void Step(float time){if(killed)return;foreach(var e in entries){if(!e.Done&&e.At<=time+0.000001f){e.Done=true;DOTween.CallbackTime=e.At;e.Apply?.Invoke();}}if(!finished&&time+0.000001f>=Length){finished=true;DOTween.CallbackTime=Length;complete?.Invoke();}}
 }
 public static class DOTween {public static Sequence Last;public static float CallbackTime;public static Sequence Sequence(){Last=new Sequence();return Last;}public static Tween To(Func<float> getter,Action<float> setter,float end,float duration)=>new Tween{Length=duration,Apply=()=>setter(end)};}
 public static class Extensions {
  public static Tween DOLocalMove(this Transform t,Vector3 value,float duration)=>new Tween{Length=duration,Apply=()=>t.localPosition=value};
  public static Tween DOLocalRotate(this Transform t,Vector3 value,float duration)=>new Tween{Length=duration,Apply=()=>t.localRotation=Quaternion.Euler(value.x,value.y,value.z)};
  public static Tween DOLocalRotateQuaternion(this Transform t,Quaternion value,float duration)=>new Tween{Length=duration,Apply=()=>t.localRotation=value};
 }
}
public class WeaponData {public string WeaponId;public object AttackSound;}
public class WeaponManager {public static WeaponManager Instance=new WeaponManager();public WeaponData CurrentWeapon;}
public class AudioManager {public static AudioManager Instance=new AudioManager();public int Plays;public void PlaySFX(object sound){Plays++;}}
public static class NormalAttackTests {
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(object o,string name,object value){o.GetType().GetField(name,flags).SetValue(o,value);}
 static void Call(object o,string name){o.GetType().GetMethod(name,flags).Invoke(o,null);}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static bool Same(Vector3 a,Vector3 b)=>Math.Abs(a.x-b.x)<.0001&&Math.Abs(a.y-b.y)<.0001&&Math.Abs(a.z-b.z)<.0001;
 public static string Run(){
  var view=new PlayerWeaponView();var root=new Transform{localScale=new Vector3(1,1,1)};
  var pivot=new Transform{localPosition=new Vector3(.17f,-.12f,0),localScale=new Vector3(.8f,1.1f,1),localRotation=Quaternion.Euler(0,0,13)};
  Set(view,"armFacingRoot",root);Set(view,"armPivot",pivot);Call(view,"Awake");
  Vector3 start=pivot.localPosition,scale=pivot.localScale;
  var directions=new[]{new Vector2Int(-1,0),new Vector2Int(1,0),new Vector2Int(0,-1),new Vector2Int(-1,-1),new Vector2Int(1,-1)};
  foreach(string id in new[]{"0","1","2"})foreach(var direction in directions){
   WeaponManager.Instance.CurrentWeapon=new WeaponData{WeaponId=id};int hits=0,ends=0;float hitAt=0,endAt=0;
   view.PlayAttack(direction,()=>{hits++;hitAt=DOTween.CallbackTime;},()=>{ends++;endAt=DOTween.CallbackTime;});
   var sequence=DOTween.Last;Check(Math.Abs(sequence.Length-.23f)<.00001,"changed duration");
   sequence.Step(.129f);Check(hits==0&&ends==0,"early hit");
   sequence.Step(.13f);Check(hits==1&&ends==0&&Math.Abs(hitAt-.13f)<.00001,"hit timing");
   sequence.Step(.229f);Check(ends==0,"early unlock callback");
   sequence.Step(.23f);sequence.Step(1f);Check(hits==1&&ends==1&&Math.Abs(endAt-.23f)<.00001,"duplicate/late callbacks");
   Check(Same(pivot.localPosition,start)&&Same(pivot.localScale,scale)&&pivot.localRotation.z==13,"pose drift");
   Check(root.localScale.x==(direction.x<0?-1:1),"direction mirror");
  }
  int interruptedHits=0;view.PlayAttack(directions[0],()=>interruptedHits++);var old=DOTween.Last;
  view.PlayAttack(directions[1]);old.Step(1f);Check(interruptedHits==0,"killed sequence called hit");
  var current=DOTween.Last;current.Step(.05f);Call(view,"OnDisable");current.Step(1f);
  Check(Same(pivot.localPosition,start)&&Same(pivot.localScale,scale)&&pivot.localRotation.z==13,"disable cleanup");
  var camera=new CameraShakeController();Set(camera,"gameplayCamera",new CinemachineVirtualCamera());Call(camera,"Awake");
  var noise=(CinemachineBasicMultiChannelPerlin)camera.GetType().GetField("noise",flags).GetValue(camera);
  Time.time=0;camera.ShakeBossKill();camera.ShakeAttackImpact(AttackImpactKind.Break);
  Check(Math.Abs(noise.m_AmplitudeGain-.75f)<.001,"normal hit overwrote boss kill");
  camera.ShakeShockwave();
  Check(Math.Abs(noise.m_AmplitudeGain-.75f)<.001,"Shockwave overwrote same-cast boss kill");
  Time.time=.33f;camera.ShakeAttackImpact(AttackImpactKind.Enemy);
  Check(Math.Abs(noise.m_AmplitudeGain-.075f)<.001,"normal hit failed after protection");
  camera.ShakeShield();camera.ShakeAttackImpact(AttackImpactKind.HardTerrain);
  Check(Math.Abs(noise.m_AmplitudeGain-.14f)<.001,"normal hit overwrote Shield");
  Time.time=1;camera.ShakeAttackImpact(AttackImpactKind.HardTerrain);
  Check(Math.Abs(noise.m_AmplitudeGain-.13f)<.001,"hard terrain preset");
  camera.ShakeShockwave();camera.ShakeAttackImpact(AttackImpactKind.Enemy);
  Check(Math.Abs(noise.m_AmplitudeGain-.5f)<.001,"normal hit overwrote Shockwave");
  return "PASS: production PlayerWeaponView, 3 weapons x 5 directions: 130ms hit, 230ms completion, single callbacks, exact pose restore, mirroring, interruption/disable cleanup. Production camera: normal/hard presets, Shield/Shockwave/boss-kill protection. Managed scheduler; Unity not executed.";
 }
}
'@
$source = [IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Player/PlayerWeaponView.cs'))
$source = [regex]::Replace($source, '(?m)^using [^;]+;\r?\n', '')
$source += [regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Camera/CameraShakeController.cs')), '(?m)^using [^;]+;\r?\n', '')
Add-Type ($stub + $source)
[NormalAttackTests]::Run()

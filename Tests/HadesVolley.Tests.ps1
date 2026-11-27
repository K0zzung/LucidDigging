# Extracted production volley coroutine and orb playback, managed scheduler only.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$boss=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Boss/BossEnemy.cs'),[Text.Encoding]::GetEncoding(949))
$start=$boss.IndexOf('    private IEnumerator FireAimedWave()')
$end=$boss.IndexOf('    private void LaunchMouthMissile()', $start)
if($start -lt 0 -or $end -le $start){throw 'Production volley not found'}
$methods=$boss.Substring($start,$end-$start)
$stub=@'
using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
namespace UnityEngine {
 public struct Vector3 {public static Vector3 down=>new Vector3();public static Vector3 up=>new Vector3();public static Vector3 operator+(Vector3 a,Vector3 b)=>a;public static Vector3 operator*(Vector3 a,float b)=>a;}
 public struct Color {public float a;public static Color white=>new Color{a=1};}
 public class Sprite {}
 public class SpriteRenderer {public bool enabled;public Sprite sprite;public Color color;}
 public class Transform {}
 public static class Time {public static float time;}
 public static class Mathf {public static float Clamp(float a,float min,float max)=>Math.Max(min,Math.Min(max,a));public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));}
}
namespace DG.Tweening {
 public enum Ease {OutSine,InOutSine}
 public class Tween {public Tween SetEase(Ease e)=>this;}
 public class Sequence {public void Append(Tween t){} public void Join(Tween t){} public bool IsActive()=>true;public bool IsComplete()=>true;public IEnumerator WaitForCompletion(){yield break;}}
 public static class DOTween {public static Sequence Sequence()=>new Sequence();}
 public static class Extensions {public static Tween DOLocalMove(this Transform t,Vector3 v,float d)=>new Tween();}
}
public class VolleyProbe {
 public bool CanAttack=true;
 public Transform jawPart=new Transform(),headPart=new Transform();
 public SpriteRenderer mouthOrb=new SpriteRenderer();
 public Sprite[] missileFrames=new Sprite[20];
 public float mouthOrbFPS=20,missileVolleyDuration=4,missileFireInterval=.45f,mouthOrbFadeTime=.25f,jawTravel=.14f,missileJawTravelMultiplier=6;
 public Vector3 jawRest,headRest;
 public Sequence tellSequence;
 public int Shots,Restores;
 void HideMouthOrb(){mouthOrb.enabled=false;} void DrawMouthOrb(float age,float alpha){mouthOrb.enabled=true;mouthOrb.color=new Color{a=alpha};} void StopTell(){} void EnsureMissilePool(){} void LaunchMouthMissile(){Shots++;} void RestoreTell(){Restores++;}
 public IEnumerator Cast()=>FireAimedWave();
 public void Frame(float age,float alpha)=>DrawMouthOrb(age,alpha);
 METHODS
}
public static class VolleyTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 public static string Run(){
  var p=new VolleyProbe();for(int i=0;i<20;i++)p.missileFrames[i]=new Sprite();
  Time.time=0;var run=p.Cast();int steps=0;
  while(run.MoveNext()){Time.time+=.05f;if(++steps>200)throw new Exception("volley did not finish");}
  Check(p.Shots==9,"4 second volley emits 9 shots at .45 seconds, not one");Check(!p.mouthOrb.enabled&&p.Restores==1,"orb and jaw cleanup");
  p=new VolleyProbe();Time.time=0;run=p.Cast();run.MoveNext();run.MoveNext();int shots=p.Shots;for(int i=0;i<50;i++)run.MoveNext();Check(p.Shots==shots,"frozen game time produces no extra shots");
  p.CanAttack=false;while(run.MoveNext()){}Check(!p.mouthOrb.enabled,"interruption hides orb");
  return "PASS: production volley schedule, 9 shots/4s, pause-safe firing, pause and interruption cleanup. Managed stand-ins; Unity not executed.";
 }
}
'@
Add-Type -TypeDefinition $stub.Replace('METHODS',$methods)
[VolleyTests]::Run()

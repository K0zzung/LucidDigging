# Production planner + strike sequence, managed stand-ins. Not Unity play/rendering.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stub = @'
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DG.Tweening;
namespace UnityEngine {
 public class Object {public bool Destroyed;}
 public class MonoBehaviour {public GameObject gameObject=new GameObject();public Transform transform=new Transform();public static Dictionary<Type,object> Components=new Dictionary<Type,object>();public T GetComponent<T>()=>(T)Components[typeof(T)];public void Destroy(Object o){o.Destroyed=true;}}
 public class GameObject:Object {}
 public class SerializeField:Attribute {}
 public struct Vector3Int {public int x,y,z;public Vector3Int(int a,int b,int c){x=a;y=b;z=c;}public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 public struct Bounds {public Vector3 size;}
 public struct Color {public float a;public static Color white=>new Color{a=1};}
 public class Sprite {public Bounds bounds=new Bounds{size=new Vector3(1,1,0)};}
 public class SpriteRenderer {public Sprite sprite;public Color color;}
 public class Animator {public bool enabled=true;}
 public class Transform {public Vector3 localScale;}
 public static class Mathf {public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(x,b));public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);}
}
namespace DG.Tweening {
 public enum Ease {OutCubic,InCubic}
 public class Tween {public float Length;public Action Apply;public Tween SetEase(Ease e)=>this;}
 public class Sequence {
  class Entry {public float At;public Action Apply;public bool Done;}
  List<Entry> entries=new List<Entry>();public float Length;float lastStart;bool killed,finished;Action complete;
  public Sequence Append(Tween t){lastStart=Length;Length+=t.Length;entries.Add(new Entry{At=Length,Apply=t.Apply});return this;}
  public Sequence Join(Tween t){Length=Math.Max(Length,lastStart+t.Length);entries.Add(new Entry{At=lastStart+t.Length,Apply=t.Apply});return this;}
  public Sequence AppendInterval(float delay){Length+=delay;return this;}
  public Sequence AppendCallback(Action action){entries.Add(new Entry{At=Length,Apply=action});return this;}
  public Sequence OnComplete(Action action){complete=action;return this;}
  public void Kill(){killed=true;}
  public void Step(float time){if(killed)return;foreach(var e in entries)if(!e.Done&&e.At<=time+.000001f){e.Done=true;e.Apply?.Invoke();}if(!finished&&time+.000001f>=Length){finished=true;complete?.Invoke();}}
 }
 public static class DOTween {public static Sequence Last;public static Sequence Sequence()=>Last=new Sequence();}
 public static class Extensions {
  public static Tween DOScaleY(this Transform t,float y,float duration)=>new Tween{Length=duration,Apply=()=>t.localScale=new Vector3(t.localScale.x,y,t.localScale.z)};
  public static Tween DOFade(this SpriteRenderer r,float a,float duration)=>new Tween{Length=duration,Apply=()=>r.color=new Color{a=a}};
 }
}
public static class HadesTests {
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 public static string Run(){
  var danger=new List<Vector3Int>();int checkedCases=0,pruned=0,skipped=0;
  var steps=new[]{new Vector3Int(-1,0,0),new Vector3Int(1,0,0),new Vector3Int(0,-1,0),new Vector3Int(-1,-1,0),new Vector3Int(1,-1,0)};
  for(int kind=0;kind<5;kind++)for(int x=0;x<7;x++)for(int row=0;row<4;row++)for(int mask=0;mask<32;mask++)for(int rev=0;rev<2;rev++){
   var player=new Vector3Int(x,row,0);var blocked=new HashSet<Vector3Int>();for(int n=0;n<5;n++)if((mask&(1<<n))!=0)blocked.Add(player+steps[n]);
   HadesSpikePatterns.Build((HadesSpikePattern)kind,0,7,0,4,player,rev==1,danger);
   Check(danger.Count==new HashSet<Vector3Int>(danger).Count,"duplicate danger cell");
   foreach(var c in danger)Check(c.x>=0&&c.x<7&&c.y>=0&&c.y<4,"outside arena band");
   int before=danger.Count;
   Func<Vector3Int,bool> free=c=>c.x>=0&&c.x<7&&!blocked.Contains(c);
   bool allowed=HadesSpikePatterns.ReserveEscape(player,danger,free);
   if(allowed)Check(HadesSpikePatterns.HasEscape(player,danger,free),"unsafe pattern accepted");else skipped++;
   if(before!=danger.Count)pruned++;
   checkedCases++;
  }
  for(int width=3;width<=9;width++)for(int parity=0;parity<2;parity++) {
   var first=new List<Vector3Int>();var second=new List<Vector3Int>();
   HadesSpikePatterns.Build(HadesSpikePattern.Fork,-2,width,5,4,new Vector3Int(0,5,0),parity==1,first);
   HadesSpikePatterns.BuildComplement(-2,width,5,first,second);
   Check(first.Count+second.Count==width,"complement covers arena columns");
   foreach(var cell in second)Check(!first.Contains(cell)&&cell.y==5,"followup is disjoint and row locked");
   var removed=first[0];first.RemoveAt(0);
   HadesSpikePatterns.BuildComplement(-2,width,5,first,second);
   Check(second.Contains(removed),"complement follows actual first wave after safety pruning");
  }
  for(int width=7;width<=10;width++) {
   var bases=new List<Vector3Int>();var horizontal=new List<Vector3Int>();
   HadesSpikePatterns.BuildHorizontal(-2,width,5,bases,horizontal);
   Check(bases.Count==2&&horizontal.Count==6,"two 3-cell horizontal attacks");
   Check(new HashSet<Vector3Int>(horizontal).Count==6,"no duplicate side cells");
   foreach(var c in horizontal)Check(c.y==5&&c.x>=-2&&c.x<width-2,"3x1 horizontal footprint");
   Check(!horizontal.Contains(new Vector3Int(1,5,0)),"central escape lane");
  }
  Check(HadesSpikePatterns.IsAvailable(HadesSpikePattern.Horizontal,1f),"horizontal pattern available");
  Check(HadesSpikePatterns.PatternCount(1)==2&&HadesSpikePatterns.PatternCount(.7f)==4&&HadesSpikePatterns.PatternCount(.35f)==5,"phase boundaries");
  Check(HadesSpikePatterns.IsAvailable(HadesSpikePattern.AcceleratingHunt,1f),"pursuit availability");
  Check(HadesSpikePatterns.IsAvailable(HadesSpikePattern.AimedWave,1f),"aimed wave availability");
  return "PASS: production planner "+checkedCases+" arena/occupancy cases; single, alternating columns with complementary followup, sweep and safe lane; valid 5-direction escape and phase thresholds. Pattern geometry only; registered sprite-frame playback is checked separately. Unity not executed.";
 }
}
'@
foreach($file in @('Assets/Scripts/Boss/HadesSpikePatterns.cs')) {
 $stub += [regex]::Replace([IO.File]::ReadAllText((Join-Path $root $file)), '(?m)^using [^;]+;\r?\n', '')
}
Add-Type $stub
[HadesTests]::Run()

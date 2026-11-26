# Managed tests of the production skill owner. Does not run Unity or render VFX.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stub = @'
#pragma warning disable 0649
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public class MonoBehaviour {
  public Transform transform=new Transform();
  public static Dictionary<Type,object> Components=new Dictionary<Type,object>();
  public List<Coroutine> Running=new List<Coroutine>();
  public T GetComponent<T>() {return (T)Components[typeof(T)];}
  public Coroutine StartCoroutine(IEnumerator routine){var c=new Coroutine{Routine=routine};if(routine.MoveNext())Running.Add(c);return c;}
  public void StopCoroutine(Coroutine c){Running.Remove(c);}
  public void Step(){foreach(var c in Running.ToArray())if(!c.Routine.MoveNext())Running.Remove(c);}
  public T Instantiate<T>(T source,Vector3 pos,Quaternion rot){return source;}
 }
 public class Coroutine {public IEnumerator Routine;}
 public class Transform {public Vector3 position;}
 public struct Vector3 {}
 public struct Quaternion {public static Quaternion identity;}
 public struct Vector2Int {public static Vector2Int zero; public static bool operator==(Vector2Int a,Vector2Int b)=>true;public static bool operator!=(Vector2Int a,Vector2Int b)=>false;public override bool Equals(object o)=>false;public override int GetHashCode()=>0;}
 public class SerializeField:Attribute {}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string s){}}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}
 public static class Time {public static float time;}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);}
 public class WaitForSeconds {public WaitForSeconds(float seconds){}}
 public static class Debug {public static void Log(object o){}public static void LogError(object o){}}
}
public enum SkillType {Dash,Shield,Shockwave}
public enum PermanentUpgradeType {Dash,Shield,Shockwave}
public class PlayerStatus {
 public int Bonus; public int Defense=>5+Bonus;public event Action OnPlayerDied;
 public void AddTemporaryDefense(int x){Bonus+=x;}public void RemoveTemporaryDefense(int x){Bonus-=x;}
 public void Die(){OnPlayerDied?.Invoke();}
}
public class PlayerGridController {public bool StartDash(Vector2Int d,float a,float b,float c,float e,Action f)=>true;public void PerformShockwave(int a,float b){}}
public class ShieldEffect {}
public class ShockwaveEffect {public void Play(Vector3 p,int s){}}
public class PermanentUpgradeManager {public static PermanentUpgradeManager Instance; public int GetLevel(PermanentUpgradeType t)=>0;}
public class RunSkillManager {public static RunSkillManager Instance=new RunSkillManager();public int Consumed;public bool HasSkill(SkillType t)=>true;public bool TryConsumeSkill(SkillType t){Consumed++;return true;}}
public static class ShieldTests {
 static void Call(object o,string method){o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);}
 static void Set(object o,string field,object value){o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 public static string Run(){
  var status=new PlayerStatus();var skills=new PlayerSkillController();
  MonoBehaviour.Components[typeof(PlayerStatus)]=status;MonoBehaviour.Components[typeof(PlayerGridController)]=new PlayerGridController();
  Call(skills,"Awake");Call(skills,"OnEnable");Set(skills,"shieldDuration",15f);
  Time.time=0;Check(skills.TryUseSkill(SkillType.Shield,Vector2Int.zero),"cast rejected");
  Check(status.Defense==55&&skills.IsShieldActive&&skills.ShieldRemainingTime==15,"activation");
  Time.time=0.2f;skills.Step();
  Check(skills.TryUseSkill(SkillType.Shield,Vector2Int.zero),"refresh rejected");
  Check(status.Defense==55&&Math.Abs(skills.ShieldRemainingTime-15)<.001,"refresh stacks or fails to reset");
  Check(skills.Running.Count==2,"coroutines accumulated");
  skills.Step(); // Frozen scaled time: the buff must not count down.
  Check(Math.Abs(skills.ShieldRemainingTime-15)<.001&&status.Defense==55,"pause mismatch");
  Time.time=15.19f;skills.Step();Check(skills.IsShieldActive,"early expiry");
  Time.time=15.21f;skills.Step();Check(!skills.IsShieldActive&&skills.ShieldRemainingTime==0&&status.Defense==5,"expiry cleanup");
  Check(skills.TryUseSkill(SkillType.Shield,Vector2Int.zero),"second cast");status.Die();
  Check(!skills.IsShieldActive&&status.Defense==5,"death cleanup");
  Call(skills,"OnDisable");Check(skills.Running.Count==0&&!skills.IsSkillLocked,"disable cleanup");
  Call(skills,"OnEnable");Time.time=20;Check(skills.TryUseSkill(SkillType.Shield,Vector2Int.zero),"reenable cast");
  Call(skills,"OnDisable");Call(skills,"OnDisable");
  Check(status.Defense==5&&!skills.IsShieldActive&&skills.Running.Count==0,"repeated cleanup");
  Check(RunSkillManager.Instance.Consumed==4,"skill consumption changed");
  return "PASS: production Shield activation, 15s lifetime, refresh without defense stacking, shared remaining time, frozen scaled time, expiry, death, disable/reenable, coroutine cleanup and one charge per cast. Managed stand-ins; Unity not executed.";
 }
}
'@
$source = [IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Player/PlayerSkillController.cs'))
$source = [regex]::Replace($source, '(?m)^using [^;]+;\r?\n', '')
Add-Type ($stub + $source)
[ShieldTests]::Run()

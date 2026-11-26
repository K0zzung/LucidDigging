# Production manager/pickup tests with managed Unity stand-ins. Not Unity playback.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stub = @'
#pragma warning disable 0649
using System;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public class MonoBehaviour {public object gameObject=new object();public static PlayerStatus Player=new PlayerStatus();public T FindObjectOfType<T>() where T:class=>Player as T;public void Destroy(object value){}}
 public class SerializeField:Attribute {}
 public static class Debug {public static void Log(object o){}public static void LogError(object o){}public static void LogException(Exception e,object context){}}
}
public class PlayerStatus {public bool IsDead;public event Action OnPlayerDied;public void Die(){IsDead=true;OnPlayerDied?.Invoke();}}
public abstract class GridPickup {protected abstract bool TryApplyPickupEffect();public bool Collected;public bool TryCollect(){if(Collected)return false;if(!TryApplyPickupEffect())return false;Collected=true;return true;}}
public class AudioManager {public static AudioManager Instance=new AudioManager();public int Pickups;public void PlayItemPickup(){Pickups++;}}
public static class EvolutionTests {
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Call(object o,string n){o.GetType().GetMethod(n,flags).Invoke(o,null);}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static bool Pickup(SkillType t){var p=new SkillItemPickup();typeof(SkillItemPickup).GetField("skillType",flags).SetValue(p,t);bool ok=p.TryCollect();Check(p.Collected==ok,"pickup retention");if(ok)Check(!p.TryCollect(),"duplicate pickup");return ok;}
 public static string Run(){
  var m=new RunSkillManager();Call(m,"Awake");Call(m,"Start");int events=0;
  m.OnSkillChargeChanged+=(t,c)=>events++;
  Check(Pickup(SkillType.Dash)&&Pickup(SkillType.Dash),"duplicate Dash acquisition");
  Check(m.DashCharges==2&&m.GetTotalPickups(SkillType.Dash)==2&&m.GetEvolutionLevel(SkillType.Dash)==2,"Dash state");
  for(int i=0;i<3;i++)Check(Pickup(SkillType.Shield),"three Shields");
  int sounds=AudioManager.Instance.Pickups,notifications=events;
  Check(!Pickup(SkillType.Shield)&&m.ShieldCharges==3&&m.GetTotalPickups(SkillType.Shield)==3,"capacity");
  Check(sounds==AudioManager.Instance.Pickups&&events==notifications,"failed pickup sound/event");
  Check(m.TryConsumeSkill(SkillType.Shield)&&Pickup(SkillType.Shield),"consume/reacquire");
  Check(m.GetTotalPickups(SkillType.Shield)==4&&m.GetEvolutionLevel(SkillType.Shield)==3,"fourth evolution");
  for(int i=0;i<3;i++)Check(m.TryConsumeSkill(SkillType.Shield),"consume");
  Check(!m.HasSkill(SkillType.Shield)&&!m.TryConsumeSkill(SkillType.Shield)&&m.GetEvolutionLevel(SkillType.Shield)==3,"empty retains level");
  Check(m.ShockwaveCharges==0&&m.GetEvolutionLevel(SkillType.Shockwave)==0&&m.DashCharges==2,"independence");
  int success=0;for(int i=0;i<100;i++)if(m.TryConsumeSkill(SkillType.Dash))success++;
  Check(success==2&&m.DashCharges==0&&m.GetEvolutionLevel(SkillType.Dash)==2,"rapid consumption bound");
  Check(Pickup(SkillType.Dash)&&m.GetTotalPickups(SkillType.Dash)==3&&m.GetEvolutionLevel(SkillType.Dash)==2,"third pickup");
  Check(Pickup(SkillType.Shockwave),"Shockwave pickup");Check(Pickup(SkillType.Shield),"Shield before death");MonoBehaviour.Player.Die();
  foreach(SkillType t in Enum.GetValues(typeof(SkillType)))Check(m.GetCharges(t)==0&&m.GetTotalPickups(t)==0&&m.GetEvolutionLevel(t)==0,"death reset");
  Check(!Pickup(SkillType.Dash),"dead acquisition");MonoBehaviour.Player.IsDead=false;
  Check(Pickup(SkillType.Dash),"new run pickup");m.ResetRunSkills();Check(m.DashCharges==0&&m.GetEvolutionLevel(SkillType.Dash)==0,"explicit reset");
  Check(!m.TryAddSkillItem((SkillType)99)&&!m.TryConsumeSkill((SkillType)99),"invalid enum");
  m.OnSkillChargeChanged+=(t,c)=>{throw new Exception("simulated HUD observer failure");};
  Check(Pickup(SkillType.Dash)&&m.DashCharges==1,"observer makes pickup retryable");
  return "PASS: production manager and pickup effect: independent charges/totals/evolution, duplicate acquisition, cap retention/no SFX, consumption/reacquisition, fourth pickup Lv3, 100 consume attempts bounded by charges, death/new-run reset and observer safety. Managed stand-ins; Unity not executed.";
 }
}
'@
foreach ($file in @('Assets/Scripts/Player/SkillType.cs','Assets/Scripts/Player/RunSkillManager.cs','Assets/Scripts/Player/SkillItemPickup.cs')) {
 $stub += [regex]::Replace([IO.File]::ReadAllText((Join-Path $root $file)), '(?m)^using [^;]+;\r?\n', '')
}
Add-Type $stub
[EvolutionTests]::Run()

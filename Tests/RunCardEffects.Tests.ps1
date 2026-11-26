# Run in a fresh process: pwsh -File Tests/RunCardEffects.Tests.ps1
# Managed stand-ins exercise production logic; this is not a Unity play test.
$ErrorActionPreference = 'Stop'
$stub = @'
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
namespace UnityEngine {
 public class Object {public static void Destroy(object value){}}
 public class MonoBehaviour:Object {public bool isActiveAndEnabled=true;public Transform transform=new Transform();}
 public class ScriptableObject:Object {}
 public class Transform {public Vector3 position;}
 public struct Vector3 {public static Vector3 up{get{return new Vector3();}}public static Vector3 operator*(Vector3 a,float b){return a;}public static Vector3 operator+(Vector3 a,Vector3 b){return a;}}
 public static class Time {public static int frameCount;}
 public static class Mathf {public static int Max(int a,int b){return Math.Max(a,b);}public static int Min(int a,int b){return Math.Min(a,b);}public static int RoundToInt(float f){return (int)Math.Round(f);}public static float Clamp01(float f){return Math.Max(0,Math.Min(1,f));}}
 public static class PlayerPrefs {public static Dictionary<string,int> Values=new Dictionary<string,int>();public static int Writes;public static int GetInt(string key,int fallback){int value;return Values.TryGetValue(key,out value)?value:fallback;}public static void SetInt(string key,int value){Values[key]=value;Writes++;}public static void Save(){}}
 public class SerializeField:Attribute {}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string value){}}
 public class MinAttribute:Attribute {public MinAttribute(float value){}}
 public class TextAreaAttribute:Attribute {}
 public class CreateAssetMenuAttribute:Attribute {public string fileName,menuName;}
 public static class Debug {public static void Log(object value){}public static void LogException(Exception exception,object context){}}
}
namespace UnityEngine.UI {public class Image {public float fillAmount;}public class Button {public bool interactable;}}
public class WeaponManager {public static WeaponManager Instance=new WeaponManager();public int GetAdditionalAttack(){return 3;}}
public class FloatingDamageManager {public static FloatingDamageManager Instance;public void ShowReceivedDamage(Vector3 position,int damage){}}
// P1-2 state and pause behavior is unchanged; only adapt the selection contract here.
public class GameFlowController {public static GameFlowController Instance;public PlayerStatus Status;public GameState CurrentState=GameState.LevelUpSelection;public bool CanConfirmRunCard{get{return CurrentState==GameState.LevelUpSelection&&!Status.IsDead;}}public void EndLevelUpSelection(){CurrentState=Status.IsDead?GameState.GameOver:GameState.Playing;}}
public class SelectionHarness {
 public bool acceptingSelection=true,IsOpen=true;
 public int offerVersion,offerFrame;
 public GameFlowController gameFlow;
 public RunProgression progression;
 public MonoBehaviour selectionHandler;
 public RunCardData[] offeredCards;
 public Button[] buttons={new Button(),new Button(),new Button()};
 public event Action<RunCardData> OnCardSelected;
 void ShowNextOffer(){offerVersion++;offerFrame=Time.frameCount;acceptingSelection=true;}
 void Hide(){IsOpen=false;acceptingSelection=false;offeredCards=null;offerVersion++;}
 public void Click(int version){SelectCard(0,version);}
'@
$tests = @'
}
public static class RunCardEffectsTests {
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static void Put(object target,string field,object value){target.GetType().GetField(field,flags).SetValue(target,value);}
 static object Call(object target,string method){return target.GetType().GetMethod(method,flags).Invoke(target,null);}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static RunCardData Card(RunCardEffectType type,int attack,int defense,int health,int heal){var card=new RunCardData();Put(card,"id",type.ToString());Put(card,"effectType",type);Put(card,"attackBonus",attack);Put(card,"defenseBonus",defense);Put(card,"maxHealthBonus",health);Put(card,"healAmount",heal);return card;}
 static PlayerStatus Status(){var status=new PlayerStatus();Call(status,"Awake");return status;}
 static RunProgression Progression(){if(RunProgression.Instance!=null)Call(RunProgression.Instance,"OnDestroy");var xp=new RunProgression();Call(xp,"Awake");return xp;}
 static RunCardEffectHandler Handler(PlayerStatus status,RunProgression xp,GameFlowController flow){var handler=new RunCardEffectHandler();Put(handler,"playerStatus",status);Put(handler,"progression",xp);Put(handler,"gameFlow",flow);Call(handler,"Start");return handler;}
 public static string Run(){
 PlayerPrefs.Values["Player_MaxHP"]=100;PlayerPrefs.Values["Player_Attack"]=10;PlayerPrefs.Values["Player_Defense"]=5;
 var attack=Card(RunCardEffectType.Attack,2,0,0,0);var defense=Card(RunCardEffectType.Defense,0,1,0,0);var health=Card(RunCardEffectType.MaxHealth,0,0,15,0);var heal=Card(RunCardEffectType.Heal,0,0,0,20);var both=Card(RunCardEffectType.AttackAndDefense,1,1,0,0);
 var status=Status();var xp=Progression();xp.AddExperience(1000);var flow=new GameFlowController{Status=status};var handler=Handler(status,xp,flow);
 int initialPending=xp.PendingLevelUps;int baselineDamage=DamageCalculator.CalculateDamage(status.Attack,0);int baselineReceived=DamageCalculator.CalculateDamage(1000,status.Defense);
 Check(handler.TrySelectCard(attack)&&handler.TrySelectCard(attack),"attack selections");Check(status.Attack==17&&status.BaseAttack==10&&status.RunAttackBonus==4,"attack stacking and weapon");Check(DamageCalculator.CalculateDamage(status.Attack,0)==baselineDamage+4,"attack damage");Check(Mathf.RoundToInt(status.Attack*200/100f)==34&&Mathf.RoundToInt(status.Attack*150/100f)==26,"skill damage inputs");
 status.AddTemporaryDefense(50);Check(handler.TrySelectCard(defense)&&handler.TrySelectCard(both),"defense selections");Check(status.Defense==57&&status.BaseDefense==5,"shield and run defense");status.RemoveTemporaryDefense(50);Check(status.Defense==7&&DamageCalculator.CalculateDamage(1000,status.Defense)<baselineReceived,"defense damage");
 var hpUI=new PlayerHealthBarUI();var fill=new Image();Put(hpUI,"playerStatus",status);Put(hpUI,"hpFill",fill);Call(hpUI,"OnEnable");int statsEvents=0;status.OnStatsChanged+=()=>statsEvents++;
 status.TakeDamage(30);Check(handler.TrySelectCard(health)&&status.MaxHP==115&&status.CurrentHP==85,"max/current health");Check(Math.Abs(fill.fillAmount-85f/115)<.0001f,"health HUD");Check(handler.TrySelectCard(heal)&&status.CurrentHP==105,"healing");Check(handler.TrySelectCard(heal)&&status.CurrentHP==115,"heal cap");Check(statsEvents==3&&xp.PendingLevelUps==initialPending-7,"notifications and reward count");
 Check(PlayerPrefs.Writes==0,"run card saved data");status.SaveStats();Check(PlayerPrefs.Values["Player_MaxHP"]==100&&PlayerPrefs.Values["Player_Attack"]==10&&PlayerPrefs.Values["Player_Defense"]==5,"save excludes run bonuses");
 status.IncreasePermanentMaxHP(5);status.IncreasePermanentAttack(3);status.IncreasePermanentDefense(2);Check(PlayerPrefs.Values["Player_MaxHP"]==105&&PlayerPrefs.Values["Player_Attack"]==13&&PlayerPrefs.Values["Player_Defense"]==7,"permanent upgrade");Check(status.MaxHP==120&&status.Attack==21&&status.Defense==9,"permanent plus run stats");
 xp.ResetRun();Check(status.RunAttackBonus==0&&status.RunDefenseBonus==0&&status.RunMaxHPBonus==0&&status.MaxHP==105&&status.CurrentHP==105,"run reset");Check(!handler.TrySelectCard(attack),"no reward");xp.AddExperience(20);status.TakeDamage(10000);Check(!handler.TrySelectCard(heal),"dead player heal");var fresh=Status();Check(fresh.CurrentHP==105&&fresh.RunMaxHPBonus==0&&fresh.Attack==16&&fresh.Defense==7,"scene recreation");
 status=Status();xp=Progression();xp.AddExperience(100);flow=new GameFlowController{Status=status};handler=Handler(status,xp,flow);bool reentered=false;xp.OnProgressionChanged+=()=>{reentered=handler.TrySelectCard(attack);};Action fail=()=>{throw new Exception("observer failure");};xp.OnProgressionChanged+=fail;status.OnHPChanged+=(current,max)=>{throw new Exception("HUD failure");};Check(handler.TrySelectCard(attack)&&!reentered&&status.RunAttackBonus==2&&xp.PendingLevelUps==2,"observer/reentrancy safety");
 status=Status();xp=Progression();xp.AddExperience(100);flow=new GameFlowController{Status=status};handler=Handler(status,xp,flow);var ui=new SelectionHarness{gameFlow=flow,progression=xp,selectionHandler=handler,offeredCards=new[]{attack,defense,health}};int selections=0;ui.OnCardSelected+=card=>selections++;
 for(int i=0;i<3;i++){int old=ui.offerVersion;Time.frameCount++;ui.Click(old);ui.Click(old);ui.Click(ui.offerVersion);}
 Check(selections==3&&xp.PendingLevelUps==0&&!ui.IsOpen&&flow.CurrentState==GameState.Playing,"three rewards and duplicate clicks");Check(status.RunAttackBonus==6,"effect exactly once per UI choice");
 var invalid=Card((RunCardEffectType)99,10,10,10,10);xp.AddExperience(50);flow.CurrentState=GameState.LevelUpSelection;int pending=xp.PendingLevelUps;Check(!handler.TrySelectCard(invalid)&&xp.PendingLevelUps==pending,"invalid effect");
 Check(health.Description.Contains("15")&&heal.Description.Contains("20"),"data-driven descriptions");
 return "PASS: production status/card/handler/progression/HP HUD and extracted UI selection logic: stacking, damage inputs, defense/shield separation, healing, reward counts, save isolation, reset/death, observer safety, three choices and duplicate-click guards. Unity not executed.";
 }
}
'@
$projectRoot = Split-Path $PSScriptRoot -Parent
$uiSource = [IO.File]::ReadAllText((Join-Path $projectRoot 'Assets/Scripts/UI/RunCardSelectionUI.cs'))
$start = $uiSource.IndexOf('    private void SelectCard(')
$end = $uiSource.IndexOf('    private void HandleStateChanged(', $start)
if ($start -lt 0 -or $end -le $start) { throw 'Cannot locate production UI selection method.' }
$source = $stub + $uiSource.Substring($start, $end - $start) + $tests
foreach ($file in @('Assets/Scripts/Player/PlayerStatus.cs','Assets/Scripts/Player/RunCardRarity.cs','Assets/Scripts/Player/RunCardData.cs','Assets/Scripts/Player/IRunCardSelectionHandler.cs','Assets/Scripts/Player/RunCardEffectHandler.cs','Assets/Scripts/Player/RunProgression.cs','Assets/Scripts/UI/PlayerHealthBarUI.cs','Assets/Scripts/System/GameState.cs','Assets/Scripts/System/DamageCalculator.cs')) {
    $source += [regex]::Replace([IO.File]::ReadAllText((Join-Path $projectRoot $file)), '(?m)^using [^;]+;\r?\n', '')
}
Add-Type -TypeDefinition $source
[RunCardEffectsTests]::Run()

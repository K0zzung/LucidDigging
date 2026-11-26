# Fresh process: pwsh -File Tests/RunCardRarity.Tests.ps1
# Synthetic in-memory cards only. No production balance assets or Unity state are changed.
$ErrorActionPreference = 'Stop'
$stub = @'
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public class ScriptableObject {}
 public class Sprite {}
 public class SerializeField:Attribute {}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string value){}}
 public class MinAttribute:Attribute {public MinAttribute(int value){}}
 public class TextAreaAttribute:Attribute {}
 public class CreateAssetMenuAttribute:Attribute {public string fileName,menuName;}
 public static class Mathf {public static int Max(int a,int b){return Math.Max(a,b);}}
 public static class Random {static System.Random random=new System.Random(5);public static float value{get{return (float)random.NextDouble();}}public static int Range(int a,int b){return random.Next(a,b);}}
 public static class Time {public static float timeScale=1;}
 public static class Debug {public static int Errors;public static void LogError(string message,object context){Errors++;}}
}
public enum GameState {Playing,LevelUpSelection,GameOver}
public class RunProgression {public int PendingLevelUps=3;}
public class GameFlowController {
 public GameState CurrentState=GameState.Playing;public Func<bool> Ready;public int BeginCalls,EndCalls;
 public bool CanConfirmRunCard{get{return CurrentState==GameState.LevelUpSelection;}}
 public bool TryBeginLevelUpSelection(){BeginCalls++;if(Ready!=null&&!Ready())return false;CurrentState=GameState.LevelUpSelection;Time.timeScale=0;return true;}
 public void EndLevelUpSelection(){EndCalls++;if(CurrentState==GameState.LevelUpSelection){CurrentState=GameState.Playing;Time.timeScale=1;}}
}
public class UIReference {public bool isActiveAndEnabled=true;}
public class EventSystem {public static EventSystem current=new EventSystem();}
public class UIRoot {public bool activeSelf;public void SetActive(bool value){activeSelf=value;}}
public class RarityFailureHarness {
 public bool configured=true,configurationFailed,isActiveAndEnabled=true,acceptingSelection;
 public UIReference parentCanvas=new UIReference(),selectionHandler=new UIReference();
 public UIRoot root=new UIRoot();
 public bool IsOpen{get{return root.activeSelf;}}
 public RunProgression progression=new RunProgression();
 public GameFlowController gameFlow=new GameFlowController();
 public List<RunCardData> candidates=new List<RunCardData>();
 public RunCardRaritySettings raritySettings;
 public RunCardData[] offeredCards;
 public int offerVersion;
 void UpdateSafeArea(){}
 void ShowNextOffer(){}
 public void Tick(){LateUpdate();}
'@
$tests = @'
}
public static class RarityTests {
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Put(object target,string field,object value){target.GetType().GetField(field,flags).SetValue(target,value);}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static RunCardData Card(string id,string family,RunCardRarity rarity){var card=new RunCardData();Put(card,"id",id);Put(card,"baseCardId",family);Put(card,"rarity",rarity);Put(card,"attackBonus",2);return card;}
 static RunCardRaritySettings Settings(int common,int rare){var settings=new RunCardRaritySettings();var entries=new RunCardRaritySettings.Entry[5];for(int i=0;i<5;i++)entries[i]=new RunCardRaritySettings.Entry{rarity=(RunCardRarity)i,enabled=i!=4,weight=i==0?common:i==1?rare:0,frame=new Sprite()};Put(settings,"entries",entries);return settings;}
 static RunCardData[] Draw(List<RunCardData> pool,RunCardRaritySettings settings,System.Random rng){RunCardData[] result;string error;Check(RunCardSampler.TryDrawThree(pool,settings,out result,out error,rng),error);Check(new HashSet<string>(new[]{result[0].BaseCardId,result[1].BaseCardId,result[2].BaseCardId}).Count==3,"duplicate family");return result;}
 public static string Run(){
 var common=new List<RunCardData>();for(int i=0;i<5;i++)common.Add(Card("common_"+i,"family_"+i,RunCardRarity.Common));
 var pool=new List<RunCardData>(common);for(int i=0;i<3;i++)pool.Add(Card("rare_"+i,"family_"+i,RunCardRarity.Rare));pool.Add(Card("epic","epic_family",RunCardRarity.Epic));pool.Add(Card("nightmare","nightmare_family",RunCardRarity.Nightmare));
 var settings=Settings(100,0);settings.GetEntry(RunCardRarity.Nightmare).weight=100000;var rng=new System.Random(12345);var seen=new HashSet<string>();
 for(int i=0;i<10000;i++){var offer=Draw(pool,settings,rng);foreach(var card in offer){Check(card.Rarity==RunCardRarity.Common,"zero-weight or nightmare appeared");seen.Add(card.Id);}}Check(seen.Count==5,"existing common pool coverage");
 settings=Settings(70,30);settings.GetEntry(RunCardRarity.Legendary).weight=100000;settings.GetEntry(RunCardRarity.Nightmare).weight=100000;
 const int count=100000;int commonCount=0,rareCount=0;var perCard=new Dictionary<string,int>();rng=new System.Random(6789);
 for(int i=0;i<count;i++){var offer=Draw(pool,settings,rng);var first=offer[0];if(first.Rarity==RunCardRarity.Common)commonCount++;else if(first.Rarity==RunCardRarity.Rare)rareCount++;else throw new Exception("empty/zero/nightmare rarity selected");if(!perCard.ContainsKey(first.Id))perCard[first.Id]=0;perCard[first.Id]++;}
 Check(Math.Abs(commonCount/(double)count-.7)<.006&&Math.Abs(rareCount/(double)count-.3)<.006,"weighted distribution");
 foreach(var card in common)Check(Math.Abs(perCard[card.Id]/(double)count-.14)<.004,"common card conditional distribution");for(int i=0;i<3;i++)Check(Math.Abs(perCard["rare_"+i]/(double)count-.10)<.004,"rare card conditional distribution");
 var scaled=Settings(700,300);var firstRng=new System.Random(12);var secondRng=new System.Random(12);
 for(int i=0;i<1000;i++){var a=Draw(pool,settings,firstRng);var b=Draw(pool,scaled,secondRng);for(int j=0;j<3;j++)Check(a[j].Id==b[j].Id,"weights are not relative");}
 settings.GetEntry(RunCardRarity.Rare).enabled=false;for(int i=0;i<1000;i++)foreach(var card in Draw(pool,settings,rng))Check(card.Rarity==RunCardRarity.Common,"disabled rarity appeared");
 settings=Settings(100,0);var repeated=new List<RunCardData>(common);repeated.Add(common[0]);firstRng=new System.Random(33);secondRng=new System.Random(33);for(int i=0;i<1000;i++){var a=Draw(common,settings,firstRng);var b=Draw(repeated,settings,secondRng);for(int j=0;j<3;j++)Check(a[j].Id==b[j].Id,"repeated reference changed probability");}
 string error;RunCardData[] result;var ambiguous=new List<RunCardData>(common);ambiguous.Add(Card(common[0].Id,"other",RunCardRarity.Common));Check(!RunCardSampler.TryDrawThree(ambiguous,settings,out result,out error,rng)&&result==null,"ambiguous duplicate ID accepted");
 var scarce=new List<RunCardData>{common[0],common[1],Card("variant","family_0",RunCardRarity.Rare)};Check(!RunCardSampler.TryDrawThree(scarce,Settings(100,100),out result,out error,rng)&&result==null,"insufficient families accepted");
 var malformed=new List<RunCardData>{common[0],common[1],Card("missing","",RunCardRarity.Common)};Check(!RunCardSampler.CanDrawThree(malformed,settings,out error),"missing family accepted");settings.GetEntry(RunCardRarity.Common).frame=null;Check(!RunCardSampler.CanDrawThree(common,settings,out error),"missing active frame accepted");
 // Extracted production failure path: preserve rewards, release an owned pause, log once.
 var ui=new RarityFailureHarness{raritySettings=Settings(100,0),candidates=new List<RunCardData>{common[0],common[1]}};ui.gameFlow.Ready=()=>ui.IsReady;Debug.Errors=0;for(int i=0;i<500;i++)ui.Tick();Check(Debug.Errors==1&&!ui.IsReady&&!ui.IsOpen&&ui.gameFlow.BeginCalls==0&&ui.progression.PendingLevelUps==3&&Time.timeScale==1,"failure retry/pause/reward loop");
 ui=new RarityFailureHarness{raritySettings=Settings(100,0),candidates=new List<RunCardData>(common)};ui.gameFlow.Ready=()=>ui.IsReady;ui.Tick();Check(ui.IsOpen&&Time.timeScale==0,"valid selection entry");ui.candidates.RemoveRange(2,3);Debug.Errors=0;for(int i=0;i<500;i++)ui.Tick();Check(Debug.Errors==1&&!ui.IsOpen&&Time.timeScale==1&&ui.progression.PendingLevelUps==3,"paused configuration failure cleanup");
 return "PASS: 100000 weighted offers / 300000 draws: first-slot Common="+commonCount+", Rare="+rareCount+". Per-card uniformity, relative weights, cross-rarity family exclusion, zero/disabled/empty rarities, disabled Nightmare exclusion, duplicate references, invalid pools and 500-frame error/pause/reward guards passed. Not a Unity play test.";
 }
}
'@
$projectRoot = Split-Path $PSScriptRoot -Parent
$ui = [IO.File]::ReadAllText((Join-Path $projectRoot 'Assets/Scripts/UI/RunCardSelectionUI.cs'))
function Get-MethodText([string]$signature) {
    $start=$ui.IndexOf($signature)
    if ($start -lt 0) { throw "Missing method: $signature" }
    $position=$ui.IndexOf('{',$start);$depth=1;$position++
    while ($depth -gt 0) { if($ui[$position] -eq '{'){$depth++};if($ui[$position] -eq '}'){$depth--};$position++ }
    $ui.Substring($start,$position-$start)
}
$start=$ui.IndexOf('    public bool IsReady =>')
$ready=$ui.Substring($start,$ui.IndexOf(';',$start)-$start+1)
$source=$stub+$ready+(Get-MethodText '    private void LateUpdate(')+(Get-MethodText '    private void ReportConfigurationError(')+(Get-MethodText '    private void Hide(')+$tests
foreach($file in @('RunCardData','RunCardRarity','RunCardRaritySettings','RunCardSampler')) {
    $source += [regex]::Replace([IO.File]::ReadAllText((Join-Path $projectRoot "Assets/Scripts/Player/$file.cs")), '(?m)^using [^;]+;\r?\n', '')
}
Add-Type -TypeDefinition $source
[RarityTests]::Run()

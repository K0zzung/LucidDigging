# pwsh -NoProfile -File Tests/RunCardAssets.Tests.ps1
# Loads the actual YAML card/config assets and tests production managed logic.
# Unity serialization, rendering and scene lifecycle are not executed.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$effectsScript = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'RunCardEffects.Tests.ps1'))
function Get-CSharpBlock([string]$name) {
    $match = [regex]::Match($effectsScript, '(?s)\$' + $name + " = @'\r?\n(.*?)\r?\n'@")
    if (!$match.Success) { throw "Missing shared test block: $name" }
    $match.Groups[1].Value
}
$ui = [IO.File]::ReadAllText((Join-Path $projectRoot 'Assets/Scripts/UI/RunCardSelectionUI.cs'))
$start = $ui.IndexOf('    private void SelectCard(')
$end = $ui.IndexOf('    private void HandleStateChanged(', $start)
$source = (Get-CSharpBlock 'stub') + $ui.Substring($start,$end-$start) + (Get-CSharpBlock 'tests')
$source += @'
namespace UnityEngine {
 public class Sprite {}
 public static class Random {static System.Random random=new System.Random(1);public static float value{get{return (float)random.NextDouble();}}public static int Range(int a,int b){return random.Next(a,b);}}
}
public static class RunCardAssetTests {
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Put(object target,string field,object value){target.GetType().GetField(field,flags).SetValue(target,value);}
 static void Call(object target,string method){target.GetType().GetMethod(method,flags).Invoke(target,null);}
 static string Field(string text,string name){var match=System.Text.RegularExpressions.Regex.Match(text,@"(?m)^  "+name+@":\s*([^\r\n]*)");Check(match.Success,"missing "+name);return match.Groups[1].Value.Trim().Trim('"');}
 internal static RunCardData Load(string path){string text=System.IO.File.ReadAllText(path);var card=new RunCardData();foreach(string field in new[]{"id","baseCardId","displayName","description"})Put(card,field,Field(text,field));Put(card,"rarity",(RunCardRarity)int.Parse(Field(text,"rarity")));Put(card,"effectType",(RunCardEffectType)int.Parse(Field(text,"effectType")));foreach(string field in new[]{"attackBonus","defenseBonus","maxHealthBonus","healAmount"})Put(card,field,int.Parse(Field(text,field)));foreach(string field in new[]{"attackPenalty","defensePenalty","maxHealthPenalty"}){var m=System.Text.RegularExpressions.Regex.Match(text,@"(?m)^  "+field+@": (\d+)");Put(card,field,m.Success?int.Parse(m.Groups[1].Value):0);}return card;}
 static RunProgression Progression(){if(RunProgression.Instance!=null)Call(RunProgression.Instance,"OnDestroy");var xp=new RunProgression();Call(xp,"Awake");xp.AddExperience(100);return xp;}
 static RunCardEffectHandler Handler(PlayerStatus status,RunProgression xp,GameFlowController flow){var handler=new RunCardEffectHandler();Put(handler,"playerStatus",status);Put(handler,"progression",xp);Put(handler,"gameFlow",flow);Call(handler,"Start");return handler;}
 public static string Run(string root){
  PlayerPrefs.Values.Clear();PlayerPrefs.Writes=0;
  var cards=new List<RunCardData>();foreach(string path in System.IO.Directory.GetFiles(System.IO.Path.Combine(root,"Assets/Datas"),"RunCard*.asset")){string text=System.IO.File.ReadAllText(path);if(System.Text.RegularExpressions.Regex.IsMatch(text,@"(?m)^  id:")&&!System.Text.RegularExpressions.Regex.IsMatch(text,@"(?m)^  rarity: 4\s*$"))cards.Add(Load(path));}
  cards.Sort((a,b)=>string.CompareOrdinal(a.Id,b.Id));Check(cards.Count==20,"expected 20 card assets");var ids=new HashSet<string>();var families=new Dictionary<string,HashSet<RunCardRarity>>();var counts=new int[5];
  int[][] attack={new[]{2,4,6,10},new[]{1,2,3,5}};int[] hp={15,25,40,60},heal={20,35,55,80};
  foreach(var card in cards){int rarity=(int)card.Rarity;Check(rarity>=0&&rarity<4,"Nightmare/invalid asset");counts[rarity]++;Check(ids.Add(card.Id)&&card.HasValidEffect,"invalid or duplicate ID");Check(card.Id==(rarity==0?card.BaseCardId:card.BaseCardId+"."+card.Rarity.ToString().ToLowerInvariant()),"ID/family mismatch");if(!families.ContainsKey(card.BaseCardId))families[card.BaseCardId]=new HashSet<RunCardRarity>();Check(families[card.BaseCardId].Add(card.Rarity),"duplicate family rarity");
   int a=0,d=0,h=0,r=0;switch(card.EffectType){case RunCardEffectType.Attack:a=attack[0][rarity];break;case RunCardEffectType.Defense:d=attack[1][rarity];break;case RunCardEffectType.MaxHealth:h=hp[rarity];break;case RunCardEffectType.Heal:r=heal[rarity];break;case RunCardEffectType.AttackAndDefense:a=d=attack[1][rarity];break;default:throw new Exception("unknown effect");}
   Check(card.AttackBonus==a&&card.DefenseBonus==d&&card.MaxHealthBonus==h&&card.HealAmount==r,"incorrect asset values: "+card.Id);
   var status=new PlayerStatus();Call(status,"Awake");status.TakeDamage(30);var xp=Progression();var flow=new GameFlowController{Status=status};var handler=Handler(status,xp,flow);Check(handler.TrySelectCard(card),"effect rejected: "+card.Id);Check(status.Attack==13+a&&status.Defense==5+d&&status.MaxHP==100+h&&status.CurrentHP==Math.Min(100+h,70+h+r),"applied stats: "+card.Id);Check(xp.PendingLevelUps==2,"reward count: "+card.Id);xp.ResetRun();Check(status.RunAttackBonus==0&&status.RunDefenseBonus==0&&status.RunMaxHPBonus==0&&status.MaxHP==100,"reset: "+card.Id);
  }
  for(int i=0;i<4;i++)Check(counts[i]==5,"five cards per rarity");Check(families.Count==5,"five families");foreach(var family in families.Values)Check(family.Count==4,"missing family variant");Check(PlayerPrefs.Writes==0,"run effects wrote PlayerPrefs");
  string config=System.IO.File.ReadAllText(System.IO.Path.Combine(root,"Assets/Datas/RunCardRaritySettings.asset"));var matches=System.Text.RegularExpressions.Regex.Matches(config,@"- rarity: (\d+)\r?\n    enabled: (\d+)\r?\n    weight: (\d+)");Check(matches.Count==5,"config entries");var entries=new RunCardRaritySettings.Entry[5];int[] weights={64,25,8,2,1};for(int i=0;i<5;i++){var m=matches[i];Check(int.Parse(m.Groups[1].Value)==i&&int.Parse(m.Groups[3].Value)==weights[i]&&int.Parse(m.Groups[2].Value)==(i<4?1:0),"production weight config");entries[i]=new RunCardRaritySettings.Entry{rarity=(RunCardRarity)i,enabled=i<4,weight=weights[i],frame=new Sprite()};}var settings=new RunCardRaritySettings();Put(settings,"entries",entries);
  const int samples=100000;var first=new int[5];var all=new int[5];var screens=new int[5];var rng=new System.Random(20261126);for(int n=0;n<samples;n++){RunCardData[] offer;string error;Check(RunCardSampler.TryDrawThree(cards,settings,out offer,out error,rng),error);var unique=new HashSet<string>();var present=new bool[5];for(int i=0;i<3;i++){var card=offer[i];Check(unique.Add(card.BaseCardId),"duplicate family in offer");int rarity=(int)card.Rarity;Check(rarity<4,"Nightmare drawn");all[rarity]++;present[rarity]=true;if(i==0)first[rarity]++;}for(int i=0;i<4;i++)if(present[i])screens[i]++;}
  for(int i=0;i<4;i++){double expected=weights[i]/99.0;Check(Math.Abs(first[i]/(double)samples-expected)<.006,"first-slot distribution");Check(Math.Abs(all[i]/(double)(samples*3)-expected)<.006,"all-slot distribution");Check(Math.Abs(screens[i]/(double)samples-(1-Math.Pow(1-expected,3)))<.007,"screen presence distribution");}
  // Three real asset choices; same family may be selected again at the next reward.
  var player=new PlayerStatus();Call(player,"Awake");var progression=Progression();var gameFlow=new GameFlowController{Status=player};var effectHandler=Handler(player,progression,gameFlow);var selected=new[]{cards.Find(c=>c.Id=="attack_up.rare"),cards.Find(c=>c.Id=="attack_up.epic"),cards.Find(c=>c.Id=="attack_up.legendary")};var ui=new SelectionHarness{progression=progression,gameFlow=gameFlow,selectionHandler=effectHandler,offeredCards=selected};int confirmed=0;ui.OnCardSelected+=c=>confirmed++;foreach(var card in selected){ui.offeredCards=new[]{card};int version=ui.offerVersion;Time.frameCount++;ui.Click(version);ui.Click(version);}Check(confirmed==3&&player.RunAttackBonus==20&&progression.PendingLevelUps==0&&!ui.IsOpen,"consecutive same-family stacking / multiple rewards");player.TakeDamage(10000);Check(!effectHandler.TrySelectCard(selected[0]),"dead player choice");var retry=new PlayerStatus();Call(retry,"Awake");Check(retry.Attack==13&&retry.MaxHP==100&&retry.Defense==5&&retry.RunAttackBonus==0,"retry-style recreation");
  var lines=new List<string>{"PASS: actual 20 assets, IDs/families, approved values, all 20 applied effects/reset, no PlayerPrefs writes, three same-family high-rarity UI choices and duplicate-click guards.","100000 offers / 300000 draws; rarity: first slot / all cards / screens containing rarity"};for(int i=0;i<4;i++)lines.Add(((RunCardRarity)i)+": "+first[i]+" ("+(first[i]/1000.0).ToString("F3")+"%) / "+all[i]+" ("+(all[i]/3000.0).ToString("F3")+"%) / "+screens[i]+" ("+(screens[i]/1000.0).ToString("F3")+"%)");lines.Add("Nightmare: 0. Managed stand-ins only; Unity Editor was not executed.");return string.Join(Environment.NewLine,lines);
 }
}
'@
foreach($file in @('Player/PlayerStatus','Player/RunCardData','Player/RunCardRarity','Player/RunCardRaritySettings','Player/RunCardSampler','Player/IRunCardSelectionHandler','Player/RunCardEffectHandler','Player/RunProgression','UI/PlayerHealthBarUI','System/GameState','System/DamageCalculator')) {
    $source += [regex]::Replace([IO.File]::ReadAllText((Join-Path $projectRoot "Assets/Scripts/$file.cs")), '(?m)^using [^;]+;\r?\n', '')
}
$source += [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'RunNightmare.Tests.cs'))
Add-Type -TypeDefinition $source
[RunCardEffectsTests]::Run()
[RunCardAssetTests]::Run($projectRoot)

[RunNightmareTests]::Run($projectRoot)

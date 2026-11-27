# Production status/HUD, plus extracted Hades status binding and damage gate.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$prior=[IO.File]::ReadAllText((Join-Path $root 'Tests/RunCardEffects.Tests.ps1'))
$stub=[regex]::Match($prior,'(?s)\$stub = @''\r?\n(.*?)\r?\n''@').Groups[1].Value
$stub=$stub.Substring(0,$stub.IndexOf('public class SelectionHarness'))
$boss=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Boss/BossEnemy.cs'),[Text.Encoding]::GetEncoding(949))
$binding=[regex]::Match($boss,'playerStatus =\s*player.Status;').Value
if(!$binding){throw 'Hades must use the scene-owned status instead of GetComponent.'}
$gate=[regex]::Match($boss,'public override bool TakeDamage\(int attack, out int damage\) \{[^}]+\}').Value
$test=@'
public class CanonicalGrid {public PlayerStatus Status; public PlayerStatus OtherComponent;}
public class EnemyBase {public int HP=1000;public virtual bool TakeDamage(int attack,out int damage){damage=attack;HP-=attack;return HP<=0;}}
public class BossProbe:EnemyBase {
 public bool awakened;
 public Action HealthChanged;
 public PlayerStatus playerStatus;
 public void Bind(CanonicalGrid player) { BINDING }
 GATE
}
public static class HadesStatusProbe {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Call(object value,string method){value.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(value,null);}
 public static string Run(){
  var actual=new PlayerStatus();Call(actual,"Awake");var other=new PlayerStatus();Call(other,"Awake");
  var hud=new PlayerHealthBarUI();var fill=new Image();
  typeof(PlayerHealthBarUI).GetField("playerStatus",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hud,actual);
  typeof(PlayerHealthBarUI).GetField("hpFill",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hud,fill);Call(hud,"OnEnable");
  int deaths=0;bool inputEnabled=true;actual.OnPlayerDied+=()=>{deaths++;inputEnabled=false;};
  var boss=new BossProbe();boss.Bind(new CanonicalGrid{Status=actual,OtherComponent=other});
  Check(object.ReferenceEquals(boss.playerStatus,actual),"canonical serialized status, not other component");
  boss.playerStatus.TakeDamage(DamageCalculator.CalculateDamage(25,actual.Defense));
  Check(fill.fillAmount<1&&other.CurrentHP==other.MaxHP,"actual HUD changed; other status untouched");
  boss.playerStatus.TakeDamage(10000);Check(actual.IsDead&&fill.fillAmount==0&&deaths==1&&!inputEnabled,"lethal damage reaches HUD and death listener");
  boss.playerStatus.TakeDamage(10000);Check(deaths==1,"single death notification");
  int damage;boss.TakeDamage(100,out damage);Check(damage==0&&boss.HP==1000,"sleep/wake immunity");
  boss.awakened=true;boss.TakeDamage(100,out damage);Check(damage==100&&boss.HP==900,"awake damage allowed");
  Check(PlayerPrefs.Writes==0,"no saved data changes");
  return "PASS: production HP HUD + PlayerStatus nonlethal/lethal damage, one death notification; extracted canonical Hades binding and sleeping/awake damage gate. Managed stand-ins, not real GameFlow/Unity play.";
 }
}
'@
$test=$test.Replace('BINDING',$binding).Replace('GATE',$gate)
$source=$stub+$test
foreach($file in @('Player/PlayerStatus.cs','Player/RunCardRarity.cs','Player/RunCardData.cs','UI/PlayerHealthBarUI.cs','System/GameState.cs','System/DamageCalculator.cs')){
 $source += [regex]::Replace([IO.File]::ReadAllText((Join-Path $root ('Assets/Scripts/'+$file))),'(?m)^\uFEFF?[ \t]*using [^;]+;\r?\n','')
}
Add-Type -TypeDefinition $source
[HadesStatusProbe]::Run()

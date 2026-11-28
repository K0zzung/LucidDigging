# Actual lethal-damage path and phase interception; no Unity play/rendering.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
function Method([string]$source,[string]$signature) {
 $start=$source.IndexOf($signature); if($start -lt 0){throw "Missing $signature"}
 $open=$source.IndexOf('{',$start);$depth=0
 for($i=$open;$i -lt $source.Length;$i++) {
  if($source[$i] -eq '{'){$depth++};if($source[$i] -eq '}'){$depth--;if($depth -eq 0){return $source.Substring($start,$i-$start+1)}}
 }
 throw 'Unclosed method'
}
$encoding=[Text.Encoding]::GetEncoding(949)
$enemy=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Enemy/EnemyBase.cs'),$encoding)
$boss=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Boss/BossEnemy.cs'),$encoding)
$grid=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Player/PlayerGridController.cs'),$encoding)
$damage=Method $enemy 'public virtual bool TakeDamage('
$single=$damage
# First overload delegates; use the second, actual damage/XP implementation.
$second=$enemy.IndexOf('public virtual bool TakeDamage(',$enemy.IndexOf('public virtual bool TakeDamage(')+1)
$damage=Method $enemy.Substring($second) 'public virtual bool TakeDamage('
$phase=Method $boss 'protected override bool TryPreventDeath()'
$enemyHit=Method $enemy 'public bool TakeDamageFromEnemy('
$power=($boss -split "`n" | Where-Object {$_ -match 'private int PatternAttack\('}) -join "`n"
$wait=Method $grid 'private IEnumerator WaitForHadesPresentation('
$complete=Method $grid 'private void CompleteBossDefeat('
$spike=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Boss/BossSpikeStrike.cs'))
$timing=($spike -split "`n" | Where-Object {$_ -match 'public float (HitDelay|Lifetime) =>'}) -join "`n"
$stub=@'
using System;
using System.Collections;
using UnityEngine;
namespace UnityEngine {
 public class Sprite {}
 public class SpriteRenderer {public Sprite sprite;}
 public class GameObject {public bool activeSelf=true;public void SetActive(bool value){activeSelf=value;}}
 public static class Debug {public static void Log(object s){}}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int RoundToInt(float x)=>(int)Math.Round(x);}
}
public class EnemyData {public string enemyName="Hades";public int defense=25,experienceReward=90;}
public class RunProgression {public static RunProgression Instance=new RunProgression();public int XP;public void AddExperience(int n){XP+=n;}}
public class HadesWaveProjectile {public GameObject gameObject=new GameObject();}
public class EnemyBase {
 public int CurrentHP=1200,MaxHP=1200,Deaths,HealthRefreshes; public bool IsDead;private bool experienceGranted;
 protected EnemyData data=new EnemyData();private object healthBarInstance=new object();
 private int enemyDamageDepth;
 ENEMYHIT
 protected virtual bool TryPreventDeath()=>false;
 protected virtual void Die(){IsDead=true;Deaths++;}
 void HideHealthBar(){} void RefreshHealthBar(){HealthRefreshes++;}void PlayHitAnimation(){}
 SINGLE
 DAMAGE
}
public class WarningSpy {public int Calls;public string Text;public float ShowBossMessage(string text,object font){Calls++;Text=text;return 2;}}
public class BossEnemy:EnemyBase {
 public WarningSpy awakeningWarning=new WarningSpy();public object awakeningMessageFont;
 public bool awakened=true,IsDeathPresentationPlaying;
 public int Phase{get;private set;}=1;public int phaseTwoMaxHP;
 public float phaseTwoPower=1.5f;
 public Sprite phaseTwoHead=new Sprite(),phaseTwoJaw=new Sprite();
 public SpriteRenderer headRenderer=new SpriteRenderer(),jawRenderer=new SpriteRenderer();
 public HadesWaveProjectile[] missiles={new HadesWaveProjectile()};
 public object attackRoutine=new object(); public int CancelCalls; bool CanAttack=>false;
 void StopTell(){CancelCalls++;}void CleanupPattern(){CancelCalls++;}void HideMouthOrb(){CancelCalls++;}void StopCoroutine(object o){CancelCalls++;}object StartCoroutine(IEnumerator e)=>e;IEnumerator AttackLoop(){yield break;}
 PHASE
 POWER
 public int Attack(int n)=>PatternAttack(n);
}
public class PlayerStatus {public bool IsDead;}
public class LandingProbe {
 public PlayerStatus playerStatus=new PlayerStatus();public bool isActiveAndEnabled=true,IsWaitingForBossPresentation=true,Locked=true;public int Falls;
 void EndBossPresentationLock(){Locked=false;}
 public bool isBusy=true;
 object StartCoroutine(IEnumerator e)=>e;
 COMPLETE
 public IEnumerator Wait(BossEnemy boss,Action complete)=>WaitForHadesPresentation(boss,complete);
 WAIT
}
public class SpikeTimingProbe {public float phaseSpeed=1;float hitDelay=.15f,playbackSpeed=1.3f,peakHold=.06f;const float ClipLength=.6f; TIMING }
public static class PhaseTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 public static string Run(){
  var spike=new SpikeTimingProbe();float lifetime=spike.Lifetime;spike.phaseSpeed=1.5f;Check(Math.Abs(spike.HitDelay-.1f)<.00001&&Math.Abs(spike.Lifetime-lifetime/1.5f)<.00001,"phase two 1.5 animation and hit-delay speed");
  var b=new BossEnemy();object running=b.attackRoutine;int damage;
  Check(b.Attack(100)==100,"phase one power");
  Check(!b.TakeDamage(5000,out damage)&&b.Phase==2&&!b.IsDead&&b.CurrentHP==1200&&b.Deaths==0&&RunProgression.Instance.XP==0,"first lethal refills without death/drop/XP path");
  Check(b.headRenderer.sprite==b.phaseTwoHead&&b.jawRenderer.sprite==b.phaseTwoJaw,"phase sprites");
  Check(b.awakeningWarning.Calls==1&&b.awakeningWarning.Text=="\ub0b4 \uc7a0\uc744 \ubc29\ud574\ud558\ub2e4\ub2c8, \ud6c4\ud68c\ud558\uac8c \ud574\uc8fc\ub9c8..","phase two announcement once");
  Check(b.missiles[0].gameObject.activeSelf&&b.CancelCalls==0&&object.ReferenceEquals(running,b.attackRoutine),"phase refill preserves projectiles, warning/tell and active coroutine");
  Check(b.Attack(100)==150&&DamageCalculator.CalculateDamage(b.Attack(100),25)==120,"1.5 power before defense");
  Check(b.TakeDamage(5000,out damage)&&b.IsDead&&b.Deaths==1&&RunProgression.Instance.XP==90,"second lethal claims one final death and XP");
  Check(!b.TakeDamage(5000,out damage)&&b.Deaths==1&&RunProgression.Instance.XP==90&&b.awakeningWarning.Calls==1,"duplicate final damage ignored and announcement not repeated");
  int savedXP=RunProgression.Instance.XP;var npcVictim=new EnemyBase();
  Check(npcVictim.TakeDamageFromEnemy(5000)&&npcVictim.IsDead&&RunProgression.Instance.XP==savedXP,"NPC kill grants no player XP");
  npcVictim.TakeDamage(5000,out damage);Check(RunProgression.Instance.XP==savedXP,"player cannot claim XP after NPC kill");
  var playerVictim=new EnemyBase();playerVictim.TakeDamageFromEnemy(100);
  Check(playerVictim.TakeDamage(5000,out damage)&&RunProgression.Instance.XP==savedXP+90,"player last hit after nonlethal NPC hit still grants XP");
  var custom=new BossEnemy{phaseTwoMaxHP=2000};custom.TakeDamage(5000,out damage);Check(custom.CurrentHP==2000&&custom.MaxHP==2000,"editable phase two HP");
  var p=new LandingProbe();b.IsDeathPresentationPlaying=true;int completed=0;var wait=p.Wait(b,()=>completed++);
  for(int i=0;i<4;i++)Check(wait.MoveNext()&&p.Falls==0&&p.Locked,"no landing during stone stages");
  b.IsDeathPresentationPlaying=false;Check(!wait.MoveNext()&&p.Falls==0&&completed==1&&!p.Locked&&!p.IsWaitingForBossPresentation,"completion resumed without forced movement");
  p=new LandingProbe();b.IsDeathPresentationPlaying=false;wait=p.Wait(b,null);Check(!wait.MoveNext()&&!p.isBusy&&!p.Locked,"Dash/Shockwave completion releases busy without movement");
  p=new LandingProbe();b.IsDeathPresentationPlaying=true;wait=p.Wait(b,()=>{});wait.MoveNext();p.playerStatus.IsDead=true;Check(!wait.MoveNext()&&p.Falls==0&&!p.Locked,"player death cancels deferred landing");
  return "PASS: actual EnemyBase damage/XP + Hades phase interception: phase refill, sprites, power, final death once; actual boss completion waits without forcing descent and cancels on player death. Managed stand-ins; Unity not executed.";
 }
}
'@
$calculator=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/System/DamageCalculator.cs')) -replace '(?m)^using [^;]+;\r?\n',''
Add-Type -TypeDefinition ($stub.Replace('DAMAGE',$damage).Replace('SINGLE',$single).Replace('ENEMYHIT',$enemyHit).Replace('PHASE',$phase).Replace('POWER',$power).Replace('WAIT',$wait).Replace('TIMING',$timing).Replace('COMPLETE',$complete)+$calculator)
[PhaseTests]::Run()

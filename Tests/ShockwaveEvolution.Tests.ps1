# Extracts the production attack loop and DamageCalculator. Managed stand-ins, not Unity.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stub = @'
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {
 public struct Vector3Int {public int x,y,z;public Vector3Int(int a,int b,int c){x=a;y=b;z=c;}public static Vector3Int right=>new Vector3Int(1,0,0);public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);}
 public struct Vector3 {public float x,y,z;public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3{x=a.x-b.x,y=a.y-b.y,z=a.z-b.z};}
 public class Transform {public Vector3 position;}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static int Abs(int v)=>Math.Abs(v);public static int RoundToInt(float v)=>(int)Math.Round(v);}
}
public class Tilemap {public HashSet<Vector3Int> Cells=new HashSet<Vector3Int>();public bool HasTile(Vector3Int c)=>Cells.Contains(c);public Vector3 GetCellCenterWorld(Vector3Int c)=>new Vector3{x=c.x,y=c.y,z=c.z};}
public class PlayerStatus {public int Attack=10;}
public class EnemyBase {public bool Boss,Killed;public int Calls,LastAttack,Damage;public Action OnHit;public bool TakeDamage(int attack,out int damage){Calls++;LastAttack=attack;damage=DamageCalculator.CalculateDamage(attack,30);Damage+=damage;OnHit?.Invoke();return Killed;}public void ShowHealthBar(){}}
public class EnemyManager {public Dictionary<Vector3Int,EnemyBase> Cells=new Dictionary<Vector3Int,EnemyBase>();public bool TryGetEnemy(Vector3Int c,out EnemyBase e)=>Cells.TryGetValue(c,out e);}
public class TerrainManager {public Dictionary<Vector3Int,int> Attacks=new Dictionary<Vector3Int,int>();public bool DamageTerrain(Vector3Int c,int attack,out int damage){Attacks[c]=attack;damage=DamageCalculator.CalculateDamage(attack,5);return true;}public void ShowHealthBar(Vector3Int c){}}
public class ShockwaveEffect {public void SetGrid(Vector3 c,int s,Vector3 x,Vector3 y){}public void AddImpact(Vector3 c,int d,bool strong){}}
public class CameraShakeController {public static CameraShakeController Instance;public void ShakeBossKill(){}}
public class AudioManager {public static AudioManager Instance;public void PlayEnemyKill(){}}
public class AttackHarness {
 public Vector3Int currentCell;
 public Transform transform=new Transform();public PlayerStatus playerStatus=new PlayerStatus();
 public Tilemap wallTilemap=new Tilemap(),terrainTilemap=new Tilemap();
 public EnemyManager enemyManager=new EnemyManager();public TerrainManager terrainManager=new TerrainManager();
 public int Landings;
 private bool IsBoss(EnemyBase e)=>e.Boss;
 private void ShowDealtDamage(Vector3Int c,int damage){}
 private void MoveDownAfterBossDeath(){Landings++;currentCell=new Vector3Int(100,100,0);}
'@
$source = [IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Player/PlayerGridController.cs'))
$start = $source.IndexOf('    private readonly EnemyBase[] shockwaveCells')
$end = $source.IndexOf('    private void CaptureSkillInput(', $start)
$stub += $source.Substring($start, $end - $start) + "}`n"
$stub += [regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/System/DamageCalculator.cs')), '(?m)^using [^;]+;\r?\n', '')
$stub += @'
public static class ShockwaveTests {
 static Vector3Int Cell(int x,int y)=>new Vector3Int(x,y,0);
 static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);}
 public static string Run(){
  foreach(int size in new[]{3,5,7}) {
   var h=new AttackHarness();for(int x=-4;x<=4;x++)for(int y=-4;y<=4;y++)h.terrainTilemap.Cells.Add(Cell(x,y));
   h.wallTilemap.Cells.Add(Cell(0,0));h.PerformShockwave(size,150f,null,size==7);
   Check(h.terrainManager.Attacks.Count==size*size-1,"range/wall cell count");
   foreach(var item in h.terrainManager.Attacks){int d=Math.Max(Math.Abs(item.Key.x),Math.Abs(item.Key.y));Check(d<=size/2,"outside hit");Check(item.Value==(size==7&&d==3?8:15),"inner/outer attack");}
  }
  var b=new AttackHarness();var boss=new EnemyBase{Boss=true,Killed=true};
  b.enemyManager.Cells[Cell(-3,-3)]=boss;b.enemyManager.Cells[Cell(-2,-2)]=boss;b.enemyManager.Cells[Cell(0,0)]=boss;
  var outer=new EnemyBase();b.enemyManager.Cells[Cell(3,3)]=outer;
  var outside=new EnemyBase();b.enemyManager.Cells[Cell(4,0)]=outside;
  // Simulate a death observer changing player stats and boss landing changing currentCell.
  boss.OnHit=()=>b.playerStatus.Attack=999;
  b.terrainTilemap.Cells.Add(Cell(2,2));b.PerformShockwave(7,150f,null,true);
  Check(boss.Calls==1&&boss.LastAttack==15&&boss.Damage==12,"spanning boss full damage once");
  Check(outer.Calls==1&&outer.LastAttack==8&&outer.Damage==6,"outer-only target half before defense");
  Check(outside.Calls==0&&b.Landings==1,"outside/landing");
  Check(b.terrainManager.Attacks[Cell(2,2)]==15,"cast origin/attack snapshot");
  // Reuse scratch containers over many casts, including dead targets and overlapping cells.
  boss.Killed=false;boss.OnHit=null;b.currentCell=Cell(0,0);b.playerStatus.Attack=10;
  int before=boss.Calls;for(int i=0;i<1000;i++)b.PerformShockwave(7,150f,null,true);
  Check(boss.Calls-before==1000,"scratch reuse duplicate hits");
  Check(AttackHarness.GetShockwaveCellAttack(3,150f,3,0,true)==2,"round once after multiplier");
  return "PASS: production Shockwave loop + DamageCalculator: 3/5/7 square ranges, walls/outside exclusion, terrain, outer 50% before defense, spanning boss max weight/one hit, cast snapshots across death/landing, and 1000 repeated casts. Managed stand-ins; Unity not executed or GC-profiled.";
 }
}
'@
Add-Type $stub
[ShockwaveTests]::Run()

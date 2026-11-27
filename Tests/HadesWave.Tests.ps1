# Actual projectile Update/Launch with managed Unity stand-ins. No Unity rendering.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$stub=@'
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace UnityEngine {
 public class MonoBehaviour { public GameObject gameObject=new GameObject(); public Transform transform=new Transform(); public T GetComponent<T>() where T:new()=>new T(); }
 public class GameObject { public bool activeSelf=true; public void SetActive(bool value){activeSelf=value;} }
 public class Transform { public Vector3 position,localScale,lossyScale=Vector3.one; public Quaternion rotation; public Transform parent; }
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 down=>new Vector3(0,-1,0);public float sqrMagnitude=>x*x+y*y+z*z;public Vector3 normalized=>this/(float)Math.Sqrt(sqrMagnitude);public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator/(Vector3 a,float b)=>a*(1f/b);}
 public struct Vector3Int {public int x,y,z; public Vector3Int(int x,int y,int z=0){this.x=x;this.y=y;this.z=z;}public static bool operator==(Vector3Int a,Vector3Int b)=>a.x==b.x&&a.y==b.y;public static bool operator!=(Vector3Int a,Vector3Int b)=>!(a==b);public override bool Equals(object o)=>o is Vector3Int v&&this==v;public override int GetHashCode()=>x^y;}
 public struct Quaternion {public static Quaternion Euler(float x,float y,float z)=>new Quaternion();}
 public struct Color { public float a;public static Color white=>new Color{a=1};}
 public struct Bounds {public Vector3 size;}
 public class Sprite {public Bounds bounds=new Bounds{size=Vector3.one};}
 public class Animator {public void Rebind(){} public void Update(float dt){}}
 public class SpriteRenderer {public Sprite sprite; public Color color;}
 public static class Time {public static float deltaTime;}
 public static class Mathf {public static int RoundToInt(float a)=>(int)Math.Round(a);public const float Rad2Deg=57.29578f;public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int CeilToInt(float a)=>(int)Math.Ceiling(a);public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));}
}
namespace UnityEngine.Tilemaps {public class Tilemap {public int WallX=999; public UnityEngine.Vector3Int WorldToCell(UnityEngine.Vector3 v)=>new UnityEngine.Vector3Int((int)Math.Floor(v.x),(int)Math.Floor(v.y));public bool HasTile(UnityEngine.Vector3Int v)=>v.x==WallX;}}
public class PlayerGridController {public UnityEngine.Vector3Int CurrentCell;}
public class PlayerStatus {public bool IsDead;public int Defense=5,HP=100,HudHP=100,DeathEvents;public void TakeDamage(int n){HP-=n;HudHP=HP;if(HP<=0){IsDead=true;DeathEvents++;}}}
public class EnemyBase {public bool IsDead;public int Calls,LastAttack,HP=1000;public bool TakeDamage(int attack){Calls++;LastAttack=attack;HP-=DamageCalculator.CalculateDamage(attack,10);return HP<=0;}}
public class EnemyManager {public System.Collections.Generic.Dictionary<UnityEngine.Vector3Int,EnemyBase> Cells=new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int,EnemyBase>();public bool TryGetEnemy(UnityEngine.Vector3Int c,out EnemyBase e)=>Cells.TryGetValue(c,out e);}
public static class Probe {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Tick(HadesWaveProjectile p,float dt){UnityEngine.Time.deltaTime=dt;typeof(HadesWaveProjectile).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);}
 static HadesWaveProjectile New()=>new HadesWaveProjectile();
 public static string Run(){
  var p=New();p.Prepare();p.transform.localScale=new UnityEngine.Vector3(2.5f,2.5f,1);var player=new PlayerGridController{CurrentCell=new UnityEngine.Vector3Int(99,99)};var status=new PlayerStatus();var map=new UnityEngine.Tilemaps.Tilemap();
  p.Launch(new UnityEngine.Vector3(.5f,.5f,0),new UnityEngine.Vector3(1,0,0),1,7,12,100,player,status,map);
  Check(p.transform.localScale.x==2.5f&&p.transform.localScale.y==2.5f,"authored prefab size preserved");Tick(p,0);Check(p.transform.position.x==.5f,"pause");
  player.CurrentCell=new UnityEngine.Vector3Int(0,5);Tick(p,.1f);Check(p.transform.position.x>1&&p.transform.position.y==.5f,"locked aim, no homing");
  map.WallX=2;Tick(p,1);Check(p.transform.position.x<3,"swept wall at low FPS");Tick(p,.2f);Check(!p.gameObject.activeSelf,"wall fade returns to pool");
  map.WallX=999;p.Launch(new UnityEngine.Vector3(.5f,.5f,0),new UnityEngine.Vector3(1,0,0),1,7,1,100,player,status,map);Tick(p,1);Check(Math.Abs(p.transform.position.x-1.5f)<.001,"max range");Tick(p,.2f);Check(!p.gameObject.activeSelf,"range fade");
  player.CurrentCell=new UnityEngine.Vector3Int(2,0);p.Launch(new UnityEngine.Vector3(.5f,.5f,0),new UnityEngine.Vector3(1,0,0),1,7,12,100,player,status,map);Tick(p,1);int hp=status.HP;Check(hp<100&&status.HudHP==hp,"damage reaches passed HUD status");Tick(p,.05f);Check(status.HP==hp,"one hit only");
  status.IsDead=true;Tick(p,.01f);Check(!p.gameObject.activeSelf,"death cleanup");
  for(int i=0;i<1000;i++){status.IsDead=false;player.CurrentCell=new UnityEngine.Vector3Int(99,99);p.Launch(new UnityEngine.Vector3(.5f,.5f,0),new UnityEngine.Vector3(1,0,0),1,7,1,100,player,status,map);Tick(p,1);Tick(p,.2f);Check(!p.gameObject.activeSelf,"pool reuse");}
  var manager=new EnemyManager();var owner=new EnemyBase();var enemy=new EnemyBase();
  manager.Cells[new UnityEngine.Vector3Int(0,0)]=owner;manager.Cells[new UnityEngine.Vector3Int(2,0)]=enemy;manager.Cells[new UnityEngine.Vector3Int(3,0)]=enemy;
  p.Launch(new UnityEngine.Vector3(.5f,.5f,0),new UnityEngine.Vector3(1,0,0),1,7,12,100,player,status,map,manager,owner);
  Tick(p,1);Tick(p,.05f);Check(owner.Calls==0&&enemy.Calls==1&&enemy.LastAttack==100&&enemy.HP==909,"enemy friendly fire: owner excluded, multi-cell target hit once, defense applied");
  map.WallX=1;int before=enemy.Calls;p.Launch(new UnityEngine.Vector3(.5f,.5f,0),new UnityEngine.Vector3(1,0,0),1,7,12,100,player,status,map,manager,owner);Tick(p,1);Check(enemy.Calls==before,"wall prevents enemy hit");
  return "PASS: actual projectile: frozen time, fixed aim, swept wall, max distance, fade/reuse, one damage, owner exclusion/enemy friendly fire, death cleanup, 1000 reuse cycles. Managed stand-ins; Unity not executed.";
 }
}
'@
$damage=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/System/DamageCalculator.cs'))
$projectile=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Boss/HadesWaveProjectile.cs'))
# Keep using directives at the beginning of the combined compilation unit.
$projectile=$projectile -replace '(?m)^using [^;]+;\r?\n',''
$damage=$damage -replace '(?m)^using [^;]+;\r?\n',''
Add-Type -TypeDefinition ($stub+"`n"+$damage+"`n"+$projectile)
[Probe]::Run()

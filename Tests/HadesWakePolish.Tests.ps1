# Production background ownership and base-padding spawn methods. No Unity rendering.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
function Method([string]$s,[string]$signature) {
 $start=$s.IndexOf($signature);if($start -lt 0){throw $signature};$open=$s.IndexOf('{',$start);$depth=0
 for($i=$open;$i -lt $s.Length;$i++){if($s[$i] -eq '{'){$depth++};if($s[$i] -eq '}'){$depth--;if($depth -eq 0){return $s.Substring($start,$i-$start+1)}}}
 throw 'Unclosed method'
}
$enc=[Text.Encoding]::GetEncoding(949)
$background=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/System/DepthBackgroundController.cs'),$enc)
$generator=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Dungeon/DungeonGenerator.cs'),$enc)
$boss=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Boss/BossEnemy.cs'),$enc)
$methods=''
foreach($sig in @('public void BeginHadesBackground(','private IEnumerator HadesBackgroundRoutine(','public void EndHadesBackground()','private void LateUpdate()')){$methods+=Method $background $sig}
$padding=Method $generator 'private void SpawnHadesBasePadding()'
$enemy=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Enemy/EnemyBase.cs'),$enc)
$placement=Method $enemy 'public bool TrySetPlacementFootprint('
$footprint=[regex]::Match($enemy,'public Vector2Int FootprintSize =>[\s\S]*?;').Value
$stub=@'
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {
 public enum CameraClearFlags {Skybox,SolidColor}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public class Camera {public static Camera main;public Color backgroundColor;public CameraClearFlags clearFlags;}
 public class Coroutine {public IEnumerator Routine;public bool Stopped;}
 public struct Vector2Int {public int x,y;public Vector2Int(int x,int y){this.x=x;this.y=y;}public static Vector2Int one=>new Vector2Int(1,1);public static bool operator!=(Vector2Int a,Vector2Int b)=>a.x!=b.x||a.y!=b.y;public static bool operator==(Vector2Int a,Vector2Int b)=>!(a!=b);public override bool Equals(object o)=>o is Vector2Int v&&this==v;public override int GetHashCode()=>x^y;}
 public struct Vector3Int {public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);}
 public static class Mathf {public static int Clamp(int n,int a,int b)=>Math.Max(a,Math.Min(b,n));public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class Debug {public static void LogWarning(object s){}}
}
public class BackgroundProbe {
 public Camera mainCamera=new Camera();public bool backgroundOverride,hadesBackgroundActive;public Color savedHadesBackground;public CameraClearFlags savedHadesClearFlags;public Coroutine hadesBackgroundRoutine;public int DepthWrites;
 public List<Color> Colors=new List<Color>();
 Coroutine StartCoroutine(IEnumerator e)=>new Coroutine{Routine=e};void StopCoroutine(Coroutine c){c.Stopped=true;}
 IEnumerator Empty(){yield break;}
 IEnumerator LerpBackgroundColor(Color from,Color to,float duration){mainCamera.backgroundColor=to;Colors.Add(to);return Empty();}
 void UpdateBackgroundColor(){DepthWrites++;}
 public void Tick()=>LateUpdate();
 BACKGROUND
}
public class Transform {public Transform parent;}
public class EnemyBase {public Transform transform=new Transform();public object gameObject=new object();public Vector2Int FootprintSize=Vector2Int.one;public bool TrySetPlacementFootprint(Vector2Int s){FootprintSize=s;return true;}}
public class EnemyManager {public HashSet<Vector3Int> Cells=new HashSet<Vector3Int>();public int Spawns;public bool HasEnemy(Vector3Int c)=>Cells.Contains(c);public bool RegisterEnemy(EnemyBase p,Vector3Int c,int depth){if(Cells.Add(c)){Spawns++;return true;}return false;}}
public class WallMap {public int X=-100;public bool HasTile(Vector3Int c)=>c.x==X;}
public class EnemyAsset {public Vector2Int footprintSize=new Vector2Int(2,2);}
public class PlacementProbe {public object enemyManager;public EnemyAsset data=new EnemyAsset();private Vector2Int placementFootprint; FOOTPRINT PLACEMENT }
public class PaddingProbe {
 public bool fillHadesBaseWithEnemies=true;public EnemyManager enemyManager=new EnemyManager();public EnemyBase hadesPaddingEnemyPrefab;
 public EnemyBase enemy1Prefab=new EnemyBase(),firstBossPrefab=new EnemyBase{FootprintSize=new Vector2Int(7,7)};
 public Vector3Int firstBossAnchorCell=new Vector3Int(0,100,0);public int firstBossDepth=333;public WallMap wallTilemap=new WallMap();
 public EnemyBase firstBossInstance=new EnemyBase();
 EnemyBase Instantiate(EnemyBase prefab,Transform parent)=>new EnemyBase();void Destroy(object o){}
 public void Spawn()=>SpawnHadesBasePadding();
 PADDING
}
public static class PolishTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static bool Equal(Color a,Color b)=>a.r==b.r&&a.g==b.g&&a.b==b.b&&a.a==b.a;
 public static string Run(){
  var p=new BackgroundProbe();Color original=new Color(.4f,.5f,.6f);Color light=new Color(.92f,.94f,.98f);Color dark=new Color(.18f,.2f,.26f);
  p.mainCamera.backgroundColor=original;p.mainCamera.clearFlags=CameraClearFlags.Skybox;p.BeginHadesBackground(light,dark,2,.18f);
  Check(p.hadesBackgroundActive&&p.mainCamera.clearFlags==CameraClearFlags.SolidColor,"camera snapshot and solid-color ownership");p.Tick();Check(p.DepthWrites==0,"depth tracking cannot overwrite Hades color");
  var routine=p.hadesBackgroundRoutine.Routine;while(routine.MoveNext()){}
  Check(p.Colors.Count==4&&Equal(p.Colors[0],light)&&Equal(p.Colors[1],dark)&&Equal(p.Colors[2],light)&&Equal(p.Colors[3],dark)&&Equal(p.mainCamera.backgroundColor,dark),"two light/dark cycles settle on configured dark");
  p.EndHadesBackground();Check(!p.hadesBackgroundActive&&Equal(p.mainCamera.backgroundColor,original)&&p.mainCamera.clearFlags==CameraClearFlags.Skybox,"original camera settings restored");p.Tick();Check(p.DepthWrites==1,"normal depth tracking resumes");
  p.backgroundOverride=true;p.BeginHadesBackground(light,dark,2,.18f);Check(!p.hadesBackgroundActive,"existing death/gameover override has priority");
  var placement=new PlacementProbe();Check(placement.FootprintSize==new Vector2Int(2,2)&&placement.TrySetPlacementFootprint(Vector2Int.one)&&placement.FootprintSize==Vector2Int.one&&placement.data.footprintSize==new Vector2Int(2,2),"instance padding footprint does not mutate EnemyData");placement.enemyManager=new object();Check(!placement.TrySetPlacementFootprint(new Vector2Int(2,2)),"registered footprint cannot be changed");
  var pad=new PaddingProbe();pad.Spawn();Check(pad.enemyManager.Spawns==7,"seven existing 1x1 enemies fill base row");for(int x=0;x<7;x++)Check(pad.enemyManager.Cells.Contains(new Vector3Int(x,99,0)),"padding below Hades, not inside occupied cells");pad.Spawn();Check(pad.enemyManager.Spawns==7,"no duplicate padding");
  pad=new PaddingProbe();pad.wallTilemap.X=3;pad.Spawn();Check(pad.enemyManager.Spawns==6,"wall excluded");pad=new PaddingProbe();pad.fillHadesBaseWithEnemies=false;pad.Spawn();Check(pad.enemyManager.Spawns==0,"padding can be disabled");
  return "PASS: production background light/dark sequence, camera save/restore, override priority; seven-cell existing-enemy padding, wall/duplicate guards. Managed stand-ins; Unity not executed.";
 }
}
'@
Add-Type -TypeDefinition $stub.Replace('BACKGROUND',$methods).Replace('PADDING',$padding).Replace('FOOTPRINT',$footprint).Replace('PLACEMENT',$placement)
[PolishTests]::Run()
$sleep=Method $boss 'private void SetupSleepingHead()'
if($sleep.Contains('jawRenderer.enabled = false')){throw 'Sleeping jaw must remain visible'}
if(!$sleep.Contains('jawRenderer.enabled = jawWasEnabled')){throw 'Missing sleeping jaw visibility'}
'PASS: sleeping setup retains jaw renderer; visual jaw assembly still requires Unity inspection.'

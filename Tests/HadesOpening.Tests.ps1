# Actual opening scheduler, managed stand-ins. No Unity animation/input test.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$source=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Boss/BossEnemy.cs'),[Text.Encoding]::GetEncoding(949))
$start=$source.IndexOf('    private IEnumerator OpeningHorizontalSweep()')
$end=$source.IndexOf('    private IEnumerator RunOpeningRow(', $start)
if($start -lt 0 -or $end -le $start){throw 'Missing opening scheduler'}
$opening=$source.Substring($start,$end-$start)
$stub=@'
using System;
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine {
 public class Coroutine {}
 public class GameObject {public GameObject(string name){}}
 public struct Vector3Int {public int y;}
 public struct Vector2Int {public int y;}
 public static class Mathf {public static int Clamp(int v,int min,int max)=>Math.Max(min,Math.Min(max,v));public static float Max(float a,float b)=>Math.Max(a,b);}
}
public class OpeningProbe {
 public bool CanAttack=true;
 public object spikeWarningPrefab=new object(),spikeStrikePrefab=new object();
 public UnityEngine.GameObject patternRoot;
 public List<int> patternHitEnemies=new List<int>(),damagedSpikeCells=new List<int>();public bool playerHitThisWave;
 public int openingSweepRows=8;public float openingSweepRowInterval=.2f;
 public UnityEngine.Coroutine[] openingRowRoutines=new UnityEngine.Coroutine[12];public bool[] openingRowsFinished=new bool[12];
 public UnityEngine.Vector3Int Cell=new UnityEngine.Vector3Int{y=100};public UnityEngine.Vector2Int FootprintSize=new UnityEngine.Vector2Int{y=7};
 public List<int> Rows=new List<int>();public List<float> Delays=new List<float>();public int Stops,Cleanups;
 void StopOpeningSweep(){Stops++;for(int i=0;i<12;i++)openingRowsFinished[i]=true;}
 void CleanupPattern(){Cleanups++;patternRoot=null;}
 UnityEngine.Coroutine StartCoroutine(IEnumerator routine)=>new UnityEngine.Coroutine();
 IEnumerator RunOpeningRow(int row,float delay,int index){Rows.Add(row);Delays.Add(delay);return Empty();}
 IEnumerator Empty(){yield break;}
 public IEnumerator Begin()=>OpeningHorizontalSweep();
 OPENING
}
public static class OpeningTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 public static string Run(){
  var p=new OpeningProbe();var routine=p.Begin();Check(routine.MoveNext(),"opening waits for active rows");
  Check(p.Rows.Count==8&&p.patternRoot!=null,"eight rows scheduled");
  for(int i=0;i<8;i++)Check(p.Rows[i]==107+i&&Math.Abs(p.Delays[i]-.2f*i)<.0001,"bottom to top one-cell increments at .2 seconds");
  for(int i=0;i<7;i++)p.openingRowsFinished[i]=true;
  Check(routine.MoveNext()&&p.Cleanups==1,"do not truncate last row retraction");
  p.openingRowsFinished[7]=true;Check(!routine.MoveNext()&&p.patternRoot==null&&p.Cleanups==2,"all rows finish before next pattern");
  p=new OpeningProbe();routine=p.Begin();routine.MoveNext();p.CanAttack=false;Check(!routine.MoveNext()&&p.patternRoot==null,"death cancellation cleans opening");
  return "PASS: production opening scheduler: 8 ascending rows, .2s stagger, waits for final row completion, cancellation cleanup. Managed stand-ins; Unity not executed.";
 }
}
'@
$stub="using UnityEngine;`n"+$stub
Add-Type -TypeDefinition $stub.Replace('OPENING',$opening)
[OpeningTests]::Run()
$wakeStart=$source.IndexOf('    private IEnumerator WakeAndAttack()')
$wakeEnd=$source.IndexOf('    private void ConfigureBossSorting()', $wakeStart)
$wake=$source.Substring($wakeStart,$wakeEnd-$wakeStart)
if($wake.IndexOf('awakened = true;') -ge $wake.IndexOf('yield return OpeningHorizontalSweep();') -or
 $wake.IndexOf('yield return OpeningHorizontalSweep();') -ge $wake.IndexOf('yield return AttackLoop();')){throw 'Awaken -> opening -> regular loop order changed'}
'PASS: awakening completes before one opening sweep; regular attack loop follows.'

$rowStart=$source.IndexOf('    private IEnumerator OpeningHorizontalRow(')
$rowEnd=$source.IndexOf('    private bool HorizontalBaseValid(', $rowStart)
if($rowEnd -lt 0){$rowEnd=$source.IndexOf('    private IEnumerator HorizontalSpikeWave()', $rowStart)}
$row=$source.Substring($rowStart,$rowEnd-$rowStart)
if($row.Contains('ReserveEscape') -or $row.Contains('HasEscape')){throw 'Intro must not skip sides based on player position'}
if(!$row.Contains('HorizontalBaseValid')){throw 'Intro must still exclude walls and Hades cells'}
'PASS: intro attacks do not omit sides based on player position; wall/owner exclusion retained.'

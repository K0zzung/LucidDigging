# Actual manager + actual skill controller; managed scheduler, not Unity input/playback.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$prior = [IO.File]::ReadAllText((Join-Path $root 'Tests/ShieldPresentation.Tests.ps1'))
$stub = [regex]::Match($prior, '(?s)\$stub = @''\r?\n(.*?)\r?\n''@').Groups[1].Value
$stub = $stub.Substring(0, $stub.IndexOf('public static class ShieldTests'))
$stub = [regex]::Replace($stub, 'public class RunSkillManager \{[^\r\n]+', '')
$stub = $stub.Replace('public Transform transform=new Transform();', 'public Transform transform=new Transform(); public object gameObject=new object(); public T FindObjectOfType<T>(){return (T)Components[typeof(T)];}')

$stub = $stub.Replace('public void Die(){OnPlayerDied?.Invoke();}', 'public void Die(){IsDead=true;OnPlayerDied?.Invoke();}')
$stub = $stub.Replace('public static void Log(object o){}', 'public static void Log(object o){} public static void LogException(Exception e,object c){}')
$stub = [regex]::Replace($stub, 'public struct Vector2Int \{[^\r\n]+', 'public struct Vector2Int {public int x,y;public static Vector2Int zero;public static bool operator==(Vector2Int a,Vector2Int b)=>a.x==b.x&&a.y==b.y;public static bool operator!=(Vector2Int a,Vector2Int b)=>!(a==b);public override bool Equals(object o)=>false;public override int GetHashCode()=>0;}')
$stub = $stub.Replace('public class PlayerGridController {', 'public class PlayerGridController {public bool AllowDash=true;').Replace('Action f)=>true;', 'Action f)=>AllowDash;')
$stub = $stub.Replace('public bool AllowDash=true;', 'public bool AllowDash=true;public float LastDuration;').Replace('Action f)=>AllowDash;', 'Action f){LastDuration=a;return AllowDash;}')
$gridSource = [IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Player/PlayerGridController.cs'))
$start = $gridSource.IndexOf('    private void UpdateDashSteering(')
$end = $gridSource.IndexOf('    private IEnumerator DashRoutine(', $start)
$stub += 'public class SteeringHarness { private Vector2Int dashDirection,dashCandidate;private float dashDirectionDeadline,directionCombinationBuffer=.04f;private bool dashDirectionPending,directionPending,pendingFromDiagonal;private Vector2Int activeHeldDirection;public Vector2Int Direction=>dashDirection;public void Begin(Vector2Int d){dashDirection=d;ClearDashSteering();}public void Steer(Vector2Int d){UpdateDashSteering(d);}' + $gridSource.Substring($start,$end-$start) + '}'
$refreshStart = $gridSource.IndexOf('    public bool TryRefreshDash(')
$refreshEnd = $gridSource.IndexOf('    private void CompleteDashSkill()', $refreshStart)
$stub = $stub.Replace('public bool TryRefreshDash(float duration)=>false;', 'public DashRefreshHarness Refresh;public bool TryRefreshDash(float duration)=>Refresh!=null&&Refresh.TryRefreshDash(duration);')
$stub += 'public class DashRefreshHarness {private bool dashRunning=true,inputEnabled=true;private PlayerStatus playerStatus;private float dashEndsAt;private int lastDashRefreshFrame=-1;public float Deadline=>dashEndsAt;public void Setup(PlayerStatus status,float deadline){playerStatus=status;dashEndsAt=deadline;}public void End(){dashRunning=false;}' + $gridSource.Substring($refreshStart,$refreshEnd-$refreshStart) + '}'
$stub = $stub.Replace('public void PerformShockwave(int a,float b,ShockwaveEffect v=null,bool outer=false){}', 'public int LastShockSize;public bool LastShockOuter;public int ShockCalls;public void PerformShockwave(int a,float b,ShockwaveEffect v=null,bool outer=false){ShockCalls++;LastShockSize=a;LastShockOuter=outer;}')
$stub += @'
public static class ConsumptionTests {
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Call(object o,string n){o.GetType().GetMethod(n,flags).Invoke(o,null);}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static string Run(){
  var status=new PlayerStatus();var grid=new PlayerGridController();
  MonoBehaviour.Components[typeof(PlayerStatus)]=status;MonoBehaviour.Components[typeof(PlayerGridController)]=grid;
  var manager=new RunSkillManager();Call(manager,"Awake");Call(manager,"Start");
  var skills=new PlayerSkillController();Call(skills,"Awake");Call(skills,"OnEnable");
  manager.TryAddSkillItem(SkillType.Dash);manager.TryAddSkillItem(SkillType.Dash);
  var right=new Vector2Int{x=1};
  Check(!skills.TryUseSkill(SkillType.Dash,Vector2Int.zero)&&manager.DashCharges==2,"invalid direction consumed");
  grid.AllowDash=false;Check(!skills.TryUseSkill(SkillType.Dash,right)&&manager.DashCharges==2,"busy dash consumed");grid.AllowDash=true;
  Check(skills.TryUseSkill(SkillType.Dash,right)&&manager.DashCharges==1&&grid.LastDuration==1.5f,"x2 duration and consumption");
  Check(!skills.TryUseSkill(SkillType.Dash,right)&&manager.DashCharges==1,"duplicate while locked");
  Call(skills,"OnDashFinished");Check(skills.TryUseSkill(SkillType.Dash,right)&&manager.DashCharges==0,"second Dash");
  Check(grid.LastDuration==1.2f,"x1 duration");
  Call(skills,"OnDashFinished");Check(!skills.TryUseSkill(SkillType.Dash,right)&&manager.GetEvolutionLevel(SkillType.Dash)==2,"empty evolved Dash");
  manager.TryAddSkillItem(SkillType.Shield);manager.TryAddSkillItem(SkillType.Shield);
  Check(skills.TryUseSkill(SkillType.Shield,Vector2Int.zero)&&manager.ShieldCharges==1&&status.Defense==55,"Shield charge");
  Check(!skills.TryUseSkill(SkillType.Shield,Vector2Int.zero)&&manager.ShieldCharges==1,"Shield duplicate");
  Time.time=.2f;skills.Step();Check(skills.TryUseSkill(SkillType.Shield,Vector2Int.zero)&&manager.ShieldCharges==0&&status.Defense==55,"Shield refresh stacks defense");
  Time.time=.4f;skills.Step();manager.TryAddSkillItem(SkillType.Shockwave);
  Check(skills.TryUseSkill(SkillType.Shockwave,Vector2Int.zero)&&manager.ShockwaveCharges==0,"Shockwave charge");
  Check(!skills.TryUseSkill(SkillType.Shockwave,Vector2Int.zero),"Shockwave duplicate");
  Time.time=1f;skills.Step();Call(skills,"OnDashFinished");
  for(int i=0;i<3;i++)manager.TryAddSkillItem(SkillType.Dash);
  Check(skills.TryUseSkill(SkillType.Dash,right)&&manager.DashCharges==2&&grid.LastDuration==1.8f,"x3 duration");
  manager.TryAddSkillItem(SkillType.Dash);Check(grid.LastDuration==1.8f,"mid-cast pickup altered duration");
  var refresh=new DashRefreshHarness();grid.Refresh=refresh;refresh.Setup(status,5f);
  Time.time=2f;Time.frameCount=10;
  Check(skills.TryContinueDash()&&manager.DashCharges==2&&refresh.Deadline==3.8f,"refresh x3 deadline");
  Check(!skills.TryContinueDash()&&manager.DashCharges==2,"same-frame duplicate");
  Time.time=2.1f;Time.frameCount=11;
  Check(skills.TryContinueDash()&&manager.DashCharges==1&&Math.Abs(refresh.Deadline-3.6f)<.001,"refresh replaces, not adds");
  Time.time=2.2f;Time.frameCount=12;Time.timeScale=0f;
  Check(!skills.TryContinueDash()&&manager.DashCharges==1,"paused consumed");Time.timeScale=1f;
  Check(skills.TryContinueDash()&&manager.DashCharges==0&&Math.Abs(refresh.Deadline-3.4f)<.001,"x1 refresh");
  Time.frameCount=13;Check(!skills.TryContinueDash(),"zero charges refresh");
  manager.TryAddSkillItem(SkillType.Dash);Time.time=refresh.Deadline;Time.frameCount=14;
  Check(!skills.TryContinueDash()&&manager.DashCharges==1,"expired refresh consumed");
  refresh.End();Time.time=2.3f;Check(!skills.TryContinueDash(),"ended refresh");
  var steering=new SteeringHarness();steering.Begin(right);Time.time=2f;
  steering.Steer(new Vector2Int{x=-1});Check(steering.Direction.x==1,"unbuffered single turn");
  Time.time=2.05f;steering.Steer(new Vector2Int{x=-1});Check(steering.Direction.x==-1,"left turn");
  steering.Steer(Vector2Int.zero);Check(steering.Direction.x==-1,"release lost direction");
  steering.Steer(new Vector2Int{y=-1});steering.Steer(new Vector2Int{x=1,y=-1});
  Check(steering.Direction.x==1&&steering.Direction.y==-1,"diagonal combination");
  Check(grid.LastShockSize==3&&!grid.LastShockOuter&&grid.ShockCalls==1,"x1 range once");
  Call(skills,"OnDashFinished");Time.time=10f;skills.Step();manager.TryAddSkillItem(SkillType.Shockwave);
  int total=manager.GetTotalPickups(SkillType.Shockwave);
  Check(skills.TryUseSkill(SkillType.Shockwave,Vector2Int.zero)&&grid.LastShockSize==3&&manager.ShockwaveCharges==0,"reacquired x1 still 3x3");
  Time.time=11f;skills.Step();manager.TryAddSkillItem(SkillType.Shockwave);manager.TryAddSkillItem(SkillType.Shockwave);
  int zeroEvents=0;manager.OnSkillChargeChanged+=(t,c)=>{if(t==SkillType.Shockwave&&c==0)zeroEvents++;};
  Check(skills.TryUseSkill(SkillType.Shockwave,Vector2Int.zero)&&grid.LastShockSize==5&&!grid.LastShockOuter&&manager.ShockwaveCharges==0,"x2 bulk 5x5");
  Check(zeroEvents==1&&grid.ShockCalls==3,"one HUD event/one attack");
  manager.TryAddSkillItem(SkillType.Shockwave);
  Check(!skills.TryUseSkill(SkillType.Shockwave,Vector2Int.zero)&&manager.ShockwaveCharges==1,"locked E consumed");
  Time.time=12f;skills.Step();manager.TryAddSkillItem(SkillType.Shockwave);manager.TryAddSkillItem(SkillType.Shockwave);
  total=manager.GetTotalPickups(SkillType.Shockwave);
  Check(skills.TryUseSkill(SkillType.Shockwave,Vector2Int.zero)&&grid.LastShockSize==7&&grid.LastShockOuter&&manager.ShockwaveCharges==0,"x3 bulk 7x7");
  Check(grid.ShockCalls==4&&zeroEvents==2&&manager.GetTotalPickups(SkillType.Shockwave)==total,"bulk attack count/events/totals");
  for(int i=0;i<100;i++)Check(!skills.TryUseSkill(SkillType.Shockwave,Vector2Int.zero),"rapid E repeated");
  Check(grid.ShockCalls==4,"x3 produced repeated attacks");
  Time.time=13f;skills.Step();manager.TryAddSkillItem(SkillType.Shockwave);
  typeof(PlayerSkillController).GetField("gridController",flags).SetValue(skills,null);
  Check(!skills.TryUseSkill(SkillType.Shockwave,Vector2Int.zero)&&manager.ShockwaveCharges==1,"invalid cast consumed");
  typeof(PlayerSkillController).GetField("gridController",flags).SetValue(skills,grid);
  Check(skills.TryUseSkill(SkillType.Shockwave,Vector2Int.zero)&&grid.LastShockSize==3&&!grid.LastShockOuter&&manager.ShockwaveCharges==0,"high evolution x1 must be 3x3");
  Check(manager.GetEvolutionLevel(SkillType.Shockwave)==3,"bulk erased evolution");
  return "PASS: production manager + PlayerSkillController: failed/busy Dash no consumption, successful casts consume once, locked repeats rejected, Lv2/zero charges rejected, Shield refresh nonstacking and Shockwave consumption. Shockwave x1/x2/x3 bulk casts, high-evolution x1, single HUD commit and failed/locked/rapid E verified. Charge-based 1.2/1.5/1.8s snapshot and production steering method verified. Managed stand-ins; Unity not executed.";
 }
}
'@
foreach ($file in @('Assets/Scripts/Player/RunSkillManager.cs','Assets/Scripts/Player/PlayerSkillController.cs')) {
 $stub += [regex]::Replace([IO.File]::ReadAllText((Join-Path $root $file)), '(?m)^using [^;]+;\r?\n', '')
}
Add-Type ("#pragma warning disable 0414`n" + $stub)
[ConsumptionTests]::Run()

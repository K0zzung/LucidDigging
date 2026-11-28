"""Portable tests of production card selection methods and number-key gate; no Unity play."""
from pathlib import Path
import tempfile,subprocess
root=Path(__file__).resolve().parents[1];folder=Path(tempfile.mkdtemp(prefix='LucidShortcuts-'))
b=(root/'Assets/Scripts/UI/RunCardSelectionUI.cs').read_bytes()
try:source=b.decode('utf-8-sig')
except UnicodeDecodeError:source=b.decode('cp949')
def method(signature):
 start=source.index(signature);opening=source.index('{',start);depth=1;i=opening+1
 while depth:
  if source[i]=='{':depth+=1
  elif source[i]=='}':depth-=1
  i+=1
 return source[start:i].replace('private void','public void',1)
methods='\n'.join(method(s) for s in ['private void Update()','private void SelectCard(','private void Hide()'])
inventory=(root/'Assets/Scripts/UI/RunInventoryPanel.cs').read_text(encoding='utf-8')
cardSource=source
source=inventory
inventoryUpdate=method('private void Update()')
inventoryState=method('private void HandleState(GameState state)')
source=cardSource
(folder/'UiNumberShortcuts.cs').write_bytes((root/'Assets/Scripts/UI/UiNumberShortcuts.cs').read_bytes())
(folder/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
(folder/'Program.cs').write_text(r'''
using System;using UnityEngine;
namespace UnityEngine {
 public enum KeyCode{Alpha1=49,Alpha2,Alpha3,Tab=9,Keypad1=257,Keypad2,Keypad3}
 public enum RuntimeInitializeLoadType{SubsystemRegistration}public class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
 public static class Time{public static int frameCount=10;public static float timeScale=1f;}
 public static class Input {public static System.Collections.Generic.HashSet<KeyCode> downs=new System.Collections.Generic.HashSet<KeyCode>();public static bool GetKeyDown(KeyCode key)=>downs.Contains(key);}
 public class GameObject {public bool activeSelf=true;public bool activeInHierarchy=>activeSelf;public void SetActive(bool v){activeSelf=v;}}
 public static class Debug{public static void Log(string s){}public static void LogException(Exception e,object o){}}
}
public enum GameState {LevelUpSelection,Playing,GameOver,Ready}
public class GameFlow {public GameState CurrentState=GameState.LevelUpSelection;public bool CanConfirmRunCard=true;public void EndLevelUpSelection(){CurrentState=GameState.Playing;}}
public class Progression {public int PendingLevelUps=3;}
public class RunCardData{public string Id;}
public interface IRunCardSelectionHandler {bool TrySelectCard(RunCardData c);}
public class Button {public bool interactable=true,isActiveAndEnabled=true;public GameObject gameObject=new GameObject();public bool IsInteractable()=>interactable;}
public class Handler:IRunCardSelectionHandler{public int count;public string last;public Progression progress;public bool fail;public bool TrySelectCard(RunCardData card){if(fail)return false;count++;last=card.Id;progress.PendingLevelUps--;return true;}}
public class Harness {
 public bool configured=true,acceptingSelection=true;public int offerFrame=-1,offerVersion=1,selectionAttemptFrame=-1;
 public GameObject root=new GameObject();public bool IsOpen=>root!=null&&root.activeSelf;
 public GameFlow gameFlow=new GameFlow();public Progression progression=new Progression();public object selectionHandler;
 public Button[] buttons={new Button(),new Button(),new Button()};public RunCardData[] offeredCards={new RunCardData{Id="top"},new RunCardData{Id="middle"},new RunCardData{Id="bottom"}};
 public Action<RunCardData> OnCardSelected;
 public void ShowNextOffer(){offerVersion++;offerFrame=Time.frameCount;UiNumberShortcuts.BlockThisFrame();acceptingSelection=true;foreach(var b in buttons)b.interactable=true;}
''' +methods+r'''
}
public class RunProgression {public static RunProgression Instance=new RunProgression();public int PendingLevelUps;}
public class EquipmentRequests {public int swords,picks;public bool EquipLastWeapon(){swords++;return true;}public bool EquipLastPickaxe(){picks++;return true;}}
public class RootRect {public GameObject gameObject=new GameObject();}
public class StatusLabel {public string text;}
public class InventoryHarness {
 public bool built=true,hudVisible=true,Playing=true,opened=true;public bool cardSelectionSuspended;
 public void ResetDrawer(){opened=false;}public void HideWithGameplayHUD(){hudVisible=false;opened=false;cardSelectionSuspended=false;overlay.gameObject.SetActive(false);}
 public RootRect overlay=new RootRect();public EquipmentRequests weapons=new EquipmentRequests();public StatusLabel statusText=new StatusLabel();
 public void Refresh(){}public void Toggle(){opened=!opened;UiNumberShortcuts.BlockThisFrame();}
''' +inventoryUpdate+'\n'+inventoryState+r'''
}
class Program {
 static int count;static void Check(bool b,string name){if(!b)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static Harness New(){Time.frameCount++;Input.downs.Clear();var h=new Harness();h.selectionHandler=new Handler{progress=h.progression};return h;}
 static Handler H(Harness h)=>(Handler)h.selectionHandler;
 static void Press(KeyCode k){Input.downs.Clear();Input.downs.Add(k);}
 static void Main(){
 for(int i=0;i<3;i++){var h=New();Press((KeyCode)((int)KeyCode.Alpha1+i));h.Update();Check(H(h).count==1&&H(h).last==new[]{"top","middle","bottom"}[i],"card keyboard order "+(i+1));}
 var card=New();int version=card.offerVersion;Press(KeyCode.Alpha1);card.Update();card.SelectCard(1,version);Check(H(card).count==1,"keyboard then same-frame mouse commits once");
 card=New();version=card.offerVersion;card.SelectCard(1,version);Press(KeyCode.Alpha1);card.Update();Check(H(card).count==1&&H(card).last=="middle","mouse then keyboard commits once");
 card=New();Press(KeyCode.Alpha1);card.Update();Time.frameCount++;Input.downs.Clear();card.Update();Check(H(card).count==1,"held key without new KeyDown does not select next offer");
 Press(KeyCode.Alpha2);Time.frameCount++;card.Update();Check(H(card).count==2&&H(card).last=="middle","new key selects next reward");
 card=New();card.buttons[0].interactable=false;Press(KeyCode.Alpha1);card.Update();Check(H(card).count==0,"disabled button ignored");
 card=New();card.buttons[0].gameObject.SetActive(false);Press(KeyCode.Alpha1);card.Update();Check(H(card).count==0,"inactive button ignored");
 card=New();card.offeredCards=new[]{new RunCardData{Id="only"}};Press(KeyCode.Alpha3);card.Update();Check(H(card).count==0,"missing third card ignored");
 card=New();card.progrESSION_PLACEHOLDER
 card=New();card.gameFlow.CurrentState=GameState.Playing;Press(KeyCode.Alpha1);card.Update();Check(H(card).count==0,"no card input during gameplay");
 card=New();card.progrESSION_PLACEHOLDER2
 card=New();H(card).fail=true;Press(KeyCode.Alpha1);card.Update();card.SelectCard(1,card.offerVersion);Check(card.selectionAttemptFrame==Time.frameCount&&H(card).count==0,"failed handler cannot be invoked twice in a frame");
 card=New();card.Hide();Press(KeyCode.Alpha1);card.Update();Check(H(card).count==0&&!UiNumberShortcuts.TryRead(out _),"closed card blocks transition-frame number dispatch");
 card=New();Press(KeyCode.Keypad2);card.Update();Check(H(card).last=="middle","keypad supported");
 Time.frameCount++;Press(KeyCode.Alpha1);Check(UiNumberShortcuts.TryRead(out int number)&&number==1&&!UiNumberShortcuts.TryRead(out _),"one numeric dispatch per frame");
 Time.frameCount++;UiNumberShortcuts.BlockThisFrame();Press(KeyCode.Alpha2);Check(!UiNumberShortcuts.TryRead(out _),"inventory toggle frame blocks number fallthrough");

 var inv=new InventoryHarness();Time.frameCount++;Press(KeyCode.Alpha1);inv.Update();Check(inv.weapons.swords==1&&inv.weapons.picks==0,"open inventory number1 calls weapon API");
 Time.frameCount++;Press(KeyCode.Alpha2);inv.Update();Check(inv.weapons.picks==1,"open inventory number2 calls pickaxe API");
 Time.frameCount++;Press(KeyCode.Alpha3);inv.Update();Check(inv.weapons.swords==1&&inv.weapons.picks==1,"inventory ignores number3");
 var closed=new InventoryHarness{opened=false};Time.frameCount++;Press(KeyCode.Alpha1);closed.Update();
 Check(closed.weapons.swords==1&&!closed.opened,"closed inventory allows weapon swap without opening");
 Time.frameCount++;Press(KeyCode.Alpha2);closed.Update();
 Check(closed.weapons.picks==1&&!closed.opened,"closed inventory allows pickaxe swap without opening");
 Check(inv.opened,"open drawer stays open after swapping");
 inv.opened=true;inv.Playing=false;Time.frameCount++;Press(KeyCode.Alpha2);inv.Update();Check(inv.weapons.picks==1,"card selection state blocks equipment");
 inv.Playing=true;RunProgression.Instance.PendingLevelUps=1;Time.frameCount++;Press(KeyCode.Alpha1);inv.Update();Check(inv.weapons.swords==1,"pending offer has priority before card LateUpdate");
 RunProgression.Instance.PendingLevelUps=0;Time.frameCount++;UiNumberShortcuts.BlockThisFrame();Press(KeyCode.Alpha1);inv.Update();Check(inv.weapons.swords==1,"card transition cannot fall through to equipment");
 Time.frameCount++;Press(KeyCode.Tab);Input.downs.Add(KeyCode.Alpha1);inv.Update();Check(!inv.opened&&inv.weapons.swords==1,"Tab plus number only closes inventory");
 inv.opened=true;Time.timeScale=0;Time.frameCount++;Press(KeyCode.Alpha1);inv.Update();Check(inv.weapons.swords==1,"paused inventory ignores numbers");Time.timeScale=1;
 inv.hudVisible=false;Time.frameCount++;Press(KeyCode.Alpha2);inv.Update();Check(inv.weapons.picks==1,"hidden HUD ignores numbers");

 inv=new InventoryHarness();Time.frameCount++;inv.HandleState(GameState.LevelUpSelection);
 Check(inv.opened&&inv.overlay.gameObject.activeSelf,"card modal preserves open inventory");
 inv.Playing=false;Time.frameCount++;Press(KeyCode.Alpha1);inv.Update();Check(inv.weapons.swords==0,"card number cannot equip while inventory remains visible");
 inv.Playing=true;inv.HandleState(GameState.Playing);inv.Update();Check(inv.opened&&inv.weapons.swords==0,"card completion preserves drawer and blocks confirmation key");
 Time.frameCount++;Input.downs.Clear();inv.Update();Check(inv.weapons.swords==0,"held card key cannot equip after completion");
 Time.frameCount++;Press(KeyCode.Alpha1);inv.Update();Check(inv.weapons.swords==1,"fresh number works after card modal");
 inv.opened=false;inv.HandleState(GameState.LevelUpSelection);inv.HandleState(GameState.Playing);Check(!inv.opened,"closed drawer stays closed across cards");
 inv.opened=true;inv.HandleState(GameState.LevelUpSelection);inv.HandleState(GameState.GameOver);Check(!inv.opened&&!inv.hudVisible,"death still closes inventory");
 Console.WriteLine(count+" shortcut tests passed. Production methods with managed doubles, not Unity play.");
 }
}
'''.replace('card.progrESSION_PLACEHOLDER2','card.progression.PendingLevelUps=1;Press(KeyCode.Alpha1);card.Update();Time.frameCount++;Press(KeyCode.Alpha2);card.Update();Check(H(card).count==1&&!card.IsOpen,"final reward closes UI; subsequent keys ignored");').replace('card.progrESSION_PLACEHOLDER','card.progression.PendingLevelUps=0;Press(KeyCode.Alpha1);card.Update();Check(H(card).count==0,"no rewards ignored");'),encoding='utf-8')
result=subprocess.run(['dotnet','run','--project',str(folder/'Tests.csproj'),'--verbosity','quiet'],cwd=root,text=True,encoding='utf-8',errors='replace',capture_output=True)
print(result.stdout);print(result.stderr);raise SystemExit(result.returncode)

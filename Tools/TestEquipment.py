"""Managed tests of production equipment/save/damage logic using Unity API doubles; no Editor simulation."""
from pathlib import Path
import tempfile,subprocess
root=Path(__file__).resolve().parents[1]
folder=Path(tempfile.mkdtemp(prefix='LucidEquipment-'))
for rel in ['Weapon/WeaponData.cs','Weapon/WeaponManager.cs','Weapon/EquipmentCombat.cs','Weapon/PickaxeBossReward.cs','System/DamageCalculator.cs','System/TerrainData.cs','System/TerrainManager.cs','UI/HUDController.cs','UI/IInventoryEquipmentProvider.cs','Player/PermanentUpgradeManager.cs','Player/PermanentUpgradeType.cs','Currency/CurrencyManager.cs','Currency/CurrencyType.cs']:
 b=(root/'Assets/Scripts'/rel).read_bytes()
 try:s=b.decode('utf-8-sig')
 except UnicodeDecodeError:s=b.decode('cp949')
 (folder/Path(rel).name).write_text(s,encoding='utf-8')
def production_method(path,signature):
 b=(root/path).read_bytes()
 try:source=b.decode('utf-8-sig')
 except UnicodeDecodeError:source=b.decode('cp949')
 start=source.index(signature);i=source.index('{',start)+1;depth=1
 while depth:
  if source[i]=='{':depth+=1
  elif source[i]=='}':depth-=1
  i+=1
 return source[start:i]
category=production_method('Assets/Scripts/UI/ShopUpgradeCategories.cs','public static int CategoryOf(')
click=production_method('Assets/Scripts/Player/PermanentUpgradeRowUI.cs','public void OnClickUpgrade()')
(folder/'ShopHarness.cs').write_text('using UnityEngine;\npublic static class ShopClassifier{'+category+'}\n'+
 'public class PurchaseButton{public bool interactable=true;public bool IsInteractable()=>interactable;}\n'+
 'public class RowHarness {public bool isActiveAndEnabled=true;public GameObject gameObject=new GameObject{activeSelf=true};public int blockedPurchaseFrame=-1;public PurchaseButton upgradeButton=new PurchaseButton();public PermanentUpgradeType upgradeType=PermanentUpgradeType.PickaxeAttack;public CurrencyType currencyType=CurrencyType.Dash;public object levelText,costText,currentValueText;public int refreshes,punches;private void Refresh(){refreshes++;}private void PlayTextPunch(object o){punches++;}'+click+'}',encoding='utf-8')
(folder/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649</NoWarn></PropertyGroup></Project>')
(folder/'Doubles.cs').write_text(r'''
using System;using System.Collections.Generic;
namespace UnityEngine {
 public class Object {public static void Destroy(object o){} public static T Instantiate<T>(T p,Vector3 v,Quaternion q)=>p;}
 public class MonoBehaviour:Object {public GameObject gameObject=new GameObject();public bool isActiveAndEnabled=true; protected T FindObjectOfType<T>() where T:new()=>new T();}
 public class ScriptableObject:Object {} public class Sprite:Object {} public class GameObject:Object {public T GetComponent<T>() where T:new()=>new T();public bool activeSelf;public bool activeInHierarchy=>activeSelf;public void SetActive(bool b){activeSelf=b;}} public class AudioClip:Object {}
 public class RectTransform {public Vector2 anchoredPosition;public object component;public T GetComponent<T>()=> (T)component;}
 public class SerializeField:Attribute{} public class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}
 public class MinAttribute:Attribute{public MinAttribute(float f){}}public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}
 public class ContextMenu:Attribute{public ContextMenu(string s){}}
 [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenuAttribute:Attribute{public string fileName;public string menuName;}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public struct Vector3 {public Vector3(float x,float y,float z){}public static Vector3 up=>new Vector3();public static Vector3 right=>new Vector3();public static Vector3 operator +(Vector3 a,Vector3 b)=>a; public static Vector3 operator -(Vector3 a,Vector3 b)=>a;public static Vector3 operator *(Vector3 a,float b)=>a;public float magnitude=>1;}
 public struct Vector3Int{public int x,y,z;public Vector3Int(int a,int b,int c){x=a;y=b;z=c;}public static Vector3Int up=>new Vector3Int();public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>a;}
 public struct Quaternion{public static Quaternion identity=>new Quaternion();}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int RoundToInt(float v)=>(int)Math.Round(v,MidpointRounding.ToEven);public static float Clamp01(float v)=>Math.Clamp(v,0,1);}
 public static class Random {public static float value=>.1f;}
 public static class Time {public static float timeScale=1;public static int frameCount=10;}
 public static class Debug {public static void Log(string s){} public static void LogError(string s){}public static void LogWarning(string s){}}
 public static class PlayerPrefs {
  public static Dictionary<string,string> data=new Dictionary<string,string>();
  public static int GetInt(string k,int d=0)=>data.ContainsKey(k)?int.Parse(data[k]):d;
  public static void SetInt(string k,int v)=>data[k]=v.ToString();
  public static string GetString(string k,string d="")=>data.ContainsKey(k)?data[k]:d;
  public static void SetString(string k,string v)=>data[k]=v;public static void Save(){}public static void DeleteKey(string k)=>data.Remove(k);
 }
}
public enum GameState { Playing,GameOver,Inventory }public class GameFlowController {public static GameFlowController Instance=new GameFlowController();public GameState CurrentState=GameState.Playing;}
public class PlayerGridController {public bool IsActionInProgress; public bool TryGetBossLandingCell(out UnityEngine.Vector3Int c){c=new UnityEngine.Vector3Int();return true;}}
public class PlayerStatus {public int Attack=160,BaseAttack=10,Defense=5,BaseDefense=5,MaxHP=100,PermanentMaxHP=100,RunAttackBonus,RunAttackPenalty;
 public void IncreasePermanentAttack(int amount){BaseAttack+=amount;Attack+=amount;UnityEngine.PlayerPrefs.SetInt("Player_Attack",BaseAttack);}
 public void IncreasePermanentDefense(int amount){BaseDefense+=amount;Defense+=amount;UnityEngine.PlayerPrefs.SetInt("Player_Defense",Defense);}
 public void IncreasePermanentMaxHP(int amount){PermanentMaxHP+=amount;MaxHP+=amount;UnityEngine.PlayerPrefs.SetInt("Player_MaxHP",MaxHP);}
}
public class PlayerSkillController {public float GetCurrentDashAttackPercent()=>200;public float GetCurrentShockwaveAttackPercent()=>150;public int GetCurrentShieldDefenseBonus()=>50;}
public class ShopPanelController {public bool IsOpen;}
public class InventoryUIController {public bool IsOpen=true,isActiveAndEnabled=true;}

public class EnemyBase {public EnemyCombatCategory CombatCategory; }public class BossEnemy:EnemyBase{}public class BossExplosionEnemy:EnemyBase{}
public class EnemyManager {public UnityEngine.Vector3 GetCellCenterWorld(UnityEngine.Vector3Int cell)=>new UnityEngine.Vector3();}
namespace UnityEngine.Tilemaps {
 public class TileBase:UnityEngine.Object{}
 public class Tilemap {
 private readonly System.Collections.Generic.Dictionary<UnityEngine.Vector3Int,TileBase> cells=new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int,TileBase>();
 public TileBase GetTile(UnityEngine.Vector3Int c)=>cells.TryGetValue(c,out var t)?t:null;
 public void SetTile(UnityEngine.Vector3Int c,TileBase t){if(t==null)cells.Remove(c);else cells[c]=t;}
 public bool HasTile(UnityEngine.Vector3Int c)=>GetTile(c)!=null;
 public UnityEngine.Vector3 GetCellCenterWorld(UnityEngine.Vector3Int c)=>new UnityEngine.Vector3();
 public UnityEngine.Sprite GetSprite(UnityEngine.Vector3Int c)=>null;
 }
}
public class EnemyHealthBar:UnityEngine.Object {public object gameObject;public void SetRatio(float value){}}
public class TerrainHitEffect:UnityEngine.Object{public void Play(UnityEngine.Sprite s,UnityEngine.Vector3 p){}}
public class TerrainBreakEffect:UnityEngine.Object{public void Play(UnityEngine.Sprite s,UnityEngine.Vector3 p){}}
public class AudioManager {public static AudioManager Instance;public void PlayTerrainBreak(){}}
namespace DG.Tweening {
 public enum Ease {OutBack,InBack}
 public class Tween {public void Kill(){}public Tween SetEase(Ease e)=>this;}
 public static class ShortcutExtensions {
  public static Tween DOAnchorPos(this UnityEngine.RectTransform r,UnityEngine.Vector2 p,float duration){HudTrace.Events.Add("SkillTween");return new Tween();}
 }
}
public static class HudTrace {public static System.Collections.Generic.List<string> Events=new System.Collections.Generic.List<string>();}
public class RunInventoryPanel {public void ShowWithGameplayHUD(){HudTrace.Events.Add("HandleShow");}public void HideWithGameplayHUD(){HudTrace.Events.Add("HandleHide");}}
public class WeaponPickup {public void Initialize(WeaponData p,UnityEngine.Vector3Int c,UnityEngine.Vector3 s,PlayerGridController player,float rise){}}
''',encoding='utf-8')
(folder/'Program.cs').write_text(r'''
using System;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;
class Program {
 static int count;static void Check(bool pass,string name){if(!pass)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static void Awake(WeaponManager m)=>typeof(WeaponManager).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null);
 static WeaponData Item(string id,bool pick,int attack){var p=new WeaponData();Set(p,"weaponId",id);Set(p,"isPickaxe",pick);Set(p,"pickaxeAttack",attack);return p;}
 static WeaponManager Manager(List<WeaponData> swords,List<WeaponData> picks,PlayerGridController player){var m=new WeaponManager();Set(m,"defaultWeapon",swords[0]);Set(m,"allWeapons",swords);Set(m,"allPickaxes",picks);Set(m,"defaultPickaxe",picks[0]);Set(m,"equipmentPlayer",player);Awake(m);return m;}
 static void Main(){
 var sword=Item("0",false,10);var stone=Item("stone_axe",true,10);var aron=Item("arondight_pickaxe",true,160);var dragon=Item("dragon_pickaxe",true,310);
 var swords=new List<WeaponData>{sword};var picks=new List<WeaponData>{stone,aron,dragon};var player=new PlayerGridController();
 PlayerPrefs.SetInt("Player_Attack",130);PlayerPrefs.SetInt("Skill_DashLevel",7);
 var m=Manager(swords,picks,player);
 Check(m.IsOwned(stone)&&!m.IsOwned(aron)&&!m.IsOwned(dragon),"default ownership");
 Check(!m.EquipPickaxe(aron)&&m.ActiveEquipment==sword,"locked equip rejected");
 Check(m.EquipPickaxe(stone)&&m.IsPickaxeActive&&m.ActiveEquipment==stone,"single active pickaxe");
 Check(m.CurrentWeapon==sword,"skills retain sword reference");
 Check(m.Items.Count(i=>i.Equipped)==1,"one pickaxe outline");
 player.IsActionInProgress=true;Check(!m.EquipWeapon(sword)&&m.ActiveEquipment==stone,"busy sword switch rejected");
 Check(!m.EquipPickaxe(stone),"busy pickaxe switch rejected");player.IsActionInProgress=false;
 Check(m.EquipWeapon(sword)&&!m.IsPickaxeActive&&m.Items.All(i=>!i.Equipped),"switch to same remembered sword");
 Check(m.UnlockWeapon(aron)&&!m.UnlockWeapon(aron),"unlock only once");
 Check(m.EquipPickaxe(aron),"equip unlocked reward");
 var status=new PlayerStatus{Attack=999,RunAttackBonus=6,RunAttackPenalty=2};var monster=new EnemyBase();var turret=new EnemyBase{CombatCategory=EnemyCombatCategory.Turret};
 Check(m.GetNormalAttack(status,monster)==41,"pickaxe ignores permanent and sword ATK; adds run delta");
 Check(m.GetNormalAttack(status,new BossEnemy())==41,"pickaxe boss efficiency");
 Check(m.GetNormalAttack(status,turret)==328,"pickaxe turret 200 percent");
 Check(DamageCalculator.CalculateDamage(m.GetNormalAttack(status,turret),100)==164,"actual DamageCalculator defense");
 m.EquipWeapon(sword);Check(m.GetNormalAttack(status,monster)==999&&m.GetNormalAttack(status,turret)==250,"weapon target efficiency");
 m.EquipPickaxe(aron);var loaded=Manager(swords,picks,player);Check(loaded.ActiveEquipment==aron&&loaded.IsOwned(aron),"active equipment and unlock reload");
 Check(PlayerPrefs.GetInt("Player_Attack")==130&&PlayerPrefs.GetInt("Skill_DashLevel")==7,"permanent save keys preserved");
 Check(loaded.CurrentWeapon==sword&&PlayerPrefs.GetString("EquippedWeaponId")=="0","legacy equipped key preserved");
 Check(PickaxeBossReward.ShouldDrop(false,false,.15f,.149f)&&!PickaxeBossReward.ShouldDrop(false,false,.15f,.15f),"15 percent boundary");
 Check(!PickaxeBossReward.ShouldDrop(true,false,1,0)&&!PickaxeBossReward.ShouldDrop(false,true,1,0),"owned and pending drops excluded");
 Check(!PickaxeBossReward.ShouldDrop(false,false,0,0)&&PickaxeBossReward.ShouldDrop(false,false,1,.999f),"drop probability 0 and 1");
 Time.timeScale=0;Check(!loaded.EquipWeapon(sword),"pause switch rejected");Time.timeScale=1;
 GameFlowController.Instance.CurrentState=GameState.GameOver;Check(!loaded.EquipWeapon(sword),"gameover switch rejected");
 Check(EquipmentCombat.Attack(10,0,100,.25f)==1,"attack minimum");
 GameFlowController.Instance.CurrentState=GameState.Playing;
 Check(loaded.EquipLastWeapon()&&loaded.ActiveEquipment==sword,"number shortcut remembers weapon");
 Check(loaded.EquipLastPickaxe()&&loaded.ActiveEquipment==aron,"number shortcut remembers pickaxe across reload");
 loaded.EquipPickaxe(stone);loaded.EquipWeapon(sword);
 Check(loaded.EquipLastPickaxe()&&loaded.ActiveEquipment==stone,"mouse pickaxe selection updates shortcut");
 loaded.EquipPickaxe(aron);loaded.EquipWeapon(sword);PlayerPrefs.DeleteKey("PickaxeOwned_arondight_pickaxe");
 Check(loaded.EquipLastPickaxe()&&loaded.ActiveEquipment==stone,"locked remembered pickaxe falls back to default");
 player.IsActionInProgress=true;Check(!loaded.EquipLastWeapon()&&loaded.ActiveEquipment==stone,"shortcut refuses busy equip without queue");player.IsActionInProgress=false;
 loaded.EquipWeapon(sword);PlayerPrefs.DeleteKey("LastSelectedPickaxeId");var noHistory=Manager(swords,picks,player);
 Check(noHistory.EquipLastPickaxe()&&noHistory.ActiveEquipment==stone,"no pickaxe history uses StoneAxe");

 GameFlowController.Instance.CurrentState=GameState.Playing;
 var tm=new TerrainManager();var map=new UnityEngine.Tilemaps.Tilemap();
 var tile=new UnityEngine.Tilemaps.TileBase();var ordinaryTile=new UnityEngine.Tilemaps.TileBase();
 var tileData=new TerrainData{tile=tile,maxHP=35,attack=100,defense=30,combatCategory=TerrainCombatCategory.Turret};
 var ordinary=new TerrainData{tile=ordinaryTile,maxHP=100,defense=30};
 Set(tm,"terrainTilemap",map);Set(tm,"terrainDatas",new List<TerrainData>{tileData,ordinary});
 typeof(TerrainManager).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tm,null);
 var cell=new Vector3Int(1,1,0);var ordinaryCell=new Vector3Int(2,1,0);tm.PlaceTerrain(cell,tileData);tm.PlaceTerrain(ordinaryCell,ordinary);
 Check(tm.IsTurret(cell)&&!tm.IsTurret(ordinaryCell)&&!tm.IsTurret(new Vector3Int(8,8,0)),"actual Tilemap lookup distinguishes turret / ordinary / empty");
 loaded.EquipWeapon(sword);status.Attack=10;status.RunAttackBonus=0;status.RunAttackPenalty=0;
 Check(!tm.DamageTerrain(cell,loaded.GetTurretAttack(status),out int swordDamage)&&swordDamage==2,"actual TerrainManager sword damage and survival");
 Check(tm.GetTerrainAttack(cell)==100&&tm.GetTerrainDefense(cell)==30&&tm.HasTerrain(cell),"turret retaliation stats and surviving tile preserved");
 loaded.EquipPickaxe(stone);
 Check(!tm.DamageTerrain(cell,loaded.GetTurretAttack(status),out int pickDamage)&&pickDamage==15,"actual TerrainManager pickaxe 200 percent damage");
 Check(tm.DamageTerrain(cell,loaded.GetTurretAttack(status))==false&&tm.DamageTerrain(cell,loaded.GetTurretAttack(status))&&!tm.HasTerrain(cell),"turret destruction through existing tile removal");
 Check(!tm.DamageTerrain(ordinaryCell,status.Attack,out int terrainDamage)&&terrainDamage==8,"ordinary terrain old damage preserved");
 Console.WriteLine("TURRET MATRIX: base permanent ATK10, no Run bonus; DEF1 / DEF5 / DEF30");
 int[] swordAttacks={10,160,310};int[] axeAttacks={10,160,310};
 for(int d=0;d<3;d++) {
  int defense=new int[]{1,5,30}[d];var sw=new int[3];var ax=new int[3];
  for(int i=0;i<3;i++) {
   loaded.EquipWeapon(sword);status.Attack=swordAttacks[i];sw[i]=DamageCalculator.CalculateDamage(loaded.GetTurretAttack(status),defense);
   Set(stone,"pickaxeAttack",axeAttacks[i]);loaded.EquipPickaxe(stone);ax[i]=DamageCalculator.CalculateDamage(loaded.GetTurretAttack(status),defense);
   Check(ax[i]>sw[i],$"same tier {i} pickaxe superiority at DEF{defense}");
  }
  Console.WriteLine($"DEF{defense}: Sword [{string.Join(",",sw)}] / Pickaxe [{string.Join(",",ax)}]");
  for(int i=0;i<3;i++)for(int j=0;j<3;j++)Console.WriteLine($"PAIR Sword{i}/Pickaxe{j} DEF{defense}: {sw[i]}/{ax[j]}");
 }

 var hud=new HUDController();var skillBar=new RectTransform{component=new RunInventoryPanel()};
 Set(hud,"skillBar",skillBar);Set(hud,"hpSlider",new GameObject());
 hud.ShowGameplayHUD();Check(HudTrace.Events.SequenceEqual(new[]{"HandleShow","SkillTween"}),"actual HUD show synchronously starts handle before QWE tween");
 HudTrace.Events.Clear();hud.HideGameplayHUD();Check(HudTrace.Events.SequenceEqual(new[]{"HandleHide","SkillTween"}),"actual HUD hide closes inventory too");
 HudTrace.Events.Clear();hud.ResetHUD();Check(HudTrace.Events.SequenceEqual(new[]{"HandleHide"}),"actual HUD reset clears inventory presentation");
 int hits=0;var random=new System.Random(1655);for(int i=0;i<100000;i++)if(PickaxeBossReward.ShouldDrop(false,false,.15f,(float)random.NextDouble()))hits++;
 Check(Math.Abs(hits-15000)<600,"100000 independent drop rolls");
 GameFlowController.Instance.CurrentState=GameState.Inventory;
 PlayerPrefs.SetInt("Currency_Dash",100);PlayerPrefs.SetInt("Currency_Shockwave",100);PlayerPrefs.SetInt("Currency_Shield",100);
 var currency=new CurrencyManager();typeof(CurrencyManager).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(currency,null);
 var upgrades=new PermanentUpgradeManager();var shop=new InventoryUIController();var stats=new PlayerStatus{Attack=160};
 Set(upgrades,"shopUI",shop);Set(upgrades,"playerStatus",stats);Set(upgrades,"attackIncreasePerLevel",10);Set(upgrades,"hpIncreasePerLevel",50);Set(upgrades,"defenseIncreasePerLevel",5);
 typeof(PermanentUpgradeManager).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(upgrades,null);
 PlayerPrefs.SetInt("PermanentUpgrade_Attack_Level",5);PlayerPrefs.SetInt("PermanentUpgrade_Shield_Level",4);
 Check(upgrades.GetLevel(PermanentUpgradeType.Attack)==5&&upgrades.GetLevel(PermanentUpgradeType.Shield)==4,"legacy levels unchanged");
 Check((int)PermanentUpgradeType.Shockwave==5&&(int)PermanentUpgradeType.PickaxeAttack==6,"serialized enum compatibility");
 Check(upgrades.GetLevel(PermanentUpgradeType.PickaxeAttack)==0&&upgrades.PickaxeAttackBonus==0,"new and old saves default to pickaxe level zero");
 Check(upgrades.GetUpgradeCost(PermanentUpgradeType.Attack)==6&&upgrades.GetUpgradeCost(PermanentUpgradeType.PickaxeAttack)==1,"old and new cost rules");
 Time.frameCount++;int before=currency.DashCurrency;
 Check(upgrades.TryUpgrade(PermanentUpgradeType.PickaxeAttack,CurrencyType.Dash)&&currency.DashCurrency==before-1&&upgrades.GetLevel(PermanentUpgradeType.PickaxeAttack)==1,"pickaxe purchase debits exactly once");
 Check(!upgrades.TryUpgrade(PermanentUpgradeType.PickaxeAttack,CurrencyType.Dash)&&currency.DashCurrency==before-1,"same-frame duplicate purchase rejected");
 Check(upgrades.PickaxeAttackBonus==10&&upgrades.GetLevel(PermanentUpgradeType.Attack)==5&&stats.Attack==160,"pickaxe purchase leaves sword stats and legacy level unchanged");
 GameFlowController.Instance.CurrentState=GameState.Playing;loaded.EquipPickaxe(stone);Set(stone,"pickaxeAttack",10);status.Attack=160;status.RunAttackBonus=0;status.RunAttackPenalty=0;
 Check(loaded.GetTurretAttack(status)==40&&DamageCalculator.CalculateDamage(loaded.GetTurretAttack(status),30)==31,"permanent pickaxe bonus reaches actual defense calculation");
 status.RunAttackBonus=6;status.RunAttackPenalty=2;Check(loaded.GetTurretAttack(status)==48,"Run bonus and Nightmare penalty still additive");
 loaded.UnlockWeapon(dragon);loaded.EquipPickaxe(dragon);Check(loaded.GetPickaxeBaseAttack()==320,"same permanent bonus shared by other pickaxe");
 loaded.EquipWeapon(sword);Check(loaded.GetNormalAttack(status,monster)==160,"pickaxe upgrade does not change sword or skill Attack");
 GameFlowController.Instance.CurrentState=GameState.Inventory;Time.frameCount++;before=currency.DashCurrency;
 Check(upgrades.TryUpgrade(PermanentUpgradeType.Attack,CurrencyType.Dash)&&stats.Attack==170&&currency.DashCurrency==before-6&&upgrades.PickaxeAttackBonus==10,"legacy weapon purchase remains independent");
 Time.frameCount++;before=currency.DashCurrency;bool nested=false;
 Action reenter=()=>{nested=upgrades.TryUpgrade(PermanentUpgradeType.PickaxeAttack,CurrencyType.Dash);};currency.OnCurrencyChanged+=reenter;
 Check(upgrades.TryUpgrade(PermanentUpgradeType.PickaxeAttack,CurrencyType.Dash)&&!nested&&currency.DashCurrency==before-2,"currency-event reentrancy cannot double-buy");currency.OnCurrencyChanged-=reenter;
 Time.frameCount++;shop.IsOpen=false;before=currency.DashCurrency;Check(!upgrades.TryUpgrade(PermanentUpgradeType.PickaxeAttack,CurrencyType.Dash)&&currency.DashCurrency==before,"closed shop does not charge");shop.IsOpen=true;
 Time.frameCount++;Check(!upgrades.TryUpgrade(PermanentUpgradeType.PickaxeAttack,CurrencyType.Shield),"wrong currency rejected");
 Time.frameCount++;GameFlowController.Instance.CurrentState=GameState.Playing;Check(!upgrades.TryUpgrade(PermanentUpgradeType.PickaxeAttack,CurrencyType.Dash),"gameplay cannot buy");GameFlowController.Instance.CurrentState=GameState.Inventory;
 Time.frameCount++;currency.SpendCurrency(CurrencyType.Dash,currency.DashCurrency);int level=upgrades.GetLevel(PermanentUpgradeType.PickaxeAttack);
 Check(!upgrades.TryUpgrade(PermanentUpgradeType.PickaxeAttack,CurrencyType.Dash)&&upgrades.GetLevel(PermanentUpgradeType.PickaxeAttack)==level,"insufficient currency keeps level");
 typeof(CurrencyManager).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(currency,null);
 Check(currency.DashCurrency==0&&upgrades.GetLevel(PermanentUpgradeType.PickaxeAttack)==2,"purchase save reload keeps level and currency");
 Check(PlayerPrefs.GetString("EquippedWeaponId")=="0"&&PlayerPrefs.GetInt("Skill_DashLevel")==7,"equipment and old skill save keys preserved");
 PlayerPrefs.SetInt("PermanentUpgrade_PickaxeAttack_Level",-8);Check(upgrades.GetLevel(PermanentUpgradeType.PickaxeAttack)==0&&upgrades.PickaxeAttackBonus==0,"negative saved level safely reads zero");
 PlayerPrefs.SetInt("PermanentUpgrade_PickaxeAttack_Level",int.MaxValue);Time.frameCount++;
 Check(upgrades.GetUpgradeCost(PermanentUpgradeType.PickaxeAttack)==int.MaxValue&&!upgrades.TryUpgrade(PermanentUpgradeType.PickaxeAttack,CurrencyType.Dash),"integer-limit level and cost do not overflow");
 Check(EquipmentCombat.Attack(int.MaxValue,100,0,2)==int.MaxValue,"oversized pickaxe attack does not overflow");

 Check(ShopClassifier.CategoryOf(PermanentUpgradeType.HP)==0&&ShopClassifier.CategoryOf(PermanentUpgradeType.Defense)==0,"basic category HP and DEF");
 Check(ShopClassifier.CategoryOf(PermanentUpgradeType.Attack)==1&&ShopClassifier.CategoryOf(PermanentUpgradeType.PickaxeAttack)==1,"equipment category independent ATK upgrades");
 Check(ShopClassifier.CategoryOf(PermanentUpgradeType.Dash)==2&&ShopClassifier.CategoryOf(PermanentUpgradeType.Shield)==2&&ShopClassifier.CategoryOf(PermanentUpgradeType.Shockwave)==2,"skill category all three skills");
 PlayerPrefs.SetInt("PermanentUpgrade_PickaxeAttack_Level",2);currency.AddCurrency(CurrencyType.Dash,10);currency.SettleRunCurrency();Time.frameCount++;
 var row=new RowHarness();row.blockedPurchaseFrame=Time.frameCount;before=currency.DashCurrency;row.OnClickUpgrade();Check(currency.DashCurrency==before,"tab-change-frame click rejected");
 Time.frameCount++;row.gameObject.activeSelf=false;row.OnClickUpgrade();Check(currency.DashCurrency==before,"hidden row cannot purchase");row.gameObject.activeSelf=true;
 row.upgradeButton.interactable=false;row.OnClickUpgrade();Check(currency.DashCurrency==before,"disabled buy button cannot purchase");row.upgradeButton.interactable=true;
 row.OnClickUpgrade();row.OnClickUpgrade();Check(currency.DashCurrency==before-3&&row.refreshes==1&&row.punches==3,"real row callback buys once and refreshes after success");
 Time.frameCount++;shop.IsOpen=false;before=currency.DashCurrency;row.OnClickUpgrade();Check(currency.DashCurrency==before&&row.refreshes==1,"closed shop stale row callback cannot buy");

 shop.IsOpen=true;GameFlowController.Instance.CurrentState=GameState.Inventory;
 PlayerPrefs.SetInt("Currency_Dash",10000);PlayerPrefs.SetInt("Currency_Shockwave",10000);PlayerPrefs.SetInt("Currency_Shield",10000);
 currency=new CurrencyManager();typeof(CurrencyManager).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(currency,null);
 foreach(PermanentUpgradeType type in Enum.GetValues(typeof(PermanentUpgradeType))) {
  Time.frameCount++;int oldLevel=upgrades.GetLevel(type);int expectedCost=upgrades.GetUpgradeCost(type);var payment=upgrades.GetCurrencyType(type);
  int oldBalance=currency.GetCurrency(payment);var snapshot=new Dictionary<PermanentUpgradeType,int>();
  foreach(PermanentUpgradeType other in Enum.GetValues(typeof(PermanentUpgradeType)))snapshot[other]=upgrades.GetLevel(other);
  var offer=new RowHarness{upgradeType=type,currencyType=payment};offer.OnClickUpgrade();offer.OnClickUpgrade();
  Check(upgrades.GetLevel(type)==oldLevel+1&&currency.GetCurrency(payment)==oldBalance-expectedCost&&offer.refreshes==1,$"{type} purchase callback: one level and one charge");
  Check(snapshot.All(pair=>pair.Key==type||upgrades.GetLevel(pair.Key)==pair.Value),$"{type} callback: other six upgrade levels unchanged");
  Time.frameCount++;int costBefore=upgrades.GetUpgradeCost(type);
  Check(upgrades.GetLevel(type)==oldLevel+1&&costBefore==expectedCost+1,$"{type} saved level and next cost reload");
 }

 stats.PermanentMaxHP=250;stats.MaxHP=900;stats.BaseDefense=30;stats.Defense=110;
 Check(upgrades.GetPermanentValue(PermanentUpgradeType.HP)==250&&upgrades.GetNextPermanentValue(PermanentUpgradeType.HP)==300,"new shop HP preview excludes Run health");
 Check(upgrades.GetPermanentValue(PermanentUpgradeType.Defense)==30&&upgrades.GetNextPermanentValue(PermanentUpgradeType.Defense)==35,"new shop DEF preview excludes Shield and Run defense");
 var newShop=new ShopPanelController();Set(upgrades,"shopPanelUI",newShop);shop.IsOpen=true;
 Check(!upgrades.IsShopOpen,"legacy open UI cannot authorize purchases once new panel is assigned");
 newShop.IsOpen=true;Check(upgrades.IsShopOpen,"new fully open panel authorizes same purchase API");
 newShop.IsOpen=false;Time.frameCount++;before=currency.ShieldCurrency;Check(!upgrades.TryUpgrade(PermanentUpgradeType.Defense,CurrencyType.Shield)&&currency.ShieldCurrency==before,"new closing or hidden panel cannot charge");
Console.WriteLine($"{count} tests passed; drops {hits}/100000. Managed doubles; not Unity play.");
 }
}
''',encoding='utf-8')
result=subprocess.run(['dotnet','run','--project',str(folder/'Tests.csproj'),'--verbosity','quiet'],cwd=root,text=True,encoding='utf-8',errors='replace',capture_output=True)
print(result.stdout);print(result.stderr);raise SystemExit(result.returncode)

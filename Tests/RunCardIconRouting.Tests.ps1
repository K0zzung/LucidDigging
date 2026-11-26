# Tests the production icon routing methods; Unity rendering is not executed.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$ui = [IO.File]::ReadAllText((Join-Path $projectRoot 'Assets/Scripts/UI/RunCardSelectionUI.cs'))
$start = $ui.IndexOf('    private Sprite GetIcon(RunCardData card)')
$end = $ui.IndexOf('    private void BuildUI()', $start)
if ($start -lt 0 -or $end -le $start) { throw 'Icon routing methods not found.' }
$stub = @'
using System;
public class Sprite {}
public enum RunCardRarity {Common,Rare,Epic,Legendary,Nightmare}
public enum RunCardEffectType {Attack,Defense,MaxHealth,Heal,AttackAndDefense}
public class RunCardData {public RunCardRarity Rarity;public RunCardEffectType EffectType;}
public class IconRoutingCheck {
 Sprite attackIcon=new Sprite(),defenseIcon=new Sprite(),maxHealthIcon=new Sprite(),healIcon=new Sprite(),attackAndDefenseIcon=new Sprite();
 Sprite nightmareAttackIcon=new Sprite(),nightmareDefenseIcon=new Sprite(),nightmareMaxHealthIcon=new Sprite(),nightmareHealIcon=new Sprite(),nightmareAttackAndDefenseIcon=new Sprite();
'@
$tests = @'
 public static string Run(){var test=new IconRoutingCheck();Sprite[] normal={test.attackIcon,test.defenseIcon,test.maxHealthIcon,test.healIcon,test.attackAndDefenseIcon};Sprite[] nightmare={test.nightmareAttackIcon,test.nightmareDefenseIcon,test.nightmareMaxHealthIcon,test.nightmareHealIcon,test.nightmareAttackAndDefenseIcon};for(int r=0;r<5;r++)for(int e=0;e<5;e++){var result=test.GetIcon(new RunCardData{Rarity=(RunCardRarity)r,EffectType=(RunCardEffectType)e});if(!object.ReferenceEquals(result,r==4?nightmare[e]:normal[e]))throw new Exception("wrong rarity/effect icon");}return "PASS: production icon routing, 25 rarity/effect combinations. No Unity rendering performed.";}
}
'@
Add-Type -TypeDefinition ($stub + $ui.Substring($start,$end-$start) + $tests)
[IconRoutingCheck]::Run()

// Included by RunCardAssets.Tests.ps1 with managed Unity stand-ins and production sources.
public static class RunNightmareTests
{
    static System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    static void Check(bool condition, string message) { if (!condition) throw new System.Exception(message); }
    static void Put(object target, string name, object value) { target.GetType().GetField(name, flags).SetValue(target, value); }
    static void Call(object target, string name) { target.GetType().GetMethod(name, flags).Invoke(target, null); }
    static PlayerStatus Status() { var status = new PlayerStatus(); Call(status, "Awake"); return status; }
    static RunProgression Progression(int amount = 100) {
        if (RunProgression.Instance != null) Call(RunProgression.Instance, "OnDestroy");
        var xp = new RunProgression(); Call(xp, "Awake"); xp.AddExperience(amount); return xp;
    }
    static RunCardEffectHandler Handler(PlayerStatus status, RunProgression xp, GameFlowController flow) {
        var handler = new RunCardEffectHandler(); Put(handler, "playerStatus", status);
        Put(handler, "progression", xp); Put(handler, "gameFlow", flow); Call(handler, "Start"); return handler;
    }
    static RunCardRaritySettings Settings(string root) {
        string text = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets/Datas/RunCardRaritySettings.asset"));
        var matches = System.Text.RegularExpressions.Regex.Matches(text, @"- rarity: (\d+)\r?\n    enabled: (\d+)\r?\n    weight: (\d+)");
        var entries = new RunCardRaritySettings.Entry[5];
        foreach (System.Text.RegularExpressions.Match m in matches) {
            int i = int.Parse(m.Groups[1].Value);
            entries[i] = new RunCardRaritySettings.Entry { rarity = (RunCardRarity)i,
                enabled = m.Groups[2].Value == "1", weight = int.Parse(m.Groups[3].Value), frame = new Sprite() };
        }
        var result = new RunCardRaritySettings(); Put(result, "entries", entries); return result;
    }
    public static string Run(string root) {
        PlayerPrefs.Values.Clear(); PlayerPrefs.Writes = 0;
        var cards = new System.Collections.Generic.List<RunCardData>();
        foreach (string path in System.IO.Directory.GetFiles(System.IO.Path.Combine(root, "Assets/Datas"), "RunCard*.asset")) {
            string text = System.IO.File.ReadAllText(path);
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"(?m)^  id:")) cards.Add(RunCardAssetTests.Load(path));
        }
        cards.Sort((a,b) => string.CompareOrdinal(a.Id,b.Id));
        Check(cards.Count == 25, "25 total assets");
        var nightmare = cards.FindAll(c => c.Rarity == RunCardRarity.Nightmare);
        Check(nightmare.Count == 5, "5 Nightmare assets");
        var commonNames = new System.Collections.Generic.Dictionary<RunCardEffectType,string> {
            {RunCardEffectType.Attack,"꿈결의 칼날"}, {RunCardEffectType.Defense,"달빛의 가호"},
            {RunCardEffectType.MaxHealth,"꿈의 심장"}, {RunCardEffectType.Heal,"꿈의 이슬"},
            {RunCardEffectType.AttackAndDefense,"균형의 의지"} };
        foreach (var card in cards) {
            Check(!System.Text.RegularExpressions.Regex.IsMatch(card.DisplayName + card.Description, "[A-Za-z]"), "English card text: " + card.Id);
            if (card.Rarity == RunCardRarity.Common) Check(card.DisplayName == commonNames[card.EffectType], "common name");
            if (card.Rarity == RunCardRarity.Nightmare) {
                Check(card.HasValidEffect && card.HasPenalty && card.Id == card.BaseCardId + ".nightmare", "Nightmare identity/validation");
                Check(card.EffectDescription.Contains(card.PenaltyDescription) && !string.IsNullOrEmpty(card.PenaltyDescription), "missing penalty text");
            }
        }
        int[,] expected = { {12,0,0,0,0,0,20}, {0,7,0,0,3,0,0}, {0,0,100,0,3,0,0}, {0,0,0,120,0,0,20}, {7,3,0,0,0,0,30} };
        foreach (var card in nightmare) {
            int row = (int)card.EffectType;
            Check(card.AttackBonus==expected[row,0] && card.DefenseBonus==expected[row,1] &&
                card.MaxHealthBonus==expected[row,2] && card.HealAmount==expected[row,3] &&
                card.AttackPenalty==expected[row,4] && card.DefensePenalty==expected[row,5] &&
                card.MaxHealthPenalty==expected[row,6], "Nightmare asset values");
            var status=Status(); status.TakeDamage(30); var xp=Progression();
            var handler=Handler(status,xp,new GameFlowController {Status=status});
            Check(handler.TrySelectCard(card), "Nightmare application");
            Check(status.Attack==System.Math.Max(1,13+card.AttackBonus-card.AttackPenalty),"Nightmare attack");
            Check(status.Defense==System.Math.Max(0,5+card.DefenseBonus-card.DefensePenalty),"Nightmare defense");
            int max=System.Math.Max(1,100+card.MaxHealthBonus-card.MaxHealthPenalty);
            int hp=System.Math.Min(max,70+card.MaxHealthBonus); hp=System.Math.Min(max,hp+card.HealAmount);
            Check(status.MaxHP==max && status.CurrentHP==hp && !status.IsDead && xp.PendingLevelUps==2,"Nightmare HP/reward");
            if(card.MaxHealthPenalty>0) Check(card.PenaltyDescription.Contains("-"+card.MaxHealthPenalty),"max HP penalty amount");
            if(card.AttackPenalty>0) Check(card.PenaltyDescription.Contains("-"+card.AttackPenalty),"attack penalty amount");
            xp.ResetRun(); Check(status.RunAttackPenalty==0 && status.RunDefensePenalty==0 && status.RunMaxHPPenalty==0 &&
                status.Attack==13 && status.Defense==5 && status.MaxHP==100,"Nightmare reset");
        }
        var red=nightmare.Find(c=>c.EffectType==RunCardEffectType.Attack);
        var armor=nightmare.Find(c=>c.EffectType==RunCardEffectType.Defense);
        var life=nightmare.Find(c=>c.EffectType==RunCardEffectType.MaxHealth);
        var heal=nightmare.Find(c=>c.EffectType==RunCardEffectType.Heal);
        var player=Status(); var progression=Progression(100000); var flow=new GameFlowController{Status=player};
        var effect=Handler(player,progression,flow);
        for(int i=0;i<6;i++) Check(effect.TrySelectCard(red),"repeated max HP penalty");
        Check(player.RunAttackBonus==72 && player.RunMaxHPPenalty==120 && player.MaxHP==1 && player.CurrentHP==1 && !player.IsDead,"HP floor/safe selection");
        Check(effect.TrySelectCard(life) && player.MaxHP==80 && player.CurrentHP==80,"retain accumulated penalty after later health bonus");
        Check(effect.TrySelectCard(heal) && player.MaxHP==60 && player.CurrentHP==60,"reduce max before healing and cap");
        for(int i=0;i<40;i++) Check(effect.TrySelectCard(armor),"repeated attack penalties");
        Check(player.Attack==1,"attack floor");
        var defenseTest=new RunCardData(); Put(defenseTest,"rarity",RunCardRarity.Nightmare);
        Put(defenseTest,"attackBonus",1); Put(defenseTest,"defensePenalty",1000);
        Check(effect.TrySelectCard(defenseTest) && player.Defense==0,"defense floor");
        player.AddTemporaryDefense(50); Check(player.Defense==0,"shield remains additive before final floor"); player.RemoveTemporaryDefense(50);
        Check(PlayerPrefs.Writes==0,"Nightmare saved permanent data");
        player.SaveStats(); Check(PlayerPrefs.Values["Player_MaxHP"]==100 && PlayerPrefs.Values["Player_Attack"]==10 && PlayerPrefs.Values["Player_Defense"]==5,"save excludes penalties");
        progression.ResetRun(); Check(player.Attack==13 && player.Defense==5 && player.MaxHP==100,"all bonuses/penalties cleared");
        progression.AddExperience(100); flow.CurrentState=GameState.LevelUpSelection;
        Put(player,"<RunAttackBonus>k__BackingField",int.MaxValue);
        int pending=progression.PendingLevelUps; int oldHP=player.CurrentHP;
        Check(!effect.TrySelectCard(red) && progression.PendingLevelUps==pending && player.CurrentHP==oldHP &&
            player.RunAttackBonus==int.MaxValue && player.RunMaxHPPenalty==0,"overflow transaction rollback");
        player.ResetRunBonuses(); var invalid=new RunCardData(); Put(invalid,"attackBonus",2); Put(invalid,"maxHealthPenalty",20);
        Check(!effect.TrySelectCard(invalid) && progression.PendingLevelUps==pending,"penalty on ordinary card rejected without reward loss");
        Put(invalid,"rarity",RunCardRarity.Nightmare); Put(invalid,"attackPenalty",-1);
        Check(!effect.TrySelectCard(invalid) && progression.PendingLevelUps==pending,"negative penalty magnitude rejected");
        // Exercise the production UI selection method for three consecutive Nightmare choices.
        player=Status(); progression=Progression(); flow=new GameFlowController{Status=player}; effect=Handler(player,progression,flow);
        var ui=new SelectionHarness{progression=progression,gameFlow=flow,selectionHandler=effect,offeredCards=new[]{red}};
        int confirmed=0; ui.OnCardSelected+=c=>confirmed++;
        for(int i=0;i<3;i++){int version=ui.offerVersion;Time.frameCount++;ui.Click(version);ui.Click(version);}
        Check(confirmed==3 && player.RunAttackBonus==36 && player.RunMaxHPPenalty==60 && player.MaxHP==40 &&
            player.CurrentHP==40 && progression.PendingLevelUps==0 && !ui.IsOpen && flow.CurrentState==GameState.Playing,"three UI commits / duplicate click protection");
        player.TakeDamage(10000); Check(player.IsDead && player.CurrentHP==0 &&
            player.RunAttackBonus==0 && player.RunMaxHPPenalty==0 && !effect.TrySelectCard(red),"death clears run stats and rejects card");
        var retry=Status(); Check(retry.RunMaxHPPenalty==0 && retry.RunAttackPenalty==0 && retry.Attack==13 && retry.MaxHP==100,"new scene defaults");
        var settings=Settings(root); Check(!settings.GetEntry(RunCardRarity.Nightmare).enabled && settings.GetEntry(RunCardRarity.Nightmare).weight==1,"default remains opt-in");
        // Reproduce the reported configuration: old 20-card pool with only Nightmare enabled.
        var onlyNightmare=Settings(root);
        for(int i=0;i<5;i++) onlyNightmare.GetEntry((RunCardRarity)i).enabled=i==4;
        var oldPool=cards.FindAll(c=>c.Rarity!=RunCardRarity.Nightmare);
        RunCardData[] diagnosticOffer; string diagnosticError;
        Check(!RunCardSampler.TryDrawThree(oldPool,onlyNightmare,out diagnosticOffer,out diagnosticError,new System.Random(1)) &&
            diagnosticError.Contains("Pool=20, eligible=0, families=0") &&
            diagnosticError.Contains("disabledOrZeroWeight=20"),"old pool / Nightmare-only failure diagnosis");
        Check(RunCardSampler.TryDrawThree(cards,onlyNightmare,out diagnosticOffer,out diagnosticError,new System.Random(1)),"repaired Nightmare-only pool");
        foreach(var c in diagnosticOffer) Check(c.Rarity==RunCardRarity.Nightmare,"Nightmare-only repair");
        var rng=new System.Random(456); const int samples=100000;
        for(int i=0;i<samples;i++){RunCardData[] offer;string error;Check(RunCardSampler.TryDrawThree(cards,settings,out offer,out error,rng),error);foreach(var c in offer)Check(c.Rarity!=RunCardRarity.Nightmare,"disabled Nightmare appeared");}
        settings.GetEntry(RunCardRarity.Nightmare).enabled=true; var first=new int[5]; var seen=new System.Collections.Generic.HashSet<string>();
        rng=new System.Random(20261127);
        for(int i=0;i<samples;i++){RunCardData[] offer;string error;Check(RunCardSampler.TryDrawThree(cards,settings,out offer,out error,rng),error);first[(int)offer[0].Rarity]++;var families=new System.Collections.Generic.HashSet<string>();foreach(var c in offer){Check(families.Add(c.BaseCardId),"duplicate family with Nightmare");if(c.Rarity==RunCardRarity.Nightmare)seen.Add(c.Id);}}
        Check(seen.Count==5,"all Nightmare cards reachable"); int[] weights={64,25,8,2,1};
        for(int i=0;i<5;i++) Check(System.Math.Abs(first[i]/(double)samples-weights[i]/100.0)<.006,"enabled rarity distribution");
        return "PASS: 25 Korean assets; five Nightmare values/effects; stat floors; HP clamp/heal order; repeated penalties; checked atomic rejection; no permanent save pollution; reset/death; three production UI commits. Disabled 100000 offers: Nightmare=0. In-memory enabled 100000 offers first slots: "+string.Join(",",first)+" (Common/Rare/Epic/Legendary/Nightmare). Production Nightmare stays disabled. Unity not executed.";
    }
}

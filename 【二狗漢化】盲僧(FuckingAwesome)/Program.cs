/*
___________             __   .__                    _____                                              .____                     _________.__        
\_   _____/_ __   ____ |  | _|__| ____    ____     /  _  \__  _  __ ____   __________   _____   ____   |    |    ____   ____    /   _____/|__| ____  
 |    __)|  |  \_/ ___\|  |/ /  |/    \  / ___\   /  /_\  \ \/ \/ // __ \ /  ___/  _ \ /     \_/ __ \  |    |  _/ __ \_/ __ \   \_____  \ |  |/    \ 
 |     \ |  |  /\  \___|    <|  |   |  \/ /_/  > /    |    \     /\  ___/ \___ (  <_> )  Y Y  \  ___/  |    |__\  ___/\  ___/   /        \|  |   |  \
 \___  / |____/  \___  >__|_ \__|___|  /\___  /  \____|__  /\/\_/  \___  >____  >____/|__|_|  /\___  > |_______ \___  >\___  > /_______  /|__|___|  /
     \/              \/     \/       \//_____/           \/            \/     \/            \/     \/          \/   \/     \/          \/         \/ 
*/
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Linq;

namespace FuckingAwesomeLeeSin
{
    class Program
    {
        public static string ChampName = "LeeSin";
        public static Orbwalking.Orbwalker Orbwalker;
        private static Obj_AI_Hero Player = ObjectManager.Player; // Instead of typing ObjectManager.Player you can just type Player
        public static Spell Q,W, E, R;
        public static Spellbook SBook;
        public static Items.Item Dfg;
        public static Vector2 JumpPos;
        public static Vector3 mouse = Game.CursorPos;
        public static SpellSlot smiteSlot;
        public static SpellSlot flashSlot;
        public static Menu Menu;
        public static bool CastQAgain;
        public static bool CastWardAgain = true;
        public static bool reCheckWard = true;
        public static bool wardJumped = false;
        public static Obj_AI_Base minionerimo;
        public static bool checkSmite = false;
        public static bool delayW = false;
        public static Vector2 insecLinePos;
        public static float TimeOffset;
        public static Vector3 lastWardPos;
        public static float lastPlaced;

        private static readonly string[] epics =
        {
            "SRU_BaronSpawn", "SRU_Dragon"
        };
        private static readonly string[] buffs =
        {
            "SRU_Red", "SRU_Blue"
        };
        private static readonly string[] buffandepics =
        {
            "SRU_Red", "SRU_Blue", "SRU_Dragon", "SRU_BaronSpawn"
        };

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        public static SpellSlot IgniteSlot;


        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampName) return;
            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            smiteSlot = Player.GetSpellSlot("SummonerSmite");
            flashSlot = Player.GetSpellSlot("summonerflash");

            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 430);
            R = new Spell(SpellSlot.R, 375);
            Q.SetSkillshot(Q.Instance.SData.SpellCastTime, Q.Instance.SData.LineWidth, Q.Instance.SData.MissileSpeed,true,SkillshotType.SkillshotLine);
            //Base menu
            Menu = new Menu("FA盲僧", "FALeeSin", true);
            //Orbwalker and menu
            Menu.AddSubMenu(new Menu("走砍", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));
            //Target selector and menu
            var ts = new Menu("目標 選擇", "Target Selector");
            SimpleTs.AddToMenu(ts);
            Menu.AddSubMenu(ts);
            //Combo menu
            Menu.AddSubMenu(new Menu("連招", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useQ", "使用 Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useQ2", "使用 Q2").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useW", "連招使用瞬眼").SetValue(false));
            Menu.SubMenu("Combo").AddItem(new MenuItem("dsjk", "使用順眼|如果範圍: "));
            Menu.SubMenu("Combo").AddItem(new MenuItem("wMode", "> AA範圍|| > Q範圍").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useE", "使用 E").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useR", "使用 R").SetValue(false));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ksR", "使用 R 搶人頭").SetValue(false));
            Menu.SubMenu("Combo").AddItem(new MenuItem("starCombo", "開始 連招").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("random2ejwej", "W->Q->R->Q2"));

            var harassMenu = new Menu("騷擾", "Harass");
            harassMenu.AddItem(new MenuItem("q1H", "使用 Q1").SetValue(true));
            harassMenu.AddItem(new MenuItem("q2H", "使用 Q2").SetValue(true));
            harassMenu.AddItem(new MenuItem("wH", "順眼|W逃跑{受傷}").SetValue(false));
            harassMenu.AddItem(new MenuItem("eH", "使用 E1").SetValue(true));
            Menu.AddSubMenu(harassMenu);

            //Jung/Wave Clear
            var waveclearMenu = new Menu("清線|清野", "wjClear");
            waveclearMenu.AddItem(new MenuItem("useQClear", "使用 Q").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("useWClear", "使用 W").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("useEClear", "使用 E").SetValue(true));
            Menu.AddSubMenu(waveclearMenu);

            //InsecMenu
            var insecMenu = new Menu("R 設置", "Insec");
            insecMenu.AddItem(new MenuItem("InsecEnabled", "使用迴旋踢").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Press)));
            insecMenu.AddItem(new MenuItem("rnshsasdhjk", "R 模式:"));
            insecMenu.AddItem(new MenuItem("insecMode", "左鍵點擊 [on] TS [off]").SetValue(true));
            insecMenu.AddItem(new MenuItem("insecOrbwalk", "使用 走砍").SetValue(true));
            insecMenu.AddItem(new MenuItem("flashInsec", "閃現 R").SetValue(false));
            insecMenu.AddItem(new MenuItem("waitForQBuff", "等待Q技能CD").SetValue(false));
            insecMenu.AddItem(new MenuItem("22222222222222", "(更多更快傷害)"));
            insecMenu.AddItem(new MenuItem("insec2champs", "R向隊友").SetValue(true));
            insecMenu.AddItem(new MenuItem("bonusRangeA", "R向隊友範圍").SetValue(new Slider(0, 0, 1000)));
            insecMenu.AddItem(new MenuItem("insec2tower", "R向炮塔").SetValue(true));
            insecMenu.AddItem(new MenuItem("bonusRangeT", "R向炮塔範圍").SetValue(new Slider(0, 0, 1000)));
            insecMenu.AddItem(new MenuItem("insec2orig", "R向原始位置").SetValue(true));
            insecMenu.AddItem(new MenuItem("22222222222", "------------"));
            insecMenu.AddItem(new MenuItem("instaFlashInsec1", "手動使用R"));
            insecMenu.AddItem(new MenuItem("instaFlashInsec2", "使用閃現迴旋踢"));
            insecMenu.AddItem(new MenuItem("instaFlashInsec", "啟用").SetValue(new KeyBind("P".ToCharArray()[0], KeyBindType.Toggle)));
            Menu.AddSubMenu(insecMenu);

            var autoSmiteSettings = new Menu("懲戒 設置", "Auto Smite Settings");
            autoSmiteSettings.AddItem(new MenuItem("smiteEnabled", "啟用").SetValue(new KeyBind("M".ToCharArray()[0], KeyBindType.Toggle)));
            autoSmiteSettings.AddItem(new MenuItem("qqSmite", "Q->懲戒->Q").SetValue(true));
            autoSmiteSettings.AddItem(new MenuItem("normSmite", "正常 懲戒").SetValue(true));
            autoSmiteSettings.AddItem(new MenuItem("drawSmite", "顯示 懲戒 範圍").SetValue(true));
            Menu.AddSubMenu(autoSmiteSettings);

            //SaveMe Menu
            var SaveMeMenu = new Menu("懲戒 節省 設置", "Smite Save Settings");
            SaveMeMenu.AddItem(new MenuItem("smiteSave", "啟用 節省懲戒").SetValue(true));
            SaveMeMenu.AddItem(new MenuItem("hpPercentSM", "WW懲戒 on x%").SetValue(new Slider(10, 1)));
            SaveMeMenu.AddItem(new MenuItem("param1", "禁用懲戒|血量設定")); // TBC
            SaveMeMenu.AddItem(new MenuItem("dBuffs", "紅藍Buffs").SetValue(true));// TBC
            SaveMeMenu.AddItem(new MenuItem("hpBuffs", "禁用懲戒|血量HP %").SetValue(new Slider(30, 1)));// TBC
            SaveMeMenu.AddItem(new MenuItem("dEpics", "野怪").SetValue(true));// TBC
            SaveMeMenu.AddItem(new MenuItem("hpEpics", "禁用懲戒|血量HP %").SetValue(new Slider(10, 1)));// TBC
            Menu.AddSubMenu(SaveMeMenu);
            //Wardjump menu
            var wardjumpMenu = new Menu("瞬眼", "Wardjump");
            wardjumpMenu.AddItem(
                new MenuItem("wjump", "瞬眼 鍵位").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            wardjumpMenu.AddItem(new MenuItem("maxRange", "總是瞬眼極限範圍").SetValue(false));
            wardjumpMenu.AddItem(new MenuItem("castInRange", "向鼠標位置瞬眼").SetValue(false));
            wardjumpMenu.AddItem(new MenuItem("m2m", "移動到鼠標位置再瞬眼").SetValue(true));
            wardjumpMenu.AddItem(new MenuItem("j2m", "瞬眼到小兵").SetValue(true));
            wardjumpMenu.AddItem(new MenuItem("j2c", "瞬眼到敵人").SetValue(true));
            Menu.AddSubMenu(wardjumpMenu);

            var drawMenu = new Menu("範圍", "Drawing");
            drawMenu.AddItem(new MenuItem("DrawEnabled", "啟用 範圍").SetValue(false));
            drawMenu.AddItem(new MenuItem("WJDraw", "顯示 瞬眼 範圍").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawQ", "顯示 Q 範圍").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawW", "顯示 W 範圍").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawE", "顯示 E 範圍").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawR", "顯示 R 範圍").SetValue(true));
            Menu.AddSubMenu(drawMenu);

            //Exploits
            var miscMenu = new Menu("雜項", "Misc");
            miscMenu.AddItem(new MenuItem("NFE", "使用 封包").SetValue(true));
            miscMenu.AddItem(new MenuItem("QHC", "Q 命中率").SetValue(new StringList(new []{"低", "中", "高"}, 1)));
            miscMenu.AddItem(new MenuItem("IGNks", "使用 點燃").SetValue(true));
            miscMenu.AddItem(new MenuItem("qSmite", "懲戒 Q!").SetValue(true));
            Menu.AddSubMenu(miscMenu);
            //Make the menu visible
            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw; // Add onDraw
            Game.OnGameUpdate += Game_OnGameUpdate; // adds OnGameUpdate (Same as onTick in bol)
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;

            PrintMessage("Loaded!");
        }

        public static double SmiteDmg()
        {
            int[] dmg =
            {
                20*Player.Level + 370, 30*Player.Level + 330, 40*+Player.Level + 240, 50*Player.Level + 100
            };
            return Player.SummonerSpellbook.CanUseSpell(smiteSlot) == SpellState.Ready ? dmg.Max() : 0;
        }

        public static void Harass()
        {
            var target = SimpleTs.GetTarget(Q.Range + 200, SimpleTs.DamageType.Physical);
            var q = paramBool("q1H");
            var q2 = paramBool("q2H");
            var e = paramBool("eH");

            if (q && Q.IsReady() && Q.Instance.Name == "BlindMonkQOne" && target.IsValidTarget(Q.Range)) CastQ1(target);
            if (q2 && Q.IsReady() &&
                (target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)))
            {
                if(CastQAgain || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player))) Q.Cast();
            }
            if (e && E.IsReady() && target.IsValidTarget(E.Range) && E.Instance.Name == "BlindMonkEOne") E.Cast();

        }


        public static bool isNullInsecPos = true;
        public static Vector3 insecPos;

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name == "BlindMonkQOne")
            {
                CastQAgain = false;
                Utility.DelayAction.Add(2900, () =>
                {
                    CastQAgain = true;
                });
            }
            if (Menu.Item("instaFlashInsec").GetValue<KeyBind>().Active && args.SData.Name == "BlindMonkRKick")
            {
                Player.SummonerSpellbook.CastSpell(flashSlot, getInsecPos((Obj_AI_Hero) (args.Target)));
            }
            if (args.SData.Name == "summonerflash" && InsecComboStep != InsecComboStepSelect.NONE)
            {
                Obj_AI_Hero target = paramBool("insecMode")
                   ? SimpleTs.GetSelectedTarget()
                   : SimpleTs.GetTarget(Q.Range + 200, SimpleTs.DamageType.Physical);
                InsecComboStep = InsecComboStepSelect.PRESSR;
                Utility.DelayAction.Add(80, () => R.CastOnUnit(target, true));
            }
            if (args.SData.Name == "BlindMonkRKick")
                InsecComboStep = InsecComboStepSelect.NONE;
            //if (args.SData.Name == "blindmonkqtwo" && HarassSelect != HarassStatEnum.NONE)
            //    HarassSelect = HarassStatEnum.WJ;
            if (args.SData.Name == "BlindMonkWOne" && InsecComboStep == InsecComboStepSelect.NONE)
            {
                Obj_AI_Hero target = paramBool("insecMode")
                    ? SimpleTs.GetSelectedTarget()
                    : SimpleTs.GetTarget(Q.Range + 200, SimpleTs.DamageType.Physical);
                InsecComboStep = InsecComboStepSelect.PRESSR;
                Utility.DelayAction.Add(100, () => R.CastOnUnit(target, true));
            }
        }
        public static Vector3 getInsecPos(Obj_AI_Hero target)
        {
            if (isNullInsecPos)
            {
                isNullInsecPos = false;
                insecPos = Player.Position;
            }
            var turrets = (from tower in ObjectManager.Get<Obj_Turret>()
                           where tower.IsAlly && !tower.IsDead && target.Distance(tower.Position) < 1500 + Menu.Item("bonusRangeT").GetValue<Slider>().Value && tower.Health > 0
                select tower).ToList();
            if (GetAllyHeroes(target, 2000 + Menu.Item("bonusRangeA").GetValue<Slider>().Value).Count > 0 && paramBool("insec2champs"))
            {
                Vector3 insecPosition = InterceptionPoint(GetAllyInsec(GetAllyHeroes(target, 2000 + Menu.Item("bonusRangeA").GetValue<Slider>().Value)));
                insecLinePos = Drawing.WorldToScreen(insecPosition);
                return V2E(insecPosition, target.Position, target.Distance(insecPosition) + 200).To3D();

            } 
            if(turrets.Any() && paramBool("insec2tower"))
            {
                insecLinePos = Drawing.WorldToScreen(turrets[0].Position);
                return V2E(turrets[0].Position, target.Position, target.Distance(turrets[0].Position) + 200).To3D();
            }
            if (paramBool("insec2orig"))
            {
                insecLinePos = Drawing.WorldToScreen(insecPos);
                return V2E(insecPos, target.Position, target.Distance(insecPos) + 200).To3D();
            }
            return new Vector3();
        }
        enum InsecComboStepSelect { NONE, QGAPCLOSE, WGAPCLOSE, PRESSR };
        static InsecComboStepSelect InsecComboStep;
        static void InsecCombo(Obj_AI_Hero target)
        {
            if (target != null && target.IsVisible)
            {
                if (Player.Distance(getInsecPos(target)) < 200)
                {
                    R.CastOnUnit(target, true);
                    InsecComboStep = InsecComboStepSelect.PRESSR;
                }
                else if (InsecComboStep == InsecComboStepSelect.NONE &&
                         getInsecPos(target).Distance(Player.Position) < 600)
                    InsecComboStep = InsecComboStepSelect.WGAPCLOSE;
                else if (InsecComboStep == InsecComboStepSelect.NONE && target.Distance(Player) < Q.Range)
                    InsecComboStep = InsecComboStepSelect.QGAPCLOSE;

                switch (InsecComboStep)
                {
                    case InsecComboStepSelect.QGAPCLOSE:
                        if (!(target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)) &&
                            Q.Instance.Name == "BlindMonkQOne")
                        {
                            CastQ1(target);
                        }
                        else if ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)))
                        {
                            Q.Cast();
                            InsecComboStep = InsecComboStepSelect.WGAPCLOSE;
                        }
                        break;
                    case InsecComboStepSelect.WGAPCLOSE:
                        if (W.IsReady() && W.Instance.Name == "BlindMonkWOne" &&
                            (paramBool("waitForQBuff")
                                ? !(target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true))
                                : true))
                        {
                            WardJump(getInsecPos(target), false, false, true);
                            wardJumped = true;
                        }
                        else if (Player.SummonerSpellbook.CanUseSpell(flashSlot) == SpellState.Ready &&
                                 paramBool("flashInsec") && !wardJumped && Player.Distance(insecPos) < 400 ||
                                 Player.SummonerSpellbook.CanUseSpell(flashSlot) == SpellState.Ready &&
                                 paramBool("flashInsec") && !wardJumped && Player.Distance(insecPos) < 400 &&
                                 FindBestWardItem() == null)
                        {
                            Player.SummonerSpellbook.CastSpell(flashSlot, getInsecPos(target));
                            Utility.DelayAction.Add(50, () => R.CastOnUnit(target, true));
                        }
                        break;
                    case InsecComboStepSelect.PRESSR:
                        R.CastOnUnit(target, true);
                        break;
                }
            }
        }
        static Vector3 InterceptionPoint(List<Obj_AI_Hero> heroes)
        {
            Vector3 result = new Vector3();
            foreach (Obj_AI_Hero hero in heroes)
            result += hero.Position;
            result.X /= heroes.Count;
            result.Y /= heroes.Count;
            return result;
        }
        static List<Obj_AI_Hero> GetAllyInsec(List<Obj_AI_Hero> heroes)
        {
            byte alliesAround = 0;
            Obj_AI_Hero tempObject = new Obj_AI_Hero();
            foreach (Obj_AI_Hero hero in heroes)
            {
                int localTemp = GetAllyHeroes(hero, 500 + Menu.Item("bonusRangeA").GetValue<Slider>().Value).Count;
                if (localTemp > alliesAround)
                {
                    tempObject = hero;
                    alliesAround = (byte)localTemp;
                }
            }
            return GetAllyHeroes(tempObject, 500 + Menu.Item("bonusRangeA").GetValue<Slider>().Value);
        }
        private static List<Obj_AI_Hero> GetAllyHeroes(Obj_AI_Hero position, int range)
        {
            List<Obj_AI_Hero> temp = new List<Obj_AI_Hero>();
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                if (hero.IsAlly && !hero.IsMe && hero.Distance(position) < range)
                    temp.Add(hero);
            return temp;
        }

        static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }
        public static void SaveMe()
        {
            if ((Player.Health / Player.MaxHealth * 100) > Menu.Item("hpPercentSM").GetValue<Slider>().Value || Player.SummonerSpellbook.CanUseSpell(smiteSlot) != SpellState.Ready) return;
            var epicSafe = false;
            var buffSafe = false;
            foreach (
                var minion in
                    MinionManager.GetMinions(Player.Position, 1000f, MinionTypes.All, MinionTeam.Neutral,
                        MinionOrderTypes.None))
            {
                foreach (var minionName in epics)
                {
                    if (minion.BaseSkinName == minionName && hpLowerParam(minion, "hpEpics") && paramBool("dEpics"))
                    {
                        epicSafe = true;
                        break;
                    }
                }
                foreach (var minionName in buffs)
                {
                    if (minion.BaseSkinName == minionName && hpLowerParam(minion, "hpBuffs") && paramBool("dBuffs"))
                    {
                        buffSafe = true;
                        break;
                    }
                }
            }

            if(epicSafe || buffSafe) return;

            foreach (var minion in MinionManager.GetMinions(Player.Position, 700f, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth))
            {
                if (!W.IsReady() && !Player.HasBuff("BlindMonkIronWill") || smiteSlot == SpellSlot.Unknown ||
                    smiteSlot != SpellSlot.Unknown &&
                    Player.SummonerSpellbook.CanUseSpell(smiteSlot) != SpellState.Ready) break;
                if (minion.Name.ToLower().Contains("ward")) return;
                if (W.Instance.Name != "blindmonkwtwo")
                {
                    W.Cast();
                    W.Cast();
                }
                if (Player.HasBuff("BlindMonkIronWill"))
                {
                    Player.SummonerSpellbook.CastSpell(smiteSlot, minion);
                }
            }
        }
        static void Game_OnGameUpdate(EventArgs args)
        {
            if(Player.IsDead) return;
            smiteSlot = Player.GetSpellSlot(smitetype());
            if ((paramBool("insecMode")
                ? SimpleTs.GetSelectedTarget()
                : SimpleTs.GetTarget(Q.Range + 200, SimpleTs.DamageType.Physical)) == null)
            {
                InsecComboStep = InsecComboStepSelect.NONE;
            }
            if (Menu.Item("smiteEnabled").GetValue<KeyBind>().Active) smiter();
            if (Menu.Item("starCombo").GetValue<KeyBind>().Active) wardCombo();
            if (paramBool("smiteSave")) SaveMe();

            if (paramBool("IGNks"))
            {
                Obj_AI_Hero NewTarget = SimpleTs.GetTarget(600, SimpleTs.DamageType.True);

                if (NewTarget != null && IgniteSlot != SpellSlot.Unknown
                    && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready
                    && ObjectManager.Player.GetSummonerSpellDamage(NewTarget, Damage.SummonerSpell.Ignite) > NewTarget.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, NewTarget);
                }
            }
            if (Menu.Item("InsecEnabled").GetValue<KeyBind>().Active)
            {
                if (paramBool("insecOrbwalk"))
                {
                    Orbwalk(Game.CursorPos);
                }
                Obj_AI_Hero newTarget = paramBool("insecMode")
                    ? SimpleTs.GetSelectedTarget()
                    : SimpleTs.GetTarget(Q.Range + 200, SimpleTs.DamageType.Physical);
                
                 if(newTarget != null) InsecCombo(newTarget);
            }
            else
            {
                isNullInsecPos = true;
                wardJumped = false;
            }
            if(Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo) InsecComboStep = InsecComboStepSelect.NONE;
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    StarCombo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    AllClear();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;

            }
            if(Menu.Item("wjump").GetValue<KeyBind>().Active)
                wardjumpToMouse();
        }
        static void Drawing_OnDraw(EventArgs args)
        {
            if (!paramBool("DrawEnabled")) return;
            Obj_AI_Hero newTarget = paramBool("insecMode")
                   ? SimpleTs.GetSelectedTarget()
                   : SimpleTs.GetTarget(Q.Range + 200, SimpleTs.DamageType.Physical);
            if (Menu.Item("instaFlashInsec").GetValue<KeyBind>().Active) Drawing.DrawText(960, 340, System.Drawing.Color.Red, "FLASH INSEC ENABLED");
            if (newTarget != null && newTarget.IsVisible && Player.Distance(newTarget) < 3000)
            {
                Vector2 targetPos = Drawing.WorldToScreen(newTarget.Position);
                Drawing.DrawLine(insecLinePos.X, insecLinePos.Y, targetPos.X, targetPos.Y, 3, System.Drawing.Color.White);
                Utility.DrawCircle(getInsecPos(newTarget), 100, System.Drawing.Color.White);
            }
            if (Menu.Item("smiteEnabled").GetValue<KeyBind>().Active && paramBool("drawSmite"))
            {
                Utility.DrawCircle(Player.Position, 700, System.Drawing.Color.White);
            }
            if (Menu.Item("wjump").GetValue<KeyBind>().Active && paramBool("WJDraw"))
            {   
                Utility.DrawCircle(JumpPos.To3D(), 20, System.Drawing.Color.Red);
                Utility.DrawCircle(Player.Position, 600, System.Drawing.Color.Red);
            }
            if (paramBool("drawQ")) Utility.DrawCircle(Player.Position, Q.Range - 80, Q.IsReady() ? System.Drawing.Color.LightSkyBlue :System.Drawing.Color.Tomato);
            if (paramBool("drawW")) Utility.DrawCircle(Player.Position, W.Range - 80, W.IsReady() ? System.Drawing.Color.LightSkyBlue :System.Drawing.Color.Tomato);
            if (paramBool("drawE")) Utility.DrawCircle(Player.Position, E.Range - 80, E.IsReady() ? System.Drawing.Color.LightSkyBlue :System.Drawing.Color.Tomato);
            if (paramBool("drawR")) Utility.DrawCircle(Player.Position, R.Range - 80, R.IsReady() ? System.Drawing.Color.LightSkyBlue :System.Drawing.Color.Tomato);

        }
        public static float Q2Damage(Obj_AI_Base target, float subHP = 0, bool monster = false)
        {
            var damage = (50 + (Q.Level*30)) + (0.09 * Player.FlatPhysicalDamageMod) + ((target.MaxHealth - (target.Health - subHP))*0.08);
            if (monster && damage > 400) return (float) Damage.CalcDamage(Player, target, Damage.DamageType.Physical, 400);
            return (float) Damage.CalcDamage(Player, target, Damage.DamageType.Physical, damage);
        }
        public static void wardjumpToMouse()
        {
            WardJump(Game.CursorPos, paramBool("m2m"), paramBool("maxRange"), paramBool("castInRange"), paramBool("j2m"), paramBool("j2c"));
        }
        public static void PrintMessage(string msg) // Credits to ChewyMoon, and his Brain.exe
        {
            Game.PrintChat("<font color=\"#6699ff\"><b>FALeeSin:</b></font> <font color=\"#FFFFFF\">" + msg + "</font>");
        }
        public static void Orbwalk(Vector3 pos, Obj_AI_Hero target = null)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, pos);
        }
        private static SpellDataInst GetItemSpell(InventorySlot invSlot)
        {
            return Player.Spellbook.Spells.FirstOrDefault(spell => (int)spell.Slot == invSlot.Slot + 4);
        }
        public static bool packets()
        {
            return Menu.Item("NFE").GetValue<bool>();
        }
        public static void smiter()
        {
            var minion =
                MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly,
                    MinionOrderTypes.MaxHealth).FirstOrDefault();
            if (minion != null)
            {
                foreach (var name in buffandepics)
                {
                    if (minion.BaseSkinName == name)
                    {
                        minionerimo = minion;
                        if (SmiteDmg() > minion.Health && minion.IsValidTarget(780) && paramBool("normSmite")) Player.SummonerSpellbook.CastSpell(smiteSlot, minion);
                        if (minion.Distance(Player) < 100 && checkSmite)
                        {
                            checkSmite = false;
                            Player.SummonerSpellbook.CastSpell(smiteSlot, minion);
                        }
                        if (!Q.IsReady() || !paramBool("qqSmite")) return;

                        if (Q2Damage(minion, ((float) SmiteDmg() + Q.GetDamage(minion)), true) + SmiteDmg() >
                            minion.Health &&
                            !(minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)))
                        {
                            Q.Cast(minion, true);
                        }
                        if ((Q2Damage(minion, (float) SmiteDmg(), true) + SmiteDmg()) > minion.Health &&
                            (minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)))
                        {
                            Q.CastOnUnit(Player, true);
                            checkSmite = true;
                        }
                        if ((minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)) &&
                            CastQAgain ||
                            (minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)) &&
                            Q2Damage(minion, 0, true) > minion.Health)
                        {
                            Q.CastOnUnit(Player, true);
                        }
                    }
                }
            }
        }

        //Start Credits to Kurisu
        public static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        public static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        public static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        public static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        public static string smitetype()
        {
            if (SmiteBlue.Any(Items.HasItem))
            {
                return "s5_summonersmiteplayerganker";
            }
            if (SmiteRed.Any(Items.HasItem))
            {
                return "s5_summonersmiteduel";
            }
            if (SmiteGrey.Any(Items.HasItem))
            {
                return "s5_summonersmitequick";
            }
            if (SmitePurple.Any(Items.HasItem))
            {
                return "itemsmiteaoe";
            }
            return "summonersmite";
        }
        //End credits

        public static void useItems(Obj_AI_Hero enemy)
        {
            if (Items.CanUseItem(3142) && Player.Distance(enemy) <= 600)
                Items.UseItem(3142);
            if (Items.CanUseItem(3144) && Player.Distance(enemy) <= 450)
                Items.UseItem(3144, enemy);
            if (Items.CanUseItem(3153) && Player.Distance(enemy) <= 450)
                Items.UseItem(3153, enemy);
            if (Items.CanUseItem(3077) && Utility.CountEnemysInRange(350) >= 1)
                Items.UseItem(3077);
            if (Items.CanUseItem(3074) && Utility.CountEnemysInRange(350) >= 1)
                Items.UseItem(3074);
            if(Items.CanUseItem(3143) && Utility.CountEnemysInRange(450) >= 1)
                Items.UseItem(3143);
        }
        public static void useClearItems(Obj_AI_Base enemy)
        {

            if (Items.CanUseItem(3077) && Player.Distance(enemy) < 350)
                Items.UseItem(3077);
            if (Items.CanUseItem(3074) && Player.Distance(enemy) < 350)
                Items.UseItem(3074);
        }
        public static void AllClear()
        {
            var passiveIsActive = Player.HasBuff("blindmonkpassive_cosmetic", true);
            bool isJung = false;
            var minion =
                MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth).FirstOrDefault();
            if (minion == null) minion = MinionManager.GetMinions(Player.ServerPosition, Q.Range).FirstOrDefault();
            else isJung = true;
                useClearItems(minion);
            if (isJung)
            {
                foreach (var name in buffandepics)
                {
                    if (minion != null && minion.Name.ToLower().Contains(name.ToLower()))
                    {
                        if (minion.Health < SmiteDmg() + 300) return;
                    }
                }
            }
            if (minion == null || minion.Name.ToLower().Contains("ward")) return;
                if (Menu.Item("useQClear").GetValue<bool>() && Q.IsReady())
                {
                    if (Q.Instance.Name == "BlindMonkQOne")
                    {
                        if (!passiveIsActive)
                        {
                            Q.Cast(minion, true);
                        }
                    }
                    else if ((minion.HasBuff("BlindMonkQOne", true) ||
                             minion.HasBuff("blindmonkqonechaos", true)) && (!passiveIsActive || Q.IsKillable(minion, 1)) ||
                             Player.Distance(minion) > 500) Q.Cast();
                }
                if (paramBool("useWClear") && isJung && Player.Distance(minion) < Orbwalking.GetRealAutoAttackRange(Player))
                {
                    if (W.Instance.Name == "BlindMonkWOne" && !delayW)
                    {
                        if (!passiveIsActive)
                        {
                            W.CastOnUnit(Player);
                            delayW = true;
                            Utility.DelayAction.Add(300, () => delayW = false);
                        }

                    }
                    else if (W.Instance.Name != "BlindMonkWOne" && (!passiveIsActive))
                    {
                        W.CastOnUnit(Player);
                    }
                }
                if (Menu.Item("useEClear").GetValue<bool>() && E.IsReady())
                {
                    if (E.Instance.Name == "BlindMonkEOne" && minion.IsValidTarget(E.Range) && !delayW)
                    {
                        if (!passiveIsActive)
                        {
                            E.Cast();
                            delayW = true;
                            Utility.DelayAction.Add(300, () => delayW = false);
                        }
                    }
                    else if (minion.HasBuff("BlindMonkEOne", true) && (!passiveIsActive || Player.Distance(minion) > 450))
                    {
                        E.Cast();
                    }
                }
        }
        private static void WardJump(Vector3 pos, bool m2m = true, bool maxRange = false, bool reqinMaxRange = false, bool minions = true, bool champions = true)
        {
            var basePos = Player.Position.To2D();
            var newPos = (pos.To2D() - Player.Position.To2D());

            if (JumpPos == new Vector2())
            {
                if (reqinMaxRange) JumpPos = pos.To2D();
                else if (maxRange || Player.Distance(pos) > 590) JumpPos = basePos + (newPos.Normalized() * (590));
                else JumpPos = basePos + (newPos.Normalized()*(Player.Distance(pos)));
            }
            if (JumpPos != new Vector2() && reCheckWard)
            {
                reCheckWard = false;
                Utility.DelayAction.Add(20, () =>
                {
                    if (JumpPos != new Vector2())
                    {
                        JumpPos = new Vector2();
                        reCheckWard = true;
                    }
                });
            }
            if (m2m) Orbwalk(pos);
            if (!W.IsReady() || W.Instance.Name == "blindmonkwtwo" || reqinMaxRange && Player.Distance(pos) > W.Range) return;
            if (minions || champions)
            {
                if (champions)
                {
                    var champs =
                        (from champ in ObjectManager.Get<Obj_AI_Hero>()
                            where champ.IsAlly && champ.Distance(Player) < W.Range && champ.Distance(pos) < 200 && !champ.IsMe
                            select champ).ToList();
                    if (champs.Count > 0)
                    {
                        W.CastOnUnit(champs[0], true);
                        return;
                    }
                }
                if (minions)
                {
                    var minion2 =
                        (from minion in ObjectManager.Get<Obj_AI_Minion>()
                            where
                                minion.IsAlly && minion.Distance(Player) < W.Range && minion.Distance(pos) < 200 &&
                                !minion.Name.ToLower().Contains("ward")
                            select minion).ToList();
                    if (minion2.Count > 0)
                    {
                        W.CastOnUnit(minion2[0], true);
                        return;
                    }
                }
            }
            var isWard = false;
            foreach (var ward in ObjectManager.Get<Obj_AI_Minion>())
            {
                if (ward.IsAlly && ward.Name.ToLower().Contains("ward") && ward.Distance(JumpPos) < 200)
                {
                    isWard = true;
                    W.CastOnUnit(ward, true);
                }
            }
            if (!isWard && CastWardAgain)
            {
                var ward = FindBestWardItem();
                if (ward == null) return;
                ward.UseItem(JumpPos.To3D());
                CastWardAgain = false;
                lastWardPos = JumpPos.To3D();
                lastPlaced = Environment.TickCount;
                Utility.DelayAction.Add(500, () => CastWardAgain = true);
            }
        }

        //Thanks to xSallice the gumbo
        private static InventorySlot FindBestWardItem()
        {
            InventorySlot slot = Items.GetWardSlot();
            if (slot == default(InventorySlot)) return null;

            SpellDataInst sdi = GetItemSpell(slot);

            if (sdi != default(SpellDataInst) && sdi.State == SpellState.Ready)
            {
                return slot;
            }
            return slot;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (Environment.TickCount < lastPlaced + 300)
            {
                var ward = (Obj_AI_Minion)sender;
                if (ward.Name.ToLower().Contains("ward") && ward.Distance(lastWardPos) < 500 && E.IsReady())
                {
                    W.Cast(ward);
                }
            }
        }

        public static void wardCombo()
        {
            var target = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Physical);
            Orbwalk(Game.CursorPos);
            if (target == null) return;
            useItems(target);
            if ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)))
            {
                if (CastQAgain || target.HasBuffOfType(BuffType.Knockup) && !Player.IsValidTarget(300) && !R.IsReady() || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && !R.IsReady())
                {
                    Q.Cast();
                }
            }
            if (target.Distance(Player) > R.Range && target.Distance(Player) < R.Range + 580 && (target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)))
            {
                WardJump(target.Position, false);
            }
            if (E.IsReady() && E.Instance.Name == "BlindMonkEOne" && target.IsValidTarget(E.Range))
                E.Cast();

            if (E.IsReady() && E.Instance.Name != "BlindMonkEOne" &&
                !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                E.Cast();

            if (Q.IsReady() && Q.Instance.Name == "BlindMonkQOne")
                CastQ1(target);

            if (R.IsReady() && Q.IsReady() &&
                ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true))))
                R.CastOnUnit(target, packets());
        }
        public static void StarCombo()
        {
            var target = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Physical);
            if (target == null) return;
            if (R.GetDamage(target) >= target.Health && paramBool("ksR")) R.Cast(target, packets());
            useItems(target);
            if ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)) && paramBool("useQ2"))
            {
                if (CastQAgain || target.HasBuffOfType(BuffType.Knockup) && !Player.IsValidTarget(300) && !R.IsReady() || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && !R.IsReady() || Q.GetDamage(target, 1) > target.Health)
                {
                    Q.Cast();
                }
            }
            if (paramBool("useW"))
            {
                if (paramBool("wMode") && target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player))
                    WardJump(target.Position, false, true);
                else if (!paramBool("wMode") && target.Distance(Player) > Q.Range) WardJump(target.Position, false, true);
            }
            if (E.IsReady() && E.Instance.Name == "BlindMonkEOne" && target.IsValidTarget(E.Range) && paramBool("useE"))
                E.Cast();

            if (E.IsReady() && E.Instance.Name != "BlindMonkEOne" &&
                !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && paramBool("useE"))
                E.Cast();

            if (Q.IsReady() && Q.Instance.Name == "BlindMonkQOne" && paramBool("useQ"))
                CastQ1(target);

            if (R.IsReady() && Q.IsReady() &&
                ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true))) && paramBool("useR"))
                R.CastOnUnit(target, packets());
        }
        public static void CastQ1(Obj_AI_Hero target)
        {
            var Qpred = Q.GetPrediction(target);
            if (Qpred.CollisionObjects.Count == 1 && Player.SummonerSpellbook.CanUseSpell(smiteSlot) == SpellState.Ready && paramBool("qSmite") && Q.MinHitChance == HitChance.High && Qpred.CollisionObjects[0].IsValidTarget(780))
            {
                Player.SummonerSpellbook.CastSpell(smiteSlot, Qpred.CollisionObjects[0]);
                Utility.DelayAction.Add(70, () => Q.Cast(Qpred.CastPosition, packets()));
            }
            else if(Qpred.CollisionObjects.Count == 0)
            {
                var minChance = GetHitChance(Menu.Item("QHC").GetValue<StringList>());
                Q.CastIfHitchanceEquals(target, minChance, true);
            }
        }
        public static bool paramBool(String paramName)
        {
            return Menu.Item(paramName).GetValue<bool>();
        }

        public static HitChance GetHitChance(StringList stringList)
        {
            switch (stringList.SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
            }
            return HitChance.Medium;
        }

        public static bool hpLowerParam(Obj_AI_Base obj, String paramName)
        {
            return ((obj.Health / obj.MaxHealth) * 100) <= Menu.Item(paramName).GetValue<Slider>().Value;
        }
    }
}
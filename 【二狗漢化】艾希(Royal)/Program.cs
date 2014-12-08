using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace RoyalAsheHelper
{
    class Program
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        private static readonly string champName = "Ashe";
        private static Spell Q, W, R;
        private static bool hasQ = false;
        private static Orbwalking.Orbwalker SOW;
        private static Menu menu;
        private const double WAngle = 57.5 * Math.PI / 180;
        
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (player.ChampionName != champName) return;
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1200);//57.5º - 2000
            R = new Spell(SpellSlot.R);
            W.SetSkillshot(0.5f, (float)WAngle, 2000f, false, SkillshotType.SkillshotCone);
            R.SetSkillshot(0.3f, 250f, 1600f, false, SkillshotType.SkillshotLine);
            LoadMenu();
            //Game.OnGameSendPacket += OnSendPacket;
            Game.OnGameUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.AfterAttack += AfterAttack;
            Orbwalking.BeforeAttack += BeforeAttack;
            Game.PrintChat("RoyalAsheHelper loaded!");
        }

        static void AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (menu.Item("exploit").GetValue<bool>() && menu.Item("UseQ").GetValue<bool>())
                foreach (BuffInstance buff in player.Buffs)
                    if (buff.Name == "FrostShot") Q.Cast();
        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (menu.Item("UseQ").GetValue<bool>())
                if (args.Target.Type == GameObjectType.obj_AI_Hero)
                {
                    foreach (BuffInstance buff in player.Buffs)
                        if (buff.Name == "FrostShot") return;
                    Q.Cast();
                }
                else
                {
                    foreach (BuffInstance buff in player.Buffs)
                        if (buff.Name == "FrostShot") Q.Cast();
                    
                }
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (R.IsReady() && menu.SubMenu("misc").Item("antigapcloser").GetValue<bool>() && Vector3.Distance(gapcloser.Sender.Position, player.Position) < 1000)
            {
                R.Cast(gapcloser.End, true);
            }
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (R.IsReady() &&
                Vector3.Distance(player.Position, unit.Position) < 1000 &&
                menu.SubMenu("misc").Item("interrupt").GetValue<bool>() &&
                spell.DangerLevel >= InterruptableDangerLevel.Medium)
            {
                R.Cast(unit.Position, true);
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (SOW.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            // Harass
            if (SOW.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();
        }
        
        static void Combo()
        {
            bool useW = W.IsReady() && menu.SubMenu("combo").Item("UseW").GetValue<bool>();
            bool useR = R.IsReady() && menu.SubMenu("combo").Item("UseR").GetValue<bool>();
            Obj_AI_Hero targetW = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
            Obj_AI_Hero targetR = SimpleTs.GetTarget(700, SimpleTs.DamageType.Magical);
            if (useW)
            {
                W.CastIfHitchanceEquals(targetW, HitChance.Medium);
            }
            if (useR)
            {
                R.CastIfHitchanceEquals(targetR, HitChance.High);
            }
        }
        
        static void Harass()
        {
            bool useW = W.IsReady() && menu.SubMenu("harass").Item("UseW").GetValue<bool>();
            Obj_AI_Hero targetW = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
            if (useW)
            {
                W.CastIfHitchanceEquals(targetW, HitChance.Medium);
            }
        }
        
        static void OnSendPacket(GamePacketEventArgs args)
        {
            if (!menu.SubMenu("combo").Item("UseQ").GetValue<bool>()) return;
            if (args.PacketData[0] == Packet.C2S.Move.Header && Packet.C2S.Move.Decoded(args.PacketData).SourceNetworkId == player.NetworkId && Packet.C2S.Move.Decoded(args.PacketData).MoveType == 3)
            {
                bool heroFound;
                foreach (BuffInstance buff in player.Buffs)
                    if (buff.Name == "FrostShot") hasQ = true;
                heroFound = false;
                foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                    if (hero.NetworkId == Packet.C2S.Move.Decoded(args.PacketData).TargetNetworkId)
                        heroFound = true;
                if (heroFound)
                {
                   if (!hasQ) Q.Cast();
                   hasQ = true;
                }
                else
                {
                    if (hasQ) Q.Cast();
                    hasQ = false;
                }
            }
        }
        
        static void LoadMenu()
        {
            // Initialize the menu
            menu = new Menu("Royal艾希-助手", "Ashe", true);

            // Target selector
            Menu targetSelector = new Menu("目標 選擇", "ts");
            SimpleTs.AddToMenu(targetSelector);
            menu.AddSubMenu(targetSelector);

            // Orbwalker
            Menu orbwalker = new Menu("走砍", "orbwalker");
            SOW = new Orbwalking.Orbwalker(orbwalker);
            menu.AddSubMenu(orbwalker);

            // Combo
            Menu combo = new Menu("連招", "combo");
            menu.AddSubMenu(combo);
            combo.AddItem(new MenuItem("UseQ", "使用 Q").SetValue(true));
            combo.AddItem(new MenuItem("UseW", "使用 W").SetValue(true));
            combo.AddItem(new MenuItem("UseR", "使用 R").SetValue(true));

            // Harass
            Menu harass = new Menu("騷擾", "harass");
            menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("UseW", "使用 W").SetValue(true));

            Menu misc = new Menu("雜項", "misc");
            menu.AddSubMenu(misc);
            misc.AddItem(new MenuItem("interrupt", "中斷法術").SetValue(true));
            misc.AddItem(new MenuItem("exploit", "Q (利用漏洞 無藍)").SetValue(false));
            //misc.AddItem(new MenuItem("interruptLevel", "Interrupt only with danger level").SetValue<InterruptableDangerLevel>(InterruptableDangerLevel.Medium));
            misc.AddItem(new MenuItem("antigapcloser", "防止突進").SetValue(true));

            // Finalize menu
            menu.AddToMainMenu();
            Console.WriteLine("Menu finalized");
        }
    }
}

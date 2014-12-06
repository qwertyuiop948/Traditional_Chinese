using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using LX_Orbwalker;

namespace Master
{
    class Rammus : Program
    {
        private const String Version = "1.0.0";

        public Rammus()
        {
            SkillQ = new Spell(SpellSlot.Q, 1100);
            SkillW = new Spell(SpellSlot.W, 325);
            SkillE = new Spell(SpellSlot.E, 300);
            SkillR = new Spell(SpellSlot.R, 300);
            SkillE.SetTargetted(SkillE.Instance.SData.SpellCastTime, SkillE.Instance.SData.MissileSpeed);
            SkillR.SetSkillshot(SkillR.Instance.SData.SpellCastTime, SkillR.Instance.SData.LineWidth, SkillR.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotCircle);

            Config.AddSubMenu(new Menu("連招/騷擾", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "使用 Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "使用 W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "使用 E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "euseMode", "E模式").SetValue(new StringList(new[] { "總是", "W 準備" })));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autoeusage", "使用E如果HP以上").SetValue(new Slider(20, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "rusage", "使用 R").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ruseMode", "R模式").SetValue(new StringList(new[] { "總是", "# 敵人" })));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "rmulti", "如果以上的敵人使用R").SetValue(new Slider(2, 1, 4)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "如果可擊殺自動使用點燃").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "使用項目").SetValue(true));

            Config.AddSubMenu(new Menu("清線/清野", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "使用 Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearW", "使用 W").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "使用 E").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearEMode", "E模式").SetValue(new StringList(new[] { "總是", "W 準備" })));

            Config.AddSubMenu(new Menu("雜項", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useAntiQ", "使用Q接近").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useInterE", "使用E打斷").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "SkinID", "|換皮膚|").SetValue(new Slider(6, 0, 6))).ValueChanged += SkinChanger;
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "使用封包").SetValue(true));

            Config.AddSubMenu(new Menu("技能範圍選項", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E 範圍").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawR", "R 範圍").SetValue(true));
Config.AddSubMenu(new Menu("初見漢化", "by chujian"));

Config.SubMenu("by chujian").AddItem(new MenuItem("qunhao", "漢化群：386289593"));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Game.PrintChat("<font color = \"#33CCCC\">Master of {0}</font> <font color = \"#00ff00\">v{1}</font>", Name, Version);
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            PacketCast = Config.Item(Name + "packetCast").GetValue<bool>();
            if (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Combo || LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Harass)
            {
                NormalCombo();
            }
            else if (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.LaneClear || LXOrbwalker.CurrentMode == LXOrbwalker.Mode.LaneFreeze)
            {
                LaneJungClear();
            }
            else if (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Flee && SkillQ.IsReady() && !Player.HasBuff("PowerBall", true)) SkillQ.Cast();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item(Name + "DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawR").GetValue<bool>() && SkillR.Level > 0) Utility.DrawCircle(Player.Position, SkillR.Range, SkillR.IsReady() ? Color.Green : Color.Red);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item(Name + "useAntiQ").GetValue<bool>()) return;
            if (gapcloser.Sender.IsValidTarget(SkillE.Range) && SkillQ.IsReady() && !Player.HasBuff("PowerBall", true)) SkillQ.Cast(PacketCast);
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item(Name + "useInterE").GetValue<bool>()) return;
            if (unit.IsValidTarget(SkillE.Range) && SkillE.IsReady()) SkillE.CastOnUnit(unit, PacketCast);
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && targetObj.IsValidTarget(1000) && !Player.HasBuff("PowerBall", true))
            {
                if (!SkillE.InRange(targetObj.Position))
                {
                    SkillQ.Cast(PacketCast);
                }
                else if (!Player.HasBuff("DefensiveBallCurl", true)) SkillQ.Cast(PacketCast);
            }
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && SkillE.InRange(targetObj.Position) && Player.Health * 100 / Player.MaxHealth >= Config.Item(Name + "autoeusage").GetValue<Slider>().Value)
            {
                switch (Config.Item(Name + "euseMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        SkillE.CastOnUnit(targetObj, PacketCast);
                        break;
                    case 1:
                        if (Player.HasBuff("DefensiveBallCurl", true)) SkillE.CastOnUnit(targetObj, PacketCast);
                        break;
                }
            }
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && SkillE.InRange(targetObj.Position) && !Player.HasBuff("PowerBall", true)) SkillW.Cast();
            if (Config.Item(Name + "rusage").GetValue<bool>() && SkillR.IsReady())
            {
                switch (Config.Item(Name + "ruseMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (SkillR.InRange(targetObj.Position)) SkillR.Cast(PacketCast);
                        break;
                    case 1:
                        if (Player.CountEnemysInRange((int)SkillR.Range) >= Config.Item(Name + "rmulti").GetValue<Slider>().Value) SkillR.Cast(PacketCast);
                        break;
                }
            }
            if (Config.Item(Name + "iusage").GetValue<bool>() && Items.CanUseItem(Rand) && Player.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, 1000, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault();
            if (minionObj == null) return;
            if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady() && !Player.HasBuff("PowerBall", true))
            {
                if (!SkillE.InRange(minionObj.Position))
                {
                    SkillQ.Cast(PacketCast);
                }
                else if (!Player.HasBuff("DefensiveBallCurl", true)) SkillQ.Cast(PacketCast);
            }
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady() && SkillE.InRange(minionObj.Position))
            {
                switch (Config.Item(Name + "useClearEMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        SkillE.CastOnUnit(minionObj, PacketCast);
                        break;
                    case 1:
                        if (Player.HasBuff("DefensiveBallCurl", true)) SkillE.CastOnUnit(minionObj, PacketCast);
                        break;
                }
            }
            if (Config.Item(Name + "useClearW").GetValue<bool>() && SkillW.IsReady() && SkillE.InRange(minionObj.Position) && !Player.HasBuff("PowerBall", true)) SkillW.Cast(PacketCast);
        }
    }
}
using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using LX_Orbwalker;

namespace Master
{
    class Amumu : Program
    {
        private const String Version = "1.0.0";

        public Amumu()
        {
            SkillQ = new Spell(SpellSlot.Q, 1100);
            SkillW = new Spell(SpellSlot.W, 300);
            SkillE = new Spell(SpellSlot.E, 350);
            SkillR = new Spell(SpellSlot.R, 550);
            SkillQ.SetSkillshot(SkillQ.Instance.SData.SpellCastTime, SkillQ.Instance.SData.LineWidth, SkillQ.Instance.SData.MissileSpeed, true, SkillshotType.SkillshotLine);
            SkillE.SetSkillshot(SkillE.Instance.SData.SpellCastTime, SkillE.Instance.SData.LineWidth, SkillE.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotCircle);
            SkillR.SetSkillshot(SkillR.Instance.SData.SpellCastTime, SkillR.Instance.SData.LineWidth, SkillR.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotCircle);

            Config.AddSubMenu(new Menu("連招/騷擾", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "使用 Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "使用 W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autowusage", "使用W如果魔法以上").SetValue(new Slider(20, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "使用 E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "rusage", "使用 R").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ruseMode", "R模式").SetValue(new StringList(new[] { "終結", "# 敵人數量" })));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "rmulti", "如果在以上的敵人使用R").SetValue(new Slider(2, 1, 4)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "擊殺使用點燃").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "使用項目").SetValue(true));

            Config.AddSubMenu(new Menu("清線/清野", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "使用 Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearW", "使用 W").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearAutoW", "使用W如果魔法以上").SetValue(new Slider(20, 1)));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "使用 E").SetValue(true));

            Config.AddSubMenu(new Menu("雜項", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useAntiQ", "使用Q接近").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "smite", "自動懲戒小兵接Q").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "SkinID", "|換皮膚|").SetValue(new Slider(6, 0, 7))).ValueChanged += SkinChanger;
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "使用封包").SetValue(true));

            Config.AddSubMenu(new Menu("技能範圍選項", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawQ", "Q 範圍").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawW", "W 範圍").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E 範圍").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawR", "R 範圍").SetValue(true));
Config.AddSubMenu(new Menu("初見漢化", "by chujian"));

Config.SubMenu("by chujian").AddItem(new MenuItem("qunhao", "漢化群：386289593"));
Config.SubMenu("by chujian").AddItem(new MenuItem("qunhao2", "娃娃群：158994507"));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
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
            else if (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.LaneClear || LXOrbwalker.CurrentMode == LXOrbwalker.Mode.LaneFreeze) LaneJungClear();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item(Name + "DrawQ").GetValue<bool>() && SkillQ.Level > 0) Utility.DrawCircle(Player.Position, SkillQ.Range, SkillQ.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawW").GetValue<bool>() && SkillW.Level > 0) Utility.DrawCircle(Player.Position, SkillW.Range, SkillW.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawR").GetValue<bool>() && SkillR.Level > 0) Utility.DrawCircle(Player.Position, SkillR.Range, SkillR.IsReady() ? Color.Green : Color.Red);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item(Name + "useAntiQ").GetValue<bool>()) return;
            if (gapcloser.Sender.IsValidTarget(SkillQ.Range) && SkillQ.IsReady() && Player.Distance(gapcloser.Sender) < 400) SkillQ.Cast(gapcloser.Sender, PacketCast);
        }

        private void NormalCombo()
        {
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && Player.HasBuff("AuraofDespair") && Player.CountEnemysInRange(500) == 0) SkillW.Cast(PacketCast);
            if (targetObj == null) return;
            if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && (CanKill(targetObj, SkillQ) || !LXOrbwalker.InAutoAttackRange(targetObj)))
            {
                if (Config.Item(Name + "smite").GetValue<bool>() && SkillQ.GetPrediction(targetObj).Hitchance == HitChance.Collision)
                {
                    if (!SmiteCollision(targetObj, SkillQ)) SkillQ.CastIfHitchanceEquals(targetObj, HitChance.VeryHigh, PacketCast);
                }
                else SkillQ.CastIfHitchanceEquals(targetObj, HitChance.VeryHigh, PacketCast);
            }
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady())
            {
                if (Player.Mana * 100 / Player.MaxMana >= Config.Item(Name + "autowusage").GetValue<Slider>().Value)
                {
                    if (SkillW.InRange(targetObj.Position))
                    {
                        if (!Player.HasBuff("AuraofDespair")) SkillW.Cast(PacketCast);
                    }
                    else if (Player.HasBuff("AuraofDespair")) SkillW.Cast(PacketCast);
                }
                else if (Player.HasBuff("AuraofDespair")) SkillW.Cast(PacketCast);
            }
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && SkillE.InRange(targetObj.Position)) SkillE.Cast(PacketCast);
            if (Config.Item(Name + "rusage").GetValue<bool>() && SkillR.IsReady())
            {
                switch (Config.Item(Name + "ruseMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (SkillR.InRange(targetObj.Position) && CanKill(targetObj, SkillR)) SkillR.Cast(PacketCast);
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
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault();
            if (minionObj == null)
            {
                if (Config.Item(Name + "useClearW").GetValue<bool>() && SkillW.IsReady() && Player.HasBuff("AuraofDespair")) SkillW.Cast(PacketCast);
                return;
            }
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady() && SkillE.InRange(minionObj.Position)) SkillE.Cast(PacketCast);
            if (Config.Item(Name + "useClearW").GetValue<bool>() && SkillW.IsReady())
            {
                if (Player.Mana * 100 / Player.MaxMana >= Config.Item(Name + "useClearAutoW").GetValue<Slider>().Value)
                {
                    if (MinionManager.GetMinions(Player.Position, SkillW.Range, MinionTypes.All, MinionTeam.NotAlly).Count >= 2)
                    {
                        if (!Player.HasBuff("AuraofDespair")) SkillW.Cast(PacketCast);
                    }
                    else if (Player.HasBuff("AuraofDespair")) SkillW.Cast(PacketCast);
                }
                else if (Player.HasBuff("AuraofDespair")) SkillW.Cast(PacketCast);
            }
            if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady() && (Player.Distance(minionObj) > 450 || CanKill(minionObj, SkillQ))) SkillQ.CastIfHitchanceEquals(minionObj, HitChance.VeryHigh, PacketCast);
        }
    }
}
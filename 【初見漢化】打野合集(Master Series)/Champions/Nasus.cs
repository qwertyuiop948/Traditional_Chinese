using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using LX_Orbwalker;

namespace Master
{
    class Nasus : Program
    {
        private const String Version = "1.0.2";
        private Int32 Sheen = 3057, Iceborn = 3025;

        public Nasus()
        {
            SkillQ = new Spell(SpellSlot.Q, 300);
            SkillW = new Spell(SpellSlot.W, 600);
            SkillE = new Spell(SpellSlot.E, 650);
            SkillR = new Spell(SpellSlot.R, 20);
            SkillW.SetTargetted(SkillW.Instance.SData.SpellCastTime, SkillW.Instance.SData.MissileSpeed);
            SkillE.SetSkillshot(SkillE.Instance.SData.SpellCastTime, SkillE.Instance.SData.LineWidth, SkillE.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotCircle);

            Config.AddSubMenu(new Menu("連招/騷擾", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "使用 Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "使用 W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "使用 E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "如果可擊殺自動使用點燃").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "使用項目").SetValue(true));

            Config.AddSubMenu(new Menu("清線/清野", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "使用 Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "使用 E").SetValue(true));

            Config.AddSubMenu(new Menu("大招選項", "useUlt"));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "surviveR", "嘗試使用R生存").SetValue(true));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "autouseR", "如果HP在使用R").SetValue(new Slider(30, 1)));

            Config.AddSubMenu(new Menu("雜項", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "lasthitQ", "使用Q最後一擊|").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "killstealE", "自動E偷殺").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "SkinID", "|換皮膚|").SetValue(new Slider(5, 0, 5))).ValueChanged += SkinChanger;
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "使用封包").SetValue(true));

            Config.AddSubMenu(new Menu("技能範圍選項", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawW", "W 範圍").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E 範圍").SetValue(true));
Config.AddSubMenu(new Menu("初見漢化", "by chujian"));

Config.SubMenu("by chujian").AddItem(new MenuItem("qunhao", "漢化群：386289593"));
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            GameObject.OnCreate += OnCreate;
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
            else if (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Lasthit && Config.Item(Name + "lasthitQ").GetValue<bool>()) LastHit();
            if (Config.Item(Name + "killstealE").GetValue<bool>()) KillSteal();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item(Name + "DrawW").GetValue<bool>() && SkillW.Level > 0) Utility.DrawCircle(Player.Position, SkillW.Range, SkillW.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
        }

        private void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is Obj_SpellMissile && sender.IsValid && Config.Item(Name + "surviveR").GetValue<bool>() && SkillR.IsReady())
            {
                var missle = (Obj_SpellMissile)sender;
                var caster = missle.SpellCaster;
                if (caster.IsEnemy)
                {
                    if (missle.SData.Name.Contains("BasicAttack"))
                    {
                        if (missle.Target.IsMe && (Player.Health - caster.GetAutoAttackDamage(Player, true)) * 100 / Player.MaxHealth <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
                    }
                    else if (missle.Target.IsMe || missle.EndPosition.Distance(Player.Position) <= 130)
                    {
                        if (missle.SData.Name == "summonerdot")
                        {
                            if ((Player.Health - (caster as Obj_AI_Hero).GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite)) * 100 / Player.MaxHealth <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
                        }
                        else if ((Player.Health - (caster as Obj_AI_Hero).GetSpellDamage(Player, (caster as Obj_AI_Hero).GetSpellSlot(missle.SData.Name, false), 1)) * 100 / Player.MaxHealth <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
                    }
                }
            }
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && SkillW.InRange(targetObj.Position)) SkillW.CastOnUnit(targetObj, PacketCast);
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && SkillE.InRange(targetObj.Position)) SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 100, PacketCast);
            if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.InRange(targetObj.Position))
            {
                var DmgAA = Player.GetAutoAttackDamage(targetObj) * Math.Floor(SkillQ.Instance.Cooldown / (1 / (Player.PercentMultiplicativeAttackSpeedMod * 0.638)));
                if ((targetObj.Health <= GetBonusDmg(targetObj) || targetObj.Health > DmgAA + GetBonusDmg(targetObj)) && SkillQ.IsReady()) SkillQ.Cast(PacketCast);
            }
            if (Config.Item(Name + "iusage").GetValue<bool>() && Items.CanUseItem(Rand) && Player.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = (Obj_AI_Base)ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(i => i.IsValidTarget(SkillQ.Range) && i.Health <= GetBonusDmg(i));
            if (minionObj == null) minionObj = MinionManager.GetMinions(Player.Position, SkillE.Range, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault();
            if (minionObj == null) return;
            if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.InRange(minionObj.Position))
            {
                var DmgAA = Player.GetAutoAttackDamage(minionObj) * Math.Floor(SkillQ.Instance.Cooldown / (1 / (Player.PercentMultiplicativeAttackSpeedMod * 0.638)));
                if ((minionObj.Health <= GetBonusDmg(minionObj) || minionObj.Health > DmgAA + GetBonusDmg(minionObj)) && (SkillQ.IsReady() || Player.HasBuff("NasusQ", true)))
                {
                    LXOrbwalker.SetAttack(false);
                    if (!Player.HasBuff("NasusQ", true)) SkillQ.Cast(PacketCast);
                    if (Player.HasBuff("NasusQ", true)) Player.IssueOrder(GameObjectOrder.AttackUnit, minionObj);
                    LXOrbwalker.SetAttack(true);
                }
            }
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady() && minionObj is Obj_AI_Minion) SkillE.Cast(minionObj.Position, PacketCast);
        }

        private void LastHit()
        {
            var minionObj = (Obj_AI_Base)ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(i => i.IsValidTarget(SkillQ.Range) && i.Health <= GetBonusDmg(i));
            if (minionObj == null) minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault(i => i.Health <= GetBonusDmg(i));
            if (minionObj == null) return;
            if (SkillQ.IsReady() || Player.HasBuff("NasusQ", true))
            {
                LXOrbwalker.SetAttack(false);
                if (!Player.HasBuff("NasusQ", true)) SkillQ.Cast(PacketCast);
                if (Player.HasBuff("NasusQ", true)) Player.IssueOrder(GameObjectOrder.AttackUnit, minionObj);
                LXOrbwalker.SetAttack(true);
            }
        }

        private void KillSteal()
        {
            var target = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(i => i.IsValidTarget(SkillE.Range) && CanKill(i, SkillE) && i != targetObj);
            if (target != null && SkillE.IsReady()) SkillE.Cast(target.Position, PacketCast);
        }

        private double GetBonusDmg(Obj_AI_Base target)
        {
            double DmgItem = 0;
            if (Items.HasItem(Sheen) && (Items.CanUseItem(Sheen) || Player.HasBuff("sheen", true)) && Player.BaseAttackDamage > DmgItem) DmgItem = Player.BaseAttackDamage;
            if (Items.HasItem(Iceborn) && (Items.CanUseItem(Iceborn) || Player.HasBuff("itemfrozenfist", true)) && Player.BaseAttackDamage * 1.25 > DmgItem) DmgItem = Player.BaseAttackDamage * 1.25;
            return SkillQ.GetDamage(target) + Player.CalcDamage(target, Damage.DamageType.Physical, Player.BaseAttackDamage + Player.FlatPhysicalDamageMod + DmgItem);
        }
    }
}
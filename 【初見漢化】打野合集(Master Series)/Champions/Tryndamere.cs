using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using LX_Orbwalker;

namespace Master
{
    class Tryndamere : Program
    {
        private const String Version = "1.0.5";

        public Tryndamere()
        {
            SkillQ = new Spell(SpellSlot.Q, 320);
            SkillW = new Spell(SpellSlot.W, 750);
            SkillE = new Spell(SpellSlot.E, 660);
            SkillR = new Spell(SpellSlot.R, 400);
            SkillE.SetSkillshot(SkillE.Instance.SData.SpellCastTime, SkillE.Instance.SData.LineWidth, SkillE.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotLine);

            Config.AddSubMenu(new Menu("連招/騷擾", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "使用 Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autoqusage", "|使用Q如果HP在|").SetValue(new Slider(40, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "使用 W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "|使用E在連招|").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autoeusage", "|使用E如果HP以上|").SetValue(new Slider(20, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "如果可擊殺自動使用點燃").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "使用項目").SetValue(true));

            Config.AddSubMenu(new Menu("清線/清野", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "使用 E").SetValue(true));

            Config.AddSubMenu(new Menu("雜項", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "killstealE", "自動E偷殺").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "surviveQ", "嘗試用Q生存").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "surviveR", "嘗試用R生存").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "SkinID", "|換皮膚|").SetValue(new Slider(4, 0, 6))).ValueChanged += SkinChanger;
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
            switch (LXOrbwalker.CurrentMode)
            {
                case LXOrbwalker.Mode.Combo:
                    NormalCombo(false);
                    break;
                case LXOrbwalker.Mode.Harass:
                    NormalCombo(true);
                    break;
                case LXOrbwalker.Mode.LaneClear:
                    LaneJungClear();
                    break;
                case LXOrbwalker.Mode.LaneFreeze:
                    LaneJungClear();
                    break;
                case LXOrbwalker.Mode.Flee:
                    if (SkillE.IsReady()) SkillE.Cast(Game.CursorPos, PacketCast);
                    break;
            }
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
            if (!(sender is Obj_SpellMissile)) return;
            var missle = (Obj_SpellMissile)sender;
            var caster = missle.SpellCaster;
            if (caster.IsEnemy)
            {
                if (Config.Item(Name + "surviveQ").GetValue<bool>() && SkillQ.IsReady())
                {
                    var HealthBuff = (Player.Mana == 100) ? new Int32[] { 80, 135, 190, 245, 300 }[SkillQ.Level - 1] + 1.5 * Player.FlatMagicDamageMod : (new Int32[] { 30, 40, 50, 60, 70 }[SkillQ.Level - 1] + 0.3 * Player.FlatMagicDamageMod + new double[] { 0.5, 0.95, 1.4, 1.85, 2.3 }[SkillQ.Level - 1] + 0.012 * Player.FlatMagicDamageMod) * Player.Mana;
                    if (missle.SData.Name.Contains("BasicAttack"))
                    {
                        if (missle.Target.IsMe && Player.Health <= caster.GetAutoAttackDamage(Player, true) && Player.Health + HealthBuff > caster.GetAutoAttackDamage(Player, true)) SkillQ.Cast();
                    }
                    else if (missle.Target.IsMe || missle.EndPosition.Distance(Player.Position) <= 130)
                    {
                        if (missle.SData.Name == "summonerdot")
                        {
                            if (Player.Health <= (caster as Obj_AI_Hero).GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite) && Player.Health + HealthBuff > (caster as Obj_AI_Hero).GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite)) SkillQ.Cast();
                        }
                        else if (Player.Health <= (caster as Obj_AI_Hero).GetSpellDamage(Player, (caster as Obj_AI_Hero).GetSpellSlot(missle.SData.Name, false), 1) && Player.Health + HealthBuff > (caster as Obj_AI_Hero).GetSpellDamage(Player, (caster as Obj_AI_Hero).GetSpellSlot(missle.SData.Name, false), 1)) SkillQ.Cast();
                    }
                }
                if (Config.Item(Name + "surviveR").GetValue<bool>() && SkillR.IsReady())
                {
                    if (missle.SData.Name.Contains("BasicAttack"))
                    {
                        if (missle.Target.IsMe && Player.Health <= caster.GetAutoAttackDamage(Player, true)) SkillR.Cast();
                    }
                    else if (missle.Target.IsMe || missle.EndPosition.Distance(Player.Position) <= 130)
                    {
                        if (missle.SData.Name == "summonerdot")
                        {
                            if (Player.Health <= (caster as Obj_AI_Hero).GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite)) SkillR.Cast();
                        }
                        else if (Player.Health <= (caster as Obj_AI_Hero).GetSpellDamage(Player, (caster as Obj_AI_Hero).GetSpellSlot(missle.SData.Name, false), 1)) SkillR.Cast();
                    }
                }
            }
        }

        private void NormalCombo(bool IsHarass)
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && Player.Health * 100 / Player.MaxHealth <= Config.Item(Name + "autoqusage").GetValue<Slider>().Value && Player.CountEnemysInRange(800) >= 1) SkillQ.Cast(PacketCast);
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && SkillW.InRange(targetObj.Position))
            {
                if (Utility.IsBothFacing(Player, targetObj, 300))
                {
                    if (Player.GetAutoAttackDamage(targetObj) < targetObj.GetAutoAttackDamage(Player) || Player.Health < targetObj.Health) SkillW.Cast(PacketCast);
                }
                else if (Player.IsFacing(targetObj) && !targetObj.IsFacing(Player) && !LXOrbwalker.InAutoAttackRange(targetObj)) SkillW.Cast(PacketCast);
            }
            if (!IsHarass && Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && SkillE.InRange(targetObj.Position) && !LXOrbwalker.InAutoAttackRange(targetObj))
            {
                if (Player.Health * 100 / Player.MaxHealth >= Config.Item(Name + "autoeusage").GetValue<Slider>().Value)
                {
                    SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
                }
                else if (SkillR.IsReady() || (Player.Mana >= 70 && SkillQ.IsReady())) SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
            }
            if (Config.Item(Name + "iusage").GetValue<bool>()) UseItem(targetObj);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillE.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (minionObj.Count == 0) return;
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady()) SkillE.Cast(SkillE.GetLineFarmLocation(minionObj.ToList()).Position, PacketCast);
        }

        private void KillSteal()
        {
            var target = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(i => i.IsValidTarget(SkillE.Range) && CanKill(i, SkillE) && i != targetObj);
            if (target != null && SkillE.IsReady()) SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
        }

        private void UseItem(Obj_AI_Hero target)
        {
            if (Items.CanUseItem(Bilge) && Player.Distance(target) <= 450) Items.UseItem(Bilge, target);
            if (Items.CanUseItem(Blade) && Player.Distance(target) <= 450) Items.UseItem(Blade, target);
            if (Items.CanUseItem(Tiamat) && Player.CountEnemysInRange(350) >= 1) Items.UseItem(Tiamat);
            if (Items.CanUseItem(Hydra) && (Player.CountEnemysInRange(350) >= 2 || (Player.GetAutoAttackDamage(target) < target.Health && Player.CountEnemysInRange(350) == 1))) Items.UseItem(Hydra);
            if (Items.CanUseItem(Rand) && Player.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
            if (Items.CanUseItem(Youmuu) && Player.CountEnemysInRange(350) >= 1) Items.UseItem(Youmuu);
        }
    }
}
using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using LX_Orbwalker;

namespace Master
{
    class JarvanIV : Program
    {
        private const String Version = "1.0.0";
        private Obj_AI_Base wallObj = null;
        private Vector3 flagPos = default(Vector3);

        public JarvanIV()
        {
            SkillQ = new Spell(SpellSlot.Q, 770);
            SkillW = new Spell(SpellSlot.W, 525);
            SkillE = new Spell(SpellSlot.E, 830);
            SkillR = new Spell(SpellSlot.R, 650);
            SkillQ.SetSkillshot(SkillQ.Instance.SData.SpellCastTime, SkillQ.Instance.SData.LineWidth, SkillQ.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotLine);
            SkillE.SetSkillshot(SkillE.Instance.SData.SpellCastTime, SkillE.Instance.SData.LineWidth, SkillE.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotCircle);
            SkillR.SetTargetted(SkillR.Instance.SData.SpellCastTime, SkillR.Instance.SData.MissileSpeed);

            Config.SubMenu("Orbwalker").SubMenu("lxOrbwalker_Modes").AddItem(new MenuItem(Name + "EQFlash", "Combo EQ Flash").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("連招", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "使用 Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "使用 W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autowusage", "使用W如果HP在|").SetValue(new Slider(20, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "使用 E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "rusage", "使用 R").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ruseMode", "R模式").SetValue(new StringList(new[] { "Finish", "# Enemy" })));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "rmulti", "如果以上的敵人使用R").SetValue(new Slider(2, 1, 4)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "如果可擊殺自動使用點燃").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "使用項目").SetValue(true));

            Config.AddSubMenu(new Menu("騷擾", "hsettings"));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "useHarE", "使用 E").SetValue(true));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "harMode", "如果hp以上使用EQ").SetValue(new Slider(20, 1)));

            Config.AddSubMenu(new Menu("清線/清野", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "使用 Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "使用 E").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearI", "使用 提亞瑪特/貪慾九頭").SetValue(true));

            Config.AddSubMenu(new Menu("雜項", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "lasthitQ", "|使用Q最後一擊|").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "killstealQ", "使用Q擊殺").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useInterEQ", "使用EQ打斷").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "surviveW", "嘗試使用W生存").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "SkinID", "|換皮膚|").SetValue(new Slider(5, 0, 6))).ValueChanged += SkinChanger;
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "使用封包").SetValue(true));

            Config.AddSubMenu(new Menu("|技能範圍|", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawQ", "Q 範圍").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawW", "W 範圍").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E 範圍").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawR", "R 範圍").SetValue(true));
Config.AddSubMenu(new Menu("初見漢化", "by chujian"));

Config.SubMenu("by chujian").AddItem(new MenuItem("qunhao", "漢化群：386289593"));
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnCreate += OnCreate;
            Obj_AI_Base.OnDelete += OnDelete;
            Game.PrintChat("<font color = \"#33CCCC\">Master of {0}</font> <font color = \"#00ff00\">v{1}</font>", Name, Version);
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            PacketCast = Config.Item(Name + "packetCast").GetValue<bool>();
            switch (LXOrbwalker.CurrentMode)
            {
                case LXOrbwalker.Mode.Combo:
                    NormalCombo();
                    break;
                case LXOrbwalker.Mode.Harass:
                    Harass();
                    break;
                case LXOrbwalker.Mode.LaneClear:
                    LaneJungClear();
                    break;
                case LXOrbwalker.Mode.LaneFreeze:
                    LaneJungClear();
                    break;
                case LXOrbwalker.Mode.Lasthit:
                    if (Config.Item(Name + "lasthitQ").GetValue<bool>()) LastHit();
                    break;
                case LXOrbwalker.Mode.Flee:
                    Flee();
                    break;
            }
            if (Config.Item(Name + "EQFlash").GetValue<KeyBind>().Active)
            {
                LXOrbwalker.CustomOrbwalkMode = true;
                ComboEQFlash();
            }
            else LXOrbwalker.CustomOrbwalkMode = false;
            if (Config.Item(Name + "killstealQ").GetValue<bool>()) KillSteal();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item(Name + "DrawQ").GetValue<bool>() && SkillQ.Level > 0) Utility.DrawCircle(Player.Position, SkillQ.Range, SkillQ.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawW").GetValue<bool>() && SkillW.Level > 0) Utility.DrawCircle(Player.Position, SkillW.Range, SkillW.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawR").GetValue<bool>() && SkillR.Level > 0) Utility.DrawCircle(Player.Position, SkillR.Range, SkillR.IsReady() ? Color.Green : Color.Red);
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item(Name + "useInterEQ").GetValue<bool>() || !SkillQ.IsReady()) return;
            if (unit.IsValidTarget(SkillQ.Range) && SkillE.IsReady()) SkillE.Cast(unit.Position + Vector3.Normalize(unit.Position - Player.Position) * 100, PacketCast);
            if (flagPos != default(Vector3) && unit.IsValidTarget(180, true, flagPos)) SkillQ.Cast(flagPos, PacketCast);
        }

        private void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "JarvanDemacianStandard_buf_green.troy") flagPos = sender.Position;
            if (sender.Name == "JarvanCataclysm_tar.troy") wallObj = (Obj_AI_Base)sender;
            if (sender is Obj_SpellMissile && sender.IsValid && Config.Item(Name + "surviveW").GetValue<bool>() && SkillW.IsReady())
            {
                var missle = (Obj_SpellMissile)sender;
                var caster = missle.SpellCaster;
                if (caster.IsEnemy)
                {
                    var ShieldBuff = new Int32[] { 50, 90, 130, 170, 210 }[SkillW.Level - 1] + new Int32[] { 20, 30, 40, 50, 60 }[SkillW.Level - 1] * Player.CountEnemysInRange(300);
                    if (missle.SData.Name.Contains("BasicAttack"))
                    {
                        if (missle.Target.IsMe && Player.Health <= caster.GetAutoAttackDamage(Player, true) && Player.Health + ShieldBuff > caster.GetAutoAttackDamage(Player, true)) SkillW.Cast(PacketCast);
                    }
                    else if (missle.Target.IsMe || missle.EndPosition.Distance(Player.Position) <= 130)
                    {
                        if (missle.SData.Name == "summonerdot")
                        {
                            if (Player.Health <= (caster as Obj_AI_Hero).GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite) && Player.Health + ShieldBuff > (caster as Obj_AI_Hero).GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite)) SkillW.Cast(PacketCast);
                        }
                        else if (Player.Health <= (caster as Obj_AI_Hero).GetSpellDamage(Player, (caster as Obj_AI_Hero).GetSpellSlot(missle.SData.Name, false), 1) && Player.Health + ShieldBuff > (caster as Obj_AI_Hero).GetSpellDamage(Player, (caster as Obj_AI_Hero).GetSpellSlot(missle.SData.Name, false), 1)) SkillW.Cast(PacketCast);
                    }
                }
            }
        }

        private void OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "JarvanDemacianStandard_buf_green.troy") flagPos = default(Vector3);
            if (sender.Name == "JarvanCataclysm_tar.troy") wallObj = null;
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && SkillQ.InRange(targetObj.Position)) SkillE.Cast(LXOrbwalker.InAutoAttackRange(targetObj) ? targetObj.Position : targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 100, PacketCast);
            if (Config.Item(Name + "eusage").GetValue<bool>() && flagPos != default(Vector3) && targetObj.IsValidTarget(180, true, flagPos))
            {
                if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.InRange(targetObj.Position) && SkillQ.IsReady()) SkillQ.Cast(flagPos, PacketCast);
            }
            else if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.InRange(targetObj.Position) && SkillQ.IsReady()) SkillQ.Cast(targetObj.Position, PacketCast);
            if (Config.Item(Name + "rusage").GetValue<bool>() && SkillR.IsReady() && wallObj == null)
            {
                switch (Config.Item(Name + "ruseMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (SkillR.InRange(targetObj.Position) && CanKill(targetObj, SkillR)) SkillR.CastOnUnit(targetObj, PacketCast);
                        break;
                    case 1:
                        var UltiObj = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(i => i.IsValidTarget(SkillR.Range) && i.CountEnemysInRange(325) >= Config.Item(Name + "rmulti").GetValue<Slider>().Value);
                        if (UltiObj != null) SkillR.CastOnUnit(UltiObj, PacketCast);
                        break;
                }
            }
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && SkillW.InRange(targetObj.Position) && Player.Health * 100 / Player.MaxHealth <= Config.Item(Name + "autowusage").GetValue<Slider>().Value) SkillW.Cast(PacketCast);
            if (Config.Item(Name + "iusage").GetValue<bool>()) UseItem(targetObj);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void Harass()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "useHarE").GetValue<bool>() && SkillE.IsReady() && SkillQ.InRange(targetObj.Position)) SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 100, PacketCast);
            if (Config.Item(Name + "useHarE").GetValue<bool>() && flagPos != default(Vector3) && targetObj.IsValidTarget(180, true, flagPos) && SkillQ.InRange(targetObj.Position) && SkillQ.IsReady())
            {
                if (Player.Health * 100 / Player.MaxHealth >= Config.Item(Name + "harMode").GetValue<Slider>().Value) SkillQ.Cast(flagPos, PacketCast);
            }
            else if (SkillQ.InRange(targetObj.Position) && SkillQ.IsReady()) SkillQ.Cast(targetObj.Position, PacketCast);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (minionObj.Count() == 0) return;
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady()) SkillE.Cast(SkillE.GetCircularFarmLocation(minionObj.ToList()).Position, PacketCast);
            if (Config.Item(Name + "useClearE").GetValue<bool>() && flagPos != default(Vector3) && minionObj.Count(i => i.IsValidTarget(180, true, flagPos)) >= 2)
            {
                if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady()) SkillQ.Cast(flagPos, PacketCast);
            }
            else if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady()) SkillQ.Cast(SkillQ.GetLineFarmLocation(minionObj.ToList()).Position, PacketCast);
            if (Config.Item(Name + "useClearI").GetValue<bool>() && minionObj.Count(i => i.IsValidTarget(350)) >= 2)
            {
                if (Items.CanUseItem(Tiamat)) Items.UseItem(Tiamat);
                if (Items.CanUseItem(Hydra)) Items.UseItem(Hydra);
            }
        }

        private void LastHit()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault(i => CanKill(i, SkillQ));
            if (minionObj != null && SkillQ.IsReady()) SkillQ.Cast(minionObj.Position, PacketCast);
        }

        private void Flee()
        {
            if (!SkillQ.IsReady()) return;
            if (SkillE.IsReady()) SkillE.Cast(Game.CursorPos, PacketCast);
            if (flagPos != default(Vector3)) SkillQ.Cast(flagPos, PacketCast);
        }

        private void ComboEQFlash()
        {
            Orbwalk(targetObj);
            if (targetObj == null || !FlashReady() || Player.Mana < SkillQ.Instance.ManaCost || Player.Distance(targetObj) > SkillQ.Range + 600) return;
            if (SkillE.IsReady()) SkillE.Cast(Game.CursorPos, PacketCast);
            if (SkillQ.IsReady() && flagPos != default(Vector3) && SkillQ.InRange(flagPos)) SkillQ.Cast(flagPos, PacketCast);
            if (flagPos != default(Vector3) && !SkillQ.IsReady() && Player.Distance(targetObj) < 600) CastFlash(targetObj.Position);
        }

        private void KillSteal()
        {
            var target = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(i => i.IsValidTarget(SkillQ.Range) && CanKill(i, SkillQ) && i != targetObj);
            if (target != null && SkillQ.IsReady()) SkillQ.Cast(target.Position, PacketCast);
        }

        private void UseItem(Obj_AI_Hero target)
        {
            if (Items.CanUseItem(Tiamat) && Player.CountEnemysInRange(350) >= 1) Items.UseItem(Tiamat);
            if (Items.CanUseItem(Hydra) && (Player.CountEnemysInRange(350) >= 2 || (Player.GetAutoAttackDamage(target) < target.Health && Player.CountEnemysInRange(350) == 1))) Items.UseItem(Hydra);
            if (Items.CanUseItem(Rand) && Player.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
        }
    }
}
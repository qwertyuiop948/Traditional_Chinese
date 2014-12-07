using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
/*
 * ToDo:
 * Q doesnt shoot much < fixed
 * Full combo burst <-- done
 * Useles gate <-- fixed
 * Add Fulldmg combo starting from hammer <-- done
 * Auto ignite if killabe/burst <-- done
 * More advanced Q calc area on hit
 * MuraMune support <-- done
 * Auto gapclosers E <-- done
 * GhostBBlade active <-- done
 * packet cast E <-- done 
 * 
 * 
 * Auto ks with QE <-done
 * Interupt channel spells <-done
 * Omen support <- done
 * 
 * 
 * */
using SharpDX;
using Color = System.Drawing.Color;


namespace MasterSharp
{
    internal class MasterSharp
    {
        public const string CharName = "MasterYi";

        public static Menu Config;

        public static Menu skillShotMenuq;
        public static Menu skillShotMenuw;


        public static List<Skillshot> DetectedSkillshots = new List<Skillshot>();

        public MasterSharp()
        {
            /* CallBAcks */
            CustomEvents.Game.OnGameLoad += onLoad;
        }

        private static void onLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != CharName)
                return;
            
            Game.PrintChat("MasterYi -  by DeTuKs");
            MasterYi.setSkillShots();
            try
            {
                TargetedSkills.setUpSkills();

                Config = new Menu("易大師-Sharp", "MasterYi", true);
                var orbwalkerMenu = new Menu("LX 走砍", "my_Orbwalker");
                LXOrbwalker.AddToMenu(orbwalkerMenu);
                Config.AddSubMenu(orbwalkerMenu);

                //TS
                Menu targetSelectorMenu = new Menu("目標 選擇", "Target Selector");
                SimpleTs.AddToMenu(targetSelectorMenu);
                Config.AddSubMenu(targetSelectorMenu);
                //Combo
                Config.AddSubMenu(new Menu("連招 Sharp", "combo"));
                Config.SubMenu("combo").AddItem(new MenuItem("comboItems", "使用物品（任何時機）")).SetValue(true);
                Config.SubMenu("combo").AddItem(new MenuItem("comboWreset", "使用W（被集火）")).SetValue(true);
                Config.SubMenu("combo").AddItem(new MenuItem("useQ", "使用 Q 突進")).SetValue(true);
                Config.SubMenu("combo").AddItem(new MenuItem("useE", "使用 E")).SetValue(true);
                Config.SubMenu("combo").AddItem(new MenuItem("useR", "使用 R")).SetValue(true);
                Config.SubMenu("combo").AddItem(new MenuItem("useSmite", "使用 懲戒")).SetValue(true);

                //Extra
                Config.AddSubMenu(new Menu("額外 Sharp", "extra"));
                Config.SubMenu("extra").AddItem(new MenuItem("packets", "使用 封包")).SetValue(false);

                Config.AddSubMenu(new Menu("躲避 指向性技能", "aShots"));
                //SmartW
                Config.SubMenu("aShots").AddItem(new MenuItem("smartW", "智能W 如果 沒有Q")).SetValue(true);
                Config.SubMenu("aShots").AddItem(new MenuItem("smartQDogue", "智能Q 躲技能")).SetValue(true);
                Config.SubMenu("aShots").AddItem(new MenuItem("wqOnDead", "危險技能自動Q或者W")).SetValue(false);
                skillShotMenuq = getSkilshotMenuQ();
                Config.SubMenu("aShots").AddSubMenu(skillShotMenuq);
                skillShotMenuw = getSkilshotMenuW();
                Config.SubMenu("aShots").AddSubMenu(skillShotMenuw);

                //Debug
                Config.AddSubMenu(new Menu("範圍顯示", "draw"));
                Config.SubMenu("draw").AddItem(new MenuItem("drawCir", "顯示 技能範圍")).SetValue(true);
                Config.SubMenu("draw").AddItem(new MenuItem("debugOn", "調試 stuff")).SetValue(new KeyBind('A', KeyBindType.Press));

                Config.AddToMainMenu();
                Drawing.OnDraw += onDraw;

                Game.OnGameUpdate += OnGameUpdate;

                Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

                SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
                SkillshotDetector.OnDeleteMissile += OnDeleteMissile;
                Game.OnGameProcessPacket += OnGameProcessPacket;
                CustomEvents.Unit.OnDash += onDash;
            }
            catch
            {
                Game.PrintChat("Oops. Something went wrong with Jayce - Sharp");
            }

        }

        private static void onDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (MasterYi.selectedTarget != null && sender.NetworkId == MasterYi.selectedTarget.NetworkId &&
                MasterYi.Q.IsReady() && LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Combo
                && sender.Distance(MasterYi.player)<=600)
                MasterYi.Q.Cast(sender);
        }

        public static void OnGameProcessPacket(GamePacketEventArgs args)
        {

            if (Config.Item("comboWreset").GetValue<bool>() && args.PacketData[0] == 0x65 && MasterYi.W.IsReady() && LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Combo)
            {

                // LogPacket(args);
                GamePacket gp = new GamePacket(args.PacketData);
                gp.Position = 1;
                Packet.S2C.Damage.Struct dmg = Packet.S2C.Damage.Decoded(args.PacketData);

                int targetID = gp.ReadInteger();
                int dType = (int) gp.ReadByte();
                int Unknown = gp.ReadShort();
                float DamageAmount = gp.ReadFloat();
                int TargetNetworkIdCopy = gp.ReadInteger();
                int SourceNetworkId = gp.ReadInteger();
                float dmga =
                    (float)
                        MasterYi.player.GetAutoAttackDamage(
                            ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(targetID));
                if (dmga - 10 > DamageAmount || dmga + 10 < DamageAmount)
                    return;
                if (MasterYi.player.NetworkId != dmg.SourceNetworkId && MasterYi.player.NetworkId == targetID)
                    return;
                Obj_AI_Base targ = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(dmg.TargetNetworkId);
                if ((int) dmg.Type == 12 || (int) dmg.Type == 4 || (int) dmg.Type == 3 )
                {
                    if (MasterYi.W.IsReady() && LXOrbwalker.InAutoAttackRange(targ))
                    {
                        MasterYi.W.Cast(targ.Position);
                       // LXOrbwalker.ResetAutoAttackTimer();
                    }
                }
                    Console.WriteLine("dtyoe: " + dType);
            }
        }

        public static Menu getSkilshotMenuQ()
        {
            //Create the skillshots submenus.
            var skillShotsQ = new Menu("Evade with Q", "aShotsSkillsQ");

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.Team != ObjectManager.Player.Team)
                {
                    foreach (var spell in SpellDatabase.Spells)
                    {
                        if (spell.ChampionName == hero.ChampionName)
                        {
                            var subMenu = new Menu(spell.MenuItemName, spell.MenuItemName);

                            subMenu.AddItem(
                                new MenuItem("qEvadeAll" + spell.MenuItemName, "Evade with Q Allways").SetValue(
                                    spell.IsDangerous));

                            subMenu.AddItem(
                                new MenuItem("qEvade" + spell.MenuItemName, "Evade with Q Combo").SetValue(
                                    spell.IsDangerous));

                            skillShotsQ.AddSubMenu(subMenu);
                        }
                    }
                }
            }
            return skillShotsQ;
        }

        public static Menu getSkilshotMenuW()
        {
            //Create the skillshots submenus.
            var skillShotsW = new Menu("Evade with W", "aShotsSkillsW");

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.Team != ObjectManager.Player.Team)
                {
                    foreach (var spell in SpellDatabase.Spells)
                    {
                        if (spell.ChampionName == hero.ChampionName)
                        {
                            var subMenu = new Menu(spell.MenuItemName, spell.MenuItemName);

                            subMenu.AddItem(
                                new MenuItem("wEvadeAll" + spell.MenuItemName, "Evade with W allways").SetValue(
                                    spell.IsDangerous));

                            subMenu.AddItem(
                                new MenuItem("wEvade" + spell.MenuItemName, "Evade with W COmbo").SetValue(
                                    spell.IsDangerous));

                            skillShotsW.AddSubMenu(subMenu);
                        }
                    }
                }
            }
            return skillShotsW;
        }

        public static bool skillShotMustBeEvaded(string Name)
        {
            if (skillShotMenuq.Item("qEvade" + Name) != null)
            {
                return skillShotMenuq.Item("qEvade" + Name).GetValue<bool>();
            }
            return true;
        }

        public static bool skillShotMustBeEvadedAllways(string Name)
        {
            if (skillShotMenuq.Item("qEvadeAll" + Name) != null)
            {
                return skillShotMenuq.Item("qEvadeAll" + Name).GetValue<bool>();
            }
            return true;
        }

        public static bool skillShotMustBeEvadedW(string Name)
        {
            if (skillShotMenuw.Item("wEvade" + Name) != null)
            {
                return skillShotMenuw.Item("wEvade" + Name).GetValue<bool>();
            }
            return true;
        }

        public static bool skillShotMustBeEvadedWAllways(string Name)
        {
            if (skillShotMenuw.Item("wEvade" + Name) != null)
            {
                return skillShotMenuw.Item("wEvade" + Name).GetValue<bool>();
            }
            return true;
        }

        private static void OnGameUpdate(EventArgs args)
        {

            if (Config.Item("debugOn").GetValue<KeyBind>().Active) //fullDMG
            {
                foreach (var buf in MasterYi.player.Buffs)
                {
                    Console.WriteLine(buf.Name);
                }
            }
            if (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Combo)
            {
                Obj_AI_Hero target = SimpleTs.GetTarget(800, SimpleTs.DamageType.Physical);
                LXOrbwalker.ForcedTarget = target;
                if(target != null)
                    MasterYi.selectedTarget = target;
                MasterYi.slayMaderDuker(target);
            }

            DetectedSkillshots.RemoveAll(skillshot => !skillshot.IsActive());
            foreach (var skillShot in DetectedSkillshots)
            {
                if (skillShot.IsAboutToHit(250, MasterYi.player))
                {
                    MasterYi.evadeSkillShot(skillShot);
                }
            }

            //anti buferino
            foreach (var buf in MasterYi.player.Buffs)
            {
                TargetedSkills.TargSkill skill = TargetedSkills.dagerousBuffs.FirstOrDefault(ob => ob.sName == buf.Name);
                if (skill != null)
                {
                   // Console.WriteLine("Evade: " + buf.Name);
                    MasterYi.evadeBuff(buf,skill);
                }
                // if(buf.EndTime-Game.Time<0.2f)
            }
            

        }

        private static void onDraw(EventArgs args)
        {

            if (!Config.Item("drawCir").GetValue<bool>())
                return;
            Utility.DrawCircle(MasterYi.player.Position, 600, Color.Green);
           
        }



        public static void OnProcessSpell(Obj_AI_Base obj, GameObjectProcessSpellCastEventArgs arg)
        {
            if (arg.Target != null && arg.Target.NetworkId == MasterYi.player.NetworkId)
            {
                Console.WriteLine(arg.SData.Name);
                if (obj is Obj_AI_Hero)
                {

                    var hero = (Obj_AI_Hero) obj;
                    var spellSlot = (hero.GetSpellSlot(arg.SData.Name));
                    TargetedSkills.TargSkill skill = TargetedSkills.targetedSkillsAll.FirstOrDefault(ob => ob.sName == arg.SData.Name);
                    if (skill != null)
                    {
                        Console.WriteLine("Evade: " + arg.SData.Name);
                        MasterYi.evadeDamage(skill.useQ,skill.useW,arg,skill.delay);
                    }

                }
            }
        }


        private static void OnDeleteMissile(Skillshot skillshot, Obj_SpellMissile missile)
        {
            if (skillshot.SpellData.SpellName == "VelkozQ")
            {
                var spellData = SpellDatabase.GetByName("VelkozQSplit");
                var direction = skillshot.Direction.Perpendicular();
                if (DetectedSkillshots.Count(s => s.SpellData.SpellName == "VelkozQSplit") == 0)
                {
                    for (var i = -1; i <= 1; i = i + 2)
                    {
                        var skillshotToAdd = new Skillshot(
                            DetectionType.ProcessSpell, spellData, Environment.TickCount, missile.Position.To2D(),
                            missile.Position.To2D() + i * direction * spellData.Range, skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                    }
                }
            }
        }

        private static void OnDetectSkillshot(Skillshot skillshot)
        {
            var alreadyAdded = false;

            foreach (var item in DetectedSkillshots)
            {
                if (item.SpellData.SpellName == skillshot.SpellData.SpellName &&
                    (item.Unit.NetworkId == skillshot.Unit.NetworkId &&
                     (skillshot.Direction).AngleBetween(item.Direction) < 5 &&
                     (skillshot.Start.Distance(item.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0)))
                {
                    alreadyAdded = true;
                }
            }

            //Check if the skillshot is from an ally.
            if (skillshot.Unit.Team == ObjectManager.Player.Team)
            {
                return;
            }

            //Check if the skillshot is too far away.
            if (skillshot.Start.Distance(ObjectManager.Player.ServerPosition.To2D()) >
                (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
            {
                return;
            }

            //Add the skillshot to the detected skillshot list.
            if (!alreadyAdded)
            {
                //Multiple skillshots like twisted fate Q.
                if (skillshot.DetectionType == DetectionType.ProcessSpell)
                {
                    if (skillshot.SpellData.MultipleNumber != -1)
                    {
                        var originalDirection = skillshot.Direction;

                        for (var i = -(skillshot.SpellData.MultipleNumber - 1) / 2;
                            i <= (skillshot.SpellData.MultipleNumber - 1) / 2;
                            i++)
                        {
                            var end = skillshot.Start +
                                      skillshot.SpellData.Range *
                                      originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i);
                            var skillshotToAdd = new Skillshot(
                                skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                                skillshot.Unit);

                            DetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "UFSlash")
                    {
                        skillshot.SpellData.MissileSpeed = 1600 + (int)skillshot.Unit.MoveSpeed;
                    }

                    if (skillshot.SpellData.Invert)
                    {
                        var newDirection = -(skillshot.End - skillshot.Start).Normalized();
                        var end = skillshot.Start + newDirection * skillshot.Start.Distance(skillshot.End);
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.Centered)
                    {
                        var start = skillshot.Start - skillshot.Direction * skillshot.SpellData.Range;
                        var end = skillshot.Start + skillshot.Direction * skillshot.SpellData.Range;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                    {
                        var angle = 60;
                        var edge1 =
                            (skillshot.End - skillshot.Unit.ServerPosition.To2D()).Rotated(
                                -angle / 2 * (float)Math.PI / 180);
                        var edge2 = edge1.Rotated(angle * (float)Math.PI / 180);

                        foreach (var minion in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            var v = minion.ServerPosition.To2D() - skillshot.Unit.ServerPosition.To2D();
                            if (minion.Name == "Seed" && edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0 &&
                                minion.Distance(skillshot.Unit) < 800 &&
                                (minion.Team != ObjectManager.Player.Team))
                            {
                                var start = minion.ServerPosition.To2D();
                                var end = skillshot.Unit.ServerPosition.To2D()
                                    .Extend(
                                        minion.ServerPosition.To2D(),
                                        skillshot.Unit.Distance(minion) > 200 ? 1300 : 1000);

                                var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                                    skillshot.Unit);
                                DetectedSkillshots.Add(skillshotToAdd);
                            }
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                    {
                        var start = skillshot.End - skillshot.Direction.Perpendicular() * 400;
                        var end = skillshot.End + skillshot.Direction.Perpendicular() * 400;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsQ")
                    {
                        var d1 = skillshot.Start.Distance(skillshot.End);
                        var d2 = d1 * 0.4f;
                        var d3 = d2 * 0.69f;


                        var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                        var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");

                        var bounce1Pos = skillshot.End + skillshot.Direction * d2;
                        var bounce2Pos = bounce1Pos + skillshot.Direction * d3;

                        bounce1SpellData.Delay =
                            (int)(skillshot.SpellData.Delay + d1 * 1000f / skillshot.SpellData.MissileSpeed + 500);
                        bounce2SpellData.Delay =
                            (int)(bounce1SpellData.Delay + d2 * 1000f / bounce1SpellData.MissileSpeed + 500);

                        var bounce1 = new Skillshot(
                            skillshot.DetectionType, bounce1SpellData, skillshot.StartTick, skillshot.End, bounce1Pos,
                            skillshot.Unit);
                        var bounce2 = new Skillshot(
                            skillshot.DetectionType, bounce2SpellData, skillshot.StartTick, bounce1Pos, bounce2Pos,
                            skillshot.Unit);

                        DetectedSkillshots.Add(bounce1);
                        DetectedSkillshots.Add(bounce2);
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsR")
                    {
                        skillshot.SpellData.Delay =
                            (int)(1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                    }

                    if (skillshot.SpellData.SpellName == "JarvanIVDragonStrike")
                    {
                        var endPos = new Vector2();

                        foreach (var s in DetectedSkillshots)
                        {
                            if (s.Unit.NetworkId == skillshot.Unit.NetworkId && s.SpellData.Slot == SpellSlot.E)
                            {
                                endPos = s.End;
                            }
                        }

                        foreach (var m in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            if (m.BaseSkinName == "jarvanivstandard" && m.Team == skillshot.Unit.Team &&
                                skillshot.IsDanger(m.Position.To2D()))
                            {
                                endPos = m.Position.To2D();
                            }
                        }

                        if (!endPos.IsValid())
                        {
                            return;
                        }

                        skillshot.End = endPos + 200 * (endPos - skillshot.Start).Normalized();
                        skillshot.Direction = (skillshot.End - skillshot.Start).Normalized();
                    }
                }

                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    var endCSpellData = SpellDatabase.GetByName("OriannaQend");

                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType, endCSpellData, skillshot.StartTick, skillshot.Start, skillshot.End,
                        skillshot.Unit);

                    DetectedSkillshots.Add(skillshotToAdd);
                }


                //Dont allow fow detection.
                if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    return;
                }
#if DEBUG
                Console.WriteLine(Environment.TickCount + "Adding new skillshot: " + skillshot.SpellData.SpellName);
#endif

                DetectedSkillshots.Add(skillshot);
            }
        }

    }
}

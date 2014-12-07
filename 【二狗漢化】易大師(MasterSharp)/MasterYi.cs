﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace MasterSharp
{
    class MasterYi
    {
        public static Obj_AI_Hero player = ObjectManager.Player;

        public static SummonerItems sumItems = new SummonerItems(player);

        public static Spellbook sBook = player.Spellbook;

        public static SpellDataInst Qdata = sBook.GetSpell(SpellSlot.Q);
        public static SpellDataInst Wdata = sBook.GetSpell(SpellSlot.W);
        public static SpellDataInst Edata = sBook.GetSpell(SpellSlot.E);
        public static SpellDataInst Rdata = sBook.GetSpell(SpellSlot.R);
        public static Spell Q = new Spell(SpellSlot.Q, 600);
        public static Spell W = new Spell(SpellSlot.W, 0);
        public static Spell E = new Spell(SpellSlot.E, 0);
        public static Spell R = new Spell(SpellSlot.R, 0);


        public static SpellSlot smite = SpellSlot.Unknown;


        public static Obj_AI_Base selectedTarget = null;

        public static void setSkillShots()
        {
            setupSmite();
        }
        public static void setupSmite()
        {
            if (player.SummonerSpellbook.GetSpell(SpellSlot.Summoner1).SData.Name.ToLower().Contains("smite"))
            {
                smite = SpellSlot.Summoner1;
            }
            else if (player.SummonerSpellbook.GetSpell(SpellSlot.Summoner2).SData.Name.ToLower().Contains("smite"))
            {
                smite = SpellSlot.Summoner2;
            }
        }

        public static void slayMaderDuker(Obj_AI_Base target)
        {
            try
            {
                if (target == null)
                    return;
                if(MasterSharp.Config.Item("useSmite").GetValue<bool>())
                    useSmiteOnTarget(target);
                useHydra(target);
                if (target.Distance(player) < 500)
                {
                    sumItems.cast(SummonerItems.ItemIds.Ghostblade);
                }
                if (target.Distance(player) < 500 && (player.Health / player.MaxHealth) * 100 < 85)
                {
                    sumItems.cast(SummonerItems.ItemIds.BotRK, target);

                }

                if(MasterSharp.Config.Item("useQ").GetValue<bool>())
                    useQSmart(target);
                if (MasterSharp.Config.Item("useE").GetValue<bool>())
                    useESmart(target);
                if (MasterSharp.Config.Item("useR").GetValue<bool>())
                    useRSmart(target);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void useHydra(Obj_AI_Base target)
        {

            if ((Items.CanUseItem(3074) || Items.CanUseItem(3074)) && target.Distance(player.ServerPosition) < (400 + target.BoundingRadius - 20))
            {
                Items.UseItem(3074, target);
                Items.UseItem(3077, target);
            }
        }
        public static void useQtoKill(Obj_AI_Base target)
        {
            if (Q.IsReady() && (target.Health <= Q.GetDamage(target) || iAmLow(0.20f)))
                Q.Cast(target, MasterSharp.Config.Item("packets").GetValue<bool>());
        }

        public static void useESmart(Obj_AI_Base target)
        {
            if (LXOrbwalker.InAutoAttackRange(target) && E.IsReady() && (aaToKill(target)>2 || iAmLow()))
                E.Cast(MasterSharp.Config.Item("packets").GetValue<bool>());
        }

        public static void useRSmart(Obj_AI_Base target)
        {
            if (LXOrbwalker.InAutoAttackRange(target) && R.IsReady() && aaToKill(target) > 5)
                R.Cast(MasterSharp.Config.Item("packets").GetValue<bool>());
        }

        public static void useQSmart(Obj_AI_Base target)
        {
            try
            {

                if (!Q.IsReady() || target.Path.Count() == 0 || !target.IsMoving)
                    return;
                Vector2 nextEnemPath = target.Path[0].To2D();
                var dist = player.Position.To2D().Distance(target.Position.To2D());
                var distToNext = nextEnemPath.Distance(player.Position.To2D());
                if (distToNext <= dist)
                    return;
                var msDif = player.MoveSpeed - target.MoveSpeed;
                if (msDif <= 0 && !LXOrbwalker.InAutoAttackRange(target) && LXOrbwalker.CanAttack())
                    Q.Cast(target);

                var reachIn = dist/msDif;
                if(reachIn>4)
                    Q.Cast(target);
            }
            catch (Exception)
            {
                throw;
            }

        }

        public static void useSmiteOnTarget(Obj_AI_Base target)
        {
            if (target.Distance(player,true)<=700*700 &&(yiGotItemRange(3714, 3718) || yiGotItemRange(3706, 3710)))
            {
                if (player.SummonerSpellbook.CanUseSpell(smite) == SpellState.Ready)
                {
                    player.SummonerSpellbook.CastSpell(smite, target);
                }
            }
        }

        public static bool iAmLow(float lownes = .25f)
        {
            return player.Health / player.MaxHealth < lownes;
        }

        public static int aaToKill(Obj_AI_Base target)
        {
            return 1+(int)(target.Health/player.GetAutoAttackDamage(target));
        }

        public static void evadeBuff(BuffInstance buf,TargetedSkills.TargSkill skill)
        {
            if (Q.IsReady() && jumpEnesAround() != 0 && buf.EndTime - Game.Time < skill.delay / 1000)
            {

                //Console.WriteLine("evade buuf");
                useQonBest();
            }
            else if (W.IsReady() && (!Q.IsReady() || jumpEnesAround() != 0 )&& buf.EndTime - Game.Time < 0.4f)
            {
                var dontMove = 400;
                LXOrbwalker.cantMoveTill = Environment.TickCount + (int)dontMove;
                W.Cast();
            }


        }

        public static void evadeDamage(int useQ, int useW,GameObjectProcessSpellCastEventArgs psCast,int delay = 250)
        {
            if (useQ != 0 && Q.IsReady() && jumpEnesAround() != 0)
            {
                if (delay != 0)
                    Utility.DelayAction.Add(delay, useQonBest);
                else
                    useQonBest();
            }
            else if (useW != 0 && W.IsReady())
            {
                var dontMove = (psCast.TimeCast > 2) ? 2000 : psCast.TimeCast*1000;
                LXOrbwalker.cantMoveTill = Environment.TickCount +(int) dontMove;
                W.Cast();
            }


        }

        public static int jumpEnesAround()
        {

            return ObjectManager.Get<Obj_AI_Base>().Count(ob => ob.IsEnemy && !(ob is FollowerObject) && (ob is Obj_AI_Minion || ob is Obj_AI_Hero) &&
                                                                ob.Distance(player) < 600 && !ob.IsDead);
        }

        public static void evadeSkillShot(Skillshot sShot)
        {
            var sd = SpellDatabase.GetByMissileName(sShot.SpellData.MissileSpellName);
            if (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Combo && (MasterSharp.skillShotMustBeEvaded(sd.MenuItemName) || MasterSharp.skillShotMustBeEvadedW(sd.MenuItemName)))
            {
                float spellDamage = (float)sShot.Unit.GetSpellDamage(player, sd.SpellName);
                int procHp = (int)((spellDamage / player.MaxHealth) * 100);
                bool willKill = player.Health <= spellDamage;
                if (Q.IsReady() && jumpEnesAround() != 0 && (MasterSharp.skillShotMustBeEvaded(sd.MenuItemName)) || willKill)
                {
                    useQonBest();
                }
                else if ((!Q.IsReady(150) || !MasterSharp.skillShotMustBeEvaded(sd.MenuItemName)) && W.IsReady() && (MasterSharp.skillShotMustBeEvadedW(sd.MenuItemName) || willKill))
                {
                    LXOrbwalker.cantMoveTill = Environment.TickCount + 500;
                    W.Cast();
                }
            }

            if (LXOrbwalker.CurrentMode != LXOrbwalker.Mode.None && (MasterSharp.skillShotMustBeEvadedAllways(sd.MenuItemName) || MasterSharp.skillShotMustBeEvadedWAllways(sd.MenuItemName)))
            {
                float spellDamage = (float)sShot.Unit.GetSpellDamage(player, sd.SpellName);
                bool willKill = player.Health <= spellDamage;
                if (Q.IsReady() && jumpEnesAround() != 0 && (MasterSharp.skillShotMustBeEvadedAllways(sd.MenuItemName) || willKill))
                {
                    useQonBest();
                    return;
                }
                else if ((!Q.IsReady() || !MasterSharp.skillShotMustBeEvadedAllways(sd.MenuItemName)) && W.IsReady() && (MasterSharp.skillShotMustBeEvadedWAllways(sd.MenuItemName) || willKill))
                {
                    LXOrbwalker.cantMoveTill = Environment.TickCount + 500;
                    W.Cast();
                    return;
                }
            }


        }



        public static void useQonBest()
        {
            try
            {
                if (!Q.IsReady())
                {
                    Console.WriteLine("Fuk uo here ");
                    return;
                }
                if (selectedTarget != null)
                {

                    if (selectedTarget.Distance(player) < 600)
                    {
                        Console.WriteLine("Q on targ ");
                        Q.Cast(selectedTarget, MasterSharp.Config.Item("packets").GetValue<bool>());
                        return;
                    }

                    var bestOther =
                        ObjectManager.Get<Obj_AI_Base>()
                            .Where(
                                ob =>
                                    ob.IsEnemy && (ob is Obj_AI_Minion || ob is Obj_AI_Hero) &&
                                    ob.Distance(player) < 600 && !ob.IsDead)
                            .OrderBy(ob => ob.Distance(selectedTarget, true)).FirstOrDefault();
                    Console.WriteLine("do shit? " + bestOther.Name);

                    if (bestOther != null)
                    {
                        Q.Cast(bestOther, MasterSharp.Config.Item("packets").GetValue<bool>());
                    }
                }
                else
                {
                    var bestOther =
                        ObjectManager.Get<Obj_AI_Base>()
                            .Where(
                                ob =>
                                    ob.IsEnemy && !(ob is FollowerObject)  && (ob is Obj_AI_Minion || ob is Obj_AI_Hero) &&
                                    ob.Distance(player) < 600 && !ob.IsDead)
                            .OrderBy(ob => ob.Distance(Game.CursorPos, true)).FirstOrDefault();
                    Console.WriteLine("do shit? " + bestOther.Name);

                    if (bestOther != null)
                    {
                        Q.Cast(bestOther, MasterSharp.Config.Item("packets").GetValue<bool>());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static bool yiGotItemRange(int from, int to)
        {
            return player.InventoryItems.Any(item => (int)item.Id >= @from && (int)item.Id <= to);
        }
    }
}

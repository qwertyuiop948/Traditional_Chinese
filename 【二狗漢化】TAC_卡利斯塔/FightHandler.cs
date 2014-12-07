using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace TAC_Kalista
{
    class FightHandler
    {
        internal static Obj_AI_Hero soul = null;
        public static void OnCombo()
        {
            if (MenuHandler.Config.Item("useItems").GetValue<KeyBind>().Active) ItemHandler.useItem();

            if (MenuHandler.Config.Item("UseQAC").GetValue<bool>() || 
                    (ObjectManager.Get<Obj_AI_Hero>().Any(
                        hero => hero.IsValidTarget(SkillHandler.E.Range+400)
                            && hero.Health < (MathHandler.getRealDamage(hero) - SkillHandler.Q.GetDamage(hero))
                )))
            {
                customQCast(SimpleTs.GetTarget(SkillHandler.Q.Range, SimpleTs.DamageType.Physical));
            }

            if (SkillHandler.E.IsReady() && (( ObjectManager.Get<Obj_AI_Hero>().Any(hero => hero.IsValidTarget(SkillHandler.E.Range)
                && hero.Buffs.FirstOrDefault(b => b.Name.ToLower() == "kalistaexpungemarker").Count >= MenuHandler.Config.Item("minE").GetValue<Slider>().Value
                            ) && MenuHandler.Config.Item("minEE").GetValue<bool>()) 
                            // auto e
                            || (MenuHandler.Config.Item("UseEAC").GetValue<bool>()
                    && ObjectManager.Get<Obj_AI_Hero>().Any(hero => hero.IsValidTarget(SkillHandler.E.Range)
                           && hero.Health < MathHandler.getRealDamage(hero)))
                            || (SkillHandler.Q.IsReady() && MenuHandler.Config.Item("UseEACSlow").GetValue<bool>()
                        && ObjectManager.Get<Obj_AI_Hero>().Any(hero => hero.IsValidTarget(SkillHandler.E.Range) 
                            && ObjectManager.Player.Distance(hero) > (SkillHandler.E.Range - 110)
                                && ObjectManager.Player.Distance(hero) < SkillHandler.E.Range
                                    && hero.CountEnemysInRange((int)SkillHandler.E.Range) <= MenuHandler.Config.Item("UseEACSlowT").GetValue<Slider>().Value
                            )

                        )))
            {
                SkillHandler.E.Cast();
            }
            if (SkillHandler.E.IsReady())
                MathHandler.castMinionE(SimpleTs.GetTarget(SkillHandler.E.Range, SimpleTs.DamageType.Physical));
        }
        public static void OnHarass()
        {
            Obj_AI_Hero target = SimpleTs.GetTarget(SkillHandler.E.Range, SimpleTs.DamageType.Physical);
            float percentManaAfterQ = 100 * ((ObjectManager.Player.Mana - SkillHandler.Q.Instance.ManaCost) / ObjectManager.Player.MaxMana);
            float percentManaAfterE = 100 * ((ObjectManager.Player.Mana - SkillHandler.E.Instance.ManaCost) / ObjectManager.Player.MaxMana);
            int minPercentMana = MenuHandler.Config.SubMenu("Harass").Item("manaPercent").GetValue<Slider>().Value;

            if (percentManaAfterQ >= minPercentMana && MenuHandler.Config.Item("harassQ").GetValue<bool>() && SkillHandler.Q.IsReady()) FightHandler.customQCast(target);
            if (SkillHandler.E.IsReady()
                    && ObjectManager.Get<Obj_AI_Hero>().Any(
                        hero => hero.IsValidTarget(SkillHandler.E.Range) 
                            &&
                                hero.Buffs.FirstOrDefault(b => b.Name.ToLower() == "kalistaexpungemarker").Count >= MenuHandler.Config.Item("stackE").GetValue<Slider>().Value                    
                            )
                 &&
                    percentManaAfterE >= minPercentMana)
            {
                SkillHandler.E.Cast(Kalista.packetCast);
            }
            if(SkillHandler.E.IsReady() && target.IsValidTarget(SkillHandler.E.Range))
            {
                MathHandler.castMinionE(SimpleTs.GetTarget(SkillHandler.E.Range,SimpleTs.DamageType.Physical));
            }
        }
        /**
         * @author Hellsing
         * */
        public static void OnLaneClear()
        {
            if (MenuHandler.Config.Item("enableClear").GetValue<bool>())// && MenuHandler.Config.Item("useEwc").GetValue<bool>() && SkillHandler.E.IsReady())
            {
                if (MenuHandler.Config.Item("wcQ").GetValue<bool>() && SkillHandler.Q.IsReady())
                {
                    var minions = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.BaseSkinName.Contains("Minion") && m.IsValidTarget(SkillHandler.Q.Range)).ToList();
                    if (minions.Count >= 3)
                    {
                        minions.Sort((m1, m2) => m2.Distance(ObjectManager.Player, true).CompareTo(m1.Distance(ObjectManager.Player, true)));
                        int bestHitCount = 0;
                        PredictionOutput bestResult = null;
                        foreach (var minion in minions)
                        {
                            var prediction = SkillHandler.Q.GetPrediction(minion);
                            var targets = prediction.CollisionObjects;
                            targets.Sort((t1, t2) => t1.Distance(ObjectManager.Player, true).CompareTo(t2.Distance(ObjectManager.Player, true)));
                            targets.Add(minion);
                            for (int i = 0; i < targets.Count; i++)
                            {
                                if (ObjectManager.Player.GetSpellDamage(targets[i], SpellSlot.Q) * 0.9 < targets[i].Health || i == targets.Count)
                                {
                                    if (i >= 3 && (bestResult == null || bestHitCount < i))
                                    {
                                        bestHitCount = i;
                                        bestResult = prediction;
                                    }
                                    break;
                                }
                            }
                        }
                        if (bestResult != null) SkillHandler.Q.Cast(bestResult.CastPosition);
                    }
                }

                if (MenuHandler.Config.Item("wcE").GetValue<bool>() && SkillHandler.E.IsReady())
                {
                    List<Obj_AI_Base> minions = MinionManager.GetMinions(ObjectManager.Player.Position, SkillHandler.E.Range);
                    if (minions.Count >= 3)
                    {
                        int conditionMet = 0;
                        foreach (var minion in minions)
                        {
                            if (MathHandler.getRealDamage(minion) * 0.9 > minion.Health)
                                conditionMet++;
                        }
                        if (conditionMet >= 3) SkillHandler.E.Cast(true);
                    }
                    IEnumerable<Obj_AI_Base> minionsBig = MinionManager.GetMinions(ObjectManager.Player.Position, SkillHandler.E.Range).Where(m => m.BaseSkinName.Contains("MinionSiege"));
                    foreach (var minion in minionsBig)
                    {
                        if (MathHandler.getRealDamage(minion) > minion.Health)
                        {
                            SkillHandler.E.Cast(true);
                            break;
                        }
                    }
                }
            }
        }
        /**
         * @author Hellsing
         * */
        public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "KalistaExpungeWrapper") 
                Utility.DelayAction.Add(250,Orbwalking.ResetAutoAttackTimer);
        }

        public static void saveSould()
        {
            if (soul == null)
            {
                foreach (var ally in
                    from ally in ObjectManager.Get<Obj_AI_Hero>().Where(tx => tx.IsAlly && !tx.IsDead && !tx.IsMe)
                    where ObjectManager.Player.Distance(ally) <= SkillHandler.R.Range
                    from buff in ally.Buffs
                    where ally.HasBuff("kalistacoopstrikeally")
                    select ally)
                {
                    soul = ally;
                    break;
                }
            }
            else
            {
                if((soul.Health/soul.MaxHealth) > MenuHandler.Config.Item("soulHP").GetValue<Slider>().Value 
                        && soul.CountEnemysInRange((int)Orbwalking.GetRealAutoAttackRange(soul)) >= MenuHandler.Config.Item("soulEnemyCount").GetValue<Slider>().Value)
                {
                    SkillHandler.R.Cast(Kalista.packetCast);
                }
            }
        }

        internal static void AntiGapCloser(ActiveGapcloser gapcloser)
        {
            if(MenuHandler.orb.ActiveMode == Orbwalking.OrbwalkingMode.Combo && MenuHandler.Config.Item("antiGapPrevent").GetValue<bool>()) return;
            if (MenuHandler.Config.Item("antiGap").GetValue<bool>() && gapcloser.Sender.IsValidTarget(MenuHandler.Config.Item("antiGapRange").GetValue<Slider>().Value))
            {
                if (SkillHandler.Q.IsReady() && gapcloser.Sender.IsValidTarget(MenuHandler.Config.Item("antiGapRange").GetValue<Slider>().Value))
                {
                    SkillHandler.Q.CastOnUnit(gapcloser.Sender, Kalista.packetCast);
                    Orbwalking.Orbwalk(gapcloser.Sender, Game.CursorPos);
                }
            }
        }
        public static void customQCast(Obj_AI_Hero target)
        {
            if (!SkillHandler.Q.IsReady() || target == null) return;
            if ((100 * ((ObjectManager.Player.Mana - SkillHandler.Q.Instance.ManaCost) / ObjectManager.Player.MaxMana)) <= 3) return; // && ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) < target.Health) return;

            PredictionOutput po = SkillHandler.Q.GetPrediction(target);
            int canCast = 0;
            switch (MenuHandler.Config.Item("UseQACM").GetValue<StringList>().SelectedIndex)
            {
                case 1:
                    if (po.Hitchance >= HitChance.Low) canCast = 1;
                    break;
                case 2:
                    if (po.Hitchance >= HitChance.Medium) canCast = 1;
                    break;
                case 3:
                    if (po.Hitchance >= HitChance.High) canCast = 1;
                    break;
            }
            if (canCast != 0 && ObjectManager.Player.Distance(po.UnitPosition) < SkillHandler.Q.Range)
            {
                SkillHandler.Q.Cast(po.CastPosition, Kalista.packetCast);
            }
            else if (po.Hitchance == HitChance.Collision)
            {
                List<Obj_AI_Base> coll = po.CollisionObjects;
                Obj_AI_Base goal = coll.FirstOrDefault(obj => SkillHandler.Q.GetPrediction(obj).Hitchance >= HitChance.Medium && SkillHandler.Q.GetDamage(target) > obj.Health);
                if (goal != null) SkillHandler.Q.Cast(goal, Kalista.packetCast);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace TAC_Kalista
{
    class MathHandler
    {
        public static float getDamageToTarget(Obj_AI_Base target)
        {
            double damage = 0;//ObjectManager.Player.GetAutoAttackDamage(target);
            if (SkillHandler.Q.IsReady() && ObjectManager.Player.Distance(target) < SkillHandler.Q.Range)
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
            if (SkillHandler.E.IsReady() && ObjectManager.Player.Distance(target) < SkillHandler.E.Range)
                damage += getRealDamage(target);
            return (float)damage;
        }
        internal static void castMinionE(Obj_AI_Base target)
        {
            if (ObjectManager.Get<Obj_AI_Hero>().Any(
                        hero => hero.IsValidTarget(SkillHandler.E.Range)
                            &&
                                hero.Buffs.FirstOrDefault(b => b.Name.ToLower() == "kalistaexpungemarker").Count >= 1
                            ))
            {
                List<Obj_AI_Base> minions = MinionManager.GetMinions(ObjectManager.Player.Position, SkillHandler.E.Range,MinionTypes.All,MinionTeam.Enemy,MinionOrderTypes.Health);
                foreach (var minion in minions)
                {
                    if (MathHandler.getRealDamage(minion) * 0.9 > minion.Health)
                    {
                        SkillHandler.E.Cast(Kalista.packetCast);
                        break;
                    }
                }
            }        
        }

        public static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
        }
        public static float GetPlayerManaPercentage()
        {
            return ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;
        }

        public static double getRealDamage(Obj_AI_Base target)
        {
            return ObjectManager.Player.GetSpellDamage(target, SpellSlot.E) * 0.9;
        }
    }
}

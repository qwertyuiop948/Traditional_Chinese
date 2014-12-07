using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;

namespace MasterSharp
{
    class TargetedSkills
    {
        internal class TargSkill
        {
            public string sName;
            public int useQ;
            public int useW;
            public int danger;
            public int delay = 250;
            public GameObjectProcessSpellCastEventArgs spell;

            public TargSkill(string name, int q, int w,int d)
            {
                sName = name;
                useQ = q;
                useW = w;
                danger = d;
            }

            public TargSkill(string name, int q, int w, int d,int del)
            {
                sName = name;
                useQ = q;
                useW = w;
                danger = d;
                delay = del;
            }
        }


        public static List<TargSkill> targetedSkillsAll = new List<TargSkill>();

        public static List<TargSkill> dagerousBuffs = new List<TargSkill>();
        /*{
            "timebombenemybuff",
            "",
            "NocturneUnspeakableHorror"
        };*/



        public static void setUpSkills()
        {
            //Bufs
            dagerousBuffs.Add(new TargSkill("timebombenemybuff", 1, 1, 1, 300));
            dagerousBuffs.Add(new TargSkill("karthusfallenonetarget", 1, 1, 1, 300));
            dagerousBuffs.Add(new TargSkill("NocturneUnspeakableHorror", 1, 0, 1, 500));

            // name of spellName, Q use, W use --- 2-prioritize more , 1- prioritize less 0 dont use
            targetedSkillsAll.Add(new TargSkill("SyndraR", 0, 1, 1));
            targetedSkillsAll.Add(new TargSkill("VayneCondemn", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Dazzle", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Overload", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("IceBlast", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("LeblancChaosOrb", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("JudicatorReckoning", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("KatarinaQ", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("NullLance", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("FiddlesticksDarkWind", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("CaitlynHeadshotMissile", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("BrandWildfire", 2, 1, 1,150));
            targetedSkillsAll.Add(new TargSkill("Disintegrate", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("Frostbite", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("AkaliMota", 2, 1, 0));
            //infiniteduresschannel  InfiniteDuress
            targetedSkillsAll.Add(new TargSkill("InfiniteDuress", 2, 0, 1,0));
            targetedSkillsAll.Add(new TargSkill("PantheonW", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("blindingdart", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("JayceToTheSkies", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("dariusexecute", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("ireliaequilibriumstrike", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("maokaiunstablegrowth", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("missfortunericochetshot", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("nautilusgandline", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("runeprison", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("goldcardpreattack", 2, 0, 1,0));
            targetedSkillsAll.Add(new TargSkill("vir", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("zedult", 2, 0, 1));
           // targetedSkillsAll.Add(new TargSkill("NocturneUnspeakableHorror", 2, 0, 1,0));
        }

    }
}

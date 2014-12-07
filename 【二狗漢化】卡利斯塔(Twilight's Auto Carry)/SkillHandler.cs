using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using SharpDX;
namespace TAC_Kalista
{
    class SkillHandler
    {
        public static Spell Q, W, E, R;
        public static Spell[] spellList = { Q, W, E, R };
        public static void init()
        {
            Q = new Spell(SpellSlot.Q, 1450);
            W = new Spell(SpellSlot.W, 5500);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1200);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
        }
    }
}

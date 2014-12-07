using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace TAC_Kalista
{
    class Kalista
    {
        public static bool packetCast;
        public static bool debug;
        public static bool drawings;
        public static bool canexport = true;
        static void Main(string[] args)
        {
            Game.PrintChat("---------------------------");
            Game.PrintChat("[<font color='#FF0000'>v3.7</font>]<font color='#7A6EFF'>Twilight's Auto Carry:</font> <font color='#86E5E1'>鐏甸瓊涔嬬煕  鍔犺級鎴愬姛锛佹饥鍖朾y浜岀嫍锛丵Q缇361630847!</font>");
            CustomEvents.Game.OnGameLoad += Load;
        }
        public static void Load(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Kalista") return;
            SkillHandler.init();
            ItemHandler.init();
            MenuHandler.init();
            DrawingHandler.init();
            Game.OnGameUpdate += OnGameUpdateModes;
            AntiGapcloser.OnEnemyGapcloser += FightHandler.AntiGapCloser;
            Obj_AI_Hero.OnProcessSpellCast += FightHandler.OnProcessSpellCast;
        }
        public static void OnGameUpdateModes(EventArgs args)
        {
            drawings = MenuHandler.Config.Item("enableDrawings").GetValue<bool>();
            debug = MenuHandler.Config.Item("debug").GetValue<bool>();
            packetCast = MenuHandler.Config.Item("Packets").GetValue<bool>();
            if (ObjectManager.Player.HasBuff("Recall")) return;

            if (MenuHandler.Config.Item("Orbwalk").GetValue<KeyBind>().Active)
            {
                FightHandler.OnCombo();
            }
            else if (MenuHandler.Config.Item("Farm").GetValue<KeyBind>().Active)
            {
                FightHandler.OnHarass();
            }
            else if (MenuHandler.Config.Item("LaneClear").GetValue<KeyBind>().Active)
            {
                FightHandler.OnLaneClear();
            }
            if (MenuHandler.Config.Item("saveSould").GetValue<bool>())
            {
                FightHandler.saveSould();
            }
            SmiteHandler.Init();

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;

namespace LxOrbwalker_Standalone
{
    class Program
    {
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }


        public static void Game_OnGameLoad(EventArgs args)
        {
            Game.PrintChat("<font color='#FF0000'>Lx Orbwalker standalone</font> loaded. - <font color='#5882FA'>E2Slayer</font>");
            var menu = new Menu("Lx Orbwalker", "my_mainmenu", true);
            var orbwalkerMenu = new Menu("Lx Orbwalker", "my_Orbwalker");
            LXOrbwalker.AddToMenu(orbwalkerMenu);
            menu.AddSubMenu(orbwalkerMenu);
            menu.AddToMainMenu();
        }

    }
}

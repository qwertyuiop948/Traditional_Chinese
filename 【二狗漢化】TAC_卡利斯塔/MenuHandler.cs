using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace TAC_Kalista
{
    class MenuHandler
    {
        public static Menu Config;
        internal static Orbwalking.Orbwalker orb;
        public static void init()
        {
            Config = new Menu("Twilight卡利斯塔 重做", "Kalista", true);

            var targetselectormenu = new Menu("目標 選擇", "Common_TargetSelector");
            SimpleTs.AddToMenu(targetselectormenu);
            Config.AddSubMenu(targetselectormenu);

            Menu orbwalker = new Menu("走砍", "orbwalker");
            orb = new Orbwalking.Orbwalker(orbwalker);
            Config.AddSubMenu(orbwalker);

            Config.AddSubMenu(new Menu("連招 設置", "ac"));
            
            Config.SubMenu("ac").AddSubMenu(new Menu("技能","skillUsage"));
            Config.SubMenu("ac").SubMenu("skillUsage").AddItem(new MenuItem("UseQAC", "使用 Q").SetValue(true));
            Config.SubMenu("ac").SubMenu("skillUsage").AddItem(new MenuItem("UseEAC", "使用 E").SetValue(true));
            
            Config.SubMenu("ac").AddSubMenu(new Menu("技能 設置","skillConfiguration"));
            Config.SubMenu("ac").SubMenu("skillConfiguration").AddItem(new MenuItem("UseQACM", "使用 Q 範圍").SetValue(new StringList(new[] { "遠", "正常", "近" }, 2)));
            Config.SubMenu("ac").SubMenu("skillConfiguration").AddItem(new MenuItem("E4K", "使用 E（4層可擊殺）").SetValue(true));
            Config.SubMenu("ac").SubMenu("skillConfiguration").AddItem(new MenuItem("UseEACSlow", "使用 E 減速目標").SetValue(false));
            Config.SubMenu("ac").SubMenu("skillConfiguration").AddItem(new MenuItem("UseEACSlowT", "使用E減速|敵人數量").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("ac").SubMenu("skillConfiguration").AddItem(new MenuItem("minE", "使用E|被動最低層數").SetValue(new Slider(1, 1, 20)));
            Config.SubMenu("ac").SubMenu("skillConfiguration").AddItem(new MenuItem("minEE", "啟用 自動 E").SetValue(false));
            
            Config.SubMenu("ac").AddSubMenu(new Menu("物品 設置","itemsAC"));
            Config.SubMenu("ac").SubMenu("itemsAC").AddItem(new MenuItem("useItems", "使用 物品").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Toggle)));

            Config.SubMenu("ac").SubMenu("itemsAC").AddItem(new MenuItem("allIn", "所有 物品").SetValue(new KeyBind("U".ToCharArray()[0], KeyBindType.Toggle)));
//            Config.SubMenu("ac").SubMenu("itemsAC").AddItem(new MenuItem("allInAt", "Auto All in when X hero").SetValue(new Slider(2, 1, 5)));
            
            Config.SubMenu("ac").SubMenu("itemsAC").AddItem(new MenuItem("BOTRK", "使用 破敗").SetValue(true));
            Config.SubMenu("ac").SubMenu("itemsAC").AddItem(new MenuItem("GHOSTBLADE", "使用 鬼刀").SetValue(true));
            Config.SubMenu("ac").SubMenu("itemsAC").AddItem(new MenuItem("SWORD", "使用 神聖之劍").SetValue(true));

            Config.SubMenu("ac").SubMenu("itemsAC").AddSubMenu(new Menu("水銀 設置", "QSS"));
            Config.SubMenu("ac").SubMenu("itemsAC").SubMenu("QSS").AddItem(new MenuItem("AnyStun", "任何 眩暈").SetValue(true));
            Config.SubMenu("ac").SubMenu("itemsAC").SubMenu("QSS").AddItem(new MenuItem("AnySnare", "任何 陷阱").SetValue(true));
            Config.SubMenu("ac").SubMenu("itemsAC").SubMenu("QSS").AddItem(new MenuItem("AnyTaunt", "任何 嘲諷").SetValue(true));
            foreach (var t in ItemHandler.BuffList)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                {
                    if (t.ChampionName == enemy.ChampionName)
                        Config.SubMenu("ac").SubMenu("itemsAC").SubMenu("QSS").AddItem(new MenuItem(t.BuffName, t.DisplayName).SetValue(t.DefaultValue));
                }
            }

            Config.AddSubMenu(new Menu("雜項 設置", "misc"));
            Config.SubMenu("misc").AddItem(new MenuItem("saveSould", "保留 R").SetValue(true));
            Config.SubMenu("misc").AddItem(new MenuItem("soulHP", "保留R|自己血量").SetValue(new Slider(15,1,100)));
            Config.SubMenu("misc").AddItem(new MenuItem("soulEnemyCount", "使用R|敵人數量").SetValue(new Slider(3, 1, 5)));
            Config.SubMenu("misc").AddItem(new MenuItem("antiGap", "使用R防止突進").SetValue(false));
            Config.SubMenu("misc").AddItem(new MenuItem("antiGapRange", "阻止敵人突進範圍").SetValue(new Slider(300, 300, 400)));
            Config.SubMenu("misc").AddItem(new MenuItem("antiGapPrevent", "連招中保留R防止突進").SetValue(true));

            Config.AddSubMenu(new Menu("騷擾 設置", "harass"));
            Config.SubMenu("harass").AddItem(new MenuItem("harassQ", "使用 Q").SetValue(true));
            Config.SubMenu("harass").AddItem(new MenuItem("stackE", "使用E（被動層數）").SetValue(new Slider(1, 1, 10)));
            Config.SubMenu("harass").AddItem(new MenuItem("manaPercent", "騷擾最低藍量").SetValue(new Slider(40, 1, 100)));

            Config.AddSubMenu(new Menu("清線 設置", "wc"));
            Config.SubMenu("wc").AddItem(new MenuItem("wcQ", "使用 Q").SetValue(true));
            Config.SubMenu("wc").AddItem(new MenuItem("wcE", "使用 E").SetValue(true));
            Config.SubMenu("wc").AddItem(new MenuItem("enableClear", "啟用 快速清線").SetValue(false));
            
            Config.AddSubMenu(new Menu("懲戒 設置", "smite"));
            Config.SubMenu("smite").AddItem(new MenuItem("SRU_Baron", "懲戒 大龍").SetValue(true));
            Config.SubMenu("smite").AddItem(new MenuItem("SRU_Dragon", "懲戒 小龍").SetValue(true));
            Config.SubMenu("smite").AddItem(new MenuItem("SRU_Gromp", "懲戒 石甲蟲").SetValue(false));
            Config.SubMenu("smite").AddItem(new MenuItem("SRU_Murkwolf", "懲戒 暗影狼").SetValue(false));
            Config.SubMenu("smite").AddItem(new MenuItem("SRU_Krug", "懲戒 魔沼蛙").SetValue(false));
            Config.SubMenu("smite").AddItem(new MenuItem("SRU_Razorbeak", "懲戒 鋒喙鳥").SetValue(false));
            Config.SubMenu("smite").AddItem(new MenuItem("Sru_Crab", "懲戒 河蟹").SetValue(false));
            Config.SubMenu("smite").AddItem(new MenuItem("smite", "啟用 自動懲戒").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("跳躍 設置", "wh"));
            Config.SubMenu("wh").AddItem(new MenuItem("JumpTo", "跳躍 鍵位(保持)").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("顯示 設置", "Drawings"));
            Config.SubMenu("Drawings").AddSubMenu(new Menu("範圍", "range"));

            Config.SubMenu("Drawings").SubMenu("range").AddItem(new MenuItem("QRange", "Q 範圍").SetValue(new Circle(true, Color.FromArgb(100, Color.Red))));
            Config.SubMenu("Drawings").SubMenu("range").AddItem(new MenuItem("WRange", "W 範圍").SetValue(new Circle(false, Color.FromArgb(100, Color.Coral))));
            Config.SubMenu("Drawings").SubMenu("range").AddItem(new MenuItem("ERange", "E 範圍").SetValue(new Circle(true, Color.FromArgb(100, Color.BlueViolet))));
            Config.SubMenu("Drawings").SubMenu("range").AddItem(new MenuItem("drawESlow", "E 減速 範圍").SetValue(true));
            Config.SubMenu("Drawings").SubMenu("range").AddItem(new MenuItem("RRange", "R 範圍").SetValue(new Circle(false, Color.FromArgb(100, Color.Blue))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("drawHp", "顯示組合連招血量傷害")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("drawStacks", "顯示E被動計數")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("enableDrawings", "禁用 所有範圍").SetValue(true));          

            Config.AddItem(new MenuItem("Packets", "使用封包").SetValue(true));

            Config.AddItem(new MenuItem("debug", "調試").SetValue(false));
            
            Config.AddToMainMenu();

        }
    }
}

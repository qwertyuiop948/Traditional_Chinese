#region License

/*
 Copyright 2014 - 2014 Nikita Bernthaler
 Trinkets.cs is part of SFXUtility.
 
 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace SFXUtility.Feature
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Class;
    using IoCContainer;
    using LeagueSharp;
    using LeagueSharp.Common;

    #endregion

    internal class Trinkets : Base
    {
        #region Fields

        private const float CheckInterval = 125f;
        private float _lastCheck = Environment.TickCount;

        #endregion

        #region Constructors

        public Trinkets(IContainer container)
            : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        #endregion

        #region Properties

        public override bool Enabled
        {
            get { return Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return "無為漢化─飾品"; }
        }

        #endregion

        #region Methods

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                Logger.Prefix = string.Format("{0} - {1}", BaseName, Name);

                Menu = new Menu(Name, Name);

                var timersMenu = new Menu("定時", Name + "Timers");
                timersMenu.AddItem(
                    new MenuItem(Name + "TimersWardingTotem", "監視圖騰 分鐘").SetValue(new Slider(0, 0, 60)));
                timersMenu.AddItem(
                    new MenuItem(Name + "TimersSweepingLens", "透視掃描 分鐘").SetValue(new Slider(20, 0, 60)));
                timersMenu.AddItem(
                    new MenuItem(Name + "TimersScryingOrb", "占撲寶珠 分鐘").SetValue(new Slider(45, 0, 60)));
                timersMenu.AddItem(new MenuItem(Name + "TimersWardingTotemEnabled", "購買監視圖騰").SetValue(true));
                timersMenu.AddItem(new MenuItem(Name + "TimersSweepingLensEnabled", "購買透視掃描").SetValue(true));
                timersMenu.AddItem(new MenuItem(Name + "TimersScryingOrbEnabled", "購買占撲寶珠").SetValue(false));
                timersMenu.AddItem(new MenuItem(Name + "TimersEnabled", "啟用").SetValue(true));

                var eventsMenu = new Menu("事件", Name + "Events");
                eventsMenu.AddItem(new MenuItem(Name + "EventsSightstone", "洞察之石").SetValue(true));
                eventsMenu.AddItem(new MenuItem(Name + "EventsRubySightstone", "洞察水晶").SetValue(true));
                eventsMenu.AddItem(new MenuItem(Name + "EventsWrigglesLantern", "瑞格之燈").SetValue(true));
                eventsMenu.AddItem(new MenuItem(Name + "EventsFeralFlare", "野火").SetValue(true));
                eventsMenu.AddItem(new MenuItem(Name + "EventsQuillCoat", "尖刺外套").SetValue(true));
                eventsMenu.AddItem(new MenuItem(Name + "EventsAncientGolem", "遠古石像").SetValue(true));
                eventsMenu.AddItem(
                    new MenuItem(Name + "EventsBuyTrinket", "Buy Trinket").SetValue(
                        new StringList(new[] {"Yellow", "Red", "Blue"})));
                eventsMenu.AddItem(new MenuItem(Name + "EventsEnabled", "啟用").SetValue(true));

                Menu.AddSubMenu(timersMenu);
                Menu.AddSubMenu(eventsMenu);

                Menu.AddItem(new MenuItem(Name + "SellUpgraded", "出售升級").SetValue(false));
                Menu.AddItem(new MenuItem(Name + "Enabled", "啟用").SetValue(true));

                BaseMenu.AddSubMenu(Menu);

                Game.OnGameUpdate += OnGameUpdate;

                Initialized = true;
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex.Message, ex.ToString());
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (!Enabled || _lastCheck + CheckInterval > Environment.TickCount)
                    return;

                _lastCheck = Environment.TickCount;
                if (ObjectManager.Player.IsDead || Utility.InShopRange())
                {
                    if (!Menu.Item(Name + "SellUpgraded").GetValue<bool>())
                    {
                        if (ObjectManager.Player.HasItem((int) Item.Ward.GreaterVisionTotem) ||
                            ObjectManager.Player.HasItem((int) Item.Ward.GreaterStealthTotem) ||
                            ObjectManager.Player.HasItem((int) Item.Misc.FarsightOrb) ||
                            ObjectManager.Player.HasItem((int) Item.Misc.OraclesLens))
                            return;
                    }

                    var hasYellow = ObjectManager.Player.HasItem((int) Item.Ward.WardingTotem) ||
                                    ObjectManager.Player.HasItem((int) Item.Ward.GreaterVisionTotem) ||
                                    ObjectManager.Player.HasItem((int) Item.Ward.GreaterStealthTotem);
                    var hasBlue = ObjectManager.Player.HasItem((int) Item.Misc.ScryingOrb) ||
                                  ObjectManager.Player.HasItem((int) Item.Misc.FarsightOrb);
                    var hasRed = ObjectManager.Player.HasItem((int) Item.Misc.SweepingLens) ||
                                 ObjectManager.Player.HasItem((int) Item.Misc.OraclesLens);

                    if (Menu.Item(Name + "EventsEnabled").GetValue<bool>())
                    {
                        bool hasTrinket;
                        int trinketId = -1;
                        switch (Menu.Item(Name + "EventsBuyTrinket").GetValue<StringList>().SelectedIndex)
                        {
                            case 0:
                                hasTrinket = hasYellow;
                                trinketId = (int) Item.Ward.WardingTotem;
                                break;
                            case 1:
                                hasTrinket = hasRed;
                                trinketId = (int) Item.Misc.SweepingLens;
                                break;
                            case 2:
                                hasTrinket = hasBlue;
                                trinketId = (int) Item.Misc.ScryingOrb;
                                break;
                            default:
                                hasTrinket = true;
                                break;
                        }

                        if (!hasTrinket && ObjectManager.Player.HasItem((int) Item.Ward.Sightstone) &&
                            Menu.Item(Name + "EventsSightstone").GetValue<bool>())
                        {
                            SwitchTrinket(trinketId);
                        }
                        if (!hasTrinket && ObjectManager.Player.HasItem((int) Item.Ward.RubySightstone) &&
                            Menu.Item(Name + "EventsRubySightstone").GetValue<bool>())
                        {
                            SwitchTrinket(trinketId);
                        }
                        if (!hasTrinket && ObjectManager.Player.HasItem((int) Item.Ward.WrigglesLantern) &&
                            Menu.Item(Name + "EventsWrigglesLantern").GetValue<bool>())
                        {
                            SwitchTrinket(trinketId);
                        }
                        if (!hasTrinket && ObjectManager.Player.HasItem((int) Item.Ward.FeralFlare) &&
                            Menu.Item(Name + "EventsFeralFlare").GetValue<bool>())
                        {
                            SwitchTrinket(trinketId);
                        }
                        if (!hasTrinket && ObjectManager.Player.HasItem((int) Item.Ward.QuillCoat) &&
                            Menu.Item(Name + "EventsQuillCoat").GetValue<bool>())
                        {
                            SwitchTrinket(trinketId);
                        }
                        if (!hasTrinket && ObjectManager.Player.HasItem((int) Item.Ward.SpiritOfTheAncientGolem) &&
                            Menu.Item(Name + "EventsAncientGolem").GetValue<bool>())
                        {
                            SwitchTrinket(trinketId);
                        }
                    }

                    if (Menu.Item(Name + "TimersEnabled").GetValue<bool>())
                    {
                        var time = Math.Floor(Game.Time/60f);
                        var tsList = new List<TrinketStruct>
                        {
                            new TrinketStruct
                            {
                                ItemId = (int) Item.Ward.WardingTotem,
                                Time = Menu.Item(Name + "TimersWardingTotem").GetValue<Slider>().Value,
                                Buy = Menu.Item(Name + "TimersWardingTotemEnabled").GetValue<bool>(),
                                HasItem = hasYellow
                            },
                            new TrinketStruct
                            {
                                ItemId = (int) Item.Misc.SweepingLens,
                                Time = Menu.Item(Name + "TimersSweepingLens").GetValue<Slider>().Value,
                                Buy = Menu.Item(Name + "TimersSweepingLensEnabled").GetValue<bool>(),
                                HasItem = hasRed
                            },
                            new TrinketStruct
                            {
                                ItemId = (int) Item.Misc.ScryingOrb,
                                Time = Menu.Item(Name + "TimersScryingOrb").GetValue<Slider>().Value,
                                Buy = Menu.Item(Name + "TimersScryingOrbEnabled").GetValue<bool>(),
                                HasItem = hasBlue
                            },
                        };
                        tsList = tsList.OrderBy(ts => ts.Time).ToList();

                        for (int i = 0, l = tsList.Count; i < l; i++)
                        {
                            if (time >= tsList[i].Time)
                            {
                                var hasHigher = false;
                                if (i != l - 1)
                                {
                                    for (int j = i + 1; j < l; j++)
                                    {
                                        if (time >= tsList[j].Time && tsList[j].Buy)
                                            hasHigher = true;
                                    }
                                }
                                if (!hasHigher && tsList[i].Buy && !tsList[i].HasItem)
                                {
                                    SwitchTrinket(tsList[i].ItemId);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex.Message, ex.ToString());
            }
        }

        private void SwitchTrinket(int id)
        {
            if (id == -1)
                return;
            Packet.C2S.SellItem.Encoded(new Packet.C2S.SellItem.Struct(SpellSlot.Trinket, ObjectManager.Player.NetworkId))
                .Send();
            Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(id, ObjectManager.Player.NetworkId)).Send();
        }

        #endregion

        #region Nested Types

        private struct TrinketStruct
        {
            #region Properties

            public bool Buy { get; set; }
            public bool HasItem { get; set; }
            public int ItemId { get; set; }
            public int Time { get; set; }

            #endregion
        }

        #endregion
    }
}
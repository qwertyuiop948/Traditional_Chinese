﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
namespace FioraRaven
{
    class DZApi
    {
        private Dictionary<String, String> dSpellsName = new Dictionary<String, String>();
        private Dictionary<int,String> itemNames = new  Dictionary<int,String>();
        public static Obj_AI_Base player = ObjectManager.Player;
        private string[] dSpellsNames;
        public DZApi()
        {
            fillDSpellList();
        }
        public Dictionary<String,String> getDanSpellsName()
        {
            return dSpellsName;
        }
         public Dictionary<int,String> getItemNames()
        {
            return itemNames;
        }
        public void addSpell(String name,String DisplayName)
        {
            dSpellsName.Add(name, DisplayName);
        }
        public void fillDSpellList()
        {
            addSpell("CurseofTheSadMummy", "阿木木| R");
            addSpell("InfernalGuardian", "安妮| R");
            addSpell("BlindMonkRKick", "盲僧| R");
            addSpell("GalioIdolOfDurand", "哨兵之殤 R");
            addSpell("syndrar", "辛德拉| R");
            addSpell("BusterShot", "小炮| R");
            addSpell("UFSlash", "石頭人| R");
            addSpell("VeigarPrimordialBurst", "小法| R");
            addSpell("ViR", "蔚| R");
            addSpell("AlZaharNetherGrasp", "馬爾扎哈| R");
        }
        public float getEnH(Obj_AI_Hero target)
        {
            float h = (target.Health / target.MaxHealth) * 100;
            return h;
        }
        public float getManaPer()
        {
            float mana = (player.Mana / player.MaxMana) * 100;
            return mana;
        }
        public float getPlHPer()
        {
            float h = (player.Health / player.MaxHealth) * 100;
            return h;
        }
        public void useItem(int id, Obj_AI_Hero target = null)
        {
            if (Items.HasItem(id) && Items.CanUseItem(id))
            {
                Items.UseItem(id, target);
            }
        }
    }
}

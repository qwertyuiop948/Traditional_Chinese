using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Collision = LeagueSharp.Common.Collision;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;

namespace BaseUlt3
{
    /*
     * HandOfBaron buffname correct???
     * Phasewalker mastery detected correctly now?
     * Draw: nice thin transparent progress bar with little champion icons moving on them
     * fixed? use for allies when fixed: champ.Spellbook.GetSpell(SpellSlot.R) = Ready
     * HPRegenRate, for 5 seconds or for 1?
     * */

    class BaseUlt
    {
        Menu Menu;
        Menu TeamUlt;
        Menu DisabledChampions;
        
        Spell Ultimate;
        int LastUltCastT;
        
        Utility.Map.MapType Map;

        List<Obj_AI_Hero> Heroes;
        List<Obj_AI_Hero> Enemies;
        List<Obj_AI_Hero> Allies;

        public List<EnemyInfo> EnemyInfo = new List<EnemyInfo>();

        public Dictionary<int, int> RecallT = new Dictionary<int, int>();

        Vector3 EnemySpawnPos;

        Font Text;

        static float BarX = Drawing.Width * 0.425f;
        float BarY = Drawing.Height * 0.80f;
        static int BarWidth = (int)(Drawing.Width - 2 * BarX);
        int BarHeight = 6;
        int SeperatorHeight = 5;
        static float Scale = (float)BarWidth / 8000;

        public BaseUlt()
        {
            (Menu = new Menu("基地大招3", "BaseUlt3", true)).AddToMainMenu();
            Menu.AddItem(new MenuItem("showRecalls", "顯示 回程").SetValue(true));
            Menu.AddItem(new MenuItem("baseUlt", "使用 大招").SetValue(true));
            Menu.AddItem(new MenuItem("panicKey", "（恐慌鍵位）禁用大招").SetValue(new KeyBind(32, KeyBindType.Press))); //32 == space
            Menu.AddItem(new MenuItem("regardlessKey", "無時間限制（保持）").SetValue(new KeyBind(17, KeyBindType.Press))); //17 == ctrl

            Heroes = ObjectManager.Get<Obj_AI_Hero>().ToList();
            Enemies = Heroes.Where(x => x.IsEnemy).ToList();
            Allies = Heroes.Where(x => x.IsAlly).ToList();

            EnemyInfo = Enemies.Select(x => new EnemyInfo(x)).ToList();

            bool compatibleChamp = IsCompatibleChamp(ObjectManager.Player.ChampionName);

            if (compatibleChamp)
            {
                TeamUlt = Menu.AddSubMenu(new Menu("大招支援隊友", "TeamUlt"));

                foreach (Obj_AI_Hero champ in Allies.Where(x => !x.IsMe && IsCompatibleChamp(x.ChampionName)))
                    TeamUlt.AddItem(new MenuItem(champ.ChampionName, "Ally with baseult: " + champ.ChampionName).SetValue(false).DontSave());

                DisabledChampions = Menu.AddSubMenu(new Menu("禁用大招支援", "DisabledChampions"));

                foreach (Obj_AI_Hero champ in Enemies)
                    DisabledChampions.AddItem(new MenuItem(champ.ChampionName, "Don't shoot: " + champ.ChampionName).SetValue(false).DontSave());
            }

            EnemySpawnPos = ObjectManager.Get<GameObject>().FirstOrDefault(x => x.Type == GameObjectType.obj_SpawnPoint && x.IsEnemy).Position;

            Map = Utility.Map.GetMap()._MapType;

            Ultimate = new Spell(SpellSlot.R);

            Text = new Font(Drawing.Direct3DDevice, new FontDescription{FaceName = "Calibri", Height = 13, Width = 6, OutputPrecision = FontPrecision.Default, Quality = FontQuality.Default});
            
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnDraw += Drawing_OnDraw;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_DomainUnload;

            if (compatibleChamp)
                Game.OnGameUpdate += Game_OnGameUpdate;

            Game.PrintChat("<font color=\"#1eff00\">基地大招v3 (alpha, not yet for using purposes) by Beaving</font> - <font color=\"#00BFFF\">重做 加載成功!漢化by二狗！QQ群361630847</font>");
        }

        public bool IsCompatibleChamp(String championName)
        {
            return UltSpellData.Keys.Any(x => x == championName);
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            int time = Environment.TickCount;

            foreach (EnemyInfo enemyInfo in EnemyInfo.Where(x => x.Player.IsVisible))
                enemyInfo.LastSeen = time;

            if (!Menu.Item("baseUlt").GetValue<bool>())
                return;

            foreach (EnemyInfo enemyInfo in EnemyInfo.Where(x =>
                x.Player.IsValid &&
                !x.Player.IsDead &&
                !DisabledChampions.Item(x.Player.ChampionName).GetValue<bool>() && 
                x.RecallInfo.Recall.Status == Packet.S2C.Recall.RecallStatus.RecallStarted).OrderBy(x => x.RecallInfo.GetRecallEnd()))
            {
                if (Environment.TickCount - LastUltCastT > 15000)
                    HandleUltTarget(enemyInfo);
            }
        }

        struct UltSpellDataS
        {
            public int SpellStage;
            public float DamageMultiplicator;
            public float Width;
            public float Delay;
            public float Speed;
            public bool Collision;
        }

        Dictionary<String, UltSpellDataS> UltSpellData = new Dictionary<string, UltSpellDataS>
        {
            {"Jinx",    new UltSpellDataS { SpellStage = 1, DamageMultiplicator = 1.0f, Width = 140f, Delay = 0600f/1000f, Speed = 1700f, Collision = true}},
            {"Ashe",    new UltSpellDataS { SpellStage = 0, DamageMultiplicator = 1.0f, Width = 130f, Delay = 0250f/1000f, Speed = 1600f, Collision = true}},
            {"Draven",  new UltSpellDataS { SpellStage = 0, DamageMultiplicator = 0.7f, Width = 160f, Delay = 0400f/1000f, Speed = 2000f, Collision = true}},
            {"Ezreal",  new UltSpellDataS { SpellStage = 0, DamageMultiplicator = 0.7f, Width = 160f, Delay = 1000f/1000f, Speed = 2000f, Collision = false}},
            {"Karthus", new UltSpellDataS { SpellStage = 0, DamageMultiplicator = 1.0f, Width = 000f, Delay = 3125f/1000f, Speed = 0000f, Collision = false}}
        };

        bool CanUseUlt(Obj_AI_Hero hero) //use for allies when fixed: champ.Spellbook.GetSpell(SpellSlot.R) = Ready
        {
            return hero.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready || 
                (hero.Spellbook.GetSpell(SpellSlot.R).Level > 0 && hero.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Surpressed && hero.Mana >= hero.Spellbook.GetSpell(SpellSlot.R).ManaCost);
        }

        void HandleUltTarget(EnemyInfo enemyInfo)
        {
            bool shoot = false;

            foreach (Obj_AI_Hero champ in Allies.Where(x => //gathering the damage from allies should probably be done once only with timers
                            x.IsValid &&
                            !x.IsDead && 
                            ((x.IsMe && !x.IsStunned) || TeamUlt.Items.Any(item => item.GetValue<bool>() && item.Name == x.ChampionName)) &&
                            CanUseUlt(x)))
            {
                if (UltSpellData[champ.ChampionName].Collision && IsCollidingWithChamps(champ, EnemySpawnPos, UltSpellData[champ.ChampionName].Width))
                {
                    enemyInfo.RecallInfo.IncomingDamage[champ.NetworkId] = 0;
                    continue;
                }

                //increase timeneeded if it should arrive earlier, decrease if later
                var timeneeded = GetUltTravelTime(champ, UltSpellData[champ.ChampionName].Speed, UltSpellData[champ.ChampionName].Delay, EnemySpawnPos) - 65;

                if (enemyInfo.RecallInfo.GetRecallCountdown() >= timeneeded)
                    enemyInfo.RecallInfo.IncomingDamage[champ.NetworkId] = (float)Damage.GetSpellDamage(champ, enemyInfo.Player, SpellSlot.R, UltSpellData[champ.ChampionName].SpellStage) * UltSpellData[champ.ChampionName].DamageMultiplicator;
                else if (enemyInfo.RecallInfo.GetRecallCountdown() < timeneeded)
                {
                    enemyInfo.RecallInfo.IncomingDamage[champ.NetworkId] = 0;
                    continue;
                }

                if (champ.IsMe && enemyInfo.RecallInfo.GetRecallCountdown() - timeneeded < 65)
                    shoot = true;
            }

            float totalUltDamage = enemyInfo.RecallInfo.IncomingDamage.Values.Sum();

            float targetHealth = GetTargetHealth(enemyInfo, enemyInfo.RecallInfo.GetRecallCountdown());

            if (!shoot || Menu.Item("panicKey").GetValue<KeyBind>().Active)
                return;

            int time = Environment.TickCount;

            if (time - enemyInfo.LastSeen > 20000 && !Menu.Item("regardlessKey").GetValue<KeyBind>().Active)
            {
                if (totalUltDamage < enemyInfo.Player.MaxHealth)
                    return;
            }
            else if (totalUltDamage < targetHealth)
                return;

            Ultimate.Cast(EnemySpawnPos, true);
            LastUltCastT = time;
        }

        float GetTargetHealth(EnemyInfo enemyInfo, int additionalTime)
        {
            if (enemyInfo.Player.IsVisible)
                return enemyInfo.Player.Health;

            float predictedHealth = enemyInfo.Player.Health + enemyInfo.Player.HPRegenRate * ((Environment.TickCount - enemyInfo.LastSeen + additionalTime) / 1000f);

            return predictedHealth > enemyInfo.Player.MaxHealth ? enemyInfo.Player.MaxHealth : predictedHealth;
        }

        float GetUltTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos)
        {
            if (source.ChampionName == "Karthus")
                return delay * 1000;

            float distance = Vector3.Distance(source.ServerPosition, targetpos);

            float missilespeed = speed;

            if(source.ChampionName == "Jinx" && distance > 1350)
            {
                const float accelerationrate = 0.3f; //= (1500f - 1350f) / (2200 - speed), 1 unit = 0.3units/second

                var acceldifference = distance - 1350f;

                if (acceldifference > 150f) //it only accelerates 150 units
                    acceldifference = 150f;

                var difference = distance - 1500f;

                missilespeed = (1350f * speed + acceldifference * (speed + accelerationrate * acceldifference) + difference * 2200f) / distance;
            }

            return (distance / missilespeed + delay) * 1000;
        }

        bool IsCollidingWithChamps(Obj_AI_Hero source, Vector3 targetpos, float width)
        {
            var input = new PredictionInput
            {
                Radius = width,
                Unit = source,
            };

            input.CollisionObjects[0] = CollisionableObjects.Heroes;

            return Collision.GetCollision(new List<Vector3> { targetpos }, input).Any(); //x => x.NetworkId != targetnetid, hard to realize with teamult
        }

        void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] == Packet.S2C.Recall.Header)
            {
                var recall = RecallDecode(args.PacketData); //Packet.S2C.Recall.Decoded(args.PacketData)
                EnemyInfo.Find(x => x.Player.NetworkId == recall.UnitNetworkId).RecallInfo.UpdateRecall(recall); 
            }
        }

        public Packet.S2C.Recall.Struct RecallDecode(byte[] data)
        {
            var time = Environment.TickCount - Game.Ping;

            var reader = new BinaryReader(new MemoryStream(data));
            var recall = new Packet.S2C.Recall.Struct();

            reader.ReadByte(); //PacketId
            reader.ReadInt32();
            recall.UnitNetworkId = reader.ReadInt32();
            reader.ReadBytes(66);

            recall.Status = Packet.S2C.Recall.RecallStatus.Unknown;

            var teleport = false;

            if (BitConverter.ToString(reader.ReadBytes(6)) != "00-00-00-00-00-00")
            {
                if (BitConverter.ToString(reader.ReadBytes(3)) != "00-00-00")
                {
                    recall.Status = Packet.S2C.Recall.RecallStatus.TeleportStart;
                    teleport = true;
                }
                else
                    recall.Status = Packet.S2C.Recall.RecallStatus.RecallStarted;
            }

            reader.Close();

            var champ = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(recall.UnitNetworkId);

            if (champ != null)
            {
                if (teleport)
                    recall.Duration = 3500;
                else //use masteries to detect recall duration, because spelldata is not initialized yet when enemy has not been seen
                {
                    if (Map == Utility.Map.MapType.CrystalScar)
                        recall.Duration = 4500;
                    else
                    {
                        recall.Duration = 8000;

                        if (champ.HasBuff("HandOfBaron", true))
                            recall.Duration -= 4000;
                    }

                    if (champ.Masteries.Any(x => x.Page == MasteryPage.Utility && x.Id == 65 && x.Points == 1))
                        recall.Duration -= Map == Utility.Map.MapType.CrystalScar ? 500 : 1000; //phasewalker mastery
                }

                if (!RecallT.ContainsKey(recall.UnitNetworkId) || RecallT[recall.UnitNetworkId] == 0)
                    RecallT[recall.UnitNetworkId] = time;
                else
                {
                    if (time - RecallT[recall.UnitNetworkId] > recall.Duration - 75)
                        recall.Status = teleport ? Packet.S2C.Recall.RecallStatus.TeleportEnd : Packet.S2C.Recall.RecallStatus.RecallFinished;
                    else
                        recall.Status = teleport ? Packet.S2C.Recall.RecallStatus.TeleportAbort : Packet.S2C.Recall.RecallStatus.RecallAborted;

                    RecallT[recall.UnitNetworkId] = 0; //recall aborted or finished, reset status
                }
            }

            return recall;
        }

        void Drawing_OnPostReset(EventArgs args)
        {
            Text.OnResetDevice();
        }

        void Drawing_OnPreReset(EventArgs args)
        {
            Text.OnLostDevice();
        }

        void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            Text.Dispose();
        }

        void Drawing_OnDraw(EventArgs args)
        {
            if (!Menu.Item("showRecalls").GetValue<bool>() || Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                return;

            bool drawFrame = false;

            foreach (EnemyInfo enemyInfo in EnemyInfo.Where(x =>
                x.Player.IsValid &&
                x.RecallInfo.IsPorting() &&
                !x.Player.IsDead && //maybe redundant
                x.RecallInfo.GetRecallCountdown() > 0))
            {
                DrawRect(BarX, BarY, (int)(Scale * (float)enemyInfo.RecallInfo.GetRecallCountdown()), BarHeight, 1, System.Drawing.Color.FromArgb(100, System.Drawing.Color.White));
                DrawRect(BarX + (int)(Scale * (float)enemyInfo.RecallInfo.GetRecallCountdown()), BarY - SeperatorHeight, 0, SeperatorHeight + 1, 1, System.Drawing.Color.LightGray);

                Text.DrawText(null, enemyInfo.Player.ChampionName, (int)BarX + (int)(Scale * (float)enemyInfo.RecallInfo.GetRecallCountdown() - (float)(enemyInfo.Player.ChampionName.Length * Text.Description.Width)/2), (int)BarY - SeperatorHeight - Text.Description.Height - 1, new ColorBGRA(255, 255, 255, 255));

                if (!drawFrame)
                    drawFrame = true;
            }

            if(drawFrame)
            {
                DrawRect(BarX, BarY, BarWidth, BarHeight, 1, System.Drawing.Color.FromArgb(40, System.Drawing.Color.White));

                DrawRect(BarX - 1, BarY + 1, 0, BarHeight, 1, System.Drawing.Color.White);
                DrawRect(BarX - 1, BarY - 1, BarWidth + 2, 1, 1, System.Drawing.Color.White);
                DrawRect(BarX - 1, BarY + BarHeight, BarWidth + 2, 1, 1, System.Drawing.Color.White);
                DrawRect(BarX + 1 + BarWidth, BarY + 1, 0, BarHeight, 1, System.Drawing.Color.White);
            }
        }

        public void DrawRect(float x, float y, int width, int height, float thickness, System.Drawing.Color color)
        {
            for (int i = 0; i < height; i++)
                Drawing.DrawLine(x, y + i, x + width, y + i, thickness, color);
        }
    }

    class EnemyInfo
    {
        public Obj_AI_Hero Player;
        public int LastSeen;

        public RecallInfo RecallInfo;

        public EnemyInfo(Obj_AI_Hero player)
        {
            Player = player;
            RecallInfo = new RecallInfo(this);
        }
    }

    class RecallInfo
    {
        public EnemyInfo EnemyInfo;
        public Dictionary<int, float> IncomingDamage; //from, damage
        public Packet.S2C.Recall.Struct Recall;

        public RecallInfo(EnemyInfo enemyInfo)
        {
            EnemyInfo = enemyInfo;
            Recall = new Packet.S2C.Recall.Struct(EnemyInfo.Player.NetworkId, Packet.S2C.Recall.RecallStatus.Unknown, Packet.S2C.Recall.ObjectType.Player, 0);
            IncomingDamage = new Dictionary<int, float>(); 
        }

        public bool IsPorting()
        {
            return Recall.Status == Packet.S2C.Recall.RecallStatus.RecallStarted || Recall.Status == Packet.S2C.Recall.RecallStatus.TeleportStart;
        }

        public EnemyInfo UpdateRecall(Packet.S2C.Recall.Struct newRecall)
        {
            IncomingDamage.Clear();

            Recall = newRecall;
            return EnemyInfo;
        }

        public int GetRecallStart()
        {
            switch (Recall.Status)
            {
                case Packet.S2C.Recall.RecallStatus.RecallStarted:
                case Packet.S2C.Recall.RecallStatus.TeleportStart:
                    return Program.BaseUlt.RecallT[Recall.UnitNetworkId];

                default:
                    return 0;
            }
        }

        public int GetRecallEnd()
        {
            return GetRecallStart() + Recall.Duration;
        }

        public int GetRecallCountdown()
        {
            int countdown = GetRecallEnd() - Environment.TickCount;
            return countdown < 0 ? 0 : countdown;
        }

        public override string ToString()
        {
            String drawtext = EnemyInfo.Player.ChampionName + ": " + Recall.Status; //change to better string and colored

            float countdown = GetRecallCountdown() / 1000f;

            if (countdown > 0)
                drawtext += " (" + countdown.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "s)";

            return drawtext;
        }
    }
}

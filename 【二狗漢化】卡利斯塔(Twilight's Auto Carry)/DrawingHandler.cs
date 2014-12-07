using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.Direct3D;
using Color = System.Drawing.Color;
namespace TAC_Kalista
{
    class DrawingHandler
    {
        public static SharpDX.Direct3D9.Device dxDevice = Drawing.Direct3DDevice;
        public static SharpDX.Direct3D9.Line dxLine;
        public static Obj_AI_Hero unit { get; set; }
        public static float width = 104;
        public static float hight = 9;
        public static readonly Vector3[] wallhops = new[] { new Vector3(794, 5914, 50), new Vector3(792, 6208, -71), new Vector3(10906, 7498, 52), new Vector3(10872, 7208, 51), new Vector3(11900, 4870, 51), new Vector3(11684, 4694, -71), new Vector3(12046, 5376, 54), new Vector3(12284, 5382, 51), new Vector3(11598, 8676, 62), new Vector3(11776, 8890, 50), new Vector3(8646, 9584, 50), new Vector3(8822, 9406, 51), new Vector3(6606, 11756, 53), new Vector3(6494, 12056, 56), new Vector3(5164, 12090, 56), new Vector3(5146, 11754, 56), new Vector3(5780, 10650, 55), new Vector3(5480, 10620, -71), new Vector3(3174, 9856, 52), new Vector3(3398, 10080, -65), new Vector3(2858, 9448, 51), new Vector3(2542, 9466, 52), new Vector3(3700, 7416, 51), new Vector3(3702, 7702, 52), new Vector3(3224, 6308, 52), new Vector3(3024, 6312, 57), new Vector3(4724, 5608, 50), new Vector3(4610, 5868, 51), new Vector3(6124, 5308, 48), new Vector3(6010, 5522, 51), new Vector3(9322, 4514, -71), new Vector3(9022, 4508, 52), new Vector3(6826, 8628, -71), new Vector3(7046, 8750, 52), };
        public static int minRange = 100;

        public static void init()
        {
            dxLine = new Line(dxDevice) { Width = 9 };
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
        }
        public static void OnDraw(EventArgs args)
        {
            if(Kalista.drawings)
            {

                if (MenuHandler.Config.Item("JumpTo").GetValue<KeyBind>().Active)
                {
                    DrawingHandler.getAvailableJumpSpots();
                }
                Spell[] spellList = { SkillHandler.Q, SkillHandler.W, SkillHandler.E, SkillHandler.R };

                foreach (var spell in spellList)
                {
                    var menuItem = MenuHandler.Config.Item(spell.Slot+"Range").GetValue<Circle>();
                    if (menuItem.Active && spell.Level > 0)
                        Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
                bool drawHp = MenuHandler.Config.Item("drawHp").GetValue<bool>();
                bool drawStacks = MenuHandler.Config.Item("drawStacks").GetValue<bool>();
                if (drawHp || drawStacks)
                {
                    int stacks;
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                    {
                        if (drawHp)
                        {
                            unit = enemy;
                            drawDmg(MathHandler.getDamageToTarget(enemy), enemy.Health < MathHandler.getRealDamage(enemy) ? Color.Red : Color.Yellow);
                        }
                        if (drawStacks)
                        {
                            stacks = enemy.Buffs.FirstOrDefault(b => b.Name.ToLower() == "kalistaexpungemarker").Count;
                            if (stacks > 0)
                            {
                                Drawing.DrawText(enemy.HPBarPosition.X, enemy.HPBarPosition.Y - 5, Color.Red, "E:" + stacks + "H:" + (int)enemy.Health + "/D:" + (int)MathHandler.getRealDamage(enemy), enemy);
                            }
                        }
                    }
                }
                if (MenuHandler.Config.Item("drawESlow").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, SkillHandler.E.Range - 110, Color.Pink);
                }
            }
        }
        /**
         * @author detuks
         * */
        public static void OnEndScene(EventArgs args)
        {
            if (MenuHandler.Config.Item("drawHp").GetValue<bool>())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                {
                    unit = enemy;
                    drawDmg(MathHandler.getDamageToTarget(enemy), enemy.Health < MathHandler.getRealDamage(enemy) ? Color.Red : Color.Yellow);
                }
            }
        }

        private static Vector2 Offset
        {
            get
            {
                if (unit != null)
                {
                    return unit.IsAlly ? new Vector2(34, 9) : new Vector2(10, 20);
                }

                return new Vector2();
            }
        }

        public static Vector2 startPosition
        {

            get { return new Vector2(unit.HPBarPosition.X + Offset.X, unit.HPBarPosition.Y + Offset.Y); }
        }


        private static float getHpProc(float dmg = 0)
        {
            float health = ((unit.Health - dmg) > 0) ? (unit.Health - dmg) : 0;
            return (health / unit.MaxHealth);
        }

        private static Vector2 getHpPosAfterDmg(float dmg)
        {
            float w = getHpProc(dmg) * width;
            return new Vector2(startPosition.X + w, startPosition.Y);
        }

        public static void drawDmg(float dmg, System.Drawing.Color color)
        {
            var hpPosNow = getHpPosAfterDmg(0);
            var hpPosAfter = getHpPosAfterDmg(dmg);

            fillHPBar(hpPosNow, hpPosAfter, color);
        }

        private static void fillHPBar(int to, int from, System.Drawing.Color color)
        {
            Vector2 sPos = startPosition;

            for (int i = from; i < to; i++)
            {
                Drawing.DrawLine(sPos.X + i, sPos.Y, sPos.X + i, sPos.Y + 9, 1, color);
            }
        }

        private static void fillHPBar(Vector2 from, Vector2 to, System.Drawing.Color color)
        {
            dxLine.Begin();

            dxLine.Draw(new[]
                                    {
                                        new Vector2((int)from.X, (int)from.Y + 4f),
                                        new Vector2( (int)to.X, (int)to.Y + 4f)
                                    },new ColorBGRA(255,255,00,90));
            dxLine.End();
        }

        public static bool IsLyingInCone(Vector2 position, Vector2 apexPoint, Vector2 circleCenter, float aperture)
        {
            float halfAperture = aperture / 2.0f;
            Vector2 apexToXVect = apexPoint - position;
            Vector2 axisVect = apexPoint - circleCenter;
            bool isInInfiniteCone = DotProd(apexToXVect, axisVect) / Magn(apexToXVect) / Magn(axisVect) > Math.Cos(halfAperture);
            if (!isInInfiniteCone) return false;
            return DotProd(apexToXVect, axisVect) / Magn(axisVect) < Magn(axisVect);
        }

        public static float DotProd(Vector2 a, Vector2 b){ return a.X * b.X + a.Y * b.Y; }
        public static float Magn(Vector2 a){ return (float)(Math.Sqrt(a.X * a.X + a.Y * a.Y)); }














        public static void getAvailableJumpSpots()
        {
            int size = 295;
            int n = 15;
            double x,y;
            Vector3 drawWhere;

            if (!SkillHandler.Q.IsReady())
            {
                Drawing.DrawText(Drawing.Width * 0.44f, Drawing.Height * 0.80f, Color.Red,
                "Jumping mode active, but Q isn't.");
            }
            else
            {
                Drawing.DrawText(Drawing.Width * 0.39f, Drawing.Height * 0.80f, Color.White,
                    "Hover the spot to jump to ");
            }
            Vector3 playerPosition = ObjectManager.Player.Position;
            Drawing.DrawCircle(ObjectManager.Player.Position, size, Color.RoyalBlue);
            Obj_AI_Hero target = SimpleTs.GetTarget(SkillHandler.Q.Range, SimpleTs.DamageType.Physical);
            for (int i = 1; i <= n; i++)
            {
                x =  size * Math.Cos(2 * Math.PI * i / n);
                y =  size * Math.Sin(2 * Math.PI * i / n);
                drawWhere = new Vector3((int)(playerPosition.X + x), (float)(playerPosition.Y + y), playerPosition.Z);
                if (!Utility.IsWall(drawWhere))
                {
                    if (SkillHandler.Q.IsReady() && Game.CursorPos.Distance(drawWhere) <= 80f)
                    {
                        if (target != null)
                            FightHandler.customQCast(target);
                        else
                            SkillHandler.Q.Cast(new Vector2(drawWhere.X, drawWhere.Y), true);

                        Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(drawWhere.X, drawWhere.Y)).Send();
                        return;
                    }
                    Utility.DrawCircle(drawWhere, 20, Color.Red);
                }
            }

        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using ClipperLib;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

namespace CassioXD
{
    class Program
    {
        public const string ChampionName  = "Cassiopeia";
        public static Menu Option;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Obj_AI_Hero Player;
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        private static long LastQCast = 0;
        private static long LastECast = 0;
        public static Obj_AI_Hero starget = null;
        public static List<Obj_AI_Hero> Targets = new List<Obj_AI_Hero>();
        public static Obj_AI_Hero MainTarget;
        public static TargetingMode TMode = TargetingMode.FastKill;
        public static AimMode AMode = AimMode.XDMode;
        public static LaneClearMode LMode = LaneClearMode.Normal;
        public static HitChance Chance = HitChance.VeryHigh;
        public static bool listed = true;
        public static bool Nopsntarget = true;
        public static bool aastatus;
        public static int kills = 0;
        public static Random rand = new Random();

        public static List<string> Messages;



        private static string[] p1 = new string[] { "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen", "Gnar",
                "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami", "Nasus", "Nautilus", "Nunu",
                "Olaf", "Rammus", "Renekton", "Sejuani", "Shen", "Shyvana", "Singed", "Sion", "Skarner", "Sona",
                "Soraka", "Taric", "Thresh", "Volibear", "Warwick", "MonkeyKing", "Yorick", "Zac", "Zyra","tahm kench" };

        private static string[] p2 = new string[] { "Aatrox", "Darius", "Elise", "Evelynn", "Galio", "Gangplank", "Gragas", "Irelia", "Jax",
                "Lee Sin", "Maokai", "Morgana", "Nocturne", "Pantheon", "Poppy", "Rengar", "Rumble", "Ryze", "Swain",
                "Trundle", "Tryndamere", "Udyr", "Urgot", "Vi", "XinZhao" };

        private static string[] p3 = new string[] { "Akali", "Diana", "Fiddlesticks", "Fiora", "Fizz", "Heimerdinger", "Jayce", "Kassadin",
                "Kayle", "Kha'Zix", "Lissandra", "Mordekaiser", "Nidalee", "Riven", "Shaco", "Vladimir", "Yasuo",
                "Zilean" };

        private static string[] p4 = new string[] { "Ahri", "Anivia", "Annie", "Ashe", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven",
                "Ezreal", "Ekko", "Graves", "Jinx", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw", "LeBlanc", "Lucian",
                "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon", "Teemo",
                "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "VelKoz", "Viktor", "Xerath", "Zed",
                "Ziggs" };

        public enum TargetingMode
        {
            FastKill = 1,
            AutoPriority = 0
        }
        
        public class Ultplayer
        {
            public Ultplayer()
            {
                Angle = 0;
                Diff = 0;
                Hero = null;
            }
            public Ultplayer(double angle, double diff, Obj_AI_Hero hero)
            {

                Angle = angle;
                Diff = diff;
                Hero = hero;
            }
            public double Angle;
            public double Diff;
            public Obj_AI_Hero Hero;
        }

        public enum AimMode
        {
            XDMode = 2,
            Normal = 1,
            HitChance = 0
        }

        public enum LaneClearMode
        {
            Normal = 1,
            Logical = 0
        }

        static void setupMessages()
        {
            Messages = new List<string>
            {
                "gj", "good job", "very gj", "very good job",
                "wp", "well played",
                "nicely played",
                "amazing",
                "nice", "nice1", "nice one",
                "well done",
                "sweet",                
            };

        }

        static string getRandomElement(List<string> collection, bool firstEmpty = true)
        {
            if (firstEmpty && rand.Next(3) == 0)
                return collection[0];

            return collection[rand.Next(collection.Count)];
        }

        static string generateMessage()
        {
            string message = getRandomElement(Messages, false);
            return message;
        }


        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += onGameLoad;
            setupMessages();
        }

        private static Vector3 PreCastPos (Obj_AI_Hero Hero ,float Delay)
        {
            float value = 0f;
            if (Hero.IsFacing(Player))
            {
                value = (50f - Hero.BoundingRadius);
            }
            else
            {
                value = -(100f - Hero.BoundingRadius);
            }
            var distance = Delay * Hero.MoveSpeed + value;
            var path = Hero.GetWaypoints();

            for (var i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                var d = a.Distance(b);

                if (d < distance)
                {
                    distance -= d;
                }
                else
                {
                    return (a + distance * (b - a).Normalized()).To3D();
                }
            }


            return (path[path.Count - 1]).To3D();
        }

#region Targetlist
        private static double FindPrioForTarget(Obj_AI_Hero enemy, TargetingMode TMode)
        {
            switch (TMode)
            {
                case TargetingMode.AutoPriority:
                    {
                        if (p1.Contains(enemy.CharData.BaseSkinName))
                        {
                            return 4;
                        }
                        else if (p2.Contains(enemy.CharData.BaseSkinName))
                        {
                            return 3;
                        }
                        else if (p3.Contains(enemy.CharData.BaseSkinName))
                        {
                            return 2;
                        }
                        else if (p4.Contains(enemy.CharData.BaseSkinName))
                        {
                            return 1;
                        }
                        else
                        {
                            return 5;
                        }
                    }
                case TargetingMode.FastKill:
                    {
                        if (enemy.IsValid && enemy != null && enemy.IsVisible && !enemy.IsDead)
                            return (enemy.Health / Player.GetSpellDamage(enemy, SpellSlot.E));
                        else
                            return 1000000;
                    }
                default:
                    return 0;
            }
        }

        private static void Targetlist(TargetingMode TMode)
        {
            int i1, i2;
            Obj_AI_Hero Buf;
            {
                if (Targets.Count != ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Team != ObjectManager.Player.Team).Count())
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Team != ObjectManager.Player.Team))
                {
                    Targets.Add(enemy);
                }
                

                for (i1 = 0; i1 < Targets.Count; i1++)
                    for (i2 = 0; i2 < Targets.Count; i2++)
                        if (FindPrioForTarget(Targets[i1], TMode) > FindPrioForTarget(Targets[i2], TMode))
                        {
                            Buf = Targets[i2];
                            Targets[i1] = Targets[i1];
                            Targets[i2] = Buf;
                        }
                        else if (FindPrioForTarget(Targets[i1], TMode) < FindPrioForTarget(Targets[i2], TMode))
                        {
                            Buf = Targets[i1];
                            Targets[i1] = Targets[i2];
                            Targets[i2] = Buf;
                        }
            }
        }

        private static float GetPoisonBuffEndTime(Obj_AI_Base target)
        {
            var buffEndTime = target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Type == BuffType.Poison)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
            return buffEndTime;
        }

#endregion

#region Targetselect

        private static Obj_AI_Hero GetQTarget()
        {
            var menuItem3 = Option.Item("AimMode").GetValue<StringList>();
            Enum.TryParse(menuItem3.SList[menuItem3.SelectedIndex], out AMode);

            if (MainTarget == null || MainTarget.IsDead || !MainTarget.IsVisible || MainTarget.HasBuffOfType(BuffType.Poison) || !(Player.ServerPosition.Distance(Q.GetPrediction(MainTarget, true).CastPosition) < Q.Range))
            {
                foreach (var target in Targets)
                {
                    if (target != null && target.IsVisible && !target.IsDead)
                    {
                        if (!target.HasBuffOfType(BuffType.Poison) || GetPoisonBuffEndTime(target) < (Game.Time + Q.Delay))
                        {
                            switch (AMode)
                            {
                                case AimMode.HitChance:
                                    if (Player.ServerPosition.Distance(Q.GetPrediction(target, true).CastPosition) < Q.Range)
                                    {
                                        return target;
                                    }
                                    break;
                                case AimMode.Normal:
                                    if (Player.ServerPosition.Distance(Q.GetPrediction(target, true).CastPosition) < Q.Range)
                                    {
                                        return target;
                                    }
                                    break;
                                case AimMode.XDMode:
                                    if (Player.ServerPosition.Distance(PreCastPos(target, 0.6f)) < Q.Range)
                                    {
                                        return target;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            else
                return MainTarget;
            return null;
        }

        private static Obj_AI_Hero GetWTarget()
        {
            var menuItem3 = Option.Item("AimMode").GetValue<StringList>();
            Enum.TryParse(menuItem3.SList[menuItem3.SelectedIndex], out AMode);

            if (MainTarget == null || MainTarget.IsDead || !MainTarget.IsVisible || MainTarget.HasBuffOfType(BuffType.Poison) || !(Player.ServerPosition.Distance(W.GetPrediction(MainTarget, true).CastPosition) < W.Range))
            {
                foreach (var target in Targets)
                {
                    if (target != null && target.IsVisible && !target.IsDead)
                    {
                            switch (AMode)
                            {
                                case AimMode.HitChance:
                                    if (!target.HasBuffOfType(BuffType.Poison) || (Player.ServerPosition.Distance(Q.GetPrediction(target, true).CastPosition) > Q.Range))
                                    {
                                        if (Player.ServerPosition.Distance(W.GetPrediction(target, true).CastPosition) < W.Range)
                                        {
                                            return target;
                                        }
                                    }
                                    break;
                                case AimMode.Normal:
                                    if (!target.HasBuffOfType(BuffType.Poison) || (Player.ServerPosition.Distance(Q.GetPrediction(target, true).CastPosition) > Q.Range))
                                    {
                                        if (Player.ServerPosition.Distance(W.GetPrediction(target, true).CastPosition) < Q.Range)
                                        {
                                            return target;
                                        }
                                    }
                                    break;
                                case AimMode.XDMode:
                                    if (!target.HasBuffOfType(BuffType.Poison) || (Player.ServerPosition.Distance(PreCastPos(target, 0.6f)) > Q.Range))
                                    {
                                        if (Player.ServerPosition.Distance(PreCastPos(target, Player.ServerPosition.Distance(target.ServerPosition) / W.Speed)) < W.Range)
                                        {
                                            return target;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            else
                return MainTarget;
            return null;
        }

        private static Obj_AI_Hero GetETarget()
        {
            if (MainTarget == null || MainTarget.IsDead || !MainTarget.IsVisible || ((!MainTarget.HasBuffOfType(BuffType.Poison) && GetPoisonBuffEndTime(MainTarget) < (Game.Time + E.Delay)) || Player.GetSpellDamage(MainTarget, SpellSlot.E) < MainTarget.Health))
            {
                foreach (var target in Targets)
                {
                    if (target != null && target.IsVisible && !target.IsDead)
                    {
                        if ((target.HasBuffOfType(BuffType.Poison) && GetPoisonBuffEndTime(target) > (Game.Time + E.Delay)) || Player.GetSpellDamage(target, SpellSlot.E) > target.Health)
                        {
                            if (target.IsValidTarget(E.Range))
                            {
                                return target;
                            }
                        }
                    }
                }
            }
            else
                return MainTarget;
            return null;
        }

        private static List<Obj_AI_Hero> GetEnemyinUltRange()
        {
            var EnemyinRange = ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget() && Player.Distance(enemy) < R.Range).ToList();
            return EnemyinRange;
        }

        private static List<Obj_AI_Hero> GetEnemyinUltRangefacing()
        {
            var EnemyFacing = GetEnemyinUltRange().Where(enemy => enemy.IsFacing(Player)).ToList();
            return EnemyFacing;
        }

        public static List<Ultplayer> PlayersAngle()
        {
            List<Ultplayer> AList = new List<Ultplayer>();

            if (GetEnemyinUltRange() != null)
            {
                foreach (var enemy in GetEnemyinUltRange())
                {
                    Ultplayer uPlayer = new Ultplayer((GetAngle(Player.Position.To2D(), enemy.Position.To2D()) * 180 / Math.PI), (GetAngle(Player.Position.To2D(), enemy.Position.To2D()) * 180 / Math.PI), enemy);
                    AList.Add(uPlayer);
                }
            }
            return AList;
        }

        private static List<List<Ultplayer>> GetBundle(List<Ultplayer> players)
        {
            List < Ultplayer> Bundle = new List<Ultplayer>();
            List<List<Ultplayer>> BTARG = new List<List<Ultplayer>>();

            for (var i1 = 0; i1 < players.Count(); i1++)
            {
                Bundle.Add(players.ElementAt(i1));
                for (var i2 = 0; i2 < players.Count();i2++)
                {
                    if (i2!=i1)
                    {
                        if (players.ElementAt(i2).Angle > players.ElementAt(i1).Angle)
                        {
                            if (players.ElementAt(i2).Angle - players.ElementAt(i1).Angle < 80)
                            {
                                Ultplayer uPlayer = new Ultplayer(players.ElementAt(i2).Angle, (players.ElementAt(i2).Angle - players.ElementAt(i1).Angle), players.ElementAt(i2).Hero);
                                Bundle.Add(uPlayer);
                            }
                        }
                        else
                        if (players.ElementAt(i2).Angle < players.ElementAt(i1).Angle)
                        {
                            if (players.ElementAt(i1).Angle - players.ElementAt(i2).Angle < 80)
                            {
                                Ultplayer uPlayer = new Ultplayer(players.ElementAt(i2).Angle, (players.ElementAt(i1).Angle - players.ElementAt(i2).Angle), players.ElementAt(i2).Hero);
                                Bundle.Add(uPlayer);
                            }
                        }
                    }
                }
                BTARG.Add(Bundle);
                Bundle = new List<Ultplayer>();
            }
            return BTARG;
        }

        public static double GetAngle(Vector2 StartPos, Vector2 EndPos)
        {
            return Math.Atan2((EndPos.Y - StartPos.Y), (EndPos.X - StartPos.X));
        }

        /*
        private static Vector3 RCastPos()
        {
            foreach(var Tlist in GetBundle(PlayersAngle()))
            {
                var AutoUltF = Option.Item("AutoUltF").GetValue<Slider>().Value;
                var AutoUltnF = Option.Item("AutoUltnF").GetValue<Slider>().Value;

                if (Tlist.Count() > 2 && Tlist.Count() >= AutoUltnF)
                {
                    return Vector3.Zero;
                }
            }
            return Vector3.Zero;
        }*/

        #endregion

        #region onGameLoad
        private static void onGameLoad(EventArgs args)
        {
            try
            {
                Player = ObjectManager.Player;
                if (Player.CharData.BaseSkinName != ChampionName) return;

                Game.OnUpdate += OnTick;
                Drawing.OnDraw += OnDraw;
                Spellbook.OnCastSpell += Spellbook_OnCastSpell;
                Game.OnWndProc += Game_OnWndProc;

                Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

                Targetlist(TargetingMode.AutoPriority);

                Q = new Spell(SpellSlot.Q, 850f);
                Q.SetSkillshot(0.6f, 75f, float.MaxValue, false, SkillshotType.SkillshotCircle);

   //             Q = new Spell(SpellSlot.Q, 850f);
   //             Q.SetSkillshot(0.75f, Q.Instance.SData.CastRadius, float.MaxValue, false, SkillshotType.SkillshotCircle);

                W = new Spell(SpellSlot.W, 850f);
                W.SetSkillshot(0.5f, W.Instance.SData.CastRadius, W.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotCircle);

                E = new Spell(SpellSlot.E, 700f);
                E.SetTargetted(0.2f, float.MaxValue);
                R = new Spell(SpellSlot.R, 82500f);
                //R = new Spell(SpellSlot.R, 825f);
                R.SetSkillshot(0.3f, (float)(80 * Math.PI / 180 ), float.MaxValue, false, SkillshotType.SkillshotCone);

                SpellList.Add(Q);
                SpellList.Add(W);
                SpellList.Add(E);
                SpellList.Add(R);

                Option = new Menu("XD-Crew", "XD-Crew Cassio", true);
                Option.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
                Orbwalker = new Orbwalking.Orbwalker(Option.SubMenu("Orbwalking"));

                Option.AddItem(new MenuItem("TargetingMode", "Target Mode").SetValue(new StringList(Enum.GetNames(typeof(TargetingMode)))));
                Option.SubMenu("Aiming").AddItem(new MenuItem("AimMode", "Aim Mode").SetValue(new StringList(Enum.GetNames(typeof(AimMode)))));
                Option.SubMenu("Aiming").AddItem(new MenuItem("Hitchance", "Hitchance Mode").SetValue(new StringList(Enum.GetNames(typeof(HitChance)))));
                Option.AddItem(new MenuItem("Edelay", "Ecombo delay").SetValue(new Slider(0, 0, 5)));
                Option.SubMenu("Farming").AddItem(new MenuItem("Qlaneclear", "Q Lane Clear").SetValue(true));
                Option.SubMenu("Farming").AddItem(new MenuItem("Wlaneclear", "W Lane Clear").SetValue(true));
                Option.SubMenu("Farming").AddItem(new MenuItem("Elasthit", "E Lasthit non psn").SetValue(true));
                Option.SubMenu("Farming").AddItem(new MenuItem("LaneClearMana", "Lane Clear Mana").SetValue(new Slider(70, 0, 100)));
               // Option.SubMenu("Ultimate").AddItem(new MenuItem("BlockR", "BlockR").SetValue(true));
                Option.SubMenu("Ultimate").AddItem(new MenuItem("AutoUlt", "AutoUltimate").SetValue(false));
                Option.SubMenu("Ultimate").AddItem(new MenuItem("AutoUltF", "AutoUlt facing").SetValue(new Slider(3, 0, 5)));
                Option.SubMenu("Ultimate").AddItem(new MenuItem("AutoUltnF", "AutoUlt not facing").SetValue(new Slider(5, 0, 5)));
                Option.SubMenu("Ultimate").AddItem(new MenuItem("AssistedUltKey", "Assisted Ult Key").SetValue((new KeyBind("R".ToCharArray()[0], KeyBindType.Press))));
                Option.SubMenu("Drawing").AddItem(new MenuItem("DrawQ", "DrawQ").SetValue(true));
                Option.SubMenu("Drawing").AddItem(new MenuItem("DrawP", "Draw Prediction").SetValue(true));
                Option.AddItem(new MenuItem("MutePlayers", "Mute all Enemys on Load").SetValue(false));
                Option.AddItem(new MenuItem("Fun", "Killspam").SetValue(false));
                Option.AddToMainMenu();

                var MutePlayers = Option.Item("MutePlayers").GetValue<bool>();

                if (MutePlayers)
                    MuteEnemy();


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
#endregion

#region OnTick
        private static void OnTick(EventArgs args)
        {

            try
            {
                var menuItem = Option.Item("TargetingMode").GetValue<StringList>();
                Enum.TryParse(menuItem.SList[menuItem.SelectedIndex], out TMode);
                
                var AutoUlt = Option.Item("AutoUlt").GetValue<bool>();
                /*
                if (AutoUlt)
                    CastAutoUltimate();*/

                var Fun = Option.Item("Fun").GetValue<bool>();

                foreach (var enemy in Targets)
                    if (enemy.IsValid && enemy.IsDead && Player.ChampionsKilled > kills && Fun)
                    {
                        kills = Player.ChampionsKilled;
                        Game.Say("/all " + generateMessage() + " " + enemy.Name);
                    }
                
                switch (Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        Combo();
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        Harass();
                        break;
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        JungleClear();
                        WaveClear();
                        break;
                    case Orbwalking.OrbwalkingMode.LastHit:
                        Freeze();
                        break;
                    default:
                        break;
                }
                switch (TMode)
                {
                    case TargetingMode.AutoPriority:
                        if (listed == false)
                        Targetlist(TargetingMode.AutoPriority);
                        listed = true;
                        break;
                    case TargetingMode.FastKill:
                        Targetlist(TargetingMode.FastKill);
                        listed = false;
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
#endregion

#region BeforeAttack
        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if ((Player.Mana < E.Instance.ManaCost) || (E.Instance.Level == 0) || ((E.Instance.CooldownExpires - Game.ClockTime) > 0.7) || Player.HasBuffOfType(BuffType.Silence))
                {
                    args.Process = true;
                    aastatus = true;
                }
                else
                {
                    args.Process = false;
                    aastatus = false;
                }
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                var LaneClearMana = Option.Item("LaneClearMana").GetValue<Slider>().Value;
                if ((Player.ManaPercentage() < LaneClearMana) || (E.Instance.Level == 0) || ((E.Instance.CooldownExpires - Game.ClockTime) > 0.7) || Nopsntarget || Player.HasBuffOfType(BuffType.Silence))
                {
                    args.Process = true;
                    aastatus = true;
                }
                else
                {
                    args.Process = false;
                    aastatus = false;
                }
            }
        }
#endregion

#region Combo

        public static void Combo()
        {
            var menuItem3 = Option.Item("AimMode").GetValue<StringList>();
            Enum.TryParse(menuItem3.SList[menuItem3.SelectedIndex], out AMode);

            var menuItem2 = Option.Item("Hitchance").GetValue<StringList>();
            Enum.TryParse(menuItem2.SList[menuItem2.SelectedIndex], out Chance);
            var EDelay = Option.Item("Edelay").GetValue<Slider>().Value;

            if (E.IsReady() && GetETarget() != null)
            {
                if (Environment.TickCount >= LastECast + (EDelay * 100))
                E.Cast(GetETarget());
            }

            if (Q.IsReady())
            {
                switch (AMode)
                {
                    case AimMode.HitChance:
                        Q.CastIfHitchanceEquals(GetQTarget(), Chance, false);
                        break;
                    case AimMode.Normal:
                        Q.Cast(Q.GetPrediction(GetQTarget(), true).CastPosition);
                        break;
                    case AimMode.XDMode:
                        Q.Cast(PreCastPos(GetQTarget(), 0.6f));
                        break;
                }
            }
            if (W.IsReady() && Environment.TickCount > LastQCast + Q.Delay * 1000)
            {
                switch (AMode)
                {
                    case AimMode.HitChance:
                        W.CastIfHitchanceEquals(GetWTarget(), Chance, false);
                        break;
                    case AimMode.Normal:
                        W.Cast(Q.GetPrediction(GetWTarget(), true).CastPosition);
                        break;
                    case AimMode.XDMode:
                        W.Cast(PreCastPos(GetWTarget(), Player.ServerPosition.Distance(GetWTarget().ServerPosition) / W.Speed));
                        break;
                }
            }

        }
#endregion

#region Harras

        public static void Harass()
        {
            var menuItem3 = Option.Item("AimMode").GetValue<StringList>();
            Enum.TryParse(menuItem3.SList[menuItem3.SelectedIndex], out AMode);

            var menuItem2 = Option.Item("Hitchance").GetValue<StringList>();
            Enum.TryParse(menuItem2.SList[menuItem2.SelectedIndex], out Chance);

            if (E.IsReady() && GetETarget() != null)
            {
                E.Cast(GetETarget());
            }/*
            if (Q.IsReady())
            {
                Q.Cast(PreCastPos(GetQTarget(), 0.6f));
            }*/
            
            if (Q.IsReady() && (Player.ServerPosition.Distance(Q.GetPrediction(GetQTarget(), true).CastPosition) < Q.Range))
            {

                Q.CastIfHitchanceEquals(GetQTarget(), HitChance.VeryHigh, false);

            }
        }

#endregion

#region Jungle

        public static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (!mobs.Any())
                return;

            var mob = mobs.First();

            if (Q.IsReady() && mob.IsValidTarget(Q.Range))
            {
                Q.Cast(mob.ServerPosition);
            }

            if (E.IsReady() && mob.HasBuffOfType(BuffType.Poison) && mob.IsValidTarget(E.Range))
            {
                E.Cast(mob);
            }

            if (W.IsReady() && mob.IsValidTarget(W.Range))
            {
                W.Cast(mob.ServerPosition);
            }

        }

#endregion

#region Farm
        public static void WaveClear()
        {
            if (!Orbwalking.CanMove(40)) return;

            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All, MinionTeam.Enemy).ToList();
            var allMinionsW = MinionManager.GetMinions(Player.ServerPosition, W.Range + W.Width, MinionTypes.All, MinionTeam.Enemy).ToList();
            var allMinionsQnopsn = MinionManager.GetMinions(Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All, MinionTeam.Enemy).Where(x => !x.HasBuffOfType(BuffType.Poison) || GetPoisonBuffEndTime(x) <= (Game.Time + Q.Delay)).ToList();
            var rangedMinionsQnopsn = MinionManager.GetMinions(Player.ServerPosition, Q.Range + Q.Width, MinionTypes.Ranged, MinionTeam.Enemy).Where(x => !x.HasBuffOfType(BuffType.Poison) || GetPoisonBuffEndTime(x) <= (Game.Time + Q.Delay)).ToList();
            var allMinionsWnopsn = MinionManager.GetMinions(Player.ServerPosition, W.Range + W.Width, MinionTypes.All, MinionTeam.Enemy).Where(x => !x.HasBuffOfType(BuffType.Poison) || GetPoisonBuffEndTime(x) <= (Game.Time + W.Delay)).ToList();
            var rangedMinionsWnopsn = MinionManager.GetMinions(Player.ServerPosition, W.Range + W.Width, MinionTypes.Ranged, MinionTeam.Enemy).Where(x => !x.HasBuffOfType(BuffType.Poison) || GetPoisonBuffEndTime(x) <= (Game.Time + W.Delay)).ToList();

            var Qlaneclear = Option.Item("Qlaneclear").GetValue<bool>();
            var Wlaneclear = Option.Item("Wlaneclear").GetValue<bool>();
            var LaneClearMana = Option.Item("LaneClearMana").GetValue<Slider>().Value;

            if (allMinionsQnopsn.Count() == allMinionsQ.Count())
                Nopsntarget = true;
            else
                Nopsntarget = false;

            if (Q.IsReady() && allMinionsQnopsn.Count() == allMinionsQ.Count() && Qlaneclear)
            {
                var FLr = Q.GetCircularFarmLocation(rangedMinionsQnopsn, Q.Width);
                var FLa = Q.GetCircularFarmLocation(allMinionsQnopsn, Q.Width);

                if (FLr.MinionsHit >= 3 && Player.Distance(FLr.Position) < (Q.Range + Q.Width))
                {
                    Q.Cast(FLr.Position);
                    return;
                }
                else
                    if (FLa.MinionsHit >= 2 || allMinionsQnopsn.Count() == 1 && Player.Distance(FLr.Position) < (Q.Range + Q.Width))
                    {
                        Q.Cast(FLa.Position);
                        return;
                    }
            }

            if (W.IsReady() && allMinionsWnopsn.Count() == allMinionsW.Count() && Wlaneclear && Environment.TickCount > (LastQCast + Q.Delay * 1000))
            {
                var FLr = W.GetCircularFarmLocation(rangedMinionsWnopsn, W.Width);
                var FLa = W.GetCircularFarmLocation(allMinionsWnopsn, W.Width);

                if (FLr.MinionsHit >= 3 && Player.Distance(FLr.Position) < (W.Range + W.Width))
                {
                    W.Cast(FLr.Position);
                    return;
                }
                else
                    if (FLa.MinionsHit >= 2 || allMinionsWnopsn.Count() == 1 && Player.Distance(FLr.Position) < (W.Range + W.Width))
                    {
                        W.Cast(FLa.Position);
                        return;
                    }
            }

            if (E.IsReady())
            {
                var MinionList = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);

                foreach (var minion in MinionList.Where(x => x.HasBuffOfType(BuffType.Poison)))
                {
                    var buffEndTime = GetPoisonBuffEndTime(minion);
                    if (buffEndTime > Game.Time + E.Delay)
                    {
                        if (Player.GetSpellDamage(minion, SpellSlot.E) > minion.Health || Player.ManaPercentage() > LaneClearMana)
                        {
                            E.Cast(minion);
                        }
                    }
                }
            }

        }

#endregion

#region Lasthit

        public static void Freeze()
        {
            var elasthit = Option.Item("Elasthit").GetValue<bool>();
            if (!Orbwalking.CanMove(40)) return;

            if (E.IsReady())
            {
                var MinionListE = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);

                foreach (var minion in MinionListE.Where(x => Player.GetSpellDamage(x, SpellSlot.E) > x.Health))
                {
                    if ((minion.HasBuffOfType(BuffType.Poison) && (GetPoisonBuffEndTime(minion) > Game.Time + E.Delay)) || elasthit)
                    {
                        E.Cast(minion);
                    }

                }
            }

        }

#endregion

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (MenuGUI.IsChatOpen)
                return;
            
            var AssistedUltKey = Option.Item("AssistedUltKey").GetValue<KeyBind>().Key;

            if (args.WParam == AssistedUltKey)
            {
                args.Process = false;
                CastAssistedUlt();
            }
            
            if (args.Msg == (uint)WindowsMessages.WM_LBUTTONDOWN)
            {

            MainTarget =
                HeroManager.Enemies
                    .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000)
                    .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();;
            }

        }
#region Ultimate
        
        public static void CastAssistedUlt()
        {
            int tbuffer = 0;
            foreach (var target in Targets)
            {
                if (target != null && target.IsVisible && !target.IsDead && (Player.ServerPosition.Distance(R.GetPrediction(target, true).CastPosition) < R.Range))// && R.WillHit(target,R.GetPrediction(target, true).CastPosition))
                {
                    Game.PrintChat(R.GetPrediction(target, true).AoeTargetsHitCount.ToString());
                    if (tbuffer < R.GetPrediction(target, true).AoeTargetsHit.Count())
                    {
                        tbuffer = R.GetPrediction(target, true).AoeTargetsHit.Count();
                        starget = target;
                    }
                }
            }
            if (starget != null)
            {
                R.Cast(R.GetPrediction(starget, true).CastPosition);
            }
        }


        public static void CastAutoUltimate()
        {

        }

        public static Tuple<int, List<Obj_AI_Hero>> GetHits(Spell spell)
        {
            var GetenemysHit = ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget() && spell.WillHit(enemy, R.GetPrediction(enemy, true).CastPosition)).ToList();

            return new Tuple<int, List<Obj_AI_Hero>>(GetenemysHit.Count, GetenemysHit);
        }
#endregion

        static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {/*
            var BlockR = Option.Item("BlockR").GetValue<bool>();

            if (args.Slot == SpellSlot.R && GetHits(R).Item1 == 0 && BlockR)
                    args.Process = false;*/
            if (args.Slot == SpellSlot.Q)
                LastQCast = Environment.TickCount;
            if (args.Slot == SpellSlot.E)
                LastECast = Environment.TickCount;
        }

        static void MuteEnemy()
        {
            foreach (var enemy in Targets)
        {
            Game.Say("/Mute " + enemy.Name);
        }
        }

        internal static Vector2 PositionAfter(Obj_AI_Base unit, float t, float speed = float.MaxValue)
        {
            var distance = t * speed;
            var path = unit.GetWaypoints();

            for (var i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                var d = a.Distance(b);

                if (d < distance)
                {
                    distance -= d;
                }
                else
                {
                    return a + distance * (b - a).Normalized();
                }
            }


            return path[path.Count - 1];
        }


        private static Paths WPPolygon(Obj_AI_Hero Hero)
        {
            List<Vector2Time> HeroPath = Hero.GetWaypointsWithTime();
            Vector2 myPath;
            Paths WPPaths = new Paths();
            for (var i = 0; i < HeroPath.Count() - 1; i++)
            {
                if (HeroPath.ElementAt<Vector2Time>(i + 1).Time <= 0.6f)
                {
                    Geometry.Polygon.Rectangle WPRectangle = new Geometry.Polygon.Rectangle(HeroPath.ElementAt<Vector2Time>(i).Position, HeroPath.ElementAt<Vector2Time>(i + 1).Position, Hero.BoundingRadius);
                    Geometry.Polygon.Circle Box = new Geometry.Polygon.Circle(HeroPath.ElementAt<Vector2Time>(i).Position, Hero.BoundingRadius);
                    WPPaths.Add(Box.ToClipperPath());
                    WPPaths.Add(WPRectangle.ToClipperPath());
                }
                else
                {
                    myPath = PositionAfter(Hero, 0.6f, Hero.MoveSpeed);
                    Geometry.Polygon.Rectangle WPRectangle = new Geometry.Polygon.Rectangle(HeroPath.ElementAt<Vector2Time>(i).Position, myPath, Hero.BoundingRadius);
                    Geometry.Polygon.Circle Box = new Geometry.Polygon.Circle(myPath, Hero.BoundingRadius);
                    WPPaths.Add(Box.ToClipperPath());
                    WPPaths.Add(WPRectangle.ToClipperPath());
                    break;
                }
            }
            Geometry.Polygon.Circle WPFirstBox = new Geometry.Polygon.Circle(HeroPath.First<Vector2Time>().Position, Hero.BoundingRadius);
            WPPaths.Add(WPFirstBox.ToClipperPath());
            return WPPaths;
        }

        private static void InterceptionQ(Obj_AI_Hero Enemy)
        {
            Geometry.Polygon.Circle Qspellpoly = new Geometry.Polygon.Circle(PreCastPos(Enemy, 0.6f), 130f);

            Paths subjs = new Paths();
            foreach (var bla in WPPolygon(Enemy).ToPolygons())
            {
                subjs.Add(bla.ToClipperPath());
            }

            Paths clips = new Paths(1);
            clips.Add(Qspellpoly.ToClipperPath());

            Paths solution = new Paths();
            Clipper c = new Clipper();
            c.AddPaths(subjs, PolyType.ptSubject, true);
            c.AddPaths(clips, PolyType.ptClip, true);
            c.Execute(ClipType.ctIntersection, solution);

            foreach (var bli in solution.ToPolygons())
            {
                bli.Draw(System.Drawing.Color.Blue);
            }
        }

        public static Vector2 GetLine(Vector2 StartPos, Vector2 EndPos, double Range)
        {
            Vector2 Pos;
            double x, y;

            x = StartPos.X + Math.Cos(GetAngle(StartPos, EndPos)) * Range;
            y = StartPos.Y + Math.Sin(GetAngle(StartPos, EndPos)) * Range;

            Pos.X = (float)x;
            Pos.Y = (float)y;

            return Pos;
        }
        public static Vector2 GetLine2(Vector2 StartPos, double Angle, double Range)
        {
            Vector2 Pos;
            double x, y;

            x = StartPos.X + Math.Cos(Angle) * Range;
            y = StartPos.Y + Math.Sin(Angle) * Range;

            Pos.X = (float)x;
            Pos.Y = (float)y;

            return Pos;
        }


        #region Draw

        private static void OnDraw(EventArgs args)
        {
            var DrawQ = Option.Item("DrawQ").GetValue<bool>();
            var DrawP = Option.Item("DrawP").GetValue<bool>();
            var menuItem3 = Option.Item("AimMode").GetValue<StringList>();
            Enum.TryParse(menuItem3.SList[menuItem3.SelectedIndex], out AMode);
            try
            {
                //Drawing.DrawText(100, 100, System.Drawing.Color.Red, GetEnemyinUltRange().Count().ToString());
                //Drawing.DrawText(100, 110, System.Drawing.Color.Red, GetEnemyinUltRangefacing().Count().ToString());
                
                if (PlayersAngle().Count() > 0)
                {
                    if (GetBundle(PlayersAngle()).Count() > 0)
                    {
                        for (var i1 = 0; i1 < GetBundle(PlayersAngle()).Count(); i1++)
                        {
                            for (var i2 = 0; i2 < GetBundle(PlayersAngle()).ElementAt(i1).Count(); i2++)
                            {
                                Drawing.DrawText(100 + 200 * i1, 100 + 10 * i2, System.Drawing.Color.Red, (GetBundle(PlayersAngle()).ElementAt(i1).ElementAt(i2).Hero.Name).ToString());
                            }
                        }

                    }
                }/*
                if (PlayersAngle().Count() > 0)
                {
                    for (var iA = 0; iA < PlayersAngle().Count();iA++)
                    {
                        Drawing.DrawText(100, 100 + 10 * iA, System.Drawing.Color.Red, PlayersAngle().ElementAt(iA).Hero.Name.ToString()+ ":" + PlayersAngle().ElementAt(iA).Angle.ToString());
                    }
                }

                if (GetEnemyinUltRange() != null)
                {
                    for (var iA = 0; iA < GetEnemyinUltRange().Count(); iA++)
                    {
                        Drawing.DrawLine(Drawing.WorldToScreen(ObjectManager.Player.Position).X, Drawing.WorldToScreen(ObjectManager.Player.Position).Y, GetLine(Drawing.WorldToScreen(Player.Position), Drawing.WorldToScreen(GetEnemyinUltRange().ElementAt(iA).Position),200).X, GetLine(Drawing.WorldToScreen(Player.Position), Drawing.WorldToScreen(GetEnemyinUltRange().ElementAt(iA).Position), 200).Y, 2, System.Drawing.Color.Red);
                    }
 
                }
                /*
                if (DrawP)
                {
                    foreach (var enemy in Targets)
                    {
                        if (enemy.IsVisible && !enemy.IsDead)
                        {
                            switch (AMode)
                            {
                                case AimMode.HitChance:
                                    Render.Circle.DrawCircle(Q.GetPrediction(enemy, true).CastPosition, Q.Width, System.Drawing.Color.Green);
                                    break;
                                case AimMode.Normal:
                                    Render.Circle.DrawCircle(Q.GetPrediction(enemy, true).CastPosition, Q.Width, System.Drawing.Color.Green);
                                    break;
                                case AimMode.XDMode:
                                    Render.Circle.DrawCircle(PreCastPos(enemy, 0.6f), Q.Width, System.Drawing.Color.Green);
                                    break;
                            }
                            foreach (var Poly in WPPolygon(enemy).ToPolygons())
                            {
                                Poly.Draw(System.Drawing.Color.White);
                            }
                            InterceptionQ(enemy);
                        }
                    }
                }*/

                if (MainTarget != null && MainTarget.IsVisible)
                    Render.Circle.DrawCircle(MainTarget.Position, 100, System.Drawing.Color.Red);

                if (DrawQ)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Khaki);
            }
            catch (Exception ex)
            {
                Game.PrintChat(ex.ToString());
            }
        }
#endregion

    }
}

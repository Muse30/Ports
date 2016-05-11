using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;
using EloBuddy.SDK.Menu.Values;
using Spell = LeagueSharp.Common.Spell;
using EloBuddy.SDK.Menu;

namespace HeavenStrikeAzir
{
    class Program
    {
        public static AIHeroClient Player { get { return ObjectManager.Player; } }


        public static Spell _q, _w, _e, _r , _q2, _r2;

        public static Menu azirMenu, spellMenu, comboMenu, harassMenu, autoMenu, drawMenu;

        public static int qcount,ecount;
        public static bool Eisready { get { return Player.Mana >= _e.ManaCost && Utils.GameTimeTickCount - ecount >= _e.Instance.Cooldown * 1000f; } }

        public static string
            drawQ = "Draw Q", drawW = "Draw W", drawQE = "Draw Q+E", drawInsec = "Draw Insec";

        public static void Game_OnGameLoad()
        {
            //Verify Champion
            if (Player.ChampionName != "Azir")
                return;


            //Spells
            _q = new Spell(SpellSlot.Q, 1175);
            _q2 = new Spell(SpellSlot.Q);
            _w = new Spell(SpellSlot.W, 450);
            _e = new Spell(SpellSlot.E, 1100);
            _r = new Spell(SpellSlot.R, 250);
            _r2 = new Spell(SpellSlot.R);
            // from detuks :D
            _q.SetSkillshot(0.0f, 65, 1500, false, SkillshotType.SkillshotLine);
            _q.MinHitChance = HitChance.Medium;


            azirMenu = MainMenu.AddMenu("Azir", "Azir");

            spellMenu = azirMenu.AddSubMenu("Spells Options");
            spellMenu.Add("EQmouse", new KeyBind("E Q to mouse", false, KeyBind.BindTypes.HoldActive, "G".ToCharArray()[0]));
            spellMenu.Add("insec", new KeyBind("Insec Selected", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));
            spellMenu.Add("insecmode", new ComboBox("Insec Mode", 1, "nearest ally", "nearest turret", "mouse", "last key press"));
            spellMenu.Add("insecpolar", new KeyBind("Insec point key", false, KeyBind.BindTypes.HoldActive, "N".ToCharArray()[0]));
            spellMenu.Add("EQdelay", new Slider("EQ lower delay", 100, 0, 300));

            comboMenu = azirMenu.AddSubMenu("Combo Options");
            comboMenu.Add("QC", new CheckBox("Q", true));
            comboMenu.Add("WC", new CheckBox("W", true));
            comboMenu.Add("donotqC", new CheckBox("Save Q if target in soldier's range", false));


            harassMenu = azirMenu.AddSubMenu("Harass Options");
            harassMenu.Add("QH", new CheckBox("Q", true));
            harassMenu.Add("WH", new CheckBox("W", true));
            harassMenu.Add("donotqH", new CheckBox("Save Q if target in soldier's range", false));

            autoMenu = azirMenu.AddSubMenu("Auto Options");
            autoMenu.Add("RKS", new CheckBox("use R KS", true));
            autoMenu.Add("RTOWER", new CheckBox("R target to Tower", true));
            autoMenu.Add("RGAP", new CheckBox("R anti GAP", true));

            drawMenu = azirMenu.AddSubMenu("Drawing Options");
            drawMenu.Add(drawQ, new CheckBox(drawQ, true));
            drawMenu.Add(drawW, new CheckBox(drawW, true));
            drawMenu.Add(drawInsec, new CheckBox(drawInsec, true));

            GameObjects.Initialize();
            Soldiers.AzirSoldier();
            OrbwalkCommands.Initialize();
            AzirCombo.Initialize();
            AzirHarass.Initialize();
            AzirFarm.Initialize();
            JumpToMouse.Initialize();
            Insec.Initialize();

            //Listen to events
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnDoCast;
            //Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private static void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;
        }

        private static bool Getcheckboxvalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<CheckBox>().CurrentValue;
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var target = gapcloser.Sender;
            if (target.IsEnemy && _r.IsReady() && target.IsValidTarget() && !target.IsZombie && RGAP)
            {
                if (target.IsValidTarget(250)) _r.Cast(target.Position);
            }
        }
        public static int EQdelay { get { return spellMenu["EQdelay"].Cast<Slider>().CurrentValue; } }
        public static bool drawinsecLine { get { return drawMenu[drawInsec].Cast<CheckBox>().CurrentValue; } }
        public static uint insecpointkey { get { return spellMenu["insecpolar"].Cast<KeyBind>().Keys.Item1; } }
        public static bool eqmouse { get { return spellMenu["EQmouse"].Cast<KeyBind>().CurrentValue; } }
        public static bool RTOWER { get { return autoMenu["RTOWER"].Cast<CheckBox>().CurrentValue; } }
        public static bool RKS { get { return autoMenu["RKS"].Cast<CheckBox>().CurrentValue; } }
        public static bool RGAP { get { return autoMenu["RGAP"].Cast<CheckBox>().CurrentValue; } }
        public static bool qcombo { get { return comboMenu["QC"].Cast<CheckBox>().CurrentValue; } }
        public static bool wcombo { get { return comboMenu["WC"].Cast<CheckBox>().CurrentValue; } }
        public static bool donotqcombo { get { return comboMenu["donotqC"].Cast<CheckBox>().CurrentValue; } }
        public static bool qharass { get { return harassMenu["QH"].Cast<CheckBox>().CurrentValue; } }
        public static bool wharass { get { return harassMenu["WH"].Cast<CheckBox>().CurrentValue; } }
        public static bool donotqharass { get { return harassMenu["donotqH"].Cast<CheckBox>().CurrentValue; } }
        public static bool insec { get { return spellMenu["insec"].Cast<KeyBind>().CurrentValue; } }
        public static int insecmode { get { return spellMenu["insecmode"].Cast<ComboBox>().CurrentValue; } }



        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name.ToLower().Contains("azirq"))
            {
                Qtick = Utils.GameTimeTickCount;
                qcount = Utils.GameTimeTickCount;
 
            }
            if (args.SData.Name.ToLower().Contains("azirw"))
            {

            }
            if (args.SData.Name.ToLower().Contains("azire"))
            {
                ecount = Utils.GameTimeTickCount;

            }
            if (args.SData.Name.ToLower().Contains("azirr"))
            {

            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            Auto();
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Getcheckboxvalue(drawMenu, drawQ))
                Render.Circle.DrawCircle(Player.Position, _q.Range, Color.Yellow);
            if (Getcheckboxvalue(drawMenu, drawW))
                Render.Circle.DrawCircle(Player.Position, _w.Range, Color.Yellow);
        }

        private static void Auto()
        {
            if (RKS)
            {
                if (_r.IsReady())
                {
                    foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget(250) && !x.IsZombie && x.Health < _r.GetDamage(x)))
                    {
                        _r.Cast(hero.Position);
                    }
                }
            }
            if(RTOWER)
            {
                if (_r.IsReady())
                {
                    var turret = ObjectManager.Get<Obj_AI_Turret>().Where(x => x.IsAlly && !x.IsDead).OrderByDescending(x => x.Distance(Player.Position)).LastOrDefault();
                    foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget(250) && !x.IsZombie))
                    {
                        if (Player.ServerPosition.Distance(turret.Position)+100 >= hero.Distance(turret.Position) && hero.Distance(turret.Position) <= 775 + 250)
                        {
                            var pos = Player.Position.Extend(turret.Position, 250);
                            _r.Cast(pos);
                        }
                    }
                }
            }
        }



        public static bool  Qisready()
        {
            if (Utils.GameTimeTickCount - Qtick >= _q.Instance.Cooldown * 1000)
            {
                return true;
            }
            else
                return false;
        }
        public static int Qtick;

        public static double Wdamage(Obj_AI_Base target)
        {
            return Player.CalcDamage(target, DamageType.Magical,
                        new double[]
                        {
                            50, 55, 60, 65, 70, 75, 80, 85, 90, 100, 110, 120, 130,
                            140, 150, 160, 170, 180
                        }[Player.Level - Player.SpellTrainingPoints - 1] + 0.6 * Player.FlatMagicDamageMod);
        }
        
    }
}

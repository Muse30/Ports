#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp.Common;
using SharpDX;
using Geometry = LeagueSharp.Common.Geometry;
using EloBuddy.SDK.Menu;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using Spell = LeagueSharp.Common.Spell;
using Utility = LeagueSharp.Common.Utility;
using Prediction = LeagueSharp.Common.Prediction;

using EloBuddy.SDK.Events;


#endregion

namespace Ports.Lucian
{
    internal class Program
    {
        private const string ChampionName = "Lucian";

        private static Spell _q, _q1, _w, _w2, _e, _r;

        public static Menu LucianMenu, comboMenu, harassMenu, laneClearMenu, lasthitMenu, jungleMenu, itemsMenu, miscMenu, drawMenu;

        public static bool Qcast, Wcast, Ecast;

        private static AIHeroClient _player;

        private static Items.Item _youmuu, _blade, _bilge;

        public static void Game_OnGameLoad()
        {
            _player = ObjectManager.Player;

            if (_player.ChampionName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 675);
            _q1 = new Spell(SpellSlot.Q, 1200);
            _w = new Spell(SpellSlot.W, 1000, DamageType.Magical);
            _w2 = new Spell(SpellSlot.W, 1000, DamageType.Magical);
            _e = new Spell(SpellSlot.E, 475f);
            _r = new Spell(SpellSlot.R, 1200);


            _q.SetTargetted(0.25f, 1400f);
            _q1.SetSkillshot(0.5f, 50, float.MaxValue, false, SkillshotType.SkillshotLine);
            _w.SetSkillshot(0.30f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            _w2.SetSkillshot(0.30f, 80f, 1600f, false, SkillshotType.SkillshotCircle);
            _r.SetSkillshot(0.2f, 110f, 2500, true, SkillshotType.SkillshotLine);

            _youmuu = new Items.Item(3142, 10);
            _bilge = new Items.Item(3144, 450f);
            _blade = new Items.Item(3153, 450f);


            LucianMenu = MainMenu.AddMenu("Lucian", "Lucian");

            comboMenu = LucianMenu.AddSubMenu("Combo Options");
            comboMenu.Add("UseQC", new CheckBox("Use Q"));
            comboMenu.Add("UseWC", new CheckBox("Use W"));
            comboMenu.Add("UseEC", new CheckBox("Use E"));
            comboMenu.Add("useRaim", new KeyBind("Use R(Semi-Manual)", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));

            harassMenu = LucianMenu.AddSubMenu("Harass Options");
            harassMenu.Add("UseQH", new CheckBox("Use Q"));
            harassMenu.Add("UseWH", new CheckBox("Use W"));
            harassMenu.Add("harasstoggle", new KeyBind("AutoHarass (toggle)", false, KeyBind.BindTypes.PressToggle, "L".ToCharArray()[0]));
            harassMenu.Add("Harrasmana", new Slider("Minimum Mana (%)", 70, 1, 100));


            lasthitMenu = LucianMenu.AddSubMenu("LastHit Options");
            lasthitMenu.Add("UseQLH", new CheckBox("Use Q LastHit"));
            lasthitMenu.Add("UseWLH", new CheckBox("Use W LastHit"));
            lasthitMenu.Add("Lastmana", new Slider("Minimum Mana (%)", 70, 1, 100));


            laneClearMenu = LucianMenu.AddSubMenu("LaneClear Options");
            laneClearMenu.Add("UseQLP", new CheckBox("Use Q To Harass"));
            laneClearMenu.Add("UseQL", new CheckBox("Q LaneClear"));
            laneClearMenu.Add("UseWL", new CheckBox("W LaneClear"));      
            laneClearMenu.Add("minminions", new Slider("Minimum minions to use Q", 3, 1, 5));
            laneClearMenu.Add("minminionsw", new Slider("Minimum minions to use W", 3, 1, 5));
            laneClearMenu.Add("Lanemana", new Slider("Minimum Mana %", 35, 1, 100));

            jungleMenu = LucianMenu.AddSubMenu("JungleClear Options");
            jungleMenu.Add("UseQJ", new CheckBox("Use Q Jungle"));
            jungleMenu.Add("UseWJ", new CheckBox("Use W Jungle"));
            jungleMenu.Add("Junglemana", new Slider("Minimum Mana %", 35, 1, 100));

            itemsMenu = LucianMenu.AddSubMenu("Items Options");
            itemsMenu.Add("Youmuu", new CheckBox("Use Youmuu's"));
            itemsMenu.Add("Bilge", new CheckBox("Use Bilge"));
            itemsMenu.Add("BilgeEnemyhp", new Slider("If Enemy Hp <", 60, 1, 100));
            itemsMenu.Add("Bilgemyhp", new Slider("Or your Hp <", 60, 1, 100));
            itemsMenu.AddSeparator();
            itemsMenu.Add("Blade", new CheckBox("Use Blade"));
            itemsMenu.Add("BladeEnemyhp", new Slider("If Enemy Hp <", 60, 1, 100));
            itemsMenu.Add("Blademyhp", new Slider("Or your Hp <", 60, 1, 100));
            itemsMenu.AddSeparator();
            itemsMenu.AddLabel("Defensive Items");
            itemsMenu.Add("useqss", new CheckBox("Use QSS/Mercurial Scimitar/Dervish Blade"));
            itemsMenu.Add("blind", new CheckBox("Blind"));
            itemsMenu.Add("charm", new CheckBox("Charm"));
            itemsMenu.Add("fear", new CheckBox("Fear"));
            itemsMenu.Add("flee", new CheckBox("Flee"));
            itemsMenu.Add("snare", new CheckBox("Snare"));
            itemsMenu.Add("taunt", new CheckBox("Taunt"));
            itemsMenu.Add("suppression", new CheckBox("Suppression"));
            itemsMenu.Add("stun", new CheckBox("Stun"));
            itemsMenu.Add("polymorph", new CheckBox("Polymorph"));
            itemsMenu.Add("silence", new CheckBox("Silence"));
            itemsMenu.Add("zedultexecute", new CheckBox("Zed Ult"));
            itemsMenu.AddSeparator();
            StringList(itemsMenu, "Cleansemode", "Use Cleanse", new[] { "Always", "Combo" }, 1);


            miscMenu = LucianMenu.AddSubMenu("Misc Options");
            miscMenu.Add("UseQM", new CheckBox("Use Q KillSteal"));
            miscMenu.Add("UseWM", new CheckBox("Use W KillSteal"));
            miscMenu.Add("Gap_E", new CheckBox("GapClosers E"));

            drawMenu = LucianMenu.AddSubMenu("Drawings Options");
            drawMenu.Add("DrawQ", new CheckBox("Draw Q"));
            drawMenu.Add("DrawW", new CheckBox("Draw W"));
            drawMenu.Add("DrawE", new CheckBox("Draw E"));
            drawMenu.Add("DrawR", new CheckBox("Draw R"));
            drawMenu.Add("Drawharass", new CheckBox("Draw Auto Harass"));


            Chat.Print("<font color='#881df2'>D-Lucian by Diabaths</font> Loaded.");
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Spellbook.OnCastSpell += OnCastSpell;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
            Chat.Print(
                "<font color='#f2f21d'>Do you like it???  </font> <font color='#ff1900'>Drop 1 Upvote in Database </font>");
            Chat.Print(
                "<font color='#f2f21d'>Buy me cigars </font> <font color='#ff1900'>ssssssssssmith@hotmail.com</font> (10) S");
        }

        public static void StringList(Menu menu, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            var mode = menu.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ": " + values[mode.CurrentValue];
            mode.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    sender.DisplayName = displayName + ": " + values[args.NewValue];
                };
        }


        private static bool Getcheckboxvalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<CheckBox>().CurrentValue;
        }
        private static bool Getkeybindvalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<KeyBind>().CurrentValue;
        }
        private static int Getslidervalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<Slider>().CurrentValue;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead) return;
            if (Getkeybindvalue(comboMenu, "useRaim")  && _r.IsReady())
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                var t = TargetSelector.GetTarget(_r.Range, DamageType.Physical);
                if (t.IsValidTarget(_r.Range) && !_player.HasBuff("LucianR")) _r.Cast(t.Position);
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && Getkeybindvalue(harassMenu, "harasstoggle") && (100 * (_player.Mana / _player.MaxMana)) > Getslidervalue(harassMenu, "Harrasmana"))
            {
                Harass();

            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)
                && (100 * (_player.Mana / _player.MaxMana)) > Getslidervalue(laneClearMenu, "Lanemana"))
            {
                Laneclear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear)
                && (100 * (_player.Mana / _player.MaxMana)) > Getslidervalue(jungleMenu, "Junglemana"))
            {
                JungleClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit)
                && (100 * (_player.Mana / _player.MaxMana)) > Getslidervalue(lasthitMenu, "Lastmana"))
                
            {
                LastHit();
            }

            _player = ObjectManager.Player;

            Usecleanse();
            KillSteal();
        }

        /* public static bool IsWall(Vector2 vector)
        {
            return NavMesh.GetCollisionFlags(vector.X, vector.Y).HasFlag(CollisionFlags.Wall);
        }*/

        private static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe)
                if (args.Animation == "Spell1" || args.Animation == "Spell2")
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None)) Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }

        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Q)
            {
                Qcast = true;
                Utility.DelayAction.Add(300, () => Qcast = false);
            }
            if (args.Slot == SpellSlot.W)
            {
                Wcast = true;

                Utility.DelayAction.Add(300, () => Wcast = false);
            }

            if (args.Slot == SpellSlot.E)
            {
                Ecast = true;
                Utility.DelayAction.Add(300, () => Ecast = false);
            }

            if (_player.HasBuff("LucianR"))
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                //args.Process = false;
            }
        }

        private static bool HavePassivee => Qcast || Wcast || Ecast || _player.HasBuff("LucianPassiveBuff");

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.Equals("LucianW", StringComparison.InvariantCultureIgnoreCase))
                {
                    Wcast = true;
                    Utility.DelayAction.Add(100, Orbwalker.ResetAutoAttack);
                    Utility.DelayAction.Add(300, () => Wcast = false);
                }
                if (args.SData.Name.Equals("LucianE", StringComparison.InvariantCultureIgnoreCase))
                {
                    Ecast = true;
                    Utility.DelayAction.Add(100, Orbwalker.ResetAutoAttack);
                    Utility.DelayAction.Add(300, () => Ecast = false);
                }
                if (args.SData.Name.Equals("LucianQ", StringComparison.InvariantCultureIgnoreCase))
                {
                    Qcast = true;
                    Utility.DelayAction.Add(100, Orbwalker.ResetAutoAttack);
                    Utility.DelayAction.Add(300, () => Qcast = false);
                }
                if (_player.HasBuff("LucianR"))
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }
            }
        }

        public static void CastQ()
        {
            if (!_q.IsReady()) return;
            var target = TargetSelector.GetTarget(_q.Range, DamageType.Physical);

            if (!target.IsValidTarget(_q.Range))
                return;
            {
                _q.Cast(target);
            }
        }

        public static void ExtendedQ()
        {
            if (!_q.IsReady()) return;
            var target = TargetSelector.GetTarget(_q1.Range, DamageType.Physical);

            if (!target.IsValidTarget(_q1.Range))
                return;

            var qpred = _q1.GetPrediction(target);
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All,
                MinionTeam.NotAlly, MinionOrderTypes.None);
            var champions = HeroManager.Enemies.Where(m => m.Distance(ObjectManager.Player) <= _q.Range);
            var monsters = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            {
                foreach (var minion in from minion in minions
                                       let polygon = new Geometry.Polygon.Rectangle(
                                            ObjectManager.Player.ServerPosition,
                                           ObjectManager.Player.ServerPosition.LSExtend(minion.ServerPosition, _q1.Range), 65f)
                                       where polygon.IsInside(qpred.CastPosition)
                                       select minion)
                {
                    if (minion.IsValidTarget(_q1.Range))
                        _q.Cast(minion);
                }

                foreach (var champ in from champ in champions
                                      let polygon = new Geometry.Polygon.Rectangle(
                                           ObjectManager.Player.ServerPosition,
                                          ObjectManager.Player.ServerPosition.LSExtend(champ.ServerPosition, _q1.Range), 65f)
                                      where polygon.IsInside(qpred.CastPosition)
                                      select champ)
                {
                    if (champ.IsValidTarget(_q1.Range))
                        _q.Cast(champ);
                }

                foreach (var monster in from monster in monsters
                                        let polygon = new Geometry.Polygon.Rectangle(
                                             ObjectManager.Player.ServerPosition,
                                            ObjectManager.Player.ServerPosition.LSExtend(monster.ServerPosition, _q1.Range), 65f)
                                        where polygon.IsInside(qpred.CastPosition)
                                        select monster)
                {
                    if (monster.IsValidTarget(_q1.Range))
                        _q.Cast(monster);
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (_e.IsReady() && gapcloser.Sender.Distance(_player.ServerPosition) <= 200 &&
                Getcheckboxvalue(miscMenu, "Gap_E"))
            {
                _e.Cast(ObjectManager.Player.Position.LSExtend(gapcloser.Sender.Position, -_e.Range));
            }
        }

        private static void Usecleanse()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && _player.IsDead && Getslidervalue(itemsMenu, "Cleansemode") == 1)
             return;

            if (Cleanse(_player) && Getcheckboxvalue(itemsMenu, "useqss"))
            {
                if (_player.HasBuff("zedulttargetmark"))
                {
                    if (Items.HasItem(3140) && Items.CanUseItem(3140))
                        Utility.DelayAction.Add(1000, () => Items.UseItem(3140));
                    else if (Items.HasItem(3139) && Items.CanUseItem(3139))
                        Utility.DelayAction.Add(1000, () => Items.UseItem(3139));
                    else if (Items.HasItem(3137) && Items.CanUseItem(3137))
                        Utility.DelayAction.Add(1000, () => Items.UseItem(3137));
                }
                else
                {
                    if (Items.HasItem(3140) && Items.CanUseItem(3140)) Items.UseItem(3140);
                    else if (Items.HasItem(3139) && Items.CanUseItem(3139)) Items.UseItem(3139);
                    else if (Items.HasItem(3137) && Items.CanUseItem(3137)) Items.UseItem(3137);
                }
            }
        }

        private static bool Cleanse(AIHeroClient hero)
        {
            var cc = false;
            if (Getcheckboxvalue(itemsMenu, "blind"))
            {
                if (hero.HasBuffOfType(BuffType.Blind))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "charm"))
            {
                if (hero.HasBuffOfType(BuffType.Charm))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "fear"))
            {
                if (hero.HasBuffOfType(BuffType.Fear))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "flee"))
            {
                if (hero.HasBuffOfType(BuffType.Flee))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "snare"))
            {
                if (hero.HasBuffOfType(BuffType.Snare))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "taunt"))
            {
                if (hero.HasBuffOfType(BuffType.Taunt))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "suppression"))
            {
                if (hero.HasBuffOfType(BuffType.Suppression))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "stun"))
            {
                if (hero.HasBuffOfType(BuffType.Stun))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "polymorph"))
            {
                if (hero.HasBuffOfType(BuffType.Polymorph))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "silence"))
            {
                if (hero.HasBuffOfType(BuffType.Silence))
                {
                    cc = true;
                }
            }
            if (Getcheckboxvalue(itemsMenu, "zedultexecute"))
            {
                if (_player.HasBuff("zedulttargetmark"))
                {
                    cc = true;
                }
            }
            return cc;
        }

        private static void Combo()
        {
            var useQ = Getcheckboxvalue(comboMenu, "UseQC");
            var useW = Getcheckboxvalue(comboMenu, "UseWC");
            if (useQ && !HavePassivee && !_player.IsDashing())
            {
                var t = TargetSelector.GetTarget(_q1.Range, DamageType.Physical);
                if (t.IsValidTarget(_q1.Range) && _q.IsReady() && !t.HasBuffOfType(BuffType.Invulnerability))
                    ExtendedQ();
                else if (t.IsValidTarget(_q.Range) && _q.IsReady() && !t.HasBuffOfType(BuffType.Invulnerability))
                    CastQ();
            }
            if (useW && _w.IsReady() && !HavePassivee && !_player.IsDashing() && !_q.IsReady())
            {
                var t = TargetSelector.GetTarget(_w.Range, DamageType.Magical);
                var predW = _w.GetPrediction(t);
                if (t.IsValidTarget(_w.Range) && predW.Hitchance >= HitChance.Medium && predW.CollisionObjects.Count == 0)
                    _w.Cast(t, false, true);
                else if (t.IsValidTarget(_w2.Range) && predW.Hitchance >= HitChance.Medium)
                {
                    _w2.Cast(t, false, true);
                }
            }
            var useE = Getcheckboxvalue(comboMenu, "UseEC");
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            if (useE && _e.IsReady() &&
               !_player.HasBuff("LucianR") && !HavePassivee)
            {
                var ta = TargetSelector.GetTarget(_q1.Range, DamageType.Physical);
                if (ta == null) return;
                if (ObjectManager.Player.Position.Extend(Game.CursorPos, 700).CountEnemiesInRange(700) <= 1)
                {
                    if (!ta.UnderTurret() && ta.IsValidTarget(_q.Range) && !Orbwalking.InAutoAttackRange(ta))
                    {
                        _e.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 450));
                    }
                    else if (ta.UnderTurret() && _e.IsReady() && ta.IsValidTarget(_q.Range + _e.Range))
                        if (_q.ManaCost + _e.ManaCost < _player.Mana && ta.Health < _q.GetDamage(ta))
                        {
                            _e.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 450));
                            CastQ();
                        }
                        else if (ta.Health < _player.GetAutoAttackDamage(ta, true) * 2 && ta.IsValidTarget())
                        {
                            _e.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 450));
                        }
                }
            }
            UseItemes();
        }


        private static void Harass()
        {
            var useQ = Getcheckboxvalue(harassMenu, "UseQH");
            var useW = Getcheckboxvalue(harassMenu, "UseWH");

            if (useQ && _q.IsReady() && !HavePassivee && !_player.IsDashing())
            {
                var t = TargetSelector.GetTarget(_q1.Range, DamageType.Physical);
                if (t.IsValidTarget(_q1.Range) && !t.HasBuffOfType(BuffType.Invulnerability))
                    ExtendedQ();
                else if (t.IsValidTarget(_q.Range) && !t.HasBuffOfType(BuffType.Invulnerability))
                    CastQ();
            }
            if (useW && _w.IsReady() && !HavePassivee && !_q.IsReady() && !_player.IsDashing())
            {
                var t = TargetSelector.GetTarget(_w.Range, DamageType.Magical);
                var predW = _w.GetPrediction(t);
                if (t.IsValidTarget(_w.Range) && predW.Hitchance >= HitChance.High && predW.CollisionObjects.Count == 0)
                    _w.Cast(t, false, true);
                else if (t.IsValidTarget(_w2.Range) && predW.Hitchance >= HitChance.High)
                {
                    _w2.Cast(t, false, true);
                }
            }
        }

        private static void Laneclear()
        {
            if (!Orbwalker.CanMove) return;
            var t = TargetSelector.GetTarget(_q1.Range, DamageType.Physical);
            var minion = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range).FirstOrDefault();
            if (minion == null || minion.Name.ToLower().Contains("ward"))
            {
                return;
            }
            var minionhitq = Getslidervalue(laneClearMenu, "minminions");
            var minionhitw = Getslidervalue(laneClearMenu, "minminionsw");
            var useQl = Getcheckboxvalue(laneClearMenu, "UseQL");
            var useWl = Getcheckboxvalue(laneClearMenu, "UseWL");
            var useQlP = Getcheckboxvalue(laneClearMenu, "UseQLP");
            var farmminions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All,
                MinionTeam.NotAlly);
            var minionsq = farmminions.FindAll(qminion => minion.IsValidTarget(_q.Range));
            var minionq = minionsq.Find(minionQ => minionQ.IsValidTarget());

            if (_q.IsReady() && useQl && !HavePassivee)
            {
                foreach (var minionssq in farmminions)
                {
                    var prediction = Prediction.GetPrediction(minionssq, _q.Delay, 10);

                    var collision = _q.GetCollision(_player.Position.To2D(),
                        new List<Vector2> { prediction.UnitPosition.To2D() });
                    foreach (var collisions in collision)
                    {
                        if (collision.Count() >= minionhitq)

                        {
                            if (collision.Last().Distance(_player) - collision[0].Distance(_player) < 600 &&
                                collision[0].Distance(_player) < 500)
                                _q.Cast(collisions);
                        }
                    }
                }
            }
            if (_q.IsReady() && useQlP && !HavePassivee)
                if (_q.IsReady() && t.IsValidTarget(_q1.Range) && !HavePassivee)
                    ExtendedQ();


            if (_w.IsReady() && useWl && !HavePassivee && !_q.IsReady())
            {
                if (_w.GetLineFarmLocation(farmminions).MinionsHit >= minionhitw)
                {
                    _w.Cast(minionq);
                }
            }
        }


        private static void LastHit()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All);
            var useQ = Getcheckboxvalue(lasthitMenu, "UseQLH");
            var useW = Getcheckboxvalue(lasthitMenu, "UseWLH");
            if (allMinions.Count < 3) return;
            foreach (var minion in allMinions)
            {
                if (useQ && _q.IsReady() && minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q) &&
                    !HavePassivee)
                {
                    _q.Cast(minion);
                }

                if (_w.IsReady() && useW && minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.W) &&
                    !HavePassivee)
                {
                    _w.Cast(minion);
                }
            }
        }

        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var useQ = Getcheckboxvalue(jungleMenu, "UseQJ");
            var useW = Getcheckboxvalue(jungleMenu, "UseWJ");
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (useQ && _q.IsReady() && mob.IsValidTarget(_q.Range) && !HavePassivee && !mob.Name.Contains("Mini"))
                {
                    _q.Cast(mob);
                }
                if (_w.IsReady() && useW && mob.IsValidTarget(_w.Range) && !HavePassivee && !mob.Name.Contains("Mini") && !_q.IsReady())
                {
                    _w.Cast(mob);
                }
            }
        }

        private static void UseItemes()
        {
            foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsEnemy))
            {
                var iBilge = Getcheckboxvalue(itemsMenu, "Bilge");
                var iBilgeEnemyhp = hero.Health <=
                                    (hero.MaxHealth * (Getslidervalue(itemsMenu, "BilgeEnemyhp")) / 100);
                var iBilgemyhp = _player.Health <=
                                 (_player.MaxHealth * (Getslidervalue(itemsMenu, "Bilgemyhp")) / 100);
                var iBlade = Getcheckboxvalue(itemsMenu, "Blade");
                var iBladeEnemyhp = hero.Health <=
                                    (hero.MaxHealth * (Getslidervalue(itemsMenu, "BladeEnemyhp")) / 100);
                var iBlademyhp = _player.Health <=
                                 (_player.MaxHealth * (Getslidervalue(itemsMenu, "Blademyhp")) / 100);
                var iYoumuu = Getcheckboxvalue(itemsMenu, "Youmuu");

                if (hero.IsValidTarget(450) && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
                {
                    _bilge.Cast(hero);

                }
                if (hero.IsValidTarget(450) && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
                {
                    _blade.Cast(hero);

                }
                if (hero.IsValidTarget(450) && iYoumuu && _youmuu.IsReady())
                {
                    _youmuu.Cast();
                }
            }
        }



        private static void KillSteal()
        {
            foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsEnemy))
            {
                if (_q.IsReady() && Getcheckboxvalue(miscMenu, "UseQM") && !HavePassivee && !_player.IsDashing())
                {
                    if (_q.GetDamage(hero) > hero.Health && hero.IsValidTarget(_q.Range - 30))
                    {
                        ExtendedQ();
                    }
                    if (_q.GetDamage(hero) > hero.Health && hero.IsValidTarget(_q.Range))
                    {
                        CastQ();
                    }
                }
                if (_w.IsReady() && Getcheckboxvalue(miscMenu, "UseWM") && hero.IsValidTarget(_w.Range)
                    && _w.GetDamage(hero) > hero.Health && !HavePassivee && !_player.IsDashing())
                {
                    var predW = _w.GetPrediction(hero);
                    if (hero.IsValidTarget(_w.Range) && predW.Hitchance >= HitChance.High
                        && predW.CollisionObjects.Count == 0)
                        _w.Cast(hero, false, true);
                    else if (hero.IsValidTarget(_w2.Range) && predW.Hitchance >= HitChance.High)
                    {
                        _w2.Cast(hero, false, true);
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var harass = (Getkeybindvalue(harassMenu, "harasstoggle"));

            if (Getcheckboxvalue(drawMenu, "Drawharass"))
            {
                if (harass)
                {
                    Drawing.DrawText(Drawing.Width * 0.02f, Drawing.Height * 0.92f, System.Drawing.Color.GreenYellow,
                        "Auto harass Enabled");
                }
                else
                    Drawing.DrawText(Drawing.Width * 0.02f, Drawing.Height * 0.92f, System.Drawing.Color.OrangeRed,
                        "Auto harass Disabled");
            }

            if (Getcheckboxvalue(drawMenu, "DrawQ") && _q.Level > 0)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, _q.IsReady() ? System.Drawing.Color.GreenYellow : System.Drawing.Color.OrangeRed);
            }

            if (Getcheckboxvalue(drawMenu, "DrawW") && _w.Level > 0)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _w.Range, System.Drawing.Color.GreenYellow);
            }

            if (Getcheckboxvalue(drawMenu, "DrawE") && _e.Level > 0)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.GreenYellow);
            }

            if (Getcheckboxvalue(drawMenu, "DrawR") && _r.Level > 0)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.GreenYellow);
            }
        }
    }
}
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

namespace Ports.Riven
{
    internal class KurisuRiven
    {
        #region Riven: Main

        private static int lastq;
        private static int lastw;
        private static int laste;
        private static int lastaa;
        private static int lasthd;
        private static int lastwd;

        private static bool canq;
        private static bool canw;
        private static bool cane;
        private static bool canmv;
        private static bool canaa;
        private static bool canws;
        private static bool canhd;
        private static bool hashd;

        public static int LastAATick;

        private static bool didq;
        private static bool didw;
        private static bool dide;
        private static bool didws;
        private static bool didaa;
        private static bool didhd;
        private static bool didhs;
        private static bool ssfl;

        public static Menu rivenMenu, farmMenu, harassMenu, keybindsMenu, qMenu, wMenu, eMenu, r1Menu, r2Menu, drawMenu;

        private static Spell q, w, e, r;
        private static AIHeroClient player = ObjectManager.Player;
        private static HpBarIndicator hpi = new HpBarIndicator();
        private static Obj_AI_Base qtarg; // semi q target

        private static int qq;
        private static int cc;
        private static int pc;
        private static bool uo;
        private static SpellSlot flash;

        private static float truerange;
        private static Vector3 movepos;
        #endregion

        #region Riven: Utils


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

        private static float xtra(float dmg)
        {
            return r.IsReady() ? (float)(dmg + (dmg * 0.2)) : dmg;
        }

        private static bool IsLethal(Obj_AI_Base unit)
        {
            return ComboDamage(unit) / 1.65 >= unit.Health;
        }

        private static Obj_AI_Base GetCenterMinion()
        {
            var minionposition = MinionManager.GetMinions(300 + q.Range).Select(x => x.Position.To2D()).ToList();
            var center = MinionManager.GetBestCircularFarmLocation(minionposition, 250, 300 + q.Range);

            return center.MinionsHit >= 3
                ? MinionManager.GetMinions(1000).OrderBy(x => x.Distance(center.Position)).FirstOrDefault()
                : null;
        }

        private static void TryIgnote(Obj_AI_Base target)
        {
            var ignote = player.GetSpellSlot("summonerdot");
            if (player.Spellbook.CanUseSpell(ignote) == SpellState.Ready)
            {
                if (target.Distance(player.ServerPosition) <= 600)
                {
                    if (cc <= Getslidervalue(r1Menu, "userq") && q.IsReady() && Getcheckboxvalue(r1Menu, "useignote"))
                    {
                        if (ComboDamage(target) >= target.Health &&
                            target.Health / target.MaxHealth * 100 > Getslidervalue(r1Menu, "overk") ||
                           Getkeybindvalue(keybindsMenu, "shycombo"))
                        {
                            if (r.IsReady() && uo)
                            {
                                player.Spellbook.CastSpell(ignote, target);
                            }
                        }
                    }
                }
            }
        }

        private static void useinventoryitems(Obj_AI_Base target)
        {
            if (Items.HasItem(3142) && Items.CanUseItem(3142))
                Items.UseItem(3142);

            if (target.Distance(player.ServerPosition, true) <= 450 * 450)
            {
                if (Items.HasItem(3144) && Items.CanUseItem(3144))
                    Items.UseItem(3144, target);
                if (Items.HasItem(3153) && Items.CanUseItem(3153))
                    Items.UseItem(3153, target);
            }
        }

        private static readonly string[] minionlist =
        {
            // summoners rift
            "SRU_Razorbeak", "SRU_Krug", "Sru_Crab", "SRU_Baron", "SRU_Dragon",
            "SRU_Blue", "SRU_Red", "SRU_Murkwolf", "SRU_Gromp", 
            
            // twisted treeline
            "TT_NGolem5", "TT_NGolem2", "TT_NWolf6", "TT_NWolf3",
            "TT_NWraith1", "TT_Spider"
        };

        #endregion

        public KurisuRiven()
        {
            if (player.ChampionName != "Riven")
            {
                return;
            }

            w = new Spell(SpellSlot.W, 250f);
            e = new Spell(SpellSlot.E, 270f);

            q = new Spell(SpellSlot.Q, 260f);
            q.SetSkillshot(0.25f, 100f, 2200f, false, SkillshotType.SkillshotCircle);

            r = new Spell(SpellSlot.R, 900f);
            r.SetSkillshot(0.25f, (float)(45 * 0.5), 1600f, false, SkillshotType.SkillshotCircle);

            flash = player.GetSpellSlot("summonerflash");
            OnDoCast();

            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
            Interrupter();
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Drawings();
            OnMenuLoad();

            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Chat.Print("<b><font color=\"#66FF33\">Kurisu's Riven</font></b> - Loaded!");

        }

        private static AIHeroClient _sh;
        static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (ulong)WindowsMessages.WM_LBUTTONDOWN)
            {
                _sh = HeroManager.Enemies
                     .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000) // 200 * 200
                     .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();
            }
        }

        private static AIHeroClient riventarget()
        {
            var cursortarg = HeroManager.Enemies
                .Where(x => x.Distance(Game.CursorPos) <= 1400 && x.Distance(player.ServerPosition) <= 1400)
                .OrderBy(x => x.Distance(Game.CursorPos)).FirstOrDefault(x => x.IsValidTarget());

            var closetarg = HeroManager.Enemies
                .Where(x => x.Distance(player.ServerPosition) <= e.Range + 100)
                .OrderBy(x => x.Distance(player.ServerPosition)).FirstOrDefault(x => x.IsValidTarget());

            return _sh ?? cursortarg ?? closetarg;
        }

        private static bool wrektAny()
        {
            return Getcheckboxvalue(wMenu, "req") &&
                 player.GetEnemiesInRange(1250).Any(ez => Getcheckboxvalue(wMenu, "w" + ez.ChampionName));
        }

        private static bool rrektAny()
        {
            return Getcheckboxvalue(r2Menu, "req2") &&
                 player.GetEnemiesInRange(1250).Any(ez => Getcheckboxvalue(r2Menu, "r" + ez.ChampionName));
        }

        #region Riven: OnDoCast
        private static void OnDoCast()
        {
            Obj_AI_Base.OnSpellCast += (sender, args) =>
            {
                if (sender.IsMe && args.SData.IsAutoAttack())
                {
                    if (Getkeybindvalue(keybindsMenu, "shycombo"))
                    {
                        if (riventarget().IsValidTarget() && !riventarget().IsZombie && !riventarget().HasBuff("kindredrnodeathbuff"))
                        {
                            if (shy() && uo)
                            {
                                if (riventarget().HasBuffOfType(BuffType.Stun))
                                    r.Cast(riventarget().ServerPosition);

                                if (!riventarget().HasBuffOfType(BuffType.Stun))
                                    r.CastIfHitchanceEquals(riventarget(), HitChance.Medium);
                            }
                        }
                    }

                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        if (riventarget().IsValidTarget(e.Range + 200))
                        {
                            if (player.Health / player.MaxHealth * 100 <= Getslidervalue(eMenu, "vhealth"))
                            {
                                if (Getcheckboxvalue(eMenu, "usecomboe") && cane)
                                    e.Cast(riventarget().ServerPosition);
                            }
                        }
                    }

                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        if (qtarg != null && riventarget() != null)
                        {
                            if (qtarg.NetworkId == riventarget().NetworkId)
                            {
                                if (Items.CanUseItem(3077))
                                    Items.UseItem(3077);
                                if (Items.CanUseItem(3074))
                                    Items.UseItem(3074);
                                if (Items.CanUseItem(3748))
                                    Items.UseItem(3748);
                            }
                        }
                    }

                    else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) && !player.UnderTurret(true))
                    {
                        if (qtarg.IsValid<Obj_AI_Base>() && !qtarg.Name.StartsWith("Minion"))
                        {
                            if (Items.CanUseItem(3077))
                                Items.UseItem(3077);
                            if (Items.CanUseItem(3074))
                                Items.UseItem(3074);
                            if (Items.CanUseItem(3748))
                                Items.UseItem(3748);
                        }
                    }
                }

                if (sender.IsMe && args.SData.IsAutoAttack())
                {
                    didaa = false;
                    canmv = true;
                    canaa = true;
                    canq = true;
                    cane = true;
                    canw = true;
                    canws = true;
                }
            };
        }

        #endregion

        #region Riven: OnUpdate

        private static bool isteamfightkappa;
        private static void Game_OnUpdate(EventArgs args)
        {
            // harass active
            didhs = Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass);

            // ulti check
            uo = player.GetSpell(SpellSlot.R).Name != "RivenFengShuiEngine";

            // hydra check
            hashd = Items.HasItem(3077) || Items.HasItem(3074) || Items.HasItem(3748);
            canhd = Items.CanUseItem(3077) || Items.CanUseItem(3074) || Items.CanUseItem(3748);

            // my radius
            truerange = player.AttackRange + player.Distance(player.BBox.Minimum) + 1;

            // if no valid target cancel to cursor pos
            if (!qtarg.IsValidTarget(truerange + 100))
                qtarg = player;

            if (!riventarget().IsValidTarget())
                _sh = null;

            if (!canmv && didq)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None) ||
                    Getkeybindvalue(keybindsMenu, "shycombo"))
                {
                    if (Player.IssueOrder(GameObjectOrder.MoveTo, movepos))
                    {
                        didq = false;
                        Utility.DelayAction.Add(40, () =>
                        {
                            canmv = true;
                            canaa = true;
                        });
                    }
                }

                else if (qtarg.IsValidTarget(q.Range) && Getcheckboxvalue(keybindsMenu, "semiq"))
                {
                    if (Player.IssueOrder(GameObjectOrder.MoveTo, movepos))
                    {
                        didq = false;
                        Utility.DelayAction.Add(40, () =>
                        {
                            canmv = true;
                            canaa = true;
                        });
                    }
                }
            }

            // move target position
            if (qtarg != player && qtarg.Distance(player.ServerPosition) < r.Range)
                movepos = player.Position.LSExtend(Game.CursorPos, player.Distance(Game.CursorPos) + 500);

            // move to game cursor pos
            if (qtarg == player)
                movepos = player.ServerPosition + (Game.CursorPos - player.ServerPosition).Normalized() * 125;

            SemiQ();
            AuraUpdate();
            CombatCore();
            Orbwalker.DisableAttacking = false;
            Orbwalker.DisableMovement = false;

            if (riventarget().IsValidTarget())
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    ComboTarget(riventarget());
                    TryIgnote(riventarget());
                }
            }

            if (Getkeybindvalue(keybindsMenu, "shycombo"))
            {
                OrbTo(riventarget(), 350);

                if (riventarget().IsValidTarget())
                {
                    SomeDash(riventarget());

                    if (w.IsReady() && riventarget().Distance(player.ServerPosition) <= w.Range + 50)
                    {
                        checkr();

                        if (!Items.HasItem(3074) &&
                            !Items.HasItem(3077))
                        {
                            w.Cast(riventarget());
                        }

                        if (canhd)
                        {
                            Items.UseItem(3077);
                            Items.UseItem(3074);
                        }

                        else
                        {
                            Utility.DelayAction.Add(20, () => w.Cast());
                        }
                    }

                    else if (q.IsReady() && riventarget().Distance(player.ServerPosition) <= truerange + 100)
                    {
                        checkr();
                        TryIgnote(riventarget());

                        if (canq && !canhd && Utils.GameTimeTickCount - lasthd >= 300)
                        {
                            if (Utils.GameTimeTickCount - lastw >= 300 + Game.Ping)
                            {
                                useinventoryitems(riventarget());
                                q.Cast(riventarget().ServerPosition);
                            }
                        }
                    }
                }
            }

            if (didhs && riventarget().IsValidTarget())
                HarassTarget(riventarget());

            if (player.IsValid && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                Clear();
                Wave();
            }

            if (player.IsValid && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                Flee();

            Windslash();

            isteamfightkappa = player.CountAlliesInRange(1500) > 1 && player.CountEnemiesInRange(1350) > 2 ||
                               player.CountEnemiesInRange(1200) > 2;
        }

        #endregion

        #region Riven: Menu
        private static void OnMenuLoad()
        {
            rivenMenu = MainMenu.AddMenu("Riven", "Riven");

            keybindsMenu = rivenMenu.AddSubMenu("keybinds Options");
            keybindsMenu.Add("shycombo", new KeyBind("Burst Combo", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));
            keybindsMenu.Add("semiq", new CheckBox("Auto Q Harass/Jungle"));


            drawMenu = rivenMenu.AddSubMenu("Drawings Options");
            drawMenu.Add("linewidth", new Slider("Line Width", 1, 1, 6));
            drawMenu.Add("drawengage", new CheckBox("Draw Engage Range"));
            drawMenu.Add("drawr2", new CheckBox("Draw R2 Range"));
            drawMenu.Add("drawburst", new CheckBox("Draw Burst Range"));
            drawMenu.Add("drawf", new CheckBox("Draw Target"));
            drawMenu.Add("drawdmg", new CheckBox("Draw Combo Damage Fill"));

            qMenu = rivenMenu.AddSubMenu("Q Options");
            qMenu.Add("wq3", new CheckBox("Ward + Q3 (Flee)"));
            qMenu.Add("qint", new CheckBox("Interrupt with 3rd Q"));
            qMenu.Add("keepq", new CheckBox("Use Q Before Expiry"));
            qMenu.Add("usegap", new CheckBox("Gapclose with Q"));
            qMenu.Add("gaptimez", new Slider("Gapclose Q Delay (ms)", 115, 0, 200));
            qMenu.Add("q1delay", new Slider("Q1 animation reset delay {0}ms default 293", 291, 0, 500));
            qMenu.Add("q2delay", new Slider("Q2 animation reset delay {0}ms default 293", 291, 0, 500));
            qMenu.Add("q3delay", new Slider("Q3 animation reset delay {0}ms default 393", 393, 0, 500));
            qMenu.Add("alwayscancel", new CheckBox("Cancel animation from manual Qs"));

            wMenu = rivenMenu.AddSubMenu("W Options");
            wMenu.Add("req", new CheckBox("Required Targets"));
            foreach (var hero in HeroManager.Enemies)
            {
                wMenu.Add("w" + hero.ChampionName, new CheckBox("Only W if it will hit: " + hero.ChampionName));

            }
            wMenu.Add("usecombow", new CheckBox("Use W in Combo"));
            wMenu.Add("fq", new CheckBox("-> Q after W"));
            wMenu.Add("wint", new CheckBox("Use on Interrupt"));

            eMenu = rivenMenu.AddSubMenu("E Options");
            eMenu.Add("usecomboe", new CheckBox("Use E in Combo"));
            eMenu.Add("vhealth", new Slider("Use E if HP% <=" , 60, 0, 100));

            r1Menu = rivenMenu.AddSubMenu("R1 Options");
            r1Menu.Add("useignote", new CheckBox("Combo with Ignite"));
            r1Menu.Add("user", new KeyBind("Use R1 in Combo", false, KeyBind.BindTypes.PressToggle, "H".ToCharArray()[0]));
            StringList(r1Menu, "ultwhen", "Use R1 when", new[] { "Normal Kill", "Hard Kill", "Always" }, 1);
            r1Menu.Add("overk", new Slider("Dont R1 if target HP % <=", 25, 1, 99));
            r1Menu.Add("userq", new Slider("Use only if Q Count <=", 2, 1, 3));
            StringList(r1Menu, "multib", "Burst:", new[] { "Damage Check", "Always" }, 1);
            r1Menu.Add("flashb", new CheckBox("Burst: Flash in Burst"));

            r2Menu = rivenMenu.AddSubMenu("R2 Options");
            r2Menu.Add("req2", new CheckBox("Required Targets"));
            foreach (var hero in HeroManager.Enemies)
            {
                r2Menu.Add("r" + hero.ChampionName, new CheckBox("Only R2 if it will hit: " + hero.ChampionName));

            }

            r2Menu.Add("usews", new CheckBox("Use R2 in Combo"));
            r2Menu.Add("overaa", new Slider("Dont R2 if target will die in AA",  2, 1, 6));
            StringList(r2Menu, "wsmode", "Use R2 when", new[] { "Kill Only", "Max Damage" }, 1);
            r2Menu.Add("keepr", new CheckBox("Use R2 Before Expiry"));


            harassMenu = rivenMenu.AddSubMenu("Harass Options");
            harassMenu.Add("useharassw", new CheckBox("Use W in Harass"));
            harassMenu.Add("usegaph", new CheckBox("Use E in Harass"));
            StringList(harassMenu, "qtoo", "Use Escape/Flee:", new[] { "Away from Target", "To Ally Turret", "To Cursor" }, 1);
            harassMenu.Add("useitemh", new CheckBox("Use Tiamat/Hydra"));

            farmMenu = rivenMenu.AddSubMenu("Farming Options");
            farmMenu.Add("usejungleq", new CheckBox("Use Q in Jungle"));
            farmMenu.Add("fq2", new CheckBox("-> Q after W"));
            farmMenu.Add("usejunglew", new CheckBox("Use W in Jungle"));
            farmMenu.Add("usejunglee", new CheckBox("Use E in Jungle"));
            farmMenu.AddSeparator();
            farmMenu.Add("uselaneq", new CheckBox("Use Q in WaveClear"));
            farmMenu.Add("useaoeq", new CheckBox("Try Q AoE WaveClear"));
            farmMenu.Add("uselanew", new CheckBox("Use W in WaveClear"));
            farmMenu.Add("wminion", new Slider("Use W Minions >=", 3, 1, 6));
            farmMenu.Add("uselanee", new CheckBox("Use E in WaveClear"));


        }

        #endregion

        #region Riven : Some Dash
        private static bool canburst()
        {
            if (riventarget() == null || !r.IsReady())
            {
                return false;
            }

            if (IsLethal(riventarget()) && Getslidervalue(r1Menu, "multib") == 0)
            {
                return true;
            }

            return false;
        }

        private static bool shy()
        {
            if (r.IsReady() && riventarget() != null && Getslidervalue(r1Menu, "multib") != 0)
            {
                return true;
            }

            return false;
        }

        private static void doFlash()
        {
            if (riventarget() != null && (canburst() || shy()))
            {
                if (!flash.IsReady() || !Getcheckboxvalue(r1Menu, "flashb"))
                    return;

                if (Getkeybindvalue(keybindsMenu, "shycombo"))
                {
                    if (riventarget().Distance(player.ServerPosition) > e.Range + 50 &&
                        riventarget().Distance(player.ServerPosition) <= e.Range + w.Range + 275)
                    {
                        var second =
                            HeroManager.Enemies.Where(
                                x => x.NetworkId != riventarget().NetworkId &&
                                     x.Distance(riventarget().ServerPosition) <= r.Range)
                                .OrderByDescending(xe => xe.Distance(riventarget().ServerPosition))
                                .FirstOrDefault();

                        if (second != null)
                        {
                            var pos = riventarget().ServerPosition +
                                      (riventarget().ServerPosition - second.ServerPosition).Normalized() * 75;

                            player.Spellbook.CastSpell(flash, pos);
                        }

                        else
                        {
                            player.Spellbook.CastSpell(flash,
                                riventarget().ServerPosition.LSExtend(player.ServerPosition, 115));
                        }
                    }
                }
            }
        }

        private static void SomeDash(AIHeroClient target)
        {
            if (!Getkeybindvalue(keybindsMenu, "shycombo") ||
                !target.IsValid<AIHeroClient>() || uo)
                return;

            if (riventarget() == null || !r.IsReady())
                return;

            if (flash.IsReady() && w.IsReady() && (canburst() || shy()) && Getslidervalue(r1Menu, "multib") != 2)
            {
                if (e.IsReady() && target.Distance(player.ServerPosition) <= e.Range + w.Range + 275)
                {
                    if (target.Distance(player.ServerPosition) > e.Range + truerange + 50)
                    {
                        e.Cast(target.ServerPosition);

                        if (!uo)
                            r.Cast();
                    }
                }

                if (!e.IsReady() && target.Distance(player.ServerPosition) <= w.Range + 275)
                {
                    if (target.Distance(player.ServerPosition) > truerange + 50)
                    {
                        if (!uo)
                            r.Cast();
                    }
                }
            }

            else
            {
                if (e.IsReady() && target.Distance(player.ServerPosition) <= e.Range + w.Range - 25)
                {
                    if (target.Distance(player.ServerPosition) > truerange + 50)
                    {
                        e.Cast(target.ServerPosition);

                        if (!uo)
                            r.Cast();
                    }
                }

                if (!e.IsReady() && target.Distance(player.ServerPosition) <= w.Range - 10)
                {
                    if (!uo)
                        r.Cast();
                }
            }
        }

        #endregion

        #region Riven: Combo

        private static void ComboTarget(AIHeroClient target)
        {
            OrbTo(target);
            TryIgnote(target);

            if (Utils.GameTimeTickCount - lastw < 300 &&
                Utils.GameTimeTickCount - lastq > 500 &&
                target.Distance(player.ServerPosition) <= q.Range + 90 && Getcheckboxvalue(wMenu, "fq"))
            {
                q.Cast(target.ServerPosition);
            }

            if (e.IsReady() &&

               (target.Distance(player.ServerPosition) <= e.Range + w.Range ||
                uo && target.Distance(player.ServerPosition) > truerange + 200) &&
                 target.Distance(player.ServerPosition) > truerange + 100)
            {
                if (Getcheckboxvalue(eMenu, "usecomboe") && cane)
                    e.Cast(target.ServerPosition);

                if (target.Distance(player.ServerPosition) <= e.Range + w.Range)
                {
                    checkr();

                    if (!canburst() && canhd && uo)
                    {
                        if (Items.CanUseItem(3077))
                            Items.UseItem(3077);
                        if (Items.CanUseItem(3074))
                            Items.UseItem(3074);
                    }
                }

                if (!canburst() && canhd)
                {
                    if (Items.CanUseItem(3077))
                        Items.UseItem(3077);
                    if (Items.CanUseItem(3074))
                        Items.UseItem(3074);
                }
            }

            if (w.IsReady() && Getcheckboxvalue(wMenu, "usecombow") && target.Distance(player.ServerPosition) <= w.Range)
            {
                if (target.Distance(player.ServerPosition) <= w.Range)
                {
                    useinventoryitems(target);
                    checkr();

                    if (Getcheckboxvalue(wMenu, "usecombow") && canw)
                    {
                        if (!isteamfightkappa ||
                             isteamfightkappa && !wrektAny() ||
                             Getcheckboxvalue(wMenu, "w" + target.ChampionName))
                        {
                            w.Cast(target);
                        }
                    }
                }
            }

            var catchRange = e.IsReady() ? e.Range + truerange + 200 : truerange + 200;
            if (q.IsReady() && target.Distance(player.ServerPosition) <= q.Range + 100)
            {
                useinventoryitems(target);
                checkr();

                if (IsLethal(target))
                {
                    if (canhd) return;
                }

                if (Getslidervalue(r2Menu, "wsmode") == 1 && IsLethal(target))
                {
                    if (cc == 2 && e.IsReady() && cane)
                    {
                        e.Cast(target.ServerPosition);
                    }
                }

                if (canq)
                {
                    q.Cast(target.ServerPosition);
                }
            }

            else if (q.IsReady() && target.Distance(player.ServerPosition) > catchRange)
            {
                if (Getcheckboxvalue(qMenu, "usegap"))
                {
                    if (Utils.GameTimeTickCount - lastq >= Getslidervalue(qMenu, "gaptimez") * 10)
                    {
                        if (q.IsReady() && Utils.GameTimeTickCount - laste >= 600)
                        {
                            q.Cast(target.ServerPosition);
                        }
                    }
                }
            }

            else if (target.Health <= q.GetDamage(target) * 2 + player.GetAutoAttackDamage(target) * 1)
            {
                if (target.Distance(player.ServerPosition) > truerange + q.Range + 10)
                {
                    if (target.Distance(player.ServerPosition) <= q.Range * 2)
                    {
                        if (Utils.GameTimeTickCount - lastq >= 400)
                        {
                            q.Cast(target.ServerPosition);
                        }
                    }
                }
            }
        }

        #endregion

        #region Riven: Harass

        private static void HarassTarget(Obj_AI_Base target)
        {
            Vector3 qpos;
            switch (Getslidervalue(harassMenu, "qtoo"))
            {
                case 0:
                    qpos = player.ServerPosition +
                        (player.ServerPosition - target.ServerPosition).Normalized() * 500;
                    break;
                case 1:
                    var tt = ObjectManager.Get<Obj_AI_Turret>()
                        .Where(t => (t.IsAlly)).OrderBy(t => t.Distance(player.Position)).First();
                    if (tt != null)
                        qpos = tt.Position;
                    else if (target != null)
                        qpos = player.ServerPosition +
                               (player.ServerPosition - target.ServerPosition).Normalized() * 500;
                    else
                        qpos = Game.CursorPos;
                    break;
                default:
                    qpos = Game.CursorPos;
                    break;
            }

            if (q.IsReady())
                OrbTo(target);

            if (cc == 2 && canq && q.IsReady())
            {
                if (!e.IsReady())
                {
                    Orbwalker.DisableAttacking = false;
                    Orbwalker.DisableMovement = false;

                    canaa = false;
                    canmv = false;

                    if (Player.IssueOrder(GameObjectOrder.MoveTo, qpos))
                    {
                        Utility.DelayAction.Add(150 - Game.Ping, () =>
                        {
                            q.Cast(qpos);

                            Orbwalker.DisableAttacking = false;
                            Orbwalker.DisableMovement = false;

                            canaa = true;
                            canmv = true;
                        });
                    }
                }
            }

            if (e.IsReady() && (cc == 3 || !q.IsReady() && cc == 0))
            {
                if (player.Distance(target.ServerPosition) <= 300)
                {
                    if (Getcheckboxvalue(harassMenu, "usegaph") && cane)
                        e.Cast(qpos);
                }
            }

            if (!player.ServerPosition.LSExtend(target.ServerPosition, q.Range * 3).UnderTurret(true))
            {
                if (q.IsReady() && canq && (cc < 2 || e.IsReady()))
                {
                    if (target.Distance(player.ServerPosition) <= truerange + q.Range)
                    {
                        q.Cast(target.ServerPosition);
                    }
                }
            }

            if (e.IsReady() && cane && q.IsReady() && cc < 1 &&
                target.Distance(player.ServerPosition) > truerange + 100 &&
                target.Distance(player.ServerPosition) <= e.Range + truerange + 50)
            {
                if (!player.ServerPosition.LSExtend(target.ServerPosition, e.Range).UnderTurret(true))
                {
                    if (Getcheckboxvalue(harassMenu, "usegaph") && cane)
                    {
                        e.Cast(target.ServerPosition);
                    }
                }
            }

            else if (w.IsReady() && canw && target.Distance(player.ServerPosition) <= w.Range + 10)
            {
                if (!target.ServerPosition.UnderTurret(true))
                {
                    if (Getcheckboxvalue(harassMenu, "useharassw") && canw)
                    {
                        w.Cast(target);
                    }
                }
            }
        }

        #endregion

        #region Riven: Windslash

        private static void Windslash()
        {
            if (uo && Getcheckboxvalue(r2Menu, "usews")  && r.IsReady())
            {
                foreach (var t in ObjectManager.Get<AIHeroClient>().Where(h => h.IsValidTarget(r.Range)))
                {
                    if (Getkeybindvalue(keybindsMenu, "shycombo") && canburst())
                    {
                        if (t.Distance(player.ServerPosition) <= player.AttackRange + 100)
                        {
                            if (canhd) return;
                        }
                    }

                    if (r.GetDamage(t) + w.GetDamage(t) >= t.Health && t.Distance(player.ServerPosition) <= w.Range)
                    {
                        if (w.IsReady())
                        {
                            w.Cast(t);
                        }
                    }

                    if (player.GetAutoAttackDamage(t, true) * Getslidervalue(r2Menu, "overaa")  >= t.Health &&
                       (Orbwalking.InAutoAttackRange(t) && player.CountEnemiesInRange(r.Range) > 1) &&
                        player.HealthPercent > 65)
                        return;

                    if (r.GetDamage(t) >= t.Health)
                    {
                        var p = r.GetPrediction(t, true, -1f, new[] { CollisionableObjects.YasuoWall });
                        if (p.Hitchance == HitChance.High && canws && !t.HasBuff("kindredrnodeathbuff"))
                        {
                            r.Cast(p.CastPosition);
                        }
                    }
                }

                if (Getslidervalue(r2Menu, "wsmode") == 1)
                {
                    if (riventarget().IsValidTarget(r.Range) && !riventarget().IsZombie)
                    {
                        if (Getkeybindvalue(keybindsMenu, "shycombo") && canburst())
                        {
                            if (riventarget().Distance(player.ServerPosition) <= player.AttackRange + 100)
                            {
                                if (canhd) return;
                            }
                        }

                        if (r.GetDamage(riventarget()) / riventarget().MaxHealth * 100 >= 50)
                        {
                            var p = r.GetPrediction(riventarget(), true, -1f, new[] { CollisionableObjects.YasuoWall });
                            if (p.Hitchance >= HitChance.Medium && canws && !riventarget().HasBuff("kindredrnodeathbuff"))
                            {
                                if (!isteamfightkappa || Getcheckboxvalue(r2Menu, "r" + riventarget().ChampionName) ||
                                     isteamfightkappa && !rrektAny())
                                {
                                    r.Cast(p.CastPosition);
                                }
                            }
                        }

                        if (q.IsReady() && cc <= 2)
                        {
                            var damage = r.GetDamage(riventarget())
                                + player.GetAutoAttackDamage(riventarget()) * 2
                                + Qdmg(riventarget()) * 2;

                            if (riventarget().Health <= xtra((float)damage))
                            {
                                if (riventarget().Distance(player.ServerPosition) <= truerange + q.Range)
                                {
                                    var p = r.GetPrediction(riventarget(), true, -1f, new[] { CollisionableObjects.YasuoWall });
                                    if (p.Hitchance >= HitChance.High && canws && !riventarget().HasBuff("kindredrnodeathbuff"))
                                    {
                                        if (!isteamfightkappa || Getcheckboxvalue(r2Menu, "r" + riventarget().ChampionName) ||
                                             isteamfightkappa && !rrektAny())
                                        {
                                            if (w.IsReady())
                                            {
                                                w.Cast(riventarget());
                                            }

                                            r.Cast(p.CastPosition);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Riven: Lane/Jungle

        private static void Clear()
        {
            var minions = MinionManager.GetMinions(player.Position, 600f,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            foreach (var unit in minions.Where(m => !m.Name.Contains("Mini")))
            {
                OrbTo(unit);

                if (Utils.GameTimeTickCount - lastw < 300 &&
                    Utils.GameTimeTickCount - lastq > 500 && // prevent double casting
                    unit.Distance(player.ServerPosition) <= q.Range + 90 && Getcheckboxvalue(farmMenu, "fq2"))
                {
                    q.Cast(unit.ServerPosition);
                }

                if (Utils.GameTimeTickCount - laste < 600)
                {
                    if (unit.Distance(player.ServerPosition) <= w.Range + 45)
                    {
                        if (Items.CanUseItem(3077))
                            Items.UseItem(3077);
                        if (Items.CanUseItem(3074))
                            Items.UseItem(3074);
                    }
                }

                if (e.IsReady() && cane && Getcheckboxvalue(farmMenu, "usejunglee"))
                {
                    if (player.Health / player.MaxHealth * 100 <= 70 ||
                        unit.Distance(player.ServerPosition) > truerange + 30)
                    {
                        e.Cast(unit.ServerPosition);
                    }
                }

                if (w.IsReady() && canw && Getcheckboxvalue(farmMenu, "usejunglew"))
                {
                    if (unit.Distance(player.ServerPosition) <= w.Range + 25)
                    {
                        w.Cast();
                    }
                }

                if (q.IsReady() && canq && Getcheckboxvalue(farmMenu, "usejungleq"))
                {
                    if (unit.Distance(player.ServerPosition) <= q.Range + 90)
                    {
                        if (canhd) return;

                        if (qtarg != null && qtarg.NetworkId == unit.NetworkId)
                            q.Cast(unit.ServerPosition);
                    }
                }
            }
        }

        private static void Wave()
        {
            var minions = MinionManager.GetMinions(player.Position, 600f);

            foreach (var unit in minions.Where(x => x.IsMinion))
            {
                OrbTo(Getcheckboxvalue(farmMenu, "useaoeq") && GetCenterMinion().IsValidTarget()
                    ? GetCenterMinion()
                    : unit);

                if (q.IsReady() && unit.Distance(player.ServerPosition) <= truerange + 100)
                {
                    if (canq && Getcheckboxvalue(farmMenu, "uselaneq") && minions.Count >= 2 &&
                        !player.ServerPosition.LSExtend(unit.ServerPosition, q.Range).UnderTurret(true))
                    {
                        if (GetCenterMinion().IsValidTarget() && Getcheckboxvalue(farmMenu, "useaoeq"))
                            q.Cast(GetCenterMinion());
                        else
                            q.Cast(unit.ServerPosition);
                    }
                }

                if (w.IsReady())
                {
                    if (minions.Count(m => m.Distance(player.ServerPosition) <= w.Range + 10) >= Getslidervalue(farmMenu, "wminion"))
                    {
                        if (canw && Getcheckboxvalue(farmMenu, "uselanew"))
                        {
                            if (Items.CanUseItem(3077))
                                Items.UseItem(3077);
                            if (Items.CanUseItem(3074))
                                Items.UseItem(3074);

                            w.Cast();
                        }
                    }
                }

                if (e.IsReady() && !player.ServerPosition.LSExtend(unit.ServerPosition, e.Range).UnderTurret(true))
                {
                    if (unit.Distance(player.ServerPosition) > truerange + 30)
                    {
                        if (cane && Getcheckboxvalue(farmMenu, "uselanee"))
                        {
                            if (GetCenterMinion().IsValidTarget() && Getcheckboxvalue(farmMenu, "useaoeq"))
                                e.Cast(GetCenterMinion());
                            else
                                e.Cast(unit.ServerPosition);
                        }
                    }

                    else if (player.Health / player.MaxHealth * 100 <= 70)
                    {
                        if (cane && Getcheckboxvalue(farmMenu, "uselanee"))
                        {
                            if (GetCenterMinion().IsValidTarget() && Getcheckboxvalue(farmMenu, "useaoeq"))
                                q.Cast(GetCenterMinion());
                            else
                                q.Cast(unit.ServerPosition);
                        }
                    }
                }
            }
        }

        #endregion

        #region Riven: Flee

        private static void Flee()
        {
            if (canmv)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if (cc > 2 && Items.GetWardSlot() != null && Getcheckboxvalue(qMenu, "wq3"))
            {
                var attacker = HeroManager.Enemies.FirstOrDefault(x => x.Distance(player.ServerPosition) <= q.Range + 50);
                if (attacker.IsValidTarget(q.Range))
                {
                    if (Utils.GameTimeTickCount - lastwd >= 1000 && didq)
                    {
                        Utility.DelayAction.Add(100,
                            () => Items.UseItem((int)Items.GetWardSlot().Id, attacker.ServerPosition));
                    }
                }
            }

            if (player.CountEnemiesInRange(w.Range) > 0)
            {
                if (w.IsReady())
                    w.Cast(riventarget());
            }

            if (ssfl)
            {
                if (Utils.GameTimeTickCount - lastq >= 600)
                {
                    q.Cast(Game.CursorPos);
                }

                if (cane && e.IsReady())
                {
                    if (cc >= 2 || !q.IsReady() && !player.HasBuff("RivenTriCleave"))
                    {
                        if (!player.ServerPosition.Extend(Game.CursorPos, e.Range + 10).IsWall())
                            e.Cast(Game.CursorPos);
                    }
                }
            }

            else
            {
                if (q.IsReady())
                {
                    q.Cast(Game.CursorPos);
                }

                if (e.IsReady() && Utils.GameTimeTickCount - lastq >= 250)
                {
                    if (!player.ServerPosition.Extend(Game.CursorPos, e.Range).IsWall())
                        e.Cast(Game.CursorPos);
                }
            }
        }

        #endregion

        #region Riven: Semi Q 

        private static void SemiQ()
        {
            if (canq && Utils.GameTimeTickCount - lastaa >= 150)
            {
                if (Getcheckboxvalue(keybindsMenu, "semiq"))
                {
                    if (q.IsReady() && Utils.GameTimeTickCount - lastaa < 1200 && qtarg != null)
                    {
                        if (qtarg.IsValidTarget(q.Range + 100) &&
                            !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) &&
                            !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) &&
                            !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) &&
                            !Getkeybindvalue(keybindsMenu, "shycombo"))
                        {
                            if (qtarg.IsValid<AIHeroClient>() && !qtarg.UnderTurret(true))
                                q.Cast(qtarg.ServerPosition);
                        }

                        if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) &&
                            !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) &&
                            !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) &&
                            !Getkeybindvalue(keybindsMenu, "shycombo"))
                        {
                            if (qtarg.IsValidTarget(q.Range + 100) && !qtarg.Name.Contains("Mini"))
                            {
                                if (!qtarg.Name.StartsWith("Minion") && minionlist.Any(name => qtarg.Name.StartsWith(name)))
                                {
                                    q.Cast(qtarg.ServerPosition);
                                }
                            }

                            if (qtarg.IsValidTarget(q.Range + 100))
                            {
                                if (qtarg.IsValid<AIHeroClient>() || qtarg.IsValid<Obj_AI_Turret>())
                                {
                                    if (uo)
                                        q.Cast(qtarg.ServerPosition);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Riven: Check R
        private static void checkr()
        {
            if (!r.IsReady() || uo || !Getkeybindvalue(r1Menu, "user"))
            {
                return;
            }

            if (Getkeybindvalue(keybindsMenu, "shycombo"))
            {
                r.Cast();
                return;
            }

            var targets = HeroManager.Enemies.Where(ene => ene.IsValidTarget(r.Range));
            var heroes = targets as IList<AIHeroClient> ?? targets.ToList();

            foreach (var target in heroes)
            {
                if (cc > Getslidervalue(r1Menu, "userq"))
                {
                    return;
                }

                if (target.Health / target.MaxHealth * 100 <= Getslidervalue(r1Menu, "overk") && IsLethal(target))
                {
                    if (heroes.Count() < 2)
                    {
                        continue;
                    }
                }

                if (Getslidervalue(r1Menu, "ultwhen") == 2) 
                    r.Cast();

                if (q.IsReady() || Utils.GameTimeTickCount - lastq < 1000 && cc < 3)
                {
                    if (heroes.Count() < 2)
                    {
                        if (target.Health / target.MaxHealth * 100 <= Getslidervalue(r1Menu, "overk") && IsLethal(target))
                            return;
                    }

                    if (heroes.Count(ene => ene.Distance(player.ServerPosition) <= 750) > 1)
                        r.Cast();

                    if (Getslidervalue(r1Menu, "ultwhen") == 0)
                    {
                        if ((ComboDamage(target) / 1.3) >= target.Health && target.Health >= (ComboDamage(target) / 1.8))
                        {
                            r.Cast();
                        }
                    }

                    if (Getslidervalue(r1Menu, "ultwhen") == 1)
                    {
                        if (ComboDamage(target) >= target.Health && target.Health >= ComboDamage(target) / 1.8)
                        {
                            r.Cast();
                        }
                    }
                }
            }
        }

        #endregion

        #region Riven: On Cast
        static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            {
                if (!sender.IsMe)
                {
                    return;
                }

                if (args.SData.IsAutoAttack())
                {
                    qtarg = (Obj_AI_Base)args.Target;
                    lastaa = Utils.GameTimeTickCount;
                }

                if (!didq && args.SData.IsAutoAttack())
                {
                    var targ = (AttackableUnit)args.Target;
                    if (targ != null && player.Distance(targ.Position) <= q.Range + 120)
                    {
                        didaa = true;
                        canaa = false;
                        canq = false;
                        canw = false;
                        cane = false;
                        canws = false;
                        // canmv = false;
                    }
                }

                if (args.SData.Name.ToLower().Contains("ward"))
                    lastwd = Utils.GameTimeTickCount;

                switch (args.SData.Name)
                {
                    case "ItemTiamatCleave":
                        lasthd = Utils.GameTimeTickCount;
                        didhd = true;
                        canws = true;
                        canhd = false;

                        if (Getslidervalue(r2Menu, "wsmode") == 1 || Getkeybindvalue(keybindsMenu, "shycombo"))
                        {
                            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                            {
                                if (canburst() && uo)
                                {
                                    if (riventarget().IsValidTarget() && !riventarget().IsZombie && !riventarget().HasBuff("kindredrnodeathbuff"))
                                    {
                                        if (!isteamfightkappa || Getcheckboxvalue(r2Menu, "r" + riventarget().ChampionName) ||
                                             isteamfightkappa && !rrektAny())
                                        {
                                            Utility.DelayAction.Add(100 - Game.Ping / 2,
                                                () =>
                                                {
                                                    if (riventarget().HasBuffOfType(BuffType.Stun))
                                                        r.Cast(riventarget().ServerPosition);

                                                    if (!riventarget().HasBuffOfType(BuffType.Stun))
                                                        r.Cast(r.CastIfHitchanceEquals(riventarget(), HitChance.Medium));
                                                });
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case "RivenTriCleave":
                        cc += 1;
                        didq = true;
                        didaa = false;
                        lastq = Utils.GameTimeTickCount;
                        canq = false;
                        canmv = false;

                        var dd = new[] { 280 - Game.Ping, 290 - Game.Ping, 380 - Game.Ping };
                        Utility.DelayAction.Add(dd[cc - 1], () =>
                        {
                            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None) ||
                                Getkeybindvalue(keybindsMenu, "shycombo"))
                                Chat.Say("/d");

                            else if (qtarg.IsValidTarget(450) && Getcheckboxvalue(keybindsMenu, "semiq"))
                                Chat.Say("/d");
                        });


                        if (!uo) ssfl = false;
                        break;
                    case "RivenMartyr":
                        canq = false;
                        canmv = false;
                        didw = true;
                        lastw = Utils.GameTimeTickCount;
                        canw = false;

                        break;
                    case "RivenFeint":
                        canmv = false;
                        dide = true;
                        didaa = false;
                        laste = Utils.GameTimeTickCount;
                        cane = false;

                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                        {
                            if (uo && r.IsReady() && cc == 2 && q.IsReady())
                            {
                                var btarg = TargetSelector.GetTarget(r.Range, DamageType.Physical);
                                if (btarg.IsValidTarget())
                                    r.CastIfHitchanceEquals(btarg, HitChance.Medium);
                                else
                                    r.Cast(Game.CursorPos);
                            }
                        }

                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                        {
                            if (cc == 2 && !uo)
                            {
                                checkr();
                                Utility.DelayAction.Add(140 - Game.Ping, () => q.Cast(Game.CursorPos));
                            }

                            if (Getslidervalue(r2Menu, "wsmode") == 1 && cc == 2 && uo)
                            {
                                if (riventarget().IsValidTarget(r.Range + 100) && IsLethal(riventarget()))
                                {
                                    Utility.DelayAction.Add(100 - Game.Ping,
                                    () => r.Cast(r.CastIfHitchanceEquals(riventarget(), HitChance.Medium)));
                                }
                            }
                        }

                        break;
                    case "RivenFengShuiEngine":
                        ssfl = true;
                        doFlash();
                        break;
                    case "RivenIzunaBlade":
                        ssfl = false;
                        didws = true;
                        canws = false;

                        if (w.IsReady() && riventarget().IsValidTarget(w.Range + 55))
                            w.Cast(riventarget());

                        else if (q.IsReady() && riventarget().IsValidTarget())
                            q.Cast(riventarget().ServerPosition);

                        break;
                }
            };
        }

        #endregion

        #region Riven: Misc Events
        private static void Interrupter()
        {
            Interrupter2.OnInterruptableTarget += (sender, args) =>
            {
                if (Getcheckboxvalue(wMenu, "wint") && w.IsReady())
                {
                    if (!sender.Position.UnderTurret(true))
                    {
                        if (sender.IsValidTarget(w.Range))
                            w.Cast();

                        if (sender.IsValidTarget(w.Range + e.Range) && e.IsReady())
                        {
                            e.Cast(sender.ServerPosition);
                        }
                    }
                }

                if (Getcheckboxvalue(qMenu, "qint") && q.IsReady() && cc >= 2)
                {
                    if (!sender.Position.UnderTurret(true))
                    {
                        if (sender.IsValidTarget(q.Range))
                            q.Cast(sender.ServerPosition);

                        if (sender.IsValidTarget(q.Range + e.Range) && e.IsReady())
                        {
                            e.Cast(sender.ServerPosition);
                        }
                    }
                }
            };
        }


  
        private static int lastQDelay;
        private static int QNum = 0;
        static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (player.IsDead) return;
            if (!sender.IsMe) return;
            int delay = 0;
            switch (args.Animation)
            {
                case "Spell1a":
                    delay = Getslidervalue(qMenu, "q1delay");
                    lastq = Core.GameTickCount;
                    QNum = 1;
                    break;
                case "Spell1b":
                    delay = Getslidervalue(qMenu, "q2delay");
                    lastq = Core.GameTickCount;
                    QNum = 2;
                    break;
                case "Spell1c":
                    delay = Getslidervalue(qMenu, "q3delay");
                    lastq = Core.GameTickCount;
                    QNum = 3;
                    break;
                case "Dance":
                    if (lastq > Core.GameTickCount - 500)
                    {

                        //Orbwalker.ResetAutoAttack();
                        //Utils.Debug("reset");
                    }

                    break;
            }

            if (delay != 0 && (Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None || Getcheckboxvalue(qMenu, "alwayscancel")))
            {
                lastQDelay = delay;
                Orbwalker.ResetAutoAttack();
                Core.DelayAction(DanceIfNotAborted, delay - Game.Ping);
                //Utils.Debug("reset"); 
            }


        }

        private static void ForceQ()
        {
          //  Utils.Debug("delay " + Core.GameTickCount);
            if (q.IsReady())
                Player.CastSpell(SpellSlot.Q, Game.CursorPos);
        }
        private static void DanceIfNotAborted()
        {
            Player.DoEmote(Emote.Dance);
            //if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None)
            //    Player.IssueOrder(GameObjectOrder.MoveTo, Player.Instance.Position + (new Vector3(1.0f, 0, -1.0f)));
            //Orbwalker.ResetAutoAttack();
            /*if (ComboTarget != null && ComboTarget.IsValidTarget(_Player.AttackRange))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, ComboTarget);
                return;
            }*/
            /*if (JCTarget != null && JCTarget.IsValidTarget(_Player.AttackRange))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, ComboTarget);
                return;
            }*/
        }

        #endregion

        #region Riven: Aura

        private static void AuraUpdate()
        {
            if (!player.IsDead)
            {
                foreach (var buff in player.Buffs)
                {
                    //if (buff.Name == "RivenTriCleave")
                    //    cc = buff.Count;

                    if (buff.Name == "rivenpassiveaaboost")
                        pc = buff.Count;
                }

                if (player.HasBuff("RivenTriCleave"))
                {
                    if (player.GetBuff("RivenTriCleave").EndTime - Game.Time <= 0.25f)
                    {
                        if (!player.IsRecalling() && !player.Spellbook.IsChanneling)
                        {
                            var qext = player.ServerPosition.To2D() +
                                       player.Direction.To2D().Perpendicular() * q.Range + 100;

                            if (Getcheckboxvalue(qMenu, "keepq") && !qext.To3D().UnderTurret(true))
                                q.Cast(Game.CursorPos);
                        }
                    }
                }

                if (r.IsReady() && uo && Getcheckboxvalue(r2Menu, "keepr"))
                {
                    if (player.GetBuff("RivenFengShuiEngine").EndTime - Game.Time <= 0.25f)
                    {
                        if (!riventarget().IsValidTarget(r.Range) || riventarget().HasBuff("kindredrnodeathbuff"))
                        {
                            if (e.IsReady() && uo)
                                e.Cast(Game.CursorPos);

                            r.Cast(Game.CursorPos);
                        }

                        if (riventarget().IsValidTarget(r.Range) && !riventarget().HasBuff("kindredrnodeathbuff"))
                            r.CastIfHitchanceEquals(riventarget(), HitChance.High);
                    }
                }

                if (!player.HasBuff("rivenpassiveaaboost"))
                    Utility.DelayAction.Add(1000, () => pc = 1);

                if (cc > 2)
                    Utility.DelayAction.Add(1000, () => cc = 0);
            }
        }

        #endregion

        #region Riven : Combat/Orbwalk

        private static void OrbTo(Obj_AI_Base target, float rangeoverride = 0f)
        {
            if (canmv)
            {
                if (Getkeybindvalue(keybindsMenu, "shycombo"))
                {
                    if (target.IsValidTarget(truerange + 100))
                        Orbwalker.OrbwalkTo(target.ServerPosition);

                    else
                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }
            }

            if (canmv && canaa)
            {
                if (q.IsReady() || Utils.GameTimeTickCount - lastq <= 400 - Game.Ping && cc < 3)
                {
                    if (target.IsValidTarget(truerange + 200 + rangeoverride))
                    {
                        LastAATick = 0;
                    }
                }
            }
        }

        private static void CombatCore()
        {
            if (didaa && Utils.GameTimeTickCount - lastaa >=
                100 - Game.Ping / 2 + 55 + player.AttackCastDelay * 1000)
                didaa = false;

            if (didhd && canhd && Utils.GameTimeTickCount - lasthd >= 250)
                didhd = false;

            if (didq && Utils.GameTimeTickCount - lastq >= 500)
                didq = false;

            if (didw && Utils.GameTimeTickCount - lastw >= 266)
            {
                didw = false;
                canmv = true;
                canaa = true;
            }

            if (dide && Utils.GameTimeTickCount - laste >= 350)
            {
                dide = false;
                canmv = true;
                canaa = true;
            }

            if (didws && Utils.GameTimeTickCount - laste >= 366)
            {
                didws = false;
                canmv = true;
                canaa = true;
            }

            if (!canw && w.IsReady() && !(didaa || didq || dide))
                canw = true;

            if (!cane && e.IsReady() && !(didaa || didq || didw))
                cane = true;

            if (!canws && r.IsReady() && (!(didaa || didw) && uo))
                canws = true;

            if (!canaa && !(didq || didw || dide || didws || didhd || didhs) &&
                Utils.GameTimeTickCount - lastaa >= 1000)
                canaa = true;

            if (!canmv && !(didq || didw || dide || didws || didhd || didhs) &&
                Utils.GameTimeTickCount - lastaa >= 1100)
                canmv = true;
        }

        #endregion

        #region Riven: Math/Damage

        private static float ComboDamage(Obj_AI_Base target)
        {
            if (target == null)
                return 0f;

            var ignote = player.GetSpellSlot("summonerdot");
            var ad = (float)player.GetAutoAttackDamage(target);
            var runicpassive = new[] { 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5 };

            var ra = ad +
                        (float)
                            ((+player.FlatPhysicalDamageMod + player.BaseAttackDamage) *
                            runicpassive[player.Level / 3]);

            var rw = Wdmg(target);
            var rq = Qdmg(target);
            var rr = r.IsReady() ? r.GetDamage(target) : 0;

            var ii = (ignote != SpellSlot.Unknown && player.GetSpell(ignote).State == SpellState.Ready && r.IsReady()
                ? player.GetSummonerSpellDamage(target, LeagueSharp.Common.Damage.SummonerSpell.Ignite)
                : 0);

            var tmt = Items.HasItem(3077) && Items.CanUseItem(3077)
                ? player.GetItemDamage(target, LeagueSharp.Common.Damage.DamageItems.Tiamat)
                : 0;

            var hyd = Items.HasItem(3074) && Items.CanUseItem(3074)
                ? player.GetItemDamage(target, LeagueSharp.Common.Damage.DamageItems.Hydra)
                : 0;

            var tdh = Items.HasItem(3748) && Items.CanUseItem(3748)
                ? player.GetItemDamage(target, LeagueSharp.Common.Damage.DamageItems.Hydra)
                : 0;

            var bwc = Items.HasItem(3144) && Items.CanUseItem(3144)
                ? player.GetItemDamage(target, LeagueSharp.Common.Damage.DamageItems.Bilgewater)
                : 0;

            var brk = Items.HasItem(3153) && Items.CanUseItem(3153)
                ? player.GetItemDamage(target, LeagueSharp.Common.Damage.DamageItems.Botrk)
                : 0;

            var items = tmt + hyd + tdh + bwc + brk;

            var damage = (rq * 3 + ra * 3 + rw + rr + ii + items);

            return xtra((float)damage);
        }


        private static double Wdmg(Obj_AI_Base target)
        {
            double dmg = 0;
            if (w.IsReady() && target != null)
            {
                dmg += player.CalcDamage(target, DamageType.Physical,
                    new[] { 50, 80, 110, 150, 170 }[w.Level - 1] + 1 * player.FlatPhysicalDamageMod + player.BaseAttackDamage);
            }

            return dmg;
        }

        private static double Qdmg(Obj_AI_Base target)
        {
            double dmg = 0;
            if (q.IsReady() && target != null)
            {
                dmg += player.CalcDamage(target, DamageType.Physical,
                    -10 + (q.Level * 20) + (0.35 + (q.Level * 0.05)) * (player.FlatPhysicalDamageMod + player.BaseAttackDamage));
            }

            return dmg;
        }

        #endregion

        #region Riven: Drawings

        private static void Drawings()
        {
            Drawing.OnDraw += args =>
            {
                if (!player.IsDead)
                {
                    if (riventarget().IsValidTarget())
                    {
                        var tpos = Drawing.WorldToScreen(riventarget().Position);

                        if (Getcheckboxvalue(drawMenu, "drawf"))
                        {
                            Render.Circle.DrawCircle(riventarget().Position, 120, System.Drawing.Color.GreenYellow);
      
                        }

                        if (riventarget().HasBuff("Stun"))
                        {
                            var b = riventarget().GetBuff("Stun");
                            if (b.Caster.IsMe && b.EndTime - Game.Time > 0)
                            {
                                Drawing.DrawText(tpos[0], tpos[1], System.Drawing.Color.Lime, "STUNNED " + (b.EndTime - Game.Time).ToString("F"));
                            }
                        }
                    }

                    if (_sh.IsValidTarget())
                    {
                        if (Getcheckboxvalue(drawMenu, "drawf"))
                        {
                            Render.Circle.DrawCircle(_sh.Position, 90, System.Drawing.Color.Green, 6);

                        }
                    }

                    if (Getcheckboxvalue(drawMenu, "drawengage"))
                    {
                        Render.Circle.DrawCircle(player.Position,
                                player.AttackRange + e.Range + 35, System.Drawing.Color.Red,
                               Getslidervalue(drawMenu, "linewidth"));
                    }

                    if (Getcheckboxvalue(drawMenu, "drawr2"))
                    {

                        Render.Circle.DrawCircle(player.Position, r.Range, System.Drawing.Color.Green, Getslidervalue(drawMenu, "linewidth"));
                    }

                    if (Getcheckboxvalue(drawMenu, "drawburst") && (canburst() || shy()) && riventarget().IsValidTarget())
                    {
                        var xrange = Getcheckboxvalue(r1Menu, "flashb") && flash.IsReady() ? 255 : 0;
                        Render.Circle.DrawCircle(riventarget().Position, e.Range + w.Range - 25 + xrange,
                            System.Drawing.Color.Green, Getslidervalue(drawMenu, "linewidth"));
                    }
                }
            };

            Drawing.OnEndScene += args =>
            {
                if (!Getcheckboxvalue(drawMenu, "drawdmg"))
                    return;

                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(ene => ene.IsValidTarget() && !ene.IsZombie))
                {
                    var color = r.IsReady() && IsLethal(enemy)
                        ? new ColorBGRA(0, 255, 0, 90)
                        : new ColorBGRA(255, 255, 0, 90);

                    hpi.unit = enemy;
                    hpi.drawDmg(ComboDamage(enemy), color);
                }

            };
        }

        #endregion

    }
}

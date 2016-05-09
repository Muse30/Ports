#region
/*
* Credits to:
 * Trees (Damage indicator)
 * Kurisu (ult on dangerous)
 * xQx assasin target selector
 */
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using EloBuddy;
using EloBuddy.SDK;
using LeagueSharp.Common;
using System.Threading.Tasks;
using System.Text;
using SharpDX;
using Color = System.Drawing.Color;
using Spell = LeagueSharp.Common.Spell;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;


#endregion

namespace Zed
{
    class Program
    {
        private const string ChampionName = "Zed";
        private static List<Spell> SpellList = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        public static Menu _config;
        public static Menu zedMenu, comboMenu, harassMenu, miscMenu, itemMenu, farmMenu, jungleMenu, lasthitMenu, drawMenu;
        public static Menu TargetSelectorMenu;
        private static AIHeroClient _player;
        private static SpellSlot _igniteSlot;
        private static Items.Item _tiamat, _hydra, _blade, _bilge, _rand, _lotis, _youmuu;
        private static Vector3 linepos;
        private static Vector3 castpos;
        private static int clockon;
        private static int countults;
        private static int countdanger;
        private static int ticktock;
        private static Vector3 rpos;
        private static int shadowdelay = 0;
        private static int delayw = 500;


        private static void Game_OnGameLoad(EventArgs args)
        {
                _player = ObjectManager.Player;
                if (ObjectManager.Player.BaseSkinName != ChampionName) return;
                _q = new Spell(SpellSlot.Q, 900f);
                _w = new Spell(SpellSlot.W, 700f);
                _e = new Spell(SpellSlot.E, 270f);
                _r = new Spell(SpellSlot.R, 650f);

                _q.SetSkillshot(0.25f, 50f, 1700f, false, SkillshotType.SkillshotLine);

                _bilge = new Items.Item(3144, 475f);
                _blade = new Items.Item(3153, 425f);
                _hydra = new Items.Item(3074, 250f);
                _tiamat = new Items.Item(3077, 250f);
                _rand = new Items.Item(3143, 490f);
                _lotis = new Items.Item(3190, 590f);
                _youmuu = new Items.Item(3142, 10);
                _igniteSlot = _player.GetSpellSlot("SummonerDot");

                var enemy = from hero in ObjectManager.Get<AIHeroClient>()
                            where hero.IsEnemy == true
                            select hero;
                // Just menu things test
                zedMenu = MainMenu.AddMenu("Zed", "Zed");
                comboMenu = zedMenu.AddSubMenu("Combo Options");
                comboMenu.Add("UseWC", new CheckBox("Use W (also gap close)"));
                comboMenu.Add("UseIgnitecombo", new CheckBox("Use Ignite(rush for it)"));
                comboMenu.Add("UseUlt", new CheckBox("Use Ultimate"));
                comboMenu.Add("TheLine", new KeyBind("The Line Combo", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));

                harassMenu = zedMenu.AddSubMenu("Harass Options");
                harassMenu.Add("longhar", new KeyBind("Long Poke (toggle)", false, KeyBind.BindTypes.PressToggle, "L".ToCharArray()[0]));
                harassMenu.Add("UseItemsharass", new CheckBox("Use Tiamat/Hydra"));
                harassMenu.Add("UseWH", new CheckBox("Use W"));

                itemMenu = zedMenu.AddSubMenu("items Options");
                itemMenu.Add("Youmuu", new CheckBox("Use Youmuu's"));
                itemMenu.Add("Tiamat", new CheckBox("Use Tiamat"));
                itemMenu.Add("Hydra", new CheckBox("Use Hydra"));
                itemMenu.Add("Bilge", new CheckBox("Use Bilge"));
                itemMenu.Add("BilgeEnemyhp", new Slider("If Enemy Hp <", 85, 1, 100));
                itemMenu.Add("Bilgemyhp", new Slider("Or your Hp <", 85, 1, 100));
                itemMenu.Add("BladeEnemyhp", new Slider("If Enemy Hp <", 85, 1, 100));
                itemMenu.Add("Blademyhp", new Slider("Or your Hp <", 85, 1, 100));

                farmMenu = zedMenu.AddSubMenu("Farm Options");
                farmMenu.Add("UseItemslane", new CheckBox("Use Hydra/Tiamat"));
                farmMenu.Add("UseQL", new CheckBox("Q LaneClear"));
                farmMenu.Add("UseEL", new CheckBox("E LaneClear"));
                farmMenu.Add("Energylane", new Slider("Energy Lane% >", 45, 1, 100));

                lasthitMenu = zedMenu.AddSubMenu("LastHit Options");
                lasthitMenu.Add("UseQLH", new CheckBox("Q LastHit"));
                lasthitMenu.Add("UseELH", new CheckBox("E LastHit"));
                lasthitMenu.Add("Energylast", new Slider("Energy lasthit% >", 85, 1, 100));


                jungleMenu = zedMenu.AddSubMenu("Jungle Options");
                jungleMenu.Add("UseQJ", new CheckBox("Q Jungle"));
                jungleMenu.Add("UseWJ", new CheckBox("W Jungle"));
                jungleMenu.Add("UseEJ", new CheckBox("E Jungle"));
                jungleMenu.Add("UseItemsjungle", new CheckBox("Use Hydra/Tiamat"));
                jungleMenu.Add("Energyjungle", new Slider("Energy Jungle% >", 85, 1, 100));

                miscMenu = zedMenu.AddSubMenu("Misc Options");
                miscMenu.Add("UseIgnitekill", new CheckBox("Use Ignite KillSteal"));
                miscMenu.Add("UseQM", new CheckBox("Use Q KillSteal"));
                miscMenu.Add("UseEM", new CheckBox("Use E KillSteal"));
                miscMenu.Add("AutoE", new CheckBox("Auto E"));
                miscMenu.Add("rdodge", new CheckBox("R Dodge Dangerous"));
                foreach (var e in enemy)
                {
                    SpellDataInst rdata = e.Spellbook.GetSpell(SpellSlot.R);
                    if (DangerDB.DangerousList.Any(spell => spell.Contains(rdata.SData.Name)))
                        miscMenu.Add("ds" + e.NetworkId, new ComboBox(rdata.SData.Name));
                }


                //Drawings
                drawMenu = zedMenu.AddSubMenu("Draw Options");
                drawMenu.Add("DrawHP", new CheckBox("Draw HP"));

                AssassinManager.Load();
                new DamageIndicator();

                DamageIndicator.DamageToUnit = ComboDamage;
                Chat.Print("<font color='#881df2'>Zed is Back by jackisback</font> Loaded.");
                Chat.Print("<font color='#f2881d'>if you wanna help me to pay my internet bills^^ paypal= bulut@live.co.uk</font>");


                Game.OnUpdate += Game_OnUpdate;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            }
          
       
        public static bool Getcheckboxvalue(Menu menu, string menuvalue)
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

        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs castedSpell)
        {
            if (unit.Type != GameObjectType.AIHeroClient)
                return;
            if (unit.IsEnemy)
            {
                if (Getcheckboxvalue(comboMenu, "rdodge") && _r.IsReady() && UltStage == UltCastStage.First &&
                Getcheckboxvalue(miscMenu, "ds" + unit.NetworkId))
                {
                    if (DangerDB.DangerousList.Any(spell => spell.Contains(castedSpell.SData.Name)) &&
                        (unit.Distance(_player.ServerPosition) < 650f || _player.Distance(castedSpell.End) <= 250f))
                    {
                        if (castedSpell.SData.Name == "SyndraR")
                        {
                            clockon = Environment.TickCount + 150;
                            countdanger = countdanger + 1;
                        }
                        else
                        {
                            var target = TargetSelector.GetTarget(640, DamageType.Physical);
                            _r.Cast(target);
                        }
                    }
                }
            }

            if (unit.IsMe && castedSpell.SData.Name == "zedult")
            {
                ticktock = Environment.TickCount + 200;

            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();

            }
            if (Getkeybindvalue(comboMenu, "TheLine"))
            {
                TheLine();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();

            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                Laneclear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungleClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }
            if (Getcheckboxvalue(miscMenu, "AutoE"))
            {
                CastE(target);
            }

            if (Environment.TickCount >= clockon && countdanger > countults)
            {
                _r.Cast(TargetSelector.GetTarget(640, DamageType.Physical));
                countults = countults + 1;
            }


            if (LastCastedSpell.LastCastPacketSent.Slot == SpellSlot.R)
            {
                Obj_AI_Minion shadow;
                shadow = ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow");

                rpos = shadow.ServerPosition;
            }


            _player = ObjectManager.Player;


            KillSteal();

        }

        private static float ComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;
            if (_igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, LeagueSharp.Common.Damage.SummonerSpell.Ignite);
            if (Items.HasItem(3077) && Items.CanUseItem(3077))
                damage += _player.GetItemDamage(enemy, LeagueSharp.Common.Damage.DamageItems.Tiamat);
            if (Items.HasItem(3074) && Items.CanUseItem(3074))
                damage += _player.GetItemDamage(enemy, LeagueSharp.Common.Damage.DamageItems.Hydra);
            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                damage += _player.GetItemDamage(enemy, LeagueSharp.Common.Damage.DamageItems.Botrk);
            if (Items.HasItem(3144) && Items.CanUseItem(3144))
                damage += _player.GetItemDamage(enemy, LeagueSharp.Common.Damage.DamageItems.Bilgewater);
            if (_q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q);
            if (_w.IsReady() && _q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q) / 2;
            if (_e.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.E);
            if (_r.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.R);
            damage += (_r.Level * 0.15 + 0.05) *
                      (damage - ObjectManager.Player.GetSummonerSpellDamage(enemy, LeagueSharp.Common.Damage.SummonerSpell.Ignite));

            return (float)damage;
        }

        private static void Combo()
        {
            var target = GetTarget(_r.Range);
            var overkill = _player.GetSpellDamage(target, SpellSlot.Q) + _player.GetSpellDamage(target, SpellSlot.E) + _player.GetAutoAttackDamage(target, true) * 2;
            var doubleu = _player.Spellbook.GetSpell(SpellSlot.W);


            if (Getcheckboxvalue(comboMenu, "UseUlt") && UltStage == UltCastStage.First && (overkill < target.Health ||
                (!_w.IsReady() && doubleu.Cooldown > 2f && _player.GetSpellDamage(target, SpellSlot.Q) < target.Health && target.Distance(_player.Position) > 400)))
            {
                if ((target.Distance(_player.Position) > 700 && target.MoveSpeed > _player.MoveSpeed) || target.Distance(_player.Position) > 800)
                {
                    CastW(target);
                    _w.Cast();

                }
                _r.Cast(target);
            }

            else
            {
                if (target != null && Getcheckboxvalue(comboMenu, "UseIgnitecombo") && _igniteSlot != SpellSlot.Unknown &&
                        _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                {
                    if (ComboDamage(target) > target.Health || target.HasBuff("zedulttargetmark"))
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                    }
                }
                if (target != null && ShadowStage == ShadowCastStage.First && Getcheckboxvalue(comboMenu, "UseWC") &&
                        target.Distance(_player.Position) > 400 && target.Distance(_player.Position) < 1300)
                {
                    CastW(target);
                }
                if (target != null && ShadowStage == ShadowCastStage.Second && Getcheckboxvalue(comboMenu, "UseWC") &&
                    target.Distance(WShadow.ServerPosition) < target.Distance(_player.Position))
                {
                    _w.Cast();
                }


                UseItemes(target);
                CastE(target);
                CastQ(target);

            }


        }

        private static void TheLine()
        {
            var target = GetTarget(_r.Range);

            if (target == null)
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }

            if (!_r.IsReady() || target.Distance(_player.Position) >= 640)
            {
                return;
            }
            if (UltStage == UltCastStage.First)
                _r.Cast(target);
            linepos = target.Position.LSExtend(_player.ServerPosition, -500);

            if (target != null && ShadowStage == ShadowCastStage.First && UltStage == UltCastStage.Second)
            {
                UseItemes(target);

                if (LastCastedSpell.LastCastPacketSent.Slot != SpellSlot.W)
                {
                    _w.Cast(linepos);
                    CastE(target);
                    CastQ(target);


                    if (target != null && Getcheckboxvalue(comboMenu, "UseIgnitecombo") && _igniteSlot != SpellSlot.Unknown &&
                            _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                    }

                }
            }

            if (target != null && WShadow != null && UltStage == UltCastStage.Second && target.Distance(_player.Position) > 250 && (target.Distance(WShadow.ServerPosition) < target.Distance(_player.Position)))
            {
                _w.Cast();
            }

        }

        private static void _CastQ(AIHeroClient target)
        {
            throw new NotImplementedException();
        }

        private static void Harass()
        {
            var target = GetTarget(_q.Range);

            var useItemsH = Getcheckboxvalue(harassMenu, "UseItemsharass");

            if (target.IsValidTarget() && Getkeybindvalue(harassMenu, "longhar") && _w.IsReady() && _q.IsReady() && ObjectManager.Player.Mana >
                _q.ManaCost +
                _w.ManaCost && target.Distance(_player.Position) > 850 &&
                target.Distance(_player.Position) < 1400)
            {
                CastW(target);
            }

            if (target.IsValidTarget() && (ShadowStage == ShadowCastStage.Second || ShadowStage == ShadowCastStage.Cooldown || !(Getcheckboxvalue(harassMenu, "UseWH")))
                            && _q.IsReady() &&
                                (target.Distance(_player.Position) <= 900 || target.Distance(WShadow.ServerPosition) <= 900))
            {
                CastQ(target);
            }

            if (target.IsValidTarget() && _w.IsReady() && _q.IsReady() && Getcheckboxvalue(harassMenu, "UseWH") &&
                ObjectManager.Player.Mana >
                _q.ManaCost +
                _w.ManaCost)
            {
                if (target.Distance(_player.Position) < 750)

                    CastW(target);
            }

            CastE(target);

            if (useItemsH && _tiamat.IsReady() && target.Distance(_player.Position) < _tiamat.Range)
            {
                _tiamat.Cast();
            }
            if (useItemsH && _hydra.IsReady() && target.Distance(_player.Position) < _hydra.Range)
            {
                _hydra.Cast();
            }

        }

        private static void Laneclear()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _e.Range);
            var mymana = (_player.Mana >= (_player.MaxMana * Getslidervalue(farmMenu, "Energylane")) / 100);

            var useItemsl = Getcheckboxvalue(farmMenu, "UseItemslane");
            var useQl = Getcheckboxvalue(farmMenu, "UseQL");
            var useEl = Getcheckboxvalue(farmMenu, "UseEL");
            if (_q.IsReady() && useQl && mymana)
            {
                var fl2 = _q.GetLineFarmLocation(allMinionsQ, _q.Width);

                if (fl2.MinionsHit >= 3)
                {
                    _q.Cast(fl2.Position);
                }
                else
                    foreach (var minion in allMinionsQ)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                            _q.Cast(minion);
            }

            if (_e.IsReady() && useEl && mymana)
            {
                if (allMinionsE.Count > 2)
                {
                    _e.Cast();
                }
                else
                    foreach (var minion in allMinionsE)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.E))
                            _e.Cast();
            }

            if (useItemsl && _tiamat.IsReady() && allMinionsE.Count > 2)
            {
                _tiamat.Cast();
            }
            if (useItemsl && _hydra.IsReady() && allMinionsE.Count > 2)
            {
                _hydra.Cast();
            }
        }

        private static void LastHit()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All);
            var mymana = (_player.Mana >=
                          (_player.MaxMana * Getslidervalue(lasthitMenu, "Energylast")) / 100);
            var useQ = Getcheckboxvalue(lasthitMenu, "UseQLH");
            var useE = Getcheckboxvalue(lasthitMenu, "UseELH");
            foreach (var minion in allMinions)
            {
                if (mymana && useQ && _q.IsReady() && _player.Distance(minion.ServerPosition) < _q.Range &&
                    minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    _q.Cast(minion);
                }

                if (mymana && _e.IsReady() && useE && _player.Distance(minion.ServerPosition) < _e.Range &&
                    minion.Health < 0.95 * _player.GetSpellDamage(minion, SpellSlot.E))
                {
                    _e.Cast();
                }
            }
        }

        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var mymana = (_player.Mana >=
                          (_player.MaxMana * Getslidervalue(jungleMenu, "Energyjungle")) / 100);
            var useItemsJ = Getcheckboxvalue(jungleMenu, "UseItemsjungle");
            var useQ = Getcheckboxvalue(jungleMenu, "UseQJ");
            var useW = Getcheckboxvalue(jungleMenu, "UseWJ");
            var useE = Getcheckboxvalue(jungleMenu, "UseEJ");

            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (mymana && _w.IsReady() && useW && _player.Distance(mob.ServerPosition) < _q.Range)
                {
                    _w.Cast(mob.Position);
                }
                if (mymana && useQ && _q.IsReady() && _player.Distance(mob.ServerPosition) < _q.Range)
                {
                    CastQ(mob);
                }
                if (mymana && _e.IsReady() && useE && _player.Distance(mob.ServerPosition) < _e.Range)
                {
                    _e.Cast();
                }

                if (useItemsJ && _tiamat.IsReady() && _player.Distance(mob.ServerPosition) < _tiamat.Range)
                {
                    _tiamat.Cast();
                }
                if (useItemsJ && _hydra.IsReady() && _player.Distance(mob.ServerPosition) < _hydra.Range)
                {
                    _hydra.Cast();
                }
            }

        }
        private static AIHeroClient GetTarget(float vDefaultRange = 0,
            DamageType vDefaultDamageType = DamageType.Physical)
        {
            if (Math.Abs(vDefaultRange) < 0.00001)
                vDefaultRange = _q.Range;

            if (!AssassinManager.getCheckBoxItem("AssassinActive"))
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = AssassinManager.getSliderItem("AssassinSearchRange");

            var vEnemy =
                ObjectManager.Get<AIHeroClient>()
                    .Where(
                        enemy =>
                            enemy.Team != _player.Team && !enemy.IsDead && enemy.IsVisible &&
                            AssassinManager.assMenu["Assassin" + enemy.NetworkId] != null &&
                            AssassinManager.getCheckBoxItem("Assassin" + enemy.NetworkId) &&
                            _player.Distance(enemy) < assassinRange);

            if (AssassinManager.getBoxItem("AssassinSelectOption") == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            var objAiHeroes = vEnemy as AIHeroClient[] ?? vEnemy.ToArray();
            var t = !objAiHeroes.Any() ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType) : objAiHeroes[0];
            return t;
        }

        private static void UseItemes(AIHeroClient target)
        {
            var iBilge = Getcheckboxvalue(itemMenu, "Bilge");
            var iBilgeEnemyhp = target.Health <=
                                (target.MaxHealth * (Getslidervalue(itemMenu, "BilgeEnemyhp")) / 100);
            var iBilgemyhp = _player.Health <=
                             (_player.MaxHealth * (Getslidervalue(itemMenu, "Bilgemyhp")) / 100);
            var iBlade = Getcheckboxvalue(itemMenu, "Blade");
            var iBladeEnemyhp = target.Health <=
                                (target.MaxHealth * (Getslidervalue(itemMenu, "BladeEnemyhp")) / 100);
            var iBlademyhp = _player.Health <=
                             (_player.MaxHealth * (Getslidervalue(itemMenu, "Blademyhp")) / 100);
            var iTiamat = Getcheckboxvalue(itemMenu, "Tiamat");
            var iHydra = Getcheckboxvalue(itemMenu, "Hydra");
            var iYoumuu = Getcheckboxvalue(itemMenu, "Youmuu");
            //var ihp = _config.Item("Hppotion").GetValue<bool>();
            // var ihpuse = _player.Health <= (_player.MaxHealth * (_config.Item("Hppotionuse").GetValue<Slider>().Value) / 100);
            //var imp = _config.Item("Mppotion").GetValue<bool>();
            //var impuse = _player.Health <= (_player.MaxHealth * (_config.Item("Mppotionuse").GetValue<Slider>().Value) / 100);

            if (_player.Distance(target.ServerPosition) <= 450 && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
            {
                _bilge.Cast(target);

            }
            if (_player.Distance(target.ServerPosition) <= 450 && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
            {
                _blade.Cast(target);

            }
            if (_player.Distance(target.ServerPosition) <= 300 && iTiamat && _tiamat.IsReady())
            {
                _tiamat.Cast();

            }
            if (_player.Distance(target.ServerPosition) <= 300 && iHydra && _hydra.IsReady())
            {
                _hydra.Cast();

            }
            if (_player.Distance(target.ServerPosition) <= 350 && iYoumuu && _youmuu.IsReady())
            {
                _youmuu.Cast();

            }
        }

        private static Obj_AI_Minion WShadow
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && (minion.ServerPosition != rpos) && minion.Name == "Shadow");
            }
        }
        private static Obj_AI_Minion RShadow
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && (minion.ServerPosition == rpos) && minion.Name == "Shadow");
            }
        }

        private static UltCastStage UltStage
        {
            get
            {
                if (!_r.IsReady()) return UltCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR"
                //return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "zedult"
                    ? UltCastStage.First
                    : UltCastStage.Second);
            }
        }


        private static ShadowCastStage ShadowStage
        {
            get
            {
                if (!_w.IsReady()) return ShadowCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedW"
                //return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash"
                    ? ShadowCastStage.First
                    : ShadowCastStage.Second);

            }
        }

        private static void CastW(Obj_AI_Base target)
        {
            if (delayw >= Environment.TickCount - shadowdelay || ShadowStage != ShadowCastStage.First ||
                (target.HasBuff("zedulttargetmark") && LastCastedSpell.LastCastPacketSent.Slot == SpellSlot.R && UltStage == UltCastStage.Cooldown))
                return;

            var herew = target.Position.Extend(ObjectManager.Player.Position, -200);

            _w.Cast(herew, true);
            shadowdelay = Environment.TickCount;

        }

        private static void CastQ(Obj_AI_Base target)
        {
            if (!_q.IsReady()) return;

            if (WShadow != null && target.Distance(WShadow.ServerPosition) <= 900 && target.Distance(_player.ServerPosition) > 450)
            {

                var shadowpred = _q.GetPrediction(target);
                _q.UpdateSourcePosition(WShadow.ServerPosition, WShadow.ServerPosition);
                if (shadowpred.Hitchance >= HitChance.Medium)
                    _q.Cast(target);


            }
            else
            {

                _q.UpdateSourcePosition(_player.ServerPosition, _player.ServerPosition);
                var normalpred = _q.GetPrediction(target);

                if (normalpred.CastPosition.Distance(_player.ServerPosition) < 900 && normalpred.Hitchance >= HitChance.Medium)
                {
                    _q.Cast(target);
                }


            }


        }

        private static void CastE(Obj_AI_Base target)
        {
            if (!_e.IsReady() || !target.IsValidTarget(_e.Range))
            {
                return;
            }
            if (ObjectManager.Get<AIHeroClient>().Count(hero =>hero.IsValidTarget() && (hero.Distance(ObjectManager.Player.ServerPosition) <= _e.Range || (WShadow != null && hero.Distance(WShadow.ServerPosition) <= _e.Range))) > 0) _e.Cast(target);
        }

        internal enum UltCastStage
        {
            First,
            Second,
            Cooldown
        }

        internal enum ShadowCastStage
        {
            First,
            Second,
            Cooldown
        }

        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(2000, DamageType.Physical);
            var igniteDmg = _player.GetSummonerSpellDamage(target, LeagueSharp.Common.Damage.SummonerSpell.Ignite);
            if (target.IsValidTarget() && Getcheckboxvalue(miscMenu, "UseIgnitekill") && _igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (igniteDmg > target.Health && _player.Distance(target.ServerPosition) <= 600)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, target);
                }
            }
            if (target.IsValidTarget() && _q.IsReady() && Getcheckboxvalue(miscMenu, "UseQM") && _q.GetDamage(target) > target.Health)
            {
                if (_player.Distance(target.ServerPosition) <= _q.Range)
                {
                    _q.Cast(target);
                }
                else if (WShadow != null && WShadow.Distance(target.ServerPosition) <= _q.Range)
                {
                    _q.UpdateSourcePosition(WShadow.ServerPosition, WShadow.ServerPosition);
                    _q.Cast(target);
                }
                else if (RShadow != null && RShadow.Distance(target.ServerPosition) <= _q.Range)
                {
                    _q.UpdateSourcePosition(RShadow.ServerPosition, RShadow.ServerPosition);
                    _q.Cast(target);
                }
            }

            if (target.IsValidTarget() && _q.IsReady() && Getcheckboxvalue(miscMenu, "UseQM") && _q.GetDamage(target) > target.Health)
            {
                if (_player.Distance(target.ServerPosition) <= _q.Range)
                {
                    _q.Cast(target);
                }
                else if (WShadow != null && WShadow.Distance(target.ServerPosition) <= _q.Range)
                {
                    _q.UpdateSourcePosition(WShadow.ServerPosition, WShadow.ServerPosition);
                    _q.Cast(target);
                }
            }
            if (_e.IsReady() && Getcheckboxvalue(miscMenu, "UseEM"))
            {
                var t = TargetSelector.GetTarget(_e.Range, DamageType.Physical);
                if (_e.GetDamage(t) > t.Health && (_player.Distance(t.ServerPosition) <= _e.Range || WShadow.Distance(t.ServerPosition) <= _e.Range))
                {
                    _e.Cast();
                }
            }
        }
    }
}

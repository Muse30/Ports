#region

using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using LeagueSharp.Common;
using SharpDX;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

#endregion

namespace AutoLantern
{
    internal class Program
    {
        public static Menu lanternMenu;
        public static SpellSlot LanternSlot = (SpellSlot)62;
        public static int LastLantern;

        private static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static SpellDataInst LanternSpell
        {
            get { return Player.Spellbook.GetSpell(LanternSlot); }
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

        public static void OnGameLoad()
        {
            if (!ThreshInGame())
            {
                return;
            }

            lanternMenu = MainMenu.AddMenu("AutoLantern", "Auto Lantern");
            lanternMenu.Add("Auto", new CheckBox("Auto-Lantern at Low HP", true));
            lanternMenu.Add("Hotkey", new KeyBind("Burst Combo", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));
            lanternMenu.Add("LanternReady", new CheckBox("Lantern Ready", false));
            lanternMenu.Add("Low", new Slider("Low HP Percent", 20, 10, 50));

           
            Game.OnUpdate += OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender is AIHeroClient && sender.IsAlly && args.SData.Name.Equals("LanternWAlly"))
            {
                LastLantern = Utils.TickCount;
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (!IsLanternSpellActive())
            {
                Getcheckboxvalue(lanternMenu, "LanternReady");
                return;
            }


            if (Getcheckboxvalue(lanternMenu, "Auto") && IsLow() && UseLantern())
            {
                return;
            }

            if (!Getkeybindvalue(lanternMenu, "Hotkey"))
            {
                return;
            }

            UseLantern();
        }

        private static bool IsLanternSpellActive()
        {
            return LanternSpell != null && LanternSpell.Name.Equals("LanternWAlly");
        }

        private static bool UseLantern()
        {
            var lantern =
                ObjectManager.Get<Obj_AI_Base>()
                    .FirstOrDefault(
                        o => o.IsValid && o.IsAlly && o.Name.Equals("ThreshLantern") && Player.Distance(o) <= 500);

            return lantern != null && lantern.IsVisible && Utils.TickCount - LastLantern > 5000 &&
                   Player.Spellbook.CastSpell(LanternSlot, lantern);
        }

        private static bool IsLow()
        {
            return Player.HealthPercent <= Getslidervalue(lanternMenu, "Low");
        }

        private static bool ThreshInGame()
        {
            return HeroManager.Allies.Any(h => !h.IsMe && h.ChampionName.Equals("Thresh"));
        }
    }
}
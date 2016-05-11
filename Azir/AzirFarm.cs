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
using System.Text.RegularExpressions;
using Color = System.Drawing.Color;

namespace HeavenStrikeAzir
{
    public static class AzirFarm
    {
        public static void Initialize()
        {
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
                return;
            if (OrbwalkCommands.CanDoAttack())
            {
                var minion = OrbwalkCommands.GetClearMinionsAndBuildings();
                if (minion.IsValidTarget())
                {
                    OrbwalkCommands.AttackTarget(minion);
                }
            }
        }
    }
}

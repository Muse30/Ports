﻿#region

using System;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;

#endregion

namespace Ports
{
    internal static class Init
    {
        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Initialize;
        }

        private static void Initialize(EventArgs args)
        {

            // Misc.'s - Will add an option when the AIO is done to disable certain Utility Addons


            switch (ObjectManager.Player.ChampionName.ToLower())
            {
                case "lucian": // BrianSharp's Aatrox
                    Ports.Lucian.Program.Game_OnGameLoad();
                    break;
                case "riven": // DZAhri
                    Ports.Riven.Program.Game_OnGameLoad();
                    break;           
                default:
                    Chat.Print("This champion is not supported yet! - Berb");
                    break;
            }
        }
    }
}
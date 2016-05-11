#region

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


            switch (ObjectManager.Player.ChampionName.ToLower())
            { 
                case "riven": // Kurisu Riven
                    NechritoRiven.Program.OnGameLoad();
                    break;
                case "azir": // azir
                    HeavenStrikeAzir.Program.Game_OnGameLoad();
                    break;
                case "leesin": // bubba
                    WreckingBall.WreckingBall.WreckingBallLoad();
                    break;
                case "lantern":
                    AutoLantern.Program.OnGameLoad();
                    break;
                default:
                    Chat.Print("This champion is not supported yet!");
                    break;
            }
        }
    }
}
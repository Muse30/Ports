using System;
using System.Reflection;
using LeagueSharp.Common;
using EloBuddy.SDK.Events;

namespace Ports.Riven
{
    internal static class Program
    {
        public static void Game_OnGameLoad()
        {
            new KurisuRiven();
        }
    }
}
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;

namespace SpeedMeter
{
    public static class Utils
    {
        public static void Print(this CCSPlayerController player, string msg, params object[] args)
        {
            player.PrintToChat($" {SpeedMeter._Config.Prefix} {ChatColors.White}{SpeedMeter._Localizer?.ForPlayer(player, msg, args).TrimStart(' ')}");
        }

        public static void PrintToAll(string msg, params object[] args)
        {
            foreach (var x in Utilities.GetPlayers())
            {
                x.Print(msg, args);
            }
        }
    }
}

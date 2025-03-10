using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}

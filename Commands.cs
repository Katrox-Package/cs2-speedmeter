using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace SpeedMeter
{
    public partial class SpeedMeter
    {
        public void OnSpeedCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            SpeedMeterManager.SpeedMeterToggle(player);
            var text = SpeedMeterPlayers[player.SteamID].Enabled ? "Status_Enabled" : "Status_Disabled";
            player.Print(text);
        }

        public void OnSpeedEditCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            if (!SpeedMeterPlayers.TryGetValue(player.SteamID, out var settings) || !settings.Enabled)
            {
                player.Print("Edit_NeedEnable");
                return;
            }

            settings.Editing = !settings.Editing;

            var text = settings.Editing ? "Edit_Enabled" : "Edit_Disabled";
            player.Print(text);
        }

        public void OnTopSpeedCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            var records = GetTopSpeedRecords(10);
            if (records == null || records.Count == 0)
            {
                player.Print("NoRecords");
                return;
            }

            player.Print("TopSpeed_Title");

            for (int i = 0; i < records.Count; i++)
            {
                player.Print("TopSpeed_Entry", i + 1, records[i].PlayerName, Math.Round(records[i].Speed));
            }

            var playerRecord = GetPlayerBestSpeed(player.SteamID);
            if (playerRecord != null)
            {
                player.Print("MySpeed", Math.Round(playerRecord.Speed));
            }
        }

        [RequiresPermissions("@css/root")]
        public void OnResetTopSpeedCommand(CCSPlayerController? player, CommandInfo command)
        {
            var admin = player?.PlayerName ?? "Console";

            ResetSpeedRecordDatas();

            Utils.PrintToAll("TopSpeed_Reset", admin);
        }
    }
}
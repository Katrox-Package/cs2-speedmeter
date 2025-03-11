using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace SpeedMeter
{
    public partial class SpeedMeter
    {
        private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (@event.Userid is not { } player)
                return HookResult.Continue;

            if (Config.DefaultHudEnabled)
            {
                SpeedMeterManager.SpeedMeterToggle(player);
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (@event.Userid is not { } player)
                return HookResult.Continue;

            if (player.SteamID is { } steamid)
            {
                if (SpeedMeterPlayers.TryGetValue(steamid, out var settings))
                {
                    if (settings.UsingSave)
                    {
                        SaveSpeedMeterSettingsDb(steamid, settings.X, settings.Y, settings.Size, ColorMaps.FirstOrDefault(x => x.Value == settings.Color).Key);
                    }

                    SpeedMeterPlayers.Remove(steamid);
                }
            }
            return HookResult.Continue;
        }

        private void OnTick()
        {
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                var ent = NativeAPI.GetEntityFromIndex(i);
                if (ent == 0)
                    continue;

                var player = new CCSPlayerController(ent);
                if (player == null || !player.IsValid)
                    continue;

                SpeedMeterManager.SpeedMeterOnTick(player);
            }
        }
    }
}

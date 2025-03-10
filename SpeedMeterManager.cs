using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using static SpeedMeter.SpeedMeter;
using CounterStrikeSharp.API.Modules.Entities;

namespace SpeedMeter
{
    public class SpeedMeterSettings
    {
        public bool Enabled { get; set; } = false;
        public bool Editing { get; set; } = false;
        public bool UsingSave { get; set; } = false;
        public byte Size { get; set; } = 50;
        public float X { get; set; } = 0.57f;
        public float Y { get; set; } = 4.367f;
        public Color Color { get; set; } = Color.Beige;
        public string Font { get; set; } = "Arial Bold";
        public uint Fov { get; set; } = 0;
    }

    public static class SpeedMeterManager
    {
        private const uint DeadCheckFov = 48392; // magic number
        private const byte SpeedMeterChannel = 11; // some magic
        private const float ZPos = 6.75f;
        private const float MinSpeedToRecord = 260;

        private const float RecordCooldown = 0.3f;
        private static float LastRecordTime = 0.0f;

        public static void SpeedMeterToggle(CCSPlayerController controller)
        {
            if (GameHudApi == null)
                return;

            if (!SpeedMeterPlayers.TryGetValue(controller.SteamID, out var settings))
                settings = SpeedMeterPlayers[controller.SteamID] = new();

            if (!settings.Enabled)
            {
                if (!settings.UsingSave)
                {
                    var data = GetSpeedMeterSettingsDb(controller.SteamID);
                    if (data is not null)
                    {
                        settings.X = data.X;
                        settings.Y = data.Y;
                        settings.Size = data.Size;
                    }
                    settings.UsingSave = true;
                }

                var (x, y, size) = GetXYWithFov(controller, settings.X, settings.Y, settings.Size);
                GameHudApi.Native_GameHUD_SetParams(controller, SpeedMeterChannel, new Vector(x, y, ZPos), settings.Color, size, settings.Font, 0.01f);
                GameHudApi.Native_GameHUD_ShowPermanent(controller, SpeedMeterChannel, "0");
            }
            else
            {
                GameHudApi.Native_GameHUD_Remove(controller, SpeedMeterChannel);
            }
            settings.Enabled = !settings.Enabled;
        }

        public static void SpeedMeterOnTick(CCSPlayerController controller)
        {
            if (GameHudApi == null)
                return;

            if (controller.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE)
            {
                var speed = controller.PlayerPawn.Value?.AbsVelocity?.Length2D() ?? 0;
                ProcessSpeedRecord(controller, speed);
            }

            if (!SpeedMeterPlayers.TryGetValue(controller.SteamID, out var settings))
                return;

            if (controller.PlayerPawn.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                if (settings.Fov != DeadCheckFov)
                {
                    GameHudApi.Native_GameHUD_Remove(controller, SpeedMeterChannel);
                    settings.Fov = DeadCheckFov;
                }
                return;
            }

            if (settings.Enabled)
            {
                var speed = controller.PlayerPawn.Value?.AbsVelocity?.Length2D() ?? 0;
                var text = Math.Round(speed).ToString();
                GameHudApi.Native_GameHUD_ShowPermanent(controller, SpeedMeterChannel, text);
            }
            else
            {
                return;
            }

            if (settings.Editing)
            {
                var buttons = controller.Buttons;
                float adjustment = 0.03f;
                if (buttons.HasFlag(PlayerButtons.Duck)) adjustment /= 2.2f;
                if (buttons.HasFlag(PlayerButtons.Speed)) adjustment *= 2.2f;

                if (buttons.HasFlag(PlayerButtons.Forward)) settings.Y += adjustment;
                if (buttons.HasFlag(PlayerButtons.Back)) settings.Y -= adjustment;
                if (buttons.HasFlag(PlayerButtons.Moveleft)) settings.X -= adjustment;
                if (buttons.HasFlag(PlayerButtons.Moveright)) settings.X += adjustment;

                if (buttons.HasFlag(PlayerButtons.Attack)) settings.Size += 1;
                if (buttons.HasFlag(PlayerButtons.Attack2)) settings.Size -= 1;

                var (x, y, size) = GetXYWithFov(controller, settings.X, settings.Y, settings.Size);
                var z = ZPos;
                GameHudApi.Native_GameHUD_UpdateParams(controller, SpeedMeterChannel, new Vector(x, y, z), settings.Color, size, settings.Font, 0.01f);
            }

            var fov = controller.DesiredFOV == 0 ? 90 : controller.DesiredFOV;
            if (settings.Fov != fov)
            {
                var (x, y, size) = GetXYWithFov(controller, settings.X, settings.Y, settings.Size);
                var z = ZPos;
                GameHudApi.Native_GameHUD_UpdateParams(controller, SpeedMeterChannel, new Vector(x, y, z), settings.Color, size, settings.Font, 0.01f);
            }
            settings.Fov = fov;
        }

        private static (float x, float y, byte size) GetXYWithFov(CCSPlayerController controller, float x, float y, float size)
        {
            var fov = controller.DesiredFOV == 0 ? 90 : controller.DesiredFOV;
            float baseTan = (float)Math.Tan(45 * Math.PI / 180); // tan(45) = 1
            float currentTan = (float)Math.Tan((fov / 2) * Math.PI / 180);
            float newX = x * currentTan / baseTan;
            float newY = y * currentTan / baseTan;
            byte newSize = (byte)(size * currentTan / baseTan);
            return (newX, newY, newSize);
        }

        private static void ProcessSpeedRecord(CCSPlayerController controller, float speed)
        {
            if (speed < MinSpeedToRecord)
                return;

            var bestRecord = GetTopSpeedRecords(1).FirstOrDefault();

            var roundedSpeed = Math.Round(speed);
            var bestRounded = bestRecord != null ? Math.Round(bestRecord.Speed) : 0;

            if (bestRecord == null || roundedSpeed > bestRounded)
            {
                SaveSpeedRecord(controller.SteamID, controller.PlayerName, speed);

                if (!(Server.CurrentTime - LastRecordTime < RecordCooldown)
                    && _Config.NotifyChatOnNewRecord)
                {
                    LastRecordTime = Server.CurrentTime;
                    Utils.PrintToAll("Record_Better", roundedSpeed, bestRounded, controller.PlayerName);
                }
            }
        }

    }
}

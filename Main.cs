using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Timers;
using CS2_GameHUDAPI;
using cs2_speedmeter;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace SpeedMeter
{
    public class SpeedMeterConfig : BasePluginConfig
    {
        public string Prefix { get; set; } = "{lightred}[Katrox]";
        public bool DefaultHudEnabled { get; set; } = false;
        public bool NotifyChatOnNewRecord { get; set; } = true;

        public string[] SpeedMeterCommands { get; set; } =
        {
            "speedmeter", "myspeed"
        };

        public string[] EditSpeedMeterCommands { get; set; } =
        {
            "speedmeteredit", "myspeededit", "editspeedmeter"
        };

        public string[] TopSpeedCommands { get; set; } =
        {
            "topspeed", "speedtop"
        };

        public string[] ResetTopSpeedCommands { get; set; } =
        {
            "resettopspeed", "resetspeedtop"
        };

        public string DatabaseHost { get; set; } = "";
        public int DatabasePort { get; set; } = 3306;
        public string DatabaseUser { get; set; } = "";
        public string DatabasePassword { get; set; } = "";
        public string DatabaseName { get; set; } = "";
    }

    public partial class SpeedMeter : BasePlugin, IPluginConfig<SpeedMeterConfig>
    {
        public override string ModuleName => "cs2-speedmeter";
        public override string ModuleAuthor => "Roxy & Katarina";
        public override string ModuleVersion => "0.0.4";

        public override void Load(bool hotReload)
        {
            _Logger = Logger;
            _Localizer = Localizer;

            try
            {
                InitDatabase();
            }
            catch (Exception ex)
            {
                _Logger?.LogError($"[SpeedMeter] InitDatabase: {ex.Message}");
            }

            RegisterListener<Listeners.OnTick>(OnTick);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

            RegisterListener<Listeners.OnMapStart>(_ => SaveCacheToDatabase());
            RegisterListener<Listeners.OnMapEnd>(() => SaveCacheToDatabase());

            foreach (var x in Config.SpeedMeterCommands) AddCommand(x, "", OnSpeedCommand);
            foreach (var x in Config.EditSpeedMeterCommands) AddCommand(x, "", OnSpeedEditCommand);
            foreach (var x in Config.TopSpeedCommands) AddCommand(x, "", OnTopSpeedCommand);
            foreach (var x in Config.ResetTopSpeedCommands) AddCommand(x, "", OnResetTopSpeedCommand);

            if (Db != null)
            {
                _SaveCacheToDatabaseTimer = AddTimer(15f * 60f, SaveCacheToDatabase, TimerFlags.REPEAT);
            }
        }

        public override void Unload(bool hotReload)
        {
            if (Db != null)
            {
                _SaveCacheToDatabaseTimer?.Kill();
                _SaveCacheToDatabaseTimer = null;
                SaveCacheToDatabase();
            }

            RemoveListener<Listeners.OnTick>(OnTick);
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            try
            {
                GameHudApi = IGameHUDAPI.Capability.Get() ?? throw new Exception("GameHudApi bulunamadı.");
            }
            catch (Exception ex)
            {
                _Logger?.LogError($"[SpeedMeter] OnAllPluginsLoaded: {ex.Message}");
            }
        }

        public void OnConfigParsed(SpeedMeterConfig config)
        {
            config.Prefix = config.Prefix.ReplaceColorTags();
            Config = config;
            _Config = config;
        }

        private void InitDatabase()
        {
            if (string.IsNullOrEmpty(Config.DatabaseHost) ||
                string.IsNullOrEmpty(Config.DatabaseUser) ||
                string.IsNullOrEmpty(Config.DatabasePassword) ||
                string.IsNullOrEmpty(Config.DatabaseName))
            {
                return;
            }

            Db = new Database(
                Config.DatabaseHost,
                Config.DatabasePort,
                Config.DatabaseUser,
                Config.DatabasePassword,
                Config.DatabaseName
            );

            Task.Run(async () =>
            {
                try
                {
                    await Db.InitializeDatabaseAsync();

                    TopSpeedRecords = await Db.GetTopSpeedRecordsAsync(-1);
                }
                catch (Exception ex)
                {
                    _Logger?.LogError($"[SpeedMeter] InitializeDatabaseAsync: {ex.Message}");
                }
            });
        }

        public static ILogger? _Logger { get; set; }
        public static IStringLocalizer? _Localizer { get; set; }
        public SpeedMeterConfig Config { get; set; } = new();
        public static SpeedMeterConfig _Config { get; set; } = new();
        public static Database? Db { get; set; }
        public static IGameHUDAPI? GameHudApi { get; set; }

        public static Dictionary<ulong, SpeedMeterSettings> SpeedMeterPlayers = new();
        public static List<SpeedRecord> TopSpeedRecords = new();
        public static List<SpeedRecord> TopSpeedRecordsCache = new();

        public CounterStrikeSharp.API.Modules.Timers.Timer? _SaveCacheToDatabaseTimer;

        private void SaveCacheToDatabase()
        {
            if (Db == null)
                return;

            Task.Run(async () =>
            {
                try
                {
                    foreach (var player in SpeedMeterPlayers)
                    {
                        if (player.Value.UsingSave)
                        {
                            try
                            {
                                await Db.SaveSettingAsync(player.Key, player.Value.X, player.Value.Y, player.Value.Size);
                            }
                            catch (Exception ex)
                            {
                                _Logger?.LogError($"[SpeedMeter] SaveCacheToDatabase SaveSettingAsync: {ex.Message}");
                            }
                        }
                    }

                    foreach (var record in TopSpeedRecordsCache)
                    {
                        try
                        {
                            await Db.SaveSpeedRecordAsync(record.SteamId, record.PlayerName, record.Speed);
                            TopSpeedRecordsCache.Remove(record);
                        }
                        catch (Exception ex)
                        {
                            _Logger?.LogError($"[SpeedMeter] SaveCacheToDatabase SaveSpeedRecordAsync: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _Logger?.LogError($"[SpeedMeter] SaveCacheToDatabase: {ex.Message}");
                }
            });
        }
    }
}


using cs2_speedmeter;
using Microsoft.Extensions.Logging;

namespace SpeedMeter
{
    public partial class SpeedMeter
    {

        #region Speed Meter Settings

        public static SpeedSettingsDatabase? GetSpeedMeterSettingsDb(ulong steamId)
        {
            if (Db == null)
                return null;

            try
            {
                return Task.Run(async () => await Db.GetSettingAsync(steamId)).Result;
            }
            catch (Exception ex)
            {
                _Logger?.LogError($"[SpeedMeter] GetSpeedMeterSettings: {ex.Message}");
                return null;
            }
        }

        public static void SaveSpeedMeterSettingsDb(ulong steamId, float x, float y, byte size)
        {
            if (Db == null)
                return;

            try
            {
                Task.Run(async () => await Db.SaveSettingAsync(steamId, x, y, size));
            }
            catch (Exception ex)
            {
                _Logger?.LogError($"[SpeedMeter] SaveSpeedMeterSettings: {ex.Message}");
            }
        }

        #endregion

        #region Speed Records

        public static void SaveSpeedRecord(ulong steamId, string playerName, float speed)
        {
            try
            {
                var newRecord = new SpeedRecord
                {
                    SteamId = steamId,
                    PlayerName = playerName,
                    Speed = speed
                };

                TopSpeedRecords.RemoveAll(x => x.SteamId == steamId);
                TopSpeedRecordsCache.RemoveAll(x => x.SteamId == steamId);

                TopSpeedRecords.Add(newRecord);
                TopSpeedRecordsCache.Add(newRecord);

                if (!PlayerBestSpeeds.ContainsKey(steamId) || PlayerBestSpeeds[steamId].Speed < speed)
                {
                    PlayerBestSpeeds[steamId] = newRecord;
                }
            }
            catch (Exception ex)
            {
                _Logger?.LogError($"[SpeedMeter] SaveSpeedRecord: {ex.Message}");
            }
        }

        public static void ResetSpeedRecordDatas()
        {
            try
            {
                TopSpeedRecords.Clear();
                TopSpeedRecordsCache.Clear();

                PlayerBestSpeeds.Clear();

                if (Db != null)
                {
                    Task.Run(async () => await Db.ResetSpeedRecordDatasAsync());
                }
            }
            catch (Exception ex)
            {
                _Logger?.LogError($"[SpeedMeter] ResetSpeedRecordDatas: {ex.Message}");
            }
        }

        public static List<SpeedRecord> GetTopSpeedRecords(int limit = 10)
        {
            try
            {
                return TopSpeedRecords
                    .OrderByDescending(r => r.Speed)
                    .Take(limit == -1 ? TopSpeedRecords.Count : limit)
                    .ToList();
            }
            catch (Exception ex)
            {
                _Logger?.LogError($"[SpeedMeter] GetTopSpeedRecords: {ex.Message}");
                return new List<SpeedRecord>();
            }
        }

        public static SpeedRecord? GetPlayerBestSpeed(ulong steamId)
        {
            try
            {
                if (PlayerBestSpeeds.TryGetValue(steamId, out var record))
                {
                    return record;
                }
                return null;
            }
            catch (Exception ex)
            {
                _Logger?.LogError($"[SpeedMeter] GetPlayerBestSpeed: {ex.Message}");
                return null;
            }
        }

        #endregion

    }
}

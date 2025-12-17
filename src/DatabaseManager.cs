using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; 
using Dapper;
using SwiftlyS2.Core;

namespace Speedometer;

public static class DatabaseManager
{
    public static async Task InitializeAsync()
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try 
        {
            connection.Open();
            
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS speedometer_data (
                    steamid VARCHAR(64) PRIMARY KEY,
                    hud_enabled INT DEFAULT 1,
                    keys_enabled INT DEFAULT 1,
                    jumps_enabled INT DEFAULT 1,
                    color VARCHAR(32) DEFAULT '#00FF00'
                );");

            try { await connection.ExecuteAsync("ALTER TABLE speedometer_data ADD COLUMN show_round_stats INT DEFAULT 1;"); } catch { }

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS topspeed_records (
                    id INT AUTO_INCREMENT PRIMARY KEY, 
                    steamid VARCHAR(64),
                    player_name VARCHAR(128),
                    map_name VARCHAR(128),
                    velocity INT,
                    reach_time FLOAT, 
                    date_achieved TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );");
                
            try { await connection.ExecuteAsync("CREATE INDEX idx_topspeed_map_steamid ON topspeed_records(map_name, steamid);"); } catch { }
        }
        catch (Exception ex) { Console.WriteLine($"[Speedometer] DB Init Error: {ex.Message}"); }
    }

    public static async Task<PlayerData?> LoadPlayerAsync(string steamId)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            var data = await connection.QuerySingleOrDefaultAsync<PlayerData>(
                "SELECT * FROM speedometer_data WHERE steamid = @SteamId", new { SteamId = steamId });

            if (data == null)
            {
                data = new PlayerData
                {
                    steamid = steamId,
                    hud_enabled = Speedometer.Config.DefaultHudEnabled ? 1 : 0,
                    keys_enabled = Speedometer.Config.DefaultKeyOverlayEnabled ? 1 : 0,
                    jumps_enabled = Speedometer.Config.DefaultJumpsEnabled ? 1 : 0,
                    show_round_stats = Speedometer.Config.DefaultShowRoundStats ? 1 : 0,
                    color = Globals.GetHexFromColorName(Speedometer.Config.DefaultColor)
                };
                await SavePlayerAsync(data);
            }
            return data;
        }
        catch { return null; }
    }

    public static async Task SavePlayerAsync(PlayerData data)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            string checkQuery = "SELECT COUNT(*) FROM speedometer_data WHERE steamid = @steamid";
            int count = await connection.ExecuteScalarAsync<int>(checkQuery, new { steamid = data.steamid });

            if (count > 0)
                await connection.ExecuteAsync("UPDATE speedometer_data SET hud_enabled=@hud_enabled, keys_enabled=@keys_enabled, jumps_enabled=@jumps_enabled, show_round_stats=@show_round_stats, color=@color WHERE steamid=@steamid", data);
            else
                await connection.ExecuteAsync("INSERT INTO speedometer_data (steamid, hud_enabled, keys_enabled, jumps_enabled, show_round_stats, color) VALUES (@steamid, @hud_enabled, @keys_enabled, @jumps_enabled, @show_round_stats, @color)", data);
        }
        catch { }
    }

    public static async Task<TopSpeedRecord?> GetPlayerMapRecordAsync(string steamId, string mapName)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            return await connection.QuerySingleOrDefaultAsync<TopSpeedRecord>(
                "SELECT * FROM topspeed_records WHERE steamid = @SteamId AND map_name = @MapName ORDER BY velocity DESC LIMIT 1",
                new { SteamId = steamId, MapName = mapName });
        }
        catch { return null; }
    }

    public static async Task SaveTopSpeedRecordAsync(TopSpeedRecord record)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            var existing = await GetPlayerMapRecordAsync(record.steamid, record.map_name);

            if (existing != null)
            {
                if (record.velocity > existing.velocity)
                {
                    await connection.ExecuteAsync(
                        "UPDATE topspeed_records SET velocity = @velocity, reach_time = @reach_time, player_name = @player_name, date_achieved = CURRENT_TIMESTAMP WHERE id = @id",
                        new { velocity = record.velocity, reach_time = record.reach_time, player_name = record.player_name, id = existing.id });
                }
            }
            else
            {
                await connection.ExecuteAsync(
                    "INSERT INTO topspeed_records (steamid, player_name, map_name, velocity, reach_time) VALUES (@steamid, @player_name, @map_name, @velocity, @reach_time)",
                    record);
            }
        }
        catch (Exception ex) { Console.WriteLine($"[Speedometer] DB Save Error: {ex.Message}"); }
    }
    
    public static async Task<List<TopSpeedRecord>> GetMapTopRecordsAsync(string mapName, int limit = 10)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            var result = await connection.QueryAsync<TopSpeedRecord>(
                "SELECT * FROM topspeed_records WHERE map_name = @MapName ORDER BY velocity DESC LIMIT @Limit",
                new { MapName = mapName, Limit = limit });
            return result.AsList();
        }
        catch { return new List<TopSpeedRecord>(); }
    }

    public static async Task<List<TopSpeedRecord>> GetOverallTopRecordsAsync(int limit = 10)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            var result = await connection.QueryAsync<TopSpeedRecord>(
                "SELECT * FROM topspeed_records ORDER BY velocity DESC LIMIT @Limit",
                new { Limit = limit });
            return result.AsList();
        }
        catch { return new List<TopSpeedRecord>(); }
    }

    public static async Task<List<TopSpeedRecord>> GetPlayerAllRecordsAsync(string steamId)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            var result = await connection.QueryAsync<TopSpeedRecord>(
                "SELECT * FROM topspeed_records WHERE steamid = @SteamId ORDER BY velocity DESC",
                new { SteamId = steamId });
            return result.AsList();
        }
        catch { return new List<TopSpeedRecord>(); }
    }

    public static async Task<List<(string MapName, int Count)>> GetMapsWithRecordCountsAsync()
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        var list = new List<(string, int)>();
        try
        {
            connection.Open();
            string query = @"
                SELECT map_name, COUNT(*) as record_count 
                FROM topspeed_records 
                WHERE map_name != 'unknown' AND map_name != 'unknown_map'
                GROUP BY map_name 
                ORDER BY record_count DESC";

            var result = await connection.QueryAsync(query);
            foreach (var row in result)
                list.Add(((string)row.map_name, Convert.ToInt32(row.record_count)));
        }
        catch (Exception ex) { Console.WriteLine($"[Speedometer] Map list error: {ex.Message}"); }
        return list;
    }

    public static async Task DeletePlayerRecordAsync(string steamId, string mapName)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            await connection.ExecuteAsync("DELETE FROM topspeed_records WHERE steamid = @SteamId AND map_name = @MapName", 
                new { SteamId = steamId, MapName = mapName });
        }
        catch {}
    }

    public static async Task DeleteAllRecordsOnMapAsync(string mapName)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            await connection.ExecuteAsync("DELETE FROM topspeed_records WHERE map_name = @MapName", new { MapName = mapName });
        }
        catch {}
    }

    public static async Task DeleteAllRecordsAsync()
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            await connection.ExecuteAsync("DELETE FROM topspeed_records");
        }
        catch {}
    }
    
    public static async Task<List<(string Name, string SteamID)>> SearchPlayersByNameAsync(string partialName)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        var list = new List<(string, string)>();
        try
        {
            connection.Open();
            string query = "SELECT DISTINCT player_name, steamid FROM topspeed_records WHERE player_name LIKE @query LIMIT 15";
            var result = await connection.QueryAsync(query, new { query = $"%{partialName}%" });
            foreach (var row in result) list.Add(((string)row.player_name, (string)row.steamid));
        }
        catch {}
        return list;
    }

    public static async Task<List<string>> GetPlayerMapsAsync(string steamid)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            var result = await connection.QueryAsync<string>("SELECT DISTINCT map_name FROM topspeed_records WHERE steamid = @sid ORDER BY map_name", new { sid = steamid });
            return result.AsList();
        }
        catch { return new List<string>(); }
    }

    public static async Task UpdatePlayerRecordValueAsync(string steamid, string map, int newVelocity, float newTime)
    {
        using var connection = Speedometer.Instance.SwiftlyCore.Database.GetConnection(Speedometer.Config.DatabaseConnection);
        try
        {
            connection.Open();
            await connection.ExecuteAsync("UPDATE topspeed_records SET velocity = @vel, reach_time = @time WHERE steamid = @sid AND map_name = @map",
                new { vel = newVelocity, time = newTime, sid = steamid, map = map });
        }
        catch {}
    }
}

public class TopSpeedRecord
{
    public int id { get; set; }
    public string steamid { get; set; } = "";
    public string player_name { get; set; } = "";
    public string map_name { get; set; } = "";
    public int velocity { get; set; }
    public float reach_time { get; set; }
    public DateTime date_achieved { get; set; }
}

public class PlayerData
{
    public string steamid { get; set; } = "";
    public int hud_enabled { get; set; }
    public int keys_enabled { get; set; }
    public int jumps_enabled { get; set; }
    public int show_round_stats { get; set; } = 1;
    public string color { get; set; } = "#00FF00";
}
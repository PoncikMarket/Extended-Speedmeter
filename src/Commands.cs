using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Core;
using SwiftlyS2.Shared.Translation;
using SwiftlyS2.Core.Extensions;

namespace Speedometer;

public partial class Speedometer
{
    private bool CheckCommandCooldown(IPlayer player)
    {
        if (Config.CommandCooldownSeconds <= 0) return true;

        int pid = player.PlayerID;
        var now = DateTime.Now;
        var diff = (now - _lastCommandTime[pid]).TotalSeconds;

        if (diff < Config.CommandCooldownSeconds)
        {
            var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
            string remaining = (Config.CommandCooldownSeconds - diff).ToString("F1");
            string prefix = Globals.ProcessColors(Config.Prefix);
            string msg = localizer["general.cooldown"] ?? "Please wait {0} seconds.";
            msg = msg.Replace("{0}", remaining);
            
            player.SendChat($"{prefix} {Globals.ProcessColors($"{{Red}}{msg}")}");
            return false;
        }

        _lastCommandTime[pid] = now;
        return true; 
    }

    [Command("speedmeter")]
    public void OnSpeedometerCommand(ICommandContext context) => HandleSpeedometerToggle(context);

    [Command("myspeed")]
    public void OnMySpeedCommand(ICommandContext context) => HandleSpeedometerToggle(context);

    private void HandleSpeedometerToggle(ICommandContext context)
    {
        if (context.Sender is not IPlayer player) return;
        if (!CheckCommandCooldown(player)) return;

        int pId = player.PlayerID;
        if (pId < 0 || pId >= 65) return;

        _isSpeedometerActive[pId] = !_isSpeedometerActive[pId];
        
        Task.Run(async () => {
            var data = new PlayerData {
                steamid = player.SteamID.ToString(),
                hud_enabled = _isSpeedometerActive[pId] ? 1 : 0,
                keys_enabled = _isKeyOverlayActive[pId] ? 1 : 0,
                jumps_enabled = _showJumps[pId] ? 1 : 0,
                show_round_stats = _showRoundStats[pId] ? 1 : 0,
                color = _playerColorChoices[pId] ?? DefaultColorHex
            };
            await DatabaseManager.SavePlayerAsync(data);
        });

        var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
        string statusKey = _isSpeedometerActive[pId] ? "menu.status.enabled" : "menu.status.disabled";
        string statusText = localizer[statusKey] ?? (_isSpeedometerActive[pId] ? "ENABLED" : "DISABLED"); 
        string colorCode = _isSpeedometerActive[pId] ? Helper.ChatColors.Green : Helper.ChatColors.Red;
        
        string prefix = Globals.ProcessColors(localizer["speedometer.prefix"] ?? Config.Prefix);
        string rawMsg = localizer["speedometer.toggle"] ?? "Speedometer is now {0}{1}{{Default}}.";
        string message = rawMsg.Replace("{0}", colorCode).Replace("{1}", statusText);
        
        _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(message)}"));
    }

    [Command("speedmeteredit")]
    public void OnEditCommand1(ICommandContext context) => OpenMainMenuSafe(context);
    [Command("myspeededit")]
    public void OnEditCommand2(ICommandContext context) => OpenMainMenuSafe(context);
    [Command("editspeedmeter")]
    public void OnEditCommand3(ICommandContext context) => OpenMainMenuSafe(context);

    private void OpenMainMenuSafe(ICommandContext context)
    {
        if (context.Sender is not IPlayer player) return;
        OpenMainMenu(player);
    }

    public void ShowOnlineRecords(IPlayer player)
    {
        var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
        string prefix = Globals.ProcessColors(localizer["speedometer.prefix"] ?? Config.Prefix);
        
        var onlinePlayers = Speedometer.Instance.SwiftlyCore.PlayerManager.GetAllPlayers()
            .Where(p => p != null && p.IsValid && !p.IsFakeClient)
            .ToList();

        var sessionList = onlinePlayers
            .Select(p => new { 
                Name = p.Controller?.PlayerName ?? "Unknown", 
                Speed = _playerDbMaxSpeed[p.PlayerID],
                Time = _playerDbReachTime[p.PlayerID]
            })
            .Where(x => x.Speed > 0)
            .OrderByDescending(x => x.Speed)
            .Take(10)
            .ToList();

        if (!sessionList.Any()) {
            string noRec = Globals.ProcessColors(localizer["topspeed.norecords"] ?? "{Red}No records found.");
            _chatQueue.Enqueue((player, $"{prefix} {noRec}"));
            return;
        }

        string header = localizer["topspeed.header.map"] ?? "--- Top Records on {0} ---";
        header = header.Replace("{0}", "Online Players");
        _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(header)}"));
        
        int rank = 1;
        foreach (var item in sessionList) {
            string speedStr = Globals.FormatSpeed(item.Speed);
            string rawLine = localizer["topspeed.list.entry"] ?? "{0}. {1}: {2} ({3}s)";
            string line = rawLine.Replace("{0}", rank.ToString())
                                 .Replace("{1}", item.Name)
                                 .Replace("{2}", speedStr)
                                 .Replace("{3}", item.Time.ToString("F2"));
                                 
            _chatQueue.Enqueue((player, Globals.ProcessColors(line)));
            rank++;
        }
    }

    public void ShowCurrentMapRecords(IPlayer player)
    {
        Task.Run(async () => {
            var records = await DatabaseManager.GetMapTopRecordsAsync(Speedometer.CurrentMapName, 10);
            var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
            string prefix = Globals.ProcessColors(localizer["speedometer.prefix"] ?? Config.Prefix);
            
            if (records.Count == 0) { 
                string noRec = Globals.ProcessColors(localizer["topspeed.norecords"] ?? "{Red}No records found.");
                _chatQueue.Enqueue((player, $"{prefix} {noRec}")); 
                return; 
            }
            
            string header = localizer["topspeed.header.map"] ?? "--- Top Records on {0} ---";
            header = header.Replace("{0}", Speedometer.CurrentMapName);
            _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(header)}"));
            
            int rank = 1;
            foreach (var r in records) {
                string speedStr = Globals.FormatSpeed(r.velocity);
                string rawLine = localizer["topspeed.list.entry"] ?? "{0}. {1}: {2} ({3}s)";
                string line = rawLine.Replace("{0}", rank.ToString())
                                     .Replace("{1}", r.player_name)
                                     .Replace("{2}", speedStr)
                                     .Replace("{3}", r.reach_time.ToString("F2"));
                _chatQueue.Enqueue((player, Globals.ProcessColors(line)));
                rank++;
            }
        });
    }

    public void ShowGlobalRecords(IPlayer player)
    {
        Task.Run(async () => {
            var records = await DatabaseManager.GetOverallTopRecordsAsync(10);
            var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
            string prefix = Globals.ProcessColors(localizer["speedometer.prefix"] ?? Config.Prefix);
            
            if (records.Count == 0) { 
                string noRec = Globals.ProcessColors(localizer["topspeed.norecords"] ?? "{Red}No records found.");
                _chatQueue.Enqueue((player, $"{prefix} {noRec}")); 
                return; 
            }
            
            string header = localizer["topspeed.header.top"] ?? "--- Global Records ---";
            _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(header)}"));
            
            int rank = 1;
            foreach (var r in records) {
                string speedStr = Globals.FormatSpeed(r.velocity);
                string entry = $"{rank}. {r.player_name} ({r.map_name}): {{Green}}{speedStr}{{Default}}";
                _chatQueue.Enqueue((player, Globals.ProcessColors(entry)));
                rank++;
            }
        });
    }

    public void ShowPersonalRecords(IPlayer player)
    {
        Task.Run(async () => {
            var records = await DatabaseManager.GetPlayerAllRecordsAsync(player.SteamID.ToString());
            var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
            string prefix = Globals.ProcessColors(localizer["speedometer.prefix"] ?? Config.Prefix);
            
            if (records.Count == 0) { 
                string noRec = Globals.ProcessColors(localizer["topspeed.norecords"] ?? "{Red}No records found.");
                _chatQueue.Enqueue((player, $"{prefix} {noRec}")); 
                return; 
            }
            
            string header = localizer["topspeed.header.pr"] ?? "--- Personal Records ---";
            _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(header)}"));
            
            foreach (var r in records) {
                string speedStr = Globals.FormatSpeed(r.velocity);
                string entry = $"- {r.map_name}: {{Green}}{speedStr}{{Default}} ({r.reach_time:F2}s)";
                _chatQueue.Enqueue((player, Globals.ProcessColors(entry)));
            }
        });
    }

    [Command("topspeed")] 
    public void OnCmdTopSpeed(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return; 
            ShowOnlineRecords(player); 
        }
    }

    [Command("topspeedmap")] 
    public void OnCmdTopSpeedMap(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return;
            ShowCurrentMapRecords(player); 
        }
    }

    [Command("topspeedtop")] 
    public void OnCmdTopSpeedTop(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return;
            ShowGlobalRecords(player); 
        }
    }

    [Command("topspeedpr")] 
    public void OnCmdTopSpeedPr(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return;
            ShowPersonalRecords(player); 
        }
    }

    [Command("topspeedmaplist")] 
    public void OnCmdTopSpeedMapList(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return;
            OpenMapListMenu(player); 
        }
    }

    [Command("topspeedhelp")]
    public void OnCmdTopSpeedHelp(ICommandContext context)
    {
        if (context.Sender is not IPlayer player) return;
        if(!CheckCommandCooldown(player)) return;

        string p = Globals.ProcessColors(Config.Prefix);
        player.SendChat($"{p} {Globals.ProcessColors("{Green}!topspeed {Default}- Online oyuncu rekorlari")}");
        player.SendChat($"{p} {Globals.ProcessColors("{Green}!topspeedmap {Default}- Bu haritadaki rekorlar")}");
        player.SendChat($"{p} {Globals.ProcessColors("{Green}!topspeedmaplist {Default}- Kayitli haritalar listesi")}");
        player.SendChat($"{p} {Globals.ProcessColors("{Green}!topspeedtop {Default}- Sunucu geneli rekorlar")}");
        player.SendChat($"{p} {Globals.ProcessColors("{Green}!topspeedpr {Default}- Kendi rekorlarin")}");
    }
    
    [Command("topspeedadmin")]
    public void OnCmdAdminMenu(ICommandContext context)
    {
        if (context.Sender is not IPlayer player) return;
        if (!Speedometer.Instance.SwiftlyCore.Permission.PlayerHasPermission(player.SteamID, Config.AdminFlag)) {
            string p = Globals.ProcessColors(Config.Prefix);
            string msg = Globals.ProcessColors("{Red}Bu komutu kullanmak icin yetkiniz yok!");
            player.SendChat($"{p} {msg}");
            return;
        }
        OpenAdminMenu(player);
    }

    [Command("topspeeddeletedifferent")]
    public void OnCmdTopSpeedDeleteDifferent(ICommandContext context)
    {
        if (context.Sender is not IPlayer player) return;
        if (!Speedometer.Instance.SwiftlyCore.Permission.PlayerHasPermission(player.SteamID, Config.AdminFlag)) {
            string p = Globals.ProcessColors(Config.Prefix);
            string msg = Globals.ProcessColors("{Red}Bu komutu kullanmak icin yetkiniz yok!");
            player.SendChat($"{p} {msg}");
            return;
        }
        OpenDeleteDifferentMapMenu(player);
    }
}
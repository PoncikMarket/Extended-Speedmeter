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
    // YENİ: Oyuncuların son komut kullanma zamanını tutar
    private DateTime[] _lastCommandTime = new DateTime[65];

    // YENİ: Cooldown Kontrol Fonksiyonu
    private bool CheckCommandCooldown(IPlayer player)
    {
        // Adminler cooldown'a takılmasın (İsteğe bağlı, şimdilik herkese uyguladım, istersen admin check ekleyebiliriz)
        if (Config.CommandCooldownSeconds <= 0) return true;

        int pid = player.PlayerID;
        var now = DateTime.Now;
        var diff = (now - _lastCommandTime[pid]).TotalSeconds;

        if (diff < Config.CommandCooldownSeconds)
        {
            var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
            string remaining = (Config.CommandCooldownSeconds - diff).ToString("F1");
            string prefix = Globals.ProcessColors(Config.Prefix);
            // "Lütfen bekleyiniz" mesajı (Translation dosyasına eklenecek)
            string msg = localizer["general.cooldown", remaining]; 
            // Eğer çeviri yoksa default bir mesaj gösterelim
            if (string.IsNullOrEmpty(msg) || msg == "general.cooldown") msg = $"Please wait {remaining}s.";
            
            player.SendChat($"{prefix} {Globals.ProcessColors($"{{Red}}{msg}")}");
            return false; // Engelle
        }

        _lastCommandTime[pid] = now; // Zamanı güncelle
        return true; // İzin ver
    }

    [Command("speedmeter")]
    public void OnSpeedometerCommand(ICommandContext context) => HandleSpeedometerToggle(context);

    [Command("myspeed")]
    public void OnMySpeedCommand(ICommandContext context) => HandleSpeedometerToggle(context);

    private void HandleSpeedometerToggle(ICommandContext context)
    {
        if (context.Sender is not IPlayer player) return;
        
        // Cooldown Kontrolü
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
        string statusText = localizer[statusKey]; 
        string colorCode = _isSpeedometerActive[pId] ? Helper.ChatColors.Green : Helper.ChatColors.Red;
        
        string prefix = Globals.ProcessColors(localizer["speedometer.prefix"]);
        string message = localizer["speedometer.toggle", colorCode, statusText];
        
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
        // Menü açmak için cooldown koymuyorum, oyuncu ayar yaparken rahatsız olmasın.
        OpenMainMenu(player);
    }

    // --- HELPER FUNCTIONS ---
    public void ShowOnlineRecords(IPlayer player)
    {
        var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
        string prefix = Globals.ProcessColors(localizer["speedometer.prefix"]);
        
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
            string noRec = Globals.ProcessColors(localizer["topspeed.norecords"]);
            _chatQueue.Enqueue((player, $"{prefix} {noRec}"));
            return;
        }

        _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.header.map", "Online Players"])}"));
        int rank = 1;
        foreach (var item in sessionList) {
            string speedStr = Globals.FormatSpeed(item.Speed);
            string line = localizer["topspeed.list.entry", rank, item.Name, speedStr, item.Time.ToString("F2")];
            _chatQueue.Enqueue((player, Globals.ProcessColors(line)));
            rank++;
        }
    }

    public void ShowCurrentMapRecords(IPlayer player)
    {
        Task.Run(async () => {
            var records = await DatabaseManager.GetMapTopRecordsAsync(Speedometer.CurrentMapName, 10);
            var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
            string prefix = Globals.ProcessColors(localizer["speedometer.prefix"]);
            if (records.Count == 0) { _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.norecords"])}")); return; }
            _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.header.map", Speedometer.CurrentMapName])}"));
            int rank = 1;
            foreach (var r in records) {
                string speedStr = Globals.FormatSpeed(r.velocity);
                string line = localizer["topspeed.list.entry", rank, r.player_name, speedStr, r.reach_time.ToString("F2")];
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
            string prefix = Globals.ProcessColors(localizer["speedometer.prefix"]);
            if (records.Count == 0) { _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.norecords"])}")); return; }
            _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.header.top"])}"));
            int rank = 1;
            foreach (var r in records) {
                string speedStr = Globals.FormatSpeed(r.velocity);
                string entry = $"{rank}. {r.player_name} ({r.map_name}): {Helper.ChatColors.Green}{speedStr}{Helper.ChatColors.Default}";
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
            string prefix = Globals.ProcessColors(localizer["speedometer.prefix"]);
            if (records.Count == 0) { _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.norecords"])}")); return; }
            _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.header.pr"])}"));
            int count = 0;
            foreach (var r in records) {
                if (count >= 5) break;
                string speedStr = Globals.FormatSpeed(r.velocity);
                string entry = $"- {r.map_name}: {Helper.ChatColors.Green}{speedStr}{Helper.ChatColors.Default} ({r.reach_time:F2}s)";
                _chatQueue.Enqueue((player, Globals.ProcessColors(entry)));
                count++;
            }
        });
    }

    // --- COMMAND HANDLERS ---

    [Command("topspeed")] 
    public void OnCmdTopSpeed(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return; // Cooldown
            ShowOnlineRecords(player); 
        }
    }

    [Command("topspeedmap")] 
    public void OnCmdTopSpeedMap(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return; // Cooldown
            ShowCurrentMapRecords(player); 
        }
    }

    [Command("topspeedtop")] 
    public void OnCmdTopSpeedTop(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return; // Cooldown
            ShowGlobalRecords(player); 
        }
    }

    [Command("topspeedpr")] 
    public void OnCmdTopSpeedPr(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return; // Cooldown
            ShowPersonalRecords(player); 
        }
    }

    [Command("topspeedmaplist")] 
    public void OnCmdTopSpeedMapList(ICommandContext context) 
    { 
        if (context.Sender is IPlayer player) 
        {
            if(!CheckCommandCooldown(player)) return; // Cooldown
            OpenMapListMenu(player); 
        }
    }

    [Command("topspeedhelp")]
    public void OnCmdTopSpeedHelp(ICommandContext context)
    {
        if (context.Sender is not IPlayer player) return;
        if(!CheckCommandCooldown(player)) return; // Cooldown

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
        // Admin menüsünde cooldown olmamalı
        if (!Speedometer.Instance.SwiftlyCore.Permission.PlayerHasPermission(player.SteamID, Config.AdminFlag)) {
            string p = Globals.ProcessColors(Config.Prefix);
            string msg = Globals.ProcessColors("{Red}Bu komutu kullanmak icin yetkiniz yok!");
            player.SendChat($"{p} {msg}");
            return;
        }
        OpenAdminMenu(player);
    }

    // YENİ: Başka haritadaki rekoru silme kısayolu
    [Command("topspeeddeletedifferent")]
    public void OnCmdTopSpeedDeleteDifferent(ICommandContext context)
    {
        if (context.Sender is not IPlayer player) return;
        // Admin komutu, cooldown yok
        if (!Speedometer.Instance.SwiftlyCore.Permission.PlayerHasPermission(player.SteamID, Config.AdminFlag)) {
            string p = Globals.ProcessColors(Config.Prefix);
            string msg = Globals.ProcessColors("{Red}Bu komutu kullanmak icin yetkiniz yok!");
            player.SendChat($"{p} {msg}");
            return;
        }
        OpenDeleteDifferentMapMenu(player);
    }
}
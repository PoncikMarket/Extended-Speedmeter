using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Core;
using SwiftlyS2.Shared.Translation;

namespace Speedometer;

public partial class Speedometer
{
    private ITranslationService Translation => Speedometer.Instance.SwiftlyCore.Translation;
    private IMenuManagerAPI Menus => Speedometer.Instance.SwiftlyCore.MenusAPI;

    private void ClosePlayerMenu(IPlayer player)
    {
        var builder = Menus.CreateBuilder();
        builder.Design.SetMenuTitle(" "); 
        Menus.OpenMenuForPlayer(player, builder.Build());
    }

    // YARDIMCI: Gradient metin oluşturucu
    private string Gradient(string text, string color1, string color2)
    {
        return HtmlGradient.GenerateGradientText(text, color1, color2);
    }

    public void OpenMainMenu(IPlayer player)
    {
        var builder = Menus.CreateBuilder();
        var localizer = Translation.GetPlayerLocalizer(player);

        // Başlık
        builder.Design.SetMenuTitle(Gradient(localizer["menu.title.main"], "#00FFFF", "#0000FF"));
        builder.Design.SetMaxVisibleItems(5);

        // HUD Ayarları
        string hudText = Gradient(localizer["menu.option.hud"], "#FFA500", "#FF4500");
        var btnHudOptions = new ButtonMenuOption(hudText) { MaxWidth = 100f }; // Genişlik arttırıldı
        btnHudOptions.Click += (sender, args) => { OpenHudOptionsMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnHudOptions);

        // TopSpeed Menüsü
        string tsText = Gradient(localizer["menu.option.topspeed"] ?? "TopSpeed Menu", "#00FF00", "#00FFFF");
        var btnTopSpeed = new ButtonMenuOption(tsText) { MaxWidth = 100f };
        btnTopSpeed.Click += (sender, args) => { OpenTopSpeedMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnTopSpeed);
        
        if (Speedometer.Instance.SwiftlyCore.Permission.PlayerHasPermission(player.SteamID, Config.AdminFlag))
        {
            // Admin
            string adminText = Gradient("ADMIN MENU", "#FF0000", "#FF00FF");
            var btnAdmin = new ButtonMenuOption(adminText) { MaxWidth = 100f };
            btnAdmin.Click += (sender, args) => { OpenAdminMenu(args.Player); return ValueTask.CompletedTask; };
            builder.AddOption(btnAdmin);
        }

        Menus.OpenMenuForPlayer(player, builder.Build());
    }

    public void OpenHudOptionsMenu(IPlayer player)
    {
        var builder = Menus.CreateBuilder();
        var localizer = Translation.GetPlayerLocalizer(player);
        builder.Design.SetMenuTitle(Gradient(localizer["menu.title.hud"], "#FFA500", "#FF4500"));
        builder.Design.SetMaxVisibleItems(5);

        // Renk Değiştir
        var btnColor = new ButtonMenuOption(Gradient(localizer["menu.option.speedcolor"], "#008000", "#00FF00")) { MaxWidth = 100f };
        btnColor.Click += (sender, args) => { OpenColorMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnColor);

        // --- Toggle Butonları ---
        string On = $"<font color='#00FF00'>{localizer["menu.status.enabled"]}</font>";
        string Off = $"<font color='#FF0000'>{localizer["menu.status.disabled"]}</font>";

        // Round Stats
        bool isRoundStatsOn = _showRoundStats[player.PlayerID];
        string roundText = Gradient("Round Stats", "#FFFF00", "#FFA500");
        var btnRoundStats = new ButtonMenuOption($"{roundText} {(isRoundStatsOn ? On : Off)}") { MaxWidth = 100f }; // Genişlik arttırıldı
        btnRoundStats.Click += (sender, args) => { ToggleSetting(args.Player, "round"); return ValueTask.CompletedTask; };
        builder.AddOption(btnRoundStats);

        // Key Overlay
        bool isOverlayOn = _isKeyOverlayActive[player.PlayerID];
        string keyText = Gradient(localizer["menu.option.keyoverlay"], "#FFFF00", "#FFA500");
        var btnOverlay = new ButtonMenuOption($"{keyText} {(isOverlayOn ? On : Off)}") { MaxWidth = 100f }; // Genişlik arttırıldı
        btnOverlay.Click += (sender, args) => { ToggleSetting(args.Player, "key"); return ValueTask.CompletedTask; };
        builder.AddOption(btnOverlay);

        // Jumps
        bool isJumpsOn = _showJumps[player.PlayerID];
        string jumpText = Gradient(localizer["menu.option.jumpsoverlay"], "#FFFF00", "#FFA500");
        var btnJumps = new ButtonMenuOption($"{jumpText} {(isJumpsOn ? On : Off)}") { MaxWidth = 100f }; // Genişlik arttırıldı
        btnJumps.Click += (sender, args) => { ToggleSetting(args.Player, "jump"); return ValueTask.CompletedTask; };
        builder.AddOption(btnJumps);

        // Back
        var btnBack = new ButtonMenuOption(Gradient(localizer["menu.back"], "#FF0000", "#8B0000")) { MaxWidth = 100f };
        btnBack.Click += (sender, args) => { OpenMainMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnBack);

        Menus.OpenMenuForPlayer(player, builder.Build());
    }

    public void OpenTopSpeedMenu(IPlayer player)
    {
        var builder = Menus.CreateBuilder();
        var localizer = Translation.GetPlayerLocalizer(player);
        builder.Design.SetMenuTitle(Gradient(localizer["menu.title.topspeed"] ?? "TopSpeed", "#00FF00", "#00FFFF"));

        // Online
        var btnOnline = new ButtonMenuOption(Gradient(localizer["menu.topspeed.online"] ?? "Online Players", "#00BFFF", "#1E90FF")) { MaxWidth = 100f };
        btnOnline.Click += (sender, args) => { ShowOnlineRecords(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnOnline);

        // Current
        var btnCurrent = new ButtonMenuOption(Gradient(localizer["menu.topspeed.current"] ?? "Current Map", "#90EE90", "#006400")) { MaxWidth = 100f };
        btnCurrent.Click += (sender, args) => { ShowCurrentMapRecords(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnCurrent);

        // Map List
        var btnMapList = new ButtonMenuOption(Gradient(localizer["menu.option.maplist"] ?? "Map List", "#00FFFF", "#008B8B")) { MaxWidth = 100f };
        btnMapList.Click += (sender, args) => { OpenMapListMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnMapList);

        // Global
        var btnGlobal = new ButtonMenuOption(Gradient(localizer["menu.topspeed.global"] ?? "Global Top", "#EE82EE", "#800080")) { MaxWidth = 100f };
        btnGlobal.Click += (sender, args) => { ShowGlobalRecords(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnGlobal);

        // PR
        var btnPr = new ButtonMenuOption(Gradient(localizer["menu.topspeed.personal"] ?? "Personal Record", "#FFD700", "#DAA520")) { MaxWidth = 100f };
        btnPr.Click += (sender, args) => { ShowPersonalRecords(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnPr);

        var btnBack = new ButtonMenuOption(Gradient(localizer["menu.back"], "#FF0000", "#8B0000")) { MaxWidth = 100f };
        btnBack.Click += (sender, args) => { OpenMainMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnBack);

        Menus.OpenMenuForPlayer(player, builder.Build());
    }

    public void OpenMapListMenu(IPlayer player)
    {
        Task.Run(async () => {
            var maps = await DatabaseManager.GetMapsWithRecordCountsAsync();
            var builder = Menus.CreateBuilder();
            builder.Design.SetMenuTitle(Gradient("Map List", "#FFA500", "#FFFF00"));
            
            foreach (var item in maps) {
                // Harita İsimleri (Uzun olabilir)
                string mapNameColored = Gradient($"{item.MapName} ({item.Count})", "#00FFFF", "#0080FF");
                var btnMap = new ButtonMenuOption(mapNameColored) { MaxWidth = 100f }; // Genişlik arttırıldı
                btnMap.Click += (sender, args) => { PrintRecordsForSpecificMap(args.Player, item.MapName); return ValueTask.CompletedTask; };
                builder.AddOption(btnMap);
            }
            var btnBack = new ButtonMenuOption(Gradient("Back", "#FF0000", "#8B0000")) { MaxWidth = 100f };
            btnBack.Click += (sender, args) => { OpenTopSpeedMenu(args.Player); return ValueTask.CompletedTask; };
            builder.AddOption(btnBack);
            Menus.OpenMenuForPlayer(player, builder.Build());
        });
    }
    
    private void PrintRecordsForSpecificMap(IPlayer player, string mapName)
    {
        Task.Run(async () => {
            var records = await DatabaseManager.GetMapTopRecordsAsync(mapName, 10);
            var localizer = Translation.GetPlayerLocalizer(player);
            string prefix = Globals.ProcessColors(Speedometer.Config.Prefix);
            if (records.Count == 0) { _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.norecords"])}")); return; }
            _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.header.map", mapName])}"));
            int rank = 1;
            foreach (var r in records) {
                string speedStr = Globals.FormatSpeed(r.velocity);
                string line = localizer["topspeed.list.entry", rank, r.player_name, speedStr, r.reach_time.ToString("F2")];
                _chatQueue.Enqueue((player, Globals.ProcessColors(line)));
                rank++;
            }
        });
    }

    public void OpenAdminMenu(IPlayer player)
    {
        var builder = Menus.CreateBuilder();
        var localizer = Translation.GetPlayerLocalizer(player);
        builder.Design.SetMenuTitle(Gradient(localizer["menu.admin.title"] ?? "TopSpeed Admin", "#FF0000", "#800000"));
        
        // Reset Map
        string resetMapRaw = localizer["menu.admin.reset_map"] ?? "Reset Records (This Map)";
        string resetMapColored = Gradient(resetMapRaw, "#00FF00", "#006400");
        var btnResetMap = new ButtonMenuOption(resetMapColored) { MaxWidth = 100f };
        btnResetMap.Click += (sender, args) => {
            Task.Run(async () => { await DatabaseManager.DeleteAllRecordsOnMapAsync(Speedometer.CurrentMapName); });
            for (int i = 0; i < 65; i++) _playerDbMaxSpeed[i] = 0;
            _chatQueue.Enqueue((args.Player, $"{Globals.ProcessColors(Config.Prefix)} {Helper.ChatColors.Red}{localizer["topspeed.admin.deleted"] ?? "Deleted!"}"));
            return ValueTask.CompletedTask;
        };
        builder.AddOption(btnResetMap);

        // Delete Diff
        string deleteDiffRaw = localizer["menu.admin.delete_diff"] ?? "Delete Record (Select Map)";
        string deleteDiffColored = Gradient(deleteDiffRaw, "#00FFFF", "#0000FF");
        var btnDeleteDiff = new ButtonMenuOption(deleteDiffColored) { MaxWidth = 100f };
        btnDeleteDiff.Click += (sender, args) => { OpenDeleteDifferentMapMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnDeleteDiff);

        // Manage Player
        var btnManagePlayer = new ButtonMenuOption(Gradient(localizer["menu.admin.manage_player"] ?? "Manage Player", "#FFFF00", "#FFA500")) { MaxWidth = 100f };
        btnManagePlayer.Click += (sender, args) => { 
            Globals.AdminEditStates[args.Player.PlayerID] = new Globals.AdminEditState { ActionType = "SEARCH_PLAYER" };
            string prefix = Globals.ProcessColors(Config.Prefix);
            _chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(localizer["chat.admin.search_prompt"])}"));
            ClosePlayerMenu(args.Player);
            return ValueTask.CompletedTask; 
        };
        builder.AddOption(btnManagePlayer);

        // Reset All
        string resetAllRaw = localizer["menu.admin.reset_all"] ?? "Reset ALL Records";
        string resetAllColored = Gradient(resetAllRaw, "#FF0000", "#8B0000");
        var btnResetAll = new ButtonMenuOption(resetAllColored) { MaxWidth = 100f };
        btnResetAll.Click += (sender, args) => {
            Task.Run(async () => { await DatabaseManager.DeleteAllRecordsAsync(); });
            for (int i = 0; i < 65; i++) _playerDbMaxSpeed[i] = 0;
            _chatQueue.Enqueue((args.Player, $"{Globals.ProcessColors(Config.Prefix)} {Helper.ChatColors.Red}{localizer["topspeed.admin.resetall"] ?? "All reset!"}"));
            return ValueTask.CompletedTask;
        };
        builder.AddOption(btnResetAll);

        var btnBack = new ButtonMenuOption(Gradient(localizer["menu.back"], "#FF0000", "#8B0000")) { MaxWidth = 100f };
        btnBack.Click += (sender, args) => { OpenMainMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnBack);

        Menus.OpenMenuForPlayer(player, builder.Build());
    }

    public void OpenDeleteDifferentMapMenu(IPlayer player)
    {
        Task.Run(async () => {
            var maps = await DatabaseManager.GetMapsWithRecordCountsAsync();
            var builder = Menus.CreateBuilder();
            var localizer = Translation.GetPlayerLocalizer(player);
            builder.Design.SetMenuTitle(localizer["menu.admin.select_map_clean"] ?? "Select Map");
            
            foreach (var item in maps) {
                string mapText = Gradient($"{item.MapName} ({item.Count})", "#00FFFF", "#0080FF");
                var btnMap = new ButtonMenuOption(mapText) { MaxWidth = 100f }; // Genişlik arttırıldı
                btnMap.Click += (sender, args) => { OpenDeleteRecordsForMapMenu(args.Player, item.MapName); return ValueTask.CompletedTask; };
                builder.AddOption(btnMap);
            }
            var btnBack = new ButtonMenuOption(Gradient(localizer["menu.back"], "#FF0000", "#8B0000")) { MaxWidth = 100f };
            btnBack.Click += (sender, args) => { OpenAdminMenu(args.Player); return ValueTask.CompletedTask; };
            builder.AddOption(btnBack);
            Menus.OpenMenuForPlayer(player, builder.Build());
        });
    }

    public void OpenDeleteRecordsForMapMenu(IPlayer player, string mapName)
    {
        Task.Run(async () => {
            var records = await DatabaseManager.GetMapTopRecordsAsync(mapName, 50); 
            var builder = Menus.CreateBuilder();
            var localizer = Translation.GetPlayerLocalizer(player);
            builder.Design.SetMenuTitle(localizer["menu.admin.delete_header", mapName]);
            
            if (records.Count == 0) builder.AddOption(new ButtonMenuOption(localizer["menu.admin.no_records"] ?? "No records."));
            else {
                foreach (var rec in records) {
                    // Kayıtlar uzun olabilir, MaxWidth burada çok önemli
                    string recordText = Gradient($"{rec.player_name} ({rec.velocity} u/s - {rec.reach_time:F2}s)", "#FFFFFF", "#AAAAAA");
                    var btnDel = new ButtonMenuOption(recordText) { MaxWidth = 100f }; // Genişlik arttırıldı
                    btnDel.Click += (sender, args) => {
                        Task.Run(async () => {
                            await DatabaseManager.DeletePlayerRecordAsync(rec.steamid, mapName);
                            string prefix = Globals.ProcessColors(Config.Prefix);
                            _chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(localizer["chat.admin.record_deleted", rec.player_name, mapName])}"));
                            OpenDeleteRecordsForMapMenu(args.Player, mapName);
                        });
                        return ValueTask.CompletedTask;
                    };
                    builder.AddOption(btnDel);
                }
            }
            var btnBack = new ButtonMenuOption(Gradient(localizer["menu.back"], "#FF0000", "#8B0000")) { MaxWidth = 100f };
            btnBack.Click += (sender, args) => { OpenDeleteDifferentMapMenu(args.Player); return ValueTask.CompletedTask; };
            builder.AddOption(btnBack);
            Menus.OpenMenuForPlayer(player, builder.Build());
        });
    }

    public void HandleAdminChatInput(IPlayer player, string input, Globals.AdminEditState state)
    {
        int pid = player.PlayerID;
        var localizer = Translation.GetPlayerLocalizer(player);
        string prefix = Globals.ProcessColors(Config.Prefix);
        
        if (state.ActionType == "SEARCH_PLAYER")
        {
            Task.Run(async () => {
                var results = await DatabaseManager.SearchPlayersByNameAsync(input);
                Globals.AdminEditStates.TryRemove(pid, out _); 
                if (results.Count == 0) {
                    _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["chat.admin.player_not_found"])}"));
                    return;
                }
                OpenAdminPlayerSelectMenu(player, results);
            });
        }
        else if (state.ActionType == "EDIT_SPEED")
        {
            Globals.AdminEditStates.TryRemove(pid, out _);
            if (int.TryParse(input, out int newSpeed) && newSpeed >= 0)
            {
                Task.Run(async () => {
                    var rec = await DatabaseManager.GetPlayerMapRecordAsync(state.TargetSteamID, state.TargetMap);
                    if (rec != null) {
                        await DatabaseManager.UpdatePlayerRecordValueAsync(state.TargetSteamID, state.TargetMap, newSpeed, rec.reach_time);
                        _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["chat.admin.speed_updated", rec.player_name, state.TargetMap, newSpeed])}"));
                    }
                    OpenAdminRecordEditMenu(player, state.TargetSteamID, rec?.player_name ?? "Unknown", state.TargetMap);
                });
            }
            else {
                _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["chat.admin.invalid_speed"])}"));
                Task.Run(async () => {
                    var rec = await DatabaseManager.GetPlayerMapRecordAsync(state.TargetSteamID, state.TargetMap);
                    OpenAdminRecordEditMenu(player, state.TargetSteamID, rec?.player_name ?? "Unknown", state.TargetMap);
                });
            }
        }
        else if (state.ActionType == "EDIT_TIME")
        {
            Globals.AdminEditStates.TryRemove(pid, out _);
            if (float.TryParse(input.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float newTime) && newTime >= 0)
            {
                Task.Run(async () => {
                    var rec = await DatabaseManager.GetPlayerMapRecordAsync(state.TargetSteamID, state.TargetMap);
                    if (rec != null) {
                        await DatabaseManager.UpdatePlayerRecordValueAsync(state.TargetSteamID, state.TargetMap, rec.velocity, newTime);
                        _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["chat.admin.time_updated", rec.player_name, state.TargetMap, newTime.ToString("F2")])}"));
                    }
                    OpenAdminRecordEditMenu(player, state.TargetSteamID, rec?.player_name ?? "Unknown", state.TargetMap);
                });
            }
            else {
                _chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(localizer["chat.admin.invalid_time"])}"));
                Task.Run(async () => {
                    var rec = await DatabaseManager.GetPlayerMapRecordAsync(state.TargetSteamID, state.TargetMap);
                    OpenAdminRecordEditMenu(player, state.TargetSteamID, rec?.player_name ?? "Unknown", state.TargetMap);
                });
            }
        }
    }

    public void OpenAdminPlayerSelectMenu(IPlayer player, List<(string Name, string SteamID)> players)
    {
        var builder = Menus.CreateBuilder();
        var localizer = Translation.GetPlayerLocalizer(player);
        builder.Design.SetMenuTitle(localizer["menu.admin.select_player"] ?? "Select Player");
        foreach (var p in players) {
            var btn = new ButtonMenuOption(p.Name) { MaxWidth = 100f }; // Genişlik arttırıldı
            btn.Click += (sender, args) => { OpenAdminPlayerMapSelectMenu(args.Player, p.SteamID, p.Name); return ValueTask.CompletedTask; };
            builder.AddOption(btn);
        }
        var btnBack = new ButtonMenuOption(Gradient(localizer["menu.admin.back_admin"] ?? "Back to Admin", "#FF0000", "#8B0000")) { MaxWidth = 100f };
        btnBack.Click += (sender, args) => { OpenAdminMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnBack);
        Menus.OpenMenuForPlayer(player, builder.Build());
    }

    public void OpenAdminPlayerMapSelectMenu(IPlayer player, string targetSteamID, string targetName)
    {
        Task.Run(async () => {
            var maps = await DatabaseManager.GetPlayerMapsAsync(targetSteamID);
            var builder = Menus.CreateBuilder();
            var localizer = Translation.GetPlayerLocalizer(player);
            builder.Design.SetMenuTitle(localizer["menu.admin.select_map_player", targetName]);
            if (maps.Count == 0) builder.AddOption(new ButtonMenuOption(localizer["menu.admin.no_records"] ?? "No records."));
            foreach (var map in maps) {
                var btn = new ButtonMenuOption(Gradient(map, "#00FFFF", "#0000FF")) { MaxWidth = 100f }; // Genişlik arttırıldı
                btn.Click += (sender, args) => { OpenAdminRecordEditMenu(args.Player, targetSteamID, targetName, map); return ValueTask.CompletedTask; };
                builder.AddOption(btn);
            }
            var btnBack = new ButtonMenuOption(Gradient(localizer["menu.admin.back_search"] ?? "Back to Search", "#FF0000", "#8B0000")) { MaxWidth = 100f };
            btnBack.Click += (sender, args) => { OpenAdminMenu(args.Player); return ValueTask.CompletedTask; };
            builder.AddOption(btnBack);
            Menus.OpenMenuForPlayer(player, builder.Build());
        });
    }

    public void OpenAdminRecordEditMenu(IPlayer player, string steamID, string name, string map)
    {
        Task.Run(async () => {
            var rec = await DatabaseManager.GetPlayerMapRecordAsync(steamID, map);
            var builder = Menus.CreateBuilder();
            var localizer = Translation.GetPlayerLocalizer(player);
            builder.Design.SetMenuTitle($"{name} - {map}");
            
            if (rec == null) { builder.AddOption(new ButtonMenuOption(localizer["menu.admin.deleted_record"] ?? "Deleted.")); }
            else {
                builder.AddOption(new ButtonMenuOption($"Speed: {rec.velocity} | Time: {rec.reach_time:F2}") { MaxWidth = 100f });
                
                var btnDel = new ButtonMenuOption(Globals.ProcessColors(HtmlGradient.GenerateGradientText(localizer["menu.admin.btn_delete"] ?? "DELETE", "#FF0000", "#8B0000"))) { MaxWidth = 100f };
                btnDel.Click += (sender, args) => {
                    Task.Run(async () => {
                        await DatabaseManager.DeletePlayerRecordAsync(steamID, map);
                        string prefix = Globals.ProcessColors(Config.Prefix);
                        _chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(localizer["topspeed.admin.deleted"] ?? "Deleted!")}"));
                        ClosePlayerMenu(args.Player);
                    });
                    return ValueTask.CompletedTask;
                };
                builder.AddOption(btnDel);

                var btnEditSpeed = new ButtonMenuOption(Globals.ProcessColors($"{{Green}}{localizer["menu.admin.btn_edit_speed"] ?? "Edit Speed"}")) { MaxWidth = 100f };
                btnEditSpeed.Click += (sender, args) => {
                    Globals.AdminEditStates[args.Player.PlayerID] = new Globals.AdminEditState { ActionType = "EDIT_SPEED", TargetSteamID = steamID, TargetMap = map };
                    string prefix = Globals.ProcessColors(Config.Prefix);
                    _chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(localizer["chat.admin.enter_speed"])}"));
                    ClosePlayerMenu(args.Player);
                    return ValueTask.CompletedTask;
                };
                builder.AddOption(btnEditSpeed);

                var btnEditTime = new ButtonMenuOption(Globals.ProcessColors($"{{Cyan}}{localizer["menu.admin.btn_edit_time"] ?? "Edit Time"}")) { MaxWidth = 100f };
                btnEditTime.Click += (sender, args) => {
                    Globals.AdminEditStates[args.Player.PlayerID] = new Globals.AdminEditState { ActionType = "EDIT_TIME", TargetSteamID = steamID, TargetMap = map };
                    string prefix = Globals.ProcessColors(Config.Prefix);
                    _chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(localizer["chat.admin.enter_time"])}"));
                    ClosePlayerMenu(args.Player);
                    return ValueTask.CompletedTask;
                };
                builder.AddOption(btnEditTime);
            }
            var btnBack = new ButtonMenuOption(Globals.ProcessColors($"{{Red}}{localizer["menu.admin.back_maplist"] ?? "Back"}")) { MaxWidth = 100f };
            btnBack.Click += (sender, args) => { OpenAdminPlayerMapSelectMenu(args.Player, steamID, name); return ValueTask.CompletedTask; };
            builder.AddOption(btnBack);
            Menus.OpenMenuForPlayer(player, builder.Build());
        });
    }

    private void ToggleSetting(IPlayer player, string type)
    {
        int pid = player.PlayerID;
        bool newState = false;
        string msgKey = "";
        
        if (type == "key") 
        { 
            _isKeyOverlayActive[pid] = !_isKeyOverlayActive[pid]; 
            newState = _isKeyOverlayActive[pid]; 
            msgKey = "speedometer.keyoverlay"; 
        }
        else if (type == "jump") 
        { 
            _showJumps[pid] = !_showJumps[pid]; 
            newState = _showJumps[pid]; 
            msgKey = "speedometer.jumpsoverlay"; 
        }
        else if (type == "round") 
        {
            _showRoundStats[pid] = !_showRoundStats[pid];
            newState = _showRoundStats[pid];
            msgKey = "menu.option.roundstats"; 
        }

        SavePlayerData(player);
        var localizer = Translation.GetPlayerLocalizer(player);
        string statusKey = newState ? "menu.status.enabled" : "menu.status.disabled";
        string colorCode = newState ? Helper.ChatColors.Green : Helper.ChatColors.Red;
        
        string statusMsg = localizer[msgKey];
        if (string.IsNullOrEmpty(statusMsg) && type == "round") statusMsg = "Round Stats";

        _chatQueue.Enqueue((player, $"{Globals.ProcessColors(localizer["speedometer.prefix"])} {Globals.ProcessColors(localizer["speedometer.toggle", colorCode, localizer[statusKey]])}")); 
        OpenHudOptionsMenu(player);
    }

    public void OpenColorMenu(IPlayer player)
    {
        var builder = Menus.CreateBuilder();
        var localizer = Translation.GetPlayerLocalizer(player);
        
        builder.Design.SetMenuTitle(localizer["menu.title.color"] ?? "Select Color");
        builder.Design.SetMaxVisibleItems(5);
        
        string rainbowRaw = localizer["menu.rainbow"] ?? "Rainbow";
        string rainbowColored = HtmlGradient.GenerateGradientText(rainbowRaw, "#FF0000", "#FFA500", "#FFFF00", "#00FF00", "#00FFFF", "#0000FF", "#8B00FF");
        var btnRainbow = new ButtonMenuOption(rainbowColored) { MaxWidth = 100f }; // Genişlik arttırıldı
        
        btnRainbow.Click += (sender, args) => { SetColor(args.Player, "RAINBOW", "Rainbow", Helper.ChatColors.LightBlue); return ValueTask.CompletedTask; };
        builder.AddOption(btnRainbow);
        
        foreach (var color in AvailableColors) {
            if (color.Key == "Rainbow") continue;
            var button = new ButtonMenuOption($"<font color='{color.Value}'>{color.Key}</font>") { MaxWidth = 100f }; // Genişlik arttırıldı
            button.Click += (sender, args) => { SetColor(args.Player, color.Value, color.Key, Helper.ChatColors.Green); return ValueTask.CompletedTask; };
            builder.AddOption(button);
        }
        var btnBack = new ButtonMenuOption(Globals.ProcessColors($"{{Red}}{localizer["menu.back"]}")) { MaxWidth = 100f };
        btnBack.Click += (sender, args) => { OpenHudOptionsMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnBack);
        Menus.OpenMenuForPlayer(player, builder.Build());
    }

    private void SetColor(IPlayer player, string hexCode, string displayName, string chatColor)
    {
        int pid = player.PlayerID;
        _playerColorChoices[pid] = hexCode;
        SavePlayerData(player);
        var localizer = Translation.GetPlayerLocalizer(player);
        _chatQueue.Enqueue((player, $"{Globals.ProcessColors(localizer["speedometer.prefix"])} {Globals.ProcessColors(localizer["speedometer.colorset", chatColor, displayName])}"));
        OpenHudOptionsMenu(player);
    }

    private void SavePlayerData(IPlayer player)
    {
        int pid = player.PlayerID;
        Task.Run(async () => {
            var data = new PlayerData { 
                steamid = player.SteamID.ToString(), 
                hud_enabled = _isSpeedometerActive[pid] ? 1 : 0, 
                keys_enabled = _isKeyOverlayActive[pid] ? 1 : 0, 
                jumps_enabled = _showJumps[pid] ? 1 : 0, 
                show_round_stats = _showRoundStats[pid] ? 1 : 0,
                color = _playerColorChoices[pid] ?? DefaultColorHex 
            };
            await DatabaseManager.SavePlayerAsync(data);
        });
    }
}
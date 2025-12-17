using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Core;
using SwiftlyS2.Shared.Translation;
using System.Linq;

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

    private string Gradient(string text, string color1, string color2)
    {
        return HtmlGradient.GenerateGradientText(text, color1, color2);
    }

    private string SafeFormat(string text, params object[] args)
    {
        if (string.IsNullOrEmpty(text)) return "";
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                text = text.Replace($"{{{i}}}", args[i]?.ToString() ?? "");
            }
            return text;
        }
        catch { return text; }
    }

    private string GetText(IPlayer p, string key, params object[] args)
    {
        var localizer = Translation.GetPlayerLocalizer(p);
        string raw = localizer[key] ?? key; 
        return SafeFormat(raw, args);
    }

    public void OpenMainMenu(IPlayer player)
    {
        var builder = Menus.CreateBuilder();
        var localizer = Translation.GetPlayerLocalizer(player);

        builder.Design.SetMenuTitle(Gradient(localizer["menu.title.main"], "#00FFFF", "#0000FF"));
        builder.Design.SetMaxVisibleItems(5);

        string hudText = Gradient(localizer["menu.option.hud"], "#FFA500", "#FF4500");
        var btnHudOptions = new ButtonMenuOption(hudText) { MaxWidth = 100f };
        btnHudOptions.Click += (sender, args) => { OpenHudOptionsMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnHudOptions);

        string tsText = Gradient(localizer["menu.option.topspeed"] ?? "TopSpeed Menu", "#00FF00", "#00FFFF");
        var btnTopSpeed = new ButtonMenuOption(tsText) { MaxWidth = 100f };
        btnTopSpeed.Click += (sender, args) => { OpenTopSpeedMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnTopSpeed);
        
        if (Speedometer.Instance.SwiftlyCore.Permission.PlayerHasPermission(player.SteamID, Config.AdminFlag))
        {
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

        var btnColor = new ButtonMenuOption(Gradient(localizer["menu.option.speedcolor"], "#008000", "#00FF00")) { MaxWidth = 100f };
        btnColor.Click += (sender, args) => { OpenColorMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnColor);

        string IconOn = "<font color='#00FF00'>[ ✔ ]</font>";
        string IconOff = "<font color='#FF0000'>[ ✖ ]</font>";

        bool isRoundStatsOn = _showRoundStats[player.PlayerID];
        string roundText = Gradient("Round Stats", "#FFFF00", "#FFA500");
        var btnRoundStats = new ButtonMenuOption($"{roundText} {(isRoundStatsOn ? IconOn : IconOff)}") { MaxWidth = 100f }; 
        btnRoundStats.Click += (sender, args) => { ToggleSetting(args.Player, "round"); return ValueTask.CompletedTask; };
        builder.AddOption(btnRoundStats);

        bool isOverlayOn = _isKeyOverlayActive[player.PlayerID];
        string keyText = Gradient(localizer["menu.option.keyoverlay"], "#FFFF00", "#FFA500");
        var btnOverlay = new ButtonMenuOption($"{keyText} {(isOverlayOn ? IconOn : IconOff)}") { MaxWidth = 100f }; 
        btnOverlay.Click += (sender, args) => { ToggleSetting(args.Player, "key"); return ValueTask.CompletedTask; };
        builder.AddOption(btnOverlay);

        bool isJumpsOn = _showJumps[player.PlayerID];
        string jumpText = Gradient(localizer["menu.option.jumpsoverlay"], "#FFFF00", "#FFA500");
        var btnJumps = new ButtonMenuOption($"{jumpText} {(isJumpsOn ? IconOn : IconOff)}") { MaxWidth = 100f }; 
        btnJumps.Click += (sender, args) => { ToggleSetting(args.Player, "jump"); return ValueTask.CompletedTask; };
        builder.AddOption(btnJumps);

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

        var btnOnline = new ButtonMenuOption(Gradient(localizer["menu.topspeed.online"] ?? "Online Players", "#00BFFF", "#1E90FF")) { MaxWidth = 100f };
        btnOnline.Click += (sender, args) => { ShowOnlineRecords(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnOnline);

        var btnCurrent = new ButtonMenuOption(Gradient(localizer["menu.topspeed.current"] ?? "Current Map", "#90EE90", "#006400")) { MaxWidth = 100f };
        btnCurrent.Click += (sender, args) => { ShowCurrentMapRecords(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnCurrent);

        var btnMapList = new ButtonMenuOption(Gradient(localizer["menu.option.maplist"] ?? "Map List", "#00FFFF", "#008B8B")) { MaxWidth = 100f };
        btnMapList.Click += (sender, args) => { OpenMapListMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnMapList);

        var btnGlobal = new ButtonMenuOption(Gradient(localizer["menu.topspeed.global"] ?? "Global Top", "#EE82EE", "#800080")) { MaxWidth = 100f };
        btnGlobal.Click += (sender, args) => { ShowGlobalRecords(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnGlobal);

        var btnPr = new ButtonMenuOption(Gradient(localizer["menu.topspeed.personal"] ?? "Personal Record", "#FFD700", "#DAA520")) { MaxWidth = 100f };
        btnPr.Click += (sender, args) => { OpenPersonalRecordsMenu(args.Player); return ValueTask.CompletedTask; };
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
                string mapNameColored = Gradient($"{item.MapName} ({item.Count})", "#00FFFF", "#0080FF");
                var btnMap = new ButtonMenuOption(mapNameColored) { MaxWidth = 100f };
                btnMap.Click += (sender, args) => { OpenMapRecordsMenu(args.Player, item.MapName); return ValueTask.CompletedTask; };
                builder.AddOption(btnMap);
            }
            var btnBack = new ButtonMenuOption(Gradient("Back", "#FF0000", "#8B0000")) { MaxWidth = 100f };
            btnBack.Click += (sender, args) => { OpenTopSpeedMenu(args.Player); return ValueTask.CompletedTask; };
            builder.AddOption(btnBack);
            Menus.OpenMenuForPlayer(player, builder.Build());
        });
    }
    
    public void OpenMapRecordsMenu(IPlayer player, string mapName)
    {
        Task.Run(async () => {
            var records = await DatabaseManager.GetMapTopRecordsAsync(mapName, 50);
            var builder = Menus.CreateBuilder();
            builder.Design.SetMenuTitle(Gradient($"{mapName} Records", "#FFA500", "#FFFF00"));
            
            if (records.Count == 0) 
            {
                builder.AddOption(new ButtonMenuOption("No records") { MaxWidth = 100f, Enabled = false });
            } 
            else 
            {
                int rank = 1;
                foreach (var rec in records) {
                    string color = rank <= 3 ? "#FFD700" : "#FFFFFF";
                    string text = Gradient($"{rank}. {rec.player_name} - {Globals.FormatSpeed(rec.velocity)}", color, color);
                    
                    var btn = new ButtonMenuOption(text) { MaxWidth = 100f };
                    var currentRecord = rec;
                    var currentRank = rank;
                    btn.Click += (s, a) => { OpenRecordDetailsMenu(a.Player, currentRecord, currentRank); return ValueTask.CompletedTask; };
                    builder.AddOption(btn);
                    rank++;
                }
            }
            
            var btnBack = new ButtonMenuOption(Gradient("Back", "#FF0000", "#8B0000")) { MaxWidth = 100f };
            btnBack.Click += (s, a) => { OpenMapListMenu(a.Player); return ValueTask.CompletedTask; };
            builder.AddOption(btnBack);
            Menus.OpenMenuForPlayer(player, builder.Build());
        });
    }

    public void OpenRecordDetailsMenu(IPlayer player, TopSpeedRecord record, int rank)
    {
        var builder = Menus.CreateBuilder();
        string titleColor = rank > 0 ? (rank <= 3 ? "#FFD700" : "#FFFFFF") : "#FF00FF";
        string titleText = rank > 0 ? $"{record.player_name} #{rank}" : $"{record.player_name}";
        builder.Design.SetMenuTitle(Gradient(titleText, titleColor, titleColor));

        builder.AddOption(new ButtonMenuOption($"Map: {record.map_name}") { MaxWidth = 100f, Enabled = false });
        builder.AddOption(new ButtonMenuOption($"Speed: {Globals.FormatSpeed(record.velocity)}") { MaxWidth = 100f, Enabled = false });
        builder.AddOption(new ButtonMenuOption($"Time: {record.reach_time:F2}s") { MaxWidth = 100f, Enabled = false });
        builder.AddOption(new ButtonMenuOption($"Date: {record.date_achieved:yyyy-MM-dd}") { MaxWidth = 100f, Enabled = false });

        var btnBack = new ButtonMenuOption(Gradient("Back", "#FF0000", "#8B0000")) { MaxWidth = 100f };
        btnBack.Click += (s, a) => {
            if (rank > 0) OpenMapRecordsMenu(a.Player, record.map_name);
            else OpenPersonalRecordsMenu(a.Player);
            return ValueTask.CompletedTask; 
        };
        builder.AddOption(btnBack);
        Menus.OpenMenuForPlayer(player, builder.Build());
    }

    public void OpenPersonalRecordsMenu(IPlayer player)
    {
        Task.Run(async () => {
            var records = await DatabaseManager.GetPlayerAllRecordsAsync(player.SteamID.ToString());
            var builder = Menus.CreateBuilder();
            builder.Design.SetMenuTitle(Gradient("Personal Records", "#FF00FF", "#800080"));

            if (records.Count == 0) {
                builder.AddOption(new ButtonMenuOption("No records") { MaxWidth = 100f, Enabled = false });
            } else {
                foreach (var rec in records) {
                    string txt = Gradient($"{rec.map_name}: {Globals.FormatSpeed(rec.velocity)}", "#FFFFFF", "#AAAAAA");
                    var btn = new ButtonMenuOption(txt) { MaxWidth = 100f };
                    var currentRecord = rec;
                    btn.Click += (s, a) => { OpenRecordDetailsMenu(a.Player, currentRecord, 0); return ValueTask.CompletedTask; }; 
                    builder.AddOption(btn);
                }
            }
            var btnBack = new ButtonMenuOption(Gradient("Back", "#FF0000", "#8B0000")) { MaxWidth = 100f };
            btnBack.Click += (s, a) => { OpenTopSpeedMenu(a.Player); return ValueTask.CompletedTask; };
            builder.AddOption(btnBack);
            Menus.OpenMenuForPlayer(player, builder.Build());
        });
    }

    private void ToggleSetting(IPlayer player, string type)
    {
        int pid = player.PlayerID;
        bool newState = false;
        string featureName = "Unknown";
        
        if (type == "key") { _isKeyOverlayActive[pid] = !_isKeyOverlayActive[pid]; newState = _isKeyOverlayActive[pid]; featureName = "Key Overlay"; }
        else if (type == "jump") { _showJumps[pid] = !_showJumps[pid]; newState = _showJumps[pid]; featureName = "Jump Stats"; }
        else if (type == "round") { _showRoundStats[pid] = !_showRoundStats[pid]; newState = _showRoundStats[pid]; featureName = "Round Stats"; }

        SavePlayerData(player);
        var localizer = Translation.GetPlayerLocalizer(player);
        
        string statusKey = newState ? "menu.status.enabled" : "menu.status.disabled";
        string statusText = localizer[statusKey] ?? (newState ? "Enabled" : "Disabled");
        string colorCode = newState ? Helper.ChatColors.Green : Helper.ChatColors.Red;

        Speedometer.Instance._chatQueue.Enqueue((player, $"{Globals.ProcessColors(localizer["speedometer.prefix"])} {Globals.ProcessColors(featureName)}: {colorCode}{statusText}")); 
        OpenHudOptionsMenu(player);
    }

    public void OpenAdminMenu(IPlayer player)
    {
        var builder = Menus.CreateBuilder();
        var localizer = Translation.GetPlayerLocalizer(player);
        builder.Design.SetMenuTitle(Gradient(localizer["menu.admin.title"] ?? "TopSpeed Admin", "#FF0000", "#800000"));
        
        string resetMapColored = Gradient(localizer["menu.admin.reset_map"] ?? "Reset Records (This Map)", "#00FF00", "#006400");
        var btnResetMap = new ButtonMenuOption(resetMapColored) { MaxWidth = 100f };
        btnResetMap.Click += (sender, args) => {
            Task.Run(async () => { 
                await DatabaseManager.DeleteAllRecordsOnMapAsync(Speedometer.CurrentMapName); 
                await Speedometer.Instance.RefreshServerRecord();
            });

            for (int i = 0; i < 65; i++)
            {
                Speedometer.Instance._playerDbMaxSpeed[i] = 0;
                Speedometer.Instance._playerDbReachTime[i] = 0.0f;
                Speedometer.Instance._isBreakingRecord[i] = false;
                Speedometer.Instance._tempPeakSpeed[i] = 0;
            }

            string msg = localizer["topspeed.admin.deleted"] ?? "{DarkRed}Deleted!{Default}";
            Speedometer.Instance._chatQueue.Enqueue((args.Player, $"{Globals.ProcessColors(Config.Prefix)} {Globals.ProcessColors(msg)}"));
            return ValueTask.CompletedTask;
        };
        builder.AddOption(btnResetMap);

        string deleteDiffColored = Gradient(localizer["menu.admin.delete_diff"] ?? "Delete Record (Select Map)", "#00FFFF", "#0000FF");
        var btnDeleteDiff = new ButtonMenuOption(deleteDiffColored) { MaxWidth = 100f };
        btnDeleteDiff.Click += (sender, args) => { OpenDeleteDifferentMapMenu(args.Player); return ValueTask.CompletedTask; };
        builder.AddOption(btnDeleteDiff);

        var btnManagePlayer = new ButtonMenuOption(Gradient(localizer["menu.admin.manage_player"] ?? "Manage Player", "#FFFF00", "#FFA500")) { MaxWidth = 100f };
        btnManagePlayer.Click += (sender, args) => { 
            Globals.AdminEditStates[args.Player.PlayerID] = new Globals.AdminEditState { ActionType = "SEARCH_PLAYER" };
            string prefix = Globals.ProcessColors(Config.Prefix);
            string msg = localizer["chat.admin.search_prompt"] ?? "Enter partial player name in chat:";
            Speedometer.Instance._chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(msg)}"));
            ClosePlayerMenu(args.Player);
            return ValueTask.CompletedTask; 
        };
        builder.AddOption(btnManagePlayer);

        string resetAllColored = Gradient(localizer["menu.admin.reset_all"] ?? "Reset ALL Records", "#FF0000", "#8B0000");
        var btnResetAll = new ButtonMenuOption(resetAllColored) { MaxWidth = 100f };
        btnResetAll.Click += (sender, args) => {
            Task.Run(async () => { 
                await DatabaseManager.DeleteAllRecordsAsync(); 
                await Speedometer.Instance.RefreshServerRecord();
            });

            for (int i = 0; i < 65; i++)
            {
                Speedometer.Instance._playerDbMaxSpeed[i] = 0;
                Speedometer.Instance._playerDbReachTime[i] = 0.0f;
                Speedometer.Instance._isBreakingRecord[i] = false;
                Speedometer.Instance._tempPeakSpeed[i] = 0;
            }

            string msg = localizer["topspeed.admin.resetall"] ?? "{DarkRed}All reset!{Default}";
            Speedometer.Instance._chatQueue.Enqueue((args.Player, $"{Globals.ProcessColors(Config.Prefix)} {Globals.ProcessColors(msg)}"));
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
                var btnMap = new ButtonMenuOption(mapText) { MaxWidth = 100f }; 
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
            string titleRaw = localizer["menu.admin.delete_header"] ?? $"Delete Records - {mapName}";
            builder.Design.SetMenuTitle(SafeFormat(titleRaw, mapName));
            
            if (records.Count == 0) builder.AddOption(new ButtonMenuOption(localizer["menu.admin.no_records"] ?? "No records."));
            else {
                foreach (var rec in records) {
                    string recordText = Gradient($"{rec.player_name} ({rec.velocity} u/s - {rec.reach_time:F2}s)", "#FFFFFF", "#AAAAAA");
                    var btnDel = new ButtonMenuOption(recordText) { MaxWidth = 100f }; 
                    btnDel.Click += (sender, args) => {
                        Task.Run(async () => {
                            await DatabaseManager.DeletePlayerRecordAsync(rec.steamid, mapName);
                            
                            if (mapName == Speedometer.CurrentMapName) 
                            {
                                await Speedometer.Instance.RefreshServerRecord();
                                
                                var allPlayers = Speedometer.Instance.SwiftlyCore.PlayerManager.GetAllPlayers();
                                foreach (var p in allPlayers)
                                {
                                    if (p.SteamID.ToString() == rec.steamid)
                                    {
                                        Speedometer.Instance._playerDbMaxSpeed[p.PlayerID] = 0;
                                        Speedometer.Instance._playerDbReachTime[p.PlayerID] = 0.0f;
                                        Speedometer.Instance._isBreakingRecord[p.PlayerID] = false;
                                        Speedometer.Instance._tempPeakSpeed[p.PlayerID] = 0;
                                        break;
                                    }
                                }
                            }

                            string prefix = Globals.ProcessColors(Config.Prefix);
                            string delMsg = localizer["chat.admin.record_deleted"] ?? "Deleted {0} on {1}";
                            string formatted = SafeFormat(delMsg, rec.player_name, mapName);
                            Speedometer.Instance._chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(formatted)}"));
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
                    string msg = localizer["chat.admin.player_not_found"] ?? "{DarkRed}Player not found.{Default}";
                    Speedometer.Instance._chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(msg)}"));
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
                        if (state.TargetMap == Speedometer.CurrentMapName) 
                        {
                            await Speedometer.Instance.RefreshServerRecord();
                            var allPlayers = Speedometer.Instance.SwiftlyCore.PlayerManager.GetAllPlayers();
                            foreach (var p in allPlayers)
                            {
                                if (p.SteamID.ToString() == state.TargetSteamID)
                                {
                                    Speedometer.Instance._playerDbMaxSpeed[p.PlayerID] = newSpeed;
                                    break;
                                }
                            }
                        }
                        
                        string msg = localizer["chat.admin.speed_updated"] ?? "Speed updated for {0} on {1} to {2}";
                        string formatted = SafeFormat(msg, rec.player_name, state.TargetMap, newSpeed);
                        Speedometer.Instance._chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(formatted)}"));
                    }
                    OpenAdminRecordEditMenu(player, state.TargetSteamID, rec?.player_name ?? "Unknown", state.TargetMap);
                });
            }
            else {
                string msg = localizer["chat.admin.invalid_speed"] ?? "{DarkRed}Invalid speed!{Default}";
                Speedometer.Instance._chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(msg)}"));
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
                        
                        if (state.TargetMap == Speedometer.CurrentMapName)
                        {
                            var allPlayers = Speedometer.Instance.SwiftlyCore.PlayerManager.GetAllPlayers();
                            foreach (var p in allPlayers)
                            {
                                if (p.SteamID.ToString() == state.TargetSteamID)
                                {
                                    Speedometer.Instance._playerDbReachTime[p.PlayerID] = newTime;
                                    break;
                                }
                            }
                        }

                        string msg = localizer["chat.admin.time_updated"] ?? "Time updated for {0} on {1} to {2}";
                        string formatted = SafeFormat(msg, rec.player_name, state.TargetMap, newTime.ToString("F2"));
                        Speedometer.Instance._chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(formatted)}"));
                    }
                    OpenAdminRecordEditMenu(player, state.TargetSteamID, rec?.player_name ?? "Unknown", state.TargetMap);
                });
            }
            else {
                string msg = localizer["chat.admin.invalid_time"] ?? "{DarkRed}Invalid time!{Default}";
                Speedometer.Instance._chatQueue.Enqueue((player, $"{prefix} {Globals.ProcessColors(msg)}"));
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
            var btn = new ButtonMenuOption(p.Name) { MaxWidth = 100f };
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
            string title = localizer["menu.admin.select_map_player"] ?? $"Select Map ({targetName})";
            builder.Design.SetMenuTitle(SafeFormat(title, targetName));
            
            if (maps.Count == 0) builder.AddOption(new ButtonMenuOption(localizer["menu.admin.no_records"] ?? "No records."));
            foreach (var map in maps) {
                var btn = new ButtonMenuOption(Gradient(map, "#00FFFF", "#0000FF")) { MaxWidth = 100f };
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
                
                var btnDel = new ButtonMenuOption(Gradient(localizer["menu.admin.btn_delete"] ?? "DELETE RECORD", "#FF0000", "#8B0000")) { MaxWidth = 100f };
                btnDel.Click += (sender, args) => {
                    Task.Run(async () => {
                        await DatabaseManager.DeletePlayerRecordAsync(steamID, map);
                        if (map == Speedometer.CurrentMapName) await Speedometer.Instance.RefreshServerRecord();
                        
                        string prefix = Globals.ProcessColors(Config.Prefix);
                        string msg = localizer["topspeed.admin.deleted"] ?? "{DarkRed}Deleted!{Default}";
                        Speedometer.Instance._chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(msg)}"));
                        ClosePlayerMenu(args.Player);
                    });
                    return ValueTask.CompletedTask;
                };
                builder.AddOption(btnDel);

                var btnEditSpeed = new ButtonMenuOption(Gradient(localizer["menu.admin.btn_edit_speed"] ?? "Edit Speed (Chat Input)", "#00FF00", "#006400")) { MaxWidth = 100f };
                btnEditSpeed.Click += (sender, args) => {
                    Globals.AdminEditStates[args.Player.PlayerID] = new Globals.AdminEditState { ActionType = "EDIT_SPEED", TargetSteamID = steamID, TargetMap = map };
                    string prefix = Globals.ProcessColors(Config.Prefix);
                    string msg = localizer["chat.admin.enter_speed"] ?? "Enter speed:";
                    Speedometer.Instance._chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(msg)}"));
                    ClosePlayerMenu(args.Player);
                    return ValueTask.CompletedTask;
                };
                builder.AddOption(btnEditSpeed);

                var btnEditTime = new ButtonMenuOption(Gradient(localizer["menu.admin.btn_edit_time"] ?? "Edit Time (Chat Input)", "#00FFFF", "#0000FF")) { MaxWidth = 100f };
                btnEditTime.Click += (sender, args) => {
                    Globals.AdminEditStates[args.Player.PlayerID] = new Globals.AdminEditState { ActionType = "EDIT_TIME", TargetSteamID = steamID, TargetMap = map };
                    string prefix = Globals.ProcessColors(Config.Prefix);
                    string msg = localizer["chat.admin.enter_time"] ?? "Enter time:";
                    Speedometer.Instance._chatQueue.Enqueue((args.Player, $"{prefix} {Globals.ProcessColors(msg)}"));
                    ClosePlayerMenu(args.Player);
                    return ValueTask.CompletedTask;
                };
                builder.AddOption(btnEditTime);
            }
            var btnBack = new ButtonMenuOption(Gradient(localizer["menu.admin.back_maplist"] ?? "Back to Map List", "#FF0000", "#8B0000")) { MaxWidth = 100f };
            btnBack.Click += (sender, args) => { OpenAdminPlayerMapSelectMenu(args.Player, steamID, name); return ValueTask.CompletedTask; };
            builder.AddOption(btnBack);
            Menus.OpenMenuForPlayer(player, builder.Build());
        });
    }

    public void OpenColorMenu(IPlayer player)
    {
        var builder = Menus.CreateBuilder();
        var localizer = Translation.GetPlayerLocalizer(player);
        
        builder.Design.SetMenuTitle(localizer["menu.title.color"] ?? "Select Color");
        builder.Design.SetMaxVisibleItems(5);
        
        string rainbowRaw = localizer["menu.rainbow"] ?? "Rainbow";
        string rainbowColored = HtmlGradient.GenerateGradientText(rainbowRaw, "#FF0000", "#FFA500", "#FFFF00", "#00FF00", "#00FFFF", "#0000FF", "#8B00FF");
        var btnRainbow = new ButtonMenuOption(rainbowColored) { MaxWidth = 100f }; 
        
        btnRainbow.Click += (sender, args) => { SetColor(args.Player, "RAINBOW", "Rainbow", Helper.ChatColors.LightBlue); return ValueTask.CompletedTask; };
        builder.AddOption(btnRainbow);
        
        foreach (var color in AvailableColors) {
            if (color.Key == "Rainbow") continue;
            var button = new ButtonMenuOption($"<font color='{color.Value}'>{color.Key}</font>") { MaxWidth = 100f };
            button.Click += (sender, args) => { SetColor(args.Player, color.Value, color.Key, Helper.ChatColors.Green); return ValueTask.CompletedTask; };
            builder.AddOption(button);
        }
        var btnBack = new ButtonMenuOption(Gradient(localizer["menu.back"], "#FF0000", "#8B0000")) { MaxWidth = 100f };
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
        string msg = localizer["speedometer.colorset"] ?? "Color set to {0} ({1})";
        string formatted = SafeFormat(msg, chatColor, displayName);
        Speedometer.Instance._chatQueue.Enqueue((player, $"{Globals.ProcessColors(localizer["speedometer.prefix"])} {Globals.ProcessColors(formatted)}"));
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
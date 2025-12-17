using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq; 
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Core;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Commands;

namespace Speedometer;

public partial class Speedometer
{
    private DateTime[] _nextHudUpdate = new DateTime[65];

    [EventListener<EventDelegates.OnMapLoad>]
    public void OnMapLoad(IOnMapLoadEvent @event)
    {
        Speedometer.CurrentMapName = @event.MapName;
        Console.WriteLine($"[Speedometer] Map loaded: {Speedometer.CurrentMapName}");
        
        _serverRecordSpeed = 0;
        _serverRecordHolder = "";
        
        Task.Run(async () => {
            var records = await DatabaseManager.GetMapTopRecordsAsync(Speedometer.CurrentMapName, 1);
            if (records.Count > 0)
            {
                _serverRecordSpeed = records[0].velocity;
                _serverRecordHolder = records[0].player_name;
            }
        });
        
        _lastTopSpeedHelpTime = DateTime.Now;
        _lastSpeedometerHelpTime = DateTime.Now;
    }

    [EventListener<EventDelegates.OnMapUnload>]
    public void OnMapUnload(IOnMapUnloadEvent @event)
    {
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnClientConnect(EventPlayerConnectFull @event)
    {
        if (@event.UserIdPlayer is { } player)
        {
            int id = player.PlayerID;
            if (id >= 0 && id < 65)
            {
                _isSpeedometerActive[id] = Config.DefaultHudEnabled;
                _isKeyOverlayActive[id] = Config.DefaultKeyOverlayEnabled;
                _showJumps[id] = Config.DefaultJumpsEnabled;
                _showRoundStats[id] = Config.DefaultShowRoundStats;
                _playerColorChoices[id] = Globals.GetHexFromColorName(Config.DefaultColor);
                _playerJumpCounts[id] = 0;
                _playerSessionMaxSpeed[id] = 0;
                _playerRoundMaxSpeed[id] = 0;
                _playerSpeedRunStartTime[id] = DateTime.Now;
                _playerDbReachTime[id] = 0.0f; 
                
                _isBreakingRecord[id] = false;
                _tempPeakSpeed[id] = 0;
                _tempPeakTime[id] = 0.0f;
                _nextHudUpdate[id] = DateTime.Now; 

                if (!player.IsFakeClient)
                {
                    Task.Run(async () => 
                    {
                        string steamId = player.SteamID.ToString();
                        var data = await DatabaseManager.LoadPlayerAsync(steamId);
                        if (data != null)
                        {
                            _isSpeedometerActive[id] = data.hud_enabled == 1;
                            _isKeyOverlayActive[id] = data.keys_enabled == 1;
                            _showJumps[id] = data.jumps_enabled == 1;
                            _showRoundStats[id] = data.show_round_stats == 1;
                            _playerColorChoices[id] = data.color;
                        }

                        if (Speedometer.CurrentMapName != "unknown" && Speedometer.CurrentMapName != "unknown_map")
                        {
                            var record = await DatabaseManager.GetPlayerMapRecordAsync(steamId, Speedometer.CurrentMapName);
                            if (record != null)
                            {
                                _playerDbMaxSpeed[id] = record.velocity;
                                _playerDbReachTime[id] = record.reach_time;
                            }
                            else
                            {
                                _playerDbMaxSpeed[id] = 0;
                                _playerDbReachTime[id] = 0.0f;
                            }
                        }
                    });
                }
            }
        }
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnClientDisconnect(EventPlayerDisconnect @event)
    {
        if (@event.UserIdPlayer is { } player)
        {
            int id = player.PlayerID;
            if (id >= 0 && id < 65)
            {
                _isSpeedometerActive[id] = false;
                _isKeyOverlayActive[id] = false;
                _playerColorChoices[id] = null;
                _playerJumpCounts[id] = 0;
                _showJumps[id] = true;
                _playerDbMaxSpeed[id] = 0;
                _playerSessionMaxSpeed[id] = 0;
                _playerRoundMaxSpeed[id] = 0;
                _playerDbReachTime[id] = 0.0f; 
                
                Globals.AdminEditStates.TryRemove(id, out _);
            }
        }
        return HookResult.Continue;
    }

    [ClientChatHookHandler]
    public HookResult OnClientChat(int playerId, string text, bool teamOnly)
    {
        var player = Speedometer.Instance.SwiftlyCore.PlayerManager.GetPlayer(playerId);
        if (player == null || !player.IsValid) return HookResult.Continue;

        int pid = player.PlayerID;

        if (Globals.AdminEditStates.TryGetValue(pid, out var state))
        {
            if (!Speedometer.Instance.SwiftlyCore.Permission.PlayerHasPermission(player.SteamID, Config.AdminFlag))
            {
                Globals.AdminEditStates.TryRemove(pid, out _); 
                return HookResult.Continue;
            }
            Speedometer.Instance.HandleAdminChatInput(player, text, state);
            return HookResult.Stop; 
        }
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnRoundStart(EventRoundStart @event)
    {
        for (int i = 0; i < 65; i++) _playerRoundMaxSpeed[i] = 0;
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnRoundEnd(EventRoundEnd @event)
    {
        var players = Speedometer.Instance.SwiftlyCore.PlayerManager.GetAllPlayers();
        var stats = players
            .Where(p => p != null && p.IsValid && !p.IsFakeClient && _playerRoundMaxSpeed[p.PlayerID] > 0)
            .Select(p => new { Name = p.Controller?.PlayerName ?? "Unknown", Speed = _playerRoundMaxSpeed[p.PlayerID] })
            .OrderByDescending(x => x.Speed)
            .Take(3) 
            .ToList();

        if (stats.Any())
        {
            string prefix = Globals.ProcessColors(Config.Prefix);
            foreach (var p in players)
            {
                if (p != null && p.IsValid && !p.IsFakeClient && _showRoundStats[p.PlayerID])
                {
                    var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(p);
                    string titleKey = "speedometer.round_top_players";
                    string msgTitle = Globals.ProcessColors(localizer[titleKey] ?? "{Green}--- Fastest of the Round ---");
                    
                    p.SendChat($"{prefix} {msgTitle}");
                    int rank = 1;
                    foreach (var s in stats)
                    {
                        string speedStr = Globals.FormatSpeed(s.Speed);
                        string line = Globals.ProcessColors($"{rank}. {s.Name}: {{Green}}{speedStr}");
                        p.SendChat(line);
                        rank++;
                    }
                }
            }
        }
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerJump(EventPlayerJump @event)
    {
        if (@event.UserIdPlayer is { } player)
        {
            int id = player.PlayerID;
            if (id >= 0 && id < 65) _playerJumpCounts[id]++;
        }
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event)
    {
        if (@event.UserIdPlayer is { } player)
        {
            int id = player.PlayerID;
            if (id >= 0 && id < 65)
            {
                _playerJumpCounts[id] = 0;
                _playerSpeedRunStartTime[id] = DateTime.Now;
            }
        }
        return HookResult.Continue;
    }

    private string GetRainbowColor()
    {
        double time = (DateTime.Now.Ticks / 10000.0) * 0.08; 
        int r = (int)(Math.Sin(time * 0.1 + 0) * 127 + 128);
        int g = (int)(Math.Sin(time * 0.1 + 2) * 127 + 128);
        int b = (int)(Math.Sin(time * 0.1 + 4) * 127 + 128);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    [EventListener<EventDelegates.OnTick>]
    public void OnTick()
    {
        while (_chatQueue.TryDequeue(out var item))
        {
            if (item.player != null && item.player.IsValid) item.player.SendChat(item.message);
        }

        while (_commandQueue.TryDequeue(out var cmd))
        {
            if (cmd.player != null && cmd.player.IsValid) cmd.player.ExecuteCommand(cmd.command);
        }

        var allPlayers = Speedometer.Instance.SwiftlyCore.PlayerManager.GetAllPlayers();
        string currentRainbow = GetRainbowColor();

        string SafeTrans(IPlayer p, string key) {
            try { return Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(p)[key]; } catch { return ""; }
        }

        if (Config.HelpMessageIntervalMinutes > 0 && (DateTime.Now - _lastTopSpeedHelpTime).TotalMinutes >= Config.HelpMessageIntervalMinutes)
        {
            _lastTopSpeedHelpTime = DateTime.Now;
            string prefix = Globals.ProcessColors(Config.Prefix);
            foreach (var p in allPlayers)
                if (p?.IsValid == true && !p.IsFakeClient)
                {
                    string msg = SafeTrans(p, "help.message.topspeed");
                    if(!string.IsNullOrEmpty(msg)) p.SendChat($"{prefix} {Globals.ProcessColors(msg)}");
                }
        }

        if (Config.SpeedometerHelpIntervalMinutes > 0 && (DateTime.Now - _lastSpeedometerHelpTime).TotalMinutes >= Config.SpeedometerHelpIntervalMinutes)
        {
            _lastSpeedometerHelpTime = DateTime.Now;
            string prefix = Globals.ProcessColors(Config.Prefix);
            foreach (var p in allPlayers)
                if (p?.IsValid == true && !p.IsFakeClient)
                {
                    string msg = SafeTrans(p, "help.message.speedometer");
                    if(!string.IsNullOrEmpty(msg)) p.SendChat($"{prefix} {Globals.ProcessColors(msg)}");
                }
        }

        foreach (var player in allPlayers)
        {
            if (player == null || !player.IsValid || player.IsFakeClient) continue;
            
            int pId = player.PlayerID;
            if (pId < 0 || pId >= 65) continue;

            if (!_isSpeedometerActive[pId]) continue;

            CCSPlayerPawn? targetPawn = null;
            int currentSpeed = 0;
            GameButtonFlags pressedButtons = 0;
            bool isSpectating = false;

            if (player.PlayerPawn != null && player.PlayerPawn.LifeState == 0)
            {
                targetPawn = player.PlayerPawn;
                pressedButtons = player.PressedButtons;
            }
            else if (player.PlayerPawn != null && player.PlayerPawn.ObserverServices != null)
            {
                var observerHandle = player.PlayerPawn.ObserverServices.ObserverTarget;
                if (observerHandle.Value is CCSPlayerPawn pawn)
                {
                    targetPawn = pawn;
                    if (targetPawn.LifeState == 0) isSpectating = true;
                }
            }

            if (targetPawn == null) continue;

            var velocity = targetPawn.AbsVelocity;
            double speedDouble = Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
            currentSpeed = (int)Math.Round(speedDouble);

            if (!isSpectating && Config.TopSpeedEnabled)
            {
                if (currentSpeed < 100) _playerSpeedRunStartTime[pId] = DateTime.Now;
                
                if (currentSpeed > _playerSessionMaxSpeed[pId]) _playerSessionMaxSpeed[pId] = currentSpeed;
                if (currentSpeed > _playerRoundMaxSpeed[pId]) _playerRoundMaxSpeed[pId] = currentSpeed;

                bool isMapValid = Speedometer.CurrentMapName != "unknown" && Speedometer.CurrentMapName != "unknown_map";

                if (isMapValid)
                {
                    int recordThreshold = _serverRecordSpeed > 300 ? _serverRecordSpeed : 300;

                    if (currentSpeed > recordThreshold)
                    {
                        _isBreakingRecord[pId] = true;
                        
                        if (currentSpeed > _tempPeakSpeed[pId])
                        {
                            _tempPeakSpeed[pId] = currentSpeed;
                            float reachTime = (float)(DateTime.Now - _playerSpeedRunStartTime[pId]).TotalSeconds;
                            if (reachTime < 0) reachTime = 0;
                            _tempPeakTime[pId] = reachTime;
                        }
                    }
                    else if (_isBreakingRecord[pId] && currentSpeed < recordThreshold)
                    {
                        FinishRecordRun(player, pId);
                    }
                }
            }

            if (DateTime.Now < _nextHudUpdate[pId]) continue;
            _nextHudUpdate[pId] = DateTime.Now.AddMilliseconds(25);

            string userColor;
            string choice = _playerColorChoices[pId];
            if (choice == "RAINBOW") userColor = currentRainbow;
            else userColor = choice ?? DefaultColorHex;

            int jumps = _playerJumpCounts[pId];

            StringBuilder htmlBuilder = new StringBuilder();
            string displaySpeedStr = Globals.FormatSpeed(currentSpeed);
            string label = isSpectating ? "<font color='#AAAAAA'>(Spec)</font> Speed" : "Speed";
            htmlBuilder.Append($"<font class='fontSize-l' color='#FFFFFF'>{label}: <font color='{userColor}'>{displaySpeedStr}</font></font>");
            
            if (_showJumps[pId] && !isSpectating)
            {
                htmlBuilder.Append("<br>");
                htmlBuilder.Append($"<font class='fontSize-l' color='#CCCCCC'>Jumps: <font color='{userColor}'>{jumps}</font></font>");
            }
            
            if (_isKeyOverlayActive[pId] && !isSpectating)
            {
                htmlBuilder.Append("<br>");
                string K(GameButtonFlags flag, string txt) => ((pressedButtons & flag) != 0) ? $"<font color='#FFFFFF'>{txt}</font>" : $"<font color='#444444'>{txt}</font>";
                htmlBuilder.Append("<font face='Consolas, monospace' class='fontSize-l'>");
                string s = "&nbsp;"; 
                htmlBuilder.Append($"{K(GameButtonFlags.A, "←")}{s}{K(GameButtonFlags.W, "W")}{s}{K(GameButtonFlags.D, "→")}<br>");
                htmlBuilder.Append($"{K(GameButtonFlags.A2, "A")}{s}{K(GameButtonFlags.S, "S")}{s}{K(GameButtonFlags.D2, "D")}<br>");
                htmlBuilder.Append($"{K(GameButtonFlags.Ctrl, "C")}{s}{s}{s}{K(GameButtonFlags.Space, "J")}");
                htmlBuilder.Append("</font>");
            }
            
            player.SendCenterHTML(htmlBuilder.ToString(), 300);
        }
    }

    private void FinishRecordRun(IPlayer player, int pId)
    {
        _isBreakingRecord[pId] = false;
        
        int peakSpeed = _tempPeakSpeed[pId];
        float peakTime = _tempPeakTime[pId];
        
        if (peakSpeed > _playerDbMaxSpeed[pId])
        {
            _playerDbMaxSpeed[pId] = peakSpeed;
            _playerDbReachTime[pId] = peakTime;

            string pName = player.Controller?.PlayerName ?? "Unknown";
            string steamId = player.SteamID.ToString();
            string mapName = Speedometer.CurrentMapName;

            if (peakSpeed > _serverRecordSpeed)
            {
                _serverRecordSpeed = peakSpeed;
                _serverRecordHolder = pName;
                
                string prefix = Globals.ProcessColors(Config.Prefix);
                string speedStr = Globals.FormatSpeed(peakSpeed);
                var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(player);
                
                string msg = "{Green}{0}{Default} set a new {Red}MAP RECORD{Default}: {Green}{1}!";
                try 
                {
                    string trans = localizer["topspeed.new_record"];
                    if (!string.IsNullOrEmpty(trans)) msg = trans;
                }
                catch { }

                string safeMsg = msg.Replace("{0}", pName).Replace("{1}", speedStr);
                string finalMsg = $"{prefix} {Globals.ProcessColors(safeMsg)}";

                var allPlayers = Speedometer.Instance.SwiftlyCore.PlayerManager.GetAllPlayers();
                foreach(var p in allPlayers) if(p!=null && p.IsValid) p.SendChat(finalMsg);
                
                Task.Run(async () => 
                {
                    var newRecord = new TopSpeedRecord 
                    { 
                        steamid = steamId, 
                        player_name = pName, 
                        map_name = mapName, 
                        velocity = peakSpeed, 
                        reach_time = peakTime, 
                        date_achieved = DateTime.Now
                    };
                    await DatabaseManager.SaveTopSpeedRecordAsync(newRecord);
                    await DiscordWebhook.SendDiscordWebhook(pName, mapName, peakSpeed);
                });
            }
            else
            {
                 Task.Run(async () => 
                 {
                     var newRecord = new TopSpeedRecord 
                     { 
                         steamid = steamId, 
                         player_name = pName, 
                         map_name = mapName, 
                         velocity = peakSpeed, 
                         reach_time = peakTime, 
                         date_achieved = DateTime.Now
                     };
                     await DatabaseManager.SaveTopSpeedRecordAsync(newRecord);
                 });
            }
        }
        _tempPeakSpeed[pId] = 0;
    }
}
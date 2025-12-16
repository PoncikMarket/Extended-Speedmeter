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

namespace Speedometer;

public partial class Speedometer
{
    private int[] _playerJumpCounts = new int[65];
    public bool[] _showJumps = new bool[65];
    private DateTime[] _playerSpeedRunStartTime = new DateTime[65];
    
    // Yardım Mesajı Sayaçları
    private DateTime _lastTopSpeedHelpTime = DateTime.Now;
    private DateTime _lastSpeedometerHelpTime = DateTime.Now;

    [EventListener<EventDelegates.OnMapLoad>]
    public void OnMapLoad(IOnMapLoadEvent @event)
    {
        Speedometer.CurrentMapName = @event.MapName;
        Console.WriteLine($"[Speedometer] Harita yuklendi: {Speedometer.CurrentMapName}");
        
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
        
        // Süreleri sıfırla
        _lastTopSpeedHelpTime = DateTime.Now;
        _lastSpeedometerHelpTime = DateTime.Now;
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

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnClientChat(EventPlayerChat @event)
    {
        if (@event.UserIdPlayer is not { } player) return HookResult.Continue;
        if (@event.TeamOnly) return HookResult.Continue;
        
        int pid = player.PlayerID;

        if (Globals.AdminEditStates.TryGetValue(pid, out var state))
        {
            if (!Speedometer.Instance.SwiftlyCore.Permission.PlayerHasPermission(player.SteamID, Config.AdminFlag))
            {
                Globals.AdminEditStates.TryRemove(pid, out _); 
                return HookResult.Continue;
            }
            Speedometer.Instance.HandleAdminChatInput(player, @event.Text, state);
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
            string msgTitle = Globals.ProcessColors("{Green}--- Round En Hizlilari ---");
            
            foreach (var p in players)
            {
                if (p != null && p.IsValid && !p.IsFakeClient && _showRoundStats[p.PlayerID])
                {
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

        // --- BİLGİLENDİRME MESAJI 1 (TopSpeed) ---
        if (Config.HelpMessageIntervalMinutes > 0)
        {
            if ((DateTime.Now - _lastTopSpeedHelpTime).TotalMinutes >= Config.HelpMessageIntervalMinutes)
            {
                _lastTopSpeedHelpTime = DateTime.Now;
                string prefix = Globals.ProcessColors(Config.Prefix);
                
                foreach (var p in allPlayers)
                {
                    if (p != null && p.IsValid && !p.IsFakeClient)
                    {
                        // Oyuncunun diline göre mesajı al
                        var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(p);
                        string msg = Globals.ProcessColors(localizer["help.message.topspeed"]);
                        p.SendChat($"{prefix} {msg}");
                    }
                }
            }
        }

        // --- BİLGİLENDİRME MESAJI 2 (Speedometer) ---
        if (Config.SpeedometerHelpIntervalMinutes > 0)
        {
            if ((DateTime.Now - _lastSpeedometerHelpTime).TotalMinutes >= Config.SpeedometerHelpIntervalMinutes)
            {
                _lastSpeedometerHelpTime = DateTime.Now;
                string prefix = Globals.ProcessColors(Config.Prefix);
                
                foreach (var p in allPlayers)
                {
                    if (p != null && p.IsValid && !p.IsFakeClient)
                    {
                        // Oyuncunun diline göre mesajı al
                        var localizer = Speedometer.Instance.SwiftlyCore.Translation.GetPlayerLocalizer(p);
                        string msg = Globals.ProcessColors(localizer["help.message.speedometer"]);
                        p.SendChat($"{prefix} {msg}");
                    }
                }
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
                if (observerHandle.Value != null)
                {
                    if (observerHandle.Value is CCSPlayerPawn pawn)
                    {
                        targetPawn = pawn;
                        if (targetPawn.LifeState == 0) 
                        {
                            isSpectating = true;
                        }
                    }
                }
            }

            if (targetPawn == null) continue;

            var velocity = targetPawn.AbsVelocity;
            double speedDouble = Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
            currentSpeed = (int)Math.Round(speedDouble);

            // --- REKOR KAYDI ---
            if (!isSpectating && Config.TopSpeedEnabled)
            {
                if (currentSpeed < 100) _playerSpeedRunStartTime[pId] = DateTime.Now;
                
                if (currentSpeed > _playerSessionMaxSpeed[pId]) _playerSessionMaxSpeed[pId] = currentSpeed;
                if (currentSpeed > _playerRoundMaxSpeed[pId]) _playerRoundMaxSpeed[pId] = currentSpeed;

                bool isMapValid = Speedometer.CurrentMapName != "unknown" && Speedometer.CurrentMapName != "unknown_map";

                if (isMapValid && currentSpeed > _playerDbMaxSpeed[pId] && currentSpeed > 300)
                {
                    _playerDbMaxSpeed[pId] = currentSpeed;
                    
                    float reachTime = (float)(DateTime.Now - _playerSpeedRunStartTime[pId]).TotalSeconds;
                    if (reachTime < 0) reachTime = 0;
                    
                    _playerDbReachTime[pId] = reachTime;

                    string pName = player.Controller?.PlayerName ?? "Unknown";
                    string steamId = player.SteamID.ToString();
                    string mapName = Speedometer.CurrentMapName;

                    if (currentSpeed > _serverRecordSpeed)
                    {
                        _serverRecordSpeed = currentSpeed;
                        _serverRecordHolder = pName;
                        string prefix = Globals.ProcessColors(Config.Prefix);
                        string speedStr = Globals.FormatSpeed(currentSpeed);
                        
                        foreach(var p in allPlayers) 
                        {
                            if(p!=null && p.IsValid) 
                                p.SendChat($"{prefix} {Globals.ProcessColors($"{{Green}}{pName}{{Default}} yeni {{Red}}HARITA REKORU{{Default}} kirdi: {{Green}}{speedStr}!")}");
                        }
                    }

                    Task.Run(async () => 
                    {
                        var newRecord = new TopSpeedRecord
                        {
                            steamid = steamId,
                            player_name = pName,
                            map_name = mapName,
                            velocity = currentSpeed,
                            reach_time = reachTime,
                            date_achieved = DateTime.Now
                        };
                        await DatabaseManager.SaveTopSpeedRecordAsync(newRecord);
                    });
                }
            }

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
                var b = pressedButtons;
                string K(GameButtonFlags flag, string txt) => ((b & flag) != 0) ? $"<font color='#FFFFFF'>{txt}</font>" : $"<font color='#444444'>{txt}</font>";
                htmlBuilder.Append("<font face='Consolas, monospace' class='fontSize-l'>");
                string s = "&nbsp;"; 
                htmlBuilder.Append($"{K(GameButtonFlags.A, "←")}{s}{K(GameButtonFlags.W, "W")}{s}{K(GameButtonFlags.D, "→")}<br>");
                htmlBuilder.Append($"{K(GameButtonFlags.A2, "A")}{s}{K(GameButtonFlags.S, "S")}{s}{K(GameButtonFlags.D2, "D")}<br>");
                htmlBuilder.Append($"{K(GameButtonFlags.Ctrl, "C")}{s}{s}{s}{K(GameButtonFlags.Space, "J")}");
                htmlBuilder.Append("</font>");
            }
            
            player.SendCenterHTML(htmlBuilder.ToString(), 100);
        }
    }
}
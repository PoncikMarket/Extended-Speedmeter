using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using SwiftlyS2.Core;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Players;

namespace Speedometer;

[PluginMetadata(Id = "Extended-Speedmeter", Version = "1.0.0", Name = "Extended-Speedmeter", Author = "PoncikMarket", Description = "Speedmeter with topspeed")]
public partial class Speedometer : BasePlugin
{
    public static Speedometer Instance { get; private set; } = null!;
    public ISwiftlyCore SwiftlyCore { get; private set; }
    
    public static string CurrentMapName { get; set; } = "unknown_map";

    public int _serverRecordSpeed = 0;
    public string _serverRecordHolder = "";
    
    public bool[] _isBreakingRecord = new bool[65];
    public int[] _tempPeakSpeed = new int[65];
    public float[] _tempPeakTime = new float[65];

    public DateTime _lastTopSpeedHelpTime = DateTime.Now;
    public DateTime _lastSpeedometerHelpTime = DateTime.Now;
    public DateTime[] _lastCommandTime = new DateTime[65];

    public ConcurrentQueue<(IPlayer player, string message)> _chatQueue = new();
    public ConcurrentQueue<(IPlayer player, string command)> _commandQueue = new();

    public int[] _playerJumpCounts = new int[65];
    public bool[] _isSpeedometerActive = new bool[65];
    public bool[] _isKeyOverlayActive = new bool[65];
    public bool[] _showJumps = new bool[65];
    public bool[] _showRoundStats = new bool[65];
    public string?[] _playerColorChoices = new string?[65];
    public DateTime[] _playerSpeedRunStartTime = new DateTime[65];
    
    public int[] _playerDbMaxSpeed = new int[65];
    public float[] _playerDbReachTime = new float[65];
    public int[] _playerSessionMaxSpeed = new int[65];
    public int[] _playerRoundMaxSpeed = new int[65];

    public const string DefaultColorHex = "#00FF00";

    public static readonly Dictionary<string, string> AvailableColors = new()
    {
        { "Green", "#00FF00" }, { "Red", "#FF0000" }, { "Blue", "#0000FF" },
        { "Yellow", "#FFFF00" }, { "Cyan", "#00FFFF" }, { "Magenta", "#FF00FF" },
        { "White", "#FFFFFF" }, { "Black", "#000000" }, { "Orange", "#FFA500" },
        { "Purple", "#800080" }, { "Lime", "#00FF7F" }, { "Pink", "#FFC0CB" },
        { "Teal", "#008080" }, { "Gold", "#FFD700" }, { "Rainbow", "RAINBOW" } 
    };

    public Speedometer(ISwiftlyCore core) : base(core) 
    {
        Instance = this;
        SwiftlyCore = core;
    }

    public static PluginConfig Config => _configMonitor.CurrentValue;
    private static IOptionsMonitor<PluginConfig> _configMonitor = null!;

    public override void Load(bool hotReload)
    {
        LoadConfiguration();
        Task.Run(async () => await DatabaseManager.InitializeAsync());
        Console.WriteLine($"[Speedometer] Plugin yuklendi!");
    }

    public override void Unload()
    {
        Console.WriteLine("[Speedometer] Plugin durduruldu.");
    }

    private void LoadConfiguration()
    {
        const string ConfigFileName = "config.json";
        const string ConfigSection = "SpeedometerConfig";

        Core.Configuration.InitializeJsonWithModel<PluginConfig>(ConfigFileName, ConfigSection)
            .Configure(cfg => cfg.AddJsonFile(Core.Configuration.GetConfigPath(ConfigFileName), optional: false, reloadOnChange: true));

        ServiceCollection services = new();
        services.AddSwiftly(Core).AddOptionsWithValidateOnStart<PluginConfig>().BindConfiguration(ConfigSection);
        var provider = services.BuildServiceProvider();
        _configMonitor = provider.GetRequiredService<IOptionsMonitor<PluginConfig>>();
    }

    public async Task RefreshServerRecord()
    {
        var records = await DatabaseManager.GetMapTopRecordsAsync(CurrentMapName, 1);
        if (records.Count > 0)
        {
            _serverRecordSpeed = records[0].velocity;
            _serverRecordHolder = records[0].player_name;
        }
        else
        {
            _serverRecordSpeed = 0;
            _serverRecordHolder = "";
        }
    }
}
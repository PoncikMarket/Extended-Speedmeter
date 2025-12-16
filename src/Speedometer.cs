using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration; 
using SwiftlyS2.Core;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Speedometer;

[PluginMetadata(Id = "Speedometer", Version = "1.0.0", Name = "Speedometer", Author = "Poncik", Description = "Customizable Speedometer Plugin")]
public partial class Speedometer : BasePlugin
{
    public static Speedometer Instance { get; private set; } = null!;
    public ISwiftlyCore SwiftlyCore { get; private set; }
    
    // Harita ismini tutan değişken
    public static string CurrentMapName { get; set; } = "unknown_map";

    public Speedometer(ISwiftlyCore core) : base(core) 
    {
        Instance = this;
        SwiftlyCore = core;
    }

    public static PluginConfig Config { get; private set; } = new();

    public override void Load(bool hotReload)
    {
        LoadConfiguration();
        
        // Database Tablosunu Oluştur
        Task.Run(async () => await DatabaseManager.InitializeAsync());

        // HATA DÜZELTME: Sorun çıkaran Cvars kodu kaldırıldı.
        // Eklenti ilk yüklendiğinde harita ismi "unknown" kalabilir.
        // Ancak Events.cs'deki koruma sayesinde bu durum veritabanını bozmayacak.
        // Harita değiştiğinde (map de_mirage vb.) isim otomatik düzelecektir.

        // Yardım Mesajı Döngüsünü Başlat
        StartHelpMessageLoop();

        Console.WriteLine($"[Speedometer] Plugin yuklendi!");
    }

    public override void Unload()
    {
        Console.WriteLine("[Speedometer] Plugin durduruldu.");
    }
    
    private void StartHelpMessageLoop()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                int delayMinutes = Config.HelpMessageIntervalMinutes > 0 ? Config.HelpMessageIntervalMinutes : 4;
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes));

                if (Config.TopSpeedEnabled)
                {
                    string prefix = Globals.ProcessColors(Config.Prefix);
                    string msg = Globals.ProcessColors("{Green}Rekorlari gormek icin {Olive}!topspeedhelp {Green}yazabilirsiniz!{Default}");
                    
                    var players = SwiftlyCore.PlayerManager.GetAllPlayers();
                    foreach (var p in players)
                    {
                        if (p != null && p.IsValid && !p.IsFakeClient)
                            p.SendChat($"{prefix} {msg}");
                    }
                }
            }
        });
    }

    private void LoadConfiguration()
    {
        const string ConfigFileName = "config.json";
        const string ConfigSection = "SpeedometerConfig";

        Core.Configuration.InitializeJsonWithModel<PluginConfig>(ConfigFileName, ConfigSection)
            .Configure(cfg => cfg.AddJsonFile(
                Core.Configuration.GetConfigPath(ConfigFileName),
                optional: false,
                reloadOnChange: true));

        ServiceCollection services = new();
        services.AddSwiftly(Core)
            .AddOptionsWithValidateOnStart<PluginConfig>()
            .BindConfiguration(ConfigSection);

        var provider = services.BuildServiceProvider();
        Config = provider.GetRequiredService<IOptions<PluginConfig>>().Value;
        
        var monitor = provider.GetRequiredService<IOptionsMonitor<PluginConfig>>();
        monitor.OnChange(updatedConfig => { Config = updatedConfig; });
    }
}
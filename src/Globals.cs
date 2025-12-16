using System.Collections.Generic;
using System.Collections.Concurrent;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Speedometer;

public partial class Speedometer
{
    // --- MEVCUT DEĞİŞKENLER ---
    private readonly bool[] _isSpeedometerActive = new bool[65];
    private readonly bool[] _isKeyOverlayActive = new bool[65];
    private readonly bool[] _showRoundStats = new bool[65]; // YENİ: Round sonu istatistik ayarı
    private readonly string?[] _playerColorChoices = new string?[65];
    
    private readonly ConcurrentQueue<(IPlayer player, string message)> _chatQueue = new();
    private readonly ConcurrentQueue<(IPlayer player, string command)> _commandQueue = new();

    // --- TopSpeed Değişkenleri ---
    private readonly int[] _playerDbMaxSpeed = new int[65]; 
    private readonly float[] _playerDbReachTime = new float[65];
    private readonly int[] _playerSessionMaxSpeed = new int[65];
    private readonly int[] _playerRoundMaxSpeed = new int[65]; // YENİ: Round içi en yüksek hız

    // --- Sunucu Rekoru Takibi (Duyuru için) ---
    private int _serverRecordSpeed = 0;
    private string _serverRecordHolder = "";

    public static readonly Dictionary<string, string> AvailableColors = new()
    {
        { "Green", "#00FF00" },
        { "Red", "#FF0000" },
        { "Blue", "#0000FF" },
        { "Yellow", "#FFFF00" },
        { "Cyan", "#00FFFF" },
        { "Magenta", "#FF00FF" },
        { "White", "#FFFFFF" },
        { "Black", "#000000" },
        { "Orange", "#FFA500" },
        { "Purple", "#800080" },
        { "Lime", "#00FF7F" },
        { "Pink", "#FFC0CB" },
        { "Teal", "#008080" },
        { "Gold", "#FFD700" },
        { "Rainbow", "RAINBOW" } 
    };

    public const string DefaultColorHex = "#00FF00";

    public static class Globals 
    {
        public static ConcurrentDictionary<int, AdminEditState> AdminEditStates = new();

        public class AdminEditState
        {
            public string ActionType { get; set; } = ""; 
            public string TargetSteamID { get; set; } = "";
            public string TargetMap { get; set; } = "";
        }
        
        public static string GetHexFromColorName(string name)
        {
             if (AvailableColors.TryGetValue(name, out var hex)) return hex;
             return DefaultColorHex;
        }

        // YENİ: Hız Birimi Çevirici
        public static string FormatSpeed(int velocity)
        {
            float val = (float)velocity;
            string suffix = "u/s";
            
            // CS2 Unit Dönüşümleri (Yaklaşık)
            switch (Speedometer.Config.SpeedUnit)
            {
                case 1: // km/h
                    val *= 0.06858f; 
                    suffix = "km/h";
                    break;
                case 2: // mph
                    val *= 0.04261f; 
                    suffix = "mph";
                    break;
                case 3: // m/s
                    val *= 0.01905f; 
                    suffix = "m/s";
                    break;
                default: // 0 = u/s
                    // Değişiklik yok
                    break;
            }
            
            return $"{val:F0} {suffix}";
        }

        public static string ProcessColors(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("{Default}", Helper.ChatColors.Default)
                        .Replace("{White}", Helper.ChatColors.White)
                        .Replace("{DarkRed}", Helper.ChatColors.DarkRed)
                        .Replace("{Green}", Helper.ChatColors.Green)
                        .Replace("{LightYellow}", Helper.ChatColors.LightYellow)
                        .Replace("{LightBlue}", Helper.ChatColors.LightBlue)
                        .Replace("{Olive}", Helper.ChatColors.Olive)
                        .Replace("{Lime}", Helper.ChatColors.Lime)
                        .Replace("{Red}", Helper.ChatColors.Red)
                        .Replace("{Purple}", Helper.ChatColors.Purple)
                        .Replace("{Grey}", Helper.ChatColors.Grey)
                        .Replace("{Yellow}", Helper.ChatColors.Yellow)
                        .Replace("{Gold}", Helper.ChatColors.Gold)
                        .Replace("{Silver}", Helper.ChatColors.Silver)
                        .Replace("{Blue}", Helper.ChatColors.Blue)
                        .Replace("{DarkBlue}", Helper.ChatColors.DarkBlue)
                        .Replace("{BlueGrey}", Helper.ChatColors.BlueGrey)
                        .Replace("{Magenta}", Helper.ChatColors.Magenta)
                        .Replace("{LightRed}", Helper.ChatColors.LightRed);
        }
    }
}
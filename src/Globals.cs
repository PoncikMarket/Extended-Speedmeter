using System.Collections.Concurrent;
using System.Collections.Generic;
using SwiftlyS2.Core;
using SwiftlyS2.Shared; 

namespace Speedometer;

public static class Globals 
{
    public static ConcurrentDictionary<int, AdminEditState> AdminEditStates = new();

    public class AdminEditState
    {
        public string ActionType { get; set; } = ""; 
        public string TargetSteamID { get; set; } = "";
        public string TargetMap { get; set; } = "";
    }
    
    public static string FormatSpeed(int velocity)
    {
        float val = (float)velocity;
        string suffix = "u/s";
        
        switch (Speedometer.Config.SpeedUnit)
        {
            case 1: val *= 0.06858f; suffix = "km/h"; break;
            case 2: val *= 0.04261f; suffix = "mph"; break;
            case 3: val *= 0.01905f; suffix = "m/s"; break;
        }
        
        return $"{val:F0} {suffix}";
    }

    public static string GetHexFromColorName(string name)
    {
         if (Speedometer.AvailableColors.TryGetValue(name, out var hex)) return hex;
         return Speedometer.DefaultColorHex;
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
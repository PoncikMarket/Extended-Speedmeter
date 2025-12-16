using System.Collections.Generic;

namespace Speedometer;

public class PluginConfig
{
    public string DatabaseConnection { get; set; } = "speedometer_db";
    public string Prefix { get; set; } = "{DarkRed}[PoncikMarket]{Default}";

    public bool DefaultHudEnabled { get; set; } = true;
    public bool DefaultKeyOverlayEnabled { get; set; } = true;
    public bool DefaultJumpsEnabled { get; set; } = true;
    public string DefaultColor { get; set; } = "Green";

    public int SpeedUnit { get; set; } = 0; 
    public bool DefaultShowRoundStats { get; set; } = true; 
    public float CommandCooldownSeconds { get; set; } = 3.0f;

    public bool TopSpeedEnabled { get; set; } = true;
    
    // MEVCUT (TopSpeed Bilgisi)
    public int HelpMessageIntervalMinutes { get; set; } = 4; 
    
    // YENİ (Speedometer/Hud Bilgisi)
    public int SpeedometerHelpIntervalMinutes { get; set; } = 5; 

    public string AdminFlag { get; set; } = "@css/ban"; 

    public List<string> SpeedMeterCommands { get; set; } = new() { "speedmeter", "myspeed" };
    public List<string> EditSpeedMeterCommands { get; set; } = new() { "speedmeteredit", "myspeededit", "editspeedmeter" };

    public List<string> CmdTopSpeed { get; set; } = new() { "topspeed", "topspeeds" };
    public List<string> CmdTopSpeedMap { get; set; } = new() { "topspeedmap" };
    public List<string> CmdTopSpeedTop { get; set; } = new() { "topspeedtop" };
    public List<string> CmdTopSpeedPr { get; set; } = new() { "topspeedpr" };
    public List<string> CmdTopSpeedHelp { get; set; } = new() { "topspeedhelp" };
    
    public List<string> CmdAdminList { get; set; } = new() { "listtopspeed" };
    public List<string> CmdAdminMenu { get; set; } = new() { "topspeedadmin" };
    public List<string> CmdAdminReset { get; set; } = new() { "topspeedreset" };
    public List<string> CmdAdminResetAll { get; set; } = new() { "topspeedresetall" };
    public List<string> CmdAdminDelete { get; set; } = new() { "topspeeddelete" };
    public List<string> CmdAdminDeleteAll { get; set; } = new() { "topspeeddeleteall" };
}
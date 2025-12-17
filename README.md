<p align="center">
  <img src="https://camo.githubusercontent.com/15411371090ab808f4644366f83bf2871aeacd208b2879fbe87f562a20150a3d/68747470733a2f2f70616e2e73616d7979632e6465762f732f56596d4d5845" />
</p>

A high-performance, feature-rich Speedometer and TopSpeed Record tracking plugin for Counter-Strike 2, built on the Swiftly framework.

Designed to be Lag-Free, it tracks player speeds, saves records to a database, and sends notifications via Discord Webhooks without impacting server performance.

Features
Optimized HUD — Specially optimized speedometer (25ms update rate) ensures fluid visuals without causing "recv queue overflow" or server lag.

Detailed Statistics — Real-time Speed (km/h, mph, u/s), Key Overlay (WASD), Jump Stats, and Round End rankings.

Advanced Record System — Tracks Global, Map-Specific, and Personal records instantly.

Admin Management Panel — Delete records, search players, and edit Speed/Time values directly in-game with instant RAM & DB synchronization.

Discord Webhook Integration — Sends stylish notifications when a new record is set. Uses a Smart Search system to locate the payload.json configuration automatically.

Multi-Language Support — Fully customizable messages via tr.jsonc and en.jsonc.

Customizable — Players can toggle HUD elements and change colors individually.

Requirements
SwiftlyS2

MySQL or MariaDB Database

Installation
Download/Compile the Extended-Speedmeter.dll.

Create a folder named Extended-Speedmeter inside addons/swiftlys2/plugins/.

Upload the DLL and the resources folder (containing translations and payload.json) into that directory.

addons/swiftlys2/plugins/Extended-Speedmeter/
    ├── Extended-Speedmeter.dll
    └── resources/
        ├── payload.json
        └── translations/
Start the server to generate the configuration file.

Configuration
The plugin automatically creates a configuration file (Extended-Speedmeter.json) in the configs directory.

DatabaseConnection — The Database ID defined in your Swiftly core.json.

Prefix — Chat prefix for plugin messages.

DefaultHudEnabled — Whether the HUD is on by default for new players.

SpeedUnit — Unit of measurement: 0 (u/s), 1 (km/h), 2 (mph), 3 (m/s).

DiscordWebhookUrl — Your Discord Webhook link for record notifications.

AdminFlag — The permission flag required to access the Admin Menu.

Example Config:
JSON

{
  "DatabaseConnection": "speedometer_db",
  "Prefix": "{DarkRed}[Extended-Speedmeter]{Default}",
  "DefaultHudEnabled": true,
  "DefaultKeyOverlayEnabled": true,
  "DefaultJumpsEnabled": true,
  "DefaultColor": "Green",
  "SpeedUnit": 0,
  "DefaultShowRoundStats": true,
  "CommandCooldownSeconds": 3.0,
  "TopSpeedEnabled": true,
  "HelpMessageIntervalMinutes": 4,
  "SpeedometerHelpIntervalMinutes": 5,
  "DiscordWebhookUrl": "https://discord.com/api/webhooks/...",
  "AdminFlag": "@css/ban"
}
Commands
Player Commands
!speedmeter or !myspeed: Toggles the Speedometer HUD on/off.

!speedmeteredit: Opens the HUD, Color, and Overlay settings menu.

!topspeed: Shows records of currently online players.

!topspeedmap: Shows the top records for the current map.

!topspeedtop: Shows the all-time global top records.

!topspeedpr: Lists your personal records.

!topspeedmaplist: Displays a list of all recorded maps.

!topspeedhelp: Shows help messages for commands.

Admin Commands
!topspeedadmin: Opens the Management Panel to delete, edit, or reset records.

Database Structure
The plugin automatically creates the necessary tables:

speedometer_data: Stores player preferences (HUD toggles, colors).

topspeed_records: Stores Map, Speed, Reach Time, and Player info.

Author
PoncikMarket (Discord: poncikmarket)

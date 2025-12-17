<p align="center">
  <img src="https://camo.githubusercontent.com/15411371090ab808f4644366f83bf2871aeacd208b2879fbe87f562a20150a3d/68747470733a2f2f70616e2e73616d7979632e6465762f732f56596d4d5845" />
</p>

# Extended-Speedmeter

A **high-performance**, **feature-rich** Speedometer and TopSpeed Record tracking plugin for **Counter-Strike 2**, built on the **SwiftlyS2 framework**.

Designed to be **lag-free**, it tracks player speeds, saves records to a database, and sends notifications via **Discord Webhooks** — all **without impacting server performance**.


## 🚀 Features

### ⚡ Optimized HUD
- Specially optimized speedometer with a **25ms update rate**
- Ensures smooth visuals without causing **recv queue overflow** or server lag

### 📊 Detailed Statistics
- Real-time speed display: **km/h, mph, u/s**
- **Key Overlay (WASD)**
- **Jump statistics**
- **End-of-round rankings**

### 🏆 Advanced Record System
- Tracks **Global**, **Map-Specific**, and **Personal** records instantly
- High-speed in-memory + database synchronization

### 🛠 Admin Management Panel
- Delete records
- Search players
- Edit **Speed / Time** values
- All changes sync instantly with **RAM & Database**

### 🔔 Discord Webhook Integration
- Sends stylish notifications when a new record is set
- Uses a **Smart Search** system to automatically locate `payload.json`

### 🌍 Multi-Language Support
- Fully customizable messages
- Uses:
  - `en.jsonc`
  - `tr.jsonc`
  - `ar.jsonc`
  - `de.jsonc`
  - `es.jsonc`
  - `fr.jsonc`
  - `pl.jsonc`
  - `pt-BR.jsonc`
  - `ru.jsonc`
  - `zh-CN.jsonc`

### 🎨 Customizable HUD
- Players can:
  - Toggle HUD elements
  - Change colors individually


## Requirements
- [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2)

## Configuration
The plugin creates a JSONC configuration file (`Extended-Speedmeter.json`) in the configs directory.

- **DatabaseConnection** — The Database ID defined in your Swiftly `core.json`.
- **Prefix** — Chat prefix for plugin messages.
- **DefaultHudEnabled** — Whether the HUD is on by default for new players.
- **SpeedUnit** — Unit of measurement: `0` (u/s), `1` (km/h), `2` (mph), `3` (m/s).
- **DiscordWebhookUrl** — Your Discord Webhook link for record notifications.
- **AdminFlag** — The permission flag required to access the Admin Menu.

### Example Config:
```bash
jsonc{
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
}
```
## Commands

- `!speedmeter` or `!myspeed`: Toggles the Speedometer HUD on/off.
- `!speedmeteredit`: Opens the HUD, Color, and Overlay settings menu.
- `!topspeed`: Shows records of currently online players.
- `!topspeedmap`: Shows the top records for the current map.
- `!topspeedtop`: Shows the all-time global top records.
- `!topspeedpr`: Lists your personal records.
- `!topspeedmaplist`: Displays a list of all recorded maps.
- `!topspeedhelp`: Shows help messages for commands.

### Admin Commands

- `!topspeedadmin`: Opens the **Management Panel** to delete, edit, or reset records.

## Database Structure
The plugin automatically creates the necessary tables:
- `speedometer_data`: Stores player preferences (HUD toggles, colors).
- `topspeed_records`: Stores Map, Speed, Reach Time, and Player info.

## Author
- PoncikMarket (Discord: `poncikmarket`)

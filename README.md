# AFKManager
Simple AFK Manager plugin for CS2 (ideal for retake servers, as it doesn't track player position or camera angle but instead checks if the player is actively using game controls such as WASD, SHIFT, clicks, etc).

## Installation
1. Install [CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp) and [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master).

2. Download [AFKManager.zip](https://github.com/wiruwiru/AFKManager/releases) from the releases section.

3. Unzip the archive and upload it to the game server.

4. Start the server and wait for the configuration file to be generated.

5. Edit the configuration file with the parameters of your choice.

## Config
The configuration file will be automatically generated when the plugin is first loaded. Below are the parameters you can customize:

| Parameter                | Description                                                                                       |
|--------------------------|---------------------------------------------------------------------------------------------------|
| `AfkPunishAfterWarnings` | Number of warnings to issue before applying the punishment type (set to 0 to disable AFK feature).|
| `AfkPunishment`          | Punishment type (0 - kill, 1 - kill + move to spectator, 2 - kick).                               |
| `AfkWarnInterval`        | Issue a warning every X seconds for AFK.                                                          |
| `SpecWarnInterval`       | Issue a warning every X seconds for AFK while in spectator mode.                                  |
| `SpecKickAfterWarnings`  | Kick the player after X warnings are issued (set to 0 to disable).                                |
| `SpecKickMinPlayers`     | Minimum number of players required to kick.                                                       |
| `SpecKickOnlyMovedByPlugin` | Only check players in spectator mode who were moved by AFK Manager.                            |
| `SpecSkipFlag`           | Skip players in spectator mode with this flag during AFK verification.                            |
| `AfkSkipFlag`            | Skip players with this flag during AFK verification.                                              |
| `PlaySoundName`          | Play a sound after a warning is issued (leave empty to disable).                                  |
| `SkipWarmup`             | Skip checks during warmup.                                                                        |
| `Timer`                  | Adjust the timer for player checks (recommended to set to 1 second).                              |
| `isCSSPanel`             | Enable this if you are using CSSPanel; otherwise skip flags may not work correctly.               |
| `EnableDebug`            | Enable debug messages in the console for troubleshooting purposes.                                |

## Configuration Example
Here is an example configuration file:
```json
{
  "AfkPunishAfterWarnings": 2,
  "AfkPunishment": 1,
  "AfkWarnInterval": 60,
  "SpecWarnInterval": 60,
  "SpecKickAfterWarnings": 2,
  "SpecKickMinPlayers": 5,
  "SpecKickOnlyMovedByPlugin": false,
  "SpecSkipFlag": [
    "@css/root",
    "@css/generic"
  ],
  "AfkSkipFlag": [
    "@css/root"
  ],
  "PlaySoundName": "ui/panorama/popup_reveal_01",
  "SkipWarmup": false,
  "Timer": 1,
  "isCSSPanel": false,
  "EnableDebug": false,
  "ConfigVersion": 1
}
```

###
This project is a modification of [AFKManager](https://github.com/NiGHT757/AFKManager) by [NiGHT757](https://github.com/NiGHT757)
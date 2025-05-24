using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using AFKManager.Config;
using AFKManager.Models;
using AFKManager.Utils;

namespace AFKManager.Services;

public class AFKDetectionService
{
    private readonly AFKManagerConfig _config;
    private readonly PermissionService _permissionService;
    private readonly PunishmentService _punishmentService;

    public AFKDetectionService(
        AFKManagerConfig config,
        PermissionService permissionService,
        PunishmentService punishmentService)
    {
        _config = config;
        _permissionService = permissionService;
        _punishmentService = punishmentService;
    }

    public void ProcessPlayerAFK(CCSPlayerController player, PlayerInfo data, int playersCount)
    {
        if (player.ControllingBot) return;

        DebugUtils.Log($"[AFK DEBUG] Checking player {player.PlayerName} (Team: {player.Team})");

        var playerFlags = player.Pawn.Value!.Flags;
        if ((playerFlags & ((uint)PlayerFlags.FL_ONGROUND | (uint)PlayerFlags.FL_FROZEN)) != (uint)PlayerFlags.FL_ONGROUND)
        {
            DebugUtils.Log($"[AFK DEBUG] Skipping - Player not on ground or frozen (Flags: {playerFlags})");
            return;
        }

        if (InputUtils.IsPressingAnyKey(player))
        {
            DebugUtils.Log($"[AFK DEBUG] Player active - Resetting counters (Was: Time={data.AfkTime}, Warnings={data.AfkWarningCount})");
            data.AfkTime = 0;
            if (data.AfkWarningCount > 0) data.AfkWarningCount = 0;
            return;
        }

        if (_config.AfkPunishAfterWarnings == 0 || _config.AfkSkipFlag.Count < 1) return;
        if (playersCount < _config.AfkKickMinPlayers) return;

        _permissionService.LogPermissionDebugInfo(player, _config.AfkSkipFlag, "AFK");
        if (_permissionService.HasSkipPermission(player, _config.AfkSkipFlag))
        {
            DebugUtils.Log($"[AFK DEBUG] Skipping - Player has required permissions");
            return;
        }

        ProcessAFKPunishment(player, data);
    }

    private void ProcessAFKPunishment(CCSPlayerController player, PlayerInfo data)
    {
        data.AfkTime += _config.Timer;
        DebugUtils.Log($"[AFK DEBUG] Player inactive - Added {_config.Timer}s (Total: {data.AfkTime}/{_config.AfkWarnInterval})");

        if (data.AfkTime < _config.AfkWarnInterval) return;

        data.AfkWarningCount++;
        DebugUtils.Log($"[AFK DEBUG] Warning #{data.AfkWarningCount}/{_config.AfkPunishAfterWarnings}");

        if (data.AfkWarningCount >= _config.AfkPunishAfterWarnings)
        {
            _punishmentService.ApplyAFKPunishment(player, data);
        }
        else
        {
            _punishmentService.SendAFKWarning(player, data);
        }
    }
}
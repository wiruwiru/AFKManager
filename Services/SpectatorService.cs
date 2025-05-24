using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using AFKManager.Config;
using AFKManager.Models;
using AFKManager.Utils;

namespace AFKManager.Services;

public class SpectatorService
{
    private readonly AFKManagerConfig _config;
    private readonly PermissionService _permissionService;
    private readonly IStringLocalizer _localizer;

    public SpectatorService(AFKManagerConfig config, PermissionService permissionService, IStringLocalizer localizer)
    {
        _config = config;
        _permissionService = permissionService;
        _localizer = localizer;
    }

    public void ProcessSpectator(CCSPlayerController player, PlayerInfo data, int playersCount)
    {
        if (_config.SpecKickAfterWarnings == 0 || player.TeamNum != 1 || playersCount < _config.SpecKickMinPlayers)
            return;

        DebugUtils.Log($"[SPEC DEBUG] Checking spectator {player.PlayerName}");

        if (_config.SpecKickOnlyMovedByPlugin && !data.MovedByPlugin)
        {
            DebugUtils.Log($"[SPEC DEBUG] Skipping - Player not moved by plugin");
            return;
        }

        if (_config.SpecSkipFlag.Count >= 1)
        {
            _permissionService.LogPermissionDebugInfo(player, _config.SpecSkipFlag, "SPEC");
            if (_permissionService.HasSkipPermission(player, _config.SpecSkipFlag))
            {
                DebugUtils.Log($"[SPEC DEBUG] Skipping - Player has required permissions");
                return;
            }
        }

        if (InputUtils.IsPressingAnyKey(player))
        {
            DebugUtils.Log($"[SPEC DEBUG] Spectator active - Resetting counters (Was: Time={data.SpecAfkTime}, Warnings={data.SpecWarningCount})");
            data.SpecAfkTime = 0;
            data.SpecWarningCount = 0;
            return;
        }

        ProcessSpectatorPunishment(player, data);
    }

    private void ProcessSpectatorPunishment(CCSPlayerController player, PlayerInfo data)
    {
        data.SpecAfkTime += _config.Timer;
        DebugUtils.Log($"[SPEC DEBUG] Spectator inactive - Added {_config.Timer}s (Total: {data.SpecAfkTime}/{_config.SpecWarnInterval})");

        if (data.SpecAfkTime < _config.SpecWarnInterval) return;

        data.SpecWarningCount++;
        DebugUtils.Log($"[SPEC DEBUG] Warning #{data.SpecWarningCount}/{_config.SpecKickAfterWarnings}");

        if (data.SpecWarningCount >= _config.SpecKickAfterWarnings)
        {
            DebugUtils.Log($"[SPEC DEBUG] Kicking spectator");
            Server.PrintToChatAll(StringUtils.ReplaceVars(player, _localizer["ChatKickMessage"].Value, _localizer));
            Server.ExecuteCommand($"kickid {player.UserId}");

            data.SpecWarningCount = 0;
            data.SpecAfkTime = 0;
        }
        else
        {
            DebugUtils.Log($"[SPEC DEBUG] Sending warning to spectator");
            player.PrintToChat(StringUtils.ReplaceVars(player, _localizer["ChatWarningKickMessage"].Value, _localizer,
                _config.SpecKickAfterWarnings * _config.SpecWarnInterval - data.SpecWarningCount * _config.SpecWarnInterval));

            data.SpecAfkTime = 0;
        }
    }
}
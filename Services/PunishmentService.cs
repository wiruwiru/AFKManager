using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using AFKManager.Config;
using AFKManager.Models;
using AFKManager.Utils;

namespace AFKManager.Services;

public class PunishmentService
{
    private readonly AFKManagerConfig _config;
    private readonly IStringLocalizer _localizer;

    public PunishmentService(AFKManagerConfig config, IStringLocalizer localizer)
    {
        _config = config;
        _localizer = localizer;
    }

    public void ApplyAFKPunishment(CCSPlayerController player, PlayerInfo data)
    {
        DebugUtils.Log($"[AFK DEBUG] Applying punishment {_config.AfkPunishment} to player");

        switch (_config.AfkPunishment)
        {
            case 0:
                Server.PrintToChatAll(StringUtils.ReplaceVars(player, _localizer["ChatKillMessage"].Value, _localizer));
                player.Pawn.Value?.CommitSuicide(false, true);
                break;
            case 1:
                Server.PrintToChatAll(StringUtils.ReplaceVars(player, _localizer["ChatMoveMessage"].Value, _localizer));
                player.Pawn.Value?.CommitSuicide(false, true);
                player.ChangeTeam(CsTeam.Spectator);
                data.MovedByPlugin = true;
                break;
            case 2:
                Server.PrintToChatAll(StringUtils.ReplaceVars(player, _localizer["ChatKickMessage"].Value, _localizer));
                Server.ExecuteCommand($"kickid {player.UserId}");
                break;
        }

        data.AfkWarningCount = 0;
        data.AfkTime = 0;
    }

    public void SendAFKWarning(CCSPlayerController player, PlayerInfo data)
    {
        DebugUtils.Log($"[AFK DEBUG] Sending warning to player");

        switch (_config.AfkPunishment)
        {
            case 0:
                player.PrintToChat(StringUtils.ReplaceVars(player, _localizer["ChatWarningKillMessage"].Value, _localizer,
                    _config.AfkPunishAfterWarnings * _config.AfkWarnInterval - data.AfkWarningCount * _config.AfkWarnInterval));
                break;
            case 1:
                player.PrintToChat(StringUtils.ReplaceVars(player, _localizer["ChatWarningMoveMessage"].Value, _localizer,
                    _config.AfkPunishAfterWarnings * _config.AfkWarnInterval - data.AfkWarningCount * _config.AfkWarnInterval));
                break;
            case 2:
                player.PrintToChat(StringUtils.ReplaceVars(player, _localizer["ChatWarningKickMessage"].Value, _localizer,
                    _config.AfkPunishAfterWarnings * _config.AfkWarnInterval - data.AfkWarningCount * _config.AfkWarnInterval));
                break;
        }

        if (!string.IsNullOrEmpty(_config.PlaySoundName))
        {
            DebugUtils.Log($"[AFK DEBUG] Playing sound: {_config.PlaySoundName}");
            player.ExecuteClientCommand($"play {_config.PlaySoundName}");
        }

        data.AfkTime = 0;
    }
}
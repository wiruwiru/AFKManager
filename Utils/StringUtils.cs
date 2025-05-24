using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace AFKManager.Utils;

public static class StringUtils
{
    public static string ReplaceVars(CCSPlayerController player, string message, IStringLocalizer localizer, float timeAmount = 0.0f)
    {
        message ??= "";

        string prefix = localizer["ChatPrefix"].Value;

        return $"{prefix}{message}"
            .Replace("{playerName}", player.PlayerName)
            .Replace("{timeAmount}", $"{timeAmount:F1}");
    }
}
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace AFKManager.Utils;

public static class StringUtils
{
    public static string ReplaceVars(CCSPlayerController player, string message, IStringLocalizer localizer, float timeAmount = 0.0f)
    {
        // Asegurarse de que el mensaje no sea nulo
        message ??= "";

        // Obtener el prefijo del localizador
        string prefix = localizer["ChatPrefix"].Value;

        // Combinar prefijo y mensaje, reemplazando variables
        return $"{prefix}{message}"
            .Replace("{playerName}", player.PlayerName)
            .Replace("{timeAmount}", $"{timeAmount:F1}");
    }
}
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace AFKManager.Utils;

public static class InputUtils
{
    public static bool IsPressingAnyKey(CCSPlayerController player)
    {
        return player.Buttons.HasFlag(PlayerButtons.Attack)
               || player.Buttons.HasFlag(PlayerButtons.Jump)
               || player.Buttons.HasFlag(PlayerButtons.Duck)
               || player.Buttons.HasFlag(PlayerButtons.Forward)
               || player.Buttons.HasFlag(PlayerButtons.Back)
               || player.Buttons.HasFlag(PlayerButtons.Use)
               || player.Buttons.HasFlag(PlayerButtons.Left)
               || player.Buttons.HasFlag(PlayerButtons.Right)
               || player.Buttons.HasFlag(PlayerButtons.Moveleft)
               || player.Buttons.HasFlag(PlayerButtons.Moveright)
               || player.Buttons.HasFlag(PlayerButtons.Attack2)
               || player.Buttons.HasFlag(PlayerButtons.Run)
               || player.Buttons.HasFlag(PlayerButtons.Reload)
               || player.Buttons.HasFlag(PlayerButtons.Speed)
               || player.Buttons.HasFlag(PlayerButtons.Walk);
    }
}
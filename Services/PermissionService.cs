using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using AFKManager.Config;

namespace AFKManager.Services;

public class PermissionService
{
    private readonly AFKManagerConfig _config;

    public PermissionService(AFKManagerConfig config)
    {
        _config = config;
    }

    public bool HasSkipPermission(CCSPlayerController player, List<string> flagsToCheck)
    {
        if (_config.isCSSPanel)
        {
            var adminData = AdminManager.GetPlayerAdminData(player);
            return flagsToCheck.Any(flag =>
                AdminManager.PlayerHasPermissions(player, flag) ||
                (adminData?.Groups?.Contains(flag) == true));
        }
        return AdminManager.PlayerHasPermissions(player, flagsToCheck.ToArray());
    }

    public void LogPermissionDebugInfo(CCSPlayerController player, List<string> flagsToCheck, string flagType)
    {
        if (!_config.EnableDebug) return;

        var adminData = AdminManager.GetPlayerAdminData(player);
        var allFlags = adminData?.Flags?.SelectMany(kv => kv.Value.Select(flag => $"{kv.Key}:{flag}")).ToList() ?? new List<string>();
        var allGroups = adminData?.Groups?.ToList() ?? new List<string>();

        Console.WriteLine($"================================= [ AFKManager ] =================================");
        Console.WriteLine($"[PERM DEBUG] Player: {player.PlayerName} | " +
                         $"All Flags: {string.Join(", ", allFlags)} | " +
                         $"All Groups: {string.Join(", ", allGroups)} | " +
                         $"{flagType}SkipFlag: {string.Join(", ", flagsToCheck)}");
        Console.WriteLine("=========================================================================================");
    }
}
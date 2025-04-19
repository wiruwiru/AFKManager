using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace AFKManager;

public class AFKManager : BasePlugin, IPluginConfig<AFKManagerConfig>
{
    #region definitions
    public override string ModuleAuthor => "luca.uy (forked from NiGHT)";
    public override string ModuleName => "AFK Manager";
    public override string ModuleVersion => "1.0.7";

    public required AFKManagerConfig Config { get; set; }
    private CCSGameRules? _gGameRulesProxy;

    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        if (!_gPlayerInfo.TryGetValue(player.Index, out var value))
            return HookResult.Continue;

        if (@event.Team != 1)
            value.MovedByPlugin = false;

        return HookResult.Continue;
    }

    public void OnConfigParsed(AFKManagerConfig config)
    {
        Config = config;
        Utils.Config = config;

        if (Config.AfkPunishment is < 0 or > 2)
        {
            Config.AfkPunishment = 1;
        }

        if (Config.Timer < 0.1f)
        {
            Config.Timer = 1.0f;
        }

        if (Config.SpecWarnInterval < Config.Timer)
        {
            Config.SpecWarnInterval = Config.Timer;
        }

        AddTimer(Config.Timer, AfkTimer_Callback, TimerFlags.REPEAT);
    }

    private class PlayerInfo
    {
        public float AfkTime { get; set; }
        public int AfkWarningCount { get; set; }
        public int SpecWarningCount { get; set; }
        public float SpecAfkTime { get; set; }
        public bool MovedByPlugin { get; set; }
    }

    private readonly Dictionary<uint, PlayerInfo> _gPlayerInfo = new();
    #endregion

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            Server.NextFrame(() =>
            {
                _gGameRulesProxy =
                    Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules ??
                    throw new Exception("Failed to find game rules proxy entity.");
            });

        });

        RegisterListener<Listeners.OnMapEnd>(() =>
        {
            _gPlayerInfo.Clear();
        });

        #region OnClientConnected
        RegisterListener<Listeners.OnClientConnected>(playerSlot =>
        {
            var finalSlot = (uint)playerSlot + 1;

            if (_gPlayerInfo.ContainsKey(finalSlot))
                return;

            _gPlayerInfo.Add(finalSlot, new PlayerInfo());
        });

        RegisterListener<Listeners.OnClientDisconnectPost>(playerSlot =>
        {
            _gPlayerInfo.Remove((uint)playerSlot + 1);
        });
        #endregion
        #region hotReload
        if (hotReload)
        {
            AddTimer(1.0f, () =>
            {
                var players = Utilities.GetPlayers().Where(x => x is { IsBot: false, Connected: PlayerConnectedState.PlayerConnected });

                foreach (var player in players)
                {
                    var i = player.Index;

                    if (!_gPlayerInfo.ContainsKey(i))
                    {
                        _gPlayerInfo.Add(i, new PlayerInfo());
                    }
                }

                _gGameRulesProxy =
                    Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules ??
                        throw new Exception("Failed to find game rules proxy entity on hotReload.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }
        #endregion
        AddCommandListener("spec_mode", OnCommandListener);
        AddCommandListener("spec_next", OnCommandListener);
    }

    private HookResult OnCommandListener(CCSPlayerController? player, CommandInfo commandInfo)
    {
        return HookResult.Continue;
    }

    private void AfkTimer_Callback()
    {
        if (_gGameRulesProxy == null || _gGameRulesProxy.FreezePeriod || (Config.SkipWarmup && _gGameRulesProxy.WarmupPeriod))
            return;

        var players = Utilities.GetPlayers().Where(x => x is { IsBot: false, Connected: PlayerConnectedState.PlayerConnected }).ToList();
        var playersCount = players.Count;

        foreach (var player in players)
        {
            if (player.ControllingBot || !_gPlayerInfo.TryGetValue(player.Index, out var data))
                continue;

            #region AFK Time

            if (playersCount >= Config.AfkKickMinPlayers)
            {
                if (player is { LifeState: (byte)LifeState_t.LIFE_ALIVE, Team: CsTeam.Terrorist or CsTeam.CounterTerrorist })
                {
                    Utils.DebugMessage($"[AFK DEBUG] Checking player {player.PlayerName} (Team: {player.Team})");

                    var playerFlags = player.Pawn.Value!.Flags;
                    if ((playerFlags & ((uint)PlayerFlags.FL_ONGROUND | (uint)PlayerFlags.FL_FROZEN)) != (uint)PlayerFlags.FL_ONGROUND)
                    {
                        Utils.DebugMessage($"[AFK DEBUG] Skipping - Player not on ground or frozen (Flags: {playerFlags})");
                        continue;
                    }

                    bool isPressingKey = player.Buttons.HasFlag(PlayerButtons.Attack)
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

                    Utils.DebugMessage($"[AFK DEBUG] Player {player.PlayerName} Buttons: {player.Buttons}, PressingKey: {isPressingKey}");

                    if (isPressingKey)
                    {
                        Utils.DebugMessage($"[AFK DEBUG] Player active - Resetting counters (Was: Time={data.AfkTime}, Warnings={data.AfkWarningCount})");
                        data.AfkTime = 0;
                        if (data.AfkWarningCount > 0)
                        {
                            data.AfkWarningCount = 0;
                        }
                        continue;
                    }

                    if (Config.AfkPunishAfterWarnings != 0
                        && !(Config.AfkSkipFlag.Count >= 1 && AdminManager.PlayerHasPermissions(player, Config.AfkSkipFlag.ToArray())))
                    {
                        data.AfkTime += Config.Timer;
                        Utils.DebugMessage($"[AFK DEBUG] Player inactive - Added {Config.Timer}s (Total: {data.AfkTime}/{Config.AfkWarnInterval})");

                        if (data.AfkTime >= Config.AfkWarnInterval)
                        {
                            data.AfkWarningCount++;
                            Utils.DebugMessage($"[AFK DEBUG] Warning #{data.AfkWarningCount}/{Config.AfkPunishAfterWarnings}");

                            if (data.AfkWarningCount >= Config.AfkPunishAfterWarnings)
                            {
                                Utils.DebugMessage($"[AFK DEBUG] Applying punishment {Config.AfkPunishment} to player");
                                switch (Config.AfkPunishment)
                                {
                                    case 0:
                                        Server.PrintToChatAll(ReplaceVars(player, Localizer["ChatKillMessage"].Value));
                                        player.Pawn.Value?.CommitSuicide(false, true);
                                        break;
                                    case 1:
                                        Server.PrintToChatAll(ReplaceVars(player, Localizer["ChatMoveMessage"].Value));
                                        player.Pawn.Value?.CommitSuicide(false, true);
                                        player.ChangeTeam(CsTeam.Spectator);
                                        data.MovedByPlugin = true;
                                        break;
                                    case 2:
                                        Server.PrintToChatAll(ReplaceVars(player, Localizer["ChatKickMessage"].Value));
                                        Server.ExecuteCommand($"kickid {player.UserId}");
                                        break;
                                }

                                data.AfkWarningCount = 0;
                                data.AfkTime = 0;
                            }
                            else
                            {
                                Utils.DebugMessage($"[AFK DEBUG] Sending warning to player");
                                switch (Config.AfkPunishment)
                                {
                                    case 0:
                                        player.PrintToChat(ReplaceVars(player, Localizer["ChatWarningKillMessage"].Value,
                                            Config.AfkPunishAfterWarnings * Config.AfkWarnInterval - data.AfkWarningCount * Config.AfkWarnInterval));
                                        break;
                                    case 1:
                                        player.PrintToChat(ReplaceVars(player, Localizer["ChatWarningMoveMessage"].Value,
                                            Config.AfkPunishAfterWarnings * Config.AfkWarnInterval - data.AfkWarningCount * Config.AfkWarnInterval));
                                        break;
                                    case 2:
                                        player.PrintToChat(ReplaceVars(player, Localizer["ChatWarningKickMessage"].Value,
                                            Config.AfkPunishAfterWarnings * Config.AfkWarnInterval - data.AfkWarningCount * Config.AfkWarnInterval));
                                        break;
                                }

                                if (!string.IsNullOrEmpty(Config.PlaySoundName))
                                {
                                    Utils.DebugMessage($"[AFK DEBUG] Playing sound: {Config.PlaySoundName}");
                                    player.ExecuteClientCommand($"play {Config.PlaySoundName}");
                                }

                                data.AfkTime = 0;
                            }
                        }
                    }
                    continue;
                }
            }

            #endregion

            #region SPEC Time

            if (Config.SpecKickAfterWarnings != 0 && player.TeamNum == 1 && playersCount >= Config.SpecKickMinPlayers)
            {
                Utils.DebugMessage($"[SPEC DEBUG] Checking spectator {player.PlayerName}");

                if (Config.SpecKickOnlyMovedByPlugin && !data.MovedByPlugin)
                {
                    Utils.DebugMessage($"[SPEC DEBUG] Skipping - Player not moved by plugin");
                    continue;
                }

                if (Config.SpecSkipFlag.Count >= 1 && AdminManager.PlayerHasPermissions(player, Config.SpecSkipFlag.ToArray()))
                {
                    Utils.DebugMessage($"[SPEC DEBUG] Skipping - Player has special permissions");
                    continue;
                }

                bool isSpectPressingKey = player.Buttons.HasFlag(PlayerButtons.Attack)
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

                Utils.DebugMessage($"[SPEC DEBUG] Player {player.PlayerName} Buttons: {player.Buttons}, PressingKey: {isSpectPressingKey}");

                if (isSpectPressingKey)
                {
                    Utils.DebugMessage($"[SPEC DEBUG] Spectator active - Resetting counters (Was: Time={data.SpecAfkTime}, Warnings={data.SpecWarningCount})");
                    data.SpecAfkTime = 0;
                    data.SpecWarningCount = 0;
                    continue;
                }

                data.SpecAfkTime += Config.Timer;
                Utils.DebugMessage($"[SPEC DEBUG] Spectator inactive - Added {Config.Timer}s (Total: {data.SpecAfkTime}/{Config.SpecWarnInterval})");

                if (data.SpecAfkTime >= Config.SpecWarnInterval)
                {
                    data.SpecWarningCount++;
                    Utils.DebugMessage($"[SPEC DEBUG] Warning #{data.SpecWarningCount}/{Config.SpecKickAfterWarnings}");

                    if (data.SpecWarningCount >= Config.SpecKickAfterWarnings)
                    {
                        Utils.DebugMessage($"[SPEC DEBUG] Kicking spectator");
                        Server.PrintToChatAll(ReplaceVars(player, Localizer["ChatKickMessage"].Value));
                        Server.ExecuteCommand($"kickid {player.UserId}");

                        data.SpecWarningCount = 0;
                        data.SpecAfkTime = 0;
                        continue;
                    }

                    Utils.DebugMessage($"[SPEC DEBUG] Sending warning to spectator");
                    player.PrintToChat(ReplaceVars(player, Localizer["ChatWarningKickMessage"].Value,
                        Config.SpecKickAfterWarnings * Config.SpecWarnInterval - data.SpecWarningCount * Config.SpecWarnInterval));

                    data.SpecWarningCount++;
                    data.SpecAfkTime = 0;
                }
            }

            #endregion
        }
    }

    private static string GetTeamColor(CsTeam team)
    {
        return team switch
        {
            CsTeam.Spectator => ChatColors.Grey.ToString(),
            CsTeam.Terrorist => ChatColors.Red.ToString(),
            CsTeam.CounterTerrorist => ChatColors.Blue.ToString(),
            _ => ChatColors.Default.ToString()
        };
    }

    private string ReplaceVars(CCSPlayerController player, string message, float timeAmount = 0.0f)
    {
        return Localizer["ChatPrefix"] + message.Replace("{playerName}", player.PlayerName)
                      .Replace("{teamColor}", GetTeamColor(player.Team))
                      .Replace("{weaponName}", player.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value?.DesignerName ?? "Unknown")
                      .Replace("{timeAmount}", $"{timeAmount:F1}")
                      .Replace("{zoneName}", player.PlayerPawn?.Value?.LastPlaceName ?? "Unknown");
    }
}
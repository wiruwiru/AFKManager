using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using AFKManager.Config;
using AFKManager.Models;
using AFKManager.Services;
using AFKManager.Utils;

namespace AFKManager;

[MinimumApiVersion(300)]
public class AFKManager : BasePlugin, IPluginConfig<AFKManagerConfig>
{
    public override string ModuleAuthor => "luca.uy (forked from NiGHT)";
    public override string ModuleName => "AFK Manager";
    public override string ModuleVersion => "1.1.0";

    public required AFKManagerConfig Config { get; set; }
    private CCSGameRules? _gGameRulesProxy;

    private readonly Dictionary<uint, PlayerInfo> _gPlayerInfo = new();
    private AFKDetectionService? _afkDetectionService;
    private SpectatorService? _spectatorService;

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
        DebugUtils.Initialize(config);

        if (Config.AfkPunishment is < 0 or > 2)
            Config.AfkPunishment = 1;

        if (Config.Timer < 0.1f)
            Config.Timer = 1.0f;

        if (Config.SpecWarnInterval < Config.Timer)
            Config.SpecWarnInterval = Config.Timer;

        InitializeServices();
        AddTimer(Config.Timer, AfkTimer_Callback, TimerFlags.REPEAT);
    }

    private void InitializeServices()
    {
        var permissionService = new PermissionService(Config);
        var punishmentService = new PunishmentService(Config, Localizer);

        _afkDetectionService = new AFKDetectionService(Config, permissionService, punishmentService);
        _spectatorService = new SpectatorService(Config, permissionService, Localizer);
    }

    public override void Load(bool hotReload)
    {
        RegisterListeners();
        AddCommandListener("spec_mode", OnCommandListener);
        AddCommandListener("spec_next", OnCommandListener);

        if (hotReload)
        {
            AddTimer(1.0f, () =>
            {
                var players = Utilities.GetPlayers()
                    .Where(x => x is { IsBot: false, Connected: PlayerConnectedState.PlayerConnected });

                foreach (var player in players)
                {
                    var i = player.Index;
                    if (!_gPlayerInfo.ContainsKey(i))
                        _gPlayerInfo.Add(i, new PlayerInfo());
                }

                _gGameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
                    .First().GameRules ?? throw new Exception("Failed to find game rules proxy entity on hotReload.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }
    }

    private void RegisterListeners()
    {
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            Server.NextFrame(() =>
            {
                _gGameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
                    .First().GameRules ?? throw new Exception("Failed to find game rules proxy entity.");
            });
        });

        RegisterListener<Listeners.OnMapEnd>(() => _gPlayerInfo.Clear());

        RegisterListener<Listeners.OnClientConnected>(playerSlot =>
        {
            var finalSlot = (uint)playerSlot + 1;
            if (!_gPlayerInfo.ContainsKey(finalSlot))
                _gPlayerInfo.Add(finalSlot, new PlayerInfo());
        });

        RegisterListener<Listeners.OnClientDisconnectPost>(playerSlot => _gPlayerInfo.Remove((uint)playerSlot + 1));
    }

    private HookResult OnCommandListener(CCSPlayerController? player, CommandInfo commandInfo) =>
        HookResult.Continue;

    private void AfkTimer_Callback()
    {
        if (_gGameRulesProxy == null || _gGameRulesProxy.FreezePeriod ||
            (Config.SkipWarmup && _gGameRulesProxy.WarmupPeriod))
            return;

        var players = Utilities.GetPlayers()
            .Where(x => x is { IsBot: false, Connected: PlayerConnectedState.PlayerConnected })
            .ToList();

        var playersCount = players.Count;

        foreach (var player in players)
        {
            if (!_gPlayerInfo.TryGetValue(player.Index, out var data))
                continue;

            _afkDetectionService?.ProcessPlayerAFK(player, data, playersCount);
            _spectatorService?.ProcessSpectator(player, data, playersCount);
        }
    }
}
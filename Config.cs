using CounterStrikeSharp.API.Core;

namespace AFKManager;

public class AFKManagerConfig : BasePluginConfig
{
    public int AfkPunishAfterWarnings { get; set; } = 3;
    public int AfkPunishment { get; set; } = 1;
    public float AfkWarnInterval { get; set; } = 5.0f;
    public int AfkKickMinPlayers { get; set; } = 6;
    public float SpecWarnInterval { get; set; } = 20.0f;
    public int SpecKickAfterWarnings { get; set; } = 5;
    public int SpecKickMinPlayers { get; set; } = 8;
    public bool SpecKickOnlyMovedByPlugin { get; set; } = false;
    public List<string> SpecSkipFlag { get; set; } = [.. new[] { "@css/root", "@css/ban" }];
    public List<string> AfkSkipFlag { get; set; } = [.. new[] { "@css/root", "@css/ban" }];
    public string PlaySoundName { get; set; } = "sound/ui/beep22.wav";
    public bool SkipWarmup { get; set; } = false;
    public float Timer { get; set; } = 1.0f;
}
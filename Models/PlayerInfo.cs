namespace AFKManager.Models;

public class PlayerInfo
{
    public float AfkTime { get; set; }
    public int AfkWarningCount { get; set; }
    public int SpecWarningCount { get; set; }
    public float SpecAfkTime { get; set; }
    public bool MovedByPlugin { get; set; }
}
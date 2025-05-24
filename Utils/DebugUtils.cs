using AFKManager.Config;

namespace AFKManager.Utils;

public static class DebugUtils
{
    private static AFKManagerConfig? _config;

    public static void Initialize(AFKManagerConfig config)
    {
        _config = config;
    }

    public static void Log(string message)
    {
        if (_config?.EnableDebug != true) return;

        Console.WriteLine($"================================= [ AFKManager ] =================================");
        Console.WriteLine(message);
        Console.WriteLine("=========================================================================================");
    }
}
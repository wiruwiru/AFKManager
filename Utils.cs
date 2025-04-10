namespace AFKManager;

public class Utils
{
    public static AFKManagerConfig? Config { get; set; }

    public static void DebugMessage(string message)
    {
        if (Config?.EnableDebug != true) return;
        Console.WriteLine($"================================= [ AFKManager ] =================================");
        Console.WriteLine(message);
        Console.WriteLine("=========================================================================================");
    }

}
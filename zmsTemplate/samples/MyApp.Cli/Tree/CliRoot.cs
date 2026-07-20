using System.CommandLine;

public static class CliRoot
{
    public static RootCommand Root { get; }

    static CliRoot()
    {
        Root = new RootCommand("CLI application");

        var infoCmd = new Command("info", "Show application info");
        infoCmd.SetAction(HandleInfo);
        Root.Add(infoCmd);
    }

    private static void HandleInfo(ParseResult ctx)
    {
        Console.WriteLine($"App: MyApp");
        Console.WriteLine($"Framework: {RuntimeInformation.FrameworkDescription}");
    }
}

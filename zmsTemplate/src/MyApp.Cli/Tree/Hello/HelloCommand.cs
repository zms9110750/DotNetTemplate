using System.CommandLine;

public static class HelloCommand
{
    public static Argument<string> NameArg { get; } = new("name") { Description = "Your name" };

    public static void Configure(Command cmd)
    {
        cmd.Add(NameArg);
        cmd.SetAction(Execute);
    }

    private static void Execute(ParseResult ctx)
    {
        Console.WriteLine($"hello {ctx.GetValue(NameArg)}");
    }
}

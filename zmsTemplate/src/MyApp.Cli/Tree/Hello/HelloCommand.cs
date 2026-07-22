using System.CommandLine;

public static class HelloCommand
{
    public static void Configure(Command cmd)
    {
        var nameArg = new Argument<string>("name") { Description = "你的名字" };
        cmd.Add(nameArg);
        cmd.SetAction(ctx => Execute(ctx, nameArg));
    }

    private static void Execute(ParseResult ctx, Argument<string> nameArg)
    {
        Console.WriteLine($"hello {ctx.GetValue(nameArg)}");
    }
}

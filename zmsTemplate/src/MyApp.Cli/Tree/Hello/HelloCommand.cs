using System.CommandLine;

public static class HelloCommand
{
    public static void Configure(Command cmd)
    {
        var nameArg = new Argument<string>("name");
        nameArg.Description = "你的名字";
        cmd.Add(nameArg);

        cmd.SetAction(ctx =>
        {
            Console.WriteLine($"hello {ctx.GetValue(nameArg)}");
        });
    }
}

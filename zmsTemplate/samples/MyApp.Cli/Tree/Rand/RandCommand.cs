using System.CommandLine;

public static class RandCommand
{
    public static Option<int> MinOpt { get; } = new("--min", "Minimum value") { DefaultValueFactory = _ => 0 };
    public static Option<int> MaxOpt { get; } = new("--max", "Maximum value") { DefaultValueFactory = _ => 100 };
    public static Option<int> CountOpt { get; } = new("--count", "Count of numbers (max 100)") { DefaultValueFactory = _ => 1 };

    static RandCommand()
    {
        CountOpt.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int>() > 100)
                result.AddError("--count cannot exceed 100");
        });
    }

    public static void Configure(Command cmd)
    {
        cmd.Add(MinOpt);
        cmd.Add(MaxOpt);
        cmd.Add(CountOpt);
        cmd.SetAction(Execute);

        var jankenCmd = new Command("janken", "Rock-Paper-Scissors vs computer");
        JankenCommand.Configure(jankenCmd);
        cmd.Add(jankenCmd);
    }

    private static void Execute(ParseResult ctx)
    {
        var min = ctx.GetValue(MinOpt);
        var max = ctx.GetValue(MaxOpt);
        var count = ctx.GetValue(CountOpt);
        var rng = Random.Shared;

        for (int i = 0; i < count; i++)
            Console.WriteLine(rng.Next(min, max + 1));
    }
}

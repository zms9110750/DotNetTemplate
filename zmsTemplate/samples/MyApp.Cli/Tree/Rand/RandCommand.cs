using System.CommandLine;

public static class RandCommand
{
    public static void Configure(Command cmd)
    {
        #region 选项
        var minOpt = new Option<int>("--min", "最小值") { DefaultValueFactory = _ => 0 };
        var maxOpt = new Option<int>("--max", "最大值") { DefaultValueFactory = _ => 100 };
        var countOpt = new Option<int>("--count", "生成数量（最多 100）") { DefaultValueFactory = _ => 1 };
        countOpt.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int>() > 100)
                result.AddError("--count 不能超过 100");
        });
        #endregion

        cmd.Add(minOpt);
        cmd.Add(maxOpt);
        cmd.Add(countOpt);
        cmd.SetAction(ctx => Execute(ctx, minOpt, maxOpt, countOpt));

        // 猜拳子命令
        var jankenCmd = new Command("janken", "猜拳（VS 电脑）");
        JankenCommand.Configure(jankenCmd);
        cmd.Add(jankenCmd);
    }

    private static void Execute(ParseResult ctx, Option<int> minOpt, Option<int> maxOpt, Option<int> countOpt)
    {
        var min = ctx.GetValue(minOpt);
        var max = ctx.GetValue(maxOpt);
        var count = ctx.GetValue(countOpt);
        var rng = Random.Shared;

        for (int i = 0; i < count; i++)
            Console.WriteLine(rng.Next(min, max + 1));
    }
}

public static class JankenCommand
{
    private static readonly Random _rng = Random.Shared;

    public static void Configure(Command cmd)
    {
        var handOpt = new Option<Janken>("--hand", "你出的拳") { DefaultValueFactory = _ => Janken.Guu };
        cmd.Add(handOpt);
        cmd.SetAction(ctx => Execute(ctx, handOpt));
    }

    private static void Execute(ParseResult ctx, Option<Janken> handOpt)
    {
        var player = ctx.GetValue(handOpt);
        var cpu = (Janken)_rng.Next(3);
        var playerIdx = (int)player;
        var cpuIdx = (int)cpu;
        string[] emoji = ["👊", "✂️", "✋"];

        Console.WriteLine($"  你出: {emoji[playerIdx]} {player}");
        Console.WriteLine($"电脑出: {emoji[cpuIdx]} {cpu}");

        if (playerIdx == cpuIdx)
            Console.WriteLine("结果: 平局 😐");
        else if ((playerIdx + 1) % 3 == cpuIdx)
            Console.WriteLine("结果: 你赢了 🎉");
        else
            Console.WriteLine("结果: 你输了 😞");
    }
}

using System.CommandLine;

public static class RandCommand
{
    public static void Configure(Command cmd)
    {
        var minOpt = new Option<int>("--min");
        minOpt.Description = "最小值（默认1）";

        var maxOpt = new Option<int>("--max");
        maxOpt.Description = "最大值（默认100）";

        var countOpt = new Option<int>("--count");
        countOpt.Description = "生成数量（默认1）";

        cmd.Add(minOpt);
        cmd.Add(maxOpt);
        cmd.Add(countOpt);

        cmd.SetAction(ctx =>
        {
            var min = ctx.GetValue(minOpt);
            var max = ctx.GetValue(maxOpt);
            var count = ctx.GetValue(countOpt);
            var rng = Random.Shared;
            for (int i = 0; i < count; i++)
                Console.WriteLine(rng.Next(min, max + 1));
        });
    }
}

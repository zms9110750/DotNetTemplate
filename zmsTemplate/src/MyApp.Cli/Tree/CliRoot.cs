using System.CommandLine;

public static class CliRoot
{
    public static RootCommand Root { get; }

    static CliRoot()
    {
        Root = new RootCommand("CLI application");

        // root 可选参数：任意字符串列表
        var items = new Argument<string[]>("items");
        Root.Add(items);
        Root.SetAction(ctx =>
        {
            foreach (var item in ctx.GetValue(items))
                Console.WriteLine(item);
        });

        // rand 子命令
        var randCmd = new Command("rand", "生成随机数");
        RandCommand.Configure(randCmd);
        Root.Add(randCmd);

        // hello 子命令
        var helloCmd = new Command("hello", "输出问候语");
        HelloCommand.Configure(helloCmd);
        Root.Add(helloCmd);
    }
}

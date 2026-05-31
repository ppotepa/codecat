namespace Codecat.Cli;

internal sealed record CliOptions(string RootPath, string OutputPath)
{
    public static CliOptions? Parse(string[] args)
    {
        var root = ".";
        var output = "concat.txt";

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help")
            {
                return null;
            }

            if (arg is "-o" or "--output")
            {
                if (i + 1 >= args.Length)
                {
                    return null;
                }

                output = args[++i];
                continue;
            }

            if (arg.StartsWith('-'))
            {
                return null;
            }

            root = arg;
        }

        return new CliOptions(root, output);
    }

    public static void PrintUsage()
    {
        Console.WriteLine("""
        codecat - concatenate source files into an LLM-friendly text container

        usage:
          codecat [root] [-o concat.txt]

        examples:
          codecat .
          codecat D:\Git\my-app -o out\concat.txt
        """);
    }
}

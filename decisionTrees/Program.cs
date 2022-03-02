using decisionTrees;
using CommandLine;
using System.Diagnostics;

await Parser.Default.ParseArguments<CommandLineOptions>(args)
    .MapResult(async (CommandLineOptions args) =>
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            using StreamReader reader = new(args.Path);

            var matrix = await ReadFileAsync(reader);
            var attributes = await CountAtributes(matrix);

            //attributes.Display();

            stopwatch.Stop();
            Console.WriteLine("Elapsed time in miliseconds: {0}", stopwatch.ElapsedMilliseconds);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return -3;
        }
    }, error => Task.FromResult(-1));


ValueTask<List<Attribute>> CountAtributes(List<object[]> matrix)
{
    List<Attribute> result = new();
    for (int i = 0; i < matrix[0].Length; i++)
        result.Add(new Attribute(i, matrix.Select(x => x[i])
            .GroupBy(x => x).ToDictionary(x => x.Key,
            y => y.Select(g => g).Sum(s => 1))));

    return new(result);
}

async ValueTask<List<object[]>> ReadFileAsync(StreamReader reader)
{
    var result = Enumerable.Empty<object[]>().ToList();

    while (!reader.EndOfStream)
    {
        var line = (await reader.ReadLineAsync() ?? "").Trim().Split(' ')
            .Select(s => s as object).ToArray();
        result.Add(line);
    }

    return result;
}

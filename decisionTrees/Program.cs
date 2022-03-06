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
            var attributes = await CountAttributes(matrix);

            attributes.Display();

            stopwatch.Stop();
            Console.WriteLine("Elapsed time in milliseconds: {0}", stopwatch.ElapsedMilliseconds);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return -3;
        }
    }, error => Task.FromResult(-1));

async ValueTask<List<Attribute>> CountAttributes(IReadOnlyList<object[]> matrix)
{
    List<Attribute> result = new();
    var attributesCount = matrix[0].Length;
    var decisions = matrix.Select(x => x[attributesCount - 1]).ToArray();

    var entropy = await Math.Info(decisions.GroupBy(o => o)
        .ToDictionary(grouping => grouping.Key, grouping => grouping.Select(o => o).Sum(s => 1))
        .Values.Select(i => (double) i / decisions.Length)
        .ToArray());
    
    for (var i = 0; i < attributesCount; i++)
    {
        var attribute = new Attribute(i, matrix.Select(x => x[i]).ToArray(), entropy);
        if (i != attributesCount - 1) await attribute.Calculate(decisions);
        result.Add(attribute);
    }
    return new(result);
}

async ValueTask<List<object[]>> ReadFileAsync(StreamReader reader)
{
    var result = Enumerable.Empty<object[]>().ToList();

    while (!reader.EndOfStream)
    {
        var line = (await reader.ReadLineAsync() ?? "").Trim().Split(' ').Select(s => s as object).ToArray();
        result.Add(line);
    }

    return result;
}
using CommandLine;
using System.Diagnostics;
using decisionTrees;
using Attribute = decisionTrees.Attribute;
using Math = decisionTrees.Math;

await Parser.Default.ParseArguments<CommandLineOptions>(args)
    .MapResult(async args =>
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            using StreamReader reader = new(args.Path ?? string.Empty);

            var matrix = await ReadFileAsync(reader);
            var attributes = await CountAttributes(matrix);

            var we = attributes.Where(x => x.Index != attributes.Count - 1).FirstOrDefault(x =>
                System.Math.Abs(x.GainRatioAnT - attributes.Max(attribute => attribute.GainRatioAnT)) < 0.000001);

            
            
            stopwatch.Stop();
            Console.WriteLine("Elapsed time in milliseconds: {0}", stopwatch.ElapsedMilliseconds);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return -3;
        }
    }, _ => Task.FromResult(-1));

async ValueTask<List<Attribute>> CountAttributes(IReadOnlyList<object[]> matrix)
{
    List<Attribute> result = new();
    var attributesCount = matrix[0].Length;
    var decisions = matrix.Select(x => x[attributesCount - 1]).ToArray();

    var entropy = await Math.Info(decisions.GroupBy(o => o)
        .ToDictionary(grouping => grouping.Key, grouping => grouping.Select(o => o).Sum(_ => 1))
        .Values.Select(i => (double) i / decisions.Length)
        .ToArray());

    for (var i = 0; i < attributesCount; i++)
    {
        var attribute = new Attribute(i, matrix.Select(x => x[i]).ToArray(), entropy);
        if (i != attributesCount - 1) await attribute.Calculate(decisions);
        result.Add(attribute);
    }

    return result;
}

async ValueTask<List<object[]>> ReadFileAsync(StreamReader reader)
{
    var result = Enumerable.Empty<object[]>().ToList();

    while (!reader.EndOfStream)
        result.Add((await reader.ReadLineAsync() ?? "").Trim().Split(' ').Select(s => s as object).ToArray());

    return result;
}

Task BuildTree(int index, int startedIndex, IReadOnlyList<Attribute> attributes)
{
    if (attributes.FirstOrDefault(x=>x.Index == startedIndex)!.GainRatioAnT != 0)
    {
        
    }
    else
    {
        
    }
    
    return Task.CompletedTask;
}

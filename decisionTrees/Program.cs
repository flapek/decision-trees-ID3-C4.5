using CommandLine;
using System.Diagnostics;
using System.Text;
using decisionTrees;
using Math = decisionTrees.Math;

StringBuilder stringBuilder = new();
var tabs = 0;
await Parser.Default.ParseArguments<CommandLineOptions>(args)
    .MapResult(async args =>
    {
        var stopwatch = Stopwatch.StartNew();
        using StreamReader reader = new(args.Path ?? string.Empty);
        var data = await ReadFileAsync(reader, args.Separator);
        await BuildTree(data);
        Console.WriteLine(stringBuilder.ToString());
        stopwatch.Stop();
        Console.WriteLine("Elapsed time in milliseconds: {0}", stopwatch.ElapsedMilliseconds);
        return 0;
    }, _ => Task.FromResult(-1));

async ValueTask<List<object[]>> ReadFileAsync(StreamReader reader, char separator)
{
    var result = Enumerable.Empty<object[]>().ToList();

    while (!reader.EndOfStream)
        result.Add((await reader.ReadLineAsync() ?? "").Trim().Split(separator).Select(s => s as object).ToArray());

    return result;
}

async Task BuildTree(IReadOnlyList<object[]> data)
{
    var decisions = data.Select(x => x.Last()).GroupBy(x=> x).Select(x=>x.Key).ToArray();
    if (decisions.Length != 1)
    {
        ++tabs;
        var idx = await FindTheBest(data);
        stringBuilder.Append($"Attribute: {idx}");
        var sort = await Sort(idx, data);
        foreach (var d in sort)
        {
            stringBuilder.Append('\n');
            for (var i = 0; i < tabs; i++)
                stringBuilder.Append('\t');
            stringBuilder.Append($"{d[0][idx]} -> ");

            await BuildTree(d);
        }

        --tabs;
    }
    else
        stringBuilder.Append($"Decision: {decisions[0]}");
}

Task<List<object[][]>> Sort(int idx, IReadOnlyList<object[]> data)
{
    var attribute = data.Select(el => el[idx]);
    var result = new List<object[][]>();
    
    foreach (var uniq in attribute.GroupBy(x => x))
    {
        var temp = new List<object[]>();
        foreach (var d in data)
            if (d[idx].Equals(uniq.Key))
                temp.Add(d);
        result.Add(temp.ToArray());
    }

    return Task.FromResult(result);
}

ValueTask<int> FindTheBest(IReadOnlyList<object[]> data)
{
    var temporary = Enumerable.Range(0, data[0].Length - 1).Select(idx => Calculate(idx, data).Result).ToArray();
    var max = temporary.Max();
    return ValueTask.FromResult(Array.IndexOf(temporary, max));
}

async ValueTask<double> Calculate(int idx, IReadOnlyList<object[]> data)
{
    var values = data.Select(el => el[idx]).ToArray();
    var classes = values.GroupBy(x => x).ToDictionary(x => x.Key,
        y => y.Select(g => g).Sum(_ => 1));
    var decisions = data.Select(el => el.LastOrDefault()!).ToArray();

    var probabilities = classes.Values.Select(v => v / (double) values.Length).ToArray();
    var infoT = await Math.Info(decisions.GroupBy(o => o)
        .ToDictionary(grouping => grouping.Key, grouping => grouping.Select(o => o).Sum(_ => 1))
        .Values.Select(i => (double) i / decisions.Length)
        .ToArray());
    var infoAnT = await Math.Info(probabilities, values, decisions);
    var gainAnT = await Math.Gain(infoT, infoAnT);
    var splitInfoAnT = await Math.SplitInfo(probabilities);

    return await Math.GainRatio(gainAnT, splitInfoAnT);
}
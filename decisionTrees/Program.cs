using CommandLine;
using System.Diagnostics;
using decisionTrees;
using Math = decisionTrees.Math;

 int tabs = 0;
await Parser.Default.ParseArguments<CommandLineOptions>(args)
    .MapResult(async args =>
    {
        var stopwatch = Stopwatch.StartNew();
        using StreamReader reader = new(args.Path ?? string.Empty);
        // var (trainingSet, testSet) = await SplitData(ReadFileAsync(reader, args.Separator));

        var node = new Node(await ReadFileAsync(reader, args.Separator));
        await BuildTree(node);
        await DisplayTree(node);
        stopwatch.Stop();
        Console.WriteLine("\nElapsed time in milliseconds: {0}", stopwatch.ElapsedMilliseconds);
        return 0;
    }, _ => Task.FromResult(-1));

async ValueTask<List<object[]>> ReadFileAsync(StreamReader reader, char separator)
{
    var result = Enumerable.Empty<object[]>().ToList();

    while (!reader.EndOfStream)
        result.Add((await reader.ReadLineAsync() ?? "").Trim().Split(separator).Select(s => s as object).ToArray());

    return result;
}

async ValueTask<(IEnumerable<object[]> trainingSet, IEnumerable<object[]> testSet)> SplitData(ValueTask<List<object[]>> readFileAsync)
{
    var data = await readFileAsync;

    var min = (int) (data.Count * 0.1);
    min = min == 0 ? 1 : min;
    var max = (int) (data.Count * 0.3);
    var testSet = data.OrderBy(_ => Random.Shared.Next()).Take(Random.Shared.Next(min, max)).ToList();
    foreach (var x in testSet)
        data.Remove(x);

    return (data, testSet);
}

async Task BuildTree(Node node)
{
    var decisions = node.Data.Select(x => x.Last()).GroupBy(x => x).Select(x => x.Key).ToArray();
    var (idx, ratio) = await FindTheBest(node.Data);
    if (ratio != 0)
    {
        node.Attribute = idx;
        var sort = await Sort(idx, node.Data);
        foreach (var d in sort)
        {
            var child = new Node(d)
            {
                Value = d[0][idx]
            };
            node.Childs.Add(child);
            await BuildTree(child);
        }
    }
    else
        node.Decision = decisions[0];
}

async Task DisplayTree(Node tree)
{
    if (tree.Childs.Count != 0)
    {
        ++tabs;
        Console.Write($"Attribute: {tree.Attribute}");
        foreach (var child in tree.Childs)
        {
            Console.Write('\n');
            for (var i = 0; i < tabs; i++) Console.Write('\t');
            Console.Write($"{child.Value} -> ");

            await DisplayTree(child);
        }
        --tabs;
    }
    else
        Console.Write($"Decision: {tree.Decision}");
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

ValueTask<(int idx, double ratio)> FindTheBest(IReadOnlyList<object[]> data)
{
    var temporary = Enumerable.Range(0, data[0].Length - 1).Select(idx => Calculate(idx, data).Result).ToArray();
    var max = temporary.Max();
    return ValueTask.FromResult((Array.IndexOf(temporary, max), max));
}

async ValueTask<double> Calculate(int idx, IReadOnlyList<object[]> data)
{
    var values = data.Select(el => el[idx]).ToArray();
    var classes = values.GroupBy(x => x).ToDictionary(x => x.Key, y => y.Select(g => g).Sum(_ => 1));
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

internal class Node
{
    public IReadOnlyList<object[]> Data { get; }
    public List<Node> Childs { get; }
    public object? Attribute { get; set; }
    public object? Decision { get; set; }
    public object? Value { get; set; }

    public Node(IReadOnlyList<object[]> data)
    {
        Data = data;
        Childs = new List<Node>();
    }
}

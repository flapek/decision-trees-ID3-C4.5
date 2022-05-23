using CommandLine;
using System.Diagnostics;
using decisionTrees;
using Math = decisionTrees.Math;

await Parser.Default.ParseArguments<CommandLineOptions>(args)
    .MapResult(async args =>
    {
        var stopwatch = Stopwatch.StartNew();
        using StreamReader reader = new(args.Path ?? string.Empty);
        var (trainingSet, testSet) = await SplitData(ReadFileAsync(reader, args.Separator));

        trainingSet = trainingSet.ToList();
        var node = new Node(trainingSet.ToList());
        await BuildTree(node);
        // await DisplayTree(node, 0);

        var (confusionMatrix, notClassified) = await BuildConfusionMatrix(testSet.ToList(), node,
            trainingSet.ToList().Select(x => x.Last()).GroupBy(x => x).Select(x => x.Key).ToArray());


        await Display(confusionMatrix);
        Console.WriteLine("Not classified {0}", notClassified);
        Console.WriteLine("Accuracy {0}", await Accuracy(confusionMatrix));
        var sensitivity = await Sensitivity(confusionMatrix);
        Console.WriteLine("Sensitivity {0}", sensitivity);
        var precision = await Precision(confusionMatrix);
        Console.WriteLine("Precision {0}", precision);
        Console.WriteLine("F-measure {0}", await FMeasure(precision, sensitivity));
        Console.WriteLine("Matthews correlation coefficient {0}", await MatthewsCorrelationCoefficient(confusionMatrix));
        
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

async ValueTask<(IEnumerable<object[]> trainingSet, IEnumerable<object[]> testSet)> SplitData(
    ValueTask<List<object[]>> readFileAsync)
{
    var data = await readFileAsync;

    var min = (int) (data.Count * 0.1);
    min = min == 0 ? 1 : min;
    var max = (int) (data.Count * 0.3);
    var testSet = data.OrderBy(_ => Random.Shared.Next()).Take(Random.Shared.Next(min, max)).ToList();
    foreach (var x in testSet) data.Remove(x);

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
        foreach (var child in sort.Select(d => new Node(d) {Value = d[0][idx]}))
        {
            node.Nodes.Add(child);
            await BuildTree(child);
        }
    }
    else
        node.Decision = decisions[0];
}

// ReSharper disable once UnusedLocalFunction
async Task DisplayTree(Node tree, int tabs)
{
    if (tree.Nodes.Count != 0)
    {
        ++tabs;
        Console.Write($"Attribute: {tree.Attribute}");
        foreach (var child in tree.Nodes)
        {
            Console.Write('\n');
            for (var i = 0; i < tabs; i++) Console.Write('\t');
            Console.Write($"{child.Value} -> ");

            await DisplayTree(child, ++tabs);
        }
        --tabs;
    }
    else
        Console.Write($"Decision: {tree.Decision}");
}

Task Display(int[,] confusionMatrix)
{
    Console.WriteLine("Fail matrix:");
    for (var i = 0; i < confusionMatrix.GetLength(0); i++)
    {
        Console.Write("| ");
        for (var j = 0; j < confusionMatrix.GetLength(0); j++)
        {
            Console.Write("{0} ", confusionMatrix[i, j]); 
        }
        Console.WriteLine(" |");
    }
    return Task.CompletedTask;
}

Task<List<object[][]>> Sort(int idx, IReadOnlyList<object[]> data)
{
    var attribute = data.Select(el => el[idx]);
    var result = attribute.GroupBy(x => x).Select(uniq => data.Where(d => d[idx].Equals(uniq.Key)).ToArray()).ToList();

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

async Task<(int[,] confusionMatrix, int notClassified)> BuildConfusionMatrix(IEnumerable<object[]> testSet, Node tree, object[] decisions)
{
    var result = new int[decisions.Length, decisions.Length];
    var notClassified = 0;
    foreach (var test in testSet)
    {
        var (testDecision, treeDecision) = await Test(test, tree);
        var indexOfTestDecision = Array.IndexOf(decisions, testDecision);
        var indexOfTreeDecision = Array.IndexOf(decisions, treeDecision);

        if (indexOfTestDecision == -1 || indexOfTreeDecision == -1)
        {
            ++notClassified;
            continue;
        }

        ++result[indexOfTestDecision, indexOfTreeDecision];
    }

    return (result, notClassified);
}

ValueTask<(object testDecision, object? treeDecision)> Test(IReadOnlyList<object> test, Node tree)
{
    var node = tree;
    while (node != null && node.Nodes.Any())
    {
        var a = test[(int) node.Attribute];
        node = node.Nodes.FirstOrDefault(x => x.Value != null && x.Value.Equals(a));
    }

    return ValueTask.FromResult((test[^1], node?.Decision));
}

ValueTask<double> Accuracy(int[,] confusionMatrix)
{
    var numerator = 0d;
    var denominator = 0d;

    for (var i = 0; i < confusionMatrix.GetLength(0); i++)
    {
        for (var j = 0; j < confusionMatrix.GetLength(0); j++)
        {
            if (i == j)
                numerator += confusionMatrix[i, j];
            else
                denominator += confusionMatrix[i, j];
        }
    }
    
    return ValueTask.FromResult(denominator != 0 ? numerator/denominator : 0d);
}

ValueTask<double> Sensitivity(int[,] confusionMatrix)
{
    var numerator = (double)confusionMatrix[0,0];
    var denominator = 0d;

    for (var i = 0; i < confusionMatrix.GetLength(0); i++)
        denominator += confusionMatrix[0, i];
    
    return ValueTask.FromResult(denominator != 0 ? numerator/denominator : 0d);
}

ValueTask<double> Precision(int[,] confusionMatrix)
{
    var numerator = (double)confusionMatrix[0,0];
    var denominator = 0d;

    for (var i = 0; i < confusionMatrix.GetLength(0); i++)
        denominator += confusionMatrix[i, 0];
    
    return ValueTask.FromResult(denominator != 0 ? numerator/denominator : 0d);
}

ValueTask<double> FMeasure(double precision, double sensitivity) 
    => ValueTask.FromResult(precision * sensitivity/(precision + sensitivity));

ValueTask<double> MatthewsCorrelationCoefficient(int[,] confusionMatrix) =>
    ValueTask.FromResult((confusionMatrix[0,0] * confusionMatrix[1,1] - confusionMatrix[0,1] * confusionMatrix[1,0]) / 
                         System.Math.Sqrt((confusionMatrix[0,0] + confusionMatrix[1,0]) * (confusionMatrix[0,0] + confusionMatrix[0,1]) * 
                                          (confusionMatrix[1,0] + confusionMatrix[1,1]) * (confusionMatrix[1,1] + confusionMatrix[0,1])));

internal class Node
{
    public IReadOnlyList<object[]> Data { get; }
    public List<Node> Nodes { get; }
    public int? Attribute { get; set; }
    public object? Decision { get; set; }
    public object? Value { get; init; }

    public Node(IReadOnlyList<object[]> data)
    {
        Data = data;
        Nodes = new List<Node>();
    }
}
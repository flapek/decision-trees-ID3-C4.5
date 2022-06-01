namespace decisionTrees;

public static class ArrayExtensions
{
    public static async ValueTask<(IEnumerable<object[]> trainingSet, IEnumerable<object[]> testSet)> SplitData(
        this ValueTask<List<object[]>> readFileAsync, double percentageToTake)
    {
        var data = await readFileAsync;

        var take = (int) (data.Count * percentageToTake);
        var testSet = data.OrderBy(_ => Random.Shared.Next()).Take(take).ToList();
        foreach (var x in testSet) data.Remove(x);

        return (data, testSet);
    }

    public static async ValueTask<IEnumerable<IEnumerable<object[]>>> SplitData(
        this ValueTask<List<object[]>> readFileAsync, int countOfSets)
    {
        var data = await readFileAsync;
        var result = new List<IEnumerable<object[]>>();

        var take = data.Count / countOfSets;

        for (var i = 0; i < countOfSets; i++)
        {
            var set = i == countOfSets - 1 
                ? data.ToArray()
                : data.OrderBy(_ => Random.Shared.Next()).Take(take).ToArray();
            foreach (var x in set) data.Remove(x);
            result.Add(set);
        }

        return result;
    }

    public static Task Display(this int[,] confusionMatrix)
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

    public static async Task<(int[,] confusionMatrix, int notClassified)> BuildConfusionMatrix(this Node tree,
        IEnumerable<object[]> testSet, object[] decisions)
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

    public static Task<List<object[][]>> Sort(this IReadOnlyList<object[]> data, int idx)
    {
        var attribute = data.Select(el => el[idx]);
        var result = attribute.GroupBy(x => x)
            .Select(uniq => data.Where(d => d[idx].Equals(uniq.Key)).ToArray())
            .ToList();

        return Task.FromResult(result);
    }

    public static async ValueTask<(int idx, double ratio)> FindTheBest(this IReadOnlyList<object[]> data)
    {
        var temporary = new List<double>();

        foreach (var idx in Enumerable.Range(0, data[0].Length - 1)) temporary.Add(await Calculate(idx, data));

        var max = temporary.Max();
        return (Array.IndexOf(temporary.ToArray(), max), max);
    }

    public static Task<IEnumerable<object[]>> Zip(this IEnumerable<IEnumerable<object[]>>? list)
    {
        var result = new List<object[]>();

        if (list == null) return Task.FromResult((IEnumerable<object[]>) result);
        
        foreach (var item in list)
            result.AddRange(item);

        return Task.FromResult((IEnumerable<object[]>)result);
    }

    public static ValueTask<int[,]> Zip(this int[,] firstArray, int[,] secondArray)
    {
        if (firstArray.GetLength(0) == 0)
            firstArray = new int[secondArray.GetLength(0), secondArray.GetLength(1)];
        
        for (var i = 0; i < secondArray.GetLength(0); i++)
            for (var j = 0; j < secondArray.GetLength(1); j++)
                firstArray[i, j] += secondArray[i, j];
        
        return ValueTask.FromResult(firstArray);
    }

    private static ValueTask<(object testDecision, object? treeDecision)> Test(IReadOnlyList<object> test, Node tree)
    {
        var node = tree;
        while (node != null && node.Nodes.Any())
            node = node.Nodes.FirstOrDefault(x => x.Value != null && x.Value.Equals(test[node.Attribute ?? 0]));

        return ValueTask.FromResult((test[^1], node?.Decision));
    }

    private static async ValueTask<double> Calculate(int idx, IReadOnlyList<object[]> data)
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
}
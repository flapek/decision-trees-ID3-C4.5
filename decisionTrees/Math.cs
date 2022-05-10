namespace decisionTrees;

internal static class Math
{
    public static ValueTask<double> Info(double[] probabilities)
        => new(-probabilities.Sum(p => p == 0 ? 0 : p * System.Math.Log2(p)));

    public static async ValueTask<double> Info(double[] probabilities, object[] classes, object[] decisions)
    {
        double result = 0;
        var decisionsCount = decisions.GroupBy(x => x).Select(x => x.Key).ToArray();
        var classesTypes = classes.GroupBy(x => x)
            .ToDictionary(x => x.Key,
                y => y.Select(g => g).Sum(s => 1));
        for (int i = 0; i < probabilities.Length; i++)
        {
            List<double> classProbabilities = new();

            foreach (var decision in decisionsCount)
            {
                var (@class, classCount) = classesTypes.ElementAt(i);
                var value = classes.Where((t, j) =>
                        t.ToString() == @class.ToString() && decisions[j].ToString() == decision.ToString())
                    .Aggregate<object, double>(0, (current, t) => current + 1);
                classProbabilities.Add(value / classCount);
            }

            var v = await Info(classProbabilities.ToArray());
            result += probabilities[i] * v;
        }

        return result;
    }

    public static ValueTask<double> Gain(double infoT, double infoAnT)
        => new(infoT - infoAnT);

    public static ValueTask<double> GainRatio(double gainAnT, double splitInfoAnT) 
        => new(splitInfoAnT == 0 ? 0 : gainAnT / splitInfoAnT);

    public static async Task<double> SplitInfo(double[] probabilities) 
        => await Info(probabilities);
}
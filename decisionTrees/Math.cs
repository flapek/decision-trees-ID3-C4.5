namespace decisionTrees;

internal static class Math
{
    public static ValueTask<double> Info(IEnumerable<double> probabilities) =>
        new(-probabilities.Sum(p => p == 0 ? 0 : p * System.Math.Log2(p)));

    public static async ValueTask<double> Info(double[] probabilities, object[] classes, object[] decisions)
    {
        double result = 0;
        var decisionsCount = decisions.GroupBy(x => x).Select(x => x.Key).ToArray();
        var classesTypes = classes.GroupBy(x => x).
            ToDictionary(x => x.Key, y => y.Select(g => g).Sum(s => 1));
        for (var i = 0; i < probabilities.Length; i++)
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

    public static ValueTask<double> Gain(double infoT, double infoAnT) => new(infoT - infoAnT);

    public static ValueTask<double> GainRatio(double gainAnT, double splitInfoAnT) =>
        new(splitInfoAnT == 0 ? 0 : gainAnT / splitInfoAnT);

    public static async Task<double> SplitInfo(double[] probabilities) => await Info(probabilities);

    public static ValueTask<double> Accuracy(int[,] confusionMatrix)
    {
        var numerator = 0d;
        var denominator = 0d;

        for (var i = 0; i < confusionMatrix.GetLength(0); i++)
        {
            for (var j = 0; j < confusionMatrix.GetLength(0); j++)
            {
                if (i == j) numerator += confusionMatrix[i, j];
                denominator += confusionMatrix[i, j];
            }
        }

        return ValueTask.FromResult(denominator != 0 ? numerator / denominator : 0d);
    }

    public static ValueTask<double> Recall(int[,] confusionMatrix) =>
        confusionMatrix.GetLength(0) > 2
            ? RecallForNDecisionClass(confusionMatrix)
            : RecallForTwoDecisionClass(confusionMatrix);

    private static ValueTask<double> RecallForNDecisionClass(int[,] confusionMatrix)
    {
        var result = 0d;

        for (var i = 0; i < confusionMatrix.GetLength(0); i++)
        {
            var numerator = (double) confusionMatrix[i, i];
            var denominator = 0d;

            for (var j = 0; j < confusionMatrix.GetLength(0); j++) denominator += confusionMatrix[i, j];

            result += denominator != 0 ? numerator / denominator : 0d;
        }

        return ValueTask.FromResult(1d / confusionMatrix.GetLength(0) * result);
    }

    private static ValueTask<double> RecallForTwoDecisionClass(int[,] confusionMatrix)
    {
        var numerator = (double) confusionMatrix[0, 0];
        var denominator = 0d;

        for (var i = 0; i < confusionMatrix.GetLength(0); i++) denominator += confusionMatrix[0, i];

        return ValueTask.FromResult(denominator != 0 ? numerator / denominator : 0d);
    }

    public static ValueTask<double> Precision(int[,] confusionMatrix) =>
        confusionMatrix.GetLength(0) > 2
            ? PrecisionForNDecisionClass(confusionMatrix)
            : PrecisionForTwoDecisionClass(confusionMatrix);

    private static ValueTask<double> PrecisionForNDecisionClass(int[,] confusionMatrix)
    {
        var result = 0d;

        for (var i = 0; i < confusionMatrix.GetLength(0); i++)
        {
            var numerator = (double) confusionMatrix[i, i];
            var denominator = 0d;

            for (var j = 0; j < confusionMatrix.GetLength(0); j++) denominator += confusionMatrix[j, i];

            result += denominator != 0 ? numerator / denominator : 0d;
        }

        return ValueTask.FromResult(1d / confusionMatrix.GetLength(0) * result);
    }

    private static ValueTask<double> PrecisionForTwoDecisionClass(int[,] confusionMatrix)
    {
        var numerator = (double) confusionMatrix[0, 0];
        var denominator = 0d;

        for (var i = 0; i < confusionMatrix.GetLength(0); i++) denominator += confusionMatrix[i, 0];

        return ValueTask.FromResult(denominator != 0 ? numerator / denominator : 0d);
    }

    public static ValueTask<double> FMeasure(double precision, double sensitivity) =>
        ValueTask.FromResult(precision * sensitivity / (precision + sensitivity));

    public static ValueTask<double> MatthewsCorrelationCoefficient(int[,] confusionMatrix) =>
        ValueTask.FromResult(
            (confusionMatrix[0, 0] * confusionMatrix[1, 1] - confusionMatrix[0, 1] * confusionMatrix[1, 0]) /
            System.Math.Sqrt((confusionMatrix[0, 0] + confusionMatrix[1, 0]) *
                             (confusionMatrix[0, 0] + confusionMatrix[0, 1]) *
                             (confusionMatrix[1, 0] + confusionMatrix[1, 1]) *
                             (confusionMatrix[1, 1] + confusionMatrix[0, 1])));
}
internal static class Math
{
    public static double Info(double[] propabilities)
        => -propabilities.Sum(p => p == 0 ? 0 : p * System.Math.Log2(p));

    public static double Info(double[] propabilities, object[] classes, object[] decisions)
    {
        double result = 0;
        var decisionsCount = decisions.GroupBy(x => x).Select(x => x.Key);
        var classesTypes = classes.GroupBy(x => x)
            .ToDictionary(x => x.Key,
            y => y.Select(g => g).Sum(s => 1));
        for (int i = 0; i < propabilities.Length; i++)
        {
            List<double> classPropabilities = new();

            foreach (var decision in decisionsCount)
            {
                double value = 0;
                var element = classesTypes.ElementAt(i);
                for (int j = 0; j < classes.Length; j++)
                {
                    if (classes[j].ToString() == element.Key.ToString())
                    {
                        if (decisions[j].ToString() == decision.ToString())
                        {
                            value += 1;
                        }
                    }
                }
                classPropabilities.Add(value / element.Value);
            }

            double v = Info(classPropabilities.ToArray());
            result += propabilities[i] * v;
        }

        return result;
    }
}

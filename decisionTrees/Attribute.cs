namespace decisionTrees;

internal class Attribute
{
    public int Index { get; }
    public IEnumerable<object> Values { get; }
    public Dictionary<object, int> Classes { get; }
    public double InfoT { get; private set; }
    public double InfoAnT { get; private set; }
    public double GainAnT { get; private set; }
    public double SplitInfoAnT { get; private set; }
    public double GainRatioAnT { get; private set; }

    private readonly double _objects;

    public Attribute(int index, object[] values, double infoT)
    {
        Index = index;
        Values = values ?? throw new ArgumentNullException(nameof(values));
        InfoT = infoT;
        Classes = values.GroupBy(x => x).ToDictionary(x => x.Key,
            y => y.Select(g => g).Sum(s => 1));
        _objects = values.Length;
    }

    internal async Task Calculate(object[] decisions)
    {
        var probabilities = Classes.Values.Select(v => v / _objects).ToArray();
        InfoAnT = await Math.Info(probabilities, Values.ToArray(), decisions);
        GainAnT = await Math.Gain(InfoT, InfoAnT);
        SplitInfoAnT = await Math.SplitInfo(probabilities);
        GainRatioAnT = await Math.GainRatio(GainAnT, SplitInfoAnT);
    }
}
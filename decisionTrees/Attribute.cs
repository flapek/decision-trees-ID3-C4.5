internal class Attribute
{
    public int Index { get; }
    public Dictionary<object, int> Classes { get; }
    public double Entropy { get; }
    public double InformationValue { get; private set; }

    private readonly double _objects;
    public Attribute(int index, Dictionary<object, int> classes)
    {
        Index = index;
        Classes = classes;
        
        _objects = Classes.Select(x => x.Value).Sum(x => x);
        Entropy = Math.Info(Classes.Values.Select(v => v / _objects).ToArray());
    }

    internal void Info(object[] classes, object[] decisions)
        => InformationValue = Math.Info(Classes.Values.Select(v => v / _objects).ToArray(), classes, decisions);
}
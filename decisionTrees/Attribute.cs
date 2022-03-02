internal class Attribute
{
    public int Index { get; }
    public Dictionary<object, int> Keys { get; }

    public Attribute(int index, Dictionary<object, int> keys)
    {
        Index = index;
        Keys = keys;
    }
}
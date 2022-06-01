public class Node
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